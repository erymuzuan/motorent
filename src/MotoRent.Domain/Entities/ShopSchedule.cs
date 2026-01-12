namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents operating hours for a specific date.
/// The system maintains a rolling 8-week schedule that is auto-populated
/// from the previous week's pattern or from Shop.DefaultHours template.
/// </summary>
public class ShopSchedule : Entity
{
    public int ShopScheduleId { get; set; }

    /// <summary>
    /// The shop this schedule belongs to.
    /// </summary>
    public int ShopId { get; set; }

    /// <summary>
    /// The specific date this schedule applies to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Whether the shop is open on this date.
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Opening time in local time (Thailand UTC+7).
    /// </summary>
    public TimeSpan OpenTime { get; set; } = new(8, 0, 0);

    /// <summary>
    /// Closing time in local time (Thailand UTC+7).
    /// </summary>
    public TimeSpan CloseTime { get; set; } = new(18, 0, 0);

    /// <summary>
    /// Optional note for this date (e.g., "Songkran Holiday", "Extended hours for New Year").
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Day of week derived from the Date property.
    /// </summary>
    public DayOfWeek DayOfWeek => this.Date.DayOfWeek;

    /// <summary>
    /// Checks if a given time is within this schedule's operating hours.
    /// </summary>
    public bool IsWithinHours(TimeSpan time)
    {
        if (!this.IsOpen)
            return false;

        return time >= this.OpenTime && time <= this.CloseTime;
    }

    public override int GetId() => this.ShopScheduleId;
    public override void SetId(int value) => this.ShopScheduleId = value;
}
