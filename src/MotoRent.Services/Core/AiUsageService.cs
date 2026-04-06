using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Core;

public class AiUsageService
{
    private readonly CoreDataContext m_context;
    private readonly ILogger<AiUsageService> m_logger;

    private static readonly Dictionary<string, (decimal InputPerMillion, decimal OutputPerMillion)> s_defaultPricing = new()
    {
        ["gemini-3.1-flash-lite-preview"] = (0.25m, 1.50m),
        ["gemini-3-flash-preview"] = (0.50m, 3.00m),
    };

    private static readonly decimal s_defaultMyrRate = 4.5m;

    public AiUsageService(CoreDataContext context, ILogger<AiUsageService> logger)
    {
        m_context = context;
        m_logger = logger;
    }

    #region Rate Limiting

    public async Task<RateLimitResult> CheckRateLimitAsync(string? userName, string? ipAddress, string? sessionId)
    {
        var now = DateTimeOffset.Now;
        var startOfDay = new DateTimeOffset(now.Date, now.Offset);
        var startOfWeek = startOfDay.AddDays(-(int)now.DayOfWeek);

        if (!string.IsNullOrEmpty(userName))
        {
            return await CheckRegisteredLimitAsync(userName, startOfDay, startOfWeek);
        }

        return await CheckAnonymousLimitAsync(ipAddress, sessionId, startOfDay);
    }

    private async Task<RateLimitResult> CheckRegisteredLimitAsync(
        string userName, DateTimeOffset startOfDay, DateTimeOffset startOfWeek)
    {
        const int dailyLimit = 50;
        const int weeklyLimit = 200;

        var dailyQuery = m_context.AiUsageLogs
            .Where(x => x.UserName == userName && x.DateTime >= startOfDay);
        var dailyUsed = await m_context.GetCountAsync(dailyQuery);

        var weeklyQuery = m_context.AiUsageLogs
            .Where(x => x.UserName == userName && x.DateTime >= startOfWeek);
        var weeklyUsed = await m_context.GetCountAsync(weeklyQuery);

        return new RateLimitResult(
            Allowed: dailyUsed < dailyLimit && weeklyUsed < weeklyLimit,
            DailyUsed: dailyUsed,
            DailyLimit: dailyLimit,
            WeeklyUsed: weeklyUsed,
            WeeklyLimit: weeklyLimit);
    }

    private async Task<RateLimitResult> CheckAnonymousLimitAsync(
        string? ipAddress, string? sessionId, DateTimeOffset startOfDay)
    {
        const int dailyLimit = 3;

        var query = m_context.AiUsageLogs
            .Where(x => x.UserName == null && x.DateTime >= startOfDay);

        if (!string.IsNullOrEmpty(ipAddress))
        {
            query = query.Where(x => x.IpAddress == ipAddress);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(x => x.SessionId == sessionId);
        }
        else
        {
            return new RateLimitResult(false, 0, dailyLimit, 0, 0);
        }

        var dailyUsed = await m_context.GetCountAsync(query);

        return new RateLimitResult(
            Allowed: dailyUsed < dailyLimit,
            DailyUsed: dailyUsed,
            DailyLimit: dailyLimit,
            WeeklyUsed: 0,
            WeeklyLimit: 0);
    }

    #endregion

    #region Cost Estimation

    public (decimal Usd, decimal Myr) EstimateCost(string model, int inputTokens, int outputTokens)
    {
        var pricing = GetPricing();
        if (!pricing.TryGetValue(model, out var rates))
        {
            rates = s_defaultPricing["gemini-3.1-flash-lite-preview"];
        }

        var usd = (inputTokens * rates.InputPerMillion / 1_000_000m)
                + (outputTokens * rates.OutputPerMillion / 1_000_000m);
        var myr = usd * GetMyrRate();

        return (Math.Round(usd, 6), Math.Round(myr, 6));
    }

    private static Dictionary<string, (decimal InputPerMillion, decimal OutputPerMillion)> GetPricing()
    {
        var envPricing = MotoConfig.GetEnvironmentVariable("AiModelPricing");
        if (string.IsNullOrEmpty(envPricing))
        {
            return s_defaultPricing;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, ModelPricing>>(envPricing);
            if (parsed is not null)
            {
                var result = new Dictionary<string, (decimal, decimal)>();
                foreach (var (key, value) in parsed)
                {
                    result[key] = (value.Input, value.Output);
                }
                return result;
            }
        }
        catch
        {
            // Fall through to defaults
        }

        return s_defaultPricing;
    }

    private static decimal GetMyrRate()
    {
        return s_defaultMyrRate;
    }

    #endregion

    #region Queries for SuperAdmin

    public async Task<AiUsageStats> GetStatsAsync()
    {
        var now = DateTimeOffset.Now;
        var startOfDay = new DateTimeOffset(now.Date, now.Offset);
        var startOfWeek = startOfDay.AddDays(-(int)now.DayOfWeek);

        var todayQuery = m_context.AiUsageLogs.Where(x => x.DateTime >= startOfDay);
        var todayCount = await m_context.GetCountAsync(todayQuery);

        var weekQuery = m_context.AiUsageLogs.Where(x => x.DateTime >= startOfWeek);
        var weekCount = await m_context.GetCountAsync(weekQuery);

        var todayLogs = await m_context.LoadAsync(todayQuery, 1, 1000, includeTotalRows: false);
        var todayCostUsd = todayLogs.ItemCollection.Sum(x => x.EstimatedCostUsd);
        var todayCostMyr = todayLogs.ItemCollection.Sum(x => x.EstimatedCostMyr);

        var weekLogs = await m_context.LoadAsync(weekQuery, 1, 1000, includeTotalRows: false);
        var weekCostUsd = weekLogs.ItemCollection.Sum(x => x.EstimatedCostUsd);
        var weekCostMyr = weekLogs.ItemCollection.Sum(x => x.EstimatedCostMyr);

        return new AiUsageStats(
            todayCount, weekCount,
            todayCostUsd, todayCostMyr,
            weekCostUsd, weekCostMyr);
    }

    public async Task<LoadOperation<AiUsageLog>> GetLogsAsync(AiUsageFilter filter, int page = 1, int size = 20)
    {
        var query = BuildQuery(filter);
        query = query.OrderByDescending(x => x.AiUsageLogId);
        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    public async Task<List<AiUsageModelBreakdown>> GetModelBreakdownAsync(AiUsageFilter filter)
    {
        var query = BuildQuery(filter);
        var lo = await m_context.LoadAsync(query, 1, 5000, includeTotalRows: false);
        var logs = lo.ItemCollection;

        return logs
            .GroupBy(x => x.Model)
            .Select(g => new AiUsageModelBreakdown(
                g.Key,
                g.Count(),
                g.Sum(x => x.InputTokens),
                g.Sum(x => x.OutputTokens),
                g.Sum(x => x.EstimatedCostUsd),
                g.Sum(x => x.EstimatedCostMyr)))
            .OrderByDescending(x => x.Queries)
            .ToList();
    }

    private IQueryable<AiUsageLog> BuildQuery(AiUsageFilter filter)
    {
        var query = m_context.AiUsageLogs.AsQueryable();

        if (!string.IsNullOrEmpty(filter.UserName))
            query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

        if (!string.IsNullOrEmpty(filter.ServiceName))
            query = query.Where(x => x.ServiceName == filter.ServiceName);

        if (!string.IsNullOrEmpty(filter.Model))
            query = query.Where(x => x.Model == filter.Model);

        if (filter.UserType == AiUserType.Anonymous)
            query = query.Where(x => x.UserName == null);
        else if (filter.UserType == AiUserType.Registered)
            query = query.Where(x => x.UserName != null);

        if (filter.From.HasValue)
            query = query.Where(x => x.DateTime >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(x => x.DateTime <= filter.To.Value);

        return query;
    }

    #endregion
}

#region DTOs

public record RateLimitResult(
    bool Allowed,
    int DailyUsed,
    int DailyLimit,
    int WeeklyUsed,
    int WeeklyLimit);

public record AiUsageStats(
    int TodayCount,
    int WeekCount,
    decimal TodayCostUsd,
    decimal TodayCostMyr,
    decimal WeekCostUsd,
    decimal WeekCostMyr);

public record AiUsageModelBreakdown(
    string Model,
    int Queries,
    long InputTokens,
    long OutputTokens,
    decimal CostUsd,
    decimal CostMyr);

public record AiUsageFilter(
    string? UserName = null,
    string? ServiceName = null,
    string? Model = null,
    AiUserType UserType = AiUserType.All,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

public enum AiUserType { All, Anonymous, Registered }

internal class ModelPricing
{
    public decimal Input { get; set; }
    public decimal Output { get; set; }
}

#endregion
