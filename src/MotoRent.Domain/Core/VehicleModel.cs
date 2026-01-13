using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Represents a vehicle make/model in the global lookup database.
/// Stored in [Core] schema - shared across all tenants.
/// Used for quick vehicle registration with pre-populated data.
/// </summary>
public class VehicleModel : Entity
{
    public int VehicleModelId { get; set; }

    /// <summary>
    /// Brand/manufacturer name (e.g., "Honda", "Toyota", "Yamaha").
    /// </summary>
    public string Make { get; set; } = string.Empty;

    /// <summary>
    /// Model name (e.g., "Click 125", "PCX 160", "Camry").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle type classification.
    /// </summary>
    public VehicleType VehicleType { get; set; }

    /// <summary>
    /// Car segment (only applicable when VehicleType == Car).
    /// </summary>
    public CarSegment? Segment { get; set; }

    /// <summary>
    /// Engine displacement in CC (for motorbikes).
    /// </summary>
    public int? EngineCC { get; set; }

    /// <summary>
    /// Engine size in liters (for cars).
    /// </summary>
    public decimal? EngineLiters { get; set; }

    /// <summary>
    /// Seat count (for cars/vans).
    /// </summary>
    public int? SeatCount { get; set; }

    /// <summary>
    /// Year range when this model was produced (start).
    /// </summary>
    public int? YearFrom { get; set; }

    /// <summary>
    /// Year range when this model was produced (end, null = still in production).
    /// </summary>
    public int? YearTo { get; set; }

    /// <summary>
    /// Whether this model is active and visible in lookups.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting (lower = higher priority).
    /// Popular Thai market models should have lower values.
    /// </summary>
    public int DisplayOrder { get; set; } = 100;

    /// <summary>
    /// Alternative names/spellings for Gemini matching.
    /// E.g., ["PCX", "PCX160", "PCX 160cc"]
    /// </summary>
    public string[] Aliases { get; set; } = [];

    /// <summary>
    /// Suggested daily rental rate (for pricing hints).
    /// </summary>
    public decimal? SuggestedDailyRate { get; set; }

    /// <summary>
    /// Suggested deposit amount.
    /// </summary>
    public decimal? SuggestedDeposit { get; set; }

    /// <summary>
    /// Reference image store ID (for visual recognition and display).
    /// </summary>
    public string? ImageStoreId { get; set; }

    /// <summary>
    /// Country of origin (e.g., "Japan", "Thailand").
    /// </summary>
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// Computed display name (Make + Model).
    /// </summary>
    public string DisplayName => $"{Make} {Model}";

    public override int GetId() => VehicleModelId;
    public override void SetId(int value) => VehicleModelId = value;

    public override string ToString() => DisplayName;
}
