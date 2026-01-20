using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Models;

/// <summary>
/// Result returned when a booking or rental is selected from the transaction search dialog.
/// </summary>
public class TransactionSearchResult
{
    /// <summary>
    /// Type of entity selected.
    /// </summary>
    public TransactionEntityType EntityType { get; set; }

    /// <summary>
    /// The selected booking (if EntityType is Booking).
    /// </summary>
    public Booking? Booking { get; set; }

    /// <summary>
    /// The selected rental (if EntityType is Rental).
    /// </summary>
    public Rental? Rental { get; set; }

    /// <summary>
    /// Auto-detected transaction type based on entity status.
    /// </summary>
    public TillTransactionType TransactionType { get; set; }

    /// <summary>
    /// Line items confirmed for this transaction.
    /// Populated when user proceeds to payment from item confirmation.
    /// </summary>
    public List<TransactionLineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Calculated grand total after discounts and deductions.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Payment entries collected during the transaction.
    /// Populated when payment is complete.
    /// </summary>
    public List<ReceiptPayment> Payments { get; set; } = [];

    /// <summary>
    /// Change amount in THB (if overpaid).
    /// Calculated as sum of payments minus grand total.
    /// </summary>
    public decimal Change { get; set; }
}

/// <summary>
/// Type of entity selected in transaction search.
/// </summary>
public enum TransactionEntityType
{
    /// <summary>
    /// A booking was selected (for deposit or check-in).
    /// </summary>
    Booking,

    /// <summary>
    /// A rental was selected (for check-out).
    /// </summary>
    Rental
}
