using Moq;
using MotoRent.Domain.Models;
using MotoRent.Domain.Storage;
using MotoRent.Services;
using Xunit;

namespace MotoRent.Domain.Tests;

public class TemplateRenderingTests
{
    private readonly DocumentLayout m_testLayout;
    private readonly Dictionary<string, object?> m_testData;

    public TemplateRenderingTests()
    {
        this.m_testLayout = new DocumentLayout
        {
            Sections = new List<LayoutSection>
            {
                new LayoutSection
                {
                    Name = "Header",
                    Blocks = new List<LayoutBlock>
                    {
                        new TextBlock { Content = "Receipt for {{Customer.Name}}", IsBold = true, FontSize = 16 },
                        new SpacerBlock { Height = 10 }
                    }
                }
            }
        };

        this.m_testData = new Dictionary<string, object?>
        {
            { "Customer.Name", "Alice Smith" }
        };
    }

    [Fact]
    public void HtmlTemplateRenderer_ShouldRenderBasicLayout()
    {
        // Arrange
        var renderer = new HtmlTemplateRenderer();

        // Act
        var html = renderer.RenderHtml(this.m_testLayout, this.m_testData);

        // Assert
        Assert.Contains("Alice Smith", html);
        Assert.Contains("bold", html);
        Assert.Contains("font-size: 16pt", html);
    }

    [Fact]
    public void QuestPdfGenerator_ShouldGeneratePdfBytes()
    {
        // Arrange
        var mockStore = new Mock<IBinaryStore>();
        var generator = new QuestPdfGenerator(mockStore.Object);

        // Act
        var pdfBytes = generator.GeneratePdf(this.m_testLayout, this.m_testData);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        // PDF header is %PDF
        Assert.Equal(0x25, pdfBytes[0]);
        Assert.Equal(0x50, pdfBytes[1]);
        Assert.Equal(0x44, pdfBytes[2]);
        Assert.Equal(0x46, pdfBytes[3]);
    }
}
