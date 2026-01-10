namespace MotoRent.Client.Services;

/// <summary>
/// Represents a toast notification message.
/// </summary>
public class ToastMessage
{
    public required string Id { get; init; }
    public required string Message { get; init; }
    public required ToastType Type { get; init; }
    public string? Title { get; init; }
    public int DurationMs { get; init; } = 5000;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Type of toast notification.
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
