using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Session management operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Opens a new till session for a staff member.
    /// </summary>
    public async Task<SubmitOperation> OpenSessionAsync(
        int shopId,
        string staffUserName,
        string staffDisplayName,
        decimal openingFloat,
        string? notes = null)
    {
        // Check if day is closed (EOD-04: closed days cannot have new transactions)
        if (await this.IsDayClosedAsync(shopId, DateTime.Today))
            return SubmitOperation.CreateFailure("Cannot open session - this day has been closed");

        // Check if staff already has an open session at this shop
        var existingSession = await this.GetActiveSessionAsync(shopId, staffUserName);
        if (existingSession is not null)
            return SubmitOperation.CreateFailure("You already have an open till session");

        var session = new TillSession
        {
            ShopId = shopId,
            StaffUserName = staffUserName,
            StaffDisplayName = staffDisplayName,
            Status = TillSessionStatus.Open,
            OpeningFloat = openingFloat,
            OpenedAt = DateTimeOffset.Now,
            OpeningNotes = notes,
            // Initialize currency balances - THB starts with opening float, foreign currencies start at 0
            CurrencyBalances = new Dictionary<string, decimal>
            {
                [SupportedCurrencies.THB] = openingFloat,
                [SupportedCurrencies.USD] = 0,
                [SupportedCurrencies.EUR] = 0,
                [SupportedCurrencies.CNY] = 0
            }
        };

        using var persistenceSession = this.Context.OpenSession(staffUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("OpenTillSession");
    }

    /// <summary>
    /// Gets the active (open) session for a staff member at a shop.
    /// </summary>
    public async Task<TillSession?> GetActiveSessionAsync(int shopId, string staffUserName)
    {
        var query = this.Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId && s.StaffUserName == staffUserName && s.Status == TillSessionStatus.Open);
        return await this.Context.LoadOneAsync(query);
    }

    /// <summary>
    /// Gets any active (open) session for a staff member across all shops.
    /// Used by header button to display till status.
    /// </summary>
    public async Task<TillSession?> GetActiveSessionForUserAsync(string staffUserName)
    {
        var query = this.Context.CreateQuery<TillSession>()
            .Where(s => s.StaffUserName == staffUserName && s.Status == TillSessionStatus.Open);
        return await this.Context.LoadOneAsync(query);
    }

    /// <summary>
    /// Gets all active (open) sessions for a staff member across all shops.
    /// Used by Till page to show existing open sessions.
    /// </summary>
    public async Task<List<TillSession>> GetAllActiveSessionsForUserAsync(string staffUserName)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.StaffUserName == staffUserName && s.Status == TillSessionStatus.Open)
                .OrderByDescending(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Checks if a staff member can open a new session at a shop.
    /// Enforces one-till-per-staff-per-shop-per-day constraint.
    /// </summary>
    public async Task<(bool CanOpen, string? Reason)> CanOpenSessionAsync(int shopId, string staffUserName)
    {
        // Check if staff already has an open session anywhere
        var existingOpen = await this.GetActiveSessionForUserAsync(staffUserName);
        if (existingOpen is not null)
        {
            return (false, $"You already have an open till session at shop {existingOpen.ShopId}. Close it first.");
        }

        // Check for same-day session at this shop (one till per staff per shop per day)
        var today = DateTimeOffset.Now.Date;
        var todayStart = new DateTimeOffset(today, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var sameDayQuery = this.Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId && s.StaffUserName == staffUserName && s.OpenedAt >= todayStart && s.OpenedAt < todayEnd);
        var existingToday = await this.Context.LoadOneAsync(sameDayQuery);

        if (existingToday is not null)
        {
            return (false, "You already had a till session at this shop today.");
        }

        return (true, null);
    }

    /// <summary>
    /// Gets all active sessions at a shop.
    /// </summary>
    public async Task<List<TillSession>> GetActiveSessionsAsync(int shopId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId && s.Status == TillSessionStatus.Open)
                .OrderByDescending(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public Task<TillSession?> GetSessionByIdAsync(int sessionId) =>
        this.Context.LoadOneAsync<TillSession>(s => s.TillSessionId == sessionId);

    /// <summary>
    /// Checks if a session is stale (opened before today and still open).
    /// Stale sessions must be closed before staff can open a new session.
    /// </summary>
    /// <param name="session">The session to check</param>
    /// <returns>True if the session is stale and needs to be closed</returns>
    public static bool IsSessionStale(TillSession? session)
    {
        if (session is null) return false;
        if (session.Status != TillSessionStatus.Open) return false;
        return session.OpenedAt.Date < DateTime.Today;
    }

    /// <summary>
    /// Closes a till session with reconciliation (single-currency, backward compatible).
    /// </summary>
    public async Task<SubmitOperation> CloseSessionAsync(
        int sessionId,
        decimal actualCash,
        string? notes,
        string username)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        if (session.StaffUserName != username)
            return SubmitOperation.CreateFailure("You can only close your own session");

        session.ActualCash = actualCash;
        session.Variance = actualCash - session.ExpectedCash;
        session.ClosedAt = DateTimeOffset.Now;
        session.ClosingNotes = notes;
        session.ClosedByUserName = username;
        session.IsForceClose = false;

        // Detect late closure (session opened before today)
        if (session.OpenedAt.Date < DateTimeOffset.Now.Date)
        {
            session.IsLateClose = true;
            session.ExpectedCloseDate = session.OpenedAt.Date;
        }

        // Determine status based on variance
        session.Status = session.Variance == 0
            ? TillSessionStatus.Closed
            : TillSessionStatus.ClosedWithVariance;

        using var persistenceSession = this.Context.OpenSession(username);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("CloseTillSession");
    }

    /// <summary>
    /// Closes a till session with per-currency reconciliation.
    /// </summary>
    /// <param name="sessionId">Session to close</param>
    /// <param name="breakdowns">Currency denomination breakdowns with actuals</param>
    /// <param name="notes">Optional closing notes</param>
    /// <param name="closedByUserName">Staff closing the session</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> CloseSessionAsync(
        int sessionId,
        List<CurrencyDenominationBreakdown> breakdowns,
        string? notes,
        string closedByUserName)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        if (session.StaffUserName != closedByUserName)
            return SubmitOperation.CreateFailure("You can only close your own session");

        // Calculate per-currency actuals and variances
        var actualBalances = new Dictionary<string, decimal>();
        var closingVariances = new Dictionary<string, decimal>();

        foreach (var breakdown in breakdowns)
        {
            actualBalances[breakdown.Currency] = breakdown.Total;
            closingVariances[breakdown.Currency] = breakdown.Variance ?? 0;
        }

        // Get THB values for backward compatibility with existing fields
        var thbActual = actualBalances.GetValueOrDefault(SupportedCurrencies.THB, 0);
        var thbExpected = session.GetCurrencyBalance(SupportedCurrencies.THB);

        session.ActualCash = thbActual;
        session.Variance = thbActual - thbExpected;
        session.ActualBalances = actualBalances;
        session.ClosingVariances = closingVariances;
        session.ClosedAt = DateTimeOffset.Now;
        session.ClosedByUserName = closedByUserName;
        session.ClosingNotes = notes;
        session.IsForceClose = false;

        // Detect late closure (session opened before today)
        if (session.OpenedAt.Date < DateTimeOffset.Now.Date)
        {
            session.IsLateClose = true;
            session.ExpectedCloseDate = session.OpenedAt.Date;
        }

        // Determine status based on ANY variance
        var hasAnyVariance = closingVariances.Values.Any(v => v != 0);
        session.Status = hasAnyVariance
            ? TillSessionStatus.ClosedWithVariance
            : TillSessionStatus.Closed;

        using var persistenceSession = this.Context.OpenSession(closedByUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("CloseTillSession");
    }

    /// <summary>
    /// Gets session history for a shop with optional filters.
    /// </summary>
    public async Task<LoadOperation<TillSession>> GetSessionHistoryAsync(
        int shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? staffUserName = null,
        TillSessionStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId);

        if (fromDate.HasValue)
            query = query.Where(s => s.OpenedAt >= new DateTimeOffset(fromDate.Value));

        if (toDate.HasValue)
            query = query.Where(s => s.OpenedAt <= new DateTimeOffset(toDate.Value.AddDays(1)));

        if (!string.IsNullOrWhiteSpace(staffUserName))
            query = query.Where(s => s.StaffUserName == staffUserName);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        query = query.OrderByDescending(s => s.OpenedAt);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Force closes a till session with manager approval.
    /// Sets actual = expected (zero variance) and closes immediately.
    /// </summary>
    /// <param name="sessionId">Session to close</param>
    /// <param name="approvedByUserName">Manager who approved the force close</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="closedByUserName">Staff requesting the close</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> ForceCloseSessionAsync(
        int sessionId,
        string approvedByUserName,
        string? notes,
        string closedByUserName)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        // Force close sets actual = expected (zero variance)
        session.ActualCash = session.ExpectedCash;
        session.Variance = 0;
        session.ClosedAt = DateTimeOffset.Now;
        session.ClosedByUserName = closedByUserName;
        session.ClosingNotes = notes;
        session.IsForceClose = true;
        session.ForceCloseApprovedBy = approvedByUserName;
        session.Status = TillSessionStatus.Closed;

        // Detect late closure (session opened before today)
        if (session.OpenedAt.Date < DateTimeOffset.Now.Date)
        {
            session.IsLateClose = true;
            session.ExpectedCloseDate = session.OpenedAt.Date;
        }

        // Set per-currency actuals = expected (no variance)
        session.ActualBalances = new Dictionary<string, decimal>(session.CurrencyBalances);
        session.ClosingVariances = session.CurrencyBalances.ToDictionary(
            kvp => kvp.Key,
            kvp => 0m
        );

        using var persistenceSession = this.Context.OpenSession(closedByUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("ForceCloseTillSession");
    }
}
