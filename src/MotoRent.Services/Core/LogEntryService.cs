using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// Service for managing log entries - querying, filtering, resolving, and deleting.
/// </summary>
public class LogEntryService
{
    private readonly CoreDataContext m_context;

    public LogEntryService(CoreDataContext context)
    {
        m_context = context;
    }

    #region Logging

    /// <summary>
    /// Logs an exception to the database.
    /// </summary>
    public async Task LogExceptionAsync(Exception exception, string? accountNo = null,
        string? userName = null, string? url = null, string? ipAddress = null,
        EventLog log = EventLog.Web)
    {
        var entry = LogEntry.FromException(exception, accountNo, userName);
        entry.Url = url;
        entry.IpAddress = ipAddress;
        entry.Log = log;

        using var session = m_context.OpenSession(userName ?? "system");
        session.Attach(entry);
        await session.SubmitChanges("LogException");
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets log entries with filtering and pagination.
    /// </summary>
    public async Task<LoadOperation<LogEntry>> GetLogsAsync(LogEntryFilter filter, int page = 1, int size = 20)
    {
        var query = BuildQuery(filter);
        query = query.OrderByDescending(x => x.LogEntryId);

        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Gets a single log entry by ID.
    /// </summary>
    public async Task<LogEntry?> GetByIdAsync(int id)
    {
        return await m_context.LoadOneAsync<LogEntry>(x => x.LogEntryId == id);
    }

    /// <summary>
    /// Gets log entries grouped by IncidentHash.
    /// </summary>
    public async Task<List<LogEntrySummary>> GetGroupedLogsAsync(LogEntryFilter filter, int page = 1, int size = 20)
    {
        var query = BuildQuery(filter);

        // Load all matching entries (limited to prevent memory issues)
        var lo = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        var entries = lo.ItemCollection.ToList();

        // Group by IncidentHash
        var grouped = entries
            .Where(x => !string.IsNullOrEmpty(x.IncidentHash))
            .GroupBy(x => x.IncidentHash)
            .Select(g => new LogEntrySummary(
                g.Key!,
                g.First().Message,
                g.First().Type,
                g.First().LogSeverity,
                g.First().Status,
                g.Count(),
                g.Min(x => x.DateTime),
                g.Max(x => x.DateTime),
                g.First().LogEntryId
            ))
            .OrderByDescending(x => x.LastOccurrence)
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Gets the number of occurrences for a given incident hash.
    /// </summary>
    public async Task<int> GetOccurrenceCountAsync(string incidentHash)
    {
        var query = m_context.LogEntries.Where(x => x.IncidentHash == incidentHash);
        var lo = await m_context.LoadAsync(query, 1, 1, includeTotalRows: true);
        return lo.TotalRows;
    }

    /// <summary>
    /// Gets all log entries with the same incident hash.
    /// </summary>
    public async Task<List<LogEntry>> GetByIncidentHashAsync(string incidentHash)
    {
        var query = m_context.LogEntries
            .Where(x => x.IncidentHash == incidentHash)
            .OrderByDescending(x => x.DateTime);

        var lo = await m_context.LoadAsync(query, 1, 100, includeTotalRows: false);
        return lo.ItemCollection.ToList();
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Marks a log entry as resolved.
    /// </summary>
    public async Task ResolveAsync(int id, string userName)
    {
        var entry = await GetByIdAsync(id);
        if (entry is null) return;

        entry.Status = LogStatus.Resolved;

        using var session = m_context.OpenSession(userName);
        session.Attach(entry);
        await session.SubmitChanges("ResolveLog");
    }

    /// <summary>
    /// Marks all log entries with the same incident hash as resolved.
    /// </summary>
    public async Task ResolveAllSimilarAsync(string incidentHash, string userName)
    {
        var entries = await GetByIncidentHashAsync(incidentHash);

        using var session = m_context.OpenSession(userName);
        foreach (var entry in entries)
        {
            entry.Status = LogStatus.Resolved;
            session.Attach(entry);
        }
        await session.SubmitChanges("ResolveAllSimilar");
    }

    /// <summary>
    /// Deletes a log entry.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var entry = await GetByIdAsync(id);
        if (entry is null) return;

        using var session = m_context.OpenSession("system");
        session.Delete(entry);
        await session.SubmitChanges("DeleteLog");
    }

    /// <summary>
    /// Deletes all log entries with the same incident hash.
    /// </summary>
    public async Task DeleteAllSimilarAsync(string incidentHash)
    {
        var entries = await GetByIncidentHashAsync(incidentHash);

        using var session = m_context.OpenSession("system");
        foreach (var entry in entries)
        {
            session.Delete(entry);
        }
        await session.SubmitChanges("DeleteAllSimilar");
    }

    /// <summary>
    /// Deletes all resolved log entries older than the specified number of days.
    /// </summary>
    public async Task DeleteAllResolvedAsync(int olderThanDays = 90)
    {
        var cutoffDate = DateTimeOffset.Now.AddDays(-olderThanDays);

        var query = m_context.LogEntries
            .Where(x => x.Status == LogStatus.Resolved && x.DateTime < cutoffDate);

        var lo = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        var entries = lo.ItemCollection.ToList();

        if (entries.Count == 0) return;

        using var session = m_context.OpenSession("system");
        foreach (var entry in entries)
        {
            session.Delete(entry);
        }
        await session.SubmitChanges("CleanupResolvedLogs");
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets log statistics for dashboard display.
    /// </summary>
    public async Task<LogStats> GetStatsAsync()
    {
        var allQuery = m_context.LogEntries.AsQueryable();
        var allLo = await m_context.LoadAsync(allQuery, 1, 1, includeTotalRows: true);

        var newQuery = m_context.LogEntries.Where(x => x.Status == LogStatus.New);
        var newLo = await m_context.LoadAsync(newQuery, 1, 1, includeTotalRows: true);

        var errorQuery = m_context.LogEntries.Where(x => x.LogSeverity == LogSeverity.Error);
        var errorLo = await m_context.LoadAsync(errorQuery, 1, 1, includeTotalRows: true);

        var criticalQuery = m_context.LogEntries.Where(x => x.LogSeverity == LogSeverity.Critical);
        var criticalLo = await m_context.LoadAsync(criticalQuery, 1, 1, includeTotalRows: true);

        return new LogStats(
            allLo.TotalRows,
            newLo.TotalRows,
            errorLo.TotalRows,
            criticalLo.TotalRows
        );
    }

    #endregion

    #region Private Helpers

    private IQueryable<LogEntry> BuildQuery(LogEntryFilter filter)
    {
        var query = m_context.LogEntries.AsQueryable();

        if (filter.Severity.HasValue)
            query = query.Where(x => x.LogSeverity == filter.Severity.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Log.HasValue)
            query = query.Where(x => x.Log == filter.Log.Value);

        if (!string.IsNullOrEmpty(filter.AccountNo))
            query = query.Where(x => x.AccountNo != null && x.AccountNo.Contains(filter.AccountNo));

        if (!string.IsNullOrEmpty(filter.UserName))
            query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(x =>
                (x.Message != null && x.Message.Contains(filter.Search)) ||
                (x.Type != null && x.Type.Contains(filter.Search)));

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DateTime >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DateTime <= filter.ToDate.Value);

        return query;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Filter criteria for log queries.
/// </summary>
public record LogEntryFilter(
    LogSeverity? Severity = null,
    LogStatus? Status = null,
    EventLog? Log = null,
    string? AccountNo = null,
    string? UserName = null,
    string? Search = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null
);

/// <summary>
/// Summary of grouped log entries by IncidentHash.
/// </summary>
public record LogEntrySummary(
    string IncidentHash,
    string Message,
    string? Type,
    LogSeverity Severity,
    LogStatus Status,
    int OccurrenceCount,
    DateTimeOffset FirstOccurrence,
    DateTimeOffset LastOccurrence,
    int LatestLogEntryId
);

/// <summary>
/// Statistics for log dashboard.
/// </summary>
public record LogStats(
    int TotalCount,
    int NewCount,
    int ErrorCount,
    int CriticalCount
);

#endregion
