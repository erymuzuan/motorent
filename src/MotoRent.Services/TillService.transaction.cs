using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Transaction recording operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Records a cash-in transaction.
    /// </summary>
    public async Task<SubmitOperation> RecordCashInAsync(
        int sessionId,
        TillTransactionType type,
        decimal amount,
        string description,
        string username,
        int? paymentId = null,
        int? depositId = null,
        int? rentalId = null,
        string? notes = null)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = type,
            Direction = TillTransactionDirection.In,
            Amount = amount,
            Currency = SupportedCurrencies.THB,
            ExchangeRate = 1.0m,
            AmountInBaseCurrency = amount, // THB transaction, so same as Amount
            ExchangeRateSource = "Base",
            Description = description,
            PaymentId = paymentId,
            DepositId = depositId,
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Update session totals based on transaction type
        if (transaction.AffectsCash)
        {
            session.TotalCashIn += amount;
            // Also update CurrencyBalances for consistency
            session.CurrencyBalances[SupportedCurrencies.THB] = session.GetCurrencyBalance(SupportedCurrencies.THB) + amount;
        }

        switch (type)
        {
            case TillTransactionType.TopUp:
                session.TotalToppedUp += amount;
                break;
            case TillTransactionType.CardPayment:
                session.TotalCardPayments += amount;
                break;
            case TillTransactionType.BankTransfer:
                session.TotalBankTransfers += amount;
                break;
            case TillTransactionType.PromptPay:
                session.TotalPromptPay += amount;
                break;
        }

        using var persistenceSession = this.Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordCashIn");
    }

    /// <summary>
    /// Records a payout (cash-out) transaction.
    /// </summary>
    public async Task<SubmitOperation> RecordPayoutAsync(
        int sessionId,
        TillTransactionType type,
        decimal amount,
        string description,
        string username,
        string? category = null,
        string? recipientName = null,
        string? receiptNumber = null,
        int? depositId = null,
        int? rentalId = null,
        string? notes = null,
        List<TillAttachment>? attachments = null)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = type,
            Direction = TillTransactionDirection.Out,
            Amount = amount,
            Category = category,
            Description = description,
            RecipientName = recipientName,
            ReceiptNumber = receiptNumber,
            DepositId = depositId,
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Add attachments if provided
        if (attachments is { Count: > 0 })
        {
            transaction.Attachments.AddRange(attachments);
        }

        // Update session totals
        if (transaction.AffectsCash)
            session.TotalCashOut += amount;

        if (type == TillTransactionType.Drop)
            session.TotalDropped += amount;

        using var persistenceSession = this.Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordPayout");
    }

    /// <summary>
    /// Records a cash drop to the safe.
    /// </summary>
    public Task<SubmitOperation> RecordDropAsync(
        int sessionId,
        decimal amount,
        string username,
        string? notes = null) =>
        this.RecordPayoutAsync(
            sessionId,
            TillTransactionType.Drop,
            amount,
            "Cash drop to safe",
            username,
            notes: notes);

    /// <summary>
    /// Records a top-up from the safe.
    /// </summary>
    public Task<SubmitOperation> RecordTopUpAsync(
        int sessionId,
        decimal amount,
        string username,
        string? notes = null) =>
        this.RecordCashInAsync(
            sessionId,
            TillTransactionType.TopUp,
            amount,
            "Top-up from safe",
            username,
            notes: notes);

    /// <summary>
    /// Gets all transactions for a session.
    /// </summary>
    public async Task<List<TillTransaction>> GetTransactionsAsync(int sessionId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId)
                .OrderByDescending(t => t.TransactionTime),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets recent transactions for a session (last N).
    /// </summary>
    public async Task<List<TillTransaction>> GetRecentTransactionsAsync(int sessionId, int count = 10)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId)
                .OrderByDescending(t => t.TransactionTime),
            page: 1, size: count, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets all transactions for a specific till session.
    /// Used for handover reports and session detail views.
    /// Orders by transaction time ascending for chronological display.
    /// </summary>
    public async Task<List<TillTransaction>> GetTransactionsForSessionAsync(int sessionId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId)
                .OrderBy(t => t.TransactionTime),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }
}
