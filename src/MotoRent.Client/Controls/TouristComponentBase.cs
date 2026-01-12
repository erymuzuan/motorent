using Microsoft.AspNetCore.Components;
using MotoRent.Domain.Tourist;

namespace MotoRent.Client.Controls;

/// <summary>
/// Base class for tourist-facing Blazor components.
/// Provides access to TenantContext via cascading parameter.
/// Inherits all common functionality from MotoRentComponentBase.
/// </summary>
public class TouristComponentBase : MotoRentComponentBase
{
    /// <summary>
    /// The current tenant context, cascaded from TouristLayout.
    /// Contains organization info, branding, and settings.
    /// </summary>
    [CascadingParameter]
    public TenantContext? TenantContext { get; set; }

    #region Tenant Properties

    /// <summary>
    /// Gets the current tenant's AccountNo from the cascaded context.
    /// Falls back to URL parameter if context not available.
    /// </summary>
    protected string TenantAccountNo => TenantContext?.AccountNo ?? "";

    /// <summary>
    /// Gets the tenant's organization name for display.
    /// </summary>
    protected string TenantName => TenantContext?.OrganizationName ?? "MotoRent";

    /// <summary>
    /// Gets the tenant's currency code.
    /// </summary>
    protected string TenantCurrency => TenantContext?.Currency ?? "THB";

    /// <summary>
    /// Gets the tenant's logo URL.
    /// </summary>
    protected string? TenantLogo => TenantContext?.LogoUrl;

    /// <summary>
    /// Gets the tenant's branding configuration.
    /// </summary>
    protected TenantBranding TenantBranding => TenantContext?.Branding ?? new TenantBranding();

    /// <summary>
    /// Indicates whether the tenant context is loaded and valid.
    /// </summary>
    protected bool IsTenantLoaded => TenantContext?.IsLoaded == true;

    #endregion

    #region URL Helpers

    /// <summary>
    /// Generates a tenant-prefixed URL for navigation.
    /// Example: TenantUrl("browse") returns "/tourist/AccountNo/browse"
    /// </summary>
    /// <param name="path">The relative path within tourist section</param>
    /// <returns>Full tenant-prefixed URL</returns>
    protected string TenantUrl(string path)
    {
        if (string.IsNullOrEmpty(TenantAccountNo))
            return path.StartsWith("/") ? path : $"/{path}";

        var cleanPath = path.TrimStart('/');
        return $"/tourist/{TenantAccountNo}/{cleanPath}";
    }

    /// <summary>
    /// Navigates to a tenant-prefixed URL.
    /// </summary>
    /// <param name="path">The relative path within tourist section</param>
    /// <param name="forceLoad">Whether to force a full page reload</param>
    protected void NavigateToTenant(string path, bool forceLoad = false)
    {
        NavigationManager.NavigateTo(TenantUrl(path), forceLoad);
    }

    /// <summary>
    /// Gets the tenant's browse page URL.
    /// </summary>
    protected string BrowseUrl => TenantUrl("browse");

    /// <summary>
    /// Gets the tenant's rental history page URL.
    /// </summary>
    protected string MyRentalsUrl => TenantUrl("my-rentals");

    /// <summary>
    /// Gets the vehicle details page URL for a specific vehicle.
    /// </summary>
    protected string VehicleUrl(int vehicleId) => TenantUrl($"vehicle/{vehicleId}");

    /// <summary>
    /// Gets the reservation page URL for a specific vehicle.
    /// </summary>
    protected string ReserveUrl(int vehicleId) => TenantUrl($"reserve/{vehicleId}");

    #endregion

    #region Currency Formatting

    /// <summary>
    /// Formats a decimal as currency using the tenant's currency code.
    /// </summary>
    protected string FormatTenantCurrency(decimal amount) => $"{amount:N0} {TenantCurrency}";

    /// <summary>
    /// Formats a decimal as currency with decimals using the tenant's currency code.
    /// </summary>
    protected string FormatTenantCurrencyWithDecimals(decimal amount) => $"{amount:N2} {TenantCurrency}";

    #endregion

    #region Branding Helpers

    /// <summary>
    /// Gets the primary color from tenant branding.
    /// </summary>
    protected string PrimaryColor => TenantBranding.PrimaryColor;

    /// <summary>
    /// Gets the secondary color from tenant branding.
    /// </summary>
    protected string SecondaryColor => TenantBranding.SecondaryColor;

    /// <summary>
    /// Gets the accent color from tenant branding.
    /// </summary>
    protected string AccentColor => TenantBranding.AccentColor;

    /// <summary>
    /// Gets the layout template name (Modern, Classic, Minimal).
    /// </summary>
    protected string LayoutTemplate => TenantBranding.LayoutTemplate;

    #endregion
}
