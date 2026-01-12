using MotoRent.Domain.Core;
using MotoRent.Domain.Tourist;

namespace MotoRent.Server.Services;

/// <summary>
/// IRequestContext implementation for tourist-facing pages that resolves tenant from URL.
///
/// Resolution priority:
/// 1. HttpContext.Items["TenantAccountNo"] (set by TenantDomainMiddleware for custom domains)
/// 2. URL path segment /tourist/{accountNo}/...
/// 3. HttpContext.Items["TenantContext"] for timezone/settings
/// 4. Fallback to authenticated user's claims (for logged-in tourists)
///
/// This allows anonymous users to browse tenant pages without authentication.
/// </summary>
public class TouristRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor m_accessor;
    private readonly MotoRentRequestContext m_fallback;
    private string? m_resolvedAccountNo;
    private const double c_thailandTimezone = 7.0;

    public TouristRequestContext(IHttpContextAccessor accessor)
    {
        m_accessor = accessor;
        m_fallback = new MotoRentRequestContext(accessor);
    }

    private HttpContext? Context => m_accessor.HttpContext;

    #region Multi-Tenant Identity

    public string? GetUserName()
    {
        // Tourists are typically anonymous
        return m_fallback.GetUserName();
    }

    public string? GetAccountNo()
    {
        if (m_resolvedAccountNo != null)
            return m_resolvedAccountNo;

        if (Context == null)
            return m_fallback.GetAccountNo();

        // Priority 1: Custom domain middleware set the tenant
        if (Context.Items.TryGetValue("TenantAccountNo", out var domainTenant) &&
            domainTenant is string tenantStr && !string.IsNullOrEmpty(tenantStr))
        {
            m_resolvedAccountNo = tenantStr;
            return m_resolvedAccountNo;
        }

        // Priority 2: Extract from URL path /tourist/{accountNo}/...
        var path = Context.Request.Path.Value ?? "";
        if (path.StartsWith("/tourist/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                m_resolvedAccountNo = segments[1];
                return m_resolvedAccountNo;
            }
        }

        // Priority 3: Fall back to authenticated user's tenant
        return m_fallback.GetAccountNo();
    }

    public int GetShopId()
    {
        // Default shop for tourist browsing
        return m_fallback.GetShopId();
    }

    #endregion

    #region Claims Access

    public string? GetClaim(string claimType)
    {
        return m_fallback.GetClaim(claimType);
    }

    public Task<T?> GetClaimAsync<T>(string claimType)
    {
        return m_fallback.GetClaimAsync<T>(claimType);
    }

    public Task<string[]> GetClaimsAsync(string claimType)
    {
        return m_fallback.GetClaimsAsync(claimType);
    }

    public Task<string[]> GetSubscriptions()
    {
        return m_fallback.GetSubscriptions();
    }

    public Task<bool> HasSubscription(string subscription)
    {
        return m_fallback.HasSubscription(subscription);
    }

    public Task<bool> IsInRoleAsync(string role)
    {
        return m_fallback.IsInRoleAsync(role);
    }

    #endregion

    #region Timezone and Date/Time

    public double TimezoneOffset
    {
        get
        {
            // Try to get from tenant context first
            if (Context?.Items.TryGetValue("TenantContext", out var tenantObj) == true &&
                tenantObj is TenantContext tenant)
            {
                return tenant.Timezone;
            }

            // Fall back to user claims or default
            return m_fallback.TimezoneOffset;
        }
    }

    public DayOfWeek FirstDayOfWeek
    {
        get
        {
            // Default to Monday for Thailand
            return m_fallback.FirstDayOfWeek;
        }
    }

    #endregion

    #region Date/Time Formatting

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
        return $"{localTime:d}";
    }

    public string FormatDateTime(DateTimeOffset dateTimeOffset)
    {
        var localTime = dateTimeOffset.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:g}";
    }

    public string FormatTime(DateTimeOffset dateTimeOffset)
    {
        var localTime = dateTimeOffset.ToUniversalTime().AddHours(TimezoneOffset);
        return $"{localTime:t}";
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

    #endregion
}
