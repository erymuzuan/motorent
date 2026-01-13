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
    /// Human-readable duration display.
    /// </summary>
    [JsonIgnore]
    public string DurationDisplay => DurationType == RentalDurationType.Daily
        ? $"{RentalDays} day{(RentalDays > 1 ? "s" : "")}"
        : $"{IntervalMinutes} minutes";

    /// <summary>
    /// Whether this was a cross-shop return.
    /// </summary>
    [JsonIgnore]
    public bool IsCrossShopReturn => ReturnedToShopId.HasValue && ReturnedToShopId != RentedFromShopId;
}
