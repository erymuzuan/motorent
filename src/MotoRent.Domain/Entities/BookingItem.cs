namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a single vehicle item within a booking.
/// Uses VehicleGroupKey for cross-shop flexibility - any shop with matching vehicle can fulfill.
/// </summary>
public class BookingItem
{
    /// <summary>
    /// Unique identifier for this item within the booking.
    /// </summary>
    public string ItemId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    // Vehicle matching - uses VehicleGroupKey for cross-shop flexibility

    /// <summary>
    /// Vehicle group key for matching (Brand|Model|Year|VehicleType|EngineCC).
    /// Example: "Honda|Click|2024|Motorbike|125"
    /// PRIMARY matching key - used to find available vehicles at any shop.
    /// </summary>
    public string VehicleGroupKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Specific vehicle ID if customer selected one.
    /// May not be honored if customer checks in at different shop.
    /// </summary>
    public int? PreferredVehicleId { get; set; }

    /// <summary>
    /// Customer's color preference (not guaranteed).
    /// Staff will try to assign matching color if available.
    /// </summary>
    public string? PreferredColor { get; set; }

    // Add-ons

    /// <summary>
    /// Selected insurance package ID.
    /// </summary>
    public int? InsuranceId { get; set; }

    /// <summary>
    /// Selected accessory IDs.
    /// </summary>
    public List<int> AccessoryIds { get; set; } = [];

    // Pricing (captured at booking time)

    /// <summary>
    /// Daily rate for vehicle.
    /// </summary>
    public decimal DailyRate { get; set; }

    /// <summary>
    /// Daily rate for insurance.
    /// </summary>
    public decimal InsuranceRate { get; set; }

    /// <summary>
    /// Total accessories cost per day.
    /// </summary>
    public decimal AccessoriesTotal { get; set; }

    /// <summary>
    /// Security deposit required for this vehicle.
    /// </summary>
    public decimal DepositAmount { get; set; }

    /// <summary>
    /// Total for this item (vehicle + insurance + accessories) × days.
    /// </summary>
    public decimal ItemTotal { get; set; }

    // Fulfillment (set at check-in)

    /// <summary>
    /// Actual vehicle assigned at check-in.
    /// May differ from PreferredVehicleId if checked in at different shop.
    /// </summary>
    public int? AssignedVehicleId { get; set; }

    /// <summary>
    /// Rental record created for this item.
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Current status: Pending, CheckedIn, Cancelled.
    /// </summary>
    public string ItemStatus { get; set; } = BookingItemStatus.Pending;

    // Denormalized

    /// <summary>
    /// Display name for the vehicle (e.g., "Honda Click 125cc").
    /// </summary>
    public string? VehicleDisplayName { get; set; }

    /// <summary>
    /// Name of selected insurance package.
    /// </summary>
    public string? InsuranceName { get; set; }

    /// <summary>
    /// License plate of assigned vehicle (set at check-in).
    /// </summary>
    public string? AssignedVehiclePlate { get; set; }

    /// <summary>
    /// Name of assigned vehicle (set at check-in).
    /// </summary>
    public string? AssignedVehicleName { get; set; }

    // Calculated

    /// <summary>
    /// Whether this item has been checked in.
    /// </summary>
    public bool IsCheckedIn => ItemStatus == BookingItemStatus.CheckedIn;

    /// <summary>
    /// Whether this item is still pending.
    /// </summary>
    public bool IsPending => ItemStatus == BookingItemStatus.Pending;

    /// <summary>
    /// Whether this item is cancelled.
    /// </summary>
    public bool IsCancelled => ItemStatus == BookingItemStatus.Cancelled;
}
