using Microsoft.AspNetCore.Components;

namespace MotoRent.Client.Services;

/// <summary>
/// Service for managing modal dialogs using Tabler CSS modals.
/// Supports nested (stacked) modals — opening a modal from within another modal
/// pushes the inner modal on top; closing it reveals the outer modal.
/// </summary>
public class ModalService : IModalService
{
    private readonly Stack<ModalState> m_modalStack = new();

    public event Action? OnChange;
    public ModalState? CurrentModal => m_modalStack.Count > 0 ? m_modalStack.Peek() : null;
    public IReadOnlyList<ModalState> ModalStack => m_modalStack.Reverse().ToList();

    public Task<ModalResult> ShowAsync<TComponent>(string title, ModalOptions? options = null)
        where TComponent : IComponent
    {
        return this.ShowAsync<TComponent>(title, new Dictionary<string, object?>(), options);
    }

    public Task<ModalResult> ShowAsync<TComponent>(string title, Dictionary<string, object?> parameters, ModalOptions? options = null)
        where TComponent : IComponent
    {
        var tcs = new TaskCompletionSource<ModalResult>();

        m_modalStack.Push(new ModalState
        {
            Title = title,
            ComponentType = typeof(TComponent),
            Parameters = parameters,
            Options = options ?? new ModalOptions(),
            TaskCompletionSource = tcs
        });

        this.OnChange?.Invoke();
        return tcs.Task;
    }

    public Task<ModalResult> ShowAsync(string title, RenderFragment content, ModalOptions? options = null)
    {
        var tcs = new TaskCompletionSource<ModalResult>();

        m_modalStack.Push(new ModalState
        {
            Title = title,
            ComponentType = typeof(object), // Placeholder
            ContentFragment = content,
            Options = options ?? new ModalOptions(),
            TaskCompletionSource = tcs
        });

        this.OnChange?.Invoke();
        return tcs.Task;
    }

    public void Close(ModalResult result)
    {
        if (m_modalStack.Count == 0) return;

        var modal = m_modalStack.Pop();
        this.OnChange?.Invoke();

        modal.TaskCompletionSource.TrySetResult(result);
    }
}
