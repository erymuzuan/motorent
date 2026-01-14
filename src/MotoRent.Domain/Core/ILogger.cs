using System.Runtime.CompilerServices;

namespace MotoRent.Domain.Core;

/// <summary>
/// Interface for logging errors and events to persistent storage.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Writes a verbose/debug message.
    /// </summary>
    void WriteVerbose(string message) => Console.WriteLine(message);

    /// <summary>
    /// Writes a verbose/debug message asynchronously.
    /// </summary>
    Task WriteVerboseAsync(string message) => Task.CompletedTask;

    /// <summary>
    /// Logs a LogEntry to persistent storage.
    /// </summary>
    void Log(LogEntry log) => Console.WriteLine(log);

    /// <summary>
    /// Logs a LogEntry to persistent storage asynchronously.
    /// </summary>
    Task LogAsync(LogEntry log) => Task.CompletedTask;

    /// <summary>
    /// Writes an error message.
    /// </summary>
    void WriteError(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Console.Error.WriteLine(message);

    /// <summary>
    /// Writes an error with exception details.
    /// </summary>
    void WriteError(Exception exception, string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Console.Error.WriteLine($"{message}: {exception.Message}");

    /// <summary>
    /// Writes an error with exception details asynchronously.
    /// </summary>
    Task WriteErrorAsync(Exception exception, string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Task.CompletedTask;

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    void WriteWarning(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Console.WriteLine($"[WARNING] {message}");

    /// <summary>
    /// Writes a warning message asynchronously.
    /// </summary>
    Task WriteWarningAsync(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Task.CompletedTask;

    /// <summary>
    /// Writes an info message.
    /// </summary>
    void WriteInfo(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Console.WriteLine($"[INFO] {message}");

    /// <summary>
    /// Writes an info message asynchronously.
    /// </summary>
    Task WriteInfoAsync(string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0) => Task.CompletedTask;

    /// <summary>
    /// Writes a debug message.
    /// </summary>
    void WriteDebug(string message) => Console.WriteLine($"[DEBUG] {message}");

    /// <summary>
    /// Writes a debug message asynchronously.
    /// </summary>
    Task WriteDebugAsync(string message) => Task.CompletedTask;

    /// <summary>
    /// Writes a debug message with file content asynchronously.
    /// </summary>
    Task WriteDebugAsync(string message, string fileName, string fileContent) => Task.CompletedTask;

    /// <summary>
    /// Writes a warning message with file content asynchronously.
    /// </summary>
    Task WriteWarningAsync(string message, string fileName, string fileContent) => Task.CompletedTask;
}
