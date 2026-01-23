using MotoRent.Domain.Entities;
using Xunit;

namespace MotoRent.Domain.Tests;

public class DocumentTemplateTests
{
    [Fact]
    public void DocumentTemplate_ShouldSetProperties()
    {
        // Arrange
        var template = new DocumentTemplate
        {
            DocumentTemplateId = 1,
            Name = "Default Agreement",
            ShopId = 42,
            Type = DocumentType.RentalAgreement,
            Status = DocumentTemplateStatus.Draft,
            StoreId = "templates/rental-v1.json",
            IsDefault = true,
            Version = 1
        };

        // Assert
        Assert.Equal(1, template.DocumentTemplateId);
        Assert.Equal("Default Agreement", template.Name);
        Assert.Equal(42, template.ShopId);
        Assert.Equal(DocumentType.RentalAgreement, template.Type);
        Assert.Equal(DocumentTemplateStatus.Draft, template.Status);
        Assert.Equal("templates/rental-v1.json", template.StoreId);
        Assert.True(template.IsDefault);
        Assert.Equal(1, template.Version);
    }

    [Fact]
    public void GetId_ShouldReturnDocumentTemplateId()
    {
        // Arrange
        var template = new DocumentTemplate { DocumentTemplateId = 42 };

        // Assert
        Assert.Equal(42, template.GetId());
    }

    [Fact]
    public void SetId_ShouldSetDocumentTemplateId()
    {
        // Arrange
        var template = new DocumentTemplate();

        // Act
        template.SetId(99);

        // Assert
        Assert.Equal(99, template.DocumentTemplateId);
        Assert.Equal(99, template.GetId());
    }

    [Fact]
    public void Validation_ShouldRequireName()
    {
        // Arrange
        var template = new DocumentTemplate { Name = "" };

        // Assert
        Assert.False(template.IsValid());
    }

    [Fact]
    public void StateTransition_ShouldAllowApprovalFromDraft()
    {
        // Arrange
        var template = new DocumentTemplate { Status = DocumentTemplateStatus.Draft };

        // Act
        template.Approve();

        // Assert
        Assert.Equal(DocumentTemplateStatus.Approved, template.Status);
    }

    [Fact]
    public void DocumentLayout_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var layout = new MotoRent.Domain.Models.DocumentLayout
        {
            Sections = new System.Collections.Generic.List<MotoRent.Domain.Models.LayoutSection>
            {
                new MotoRent.Domain.Models.LayoutSection
                {
                    Name = "Header",
                    Blocks = new System.Collections.Generic.List<MotoRent.Domain.Models.LayoutBlock>
                    {
                        new MotoRent.Domain.Models.TextBlock { Content = "Hello {{Name}}", IsBold = true },
                        new MotoRent.Domain.Models.SpacerBlock { Height = 20 }
                    }
                }
            }
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(layout);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<MotoRent.Domain.Models.DocumentLayout>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Sections);
        Assert.Equal(2, deserialized.Sections[0].Blocks.Count);
        Assert.IsType<MotoRent.Domain.Models.TextBlock>(deserialized.Sections[0].Blocks[0]);
        Assert.IsType<MotoRent.Domain.Models.SpacerBlock>(deserialized.Sections[0].Blocks[1]);
        Assert.Equal("Hello {{Name}}", ((MotoRent.Domain.Models.TextBlock)deserialized.Sections[0].Blocks[0]).Content);
    }
}
