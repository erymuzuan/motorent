using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for enum value translation to SQL string literals.
/// </summary>
public class EnumQueryTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly SqlQueryProvider m_provider;

    public EnumQueryTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new SqlQueryProvider(new MockRequestContext());
    }

    [Fact]
    public void EnumEquality_GeneratesStringLiteral()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] = 'Open'");
    }

    [Fact]
    public void EnumNotEqual_GeneratesStringLiteral()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status != TillSessionStatus.Closed);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] <> 'Closed'");
    }

    [Fact]
    public void EnumWithAndOperator_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john" && s.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("[Status] = 'Open'");
        sql.Should().Contain("AND");
    }

    [Fact]
    public void EnumChainedWhere_GeneratesNestedSubqueries()
    {
        // Arrange - Chained Where clauses create nested subqueries
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.StaffUserName == "john")
            .Where(s => s.Status == TillSessionStatus.Open);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert - Both conditions present in nested structure
        sql.Should().Contain("[StaffUserName] = 'john'");
        sql.Should().Contain("[Status] = 'Open'");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void EnumLocalVariable_EvaluatesCorrectly()
    {
        // Arrange
        var status = TillSessionStatus.Reconciling;
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == status);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] = 'Reconciling'");
    }

    [Fact]
    public void EnumMultipleValues_WithOr_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == TillSessionStatus.Open || s.Status == TillSessionStatus.Reconciling);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("OR");
        sql.Should().Contain("'Open'");
        sql.Should().Contain("'Reconciling'");
    }

    [Fact]
    public void EnumVerified_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == TillSessionStatus.Verified);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] = 'Verified'");
    }

    [Fact]
    public void EnumPendingVerification_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == TillSessionStatus.PendingVerification);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] = 'PendingVerification'");
    }

    [Fact]
    public void EnumClosedWithVariance_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.Status == TillSessionStatus.ClosedWithVariance);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("[Status] = 'ClosedWithVariance'");
    }
}
