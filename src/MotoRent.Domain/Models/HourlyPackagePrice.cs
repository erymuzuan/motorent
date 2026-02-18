namespace MotoRent.Domain.Models;

/// <summary>
/// Defines a fixed package price for a specific number of rental hours.
/// Example: "Half Day" - 4 hours = 300 THB (instead of 4 × hourly rate).
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
    /// Fixed total price for this package.
    /// </summary>
    public decimal Price { get; set; }
}
