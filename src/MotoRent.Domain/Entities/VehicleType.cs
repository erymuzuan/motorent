namespace MotoRent.Domain.Entities;

/// <summary>
/// Types of vehicles available for rental.
/// </summary>
public enum VehicleType
{
    /// <summary>
    /// Scooters and motorcycles (Honda Click, PCX, etc.)
    /// </summary>
    Motorbike,

    /// <summary>
    /// Cars - sedans, SUVs, pickups
    /// </summary>
    Car,

    /// <summary>
    /// Personal watercraft for hourly/interval rental
    /// </summary>
    JetSki,

    /// <summary>
    /// Speed boats, tour boats for island hopping
    /// </summary>
    Boat,

    /// <summary>
    /// Passenger vans for group transport
    /// </summary>
    Van
}
