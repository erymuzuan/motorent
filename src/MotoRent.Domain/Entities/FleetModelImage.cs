namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a marketing/catalog image for a fleet model.
/// Supports up to 5 images per fleet model with one designated as primary.
/// </summary>
public class FleetModelImage : Entity
{
    public int FleetModelImageId { get; set; }

    public int FleetModelId { get; set; }

    public string StoreId { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public string? Caption { get; set; }

    public DateTimeOffset UploadedOn { get; set; }

    public override int GetId() => this.FleetModelImageId;
    public override void SetId(int value) => this.FleetModelImageId = value;
}
