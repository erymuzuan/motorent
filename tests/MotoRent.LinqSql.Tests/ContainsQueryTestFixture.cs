using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for IN clause translation using IsInList extension method.
/// Note: This codebase requires using IsInList() instead of .Contains() on arrays.
/// </summary>
public class ContainsQueryTestFixture(ITestOutputHelper output)
{
    private readonly SqlQueryProvider m_provider = new(new MockRequestContext());

    [Fact]
    public void IntegerListIsInList_GeneratesInClause()
    {
        // Arrange
        var shopIds = new[] { 1, 2, 3 };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ShopId] IN (1,2,3)");
    }

    [Fact]
    public void StringListIsInList_GeneratesInClause()
    {
        // Arrange
        var staffNames = new[] { "john", "jane", "bob" };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => staffNames.IsInList(s.StaffUserName));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[StaffUserName] IN (");
        sql.Should().Contain("'john'");
        sql.Should().Contain("'jane'");
        sql.Should().Contain("'bob'");
    }

    [Fact]
    public void EmptyIntegerListIsInList_Generates1Equals0()
    {
        // Arrange
        var shopIds = Array.Empty<int>();
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("1=0");
    }

    [Fact]
    public void EmptyStringListIsInList_Generates1Equals0()
    {
        // Arrange
        var staffNames = Array.Empty<string>();
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => staffNames.IsInList(s.StaffUserName));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("1=0");
    }

    [Fact]
    public void NotIsInList_GeneratesNotIn()
    {
        // Arrange
        var shopIds = new[] { 1, 2, 3 };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => !shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("NOT IN");
    }

    [Fact]
    public void EmptyListNotIsInList_Generates1Equals1()
    {
        // Arrange
        var shopIds = Array.Empty<int>();
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => !shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("1=1");
    }

    [Fact]
    public void EnumListIsInList_GeneratesStringInClause()
    {
        // Arrange
        var statuses = new[] { TillSessionStatus.Open, TillSessionStatus.Reconciling };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => statuses.IsInList(s.Status));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status]");
        sql.Should().Contain("IN (");
        sql.Should().Contain("'Open'");
        sql.Should().Contain("'Reconciling'");
    }

    [Fact]
    public void SingleItemListIsInList_GeneratesInClause()
    {
        // Arrange
        var shopIds = new[] { 1 };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ShopId] IN (1)");
    }

    [Fact]
    public void ListIsInListWithOtherConditions_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries
        var shopIds = new[] { 1, 2 };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => shopIds.IsInList(s.ShopId))
            .Where(s => s.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert - Both conditions present in nested structure
        sql.Should().Contain("[ShopId] IN (");
        sql.Should().Contain("[Status] = 'Open'");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void MultipleEnumValues_IsInList_GeneratesCorrectSql()
    {
        // Arrange
        var closedStatuses = new[]
        {
            TillSessionStatus.Closed,
            TillSessionStatus.ClosedWithVariance,
            TillSessionStatus.Verified
        };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => closedStatuses.IsInList(s.Status));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status]");
        sql.Should().Contain("IN (");
        sql.Should().Contain("'Closed'");
        sql.Should().Contain("'ClosedWithVariance'");
        sql.Should().Contain("'Verified'");
    }

    [Fact]
    public void NotInEmptyEnumList_Generates1Equals1()
    {
        // Arrange
        var statuses = Array.Empty<TillSessionStatus>();
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => !statuses.IsInList(s.Status));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("1=1");
    }

    [Fact]
    public void TwoIntegerValues_IsInList_GeneratesCorrectSql()
    {
        // Arrange
        var shopIds = new[] { 5, 10 };
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => shopIds.IsInList(s.ShopId));

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ShopId] IN (5,10)");
    }
}
