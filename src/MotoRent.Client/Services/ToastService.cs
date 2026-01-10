namespace MotoRent.Client.Services;

/// <summary>
/// Service for displaying toast notifications using Tabler CSS alerts.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> m_toasts = [];
    public event Action? OnChange;

    public IReadOnlyList<ToastMessage> Toasts => this.m_toasts.AsReadOnly();

    /// <summary>
    /// Shows a success toast notification.
    /// </summary>
    public void ShowSuccess(string message, string? title = null, int durationMs = 5000)
    {
        this.AddToast(message, ToastType.Success, title, durationMs);
    }

    /// <summary>
    /// Shows an error toast notification.
    /// </summary>
    public void ShowError(string message, string? title = null, int durationMs = 8000)
    {
        this.AddToast(message, ToastType.Error, title, durationMs);
    }

    /// <summary>
    /// Shows a warning toast notification.
    /// </summary>
    public void ShowWarning(string message, string? title = null, int durationMs = 6000)
    {
        this.AddToast(message, ToastType.Warning, title, durationMs);
    }

    /// <summary>
    /// Shows an info toast notification.
    /// </summary>
    public void ShowInfo(string message, string? title = null, int durationMs = 5000)
    {
        this.AddToast(message, ToastType.Info, title, durationMs);
    }

    /// <summary>
    /// Adds a toast notification.
    /// </summary>
    public void AddToast(string message, ToastType type, string? title = null, int durationMs = 5000)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid().ToString(),
            Message = message,
            Type = type,
            Title = title,
            DurationMs = durationMs
        };

        this.m_toasts.Add(toast);
        this.OnChange?.Invoke();

        // Auto-remove after duration
        _ = this.RemoveAfterDelayAsync(toast.Id, durationMs);
    }

    /// <summary>
    /// Removes a toast notification by ID.
    /// </summary>
    public void RemoveToast(string id)
    {
        var toast = this.m_toasts.Find(t => t.Id == id);
        if (toast is not null)
        {
            this.m_toasts.Remove(toast);
            this.OnChange?.Invoke();
        }
    }

    /// <summary>
    /// Clears all toast notifications.
    /// </summary>
    public void Clear()
    {
        this.m_toasts.Clear();
        this.OnChange?.Invoke();
    }

    private async Task RemoveAfterDelayAsync(string id, int delayMs)
    {
        await Task.Delay(delayMs);
        this.RemoveToast(id);
    }
}
