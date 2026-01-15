namespace MotoRent.Domain.Models;

/// <summary>
/// Captures per-day pricing information for a rental,
/// including any dynamic pricing adjustments applied.
/// </summary>
public class RentalDayPricing
{
    /// <summary>
    /// The date this pricing applies to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Base rate before any dynamic pricing adjustment.
    /// </summary>
    public decimal BaseRate { get; set; }

    /// <summary>
    /// Rate after dynamic pricing adjustment.
    /// </summary>
    public decimal AdjustedRate { get; set; }

    /// <summary>
    /// Multiplier applied for this day (1.0 = no adjustment).
    /// </summary>
    public decimal Multiplier { get; set; } = 1.0m;

    /// <summary>
    /// Name of the pricing rule applied (e.g., "High Season", "Weekend").
    /// </summary>
    public string? RuleName { get; set; }

    /// <summary>
    /// Whether this day has a dynamic pricing adjustment.
    /// </summary>
    public bool HasAdjustment => Multiplier != 1.0m;
}
