namespace MotoRent.Domain.Core;

/// <summary>
/// Defines the sales funnel stages for tracking leads.
/// </summary>
public enum LeadStatus
{
    /// <summary>
    /// Initial contact - prospect has shown interest.
    /// </summary>
    Lead = 0,

    /// <summary>
    /// Prospect is actively trialing the product.
    /// </summary>
    Trial = 1,

    /// <summary>
    /// Converted to paying customer with an organization.
    /// </summary>
    Customer = 2,

    /// <summary>
    /// Lead was lost - did not convert.
    /// </summary>
    Lost = 3
}
