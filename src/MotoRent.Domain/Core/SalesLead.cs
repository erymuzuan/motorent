using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Represents a sales lead captured from contact forms or other sources.
/// Tracks leads through the sales funnel: Lead -> Trial -> Customer.
/// </summary>
public class SalesLead : Entity
{
    public int SalesLeadId { get; set; }

    /// <summary>
    /// Serial number in format "SL-00001".
    /// </summary>
    public string? No { get; set; }

    #region Contact Information

    /// <summary>
    /// Full name of the contact person.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address for communication.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number (optional).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Company or business name (optional).
    /// </summary>
    public string? Company { get; set; }

    #endregion

    #region Interest Information

    /// <summary>
    /// Subscription plan the lead is interested in.
    /// </summary>
    public SubscriptionPlan? PlanInterested { get; set; }

    /// <summary>
    /// Estimated fleet size (number of vehicles).
    /// </summary>
    public int? FleetSize { get; set; }

    /// <summary>
    /// Message or inquiry from the contact form.
    /// </summary>
    public string? Message { get; set; }

    #endregion

    #region Enterprise Requirements (when Customized plan)

    /// <summary>
    /// Enterprise requirements selected by the lead.
    /// </summary>
    public List<string> EnterpriseRequirements { get; set; } = [];

    /// <summary>
    /// Custom requirements described by the lead (free text).
    /// </summary>
    public string? CustomRequirements { get; set; }

    #endregion

    #region Tracking

    /// <summary>
    /// Current status in the sales funnel.
    /// </summary>
    public LeadStatus Status { get; set; } = LeadStatus.Lead;

    /// <summary>
    /// Source from which the lead was captured.
    /// </summary>
    public LeadSource Source { get; set; } = LeadSource.ContactForm;

    /// <summary>
    /// Timestamp when lead started trial.
    /// </summary>
    public DateTimeOffset? TrialStartedAt { get; set; }

    /// <summary>
    /// Timestamp when lead converted to customer.
    /// </summary>
    public DateTimeOffset? ConvertedAt { get; set; }

    #endregion

    #region Organization Link

    /// <summary>
    /// Organization ID when converted to customer.
    /// </summary>
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Account number of the organization when converted.
    /// </summary>
    public string? AccountNo { get; set; }

    #endregion

    /// <summary>
    /// Internal notes about the lead.
    /// </summary>
    public string? Notes { get; set; }

    public override int GetId() => SalesLeadId;
    public override void SetId(int value) => SalesLeadId = value;
}
