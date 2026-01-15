using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Services;

namespace MotoRent.Client.Services;

/// <summary>
/// Client-side service for managing maintenance alerts.
/// Wraps MaintenanceAlertService for use in Blazor Server components.
/// </summary>
public class AlertService(MaintenanceAlertService alertService, IRequestContext requestContext)
{
    private MaintenanceAlertService AlertServiceBackend { get; } = alertService;
    private IRequestContext RequestContext { get; } = requestContext;

    /// <summary>
    /// Gets active maintenance alerts for the current shop.
    /// </summary>
    public async Task<LoadOperation<MaintenanceAlert>> GetActiveAlertsAsync(int page = 1, int size = 40)
    {
        var shopId = this.RequestContext.GetShopId();
        if (shopId <= 0)
        {
            return new LoadOperation<MaintenanceAlert> { ItemCollection = [], TotalRows = 0 };
        }

        return await this.AlertServiceBackend.GetActiveAlertsAsync(shopId, page, size);
    }

    /// <summary>
    /// Marks an alert as read.
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int alertId)
    {
        var username = this.RequestContext.GetUserName() ?? "system";
        var result = await this.AlertServiceBackend.MarkAsReadAsync(alertId, username);
        return result.Success;
    }

    /// <summary>
    /// Resolves an alert.
    /// </summary>
    public async Task<bool> ResolveAlertAsync(int alertId, string notes)
    {
        var username = this.RequestContext.GetUserName() ?? "system";
        var result = await this.AlertServiceBackend.ResolveAlertAsync(alertId, notes, username);
        return result.Success;
    }

    /// <summary>
    /// Manually triggers alert generation (Admin only).
    /// </summary>
    public async Task<int> TriggerAlertsAsync()
    {
        var username = this.RequestContext.GetUserName() ?? "system";
        return await this.AlertServiceBackend.TriggerAlertsAsync(username);
    }
}
