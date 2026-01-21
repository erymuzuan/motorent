using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models.VehicleInspection;

namespace MotoRent.Services;

/// <summary>
/// Service for managing vehicle inspections with 3D damage markers.
/// </summary>
public class VehicleInspectionService(RentalDataContext context, DamageReportService damageReportService)
{
    private RentalDataContext Context { get; } = context;
    private DamageReportService DamageReportService { get; } = damageReportService;

    /// <summary>
    /// Gets a vehicle inspection by ID.
    /// </summary>
    public async Task<VehicleInspection?> GetByIdAsync(int inspectionId)
    {
        return await this.Context.LoadOneAsync<VehicleInspection>(i => i.VehicleInspectionId == inspectionId);
    }

    /// <summary>
    /// Gets all inspections for a vehicle.
    /// </summary>
    public async Task<List<VehicleInspection>> GetByVehicleAsync(int vehicleId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleInspection>().Where(i => i.VehicleId == vehicleId),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.OrderByDescending(i => i.InspectedAt).ToList();
    }

    /// <summary>
    /// Gets all inspections for a rental.
    /// </summary>
    public async Task<List<VehicleInspection>> GetByRentalAsync(int rentalId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleInspection>().Where(i => i.RentalId == rentalId),
            page: 1, size: 10, includeTotalRows: false);

        return result.ItemCollection.OrderBy(i => i.InspectedAt).ToList();
    }

    /// <summary>
    /// Gets the pre-rental (check-in) inspection for a rental.
    /// </summary>
    public async Task<VehicleInspection?> GetPreRentalInspectionAsync(int rentalId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleInspection>()
                .Where(i => i.RentalId == rentalId && i.InspectionType == "PreRental"),
            page: 1, size: 1, includeTotalRows: false);

        return result.ItemCollection.FirstOrDefault();
    }

    /// <summary>
    /// Gets the post-rental (check-out) inspection for a rental.
    /// </summary>
    public async Task<VehicleInspection?> GetPostRentalInspectionAsync(int rentalId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleInspection>()
                .Where(i => i.RentalId == rentalId && i.InspectionType == "PostRental"),
            page: 1, size: 1, includeTotalRows: false);

        return result.ItemCollection.FirstOrDefault();
    }

    /// <summary>
    /// Gets the latest inspection for a vehicle.
    /// </summary>
    public async Task<VehicleInspection?> GetLatestAsync(int vehicleId)
    {
        var inspections = await this.GetByVehicleAsync(vehicleId);
        return inspections.FirstOrDefault();
    }

    /// <summary>
    /// Creates a new vehicle inspection.
    /// </summary>
    public async Task<SubmitOperation> CreateAsync(VehicleInspection inspection, string username)
    {
        using var session = this.Context.OpenSession(username);

        inspection.InspectedAt = DateTimeOffset.Now;

        // Set inspector info if not provided
        if (string.IsNullOrEmpty(inspection.Inspector?.UserName))
        {
            inspection.Inspector ??= new InspectorInfo();
            inspection.Inspector.UserName = username;
        }

        session.Attach(inspection);

        return await session.SubmitChanges("CreateVehicleInspection");
    }

    /// <summary>
    /// Updates an existing vehicle inspection.
    /// </summary>
    public async Task<SubmitOperation> UpdateAsync(VehicleInspection inspection, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(inspection);
        return await session.SubmitChanges("UpdateVehicleInspection");
    }

    /// <summary>
    /// Deletes a vehicle inspection.
    /// </summary>
    public async Task<SubmitOperation> DeleteAsync(int inspectionId, string username)
    {
        var inspection = await this.GetByIdAsync(inspectionId);
        if (inspection is null)
            return SubmitOperation.CreateFailure("Inspection not found");

        using var session = this.Context.OpenSession(username);
        session.Delete(inspection);
        return await session.SubmitChanges("DeleteVehicleInspection");
    }

    /// <summary>
    /// Creates a pre-rental (check-in) inspection.
    /// </summary>
    public async Task<SubmitOperation> CreatePreRentalInspectionAsync(
        int rentalId,
        int vehicleId,
        List<DamageMarker> markers,
        string overallCondition,
        int? odometerReading,
        int? fuelLevel,
        string? notes,
        CameraState? cameraState,
        string username)
    {
        // Mark all markers as pre-existing since this is check-in
        foreach (var marker in markers)
        {
            marker.IsPreExisting = true;
            marker.CreatedBy = username;
            marker.CreatedAt = DateTimeOffset.Now;
        }

        var inspection = new VehicleInspection
        {
            VehicleId = vehicleId,
            RentalId = rentalId,
            InspectionType = "PreRental",
            Inspector = new InspectorInfo { UserName = username },
            Markers = markers,
            OverallCondition = overallCondition,
            OdometerReading = odometerReading,
            FuelLevel = fuelLevel,
            Notes = notes,
            SavedCameraState = cameraState,
            ModelPath = this.GetModelPathForVehicle(vehicleId)
        };

        return await this.CreateAsync(inspection, username);
    }

    /// <summary>
    /// Creates a post-rental (check-out) inspection.
    /// </summary>
    public async Task<SubmitOperation> CreatePostRentalInspectionAsync(
        int rentalId,
        int vehicleId,
        List<DamageMarker> markers,
        string overallCondition,
        int? odometerReading,
        int? fuelLevel,
        string? notes,
        CameraState? cameraState,
        string username,
        bool createDamageReportsForNewDamage = true)
    {
        // Get pre-rental inspection to determine which markers are new
        var preRentalInspection = await this.GetPreRentalInspectionAsync(rentalId);

        foreach (var marker in markers)
        {
            // Check if this position existed in pre-rental
            if (preRentalInspection is not null && IsPreExistingPosition(marker, preRentalInspection.Markers))
            {
                marker.IsPreExisting = true;
            }

            marker.CreatedBy ??= username;
            if (marker.CreatedAt == default)
                marker.CreatedAt = DateTimeOffset.Now;
        }

        var inspection = new VehicleInspection
        {
            VehicleId = vehicleId,
            RentalId = rentalId,
            InspectionType = "PostRental",
            Inspector = new InspectorInfo { UserName = username },
            Markers = markers,
            OverallCondition = overallCondition,
            OdometerReading = odometerReading,
            FuelLevel = fuelLevel,
            Notes = notes,
            SavedCameraState = cameraState,
            ModelPath = this.GetModelPathForVehicle(vehicleId),
            PreviousInspectionId = preRentalInspection?.VehicleInspectionId
        };

        var result = await this.CreateAsync(inspection, username);

        // Create damage reports for new damage
        if (result.Success && createDamageReportsForNewDamage)
        {
            var newDamageMarkers = markers.Where(m => !m.IsPreExisting).ToList();
            foreach (var marker in newDamageMarkers)
            {
                await this.CreateDamageReportFromMarkerAsync(
                    rentalId, vehicleId, marker, inspection.VehicleInspectionId, username);
            }
        }

        return result;
    }

    private async Task<SubmitOperation> CreateDamageReportFromMarkerAsync(
        int rentalId,
        int vehicleId,
        DamageMarker marker,
        int inspectionId,
        string username)
    {
        using var session = this.Context.OpenSession(username);

        var damageReport = new DamageReport
        {
            RentalId = rentalId,
            MotorbikeId = vehicleId, // Using MotorbikeId for backwards compatibility
            Description = $"{marker.DamageType}: {marker.LocationDescription ?? "Unknown location"}. {marker.Description}".Trim(),
            Severity = marker.Severity,
            EstimatedCost = marker.EstimatedCost ?? 0,
            Status = "Pending",
            ReportedOn = DateTimeOffset.Now
        };

        session.Attach(damageReport);
        var result = await session.SubmitChanges("CreateDamageReportFromInspection");

        // Update the marker with the damage report ID
        if (result.Success)
        {
            marker.DamageReportId = damageReport.DamageReportId;
        }

        return result;
    }

    /// <summary>
    /// Creates a maintenance inspection.
    /// </summary>
    public async Task<SubmitOperation> CreateMaintenanceInspectionAsync(
        int vehicleId,
        int maintenanceRecordId,
        List<DamageMarker> markers,
        string overallCondition,
        int? odometerReading,
        string? notes,
        string username)
    {
        var inspection = new VehicleInspection
        {
            VehicleId = vehicleId,
            MaintenanceRecordId = maintenanceRecordId,
            InspectionType = "Maintenance",
            Inspector = new InspectorInfo { UserName = username },
            Markers = markers,
            OverallCondition = overallCondition,
            OdometerReading = odometerReading,
            Notes = notes,
            ModelPath = this.GetModelPathForVehicle(vehicleId)
        };

        return await this.CreateAsync(inspection, username);
    }

    /// <summary>
    /// Creates an accident inspection.
    /// </summary>
    public async Task<SubmitOperation> CreateAccidentInspectionAsync(
        int vehicleId,
        int accidentId,
        List<DamageMarker> markers,
        string overallCondition,
        string? notes,
        CameraState? cameraState,
        string username)
    {
        var inspection = new VehicleInspection
        {
            VehicleId = vehicleId,
            AccidentId = accidentId,
            InspectionType = "Accident",
            Inspector = new InspectorInfo { UserName = username },
            Markers = markers,
            OverallCondition = overallCondition,
            Notes = notes,
            SavedCameraState = cameraState,
            ModelPath = this.GetModelPathForVehicle(vehicleId)
        };

        return await this.CreateAsync(inspection, username);
    }

    /// <summary>
    /// Gets the damage comparison between two inspections.
    /// </summary>
    public InspectionComparison CompareInspections(VehicleInspection? before, VehicleInspection? after)
    {
        var comparison = new InspectionComparison
        {
            BeforeInspection = before,
            AfterInspection = after
        };

        if (after is null)
            return comparison;

        foreach (var marker in after.Markers)
        {
            if (before is null || !IsPreExistingPosition(marker, before.Markers))
            {
                comparison.NewDamage.Add(marker);
            }
            else
            {
                comparison.PreExistingDamage.Add(marker);
            }
        }

        comparison.TotalNewDamageCost = comparison.NewDamage
            .Where(m => m.EstimatedCost.HasValue)
            .Sum(m => m.EstimatedCost!.Value);

        return comparison;
    }

    /// <summary>
    /// Gets inspection history summary for a vehicle.
    /// </summary>
    public async Task<VehicleInspectionSummary> GetVehicleSummaryAsync(int vehicleId)
    {
        var inspections = await this.GetByVehicleAsync(vehicleId);
        var latest = inspections.FirstOrDefault();

        return new VehicleInspectionSummary
        {
            VehicleId = vehicleId,
            TotalInspections = inspections.Count,
            LatestInspection = latest,
            LatestCondition = latest?.OverallCondition ?? "Unknown",
            TotalDamageMarkers = latest?.Markers.Count ?? 0,
            LastInspectedAt = latest?.InspectedAt
        };
    }

    private static bool IsPreExistingPosition(DamageMarker marker, List<DamageMarker> preExistingMarkers)
    {
        const double tolerance = 0.15; // 15cm tolerance for position matching

        return preExistingMarkers.Any(pre =>
            Math.Abs(pre.Position.X - marker.Position.X) < tolerance &&
            Math.Abs(pre.Position.Y - marker.Position.Y) < tolerance &&
            Math.Abs(pre.Position.Z - marker.Position.Z) < tolerance);
    }

    private string GetModelPathForVehicle(int vehicleId)
    {
        // TODO: Get actual vehicle type and return appropriate model
        // For now, return default scooter model
        return "models/vehicles/scooter-generic.glb";
    }

    /// <summary>
    /// Gets the appropriate 3D model path based on vehicle type.
    /// </summary>
    public static string GetModelPath(VehicleType vehicleType, int? engineSize = null) => vehicleType switch
    {
        VehicleType.Motorbike when engineSize < 125 => "models/vehicles/scooter-generic.glb",
        VehicleType.Motorbike => "models/vehicles/motorbike-150cc.glb",
        VehicleType.Car => "models/vehicles/car-sedan.glb",
        VehicleType.Van => "models/vehicles/van-generic.glb",
        VehicleType.JetSki => "models/vehicles/jetski-generic.glb",
        VehicleType.Boat => "models/vehicles/boat-generic.glb",
        _ => "models/vehicles/scooter-generic.glb"
    };
}

/// <summary>
/// Comparison result between two inspections.
/// </summary>
public class InspectionComparison
{
    public VehicleInspection? BeforeInspection { get; set; }
    public VehicleInspection? AfterInspection { get; set; }
    public List<DamageMarker> NewDamage { get; set; } = [];
    public List<DamageMarker> PreExistingDamage { get; set; } = [];
    public decimal TotalNewDamageCost { get; set; }

    public bool HasNewDamage => this.NewDamage.Count > 0;
}

/// <summary>
/// Summary of vehicle inspection history.
/// </summary>
public class VehicleInspectionSummary
{
    public int VehicleId { get; set; }
    public int TotalInspections { get; set; }
    public VehicleInspection? LatestInspection { get; set; }
    public string LatestCondition { get; set; } = "Unknown";
    public int TotalDamageMarkers { get; set; }
    public DateTimeOffset? LastInspectedAt { get; set; }
}
