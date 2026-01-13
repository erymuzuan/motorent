using MotoRent.Domain.Spatial;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Type of service location for pick-up/drop-off.
/// </summary>
public enum ServiceLocationType
{
    /// <summary>
    /// At the shop itself (default, typically no extra fee).
    /// </summary>
    Shop,

    /// <summary>
    /// Airport terminal (e.g., Phuket International Airport).
    /// </summary>
    Airport,

    /// <summary>
    /// Hotel or resort.
    /// </summary>
    Hotel,

    /// <summary>
    /// Ferry terminal or pier (e.g., Rassada Pier for Phi Phi).
    /// </summary>
    FerryTerminal,

    /// <summary>
    /// Bus station.
    /// </summary>
    BusStation,

    /// <summary>
    /// Custom location specified by the operator.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a predefined pick-up/drop-off location with associated fees.
/// Operators can define locations like airports, popular hotels, and ferry terminals.
/// </summary>
public class ServiceLocation : Entity
{
    public int ServiceLocationId { get; set; }

    /// <summary>
    /// The shop this location belongs to.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// Display name (e.g., "Phuket International Airport", "Patong Beach Area").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of location for categorization and display.
    /// </summary>
    public ServiceLocationType LocationType { get; set; } = ServiceLocationType.Custom;

    /// <summary>
    /// Full address or description of the location.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Fee charged for vehicle pickup at this location (THB).
    /// </summary>
    public decimal PickupFee { get; set; }

    /// <summary>
    /// Fee charged for vehicle drop-off at this location (THB).
    /// </summary>
    public decimal DropoffFee { get; set; }

    /// <summary>
    /// Estimated travel time from the shop in minutes.
    /// Helps staff plan logistics.
    /// </summary>
    public int? EstimatedTravelMinutes { get; set; }

    /// <summary>
    /// Internal notes for staff (e.g., "Meet at arrivals gate 3", "Hotel lobby only").
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// GPS coordinates for the pickup/dropoff point for map display and directions.
    /// </summary>
    public LatLng? GpsLocation { get; set; }

    /// <summary>
    /// Whether pickup is available at this location.
    /// </summary>
    public bool PickupAvailable { get; set; } = true;

    /// <summary>
    /// Whether drop-off is available at this location.
    /// </summary>
    public bool DropoffAvailable { get; set; } = true;

    /// <summary>
    /// Whether this location is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in selection lists. Lower numbers appear first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public override int GetId() => this.ServiceLocationId;
    public override void SetId(int value) => this.ServiceLocationId = value;
}
