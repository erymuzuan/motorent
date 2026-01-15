namespace MotoRent.Domain.Core;

/// <summary>
/// Provides user context, timezone handling, and date/time formatting for the current request.
/// Thailand default timezone is UTC+7.
/// </summary>
public interface IRequestContext
{
    #region Multi-Tenant Identity

    /// <summary>
    /// Gets the current username from the authentication context.
    /// </summary>
    string? GetUserName();

    /// <summary>
    /// Gets the current username asynchronously.
    /// </summary>
    Task<string?> GetUserNameAsync() => Task.FromResult(GetUserName());

    /// <summary>
    /// Gets the current organization AccountNo (tenant identifier).
    /// </summary>
    string? GetAccountNo();

    /// <summary>
    /// Gets the current organization AccountNo asynchronously.
    /// </summary>
    Task<string?> GetAccountNoAsync() => Task.FromResult(GetAccountNo());

    /// <summary>
    /// Gets the database schema name for the current tenant.
    /// Returns AccountNo for tenant data, "Core" for shared data.
    /// </summary>
    string? GetSchema() => GetAccountNo();

    /// <summary>
    /// Gets the current shop ID for the authenticated user.
    /// Returns 0 if no shop is selected (user sees all shops).
    /// </summary>
    int GetShopId();

    #endregion

    #region Claims Access

    /// <summary>
    /// Gets a claim value by type.
    /// </summary>
    string? GetClaim(string claimType);

    /// <summary>
    /// Gets a typed claim value.
    /// </summary>
    Task<T?> GetClaimAsync<T>(string claimType);

    /// <summary>
    /// Gets all claim values for a claim type.
    /// </summary>
    Task<string[]> GetClaimsAsync(string claimType);

    /// <summary>
    /// Gets the subscriptions for the current organization.
    /// </summary>
    Task<string[]> GetSubscriptions();

    /// <summary>
    /// Checks if the current organization has a specific subscription.
    /// </summary>
    Task<bool> HasSubscription(string subscription);

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    Task<bool> IsInRoleAsync(string role);

    #endregion

    #region Timezone and Date/Time

    /// <summary>
    /// Gets the timezone offset in hours from UTC. Default is 7 (Thailand).
    /// </summary>
    double TimezoneOffset { get; }

    /// <summary>
    /// Gets the first day of the week. Default is Monday.
    /// </summary>
    DayOfWeek FirstDayOfWeek { get; }

    /// <summary>
    /// Converts a DateOnly to DateTimeOffset using the user's timezone.
    /// </summary>
    DateTimeOffset ConvertToDateTimeOffset(DateOnly date);

    /// <summary>
    /// Converts a DateTime to DateTimeOffset using the user's timezone.
    /// </summary>
    DateTimeOffset ConvertToDateTimeOffset(DateTime date);

    /// <summary>
    /// Gets the start of the current day in the user's timezone.
    /// </summary>
    DateTimeOffset GetStartOfDay(DateTimeOffset? dt = null);

    /// <summary>
    /// Gets the current date in the user's timezone.
    /// </summary>
    DateOnly GetDate();

    /// <summary>
    /// Formats a DateTimeOffset as a sortable string (yyyy-MM-ddTHH:mm).
    /// </summary>
    string FormatDateTimeOffsetSortable(DateTimeOffset? dateTimeOffset);

    /// <summary>
    /// Formats a DateTimeOffset as a short date string.
    /// </summary>
    string FormatDate(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Formats a DateTimeOffset as a general date/time string.
    /// </summary>
    string FormatDateTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Formats a DateTimeOffset as a short time string.
    /// </summary>
    string FormatTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Gets the beginning of the week for a given date.
    /// </summary>
    DateOnly BeginningOfWeek(DateOnly date);

    /// <summary>
    /// Gets the end of the week for a given date.
    /// </summary>
    DateOnly EndOfWeek(DateOnly date);

    #endregion
}
