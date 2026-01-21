namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Represents a single damage marker placed on a 3D vehicle model during inspection.
/// </summary>
public class DamageMarker
{
    /// <summary>
    /// Unique identifier for the marker.
    /// </summary>
    public Guid MarkerId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 3D position on the vehicle model surface.
    /// </summary>
    public MarkerPosition Position { get; set; } = new();

    /// <summary>
    /// 2D screen position for the overlay display (calculated dynamically).
    /// </summary>
    public ScreenPosition ScreenPosition { get; set; } = new();

    /// <summary>
    /// Type of damage: Scratch, Dent, Crack, Scuff, MissingPart, Paint, Rust, Mechanical.
    /// </summary>
    public string DamageType { get; set; } = "Scratch";

    /// <summary>
    /// Severity level: Minor, Moderate, Major.
    /// </summary>
    public string Severity { get; set; } = "Minor";

    /// <summary>
    /// Human-readable description of the location (e.g., "Front left fairing").
    /// </summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Detailed description of the damage.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Estimated repair cost in THB.
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Store IDs for photos associated with this damage marker.
    /// </summary>
    public List<string> PhotoStoreIds { get; set; } = [];

    /// <summary>
    /// Indicates if this damage existed before the current rental (pre-existing).
    /// </summary>
    public bool IsPreExisting { get; set; }

    /// <summary>
    /// Link to a formal DamageReport entity if one was created.
    /// </summary>
    public int? DamageReportId { get; set; }

    /// <summary>
    /// Timestamp when the marker was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Username who created the marker.
    /// </summary>
    public string? CreatedBy { get; set; }
}
