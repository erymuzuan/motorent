namespace MotoRent.Domain.Entities;

/// <summary>
/// Persists delta configuration for reapplication when provider rates refresh.
/// Deltas are applied on top of provider rates to adjust for margin, fees, etc.
/// Shop-specific deltas override org defaults.
/// </summary>
public class RateDelta : Entity
{
    /// <summary>
    /// Unique identifier for this delta record
    /// </summary>
    public int RateDeltaId { get; set; }

    /// <summary>
    /// Shop this delta applies to. Null = organization default.
    /// Shop-specific deltas take precedence over org defaults.
    /// </summary>
    public int? ShopId { get; set; }

    /// <summary>
    /// Currency code this delta applies to (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.USD;

    /// <summary>
    /// Denomination group this delta applies to.
    /// Null = applies to all groups for the currency.
    /// </summary>
    public int? DenominationGroupId { get; set; }

    /// <summary>
    /// Adjustment to apply to buy rate.
    /// Negative values reduce the rate (we give less THB per unit).
    /// </summary>
    public decimal BuyDelta { get; set; }

    /// <summary>
    /// Adjustment to apply to sell rate.
    /// Positive values increase the rate (we charge more THB per unit).
    /// </summary>
    public decimal SellDelta { get; set; }

    /// <summary>
    /// Whether this delta is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.RateDeltaId;
    public override void SetId(int value) => this.RateDeltaId = value;
}
