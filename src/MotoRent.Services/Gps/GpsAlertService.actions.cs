using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Alert action operations.
/// </summary>
public partial class GpsAlertService
{
    /// <summary>
    /// Acknowledge an alert.
    /// </summary>
    public async Task<SubmitOperation> AcknowledgeAlertAsync(int alertId, string username)
    {
        var alert = await this.Context.LoadOneAsync<GeofenceAlert>(a => a.GeofenceAlertId == alertId);
        if (alert is null)
        {
            return SubmitOperation.CreateFailure("Alert not found");
        }

        if (alert.Status != AlertStatus.Active)
        {
            return SubmitOperation.CreateFailure("Alert is not in active status");
        }

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedBy = username;
        alert.AcknowledgedTimestamp = DateTimeOffset.UtcNow;

        using var session = this.Context.OpenSession(username);
        session.Attach(alert);

        var result = await session.SubmitChanges("AcknowledgeAlert");

        if (result.Success)
        {
            this.Logger.LogInformation("Alert {AlertId} acknowledged by {Username}", alertId, username);
        }

        return result;
    }

    /// <summary>
    /// Resolve an alert with notes.
    /// </summary>
    public async Task<SubmitOperation> ResolveAlertAsync(
        int alertId,
        string resolutionNotes,
        string username)
    {
        var alert = await this.Context.LoadOneAsync<GeofenceAlert>(a => a.GeofenceAlertId == alertId);
        if (alert is null)
        {
            return SubmitOperation.CreateFailure("Alert not found");
        }

        if (alert.Status == AlertStatus.Resolved || alert.Status == AlertStatus.FalseAlarm)
        {
            return SubmitOperation.CreateFailure("Alert is already resolved");
        }

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedBy = username;
        alert.ResolvedTimestamp = DateTimeOffset.UtcNow;
        alert.ResolutionNotes = resolutionNotes;

        using var session = this.Context.OpenSession(username);
        session.Attach(alert);

        var result = await session.SubmitChanges("ResolveAlert");

        if (result.Success)
        {
            this.Logger.LogInformation("Alert {AlertId} resolved by {Username}: {Notes}",
                alertId, username, resolutionNotes);
        }

        return result;
    }

    /// <summary>
    /// Mark alert as false alarm.
    /// </summary>
    public async Task<SubmitOperation> MarkAsFalseAlarmAsync(
        int alertId,
        string reason,
        string username)
    {
        var alert = await this.Context.LoadOneAsync<GeofenceAlert>(a => a.GeofenceAlertId == alertId);
        if (alert is null)
        {
            return SubmitOperation.CreateFailure("Alert not found");
        }

        alert.Status = AlertStatus.FalseAlarm;
        alert.ResolvedBy = username;
        alert.ResolvedTimestamp = DateTimeOffset.UtcNow;
        alert.ResolutionNotes = $"False alarm: {reason}";

        using var session = this.Context.OpenSession(username);
        session.Attach(alert);

        var result = await session.SubmitChanges("MarkAsFalseAlarm");

        if (result.Success)
        {
            this.Logger.LogInformation("Alert {AlertId} marked as false alarm by {Username}: {Reason}",
                alertId, username, reason);
        }

        return result;
    }

    /// <summary>
    /// Bulk acknowledge multiple alerts.
    /// </summary>
    public async Task<int> BulkAcknowledgeAsync(IEnumerable<int> alertIds, string username)
    {
        var acknowledgedCount = 0;

        foreach (var alertId in alertIds)
        {
            var result = await this.AcknowledgeAlertAsync(alertId, username);
            if (result.Success)
            {
                acknowledgedCount++;
            }
        }

        return acknowledgedCount;
    }
}
