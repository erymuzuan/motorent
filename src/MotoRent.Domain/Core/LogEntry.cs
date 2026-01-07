using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Log severity levels.
/// </summary>
public enum LogSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Log entry status for tracking resolution.
/// </summary>
public enum LogStatus
{
    New,
    Active,
    Resolved,
    Cancelled
}

/// <summary>
/// Event log category.
/// </summary>
public enum EventLog
{
    Web,
    Api,
    Background,
    Security
}

/// <summary>
/// Audit log entry for tracking system events and errors.
/// </summary>
public class LogEntry : Entity
{
    public int LogEntryId { get; set; }

    /// <summary>
    /// Organization AccountNo associated with this log entry.
    /// </summary>
    public string? AccountNo { get; set; }

    /// <summary>
    /// Username associated with this log entry.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Application name that generated this log.
    /// </summary>
    public string Application { get; set; } = "MotoRent";

    /// <summary>
    /// Log severity level.
    /// </summary>
    public LogSeverity LogSeverity { get; set; }

    /// <summary>
    /// Log status for tracking.
    /// </summary>
    public LogStatus Status { get; set; } = LogStatus.New;

    /// <summary>
    /// Event log category.
    /// </summary>
    public EventLog Log { get; set; } = EventLog.Web;

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Error details if applicable.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Exception type name.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Request URL if applicable.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Computer/server name.
    /// </summary>
    public string? Computer { get; set; }

    /// <summary>
    /// Timestamp of the log entry.
    /// </summary>
    public DateTimeOffset DateTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Hash of the incident for grouping similar errors.
    /// </summary>
    public string? IncidentHash { get; set; }

    /// <summary>
    /// Source file path.
    /// </summary>
    public string? CallerFilePath { get; set; }

    /// <summary>
    /// Source member name.
    /// </summary>
    public string? CallerMemberName { get; set; }

    /// <summary>
    /// Source line number.
    /// </summary>
    public int? CallerLineNumber { get; set; }

    public override int GetId() => LogEntryId;
    public override void SetId(int value) => LogEntryId = value;

    public override string ToString() => $"[{LogSeverity}] {Message}";

    /// <summary>
    /// Creates a log entry from an exception.
    /// </summary>
    public static LogEntry FromException(Exception exception, string? accountNo = null, string? userName = null)
    {
        return new LogEntry
        {
            AccountNo = accountNo,
            UserName = userName,
            LogSeverity = LogSeverity.Error,
            Message = exception.Message,
            Error = exception.ToString(),
            Type = exception.GetType().Name,
            DateTime = DateTimeOffset.Now,
            Computer = Environment.MachineName,
            IncidentHash = exception.Message.GetHashCode().ToString("X")
        };
    }
}
