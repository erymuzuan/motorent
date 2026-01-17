namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for agent commission status workflow.
/// Commission becomes eligible only after rental is completed.
/// </summary>
public static class AgentCommissionStatus
{
    /// <summary>
    /// Rental completed, commission awaiting approval.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Commission approved for payment.
    /// </summary>
    public const string Approved = "Approved";

    /// <summary>
    /// Commission paid to agent.
    /// </summary>
    public const string Paid = "Paid";

    /// <summary>
    /// Booking cancelled, commission voided (no payment).
    /// </summary>
    public const string Voided = "Voided";

    public static readonly string[] All = [Pending, Approved, Paid, Voided];
}
