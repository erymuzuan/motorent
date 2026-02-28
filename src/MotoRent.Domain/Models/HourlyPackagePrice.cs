namespace MotoRent.Domain.Models;

/// <summary>
/// Defines a fixed package price for a specific number of rental hours.
/// Example: "Half Day" - 4 hours = 300 THB (instead of 4 × hourly rate).
/// Dynamic pricing adjustments are handled by Pricing Rules in Settings.
/// </summary>
public class HourlyPackagePrice
{
    /// <summary>
    /// Display name for this package (e.g., "Half Day", "Full Day").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of hours for this package.
    /// </summary>
    public int Hours { get; set; }

    /// <summary>
    /// Base price for this package.
    /// Dynamic pricing adjustments (weekends, holidays, seasons) are applied via Pricing Rules.
    /// </summary>
    public decimal Price { get; set; }
}
