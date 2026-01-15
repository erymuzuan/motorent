using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Manages GPS alerts and notifications (in-app and LINE).
/// </summary>
public partial class GpsAlertService(
    RentalDataContext context,
    ILogger<GpsAlertService> logger)
{
    private RentalDataContext Context { get; } = context;
    private ILogger<GpsAlertService> Logger { get; } = logger;
}

/// <summary>
/// Alert statistics for dashboard display.
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int FalseAlarms { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighPriorityAlerts { get; set; }
    public int ExitAlerts { get; set; }
    public int EnterAlerts { get; set; }
    public double? AverageResolutionTimeMinutes { get; set; }
}
