using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

public partial class Rental
{
    /// <summary>
    /// Number of rental days (for Daily duration type).
    /// </summary>
    [JsonIgnore]
    public int RentalDays => DurationType == RentalDurationType.Daily
        ? Math.Max(1, (int)(ExpectedEndDate.Date - StartDate.Date).TotalDays + 1)
        : 0;

    /// <summary>
    /// Number of rental hours (for Hourly duration type).
    /// </summary>
    [JsonIgnore]
    public int CalculatedRentalHours => DurationType == RentalDurationType.Hourly
        ? Math.Max(1, (int)Math.Ceiling((ExpectedEndDate - StartDate).TotalHours))
        : 0;

    /// <summary>
    /// Human-readable duration display.
    /// </summary>
    [JsonIgnore]
    public string DurationDisplay => DurationType switch
    {
        RentalDurationType.Daily => $"{RentalDays} day{(RentalDays > 1 ? "s" : "")}",
        RentalDurationType.Hourly => $"{CalculatedRentalHours} hour{(CalculatedRentalHours > 1 ? "s" : "")}",
        RentalDurationType.FixedInterval => $"{IntervalMinutes} minutes",
        _ => ""
    };

    /// <summary>
    /// Whether this was a cross-shop return.
    /// </summary>
    [JsonIgnore]
    public bool IsCrossShopReturn => ReturnedToShopId.HasValue && ReturnedToShopId != RentedFromShopId;

    /// <summary>
    /// Amount due to be paid (Total - payments).
    /// Placeholder logic: assume some balance for now.
    /// </summary>
    [JsonIgnore]
    public decimal BalanceDue => TotalAmount; // In a real scenario, this would subtract payments
}
