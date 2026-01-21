namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Severity level of damage.
/// </summary>
public enum DamageSeverity
{
    /// <summary>
    /// Minor damage - cosmetic only, no repair needed.
    /// </summary>
    Minor,

    /// <summary>
    /// Moderate damage - should be repaired but vehicle is usable.
    /// </summary>
    Moderate,

    /// <summary>
    /// Major damage - requires repair before rental.
    /// </summary>
    Major
}
