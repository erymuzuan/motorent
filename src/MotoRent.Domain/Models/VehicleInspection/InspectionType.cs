namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Types of vehicle inspections.
/// </summary>
public enum InspectionType
{
    /// <summary>
    /// Inspection before rental (check-in).
    /// </summary>
    PreRental,

    /// <summary>
    /// Inspection after rental (check-out).
    /// </summary>
    PostRental,

    /// <summary>
    /// Inspection during or after maintenance.
    /// </summary>
    Maintenance,

    /// <summary>
    /// Inspection after an accident.
    /// </summary>
    Accident,

    /// <summary>
    /// Routine periodic inspection.
    /// </summary>
    Routine
}
