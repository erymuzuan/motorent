namespace MotoRent.Domain.Entities;

/// <summary>
/// Car segment classification (only applicable when VehicleType == Car).
/// </summary>
public enum CarSegment
{
    /// <summary>
    /// Honda City, Toyota Vios, Nissan Almera
    /// </summary>
    SmallSedan,

    /// <summary>
    /// Honda Accord, Toyota Camry, Mazda 6
    /// </summary>
    BigSedan,

    /// <summary>
    /// Honda CR-V, Toyota Fortuner, Mazda CX-5
    /// </summary>
    SUV,

    /// <summary>
    /// Toyota Hilux, Isuzu D-Max, Ford Ranger
    /// </summary>
    Pickup,

    /// <summary>
    /// Honda Jazz, Toyota Yaris, Mazda 2
    /// </summary>
    Hatchback,

    /// <summary>
    /// Toyota Innova, Kia Carnival
    /// </summary>
    Minivan,

    /// <summary>
    /// BMW, Mercedes-Benz, Lexus
    /// </summary>
    Luxury
}
