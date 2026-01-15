using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// LINE notification placeholder operations.
/// Actual LINE integration to be implemented with LINE Messaging API.
/// </summary>
public partial class GpsAlertService
{
    /// <summary>
    /// Mark that LINE notification was sent for an alert.
    /// </summary>
    public async Task<SubmitOperation> MarkLineNotificationSentAsync(
        int alertId,
        string? messageId,
        string username)
    {
        var alert = await this.Context.LoadOneAsync<GeofenceAlert>(a => a.GeofenceAlertId == alertId);
        if (alert is null)
        {
            return SubmitOperation.CreateFailure("Alert not found");
        }

        alert.LineNotificationSent = true;
        alert.LineMessageId = messageId;

        using var session = this.Context.OpenSession(username);
        session.Attach(alert);

        return await session.SubmitChanges("UpdateLineNotificationStatus");
    }

    /// <summary>
    /// Get alerts that need LINE notification.
    /// </summary>
    public async Task<List<GeofenceAlert>> GetAlertsNeedingLineNotificationAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GeofenceAlert>()
                .Where(a => a.Status == AlertStatus.Active &&
                           !a.LineNotificationSent &&
                           a.AlertTimestamp > DateTimeOffset.UtcNow.AddHours(-1))
                .OrderBy(a => a.AlertTimestamp),
            1, 100);

        return result.ItemCollection;
    }
}
