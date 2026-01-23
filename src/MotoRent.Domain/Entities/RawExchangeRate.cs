namespace MotoRent.Domain.Entities;

/// <summary>
/// DTO representing raw exchange rate data from a provider API.
/// Not persisted - used for transfer between provider and service layers.
/// </summary>
public class RawExchangeRate
{
    /// <summary>
    /// Provider code identifying the source (e.g., "MamyExchange", "SuperRich")
    /// </summary>
    public string Provider { get; set; } = "";

    /// <summary>
    /// Currency code (USD, EUR, GBP, etc.)
    /// </summary>
    public string Currency { get; set; } = "";

    /// <summary>
    /// When the provider last updated this rate
    /// </summary>
    public DateTimeOffset UpdatedOn { get; set; }

    /// <summary>
    /// Buying rate - THB received per 1 unit of foreign currency
    /// </summary>
    public decimal Buying { get; set; }

    /// <summary>
    /// Selling rate - THB charged per 1 unit of foreign currency
    /// </summary>
    public decimal Selling { get; set; }

    /// <summary>
    /// Denominations this rate applies to (e.g., [100, 50] for large USD bills)
    /// </summary>
    public List<decimal> Denominations { get; set; } = [];

    /// <summary>
    /// Optional hint from provider about denomination grouping (e.g., "Large Bills", "Small Bills")
    /// </summary>
    public string? GroupHint { get; set; }
}
