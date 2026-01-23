using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Multi-currency operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Records a foreign currency payment to the till.
    /// Converts to THB using current exchange rate and tracks both foreign and THB amounts.
    /// </summary>
    /// <param name="sessionId">Till session ID</param>
    /// <param name="type">Transaction type (e.g., RentalPayment, SecurityDeposit)</param>
    /// <param name="currency">Currency code (THB, USD, EUR, CNY)</param>
    /// <param name="foreignAmount">Amount in foreign currency</param>
    /// <param name="description">Transaction description</param>
    /// <param name="username">User recording the transaction</param>
    /// <param name="paymentId">Optional linked payment ID</param>
    /// <param name="depositId">Optional linked deposit ID</param>
    /// <param name="rentalId">Optional linked rental ID</param>
    /// <param name="notes">Optional notes</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> RecordForeignCurrencyPaymentAsync(
        int sessionId,
        TillTransactionType type,
        string currency,
        decimal foreignAmount,
        string description,
        string username,
        int? paymentId = null,
        int? depositId = null,
        int? rentalId = null,
        string? notes = null)
    {
        // For THB, delegate to existing RecordCashInAsync
        if (currency == SupportedCurrencies.THB)
        {
            return await this.RecordCashInAsync(sessionId, type, foreignAmount, description, username,
                paymentId, depositId, rentalId, notes);
        }

        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        // Convert to THB using current exchange rate
        var conversion = await this.ExchangeRateService.ConvertToThbAsync(currency, foreignAmount);
        if (conversion is null)
            return SubmitOperation.CreateFailure($"No exchange rate configured for {currency}");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = type,
            Direction = TillTransactionDirection.In,
            Amount = foreignAmount,
            Currency = currency,
            ExchangeRate = conversion.RateUsed,
            AmountInBaseCurrency = conversion.ThbAmount,
            ExchangeRateSource = conversion.RateSource,
            ExchangeRateId = conversion.ExchangeRateId,
            Description = description,
            PaymentId = paymentId,
            DepositId = depositId,
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Update session totals - always use THB equivalent for reconciliation
        if (transaction.AffectsCash)
            session.TotalCashIn += conversion.ThbAmount;

        // Track actual foreign currency amount in CurrencyBalances
        // Initialize currency key if not exists
        if (!session.CurrencyBalances.ContainsKey(currency))
            session.CurrencyBalances[currency] = 0;
        session.CurrencyBalances[currency] += foreignAmount;

        switch (type)
        {
            case TillTransactionType.CardPayment:
                session.TotalCardPayments += conversion.ThbAmount;
                break;
            case TillTransactionType.BankTransfer:
                session.TotalBankTransfers += conversion.ThbAmount;
                break;
            case TillTransactionType.PromptPay:
                session.TotalPromptPay += conversion.ThbAmount;
                break;
        }

        using var persistenceSession = this.Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordForeignCurrencyPayment");
    }

    /// <summary>
    /// Records a multi-currency cash drop to the safe.
    /// Can drop multiple currencies in a single operation.
    /// </summary>
    /// <param name="sessionId">Till session ID</param>
    /// <param name="drops">List of currency amounts to drop</param>
    /// <param name="username">User recording the drop</param>
    /// <param name="notes">Optional notes</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> RecordMultiCurrencyDropAsync(
        int sessionId,
        List<CurrencyDropAmount> drops,
        string username,
        string? notes = null)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transactions = new List<TillTransaction>();
        decimal totalThbDropped = 0;

        foreach (var drop in drops.Where(d => d.Amount > 0))
        {
            // Validate sufficient balance
            var currentBalance = session.GetCurrencyBalance(drop.Currency);
            if (drop.Amount > currentBalance)
                return SubmitOperation.CreateFailure($"Insufficient {drop.Currency} balance. Available: {currentBalance:N2}, Requested: {drop.Amount:N2}");

            // Get THB equivalent for non-THB currencies
            decimal thbEquivalent;
            decimal exchangeRate = 1.0m;
            string rateSource = "Base";
            int? exchangeRateId = null;

            if (drop.Currency == SupportedCurrencies.THB)
            {
                thbEquivalent = drop.Amount;
            }
            else
            {
                var conversion = await this.ExchangeRateService.ConvertToThbAsync(drop.Currency, drop.Amount);
                if (conversion is null)
                    return SubmitOperation.CreateFailure($"No exchange rate configured for {drop.Currency}");

                thbEquivalent = conversion.ThbAmount;
                exchangeRate = conversion.RateUsed;
                rateSource = conversion.RateSource;
                exchangeRateId = conversion.ExchangeRateId;
            }

            var transaction = new TillTransaction
            {
                TillSessionId = sessionId,
                TransactionType = TillTransactionType.Drop,
                Direction = TillTransactionDirection.Out,
                Amount = drop.Amount,
                Currency = drop.Currency,
                ExchangeRate = exchangeRate,
                AmountInBaseCurrency = thbEquivalent,
                ExchangeRateSource = rateSource,
                ExchangeRateId = exchangeRateId,
                Description = $"Cash drop to safe ({drop.Currency})",
                TransactionTime = DateTimeOffset.Now,
                RecordedByUserName = username,
                Notes = notes
            };

            transactions.Add(transaction);

            // Update currency balance
            session.CurrencyBalances[drop.Currency] -= drop.Amount;
            totalThbDropped += thbEquivalent;
        }

        // Update session total dropped (always in THB equivalent)
        session.TotalDropped += totalThbDropped;

        using var persistenceSession = this.Context.OpenSession(username);
        foreach (var transaction in transactions)
            persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordMultiCurrencyDrop");
    }
}
