namespace MotoRent.Domain.Entities;

/// <summary>
/// Defines a type of maintenance service with default intervals.
/// Scoped per shop (organization).
/// </summary>
public class ServiceType : Entity
{
    public int ServiceTypeId { get; set; }
    public int ShopId { get; set; }

    /// <summary>
    /// Name of the service type (e.g., "Oil Change", "Brake Check")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this service includes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Days between services (e.g., 30 days for oil change)
    /// </summary>
    public int DaysInterval { get; set; }

    /// <summary>
    /// Kilometers between services (e.g., 3000 km for oil change)
    /// </summary>
    public int KmInterval { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this service type is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.ServiceTypeId;
    public override void SetId(int value) => this.ServiceTypeId = value;
}
