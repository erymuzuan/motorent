namespace MotoRent.Domain.Entities;

/// <summary>
/// Template for default weekly operating hours.
/// Used to auto-populate ShopSchedule entries for new weeks.
/// </summary>
public class DailyHoursTemplate
{
    /// <summary>
    /// Day of week (0 = Sunday, 6 = Saturday).
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Whether the shop is open on this day.
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Opening time in local time (e.g., 08:00).
    /// </summary>
    public TimeSpan OpenTime { get; set; } = new(8, 0, 0);

    /// <summary>
    /// Closing time in local time (e.g., 18:00).
    /// </summary>
    public TimeSpan CloseTime { get; set; } = new(18, 0, 0);
}

/// <summary>
/// Defines an out-of-hours fee band with absolute time range.
/// Examples: 18:00-20:00 (Evening) = 200 THB, 22:00-06:00 (Night) = 500 THB.
/// </summary>
public class OutOfHoursBand
{
    /// <summary>
    /// Start time of this band (e.g., 18:00 for 6PM).
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of this band (e.g., 20:00 for 8PM).
    /// For overnight bands, EndTime can be less than StartTime (e.g., 22:00-06:00).
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Fee for service during this time band (THB).
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Display name for this band (e.g., "Evening", "Late Night").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Checks if a given time falls within this band.
    /// Handles overnight bands where EndTime is less than StartTime.
    /// </summary>
    public bool ContainsTime(TimeSpan time)
    {
        if (this.StartTime <= this.EndTime)
        {
            // Normal band (e.g., 18:00-20:00)
            return time >= this.StartTime && time < this.EndTime;
        }
        else
        {
            // Overnight band (e.g., 22:00-06:00)
            return time >= this.StartTime || time < this.EndTime;
        }
    }
}
