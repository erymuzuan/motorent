using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for date/time value translation to SQL.
/// </summary>
public class DateTimeQueryTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly SqlQueryProvider m_provider;

    public DateTimeQueryTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new SqlQueryProvider(new MockRequestContext());
    }

    [Fact]
    public void DateTimeOffsetGreaterThanOrEqual_GeneratesCorrectFormat()
    {
        // Arrange
        var start = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt >= start);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[OpenedAt] >= '");
        sql.Should().Contain("2024-01-15");
    }

    [Fact]
    public void DateTimeOffsetLessThan_GeneratesCorrectFormat()
    {
        // Arrange
        var end = new DateTimeOffset(2024, 1, 16, 0, 0, 0, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt < end);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[OpenedAt] < '");
        sql.Should().Contain("2024-01-16");
    }

    [Fact]
    public void DateTimeOffsetRange_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
        var end = new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt >= start)
            .Where(s => s.OpenedAt <= end);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - Both conditions present in nested structure
        sql.Should().Contain("[OpenedAt] >=");
        sql.Should().Contain("[OpenedAt] <=");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void DateTimeEquality_GeneratesCorrectFormat()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15, 12, 30, 0);
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ExpectedCloseDate == date);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ExpectedCloseDate]");
        sql.Should().Contain("2024-01-15");
    }

    [Fact]
    public void DateOnlyEquality_GeneratesCorrectFormat()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ExpectedCloseDate == date.ToDateTime(TimeOnly.MinValue));

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ExpectedCloseDate]");
        sql.Should().Contain("2024-01-15");
    }

    [Fact]
    public void NullableDateTimeOffsetNull_GeneratesIsNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ClosedAt == null);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ClosedAt]");
        sql.Should().Contain("IS NULL");
    }

    [Fact]
    public void NullableDateTimeOffsetNotNull_GeneratesIsNotNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ClosedAt != null);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ClosedAt]");
        sql.Should().Contain("IS NOT NULL");
    }

    [Fact]
    public void DateTimeLocalVariable_EvaluatesCorrectly()
    {
        // Arrange
        var startDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt >= startDate);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[OpenedAt] >= '");
        sql.Should().Contain("2024-06-01");
    }

    [Fact]
    public void DateTimeWithTimeComponent_FormatsCorrectly()
    {
        // Arrange
        var exact = new DateTimeOffset(2024, 3, 15, 14, 30, 45, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt == exact);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("2024-03-15");
        sql.Should().Contain("14:30:45");
    }

    [Fact]
    public void DateTimeWithOtherConditions_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpenedAt >= start)
            .Where(s => s.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - Both conditions present in nested structure
        sql.Should().Contain("[OpenedAt] >=");
        sql.Should().Contain("[Status] = 'Open'");
        sql.Should().Contain("WHERE");
    }
}
