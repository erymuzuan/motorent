namespace MotoRent.Domain.Models;

/// <summary>
/// Captures inspection details for pre-rental or post-rental vehicle inspections.
/// Supports both system users (staff) and external inspectors (company representatives).
/// </summary>
public class InspectionInfo
{
    /// <summary>
    /// True if inspector is a system User (shop staff), false if external representative.
    /// </summary>
    public bool IsSystemUser { get; set; }

    /// <summary>
    /// User ID if inspector is a system user. Null for external inspectors.
    /// </summary>
    public int? InspectorUserId { get; set; }

    /// <summary>
    /// Inspector's full name (either from User or entered for external).
    /// </summary>
    public string InspectorName { get; set; } = "";

    /// <summary>
    /// Company or organization affiliation (e.g., "ABC Hotel", "Thai Insurance Co.").
    /// Typically used for external inspectors.
    /// </summary>
    public string? CompanyAffiliation { get; set; }

    /// <summary>
    /// Timestamp when inspection was performed.
    /// </summary>
    public DateTimeOffset InspectedAt { get; set; }

    /// <summary>
    /// Optional notes about the inspection (condition remarks, special observations).
    /// </summary>
    public string? Notes { get; set; }
}
