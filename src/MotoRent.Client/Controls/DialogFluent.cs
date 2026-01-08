using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MotoRent.Client.Controls;

/// <summary>
/// Fluent builder for MudBlazor dialogs.
/// Use DialogService.CreateDialog&lt;T&gt;() to create an instance.
/// </summary>
public record DialogFluent<T> where T : IComponent
{
    public IDialogService DialogService { get; }
    public DialogParameters<T> Parameters { get; }
    public DialogOptions Options { get; }
    public string Title { get; set; }

    public DialogFluent(IDialogService dialogService, string title)
    {
        DialogService = dialogService;
        Title = title;
        Parameters = new DialogParameters<T>();
        Options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };
    }

    /// <summary>
    /// Sets a parameter on the dialog component using a strongly-typed expression.
    /// </summary>
    public DialogFluent<T> WithParameter<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        Parameters.Add(selector, value);
        return this;
    }

    /// <summary>
    /// Sets the maximum width of the dialog.
    /// </summary>
    public DialogFluent<T> WithMaxWidth(MaxWidth maxWidth)
    {
        Options.MaxWidth = maxWidth;
        return this;
    }

    /// <summary>
    /// Sets the dialog to small size.
    /// </summary>
    public DialogFluent<T> Small()
    {
        Options.MaxWidth = MaxWidth.Small;
        return this;
    }

    /// <summary>
    /// Sets the dialog to medium size (default).
    /// </summary>
    public DialogFluent<T> Medium()
    {
        Options.MaxWidth = MaxWidth.Medium;
        return this;
    }

    /// <summary>
    /// Sets the dialog to large size.
    /// </summary>
    public DialogFluent<T> Large()
    {
        Options.MaxWidth = MaxWidth.Large;
        return this;
    }

    /// <summary>
    /// Sets the dialog to extra large size.
    /// </summary>
    public DialogFluent<T> ExtraLarge()
    {
        Options.MaxWidth = MaxWidth.ExtraLarge;
        return this;
    }

    /// <summary>
    /// Sets the dialog to stretch to its MaxWidth.
    /// </summary>
    public DialogFluent<T> WithFullWidth(bool fullWidth = true)
    {
        Options.FullWidth = fullWidth;
        return this;
    }

    /// <summary>
    /// Shows a close button in the dialog header.
    /// </summary>
    public DialogFluent<T> WithCloseButton(bool closeButton = true)
    {
        Options.CloseButton = closeButton;
        return this;
    }

    /// <summary>
    /// Allows closing the dialog by pressing Escape key.
    /// </summary>
    public DialogFluent<T> WithCloseOnEscapeKey(bool closeOnEsc = true)
    {
        Options.CloseOnEscapeKey = closeOnEsc;
        return this;
    }

    /// <summary>
    /// Prevents closing the dialog by clicking the backdrop.
    /// </summary>
    public DialogFluent<T> WithBackdropClick(bool backdropClick = true)
    {
        Options.BackdropClick = backdropClick;
        return this;
    }

    /// <summary>
    /// Disables backdrop click to close (modal behavior).
    /// </summary>
    public DialogFluent<T> DisableBackdropClick()
    {
        Options.BackdropClick = false;
        return this;
    }

    /// <summary>
    /// Hides the dialog header.
    /// </summary>
    public DialogFluent<T> WithNoHeader(bool noHeader = true)
    {
        Options.NoHeader = noHeader;
        return this;
    }

    /// <summary>
    /// Sets the dialog position.
    /// </summary>
    public DialogFluent<T> WithPosition(DialogPosition position)
    {
        Options.Position = position;
        return this;
    }

    /// <summary>
    /// Centers the dialog (default position).
    /// </summary>
    public DialogFluent<T> Centered()
    {
        Options.Position = DialogPosition.Center;
        return this;
    }

    /// <summary>
    /// Positions the dialog at the top center.
    /// </summary>
    public DialogFluent<T> TopCenter()
    {
        Options.Position = DialogPosition.TopCenter;
        return this;
    }

    /// <summary>
    /// Makes the dialog fullscreen.
    /// </summary>
    public DialogFluent<T> Fullscreen(bool fullscreen = true)
    {
        Options.FullScreen = fullscreen;
        return this;
    }

    /// <summary>
    /// Shows the dialog and waits for the result.
    /// Returns the DialogResult.
    /// </summary>
    public async Task<DialogResult?> ShowAsync()
    {
        var dialog = await DialogService.ShowAsync<T>(Title, Parameters, Options);
        return await dialog.Result;
    }

    /// <summary>
    /// Shows the dialog and returns true if not canceled.
    /// </summary>
    public async Task<bool> ShowAndConfirmAsync()
    {
        var result = await ShowAsync();
        return result is { Canceled: false };
    }

    /// <summary>
    /// Shows the dialog and returns the data if not canceled, otherwise default.
    /// </summary>
    public async Task<TData?> ShowAndGetDataAsync<TData>()
    {
        var result = await ShowAsync();
        if (result is { Canceled: false, Data: TData data })
        {
            return data;
        }
        return default;
    }

    /// <summary>
    /// Shows the dialog without waiting for the result.
    /// Returns the dialog reference for manual handling.
    /// </summary>
    public async Task<IDialogReference> ShowNonBlockingAsync()
    {
        return await DialogService.ShowAsync<T>(Title, Parameters, Options);
    }
}
