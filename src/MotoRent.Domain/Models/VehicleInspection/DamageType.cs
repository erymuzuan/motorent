namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Types of damage that can be marked on a vehicle.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Surface scratch on paint or plastic.
    /// </summary>
    Scratch,

    /// <summary>
    /// Dent in metal or plastic body panels.
    /// </summary>
    Dent,

    /// <summary>
    /// Crack in plastic, glass, or fiberglass.
    /// </summary>
    Crack,

    /// <summary>
    /// Scuff marks from rubbing contact.
    /// </summary>
    Scuff,

    /// <summary>
    /// Missing part or component.
    /// </summary>
    MissingPart,

    /// <summary>
    /// Paint damage (chip, peel, fade).
    /// </summary>
    Paint,

    /// <summary>
    /// Rust or corrosion.
    /// </summary>
    Rust,

    /// <summary>
    /// Mechanical damage or malfunction.
    /// </summary>
    Mechanical,

    /// <summary>
    /// Broken part or component.
    /// </summary>
    Broken,

    /// <summary>
    /// Wear and tear beyond normal use.
    /// </summary>
    Wear,

    /// <summary>
    /// Other damage type.
    /// </summary>
    Other
}
