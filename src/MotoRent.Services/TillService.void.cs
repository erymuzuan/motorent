using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Void and refund operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Voids a transaction with manager approval.
    /// Creates a compensating entry and marks original as voided.
    /// </summary>
    /// <param name="transactionId">Transaction to void</param>
    /// <param name="staffUserName">Staff initiating the void</param>
    /// <param name="managerUserName">Manager approving the void (must be different from staff)</param>
    /// <param name="reason">Reason for voiding</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> VoidTransactionAsync(
        int transactionId,
        string staffUserName,
        string managerUserName,
        string reason)
    {
        // Prevent self-approval
        if (staffUserName.Equals(managerUserName, StringComparison.OrdinalIgnoreCase))
            return SubmitOperation.CreateFailure("Staff cannot approve their own void");

        var original = await this.Context.LoadOneAsync<TillTransaction>(t => t.TillTransactionId == transactionId);
        if (original is null)
            return SubmitOperation.CreateFailure("Transaction not found");

        if (original.IsVoided)
            return SubmitOperation.CreateFailure("Transaction is already voided");

        // Check session is still open
        var session = await this.GetSessionByIdAsync(original.TillSessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Cannot void transactions in a closed session");

        // Create compensating entry (reverse direction)
        var compensating = new TillTransaction
        {
            TillSessionId = original.TillSessionId,
            TransactionType = TillTransactionType.VoidReversal,
            Direction = original.Direction == TillTransactionDirection.In
                ? TillTransactionDirection.Out
                : TillTransactionDirection.In,
            Amount = original.Amount,
            Currency = original.Currency,
            ExchangeRate = original.ExchangeRate,
            AmountInBaseCurrency = original.AmountInBaseCurrency,
            ExchangeRateSource = original.ExchangeRateSource,
            ExchangeRateId = original.ExchangeRateId,
            Description = $"VOID: {original.Description}",
            OriginalTransactionId = transactionId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = staffUserName,
            Notes = $"Voided by {managerUserName}: {reason}",
            PaymentId = original.PaymentId,
            DepositId = original.DepositId,
            RentalId = original.RentalId
        };

        // Mark original as voided
        original.IsVoided = true;
        original.VoidedAt = DateTimeOffset.Now;
        original.VoidedByUserName = staffUserName;
        original.VoidReason = reason;
        original.VoidApprovedByUserName = managerUserName;

        // Update session balances (reverse the original effect)
        if (original.AffectsCash)
        {
            if (original.Direction == TillTransactionDirection.In)
            {
                session.TotalCashIn -= original.AmountInBaseCurrency;
                // Also reverse the currency balance
                if (session.CurrencyBalances.ContainsKey(original.Currency))
                    session.CurrencyBalances[original.Currency] -= original.Amount;
            }
            else
            {
                session.TotalCashOut -= original.AmountInBaseCurrency;
                // For outflows, add back to currency balance
                if (session.CurrencyBalances.ContainsKey(original.Currency))
                    session.CurrencyBalances[original.Currency] += original.Amount;
            }
        }

        // Handle non-cash payment type totals
        switch (original.TransactionType)
        {
            case TillTransactionType.CardPayment:
                session.TotalCardPayments -= original.AmountInBaseCurrency;
                break;
            case TillTransactionType.BankTransfer:
                session.TotalBankTransfers -= original.AmountInBaseCurrency;
                break;
            case TillTransactionType.PromptPay:
                session.TotalPromptPay -= original.AmountInBaseCurrency;
                break;
            case TillTransactionType.Drop:
                session.TotalDropped -= original.AmountInBaseCurrency;
                break;
            case TillTransactionType.TopUp:
                session.TotalToppedUp -= original.AmountInBaseCurrency;
                break;
        }

        using var persistenceSession = this.Context.OpenSession(staffUserName);
        persistenceSession.Attach(compensating);
        persistenceSession.Attach(original);
        persistenceSession.Attach(session);
        var result = await persistenceSession.SubmitChanges("VoidTransaction");

        // Link original to compensating after save (compensating now has ID)
        if (result.Success)
        {
            original.RelatedTransactionId = compensating.TillTransactionId;
            using var linkSession = this.Context.OpenSession(staffUserName);
            linkSession.Attach(original);
            await linkSession.SubmitChanges("LinkVoidedTransaction");
        }

        return result;
    }

    /// <summary>
    /// Records an overpayment refund to the till.
    /// Always issued in THB cash regardless of original payment currency.
    /// </summary>
    /// <param name="sessionId">Current till session</param>
    /// <param name="refundAmountThb">Refund amount in THB</param>
    /// <param name="reason">Reason for refund</param>
    /// <param name="originalPaymentIds">IDs of original payments being refunded (string IDs from ReceiptPayment)</param>
    /// <param name="rentalId">Related rental ID</param>
    /// <param name="username">Staff recording the refund</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> RecordOverpaymentRefundAsync(
        int sessionId,
        decimal refundAmountThb,
        string reason,
        List<string>? originalPaymentIds,
        int? rentalId,
        string username)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = TillTransactionType.OverpaymentRefund,
            Direction = TillTransactionDirection.Out,
            Amount = refundAmountThb,
            Currency = SupportedCurrencies.THB,
            ExchangeRate = 1.0m,
            AmountInBaseCurrency = refundAmountThb,
            ExchangeRateSource = "Base",
            Description = $"Overpayment refund: {reason}",
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = originalPaymentIds is { Count: > 0 }
                ? $"Original payments: {string.Join(", ", originalPaymentIds)}"
                : null
        };

        // Update session totals
        session.TotalCashOut += refundAmountThb;
        session.CurrencyBalances[SupportedCurrencies.THB] -= refundAmountThb;

        using var persistenceSession = this.Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordOverpaymentRefund");
    }

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    public Task<TillTransaction?> GetTransactionByIdAsync(int transactionId) =>
        this.Context.LoadOneAsync<TillTransaction>(t => t.TillTransactionId == transactionId);

    /// <summary>
    /// Gets voided transactions for a session (for audit view).
    /// Only managers should call this.
    /// </summary>
    public async Task<List<TillTransaction>> GetVoidedTransactionsAsync(int sessionId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId && t.IsVoided == true)
                .OrderByDescending(t => t.VoidedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Checks if a transaction can be voided.
    /// Returns reason if not voidable.
    /// </summary>
    public async Task<(bool CanVoid, string? Reason)> CanVoidTransactionAsync(int transactionId)
    {
        var transaction = await this.GetTransactionByIdAsync(transactionId);
        if (transaction is null)
            return (false, "Transaction not found");

        if (transaction.IsVoided)
            return (false, "Already voided");

        if (transaction.TransactionType == TillTransactionType.VoidReversal)
            return (false, "Cannot void a void reversal");

        var session = await this.GetSessionByIdAsync(transaction.TillSessionId);
        if (session is null)
            return (false, "Session not found");

        if (session.Status != TillSessionStatus.Open)
            return (false, "Session is closed");

        return (true, null);
    }
}
