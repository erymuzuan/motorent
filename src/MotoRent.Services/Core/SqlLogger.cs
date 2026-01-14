using System.Runtime.CompilerServices;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// Logger implementation that persists log entries to SQL Server database.
/// </summary>
public class SqlLogger : ILogger
{
    private readonly CoreDataContext m_context;
    private readonly IRequestContext m_requestContext;

    public SqlLogger(CoreDataContext context, IRequestContext requestContext)
    {
        m_context = context;
        m_requestContext = requestContext;
    }

    /// <summary>
    /// Logs a LogEntry to the database.
    /// </summary>
    public async Task LogAsync(LogEntry log)
    {
        try
        {
            using var session = m_context.OpenSession(log.UserName ?? "system");
            session.Attach(log);
            await session.SubmitChanges("LogEntry");
        }
        catch (Exception ex)
        {
            // Fallback to console if database logging fails
            Console.Error.WriteLine($"[SqlLogger] Failed to log to database: {ex.Message}");
            Console.Error.WriteLine(log.ToString());
        }
    }

    /// <summary>
    /// Writes an error with exception details asynchronously.
    /// </summary>
    public async Task WriteErrorAsync(Exception exception, string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var accountNo = m_requestContext.GetAccountNo();
        var userName = m_requestContext.GetUserName();

        var log = LogEntry.FromException(exception, accountNo, userName);
        log.Message = message;
        log.CallerFilePath = filePath;
        log.CallerMemberName = memberName;
        log.CallerLineNumber = lineNumber;

        await LogAsync(log);
    }

    /// <summary>
    /// Writes a warning message asynchronously.
    /// </summary>
    public async Task WriteWarningAsync(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var log = await CreateLogEntryAsync(LogSeverity.Warning, message, filePath, memberName, lineNumber);
        await LogAsync(log);
    }

    /// <summary>
    /// Writes an info message asynchronously.
    /// </summary>
    public async Task WriteInfoAsync(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var log = await CreateLogEntryAsync(LogSeverity.Info, message, filePath, memberName, lineNumber);
        await LogAsync(log);
    }

    /// <summary>
    /// Writes a debug message asynchronously.
    /// </summary>
    public async Task WriteDebugAsync(string message)
    {
        var log = await CreateLogEntryAsync(LogSeverity.Debug, message, null, null, 0);
        await LogAsync(log);
    }

    /// <summary>
    /// Writes a debug message with file content asynchronously.
    /// </summary>
    public async Task WriteDebugAsync(string message, string fileName, string fileContent)
    {
        var log = await CreateLogEntryAsync(LogSeverity.Debug, $"{message} [{fileName}]", null, null, 0);
        log.Error = fileContent;
        await LogAsync(log);
    }

    /// <summary>
    /// Writes a warning message with file content asynchronously.
    /// </summary>
    public async Task WriteWarningAsync(string message, string fileName, string fileContent)
    {
        var log = await CreateLogEntryAsync(LogSeverity.Warning, $"{message} [{fileName}]", null, null, 0);
        log.Error = fileContent;
        await LogAsync(log);
    }

    private Task<LogEntry> CreateLogEntryAsync(LogSeverity severity, string message,
        string? filePath, string? memberName, int lineNumber)
    {
        var accountNo = m_requestContext.GetAccountNo();
        var userName = m_requestContext.GetUserName();

        var log = new LogEntry
        {
            AccountNo = accountNo,
            UserName = userName,
            LogSeverity = severity,
            Message = message,
            DateTime = DateTimeOffset.Now,
            Computer = Environment.MachineName,
            CallerFilePath = filePath,
            CallerMemberName = memberName,
            CallerLineNumber = lineNumber,
            IncidentHash = message.GetHashCode().ToString("X")
        };

        return Task.FromResult(log);
    }
}
