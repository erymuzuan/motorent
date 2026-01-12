using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for checking operating hours and calculating out-of-hours fees.
/// </summary>
public class OperatingHoursService(RentalDataContext context, ShopScheduleService scheduleService)
{
    private RentalDataContext Context { get; } = context;
    private ShopScheduleService ScheduleService { get; } = scheduleService;

    #region Operating Hours Checks

    /// <summary>
    /// Gets the effective operating hours for a shop on a specific date.
    /// </summary>
    public async Task<EffectiveHours> GetEffectiveHoursAsync(int shopId, DateOnly date)
    {
        var schedule = await this.ScheduleService.GetScheduleForDateAsync(shopId, date);

        if (!schedule.IsOpen)
        {
            return EffectiveHours.Closed(schedule.Note);
        }

        return new EffectiveHours
        {
            IsOpen = true,
            OpenTime = schedule.OpenTime,
            CloseTime = schedule.CloseTime,
            Note = schedule.Note
        };
    }

    /// <summary>
    /// Checks if a specific time is within operating hours.
    /// </summary>
    public async Task<bool> IsWithinOperatingHoursAsync(int shopId, DateTimeOffset dateTime)
    {
        var date = DateOnly.FromDateTime(dateTime.DateTime);
        var time = dateTime.TimeOfDay;

        var effectiveHours = await this.GetEffectiveHoursAsync(shopId, date);

        if (!effectiveHours.IsOpen)
            return false;

        return time >= effectiveHours.OpenTime && time <= effectiveHours.CloseTime;
    }

    /// <summary>
    /// Checks if a specific time is within operating hours (synchronous version for pricing).
    /// </summary>
    public bool IsWithinOperatingHours(ShopSchedule schedule, TimeSpan time)
    {
        if (!schedule.IsOpen)
            return false;

        return time >= schedule.OpenTime && time <= schedule.CloseTime;
    }

    #endregion

    #region Out-of-Hours Fee Calculation

    /// <summary>
    /// Gets the out-of-hours fee for a specific time, if applicable.
    /// Returns null if within operating hours.
    /// </summary>
    public async Task<OutOfHoursResult?> GetOutOfHoursFeeAsync(int shopId, DateTimeOffset dateTime)
    {
        var date = DateOnly.FromDateTime(dateTime.DateTime);
        var time = dateTime.TimeOfDay;

        // Check if within operating hours
        var schedule = await this.ScheduleService.GetScheduleForDateAsync(shopId, date);

        if (this.IsWithinOperatingHours(schedule, time))
            return null; // No fee - within operating hours

        // Get shop's out-of-hours bands
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);

        if (shop == null || shop.OutOfHoursBands.Count == 0)
        {
            // No bands configured - return zero fee but mark as out-of-hours
            return new OutOfHoursResult
            {
                IsOutOfHours = true,
                Fee = 0,
                BandName = null
            };
        }

        // Find matching band
        var matchingBand = this.FindMatchingBand(shop.OutOfHoursBands, time);

        if (matchingBand != null)
        {
            return new OutOfHoursResult
            {
                IsOutOfHours = true,
                Fee = matchingBand.Fee,
                BandName = matchingBand.Name
            };
        }

        // No matching band - use highest fee as fallback
        var highestBand = shop.OutOfHoursBands.OrderByDescending(b => b.Fee).First();
        return new OutOfHoursResult
        {
            IsOutOfHours = true,
            Fee = highestBand.Fee,
            BandName = highestBand.Name ?? "Out of Hours"
        };
    }

    /// <summary>
    /// Gets the matching band for a specific time (synchronous for pricing).
    /// </summary>
    public OutOfHoursBand? FindMatchingBand(List<OutOfHoursBand> bands, TimeSpan time)
    {
        return bands.FirstOrDefault(b => b.ContainsTime(time));
    }

    /// <summary>
    /// Calculates out-of-hours fee using pre-loaded data (for pricing service).
    /// </summary>
    public OutOfHoursResult? CalculateOutOfHoursFee(
        ShopSchedule schedule,
        List<OutOfHoursBand> bands,
        TimeSpan time)
    {
        // Check if within operating hours
        if (this.IsWithinOperatingHours(schedule, time))
            return null;

        if (bands.Count == 0)
        {
            return new OutOfHoursResult
            {
                IsOutOfHours = true,
                Fee = 0,
                BandName = null
            };
        }

        var matchingBand = this.FindMatchingBand(bands, time);

        if (matchingBand != null)
        {
            return new OutOfHoursResult
            {
                IsOutOfHours = true,
                Fee = matchingBand.Fee,
                BandName = matchingBand.Name
            };
        }

        // Fallback to highest fee
        var highestBand = bands.OrderByDescending(b => b.Fee).First();
        return new OutOfHoursResult
        {
            IsOutOfHours = true,
            Fee = highestBand.Fee,
            BandName = highestBand.Name ?? "Out of Hours"
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates that a shop has operating hours configured.
    /// </summary>
    public async Task<bool> HasOperatingHoursConfiguredAsync(int shopId)
    {
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
        return shop?.DefaultHours.Count > 0;
    }

    /// <summary>
    /// Validates that a shop has out-of-hours bands configured.
    /// </summary>
    public async Task<bool> HasOutOfHoursBandsConfiguredAsync(int shopId)
    {
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
        return shop?.OutOfHoursBands.Count > 0;
    }

    #endregion
}

/// <summary>
/// Represents the effective operating hours for a specific date.
/// </summary>
public class EffectiveHours
{
    public bool IsOpen { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public string? Note { get; set; }

    public static EffectiveHours Closed(string? note = null) => new()
    {
        IsOpen = false,
        Note = note
    };

    /// <summary>
    /// Formats the hours for display (e.g., "08:00 - 18:00" or "Closed").
    /// </summary>
    public string FormatDisplay()
    {
        if (!this.IsOpen)
            return this.Note ?? "Closed";

        return $"{this.OpenTime:hh\\:mm} - {this.CloseTime:hh\\:mm}";
    }
}

/// <summary>
/// Result of an out-of-hours fee calculation.
/// </summary>
public class OutOfHoursResult
{
    public bool IsOutOfHours { get; set; }
    public decimal Fee { get; set; }
    public string? BandName { get; set; }

    /// <summary>
    /// Formats for display (e.g., "Late Evening (฿300)").
    /// </summary>
    public string FormatDisplay()
    {
        if (string.IsNullOrEmpty(this.BandName))
            return $"฿{this.Fee:N0}";

        return $"{this.BandName} (฿{this.Fee:N0})";
    }
}
