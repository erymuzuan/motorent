using FluentAssertions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Domain.QueryProviders;
using MotoRent.SqlServerRepository;
using Xunit;
using Xunit.Abstractions;

namespace MotoRent.LinqSql.Tests;

public class DocumentTemplateQueryTests(ITestOutputHelper output)
{
    private SqlQueryProvider Provider { get; } = new(new MockRequestContext());

    [Fact]
    public void DocumentTemplate_BasicQuery()
    {
        // Arrange
        var query = new Query<DocumentTemplate>(this.Provider)
            .Where(t => t.Type == DocumentType.BookingConfirmation && t.IsDefault);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();

        // Assert
        flattened.Should().Be(
            "SELECT [DocumentTemplateId], [Json] FROM [MotoRent].[DocumentTemplate] WHERE (([Type] = 'BookingConfirmation') AND ([IsDefault] = 1))");
    }

    [Fact]
    public void DocumentTemplate_StatusQuery()
    {
        // Arrange
        var query = new Query<DocumentTemplate>(this.Provider)
            .Where(t => t.Status == DocumentTemplateStatus.Approved);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();

        // Assert
        flattened.Should().Be(
            "SELECT [DocumentTemplateId], [Json] FROM [MotoRent].[DocumentTemplate] WHERE ([Status] = 'Approved')");
    }

    [Fact]
    public void DocumentTemplate_ShopQuery()
    {
        // Arrange
        var query = new Query<DocumentTemplate>(this.Provider)
            .Where(t => t.ShopId == 42 || t.ShopId == null);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();

        // Assert
        flattened.Should().Be(
            "SELECT [DocumentTemplateId], [Json] FROM [MotoRent].[DocumentTemplate] WHERE (([ShopId] = 42) OR ([ShopId] IS NULL))");
    }

    [Fact]
    public void DocumentTemplate_ListQuery()
    {
        // Arrange - Simulate DocumentTemplateService.GetTemplatesByTypeAsync
        var type = DocumentType.RentalAgreement;
        var shopId = 42;

        var query = new Query<DocumentTemplate>(this.Provider)
            .Where(t => t.Type == type);

        if (shopId > 0)
        {
            query = query.Where(t => t.ShopId == null || t.ShopId == 0 || t.ShopId == shopId);
        }

        query = query.OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.ShopId > 0)
            .ThenBy(t => t.Name);

        // Act
        var sql = query.ToString();
        output.WriteLine(sql);

        var flattened = sql.FlattenSql();

        // Assert
        // Note: The boolean expression (t.ShopId > 0) in ThenByDescending 
        // will be translated according to VisitBinary logic.
        flattened.Should().Contain("ORDER BY ([IsDefault] = 1) DESC, ([ShopId] > 0) DESC, [Name]");
        flattened.Should().Contain("WHERE (([Type] = 'RentalAgreement') AND ((([ShopId] IS NULL) OR ([ShopId] = 0)) OR ([ShopId] = 42)))");
    }
}
