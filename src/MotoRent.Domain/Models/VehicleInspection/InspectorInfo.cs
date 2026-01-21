namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Represents information about the person who performed an inspection.
/// </summary>
public class InspectorInfo
{
    /// <summary>
    /// Username of the inspector.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Display name of the inspector.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Role of the inspector (Staff, Manager, etc.).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Shop ID where the inspection was performed.
    /// </summary>
    public int? ShopId { get; set; }

    /// <summary>
    /// Shop name where the inspection was performed.
    /// </summary>
    public string? ShopName { get; set; }
}
