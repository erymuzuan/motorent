using MotoRent.Domain.Models.VehicleInspection;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a vehicle inspection with 3D damage markers.
/// Used for check-in, check-out, maintenance, and accident inspections.
/// </summary>
public class VehicleInspection : Entity
{
    public int VehicleInspectionId { get; set; }

    /// <summary>
    /// The vehicle being inspected.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Associated rental (for pre-rental/post-rental inspections).
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Associated maintenance record (for maintenance inspections).
    /// </summary>
    public int? MaintenanceRecordId { get; set; }

    /// <summary>
    /// Associated accident (for accident inspections).
    /// </summary>
    public int? AccidentId { get; set; }

    /// <summary>
    /// Type of inspection: PreRental, PostRental, Maintenance, Accident, Routine.
    /// </summary>
    public string InspectionType { get; set; } = "PreRental";

    /// <summary>
    /// Information about the inspector.
    /// </summary>
    public InspectorInfo Inspector { get; set; } = new();

    /// <summary>
    /// List of damage markers placed on the 3D model.
    /// </summary>
    public List<DamageMarker> Markers { get; set; } = [];

    /// <summary>
    /// Overall condition assessment: Excellent, Good, Fair, Poor, Damaged.
    /// </summary>
    public string OverallCondition { get; set; } = "Good";

    /// <summary>
    /// General notes about the inspection.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when the inspection was performed.
    /// </summary>
    public DateTimeOffset InspectedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Path to the 3D model used for this inspection.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Saved camera state for restoring the view.
    /// </summary>
    public CameraState? SavedCameraState { get; set; }

    /// <summary>
    /// Odometer reading at time of inspection (for motorbikes/cars).
    /// </summary>
    public int? OdometerReading { get; set; }

    /// <summary>
    /// Fuel level percentage at time of inspection.
    /// </summary>
    public int? FuelLevel { get; set; }

    /// <summary>
    /// Reference to the previous inspection for comparison.
    /// </summary>
    public int? PreviousInspectionId { get; set; }

    /// <summary>
    /// Total estimated damage cost from all markers.
    /// </summary>
    public decimal TotalEstimatedCost => this.Markers
        .Where(m => m.EstimatedCost.HasValue)
        .Sum(m => m.EstimatedCost!.Value);

    /// <summary>
    /// Count of new damage markers (not pre-existing).
    /// </summary>
    public int NewDamageCount => this.Markers.Count(m => !m.IsPreExisting);

    /// <summary>
    /// Count of pre-existing damage markers.
    /// </summary>
    public int PreExistingDamageCount => this.Markers.Count(m => m.IsPreExisting);

    public override int GetId() => this.VehicleInspectionId;
    public override void SetId(int value) => this.VehicleInspectionId = value;
}
