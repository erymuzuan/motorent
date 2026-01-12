namespace MotoRent.Domain.Entities;

/// <summary>
/// Status of a payment to a third-party vehicle owner.
/// </summary>
public enum OwnerPaymentStatus
{
    /// <summary>
    /// Payment calculated, awaiting transfer to owner.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment has been completed and transferred to owner.
    /// </summary>
    Paid,

    /// <summary>
    /// Payment has been voided/cancelled.
    /// </summary>
    Cancelled
}
