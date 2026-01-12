namespace MotoRent.Domain.Entities;

/// <summary>
/// Status of an accident case.
/// </summary>
public enum AccidentStatus
{
    /// <summary>
    /// Initial report filed.
    /// </summary>
    Reported,

    /// <summary>
    /// Under investigation/processing.
    /// </summary>
    InProgress,

    /// <summary>
    /// Fully resolved and closed.
    /// </summary>
    Resolved
}
