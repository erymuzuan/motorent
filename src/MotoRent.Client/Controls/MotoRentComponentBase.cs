using HashidsNet;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MotoRent.Client.Services;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for all MotoRent Blazor components.
/// Provides common services: DataContext, RequestContext, DialogService, ToastService, Navigation, Logging, and CommonLocalizer.
/// </summary>
public class MotoRentComponentBase : ComponentBase
{
    [Inject] protected ILogger<MotoRentComponentBase> Logger { get; set; } = null!;
    [Inject] protected RentalDataContext DataContext { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected ToastService ToastService { get; set; } = null!;
    [Inject] protected IRequestContext RequestContext { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected IStringLocalizer<CommonResources> CommonLocalizer { get; set; } = null!;
    [Inject] protected IHashids HashId { get; set; } = null!;

    #region Request Context Properties

    /// <summary>
    /// Gets the current shop ID from the request context.
    /// </summary>
    protected int ShopId => this.RequestContext.GetShopId();

    /// <summary>
    /// Gets the current username from the request context, or "system" if not authenticated.
    /// </summary>
    protected string UserName => this.RequestContext.GetUserName() ?? "system";

    /// <summary>
    /// Gets the current organization AccountNo (tenant identifier).
    /// </summary>
    protected string? AccountNo => this.RequestContext.GetAccountNo();

    /// <summary>
    /// Gets the current date in the user's timezone.
    /// </summary>
    protected DateOnly Today => this.RequestContext.GetDate();

    #endregion

    #region Date/Time Formatting

    /// <summary>
    /// Formats a DateTimeOffset as a general date/time string in the user's timezone.
    /// </summary>
    protected string FormatDateTime(DateTimeOffset? dto) =>
        dto.HasValue ? this.RequestContext.FormatDateTime(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a short date string in the user's timezone.
    /// </summary>
    protected string FormatDate(DateTimeOffset? dto) =>
        dto.HasValue ? this.RequestContext.FormatDate(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a short time string in the user's timezone.
    /// </summary>
    protected string FormatTime(DateTimeOffset? dto) =>
        dto.HasValue ? this.RequestContext.FormatTime(dto.Value) : "";

    /// <summary>
    /// Formats a DateTimeOffset as a sortable string (yyyy-MM-ddTHH:mm).
    /// </summary>
    protected string FormatSortable(DateTimeOffset? dto) =>
        this.RequestContext.FormatDateTimeOffsetSortable(dto);

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
        this.ToastService.ShowSuccess(message);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    protected void ShowError(string message) =>
        this.ToastService.ShowError(message);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    protected void ShowWarning(string message) =>
        this.ToastService.ShowWarning(message);

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    protected void ShowInfo(string message) =>
        this.ToastService.ShowInfo(message);

    /// <summary>
    /// Triggers a UI refresh.
    /// </summary>
    public void ViewStateChanged() => this.StateHasChanged();

    #endregion

    #region Confirmation Dialogs

    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirms.
    /// </summary>
    protected async Task<bool> ConfirmAsync(string title, string message, string yesText = "Yes", string noText = "Cancel")
    {
        return await this.DialogService.ConfirmYesNoAsync(message, title);
    }

    /// <summary>
    /// Shows a delete confirmation dialog.
    /// </summary>
    protected Task<bool> ConfirmDeleteAsync(string itemName) =>
        this.ConfirmAsync("Confirm Delete", $"Are you sure you want to delete '{itemName}'?", "Delete", "Cancel");

    #endregion

    #region HashId Encoding/Decoding

    /// <summary>
    /// Decodes a hashed ID string back to an integer.
    /// Returns 0 if the ID is null, empty, or invalid.
    /// </summary>
    protected int DecodeId(string? id, int index = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            return 0;

        var list = this.HashId.Decode(id);
        return list.Length > index ? list[index] : 0;
    }

    /// <summary>
    /// Decodes a hashed ID string to an array of integers.
    /// Returns empty array if the ID is null, empty, or invalid.
    /// </summary>
    protected int[] DecodeIdList(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return [];

        return this.HashId.Decode(id);
    }

    /// <summary>
    /// Encodes one or more integer IDs to a hash string for use in URLs.
    /// </summary>
    protected string EncodeId(params int[] ids) => this.HashId.Encode(ids);

    #endregion
}

/// <summary>
/// Marker class for common/shared localization resources.
/// Resource file: Resources/CommonResources.resx
/// </summary>
public class CommonResources { }
