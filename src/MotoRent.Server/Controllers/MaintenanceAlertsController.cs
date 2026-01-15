using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Services;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for automated maintenance alerts.
/// </summary>
[ApiController]
[Route("api/maintenance-alerts")]
[Authorize]
public class MaintenanceAlertsController : ControllerBase
{
    private MaintenanceAlertService AlertService { get; }
    private IRequestContext RequestContext { get; }
    private ILogger<MaintenanceAlertsController> Logger { get; }

    public MaintenanceAlertsController(
        MaintenanceAlertService alertService,
        IRequestContext requestContext,
        ILogger<MaintenanceAlertsController> logger)
    {
        this.AlertService = alertService;
        this.RequestContext = requestContext;
        this.Logger = logger;
    }

    /// <summary>
    /// Gets active maintenance alerts for the current shop.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveAlerts([FromQuery] int page = 1, [FromQuery] int size = 40)
    {
        try
        {
            var shopId = this.RequestContext.GetShopId();
            if (shopId <= 0)
            {
                return this.BadRequest(new { error = "No shop context available" });
            }

            var result = await this.AlertService.GetActiveAlertsAsync(shopId, page, size);
            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get active maintenance alerts");
            return this.StatusCode(500, new { error = "Failed to retrieve alerts" });
        }
    }

    /// <summary>
    /// Marks a maintenance alert as read.
    /// </summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var username = this.RequestContext.GetUserName() ?? "system";
            var result = await this.AlertService.MarkAsReadAsync(id, username);

            if (!result.Success)
            {
                return this.BadRequest(new { error = result.Message });
            }

            return this.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to mark alert {AlertId} as read", id);
            return this.StatusCode(500, new { error = "Failed to update alert" });
        }
    }

    /// <summary>
    /// Resolves a maintenance alert.
    /// </summary>
    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> ResolveAlert(int id, [FromBody] ResolveAlertRequest request)
    {
        try
        {
            var username = this.RequestContext.GetUserName() ?? "system";
            var result = await this.AlertService.ResolveAlertAsync(id, request.Notes ?? "", username);

            if (!result.Success)
            {
                return this.BadRequest(new { error = result.Message });
            }

            return this.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to resolve alert {AlertId}", id);
            return this.StatusCode(500, new { error = "Failed to resolve alert" });
        }
    }

    /// <summary>
    /// Manually triggers alert generation (Admin only).
    /// </summary>
    [HttpPost("trigger")]
    [Authorize(Roles = UserAccount.ORG_ADMIN)]
    public async Task<IActionResult> TriggerAlerts()
    {
        try
        {
            var username = this.RequestContext.GetUserName() ?? "system";
            var count = await this.AlertService.TriggerAlertsAsync(username);
            return this.Ok(new { success = true, alertsCreated = count });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to manually trigger maintenance alerts");
            return this.StatusCode(500, new { error = "Failed to trigger alert generation" });
        }
    }
}

/// <summary>
/// Request model for resolving an alert.
/// </summary>
public class ResolveAlertRequest
{
    public string? Notes { get; set; }
}
