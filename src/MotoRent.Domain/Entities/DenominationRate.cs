using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents exchange rates for a specific denomination group.
/// Contains provider rates, delta adjustments, and computed final rates.
/// Shop-specific overrides supported via ShopId (null = org default).
/// </summary>
public class DenominationRate : Entity
{
    /// <summary>
    /// Unique identifier for this rate record
    /// </summary>
    public int DenominationRateId { get; set; }

    /// <summary>
    /// Shop this rate applies to. Null = organization default.
    /// Shop-specific rates take precedence over org defaults.
    /// </summary>
    public int? ShopId { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, GBP, etc.)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.USD;

    /// <summary>
    /// Reference to the denomination group
    /// </summary>
    public int DenominationGroupId { get; set; }

    /// <summary>
    /// Group name (denormalized for display without join)
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Display-friendly denomination list (e.g., "100, 50")
    /// </summary>
    public string DenominationsDisplay { get; set; } = string.Empty;

    // Provider Rate Data

    /// <summary>
    /// Provider code that supplied this rate (e.g., "MamyExchange", "SuperRich", "Manual")
    /// </summary>
    public string ProviderCode { get; set; } = RateProviderCodes.Manual;

    /// <summary>
    /// Original buy rate from provider (before delta adjustment)
    /// </summary>
    public decimal ProviderBuyRate { get; set; }

    /// <summary>
    /// Original sell rate from provider (before delta adjustment)
    /// </summary>
    public decimal ProviderSellRate { get; set; }

    /// <summary>
    /// When the provider last updated this rate
    /// </summary>
    public DateTimeOffset? ProviderUpdatedOn { get; set; }

    // Delta Adjustments

    /// <summary>
    /// Adjustment to apply to buy rate (can be negative).
    /// Example: -0.06 means we give 0.06 THB less per unit.
    /// </summary>
    public decimal BuyDelta { get; set; }

    /// <summary>
    /// Adjustment to apply to sell rate (can be positive or negative).
    /// Example: +0.10 means we charge 0.10 THB more per unit.
    /// </summary>
    public decimal SellDelta { get; set; }

    // Computed Final Rates

    /// <summary>
    /// Final buy rate after applying delta (ProviderBuyRate + BuyDelta)
    /// </summary>
    [JsonIgnore]
    public decimal BuyRate => this.ProviderBuyRate + this.BuyDelta;

    /// <summary>
    /// Final sell rate after applying delta (ProviderSellRate + SellDelta)
    /// </summary>
    [JsonIgnore]
    public decimal SellRate => this.ProviderSellRate + this.SellDelta;

    // Lifecycle

    /// <summary>
    /// When this rate became effective
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// When this rate expires (null = no expiration)
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Whether this rate is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.DenominationRateId;
    public override void SetId(int value) => this.DenominationRateId = value;
}
