using System.Security.Claims;
using MotoRent.Domain.Core;

namespace MotoRent.Server.Services;

/// <summary>
/// ASP.NET Core implementation of IRequestContext.
/// Provides user context, multi-tenant identity, timezone handling, and date/time formatting.
/// Default timezone is Thailand (UTC+7).
/// </summary>
public class MotoRentRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor m_accessor;
    private const double c_thailandTimezone = 7.0; // UTC+7

    public MotoRentRequestContext(IHttpContextAccessor accessor)
    {
        m_accessor = accessor;
    }

    private HttpContext? Context => m_accessor.HttpContext;

    #region Multi-Tenant Identity

    public string? GetUserName()
    {
        if (Context?.User.Identity?.IsAuthenticated != true)
            return null;

        return Context.User.Identity.Name;
    }

    public string? GetAccountNo()
    {
        if (Context?.User.Identity?.IsAuthenticated != true)
            return null;

        var claim = Context.User.Claims.FirstOrDefault(x => x.Type == "AccountNo");
        return claim?.Value;
    }

    public int GetShopId()
    {
        var claim = this.Context?.User.Claims.FirstOrDefault(x => x.Type == "ShopId");
        if (claim != null && int.TryParse(claim.Value, out var shopId))
            return shopId;
        return 0; // No shop selected - user sees all shops
    }

    #endregion

    #region Claims Access

    public string? GetClaim(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
            return null;

        var claim = Context?.User.Claims.FirstOrDefault(x => x.Type == claimType);
        return claim?.Value;
    }

    public Task<T?> GetClaimAsync<T>(string claimType)
    {
        var value = GetClaim(claimType);
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<T?>(default);

        try
        {
            if (typeof(T) == typeof(string))
                return Task.FromResult<T?>((T)(object)value);

            if (typeof(T) == typeof(int) && int.TryParse(value, out var intVal))
                return Task.FromResult<T?>((T)(object)intVal);

            if (typeof(T) == typeof(double) && double.TryParse(value, out var dblVal))
                return Task.FromResult<T?>((T)(object)dblVal);

            if (typeof(T) == typeof(bool) && bool.TryParse(value, out var boolVal))
                return Task.FromResult<T?>((T)(object)boolVal);

            return Task.FromResult<T?>(default);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public Task<string[]> GetClaimsAsync(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
            return Task.FromResult(Array.Empty<string>());

        var claims = Context?.User.Claims
            .Where(x => x.Type == claimType)
            .Select(x => x.Value)
            .ToArray() ?? [];

        return Task.FromResult(claims);
    }

    public Task<string[]> GetSubscriptions()
    {
        var claims = Context?.User.Claims
            .Where(x => x.Type.StartsWith("subscription:") && x.Value == "true")
            .Select(x => x.Type.Replace("subscription:", ""))
            .ToArray() ?? [];

        return Task.FromResult(claims);
    }

    public async Task<bool> HasSubscription(string subscription)
    {
        var subscriptions = await GetSubscriptions();
        return subscriptions.Contains(subscription, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Timezone and Date/Time

    public double TimezoneOffset
    {
        get
        {
            // Try to get from user claims first
            var claim = Context?.User.Claims.FirstOrDefault(x => x.Type == "Timezone");
            if (claim != null && double.TryParse(claim.Value, out var tz))
                return tz;

            return c_thailandTimezone; // Default to Thailand
        }
    }

    public DayOfWeek FirstDayOfWeek
    {
        get
        {
            var claim = Context?.User.Claims.FirstOrDefault(x => x.Type == "FirstDay");
            if (claim != null && Enum.TryParse<DayOfWeek>(claim.Value, out var day))
                return day;

            return DayOfWeek.Monday; // Default to Monday
        }
    }

    #endregion

    public DateTimeOffset ConvertToDateTimeOffset(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(TimezoneOffset));
    }

    public DateTimeOffset ConvertToDateTimeOffset(DateTime date)
    {
        return new DateTimeOffset(date, TimeSpan.FromHours(TimezoneOffset));
    }

    public DateTimeOffset GetStartOfDay(DateTimeOffset? dt = null)
    {
        dt ??= DateTimeOffset.UtcNow;
        var localTime = dt.Value.ToUniversalTime().AddHours(TimezoneOffset);
        return new DateTimeOffset(localTime.Date, TimeSpan.FromHours(TimezoneOffset));
    }

    public DateOnly GetDate()
    {
        return DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddHours(TimezoneOffset).DateTime);
    }

    public string FormatDateTimeOffsetSortable(DateTimeOffset? dateTimeOffset)
    {
        if (!dateTimeOffset.HasValue) return "";
        var localTime = dateTimeOffset.Value.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:yyyy-MM-ddTHH:mm}";
    }

    public string FormatDate(DateTimeOffset dateTimeOffset)
    {
        var localTime = dateTimeOffset.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:dd/MM/yyyy}";
    }

    public string FormatDateTime(DateTimeOffset dateTimeOffset)
    {
        var localTime = dateTimeOffset.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:dd/MM/yyyy HH:mm}";
    }

    public string FormatTime(DateTimeOffset dateTimeOffset)
    {
        var localTime = dateTimeOffset.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:HH:mm}";
    }

    public Task<bool> IsInRoleAsync(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Task.FromResult(false);

        if (Context?.User.Identity?.IsAuthenticated != true)
            return Task.FromResult(false);

        var roles = role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var r in roles)
        {
            if (Context.User.IsInRole(r))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public DateOnly BeginningOfWeek(DateOnly date)
    {
        var diff = (7 + (date.DayOfWeek - FirstDayOfWeek)) % 7;
        return date.AddDays(-diff);
    }

    public DateOnly EndOfWeek(DateOnly date)
    {
        return BeginningOfWeek(date).AddDays(6);
    }
}
