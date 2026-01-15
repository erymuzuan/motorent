using Microsoft.Extensions.Logging;
using MotoRent.Services;

namespace MotoRent.Scheduler.Runners;

/// <summary>
/// Scheduled task that triggers automated maintenance alerts for all vehicles.
/// </summary>
public class MaintenanceAlertRunner(MaintenanceAlertService alertService, ILogger<MaintenanceAlertRunner> logger) : ITaskRunner
{
    private MaintenanceAlertService AlertService { get; } = alertService;
    private ILogger<MaintenanceAlertRunner> Logger { get; } = logger;

    public string Name => nameof(MaintenanceAlertRunner);
    public string Description => "Triggers automated maintenance alerts for vehicles due for service";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        this.Logger.LogInformation("Starting automated maintenance alert generation...");

        try
        {
            int alertsCreated = await this.AlertService.TriggerAlertsAsync("scheduler");
            this.Logger.LogInformation("Alert generation completed. {Count} new alerts created.", alertsCreated);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to generate maintenance alerts");
            throw;
        }
    }
}
