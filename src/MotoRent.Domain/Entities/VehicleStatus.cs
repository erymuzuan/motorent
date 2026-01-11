namespace MotoRent.Domain.Entities;

/// <summary>
/// Status of a vehicle in the rental fleet.
/// </summary>
public enum VehicleStatus
{
    /// <summary>
    /// Ready for rental - vehicle is in good condition and can be rented.
    /// </summary>
    Available,

    /// <summary>
    /// Currently rented out - actively being used by a renter.
    /// </summary>
    Rented,

    /// <summary>
    /// Under repair/service - not available for rental.
    /// </summary>
    Maintenance,

    /// <summary>
    /// Reserved for an upcoming rental - not yet picked up.
    /// </summary>
    Reserved,

    /// <summary>
    /// No longer in active fleet - retired from service.
    /// </summary>
    Retired
}
