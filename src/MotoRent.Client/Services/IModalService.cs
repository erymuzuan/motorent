using Microsoft.AspNetCore.Components;

namespace MotoRent.Client.Services;

/// <summary>
/// Interface for the modal service that manages modal dialogs.
/// </summary>
public interface IModalService
{
    /// <summary>
    /// Event raised when a modal is shown or closed.
    /// </summary>
    event Action? OnChange;

    /// <summary>
    /// Gets the current modal state.
    /// </summary>
    ModalState? CurrentModal { get; }

    /// <summary>
    /// Shows a modal dialog with the specified component.
    /// </summary>
    Task<ModalResult> ShowAsync<TComponent>(string title, ModalOptions? options = null)
        where TComponent : IComponent;

    /// <summary>
    /// Shows a modal dialog with the specified component and parameters.
    /// </summary>
    Task<ModalResult> ShowAsync<TComponent>(string title, Dictionary<string, object?> parameters, ModalOptions? options = null)
        where TComponent : IComponent;

    /// <summary>
    /// Shows a modal dialog with a render fragment.
    /// </summary>
    Task<ModalResult> ShowAsync(string title, RenderFragment content, ModalOptions? options = null);

    /// <summary>
    /// Closes the current modal with the specified result.
    /// </summary>
    void Close(ModalResult result);
}

/// <summary>
/// Represents the state of an active modal.
/// </summary>
public class ModalState
{
    public required string Title { get; init; }
    public required Type ComponentType { get; init; }
    public Dictionary<string, object?>? Parameters { get; init; }
    public RenderFragment? ContentFragment { get; init; }
    public required ModalOptions Options { get; init; }
    public required TaskCompletionSource<ModalResult> TaskCompletionSource { get; init; }
}
