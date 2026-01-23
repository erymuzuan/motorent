using System.Text.Json.Serialization;

namespace MotoRent.Domain.Models;

/// <summary>
/// Root model for a document template layout.
/// </summary>
public class DocumentLayout
{
    public List<LayoutSection> Sections { get; set; } = [];
    public LayoutSettings Settings { get; set; } = new();
}

public class LayoutSettings
{
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 10;
    public float MarginLeft { get; set; } = 20;
    public float MarginRight { get; set; } = 20;
    public float MarginTop { get; set; } = 20;
    public float MarginBottom { get; set; } = 20;
}

public class LayoutSection
{
    public string Name { get; set; } = "Section";
    public List<LayoutBlock> Blocks { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(TableBlock), "table")]
[JsonDerivedType(typeof(SpacerBlock), "spacer")]
[JsonDerivedType(typeof(HeadingBlock), "heading")]
[JsonDerivedType(typeof(DividerBlock), "divider")]
[JsonDerivedType(typeof(SignatureBlock), "signature")]
[JsonDerivedType(typeof(TwoColumnBlock), "two-columns")]
public abstract class LayoutBlock
{
    public string? Style { get; set; }
}

public class TextBlock : LayoutBlock
{
    public string Content { get; set; } = string.Empty;
    public string? HorizontalAlignment { get; set; } // Left, Center, Right
    public bool IsBold { get; set; }
    public float? FontSize { get; set; }
}

public class HeadingBlock : TextBlock
{
    public int Level { get; set; } = 1; // 1 to 6
}

public class ImageBlock : LayoutBlock
{
    public string? ImageUrl { get; set; }
    public string? BindingPath { get; set; } // e.g., "Organization.Logo"
    public float? Width { get; set; }
    public float? Height { get; set; }
}

public class TableBlock : LayoutBlock
{
    public string BindingPath { get; set; } = string.Empty; // e.g., "Rental.Items"
    public List<TableColumn> Columns { get; set; } = [];
}

public class DividerBlock : LayoutBlock
{
    public float Thickness { get; set; } = 1;
    public string Color { get; set; } = "#000000";
}

public class SignatureBlock : LayoutBlock
{
    public string Label { get; set; } = "Signature";
    public string? BindingPath { get; set; } // e.g., "Customer.Signature"
}

public class TwoColumnBlock : LayoutBlock
{
    public List<LayoutBlock> LeftColumn { get; set; } = [];
    public List<LayoutBlock> RightColumn { get; set; } = [];
}

public class TableColumn
{
    public string Header { get; set; } = string.Empty;
    public string BindingPath { get; set; } = string.Empty; // e.g., "Description"
    public string? Format { get; set; }
    public string? HorizontalAlignment { get; set; }
}

public class SpacerBlock : LayoutBlock
{
    public float Height { get; set; } = 10;
}
