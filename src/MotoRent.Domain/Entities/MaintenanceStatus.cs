namespace MotoRent.Domain.Entities;

/// <summary>
/// Status of a maintenance schedule item
/// </summary>
public enum MaintenanceStatus
{
    /// <summary>
    /// Service is not yet due (green)
    /// </summary>
    Ok,

    /// <summary>
    /// Service is due soon - within warning threshold (yellow)
    /// </summary>
    DueSoon,

    /// <summary>
    /// Service is overdue (red)
    /// </summary>
    Overdue
}
