namespace MotoRent.Domain.Entities;

/// <summary>
/// Records a payment made for a booking.
/// Embedded in Booking entity for tracking payment history.
/// </summary>
public class BookingPayment
{
    /// <summary>
    /// Unique identifier for this payment.
    /// </summary>
    public string PaymentId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// When the payment was made.
    /// </summary>
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Payment amount (positive for payment, negative for refund).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method: Cash, Card, PromptPay, BankTransfer.
    /// </summary>
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>
    /// Payment type: Deposit, Partial, Full, Refund.
    /// </summary>
    public string PaymentType { get; set; } = BookingPaymentType.Deposit;

    /// <summary>
    /// Transaction reference (for card/online payments).
    /// </summary>
    public string? TransactionRef { get; set; }

    /// <summary>
    /// Last 4 digits of card (if paid by card).
    /// </summary>
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Notes about the payment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Username of who recorded the payment.
    /// </summary>
    public string? RecordedBy { get; set; }

    /// <summary>
    /// Whether this is a refund.
    /// </summary>
    public bool IsRefund => Amount < 0 || PaymentType == BookingPaymentType.Refund;
}

/// <summary>
/// Booking payment type constants.
/// </summary>
public static class BookingPaymentType
{
    /// <summary>
    /// Security deposit payment.
    /// </summary>
    public const string Deposit = "Deposit";

    /// <summary>
    /// Partial payment towards total.
    /// </summary>
    public const string Partial = "Partial";

    /// <summary>
    /// Full payment of remaining balance.
    /// </summary>
    public const string Full = "Full";

    /// <summary>
    /// Refund payment (negative amount).
    /// </summary>
    public const string Refund = "Refund";
}

/// <summary>
/// Payment method constants.
/// </summary>
public static class PaymentMethod
{
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string PromptPay = "PromptPay";
    public const string BankTransfer = "BankTransfer";

    public static readonly string[] AllMethods =
    [
        Cash,
        Card,
        PromptPay,
        BankTransfer
    ];
}
