using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing shop operating schedules.
/// Handles auto-population of rolling 8-week schedules from previous week's pattern.
/// </summary>
public class ShopScheduleService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    private const int DefaultRollingWeeks = 8;

    #region Query Methods

    /// <summary>
    /// Gets schedule entries for a date range, auto-populating missing dates.
    /// </summary>
    public async Task<List<ShopSchedule>> GetScheduleAsync(
        int shopId,
        DateOnly startDate,
        DateOnly endDate)
    {
        // Load existing schedules
        var query = this.Context.CreateQuery<ShopSchedule>()
            .Where(s => s.ShopId == shopId && s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date);

        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);
        var existingSchedules = result.ItemCollection.ToDictionary(s => s.Date);

        // Build complete schedule list, filling gaps
        var schedules = new List<ShopSchedule>();
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (existingSchedules.TryGetValue(date, out var existing))
            {
                schedules.Add(existing);
            }
            else
            {
                // Create placeholder from template (not saved until explicitly updated)
                var placeholder = this.CreateFromTemplate(shopId, date, shop);
                schedules.Add(placeholder);
            }
        }

        return schedules;
    }

    /// <summary>
    /// Gets schedule for a specific date, auto-populating if missing.
    /// </summary>
    public async Task<ShopSchedule> GetScheduleForDateAsync(int shopId, DateOnly date)
    {
        var existing = await this.Context.LoadOneAsync<ShopSchedule>(
            s => s.ShopId == shopId && s.Date == date);

        if (existing != null)
            return existing;

        // Create from template
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
        return this.CreateFromTemplate(shopId, date, shop);
    }

    /// <summary>
    /// Gets schedule for a specific date, returning null if not found.
    /// </summary>
    public async Task<ShopSchedule?> GetScheduleForDateOrNullAsync(int shopId, DateOnly date)
    {
        return await this.Context.LoadOneAsync<ShopSchedule>(
            s => s.ShopId == shopId && s.Date == date);
    }

    #endregion

    #region Auto-Population

    /// <summary>
    /// Ensures schedule entries exist for the next N weeks, creating from templates/previous week.
    /// </summary>
    public async Task<int> EnsureScheduleExistsAsync(int shopId, string username, int weeks = DefaultRollingWeeks)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var endDate = today.AddDays(weeks * 7);

        // Load existing schedules for the range
        var query = this.Context.CreateQuery<ShopSchedule>()
            .Where(s => s.ShopId == shopId && s.Date >= today && s.Date <= endDate);
        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);
        var existingDates = result.ItemCollection.Select(s => s.Date).ToHashSet();

        // Find missing dates
        var missingDates = new List<DateOnly>();
        for (var date = today; date <= endDate; date = date.AddDays(1))
        {
            if (!existingDates.Contains(date))
                missingDates.Add(date);
        }

        if (missingDates.Count == 0)
            return 0;

        // Load shop for default hours template
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);

        // Try to get previous week's schedules for copying
        var previousWeekStart = today.AddDays(-7);
        var previousWeekQuery = this.Context.CreateQuery<ShopSchedule>()
            .Where(s => s.ShopId == shopId && s.Date >= previousWeekStart && s.Date < today);
        var previousWeekResult = await this.Context.LoadAsync(previousWeekQuery, page: 1, size: 7, includeTotalRows: false);
        var previousWeekByDayOfWeek = previousWeekResult.ItemCollection
            .ToDictionary(s => s.DayOfWeek);

        // Create missing schedules
        using var session = this.Context.OpenSession(username);
        var createdCount = 0;

        foreach (var date in missingDates)
        {
            ShopSchedule newSchedule;

            // Try to copy from previous week's same day
            if (previousWeekByDayOfWeek.TryGetValue(date.DayOfWeek, out var previousDay))
            {
                newSchedule = new ShopSchedule
                {
                    ShopId = shopId,
                    Date = date,
                    IsOpen = previousDay.IsOpen,
                    OpenTime = previousDay.OpenTime,
                    CloseTime = previousDay.CloseTime,
                    Note = null // Don't copy notes
                };
            }
            else
            {
                // Use shop's default template
                newSchedule = this.CreateFromTemplate(shopId, date, shop);
            }

            session.Attach(newSchedule);
            createdCount++;
        }

        if (createdCount > 0)
        {
            await session.SubmitChanges("AutoPopulate");
        }

        return createdCount;
    }

    /// <summary>
    /// Creates a schedule from the shop's default hours template.
    /// </summary>
    private ShopSchedule CreateFromTemplate(int shopId, DateOnly date, Shop? shop)
    {
        var template = shop?.DefaultHours.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);

        return new ShopSchedule
        {
            ShopId = shopId,
            Date = date,
            IsOpen = template?.IsOpen ?? true,
            OpenTime = template?.OpenTime ?? new TimeSpan(8, 0, 0),
            CloseTime = template?.CloseTime ?? new TimeSpan(18, 0, 0)
        };
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Updates a single schedule entry.
    /// </summary>
    public async Task<SubmitOperation> UpdateScheduleAsync(ShopSchedule schedule, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(schedule);
        return await session.SubmitChanges("UpdateShopSchedule");
    }

    /// <summary>
    /// Batch updates multiple schedule entries.
    /// </summary>
    public async Task<SubmitOperation> BatchUpdateScheduleAsync(
        IEnumerable<ShopSchedule> schedules,
        string username)
    {
        using var session = this.Context.OpenSession(username);

        foreach (var schedule in schedules)
        {
            session.Attach(schedule);
        }

        return await session.SubmitChanges("BatchUpdate");
    }

    /// <summary>
    /// Copies a week's schedule to another week.
    /// </summary>
    public async Task<SubmitOperation> CopyWeekAsync(
        int shopId,
        DateOnly sourceWeekStart,
        DateOnly targetWeekStart,
        string username)
    {
        // Load source week (7 days)
        var sourceEnd = sourceWeekStart.AddDays(6);
        var sourceSchedules = await this.GetScheduleAsync(shopId, sourceWeekStart, sourceEnd);

        // Create target week schedules
        var targetSchedules = new List<ShopSchedule>();

        foreach (var source in sourceSchedules)
        {
            var dayOffset = (int)(source.Date.DayNumber - sourceWeekStart.DayNumber);
            var targetDate = targetWeekStart.AddDays(dayOffset);

            // Check if target already exists
            var existing = await this.GetScheduleForDateOrNullAsync(shopId, targetDate);

            var target = existing ?? new ShopSchedule { ShopId = shopId, Date = targetDate };
            target.IsOpen = source.IsOpen;
            target.OpenTime = source.OpenTime;
            target.CloseTime = source.CloseTime;
            // Note is not copied

            targetSchedules.Add(target);
        }

        return await this.BatchUpdateScheduleAsync(targetSchedules, username);
    }

    /// <summary>
    /// Marks a date as closed (holiday).
    /// </summary>
    public async Task<SubmitOperation> MarkAsClosedAsync(
        int shopId,
        DateOnly date,
        string? note,
        string username)
    {
        var schedule = await this.GetScheduleForDateAsync(shopId, date);
        schedule.IsOpen = false;
        schedule.Note = note;

        return await this.UpdateScheduleAsync(schedule, username);
    }

    /// <summary>
    /// Sets extended hours for a date.
    /// </summary>
    public async Task<SubmitOperation> SetExtendedHoursAsync(
        int shopId,
        DateOnly date,
        TimeSpan openTime,
        TimeSpan closeTime,
        string? note,
        string username)
    {
        var schedule = await this.GetScheduleForDateAsync(shopId, date);
        schedule.IsOpen = true;
        schedule.OpenTime = openTime;
        schedule.CloseTime = closeTime;
        schedule.Note = note;

        return await this.UpdateScheduleAsync(schedule, username);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Removes old schedule entries (older than specified days).
    /// </summary>
    public async Task<int> CleanupOldSchedulesAsync(int shopId, int daysToKeep, string username)
    {
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysToKeep));

        var query = this.Context.CreateQuery<ShopSchedule>()
            .Where(s => s.ShopId == shopId && s.Date < cutoffDate);
        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);

        if (result.ItemCollection.Count == 0)
            return 0;

        using var session = this.Context.OpenSession(username);

        foreach (var schedule in result.ItemCollection)
        {
            session.Delete(schedule);
        }

        await session.SubmitChanges("Cleanup");
        return result.ItemCollection.Count;
    }

    #endregion
}
