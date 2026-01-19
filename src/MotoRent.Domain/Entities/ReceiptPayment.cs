namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a payment on a receipt.
/// Supports split payments and multi-currency cash payments.
/// Embedded in the Receipt entity's Payments collection.
/// </summary>
public class ReceiptPayment
{
    /// <summary>
    /// Unique identifier for this payment within the receipt
    /// </summary>
    public string PaymentId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Payment method (Cash, Card, PromptPay, BankTransfer)
    /// </summary>
    public string Method { get; set; } = PaymentMethods.Cash;

    /// <summary>
    /// Amount in the payment currency
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (THB, USD, EUR, GBP, CNY, JPY, AUD, RUB)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Exchange rate to THB (1.0 for THB, otherwise the conversion rate)
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1.0m;

    /// <summary>
    /// Amount converted to base currency (THB)
    /// </summary>
    public decimal AmountInBaseCurrency { get; set; }

    /// <summary>
    /// Source of the exchange rate used (Manual, API, Adjusted, or "Base" for THB)
    /// Captured at transaction time for RATE-04 audit trail requirement.
    /// </summary>
    public string ExchangeRateSource { get; set; } = "Base";

    /// <summary>
    /// Reference to the ExchangeRate entity used (null for THB base currency)
    /// Enables linking back to the exact rate record for audit purposes.
    /// </summary>
    public int? ExchangeRateId { get; set; }

    /// <summary>
    /// Transaction reference (card authorization code, PromptPay reference, etc.)
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Last four digits of the card (for card payments)
    /// </summary>
    public string? CardLastFour { get; set; }

    /// <summary>
    /// Card type (Visa, Mastercard, etc.) for card payments
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// When the payment was made
    /// </summary>
    public DateTimeOffset PaidAt { get; set; }
}

/// <summary>
/// Payment method constants
/// </summary>
public static class PaymentMethods
{
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string PromptPay = "PromptPay";
    public const string BankTransfer = "BankTransfer";
}

/// <summary>
/// Supported currency codes for multi-currency payments
/// </summary>
public static class SupportedCurrencies
{
    public const string THB = "THB";  // Thai Baht (base currency)
    public const string USD = "USD";  // US Dollar
    public const string EUR = "EUR";  // Euro
    public const string GBP = "GBP";  // British Pound
    public const string CNY = "CNY";  // Chinese Yuan
    public const string JPY = "JPY";  // Japanese Yen
    public const string AUD = "AUD";  // Australian Dollar
    public const string RUB = "RUB";  // Russian Ruble

    public static readonly string[] All = [THB, USD, EUR, GBP, CNY, JPY, AUD, RUB];
}
