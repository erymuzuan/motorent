using MotoRent.Domain.Core;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Mock implementation of IRequestContext for unit testing LINQ to SQL translation.
/// </summary>
public class MockRequestContext : IRequestContext
{
    private readonly string m_schema;
    private readonly string m_userName;

    public MockRequestContext(string schema = "MotoRent", string userName = "testuser")
    {
        this.m_schema = schema;
        this.m_userName = userName;
    }

    public string? GetUserName() => this.m_userName;
    public string? GetAccountNo() => this.m_schema;
    public string? GetSchema() => this.m_schema;
    public string GetConnectionString() => "Server=(local);Database=Test;Trusted_Connection=True;";
    public int GetShopId() => 1;
    public string? GetClaim(string claimType) => null;
    public Task<T?> GetClaimAsync<T>(string claimType) => Task.FromResult<T?>(default);
    public Task<string[]> GetClaimsAsync(string claimType) => Task.FromResult(Array.Empty<string>());
    public Task<string[]> GetSubscriptions() => Task.FromResult(Array.Empty<string>());
    public Task<bool> HasSubscription(string subscription) => Task.FromResult(false);
    public Task<bool> IsInRoleAsync(string role) => Task.FromResult(false);

    public double TimezoneOffset => 7;
    public DayOfWeek FirstDayOfWeek => DayOfWeek.Monday;

    public DateTimeOffset ConvertToDateTimeOffset(DateOnly date)
        => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(this.TimezoneOffset));

    public DateTimeOffset ConvertToDateTimeOffset(DateTime date)
        => new(date, TimeSpan.FromHours(this.TimezoneOffset));

    public DateTimeOffset GetStartOfDay(DateTimeOffset? dt = null)
    {
        var d = dt ?? DateTimeOffset.UtcNow;
        return new DateTimeOffset(d.Year, d.Month, d.Day, 0, 0, 0, TimeSpan.FromHours(this.TimezoneOffset));
    }

    public DateOnly GetDate() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(this.TimezoneOffset));

    public string FormatDateTimeOffsetSortable(DateTimeOffset? dateTimeOffset)
        => dateTimeOffset?.ToString("yyyy-MM-ddTHH:mm") ?? string.Empty;

    public string FormatDate(DateTimeOffset dateTimeOffset)
        => dateTimeOffset.ToString("d");

    public string FormatDateTime(DateTimeOffset dateTimeOffset)
        => dateTimeOffset.ToString("g");

    public string FormatTime(DateTimeOffset dateTimeOffset)
        => dateTimeOffset.ToString("t");

    public DateOnly BeginningOfWeek(DateOnly date)
    {
        var daysToSubtract = ((int)date.DayOfWeek - (int)this.FirstDayOfWeek + 7) % 7;
        return date.AddDays(-daysToSubtract);
    }

    public DateOnly EndOfWeek(DateOnly date)
        => this.BeginningOfWeek(date).AddDays(6);
}
