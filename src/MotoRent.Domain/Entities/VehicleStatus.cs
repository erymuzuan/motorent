namespace MotoRent.Domain.Entities;

/// <summary>
/// Status constants for vehicles in the rental fleet.
/// Stored as strings in the database.
/// </summary>
public static class VehicleStatus
{
    /// <summary>
    /// Ready for rental - vehicle is in good condition and can be rented.
    /// </summary>
    public const string Available = "Available";

    /// <summary>
    /// Currently rented out - actively being used by a renter.
    /// </summary>
    public const string Rented = "Rented";

    /// <summary>
    /// Under repair/service - not available for rental.
    /// </summary>
    public const string Maintenance = "Maintenance";

    /// <summary>
    /// Reserved for an upcoming rental - not yet picked up.
    /// </summary>
    public const string Reserved = "Reserved";

    /// <summary>
    /// No longer in active fleet - retired from service.
    /// </summary>
    public const string Retired = "Retired";

    /// <summary>
    /// All valid status values.
    /// </summary>
    public static readonly string[] AllStatuses =
    [
        Available,
        Rented,
        Maintenance,
        Reserved,
        Retired
    ];

    /// <summary>
    /// Statuses where vehicle is rentable.
    /// </summary>
    public static readonly string[] RentableStatuses =
    [
        Available,
        Reserved
    ];
}
