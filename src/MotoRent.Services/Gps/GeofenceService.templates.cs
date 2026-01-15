using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Spatial;

namespace MotoRent.Services.Gps;

/// <summary>
/// Province template operations.
/// </summary>
public partial class GeofenceService
{
    /// <summary>
    /// Create Thai province template geofences.
    /// Called during initial setup.
    /// </summary>
    public async Task<int> SeedProvinceTemplatesAsync(string username)
    {
        var existingTemplates = await this.GetProvinceTemplatesAsync();
        var existingCodes = existingTemplates.Select(t => t.ProvinceCode).ToHashSet();

        var templates = GetThaiProvinceTemplates();
        var createdCount = 0;

        foreach (var template in templates)
        {
            if (existingCodes.Contains(template.ProvinceCode))
                continue;

            var result = await this.CreateGeofenceAsync(template, username);
            if (result.Success)
            {
                createdCount++;
            }
        }

        this.Logger.LogInformation("Created {Count} province template geofences", createdCount);
        return createdCount;
    }

    private static List<Geofence> GetThaiProvinceTemplates()
    {
        // Simplified polygon coordinates for Thai provinces
        // In production, these would be more detailed boundaries
        return
        [
            new Geofence
            {
                Name = "Phuket Province",
                Description = "Phuket island boundary",
                GeofenceType = GeofenceType.Province,
                Shape = GeofenceShape.Polygon,
                ProvinceCode = "PKT",
                AlertPriority = AlertPriority.High,
                AlertOnExit = true,
                SendLineNotification = true,
                IsTemplate = true,
                IsActive = true,
                MapColor = "#F44336",
                Coordinates =
                [
                    new LatLng(8.1650, 98.2500),
                    new LatLng(8.1650, 98.4200),
                    new LatLng(7.7500, 98.4200),
                    new LatLng(7.7500, 98.2500)
                ]
            },
            new Geofence
            {
                Name = "Krabi Province",
                Description = "Krabi province boundary",
                GeofenceType = GeofenceType.Province,
                Shape = GeofenceShape.Polygon,
                ProvinceCode = "KBI",
                AlertPriority = AlertPriority.High,
                AlertOnExit = true,
                SendLineNotification = true,
                IsTemplate = true,
                IsActive = true,
                MapColor = "#2196F3",
                Coordinates =
                [
                    new LatLng(8.5000, 98.6000),
                    new LatLng(8.5000, 99.3000),
                    new LatLng(7.7000, 99.3000),
                    new LatLng(7.7000, 98.6000)
                ]
            },
            new Geofence
            {
                Name = "Koh Samui Area",
                Description = "Koh Samui and surrounding islands",
                GeofenceType = GeofenceType.Province,
                Shape = GeofenceShape.Circle,
                ProvinceCode = "SMI",
                AlertPriority = AlertPriority.High,
                AlertOnExit = true,
                SendLineNotification = true,
                IsTemplate = true,
                IsActive = true,
                MapColor = "#4CAF50",
                CenterLatitude = 9.5120,
                CenterLongitude = 100.0136,
                RadiusMeters = 25000 // 25km radius
            },
            new Geofence
            {
                Name = "Phang Nga Province",
                Description = "Phang Nga province boundary",
                GeofenceType = GeofenceType.Province,
                Shape = GeofenceShape.Polygon,
                ProvinceCode = "PNA",
                AlertPriority = AlertPriority.Medium,
                AlertOnExit = true,
                SendLineNotification = true,
                IsTemplate = true,
                IsActive = true,
                MapColor = "#FF9800",
                Coordinates =
                [
                    new LatLng(9.2000, 98.2000),
                    new LatLng(9.2000, 98.8000),
                    new LatLng(8.0000, 98.8000),
                    new LatLng(8.0000, 98.2000)
                ]
            }
        ];
    }
}
