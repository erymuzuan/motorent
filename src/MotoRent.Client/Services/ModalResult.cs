namespace MotoRent.Client.Services;

/// <summary>
/// Result of a modal dialog operation.
/// </summary>
public class ModalResult
{
    public bool Cancelled { get; private set; }
    public object? Data { get; private set; }

    private ModalResult() { }

    public static ModalResult Ok() => new() { Cancelled = false };
    public static ModalResult Ok(object? data) => new() { Cancelled = false, Data = data };
    public static ModalResult Cancel() => new() { Cancelled = true };

    /// <summary>
    /// Gets the data as the specified type.
    /// </summary>
    public T? GetData<T>() => this.Data is T data ? data : default;
}

/// <summary>
/// Standard dialog result values.
/// </summary>
public enum DialogResult
{
    Ok,
    Cancel,
    Yes,
    No
}
