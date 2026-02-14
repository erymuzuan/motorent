using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Domain.QueryProviders;
using MotoRent.PostgreSqlRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

public class WhereClauseTestFixture(ITestOutputHelper output)
{
    private PgQueryProvider Provider { get; } = new();

    [Fact]
    public void ShopRentals()
    {
        // Arrange
        var query = new Query<Rental>(this.Provider)
            .Where(s => s.RentedFromShopId == 56);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();
        flattened.Should().Contain("FROM \"Rental\"");
        flattened.Should().Contain("\"RentedFromShopId\" = 56");
    }
    [Fact]
    public void NoSelectSelect()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john")
            .Where(x => x.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();
        flattened.Should().Contain("FROM \"TillSession\"");
        flattened.Should().Contain("\"StaffUserName\" = 'john'");
        flattened.Should().Contain("\"Status\" = 'Open'");
        flattened.Should().Contain("WHERE");
    }
    
    [Fact]
    public void SimpleStringEquality_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john");

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"StaffUserName\" = 'john'");
        sql.Should().Contain("FROM \"TillSession\"");
    }

    [Fact]
    public void SimpleIntEquality_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.ShopId == 42);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"ShopId\" = 42");
    }

    [Fact]
    public void ChainedWhereClauses_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries in this codebase
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john")
            .Where(s => s.ShopId == 1);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert - Both conditions are in separate WHERE clauses due to nested subquery structure
        sql.Should().Contain("\"StaffUserName\" = 'john'");
        sql.Should().Contain("\"ShopId\" = 1");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void CompoundAndPredicate_GeneratesAndOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john" && s.ShopId == 1);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("AND");
        sql.Should().Contain("\"StaffUserName\" = 'john'");
        sql.Should().Contain("\"ShopId\" = 1");
    }

    [Fact]
    public void CompoundOrPredicate_GeneratesOrOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john" || s.StaffUserName == "jane");

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("OR");
    }

    [Fact]
    public void NullEquality_GeneratesIsNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.ClosingNotes == null);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("IS NULL");
    }

    [Fact]
    public void NotNullEquality_GeneratesIsNotNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.ClosingNotes != null);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("IS NOT NULL");
    }

    [Fact]
    public void GreaterThan_GeneratesCorrectOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.OpeningFloat > 1000);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"OpeningFloat\" > 1000");
    }

    [Fact]
    public void LessThanOrEqual_GeneratesCorrectOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.Variance <= 0);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"Variance\" <= 0");
    }

    [Fact]
    public void StringWithSingleQuote_EscapesCorrectly()
    {
        // Arrange
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john's");

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("'john''s'");
    }

    [Fact]
    public void LocalVariable_EvaluatesCorrectly()
    {
        // Arrange
        var staffName = "john";
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == staffName);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"StaffUserName\" = 'john'");
    }

    [Fact]
    public void MultipleChainedWheres_GeneratesAllPredicates()
    {
        // Arrange - Multiple chained Where clauses are merged into a single flat query
        var query = new Query<TillSession>(this.Provider)
            .Where(s => s.StaffUserName == "john")
            .Where(s => s.ShopId == 1)
            .Where(s => s.OpeningFloat >= 5000);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        // Assert - All three conditions are present in merged WHERE clause
        sql.Should().NotBeNull();
        sql.Should().Contain("\"StaffUserName\" = 'john'");
        sql.Should().Contain("\"ShopId\" = 1");
        sql.Should().Contain("\"OpeningFloat\" >= 5000");
        // PostgreSQL formatter currently emits nested subqueries for chained where clauses.
        var whereCount = sql!.Split("WHERE").Length - 1;
        whereCount.Should().BeGreaterThan(1);
    }
}


