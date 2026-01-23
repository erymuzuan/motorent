using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Manager end-of-day verification operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Gets all sessions for a specific date for manager verification.
    /// Note: Uses chained Where calls because Repository only supports simple predicates.
    /// </summary>
    public async Task<List<TillSession>> GetSessionsForVerificationAsync(int shopId, DateTime date)
    {
        var startOfDay = new DateTimeOffset(date.Date);
        var endOfDay = startOfDay.AddDays(1);

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId && s.OpenedAt >= startOfDay && s.OpenedAt < endOfDay)
                .OrderBy(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets the daily summary for EOD reconciliation.
    /// </summary>
    public async Task<DailyTillSummary> GetDailySummaryAsync(int shopId, DateTime date)
    {
        var sessions = await this.GetSessionsForVerificationAsync(shopId, date);

        var summary = new DailyTillSummary
        {
            Date = date,
            ShopId = shopId,
            TotalSessions = sessions.Count,
            VerifiedSessions = sessions.Count(s => s.Status == TillSessionStatus.Verified),
            SessionsWithVariance = sessions.Count(s => s.Status == TillSessionStatus.ClosedWithVariance),
            TotalCashIn = sessions.Sum(s => s.TotalCashIn),
            TotalCashOut = sessions.Sum(s => s.TotalCashOut),
            TotalCardPayments = sessions.Sum(s => s.TotalCardPayments),
            TotalBankTransfers = sessions.Sum(s => s.TotalBankTransfers),
            TotalPromptPay = sessions.Sum(s => s.TotalPromptPay),
            TotalDropped = sessions.Sum(s => s.TotalDropped),
            TotalVariance = sessions.Sum(s => s.Variance),
            Sessions = sessions.Select(s => new TillSessionSummary
            {
                TillSessionId = s.TillSessionId,
                StaffDisplayName = s.StaffDisplayName,
                OpeningFloat = s.OpeningFloat,
                TotalCashIn = s.TotalCashIn,
                TotalCashOut = s.TotalCashOut,
                TotalDropped = s.TotalDropped,
                TotalToppedUp = s.TotalToppedUp,
                ExpectedCash = s.ExpectedCash,
                ActualCash = s.ActualCash,
                Variance = s.Variance,
                Status = s.Status,
                IsVerified = s.Status == TillSessionStatus.Verified,
                OpenedAt = s.OpenedAt,
                ClosedAt = s.ClosedAt
            }).ToList()
        };

        return summary;
    }

    /// <summary>
    /// Verifies a session (manager approval).
    /// </summary>
    public async Task<SubmitOperation> VerifySessionAsync(
        int sessionId,
        string managerUserName,
        string? notes = null)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status is not TillSessionStatus.Closed and not TillSessionStatus.ClosedWithVariance)
            return SubmitOperation.CreateFailure("Session must be closed before verification");

        // Prevent self-verification
        if (string.Equals(session.StaffUserName, managerUserName, StringComparison.OrdinalIgnoreCase))
            return SubmitOperation.CreateFailure("You cannot verify your own session");

        session.Status = TillSessionStatus.Verified;
        session.VerifiedByUserName = managerUserName;
        session.VerifiedAt = DateTimeOffset.Now;
        session.VerificationNotes = notes;

        using var persistenceSession = this.Context.OpenSession(managerUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("VerifyTillSession");
    }

    /// <summary>
    /// Verifies an individual transaction.
    /// </summary>
    public async Task<SubmitOperation> VerifyTransactionAsync(
        int transactionId,
        string managerUserName)
    {
        var transaction = await this.Context.LoadOneAsync<TillTransaction>(t => t.TillTransactionId == transactionId);
        if (transaction is null)
            return SubmitOperation.CreateFailure("Transaction not found");

        transaction.IsVerified = true;
        transaction.VerifiedByUserName = managerUserName;
        transaction.VerifiedAt = DateTimeOffset.Now;

        using var persistenceSession = this.Context.OpenSession(managerUserName);
        persistenceSession.Attach(transaction);
        return await persistenceSession.SubmitChanges("VerifyTillTransaction");
    }

    /// <summary>
    /// Gets unverified cash drops for a date.
    /// </summary>
    public async Task<List<TillTransaction>> GetUnverifiedDropsAsync(int shopId, DateTime date)
    {
        var sessions = await this.GetSessionsForVerificationAsync(shopId, date);
        var sessionIds = sessions.Select(s => s.TillSessionId).ToList();

        if (sessionIds.Count == 0)
            return [];

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.TransactionType == TillTransactionType.Drop && !t.IsVerified),
            page: 1, size: 1000, includeTotalRows: false);

        // Filter by session IDs (since we can't do IsInList in the query easily)
        return result.ItemCollection
            .Where(t => sessionIds.Contains(t.TillSessionId))
            .OrderBy(t => t.TransactionTime)
            .ToList();
    }

    /// <summary>
    /// Gets card/electronic payments summary for a date.
    /// </summary>
    public async Task<List<TillTransaction>> GetCardPaymentsSummaryAsync(int shopId, DateTime date)
    {
        var sessions = await this.GetSessionsForVerificationAsync(shopId, date);
        var sessionIds = sessions.Select(s => s.TillSessionId).ToList();

        if (sessionIds.Count == 0)
            return [];

        var nonCashTypes = new[]
        {
            TillTransactionType.CardPayment,
            TillTransactionType.BankTransfer,
            TillTransactionType.PromptPay
        };

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillTransaction>()
                .Where(t => t.Direction == TillTransactionDirection.In),
            page: 1, size: 10000, includeTotalRows: false);

        return result.ItemCollection
            .Where(t => sessionIds.Contains(t.TillSessionId))
            .Where(t => nonCashTypes.Contains(t.TransactionType))
            .OrderBy(t => t.TransactionTime)
            .ToList();
    }

    #region Manager Dashboard Methods

    /// <summary>
    /// Gets all currently active (open) till sessions for a shop.
    /// Used by manager dashboard to show real-time session status.
    /// </summary>
    public async Task<List<TillSession>> GetAllActiveSessionsAsync(int shopId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId && s.Status == TillSessionStatus.Open)
                .OrderBy(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets recently closed till sessions for verification review.
    /// Excludes Open and Reconciling statuses.
    /// </summary>
    public async Task<List<TillSession>> GetRecentClosedSessionsAsync(int shopId, int days = 7)
    {
        var cutoffDate = DateTimeOffset.Now.AddDays(-days);

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId
                    && s.Status != TillSessionStatus.Open
                    && s.Status != TillSessionStatus.Reconciling
                    && s.ClosedAt >= cutoffDate)
                .OrderByDescending(s => s.ClosedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Calculates total variance in THB across all currencies.
    /// Uses current exchange rates to convert foreign currency variances.
    /// </summary>
    public async Task<decimal> GetTotalVarianceInThbAsync(TillSession session)
    {
        if (session.ClosingVariances.Count == 0)
            return session.Variance; // Fall back to single-currency variance

        decimal totalThb = 0;
        foreach (var (currency, variance) in session.ClosingVariances)
        {
            if (currency == SupportedCurrencies.THB)
            {
                totalThb += variance;
            }
            else
            {
                var rate = await this.ExchangeRateService.GetCurrentRateAsync(currency);
                var buyRate = rate?.BuyRate ?? 1m;
                totalThb += variance * buyRate;
            }
        }
        return totalThb;
    }

    /// <summary>
    /// Counts sessions with variance exceeding the threshold.
    /// Checks both unverified closed sessions from today.
    /// </summary>
    public async Task<int> GetVarianceAlertCountAsync(int shopId, decimal thresholdThb)
    {
        var recentClosed = await GetRecentClosedSessionsAsync(shopId, days: 1);

        var unverified = recentClosed
            .Where(s => s.Status is TillSessionStatus.Closed or TillSessionStatus.ClosedWithVariance);

        int count = 0;
        foreach (var session in unverified.Where(s => s.ClosingVariances.Any() || s.Variance != 0))
        {
            var totalVariance = await GetTotalVarianceInThbAsync(session);
            if (Math.Abs(totalVariance) > thresholdThb)
                count++;
        }
        return count;
    }

    #endregion
}
