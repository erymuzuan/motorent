namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a pool of shops that share vehicle inventory.
/// Vehicles assigned to a pool can be rented from any pool shop
/// and returned to any pool shop.
/// </summary>
public class VehiclePool : Entity
{
    public int VehiclePoolId { get; set; }

    /// <summary>
    /// Display name for the pool (e.g., "West Coast Pool", "Phuket Network").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description explaining the pool's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this pool is active and accepting rentals.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of ShopIds that participate in this pool.
    /// </summary>
    public List<int> ShopIds { get; set; } = [];

    /// <summary>
    /// Optional: Designated "home" shop for the pool (for reporting).
    /// </summary>
    public int? PrimaryShopId { get; set; }

    public override int GetId() => this.VehiclePoolId;
    public override void SetId(int value) => this.VehiclePoolId = value;
}
