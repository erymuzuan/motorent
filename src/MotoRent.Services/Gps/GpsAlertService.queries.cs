using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services.Gps;

/// <summary>
/// Alert query operations.
/// </summary>
public partial class GpsAlertService
{
    /// <summary>
    /// Get active alerts for a shop.
    /// </summary>
    public async Task<List<GeofenceAlert>> GetActiveAlertsAsync(int shopId)
    {
        // Get vehicle IDs for this shop
        var vehiclesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId),
            1, 1000);

        var vehicleIds = vehiclesResult.ItemCollection.Select(v => v.VehicleId).ToList();

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GeofenceAlert>()
                .Where(a => vehicleIds.IsInList(a.VehicleId) &&
                           (a.Status == AlertStatus.Active || a.Status == AlertStatus.Acknowledged))
                .OrderByDescending(a => a.AlertTimestamp),
            1, 100);

        return result.ItemCollection;
    }

    /// <summary>
    /// Get alerts with filtering.
    /// </summary>
    public async Task<LoadOperation<GeofenceAlert>> GetAlertsAsync(
        int shopId,
        AlertStatus? status = null,
        AlertPriority? priority = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        // Get vehicle IDs for this shop
        var vehiclesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId),
            1, 1000);

        var vehicleIds = vehiclesResult.ItemCollection.Select(v => v.VehicleId).ToList();

        var query = this.Context.CreateQuery<GeofenceAlert>()
            .Where(a => vehicleIds.IsInList(a.VehicleId));

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(a => a.Priority == priority.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.AlertTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.AlertTimestamp <= toDate.Value);
        }

        query = query.OrderByDescending(a => a.AlertTimestamp);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Get alerts for a specific vehicle.
    /// </summary>
    public async Task<List<GeofenceAlert>> GetAlertsForVehicleAsync(
        int vehicleId,
        int limit = 50)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GeofenceAlert>()
                .Where(a => a.VehicleId == vehicleId)
                .OrderByDescending(a => a.AlertTimestamp),
            1, limit);

        return result.ItemCollection;
    }

    /// <summary>
    /// Get alert by ID.
    /// </summary>
    public async Task<GeofenceAlert?> GetAlertAsync(int alertId)
    {
        return await this.Context.LoadOneAsync<GeofenceAlert>(a => a.GeofenceAlertId == alertId);
    }

    /// <summary>
    /// Get count of active alerts for a shop.
    /// </summary>
    public async Task<int> GetActiveAlertCountAsync(int shopId)
    {
        var alerts = await this.GetActiveAlertsAsync(shopId);
        return alerts.Count;
    }

    /// <summary>
    /// Get count of critical alerts for a shop.
    /// </summary>
    public async Task<int> GetCriticalAlertCountAsync(int shopId)
    {
        var alerts = await this.GetActiveAlertsAsync(shopId);
        return alerts.Count(a => a.Priority == AlertPriority.Critical || a.Priority == AlertPriority.High);
    }
}
