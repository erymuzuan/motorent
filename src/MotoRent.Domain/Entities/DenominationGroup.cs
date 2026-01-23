namespace MotoRent.Domain.Entities;

/// <summary>
/// Defines a grouping of denominations that share the same exchange rate.
/// For example, USD 100 and 50 bills often get the same (better) rate,
/// while smaller bills (20, 10, 5, 1) share a different rate.
/// Admin-configurable through settings UI.
/// </summary>
public class DenominationGroup : Entity
{
    /// <summary>
    /// Unique identifier for this denomination group
    /// </summary>
    public int DenominationGroupId { get; set; }

    /// <summary>
    /// Currency code this group applies to (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.USD;

    /// <summary>
    /// Display name for this group (e.g., "Large Bills", "Small Bills", "Standard Notes")
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// The denominations included in this group (e.g., [100, 50])
    /// </summary>
    public List<decimal> Denominations { get; set; } = [];

    /// <summary>
    /// Display order within the currency (lower = first)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this group is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets a comma-separated display of denominations (e.g., "100, 50")
    /// </summary>
    public string DenominationsDisplay => string.Join(", ", this.Denominations.OrderByDescending(d => d));

    public override int GetId() => this.DenominationGroupId;
    public override void SetId(int value) => this.DenominationGroupId = value;
}
