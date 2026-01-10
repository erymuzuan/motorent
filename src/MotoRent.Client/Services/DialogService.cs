using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using MotoRent.Client.Controls.Dialogs;

namespace MotoRent.Client.Services;

/// <summary>
/// High-level dialog service providing fluent API for showing dialogs.
/// </summary>
public class DialogService(
    IModalService modalService,
    ToastService toastService,
    IConfiguration configuration)
{
    private IModalService ModalService { get; } = modalService;
    private ToastService ToastService { get; } = toastService;
    private IConfiguration Configuration { get; } = configuration;

    /// <summary>
    /// Creates a fluent builder for showing a dialog.
    /// </summary>
    public ModalFluent<T> Create<T>(string title) where T : IComponent
    {
        var options = new ModalOptions
        {
            Size = ModalSize.Large,
            CloseOnEsc = true,
            Centered = true
        };
        return new ModalFluent<T>(this.ModalService, title, options);
    }

    /// <summary>
    /// Shows a confirmation dialog and returns true if user clicks OK.
    /// </summary>
    public async Task<bool> ConfirmAsync(string message, string? title = null)
    {
        title ??= this.GetAppName();
        var result = await this.Create<WindowDialog>(title)
            .WithParameter(x => x.Message, message)
            .WithParameter(x => x.ButtonOptions, [DialogResult.Ok, DialogResult.Cancel])
            .ShowDialogAsync();

        return result is { Cancelled: false, Data: DialogResult.Ok };
    }

    /// <summary>
    /// Shows a Yes/No confirmation dialog and returns true if user clicks Yes.
    /// </summary>
    public async Task<bool> ConfirmYesNoAsync(string message, string? title = null)
    {
        title ??= this.GetAppName();
        var result = await this.Create<WindowDialog>(title)
            .WithParameter(x => x.Message, message)
            .WithParameter(x => x.ButtonOptions, [DialogResult.Yes, DialogResult.No])
            .WithParameter(x => x.Icon, MessageIcon.Question)
            .ShowDialogAsync();

        return result is { Cancelled: false, Data: DialogResult.Yes };
    }

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    public async Task ShowMessageAsync(string message, string? title = null, MessageIcon? icon = null)
    {
        title ??= this.GetAppName();
        await this.Create<MessageBoxDialog>(title)
            .WithParameter(x => x.Message, message)
            .WithParameter(x => x.Icon, icon ?? MessageIcon.Info)
            .WithHeader(false)
            .ShowDialogAsync();
    }

    /// <summary>
    /// Shows a prompt dialog and returns the user's input.
    /// </summary>
    public async Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null)
    {
        title ??= this.GetAppName();
        var result = await this.Create<PromptDialog>(title)
            .WithParameter(x => x.Message, message)
            .WithParameter(x => x.DefaultValue, defaultValue)
            .WithSize(ModalSize.Medium)
            .ShowDialogAsync();

        return result is { Cancelled: false, Data: string text } ? text : null;
    }

    private string GetAppName() =>
        this.Configuration.GetSection("App")["Name"] ?? "MotoRent";
}

/// <summary>
/// Message icon types for dialogs.
/// </summary>
public enum MessageIcon
{
    Info,
    Success,
    Warning,
    Error,
    Question
}
