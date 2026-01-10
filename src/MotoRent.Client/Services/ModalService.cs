using Microsoft.AspNetCore.Components;

namespace MotoRent.Client.Services;

/// <summary>
/// Service for managing modal dialogs using Tabler CSS modals.
/// </summary>
public class ModalService : IModalService
{
    public event Action? OnChange;
    public ModalState? CurrentModal { get; private set; }

    public Task<ModalResult> ShowAsync<TComponent>(string title, ModalOptions? options = null)
        where TComponent : IComponent
    {
        return this.ShowAsync<TComponent>(title, new Dictionary<string, object?>(), options);
    }

    public Task<ModalResult> ShowAsync<TComponent>(string title, Dictionary<string, object?> parameters, ModalOptions? options = null)
        where TComponent : IComponent
    {
        var tcs = new TaskCompletionSource<ModalResult>();

        this.CurrentModal = new ModalState
        {
            Title = title,
            ComponentType = typeof(TComponent),
            Parameters = parameters,
            Options = options ?? new ModalOptions(),
            TaskCompletionSource = tcs
        };

        this.OnChange?.Invoke();
        return tcs.Task;
    }

    public Task<ModalResult> ShowAsync(string title, RenderFragment content, ModalOptions? options = null)
    {
        var tcs = new TaskCompletionSource<ModalResult>();

        this.CurrentModal = new ModalState
        {
            Title = title,
            ComponentType = typeof(object), // Placeholder
            ContentFragment = content,
            Options = options ?? new ModalOptions(),
            TaskCompletionSource = tcs
        };

        this.OnChange?.Invoke();
        return tcs.Task;
    }

    public void Close(ModalResult result)
    {
        var modal = this.CurrentModal;
        this.CurrentModal = null;
        this.OnChange?.Invoke();

        modal?.TaskCompletionSource.TrySetResult(result);
    }
}
