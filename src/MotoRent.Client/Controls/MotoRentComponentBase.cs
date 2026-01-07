using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MudBlazor;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for all MotoRent Blazor components.
/// Provides common services: DataContext, RequestContext, DialogService, Snackbar, Navigation, and Logging.
/// </summary>
public class MotoRentComponentBase : ComponentBase
{
    [Inject] protected ILogger<MotoRentComponentBase> Logger { get; set; } = null!;
    [Inject] protected RentalDataContext DataContext { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IRequestContext RequestContext { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// Gets the current shop ID from the request context.
    /// </summary>
    protected int ShopId => RequestContext.GetShopId();

    /// <summary>
    /// Gets the current username from the request context.
    /// </summary>
    protected string UserName => RequestContext.GetUserName();

    /// <summary>
    /// Gets the current date in the user's timezone.
    /// </summary>
    protected DateOnly Today => RequestContext.GetDate();

    #region Date/Time Formatting

    /// <summary>
    /// Formats a DateTimeOffset as a general date/time string in the user's timezone.
    /// </summary>
    protected string FormatDateTime(DateTimeOffset? dto) =>
        dto.HasValue ? RequestContext.FormatDateTime(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a short date string in the user's timezone.
    /// </summary>
    protected string FormatDate(DateTimeOffset? dto) =>
        dto.HasValue ? RequestContext.FormatDate(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a short time string in the user's timezone.
    /// </summary>
    protected string FormatTime(DateTimeOffset? dto) =>
        dto.HasValue ? RequestContext.FormatTime(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a sortable string (yyyy-MM-ddTHH:mm).
    /// </summary>
    protected string FormatSortable(DateTimeOffset? dto) =>
        RequestContext.FormatDateTimeOffsetSortable(dto);

    #endregion

    #region Currency Formatting

    /// <summary>
    /// Formats a decimal as Thai Baht currency.
    /// </summary>
    protected string FormatCurrency(decimal amount) => $"{amount:N0} THB";

    /// <summary>
    /// Formats a decimal as Thai Baht currency with decimals.
    /// </summary>
    protected string FormatCurrencyWithDecimals(decimal amount) => $"{amount:N2} THB";

    #endregion

    #region UI Helpers

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    protected void ShowSuccess(string message) =>
        Snackbar.Add(message, Severity.Success);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    protected void ShowError(string message) =>
        Snackbar.Add(message, Severity.Error);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    protected void ShowWarning(string message) =>
        Snackbar.Add(message, Severity.Warning);

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    protected void ShowInfo(string message) =>
        Snackbar.Add(message, Severity.Info);

    /// <summary>
    /// Triggers a UI refresh.
    /// </summary>
    public void ViewStateChanged() => StateHasChanged();

    #endregion

    #region Confirmation Dialogs

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirms.
    /// </summary>
    protected async Task<bool> ConfirmAsync(string title, string message, string yesText = "Yes", string noText = "Cancel")
    {
        var parameters = new DialogParameters<MudMessageBox>
        {
            { x => x.Title, title },
            { x => x.Message, message },
            { x => x.YesText, yesText },
            { x => x.NoText, noText }
        };

        var dialog = await DialogService.ShowAsync<MudMessageBox>(title, parameters);
        var result = await dialog.Result;

        return result is { Canceled: false };
    }

    /// <summary>
    /// Shows a delete confirmation dialog.
    /// </summary>
    protected Task<bool> ConfirmDeleteAsync(string itemName) =>
        ConfirmAsync("Confirm Delete", $"Are you sure you want to delete '{itemName}'?", "Delete", "Cancel");

    #endregion
}
