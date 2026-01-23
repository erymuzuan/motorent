using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for basic WHERE clause translation.
/// </summary>
public class WhereClauseTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly SqlQueryProvider m_provider;

    public WhereClauseTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new SqlQueryProvider(new MockRequestContext());
    }

    [Fact]
    public void SimpleStringEquality_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john");

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("FROM [MotoRent].[TillSession]");
    }

    [Fact]
    public void SimpleIntEquality_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ShopId == 42);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[ShopId] = 42");
    }

    [Fact]
    public void ChainedWhereClauses_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries in this codebase
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john")
            .Where(s => s.ShopId == 1);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - Both conditions are in separate WHERE clauses due to nested subquery structure
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("[ShopId] = 1");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void CompoundAndPredicate_GeneratesAndOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john" && s.ShopId == 1);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("AND");
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("[ShopId] = 1");
    }

    [Fact]
    public void CompoundOrPredicate_GeneratesOrOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john" || s.StaffUserName == "jane");

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("OR");
    }

    [Fact]
    public void NullEquality_GeneratesIsNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ClosingNotes == null);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("IS NULL");
    }

    [Fact]
    public void NotNullEquality_GeneratesIsNotNull()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ClosingNotes != null);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("IS NOT NULL");
    }

    [Fact]
    public void GreaterThan_GeneratesCorrectOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.OpeningFloat > 1000);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[OpeningFloat] > 1000");
    }

    [Fact]
    public void LessThanOrEqual_GeneratesCorrectOperator()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Variance <= 0);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Variance] <= 0");
    }

    [Fact]
    public void StringWithSingleQuote_EscapesCorrectly()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john's");

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("'john''s'");
    }

    [Fact]
    public void LocalVariable_EvaluatesCorrectly()
    {
        // Arrange
        var staffName = "john";
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == staffName);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[StaffUserName] = 'john'");
    }

    [Fact]
    public void MultipleChainedWheres_GeneratesNestedSubqueries()
    {
        // Arrange - Multiple chained Where clauses create deeply nested subqueries
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john")
            .Where(s => s.ShopId == 1)
            .Where(s => s.OpeningFloat >= 5000);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - All three conditions are present in the nested structure
        sql.Should().NotBeNull();
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("[ShopId] = 1");
        sql.Should().Contain("[OpeningFloat] >= 5000");
        // Count occurrences of WHERE for nested subqueries
        var whereCount = sql!.Split("WHERE").Length - 1;
        whereCount.Should().BeGreaterThanOrEqualTo(3);
    }
}
