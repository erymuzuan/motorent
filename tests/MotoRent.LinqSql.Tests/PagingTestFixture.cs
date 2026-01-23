using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

/// <summary>
/// Tests for paging and ORDER BY translation.
/// </summary>
public class PagingTestFixture
{
    private readonly ITestOutputHelper m_output;
    private readonly SqlQueryProvider m_provider;
    private readonly Sql2012PagingTranslator m_pagingTranslator;

    public PagingTestFixture(ITestOutputHelper output)
    {
        this.m_output = output;
        this.m_provider = new SqlQueryProvider(new MockRequestContext());
        this.m_pagingTranslator = new Sql2012PagingTranslator();
    }

    [Fact]
    public void Translate_WithoutOrderBy_AddsDefaultOrderByEntityId()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("ORDER BY [TillSessionId]");
        result.Should().Contain("OFFSET 0 ROWS");
        result.Should().Contain("FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void Translate_WithExistingOrderBy_PreservesOrderBy()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession] ORDER BY [OpenedAt] DESC";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("ORDER BY [OpenedAt] DESC");
        result.Should().NotContain("ORDER BY [TillSessionId]");
        result.Should().Contain("OFFSET 0 ROWS");
        result.Should().Contain("FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void Translate_Page2_CalculatesOffsetCorrectly()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 2, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("OFFSET 20 ROWS");
        result.Should().Contain("FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void Translate_Page3Size50_CalculatesOffsetCorrectly()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 3, size: 50);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("OFFSET 100 ROWS");
        result.Should().Contain("FETCH NEXT 50 ROWS ONLY");
    }

    [Fact]
    public void Translate_WithWhereClause_AddsDefaultOrderBy()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession] WHERE ([ShopId] = 1)";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("WHERE ([ShopId] = 1)");
        result.Should().Contain("ORDER BY [TillSessionId]");
        result.Should().Contain("OFFSET 0 ROWS");
    }

    [Fact]
    public void TranslateWithSkip_CalculatesCorrectly()
    {
        // Arrange
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession]";

        // Act
        var result = this.m_pagingTranslator.TranslateWithSkip(sql, top: 10, skip: 5);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("OFFSET 5 ROWS");
        result.Should().Contain("FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void Translate_VehicleTable_UsesVehicleIdForOrderBy()
    {
        // Arrange
        var sql = "SELECT [VehicleId], [Json] FROM [MotoRent].[Vehicle]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("ORDER BY [VehicleId]");
    }

    [Fact]
    public void Translate_RenterTable_UsesRenterIdForOrderBy()
    {
        // Arrange
        var sql = "SELECT [RenterId], [Json] FROM [MotoRent].[Renter]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert
        result.Should().Contain("ORDER BY [RenterId]");
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
    public void Translate_CaseInsensitiveOrderByDetection()
    {
        // Arrange - lowercase "order by"
        var sql = "SELECT [TillSessionId], [Json] FROM [MotoRent].[TillSession] order by [OpenedAt]";

        // Act
        var result = this.m_pagingTranslator.Translate(sql, page: 1, size: 20);
        this.m_output.WriteLine(result);

        // Assert - should not add another ORDER BY
        var orderByCount = result.Split("ORDER BY", StringSplitOptions.None).Length - 1 +
                           result.Split("order by", StringSplitOptions.None).Length - 1;
        orderByCount.Should().Be(1);
    }
}
