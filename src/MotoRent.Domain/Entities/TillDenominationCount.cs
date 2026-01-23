namespace MotoRent.Domain.Entities;

/// <summary>
/// Type of denomination count (opening float or closing count).
/// </summary>
public enum DenominationCountType
{
    /// <summary>
    /// Opening float denomination breakdown when starting a shift.
    /// </summary>
    Opening,

    /// <summary>
    /// Closing count denomination breakdown when ending a shift.
    /// </summary>
    Closing
}

/// <summary>
/// Represents a currency's denomination breakdown with counts per denomination.
/// </summary>
public class CurrencyDenominationBreakdown
{
    /// <summary>
    /// Currency code (THB, USD, EUR, CNY).
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Denomination counts. Key is denomination value (e.g., 1000, 500, 100), value is count.
    /// Example: { 1000: 5, 500: 3, 100: 10 } means 5x1000, 3x500, 10x100.
    /// </summary>
    public Dictionary<decimal, int> Denominations { get; set; } = new();

    /// <summary>
    /// Total calculated from denominations (sum of key * value for all entries).
    /// </summary>
    public decimal Total => Denominations.Sum(d => d.Key * d.Value);

    /// <summary>
    /// Expected balance for this currency (only populated for closing counts).
    /// Used for variance calculation.
    /// </summary>
    public decimal? ExpectedBalance { get; set; }

    /// <summary>
    /// Variance between counted total and expected balance (only for closing counts).
    /// Positive = over, Negative = short.
    /// </summary>
    public decimal? Variance => ExpectedBalance.HasValue ? Total - ExpectedBalance.Value : null;
}

/// <summary>
/// Records denomination-level cash counts for a till session.
/// Supports both opening float counts and closing reconciliation counts.
/// </summary>
public class TillDenominationCount : Entity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int TillDenominationCountId { get; set; }

    /// <summary>
    /// Foreign key to the till session this count belongs to.
    /// </summary>
    public int TillSessionId { get; set; }

    /// <summary>
    /// Whether this is an opening or closing count.
    /// </summary>
    public DenominationCountType CountType { get; set; } = DenominationCountType.Opening;

    /// <summary>
    /// When the count was recorded.
    /// </summary>
    public DateTimeOffset CountedAt { get; set; }

    /// <summary>
    /// Username of the staff member who performed the count.
    /// </summary>
    public string CountedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the count (e.g., variance explanation).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Denomination breakdowns for each currency counted.
    /// Typically includes THB and any foreign currencies present.
    /// </summary>
    public List<CurrencyDenominationBreakdown> CurrencyBreakdowns { get; set; } = [];

    /// <summary>
    /// Grand total in THB (THB total + foreign amounts converted at time of count).
    /// </summary>
    public decimal TotalInThb { get; set; }

    /// <summary>
    /// Whether this is a finalized count or a draft in progress.
    /// Drafts can be overwritten; final counts are preserved.
    /// </summary>
    public bool IsFinal { get; set; }

    public override int GetId() => TillDenominationCountId;
    public override void SetId(int value) => TillDenominationCountId = value;
}
