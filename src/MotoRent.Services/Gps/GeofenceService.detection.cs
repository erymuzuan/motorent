using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Breach detection operations.
/// </summary>
public partial class GeofenceService
{
    /// <summary>
    /// Check position against all active geofences for a shop.
    /// Returns any breaches detected.
    /// </summary>
    public async Task<List<GeofenceBreach>> CheckPositionAsync(
        int shopId,
        int vehicleId,
        double latitude,
        double longitude,
        GpsPosition? previousPosition = null)
    {
        var geofences = await this.GetGeofencesAsync(shopId);
        var breaches = new List<GeofenceBreach>();

        foreach (var geofence in geofences)
        {
            var isInside = this.IsInsideGeofence(geofence, latitude, longitude);
            var wasInside = previousPosition is not null &&
                           this.IsInsideGeofence(geofence, previousPosition.Latitude, previousPosition.Longitude);

            // Detect entry
            if (isInside && !wasInside && geofence.AlertOnEnter)
            {
                breaches.Add(new GeofenceBreach
                {
                    Geofence = geofence,
                    AlertType = GeofenceAlertType.Enter,
                    Priority = geofence.AlertPriority
                });
            }

            // Detect exit
            if (!isInside && wasInside && geofence.AlertOnExit)
            {
                breaches.Add(new GeofenceBreach
                {
                    Geofence = geofence,
                    AlertType = GeofenceAlertType.Exit,
                    Priority = geofence.AlertPriority
                });
            }
        }

        return breaches;
    }

    /// <summary>
    /// Process a new GPS position for geofence violations.
    /// Creates alerts if breaches are detected.
    /// </summary>
    public async Task<List<GeofenceAlert>> ProcessPositionAsync(
        int shopId,
        int vehicleId,
        GpsPosition newPosition,
        GpsPosition? previousPosition,
        string username)
    {
        var breaches = await this.CheckPositionAsync(
            shopId, vehicleId,
            newPosition.Latitude, newPosition.Longitude,
            previousPosition);

        if (breaches.Count == 0)
            return [];

        var alerts = new List<GeofenceAlert>();

        // Get vehicle and rental info for denormalization
        var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);
        var activeRental = await this.GetActiveRentalForVehicleAsync(vehicleId);
        Renter? renter = null;

        if (activeRental?.RenterId > 0)
        {
            renter = await this.Context.LoadOneAsync<Renter>(r => r.RenterId == activeRental.RenterId);
        }

        foreach (var breach in breaches)
        {
            var alert = new GeofenceAlert
            {
                GeofenceId = breach.Geofence.GeofenceId,
                VehicleId = vehicleId,
                RentalId = activeRental?.RentalId,
                GpsPositionId = newPosition.GpsPositionId,
                AlertType = breach.AlertType,
                Priority = breach.Priority,
                Status = AlertStatus.Active,
                AlertTimestamp = DateTimeOffset.UtcNow,
                Latitude = newPosition.Latitude,
                Longitude = newPosition.Longitude,
                GeofenceName = breach.Geofence.Name,
                VehicleLicensePlate = vehicle?.LicensePlate,
                VehicleDisplayName = vehicle?.DisplayName,
                RenterName = renter?.FullName,
                RenterPhone = renter?.Phone
            };

            using var session = this.Context.OpenSession(username);
            session.Attach(alert);

            var result = await session.SubmitChanges("CreateGeofenceAlert");
            if (result.Success)
            {
                alerts.Add(alert);
                this.Logger.LogWarning(
                    "Geofence breach: Vehicle {VehicleLicensePlate} {AlertType} '{GeofenceName}' at ({Lat}, {Lng})",
                    alert.VehicleLicensePlate, breach.AlertType, breach.Geofence.Name,
                    newPosition.Latitude, newPosition.Longitude);
            }
        }

        return alerts;
    }

    private async Task<Rental?> GetActiveRentalForVehicleAsync(int vehicleId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.VehicleId == vehicleId && r.Status == "Active"),
            1, 1);

        return result.ItemCollection.FirstOrDefault();
    }
}
