namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Overall condition rating for a vehicle inspection.
/// </summary>
public enum OverallCondition
{
    /// <summary>
    /// Excellent condition with no visible damage.
    /// </summary>
    Excellent,

    /// <summary>
    /// Good condition with minor wear.
    /// </summary>
    Good,

    /// <summary>
    /// Fair condition with some visible damage.
    /// </summary>
    Fair,

    /// <summary>
    /// Poor condition requiring attention.
    /// </summary>
    Poor,

    /// <summary>
    /// Damaged condition requiring repair before use.
    /// </summary>
    Damaged
}
