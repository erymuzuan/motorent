using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// End of day operations for till service.
/// </summary>
public partial class TillService
{
    #region Daily Close Operations

    /// <summary>
    /// Gets or creates a DailyClose record for a specific shop and date.
    /// Returns existing record if found, otherwise creates new Open record.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="date">Date (time component ignored)</param>
    /// <returns>DailyClose entity</returns>
    public async Task<DailyClose> GetOrCreateDailyCloseAsync(int shopId, DateTime date)
    {
        var dateOnly = date.Date;
        var existing = await GetDailyCloseAsync(shopId, dateOnly);
        if (existing is not null)
            return existing;

        var dailyClose = new DailyClose
        {
            ShopId = shopId,
            Date = dateOnly,
            Status = DailyCloseStatus.Open
        };

        using var session = this.Context.OpenSession("system");
        session.Attach(dailyClose);
        await session.SubmitChanges("CreateDailyClose");

        return dailyClose;
    }

    /// <summary>
    /// Gets a DailyClose record for a specific shop and date.
    /// Returns null if not found.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="date">Date (time component ignored)</param>
    /// <returns>DailyClose entity or null</returns>
    public async Task<DailyClose?> GetDailyCloseAsync(int shopId, DateTime date)
    {
        var dateOnly = date.Date;
        var query = this.Context.CreateQuery<DailyClose>()
            .Where(dc => dc.ShopId == shopId)
            .Where(dc => dc.Date == dateOnly);
        return await this.Context.LoadOneAsync(query);
    }

    /// <summary>
    /// Performs daily close for a shop, capturing summary totals.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="date">Date to close</param>
    /// <param name="managerUserName">Manager performing the close</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> PerformDailyCloseAsync(int shopId, DateTime date, string managerUserName)
    {
        var dailyClose = await GetOrCreateDailyCloseAsync(shopId, date);

        if (dailyClose.Status == DailyCloseStatus.Closed || dailyClose.Status == DailyCloseStatus.Reconciled)
            return SubmitOperation.CreateFailure("This day is already closed");

        // Get daily summary for totals
        var summary = await GetDailySummaryAsync(shopId, date.Date);

        // Populate denormalized fields
        dailyClose.TotalCashIn = summary.TotalCashIn;
        dailyClose.TotalCashOut = summary.TotalCashOut;
        dailyClose.TotalDropped = summary.TotalDropped;
        dailyClose.TotalVariance = summary.TotalVariance;
        dailyClose.TotalElectronicPayments = summary.TotalElectronicPayments;
        dailyClose.SessionCount = summary.TotalSessions;
        dailyClose.SessionsWithVariance = summary.SessionsWithVariance;

        // Mark as closed
        dailyClose.Status = DailyCloseStatus.Closed;
        dailyClose.ClosedAt = DateTimeOffset.Now;
        dailyClose.ClosedByUserName = managerUserName;

        using var session = this.Context.OpenSession(managerUserName);
        session.Attach(dailyClose);
        return await session.SubmitChanges("PerformDailyClose");
    }

    /// <summary>
    /// Checks if a day is closed (Closed or Reconciled status).
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="date">Date to check</param>
    /// <returns>True if day is closed</returns>
    public async Task<bool> IsDayClosedAsync(int shopId, DateTime date)
    {
        var dailyClose = await GetDailyCloseAsync(shopId, date.Date);
        if (dailyClose is null)
            return false;

        return dailyClose.Status is DailyCloseStatus.Closed or DailyCloseStatus.Reconciled;
    }

    /// <summary>
    /// Reopens a closed day with reason tracking.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="date">Date to reopen</param>
    /// <param name="reason">Reason for reopening</param>
    /// <param name="managerUserName">Manager performing the reopen</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> ReopenDayAsync(int shopId, DateTime date, string reason, string managerUserName)
    {
        var dailyClose = await GetDailyCloseAsync(shopId, date.Date);
        if (dailyClose is null)
            return SubmitOperation.CreateFailure("No daily close record found for this date");

        if (dailyClose.Status == DailyCloseStatus.Open)
            return SubmitOperation.CreateFailure("This day is already open");

        // Reopen the day
        dailyClose.Status = DailyCloseStatus.Open;
        dailyClose.WasReopened = true;
        dailyClose.ReopenReason = reason;
        dailyClose.ReopenedAt = DateTimeOffset.Now;
        dailyClose.ReopenedByUserName = managerUserName;

        using var session = this.Context.OpenSession(managerUserName);
        session.Attach(dailyClose);
        return await session.SubmitChanges("ReopenDay");
    }

    #endregion

    #region Shortage Logging

    /// <summary>
    /// Logs a shortage entry for accountability tracking.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="tillSessionId">Till session where shortage occurred</param>
    /// <param name="dailyCloseId">Daily close ID (null if logged during session close)</param>
    /// <param name="staffUserName">Staff member username</param>
    /// <param name="staffDisplayName">Staff member display name</param>
    /// <param name="currency">Currency code</param>
    /// <param name="amount">Shortage amount (will be stored as positive)</param>
    /// <param name="reason">Manager-provided explanation</param>
    /// <param name="managerUserName">Manager logging the shortage</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> LogShortageAsync(
        int shopId,
        int tillSessionId,
        int? dailyCloseId,
        string staffUserName,
        string staffDisplayName,
        string currency,
        decimal amount,
        string reason,
        string managerUserName)
    {
        // Convert to THB if foreign currency
        decimal amountInThb;
        if (currency == SupportedCurrencies.THB)
        {
            amountInThb = Math.Abs(amount);
        }
        else
        {
            var rate = await this.ExchangeRateService.GetCurrentRateAsync(currency);
            var buyRate = rate?.BuyRate ?? 1m;
            amountInThb = Math.Abs(amount) * buyRate;
        }

        var shortageLog = new ShortageLog
        {
            ShopId = shopId,
            TillSessionId = tillSessionId,
            DailyCloseId = dailyCloseId,
            StaffUserName = staffUserName,
            StaffDisplayName = staffDisplayName,
            Currency = currency,
            Amount = Math.Abs(amount), // Always positive
            AmountInThb = amountInThb,
            Reason = reason,
            LoggedByUserName = managerUserName,
            LoggedAt = DateTimeOffset.Now
        };

        using var session = this.Context.OpenSession(managerUserName);
        session.Attach(shortageLog);
        return await session.SubmitChanges("LogShortage");
    }

    /// <summary>
    /// Gets shortage logs for a shop with optional date filtering.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="fromDate">Optional start date</param>
    /// <param name="toDate">Optional end date</param>
    /// <returns>List of shortage logs ordered by LoggedAt descending</returns>
    public async Task<List<ShortageLog>> GetShortageLogsAsync(int shopId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = this.Context.CreateQuery<ShortageLog>()
            .Where(sl => sl.ShopId == shopId);

        if (fromDate.HasValue)
        {
            var startDate = new DateTimeOffset(fromDate.Value.Date);
            query = query.Where(sl => sl.LoggedAt >= startDate);
        }

        if (toDate.HasValue)
        {
            var endDate = new DateTimeOffset(toDate.Value.Date.AddDays(1));
            query = query.Where(sl => sl.LoggedAt < endDate);
        }

        query = query.OrderByDescending(sl => sl.LoggedAt);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets shortage logs for a specific staff member.
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="staffUserName">Staff username</param>
    /// <returns>List of shortage logs ordered by LoggedAt descending</returns>
    public async Task<List<ShortageLog>> GetShortageLogsByStaffAsync(int shopId, string staffUserName)
    {
        var query = this.Context.CreateQuery<ShortageLog>()
            .Where(sl => sl.ShopId == shopId)
            .Where(sl => sl.StaffUserName == staffUserName)
            .OrderByDescending(sl => sl.LoggedAt);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    #endregion

    #region Cash Drop Verification

    /// <summary>
    /// Gets total cash drops by currency for a session.
    /// </summary>
    /// <param name="sessionId">Till session ID</param>
    /// <returns>Dictionary of currency code to total dropped amount</returns>
    public async Task<Dictionary<string, decimal>> GetDropTotalsByCurrencyAsync(int sessionId)
    {
        var query = this.Context.CreateQuery<TillTransaction>()
            .Where(t => t.TillSessionId == sessionId)
            .Where(t => t.TransactionType == TillTransactionType.Drop)
            .Where(t => !t.IsVoided);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        return result.ItemCollection
            .GroupBy(t => t.Currency)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Amount)
            );
    }

    /// <summary>
    /// Gets all drop transactions for a session.
    /// </summary>
    /// <param name="sessionId">Till session ID</param>
    /// <returns>List of drop transactions ordered by time</returns>
    public async Task<List<TillTransaction>> GetDropTransactionsAsync(int sessionId)
    {
        var query = this.Context.CreateQuery<TillTransaction>()
            .Where(t => t.TillSessionId == sessionId)
            .Where(t => t.TransactionType == TillTransactionType.Drop)
            .Where(t => !t.IsVoided)
            .OrderBy(t => t.TransactionTime);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    #endregion
}
