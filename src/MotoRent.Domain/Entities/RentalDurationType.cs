namespace MotoRent.Domain.Entities;

/// <summary>
/// Determines how the rental duration and pricing is calculated.
/// </summary>
public enum RentalDurationType
{
    /// <summary>
    /// Standard daily rental (motorbikes, cars, boats, vans).
    /// Pricing: DailyRate × number of days.
    /// </summary>
    Daily,

    /// <summary>
    /// Hourly rental. Staff selects at check-in.
    /// Pricing: HourlyRate × number of hours.
    /// </summary>
    Hourly,

    /// <summary>
    /// Fixed interval pricing (jet skis: 15min, 30min, 1hr slots).
    /// Pricing: Rate for selected interval (Rate15Min, Rate30Min, Rate1Hour).
    /// </summary>
    FixedInterval
}
