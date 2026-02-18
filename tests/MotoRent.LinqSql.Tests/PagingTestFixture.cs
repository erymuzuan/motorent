using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.PostgreSqlRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for PostgreSQL paging translation.
/// </summary>
public class PagingTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly PgQueryProvider m_provider;
    private readonly PgPagingTranslator m_pagingTranslator;

    public PagingTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new PgQueryProvider();
        this.m_pagingTranslator = new PgPagingTranslator();
    }

    [Fact]
    public void Translate_WithoutOrderBy_AppendsLimitOffset()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\"";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("LIMIT 20 OFFSET 0");
    }

    [Fact]
    public void Translate_WithExistingOrderBy_PreservesOrderByAndAppendsPaging()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\" ORDER BY \"OpenedAt\" DESC";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("ORDER BY \"OpenedAt\" DESC");
        result.Should().Contain("LIMIT 20 OFFSET 0");
    }

    [Fact]
    public void Translate_Page2_CalculatesOffsetCorrectly()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\"";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 2, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("LIMIT 20 OFFSET 20");
    }

    [Fact]
    public void Translate_Page3Size50_CalculatesOffsetCorrectly()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\"";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 3, size: 50);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("LIMIT 50 OFFSET 100");
    }

    [Fact]
    public void Translate_WithWhereClause_AppendsLimitOffset()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\" WHERE (\"ShopId\" = 1)";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("WHERE (\"ShopId\" = 1)");
        result.Should().Contain("LIMIT 20 OFFSET 0");
    }

    [Fact]
    public void TranslateWithSkip_CalculatesCorrectly()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\"";

        // Act
        var result = this.m_pagingTranslator.TranslateWithSkip(sql, top: 10, skip: 5);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("LIMIT 10 OFFSET 5");
    }

    [Fact]
    public void QueryToString_WithOrderByDescending_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ShopId == 1)
            .OrderByDescending(s => s.OpenedAt);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("DESC");
    }

    [Fact]
    public void QueryToString_WithOrderBy_GeneratesCorrectSql()
    {
        // Arrange
        var query = new Query<TillSession>(this.m_provider)
            .Where(s => s.ShopId == 1)
            .OrderBy(s => s.TillSessionId);

        // Act
        var sql = query.ToString();
        this.m_output.WriteLine(sql);

        // Assert
        sql.Should().Contain("ORDER BY");
    }

    [Fact]
    public void Translate_DoesNotInjectImplicitOrderBy()
    {
        // Arrange
        var sql = "SELECT \"TillSessionId\", \"Json\" FROM \"TillSession\"";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().NotContain("ORDER BY");
        result.Should().Contain("LIMIT 20 OFFSET 0");
    }
}


