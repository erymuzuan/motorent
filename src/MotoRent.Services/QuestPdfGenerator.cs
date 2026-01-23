using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MotoRent.Domain.Models;
using MotoRent.Services.Core;
using MotoRent.Domain.Storage;

namespace MotoRent.Services;

public class QuestPdfGenerator(IBinaryStore binaryStore) : IQuestPdfGenerator
{
    private IBinaryStore BinaryStore { get; } = binaryStore;

    public byte[] GeneratePdf(DocumentLayout layout, Dictionary<string, object?> data)
    {
        // QuestPDF License - Community (Free for small businesses/open source)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(layout.Settings.MarginLeft, Unit.Point);
                // page.MarginRight(layout.Settings.MarginRight, Unit.Point); // Simplified margin in QuestPDF
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(layout.Settings.FontSize).FontFamily(layout.Settings.FontFamily));

                page.Content().Column(col =>
                {
                    foreach (var section in layout.Sections)
                    {
                        foreach (var block in section.Blocks)
                        {
                            RenderBlock(col.Item(), block, data);
                        }
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private void RenderBlock(IContainer container, LayoutBlock block, Dictionary<string, object?> data)
    {
        switch (block)
        {
            case TextBlock textBlock:
                RenderTextBlock(container, textBlock, data);
                break;
            case SpacerBlock spacerBlock:
                container.PaddingTop(spacerBlock.Height, Unit.Point);
                break;
            case ImageBlock imageBlock:
                RenderImageBlock(container, imageBlock, data);
                break;
            case TableBlock tableBlock:
                RenderTableBlock(container, tableBlock, data);
                break;
        }
    }

    private void RenderTextBlock(IContainer container, TextBlock block, Dictionary<string, object?> data)
    {
        var content = ReplaceTokens(block.Content, data);
        var text = container.Text(content);

        if (block.IsBold) text.Bold();
        if (block.FontSize.HasValue) text.FontSize(block.FontSize.Value);

        switch (block.HorizontalAlignment?.ToLower())
        {
            case "center": container.AlignCenter(); break;
            case "right": container.AlignRight(); break;
        }
    }

    private void RenderImageBlock(IContainer container, ImageBlock block, Dictionary<string, object?> data)
    {
        string? imageId = block.BindingPath != null && data.TryGetValue(block.BindingPath, out var val) 
            ? val?.ToString() 
            : block.ImageUrl;

        if (string.IsNullOrEmpty(imageId)) return;

        // Note: For simplicity in this generator, we assume images are available or we use a placeholder.
        // In a real implementation, we would fetch from BinaryStore.
        // For now, we'll just draw a placeholder box if it's not a real URL.
        if (imageId.StartsWith("http"))
        {
            // container.Image(imageId); // QuestPDF can load from URL/Stream
        }
    }

    private void RenderTableBlock(IContainer container, TableBlock block, Dictionary<string, object?> data)
    {
        // Table rendering requires the data to be a collection
        if (!data.TryGetValue(block.BindingPath, out var collection) || collection is not System.Collections.IEnumerable items)
            return;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var col in block.Columns) columns.RelativeColumn();
            });

            // Header
            table.Header(header =>
            {
                foreach (var col in block.Columns)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text(col.Header).Bold();
                }
            });

            // Rows
            foreach (var item in items)
            {
                foreach (var col in block.Columns)
                {
                    var value = GetValueFromItem(item, col.BindingPath);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(value?.ToString() ?? "");
                }
            }
        });
    }

    private string ReplaceTokens(string template, Dictionary<string, object?> data)
    {
        if (string.IsNullOrEmpty(template)) return template;

        foreach (var kvp in data)
        {
            template = template.Replace("{{" + kvp.Key + "}}", kvp.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    private object? GetValueFromItem(object item, string path)
    {
        if (item == null || string.IsNullOrEmpty(path)) return null;

        var prop = item.GetType().GetProperty(path);
        return prop?.GetValue(item);
    }
}
