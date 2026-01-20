using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace MotoRent.Client.Services;

/// <summary>
/// Fluent builder for modal dialogs.
/// </summary>
public class ModalFluent<T> where T : IComponent
{
    private readonly IModalService m_modalService;
    private readonly string m_title;
    private readonly ModalOptions m_options;
    private readonly Dictionary<string, object?> m_parameters = new();

    public ModalFluent(IModalService modalService, string title, ModalOptions options)
    {
        this.m_modalService = modalService;
        this.m_title = title;
        this.m_options = options;
    }

    /// <summary>
    /// Sets a parameter on the dialog component using a strongly-typed expression.
    /// </summary>
    public ModalFluent<T> WithParameter<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        if (selector.Body is MemberExpression memberExpression)
        {
            this.m_parameters[memberExpression.Member.Name] = value;
        }
        return this;
    }

    /// <summary>
    /// Sets the modal size.
    /// </summary>
    public ModalFluent<T> WithSize(ModalSize size)
    {
        this.m_options.Size = size;
        return this;
    }

    /// <summary>
    /// Sets the modal to small size.
    /// </summary>
    public ModalFluent<T> Small()
    {
        this.m_options.Size = ModalSize.Small;
        return this;
    }

    /// <summary>
    /// Sets the modal to medium size.
    /// </summary>
    public ModalFluent<T> Medium()
    {
        this.m_options.Size = ModalSize.Medium;
        return this;
    }

    /// <summary>
    /// Sets the modal to large size.
    /// </summary>
    public ModalFluent<T> Large()
    {
        this.m_options.Size = ModalSize.Large;
        return this;
    }

    /// <summary>
    /// Sets the modal to extra large size.
    /// </summary>
    public ModalFluent<T> ExtraLarge()
    {
        this.m_options.Size = ModalSize.ExtraLarge;
        return this;
    }

    /// <summary>
    /// Sets the modal to fullscreen.
    /// </summary>
    public ModalFluent<T> WithFullscreen()
    {
        this.m_options.Size = ModalSize.Fullscreen;
        return this;
    }

    /// <summary>
    /// Sets whether to show the modal header.
    /// </summary>
    public ModalFluent<T> WithHeader(bool showHeader = true)
    {
        this.m_options.ShowHeader = showHeader;
        return this;
    }

    /// <summary>
    /// Sets whether to blur the background.
    /// </summary>
    public ModalFluent<T> WithBlurBackground(bool blur = true)
    {
        this.m_options.BlurBackground = blur;
        return this;
    }

    /// <summary>
    /// Sets whether the modal can be closed by pressing Escape.
    /// </summary>
    public ModalFluent<T> WithCloseOnEsc(bool closeOnEsc = true)
    {
        this.m_options.CloseOnEsc = closeOnEsc;
        return this;
    }

    /// <summary>
    /// Sets whether the modal can be closed by clicking outside.
    /// </summary>
    public ModalFluent<T> WithCloseOnClickOutside(bool closeOnClick = true)
    {
        this.m_options.CloseOnClickOutside = closeOnClick;
        return this;
    }

    /// <summary>
    /// Sets whether the modal is scrollable.
    /// </summary>
    public ModalFluent<T> WithScrollable(bool scrollable = true)
    {
        this.m_options.Scrollable = scrollable;
        return this;
    }

    /// <summary>
    /// Sets whether the modal is vertically centered.
    /// </summary>
    public ModalFluent<T> WithCentered(bool centered = true)
    {
        this.m_options.Centered = centered;
        return this;
    }

    /// <summary>
    /// Sets the status color for the modal header.
    /// </summary>
    public ModalFluent<T> WithStatusColor(string? color)
    {
        this.m_options.StatusColor = color;
        return this;
    }

    /// <summary>
    /// Shows the dialog and returns the result.
    /// </summary>
    public async Task<ModalResult> ShowDialogAsync()
    {
        return await this.m_modalService.ShowAsync<T>(this.m_title, this.m_parameters, this.m_options);
    }

    /// <summary>
    /// Shows the dialog and returns true if not cancelled.
    /// </summary>
    public async Task<bool> ShowAndConfirmAsync()
    {
        var result = await this.ShowDialogAsync();
        return !result.Cancelled;
    }

    /// <summary>
    /// Shows the dialog and returns the data if not cancelled.
    /// </summary>
    public async Task<TData?> ShowAndGetDataAsync<TData>()
    {
        var result = await this.ShowDialogAsync();
        return result is { Cancelled: false, Data: TData data } ? data : default;
    }
}
