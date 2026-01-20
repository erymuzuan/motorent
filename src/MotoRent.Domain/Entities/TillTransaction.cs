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

    /// <summary>
    /// Currency code for this transaction (THB, USD, EUR, CNY).
    /// Default is THB to maintain backward compatibility with existing transactions.
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Exchange rate used to convert to THB base currency.
    /// Default is 1.0 for THB transactions (no conversion needed).
    /// For foreign currency, this is THB per 1 unit of foreign currency.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1.0m;

    /// <summary>
    /// Amount converted to THB base currency.
    /// For THB transactions, equals Amount.
    /// For foreign currency, this is Amount * ExchangeRate.
    /// </summary>
    public decimal AmountInBaseCurrency { get; set; }

    /// <summary>
    /// Source of the exchange rate used (Manual, API, Adjusted, or "Base" for THB).
    /// Captured at transaction time for audit trail.
    /// </summary>
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Reference to the ExchangeRate entity used (null for THB base currency).
    /// Enables linking back to the exact rate record for audit purposes.
    /// </summary>
    public int? ExchangeRateId { get; set; }

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
    /// Attachments such as receipts, photos, or other supporting documents
    /// </summary>
    public List<TillAttachment> Attachments { get; set; } = [];

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

/// <summary>
/// Represents an attachment (receipt photo, document) for a till transaction
/// </summary>
public class TillAttachment
{
    /// <summary>
    /// Unique identifier for this attachment within the transaction
    /// </summary>
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// BinaryStore ID for the uploaded file
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Type of attachment (Receipt, Photo, Document)
    /// </summary>
    public string AttachmentType { get; set; } = "Receipt";

    /// <summary>
    /// Optional caption or description
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// When the attachment was uploaded
    /// </summary>
    public DateTimeOffset UploadedOn { get; set; } = DateTimeOffset.Now;
}
