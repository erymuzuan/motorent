using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.PostgreSqlRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for boolean predicate translation to SQL.
/// </summary>
public class BooleanQueryTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly PgQueryProvider m_provider;

    public BooleanQueryTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new PgQueryProvider();
    }

    [Fact]
    public void BooleanTrue_GeneratesEqualsTrue()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
    }

    [Fact]
    public void BooleanFalse_GeneratesEqualsFalse()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => !s.IsForceClose);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\" = false");
    }

    [Fact]
    public void BooleanExplicitTrue_GeneratesEqualsTrue()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose == true);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("true");
    }

    [Fact]
    public void BooleanExplicitFalse_GeneratesEqualsFalse()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose == false);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("false");
    }

    [Fact]
    public void BooleanWithOtherConditions_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose && s.ShopId == 1);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("\"ShopId\" = 1");
        sql.Should().Contain("AND");
    }

    [Fact]
    public void BooleanChainedWhere_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose)
            .Where(s => s.ShopId == 1);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - Both conditions present in nested structure
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("\"ShopId\" = 1");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void BooleanIsLateClose_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsLateClose);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsLateClose\"");
    }

    [Fact]
    public void BooleanNotLateClose_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => !s.IsLateClose);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsLateClose\" = false");
    }

    [Fact]
    public void BooleanLocalVariable_EvaluatesCorrectly()
    {
        // Arrange
        var isForced = true;
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose == isForced);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("true");
    }

    [Fact]
    public void MultipleBooleans_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.IsForceClose)
            .Where(s => s.IsLateClose);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("\"IsForceClose\"");
        sql.Should().Contain("\"IsLateClose\"");
    }
}


