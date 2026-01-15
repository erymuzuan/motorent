using Microsoft.Extensions.Logging;
using MotoRent.Services;

namespace MotoRent.Scheduler.Runners;

/// <summary>
/// Scheduled task that triggers automated maintenance alerts for all vehicles.
/// </summary>
public class MaintenanceAlertRunner : ITaskRunner
{
    private readonly MaintenanceAlertService m_alertService;
    private readonly ILogger<MaintenanceAlertRunner> m_logger;

    public string Name => nameof(MaintenanceAlertRunner);
    public string Description => "Triggers automated maintenance alerts for vehicles due for service";

    public MaintenanceAlertRunner(
        MaintenanceAlertService alertService,
        ILogger<MaintenanceAlertRunner> logger)
    {
        m_alertService = alertService;
        m_logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        m_logger.LogInformation("Starting automated maintenance alert generation...");

        try
        {
            int alertsCreated = await m_alertService.TriggerAlertsAsync("scheduler");
            m_logger.LogInformation("Alert generation completed. {Count} new alerts created.", alertsCreated);
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to generate maintenance alerts");
            throw;
        }
    }
}
