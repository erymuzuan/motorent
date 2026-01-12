namespace MotoRent.Domain.Entities;

/// <summary>
/// Severity level of an accident.
/// </summary>
public enum AccidentSeverity
{
    /// <summary>
    /// Scratches, scuffs - cosmetic only.
    /// </summary>
    Minor,

    /// <summary>
    /// Dents, mechanical damage - repairable.
    /// </summary>
    Moderate,

    /// <summary>
    /// Significant damage - expensive repair or totaled.
    /// </summary>
    Major,

    /// <summary>
    /// Personal injury to any party.
    /// </summary>
    Injury,

    /// <summary>
    /// Requires hospital treatment.
    /// </summary>
    Hospitalization,

    /// <summary>
    /// Death of any party.
    /// </summary>
    Fatality
}
