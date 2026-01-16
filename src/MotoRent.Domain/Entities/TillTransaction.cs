namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a single transaction in a till session.
/// Tracks cash in, cash out, and non-cash payments for reconciliation.
/// </summary>
public class TillTransaction : Entity
{
    public int TillTransactionId { get; set; }
    public int TillSessionId { get; set; }

    public TillTransactionType TransactionType { get; set; }
    public TillTransactionDirection Direction { get; set; }

    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Description { get; set; }

    // References to related entities
    public int? PaymentId { get; set; }
    public int? DepositId { get; set; }
    public int? RentalId { get; set; }

    // Payout details
    public string? RecipientName { get; set; }
    public string? ReceiptNumber { get; set; }

    // Audit
    public DateTimeOffset TransactionTime { get; set; }
    public string RecordedByUserName { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Verification
    public bool IsVerified { get; set; }
    public string? VerifiedByUserName { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// Whether this transaction affects the cash balance.
    /// Card, BankTransfer, and PromptPay are tracked but don't affect cash.
    /// </summary>
    public bool AffectsCash => TransactionType switch
    {
        TillTransactionType.CardPayment => false,
        TillTransactionType.BankTransfer => false,
        TillTransactionType.PromptPay => false,
        _ => true
    };

    public override int GetId() => TillTransactionId;
    public override void SetId(int value) => TillTransactionId = value;
}
