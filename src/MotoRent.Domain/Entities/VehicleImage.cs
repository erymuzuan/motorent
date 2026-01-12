using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents an image associated with a vehicle.
/// Supports up to 5 images per vehicle with one designated as primary.
/// </summary>
public class VehicleImage : Entity
{
    /// <summary>
    /// Primary key for the vehicle image.
    /// </summary>
    public int VehicleImageId { get; set; }

    /// <summary>
    /// Foreign key to the parent Vehicle.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// BinaryStore ID for the uploaded image file.
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the primary/featured image for the vehicle.
    /// Only one image per vehicle should be primary.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Display order for sorting images (1-based).
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Optional caption or description for the image.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Timestamp when the image was uploaded.
    /// </summary>
    public DateTimeOffset UploadedOn { get; set; }

    public override int GetId() => this.VehicleImageId;
    public override void SetId(int value) => this.VehicleImageId = value;
}
