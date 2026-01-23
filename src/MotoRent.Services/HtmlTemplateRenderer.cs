using System.Text;
using MotoRent.Domain.Models;
using MotoRent.Services.Core;

namespace MotoRent.Services;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderHtml(DocumentLayout layout, Dictionary<string, object?> data)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.Append("<style>");
        sb.Append($"body {{ font-family: {layout.Settings.FontFamily}; font-size: {layout.Settings.FontSize}pt; ");
        sb.Append($"padding: {layout.Settings.MarginTop}pt {layout.Settings.MarginRight}pt {layout.Settings.MarginBottom}pt {layout.Settings.MarginLeft}pt; }}");
        sb.Append(".section { margin-bottom: 20px; }");
        sb.Append(".text-center { text-align: center; }");
        sb.Append(".text-right { text-align: right; }");
        sb.Append(".bold { font-weight: bold; }");
        sb.Append("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        sb.Append("th { background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; text-align: left; padding: 8px; }");
        sb.Append("td { border-bottom: 1px solid #dee2e6; padding: 8px; }");
        sb.Append("</style></head><body>");

        foreach (var section in layout.Sections)
        {
            sb.Append("<div class='section'>");
            foreach (var block in section.Blocks)
            {
                RenderBlock(sb, block, data);
            }
            sb.Append("</div>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private void RenderBlock(StringBuilder sb, LayoutBlock block, Dictionary<string, object?> data)
    {
        switch (block)
        {
            case TextBlock textBlock:
                RenderTextBlock(sb, textBlock, data);
                break;
            case SpacerBlock spacerBlock:
                sb.Append($"<div style='height: {spacerBlock.Height}pt'></div>");
                break;
            case ImageBlock imageBlock:
                RenderImageBlock(sb, imageBlock, data);
                break;
            case TableBlock tableBlock:
                RenderTableBlock(sb, tableBlock, data);
                break;
        }
    }

    private void RenderTextBlock(StringBuilder sb, TextBlock block, Dictionary<string, object?> data)
    {
        var content = ReplaceTokens(block.Content, data);
        var classes = new List<string>();
        if (block.IsBold) classes.Add("bold");
        if (!string.IsNullOrEmpty(block.HorizontalAlignment)) classes.Add($"text-{block.HorizontalAlignment.ToLower()}");

        var style = block.FontSize.HasValue ? $" style='font-size: {block.FontSize.Value}pt'" : "";
        var classAttr = classes.Any() ? $" class='{string.Join(" ", classes)}'" : "";

        sb.Append($"<div{classAttr}{style}>{content}</div>");
    }

    private void RenderImageBlock(StringBuilder sb, ImageBlock block, Dictionary<string, object?> data)
    {
        string? imageId = block.BindingPath != null && data.TryGetValue(block.BindingPath, out var val) 
            ? val?.ToString() 
            : block.ImageUrl;

        if (string.IsNullOrEmpty(imageId)) return;

        var style = new StringBuilder("max-width: 100%;");
        if (block.Width.HasValue) style.Append($"width: {block.Width.Value}pt;");
        if (block.Height.HasValue) style.Append($"height: {block.Height.Value}pt;");

        sb.Append($"<img src='{imageId}' style='{style}'>");
    }

    private void RenderTableBlock(StringBuilder sb, TableBlock block, Dictionary<string, object?> data)
    {
        if (!data.TryGetValue(block.BindingPath, out var collection) || collection is not System.Collections.IEnumerable items)
            return;

        sb.Append("<table><thead><tr>");
        foreach (var col in block.Columns)
        {
            sb.Append($"<th>{col.Header}</th>");
        }
        sb.Append("</tr></thead><tbody>");

        foreach (var item in items)
        {
            sb.Append("<tr>");
            foreach (var col in block.Columns)
            {
                var value = GetValueFromItem(item, col.BindingPath);
                sb.Append($"<td>{value}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
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
