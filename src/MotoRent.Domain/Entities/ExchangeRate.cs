namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents an exchange rate for a foreign currency to THB (base currency).
/// Used for multi-currency cash payments at the till.
/// Organization-scoped with effective dates for rate history.
/// </summary>
public class ExchangeRate : Entity
{
    /// <summary>
    /// Unique identifier for this exchange rate record
    /// </summary>
    public int ExchangeRateId { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, GBP, CNY, JPY, AUD, RUB)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.USD;

    /// <summary>
    /// Buy rate - how much THB we give for 1 unit of foreign currency.
    /// This is the rate used when customers pay with foreign currency.
    /// Example: BuyRate of 34.50 means we give 34.50 THB for 1 USD.
    /// </summary>
    public decimal BuyRate { get; set; }

    /// <summary>
    /// Source of this exchange rate (Manual, API, Adjusted)
    /// </summary>
    public string Source { get; set; } = ExchangeRateSources.Manual;

    /// <summary>
    /// When this rate became effective
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// When this rate expires (null = no expiration, superseded when new rate is set)
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Original rate from API before any manual adjustment.
    /// Null if rate was entered manually or not adjusted.
    /// </summary>
    public decimal? ApiRate { get; set; }

    /// <summary>
    /// Notes about this rate (e.g., "Morning rate", "Adjusted for volatility")
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this rate is currently active.
    /// Old rates are deactivated when new rates are set.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override int GetId() => ExchangeRateId;

    public override void SetId(int value) => ExchangeRateId = value;
}

/// <summary>
/// Constants for exchange rate sources.
/// Tracks how the rate was obtained for audit purposes.
/// </summary>
public static class ExchangeRateSources
{
    /// <summary>
    /// Rate was entered manually by staff
    /// </summary>
    public const string Manual = "Manual";

    /// <summary>
    /// Rate was fetched from external API (Forex POS system)
    /// </summary>
    public const string API = "API";

    /// <summary>
    /// Rate was fetched from API but manually adjusted
    /// </summary>
    public const string Adjusted = "Adjusted";
}
