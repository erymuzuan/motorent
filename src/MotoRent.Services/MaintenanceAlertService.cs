using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Services.Core;

namespace MotoRent.Services;

/// <summary>
/// Service for triggering and managing automated maintenance alerts.
/// </summary>
public class MaintenanceAlertService(RentalDataContext context, MaintenanceService maintenanceService)
{
    private RentalDataContext Context { get; } = context;
    private MaintenanceService MaintenanceService { get; } = maintenanceService;

    /// <summary>
    /// Scans all vehicles and their maintenance schedules to trigger alerts.
    /// </summary>
    /// <returns>Number of new alerts created.</returns>
    public async Task<int> TriggerAlertsAsync(string username)
    {
        // 1. Get all non-retired vehicles
        // Note: In a large system, we might want to batch this by shop or last check date.
        var vehiclesQuery = this.Context.CreateQuery<Vehicle>()
            .Where(v => v.Status != VehicleStatus.Retired);

        var vehiclesResult = await this.Context.LoadAsync(vehiclesQuery, page: 1, size: 5000, includeTotalRows: false);
        var vehicles = vehiclesResult.ItemCollection;

        int alertsCreated = 0;
        var today = DateTimeOffset.UtcNow;

        using var session = this.Context.OpenSession(username);

        foreach (var vehicle in vehicles)
        {
            // 2. Get schedules for this vehicle
            // Note: MaintenanceSchedule currently uses MotorbikeId, which is VehicleId.
            var schedules = await this.MaintenanceService.GetSchedulesForMotorbikeAsync(vehicle.VehicleId);

            foreach (var schedule in schedules)
            {
                // 3. Calculate current status
                var status = this.MaintenanceService.CalculateStatus(
                    schedule.NextDueDate, schedule.NextDueMileage,
                    vehicle.Mileage ?? 0, today);

                if (status != MaintenanceStatus.Ok)
                {
                    // 4. Check if an active (unread) alert already exists for this schedule/status
                    var existingAlert = await this.Context.LoadOneAsync<MaintenanceAlert>(
                        ma => ma.VehicleId == vehicle.VehicleId &&
                              ma.ServiceTypeId == schedule.ServiceTypeId &&
                              ma.Status == status &&
                              !ma.IsRead &&
                              ma.ResolvedDate == null);

                    if (existingAlert == null)
                    {
                        // 5. Create new alert
                        var alert = new MaintenanceAlert
                        {
                            VehicleId = vehicle.VehicleId,
                            MaintenanceScheduleId = schedule.MaintenanceScheduleId,
                            ServiceTypeId = schedule.ServiceTypeId,
                            Status = status,
                            TriggerDate = today,
                            TriggerMileage = vehicle.Mileage,
                            TriggerEngineHours = vehicle.EngineHours,
                            IsRead = false,
                            VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                            LicensePlate = vehicle.LicensePlate,
                            ServiceTypeName = schedule.ServiceTypeName
                        };

                        session.Attach(alert);
                        alertsCreated++;
                    }
                }
            }
        }

        if (alertsCreated > 0)
        {
            await session.SubmitChanges("TriggerMaintenanceAlerts");
        }

        return alertsCreated;
    }

    /// <summary>
    /// Gets active alerts for a specific shop.
    /// </summary>
    public async Task<LoadOperation<MaintenanceAlert>> GetActiveAlertsAsync(int shopId, int page = 1, int size = 40)
    {
        // Get vehicle IDs for the shop using SQL DISTINCT
        var vehicleIds = await this.Context.GetDistinctAsync<Vehicle, int>(
            v => v.CurrentShopId == shopId,
            v => v.VehicleId);

        if (vehicleIds.Count == 0)
            return new LoadOperation<MaintenanceAlert> { ItemCollection = [], TotalRows = 0 };

        var query = this.Context.CreateQuery<MaintenanceAlert>()
            .Where(ma => vehicleIds.IsInList(ma.VehicleId) && !ma.IsRead && ma.ResolvedDate == null)
            .OrderByDescending(ma => ma.Status == MaintenanceStatus.Overdue ? 1 : 0)
            .ThenBy(ma => ma.TriggerDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Marks an alert as read.
    /// </summary>
    public async Task<SubmitOperation> MarkAsReadAsync(int alertId, string username)
    {
        var alert = await this.Context.LoadOneAsync<MaintenanceAlert>(ma => ma.MaintenanceAlertId == alertId);
        if (alert == null) return SubmitOperation.CreateFailure("Alert not found");

        using var session = this.Context.OpenSession(username);
        alert.IsRead = true;
        session.Attach(alert);
        return await session.SubmitChanges("MarkAlertAsRead");
    }

    /// <summary>
    /// Resolves an alert.
    /// </summary>
    public async Task<SubmitOperation> ResolveAlertAsync(int alertId, string notes, string username)
    {
        var alert = await this.Context.LoadOneAsync<MaintenanceAlert>(ma => ma.MaintenanceAlertId == alertId);
        if (alert == null) return SubmitOperation.CreateFailure("Alert not found");

        using var session = this.Context.OpenSession(username);
        alert.IsRead = true;
        alert.ResolvedDate = DateTimeOffset.UtcNow;
        alert.ResolvedBy = username;
        alert.Notes = notes;
        session.Attach(alert);
        return await session.SubmitChanges("ResolveAlert");
    }
}
