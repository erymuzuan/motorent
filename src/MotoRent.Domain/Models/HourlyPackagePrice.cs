namespace MotoRent.Domain.Models;

/// <summary>
/// Defines a fixed package price for a specific number of rental hours.
/// Example: 4 hours = 300 THB (instead of 4 × hourly rate).
/// </summary>
public class HourlyPackagePrice
{
    /// <summary>
    /// Number of hours for this package.
    /// </summary>
    public int Hours { get; set; }

    /// <summary>
    /// Fixed total price for this package.
    /// </summary>
    public decimal Price { get; set; }
}
