using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Alert statistics operations.
/// </summary>
public partial class GpsAlertService
{
    /// <summary>
    /// Get alert statistics for a shop.
    /// </summary>
    public async Task<AlertStatistics> GetAlertStatisticsAsync(int shopId, int days = 30)
    {
        var fromDate = DateTimeOffset.UtcNow.AddDays(-days);

        var alertsResult = await this.GetAlertsAsync(shopId, fromDate: fromDate, pageSize: 1000);
        var alerts = alertsResult.ItemCollection;

        return new AlertStatistics
        {
            TotalAlerts = alerts.Count,
            ActiveAlerts = alerts.Count(a => a.Status == AlertStatus.Active),
            AcknowledgedAlerts = alerts.Count(a => a.Status == AlertStatus.Acknowledged),
            ResolvedAlerts = alerts.Count(a => a.Status == AlertStatus.Resolved),
            FalseAlarms = alerts.Count(a => a.Status == AlertStatus.FalseAlarm),
            CriticalAlerts = alerts.Count(a => a.Priority == AlertPriority.Critical),
            HighPriorityAlerts = alerts.Count(a => a.Priority == AlertPriority.High),
            ExitAlerts = alerts.Count(a => a.AlertType == GeofenceAlertType.Exit),
            EnterAlerts = alerts.Count(a => a.AlertType == GeofenceAlertType.Enter),
            AverageResolutionTimeMinutes = CalculateAverageResolutionTime(alerts)
        };
    }

    private static double? CalculateAverageResolutionTime(List<GeofenceAlert> alerts)
    {
        var resolvedAlerts = alerts
            .Where(a => a.Status == AlertStatus.Resolved && a.ResolvedTimestamp.HasValue)
            .ToList();

        if (resolvedAlerts.Count == 0)
            return null;

        var totalMinutes = resolvedAlerts
            .Sum(a => (a.ResolvedTimestamp!.Value - a.AlertTimestamp).TotalMinutes);

        return totalMinutes / resolvedAlerts.Count;
    }
}
