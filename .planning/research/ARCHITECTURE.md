# Architecture Patterns: Document Template Editor

**Domain:** Document template designer for SaaS rental system
**Researched:** 2026-01-23
**Overall Confidence:** HIGH (based on existing codebase patterns and established document editor practices)

## Executive Summary

This document defines the architecture for a document template editor that integrates with the existing MotoRent Blazor application. The design follows established patterns from the codebase (Entity base class, Repository pattern, JSON columns) while introducing template-specific concerns: element models, data binding, and rendering pipelines.

## Recommended Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Presentation Layer                              │
│  ┌──────────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │   Template Designer  │  │  Template List   │  │   Print/Render View  │  │
│  │   (Canvas + Panels)  │  │  (CRUD UI)       │  │   (Document Output)  │  │
│  └──────────┬───────────┘  └────────┬─────────┘  └──────────┬───────────┘  │
└─────────────┼──────────────────────┼─────────────────────────┼──────────────┘
              │                      │                         │
┌─────────────┼──────────────────────┼─────────────────────────┼──────────────┐
│             │         Services Layer                         │              │
│  ┌──────────▼───────────┐  ┌───────▼────────┐  ┌────────────▼───────────┐  │
│  │  DesignerStateService│  │ TemplateService│  │  DocumentRenderService │  │
│  │  (State Management)  │  │ (CRUD + Query) │  │  (Template → Output)   │  │
│  └──────────────────────┘  └────────────────┘  └────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────┐  ┌────────────────┐  ┌────────────────────────┐  │
│  │ DataModelService     │  │ ClauseService  │  │  PdfExportService      │  │
│  │ (Context Resolution) │  │ (AI Suggester) │  │  (HTML → PDF)          │  │
│  └──────────────────────┘  └────────────────┘  └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
              │                      │                         │
┌─────────────┼──────────────────────┼─────────────────────────┼──────────────┐
│             │            Domain Layer                        │              │
│  ┌──────────▼───────────┐  ┌───────▼────────┐  ┌────────────▼───────────┐  │
│  │  DocumentTemplate    │  │ TemplateElement│  │  DataModel Classes     │  │
│  │  (Entity)            │  │ (Polymorphic)  │  │  (AgreementModel, etc) │  │
│  └──────────────────────┘  └────────────────┘  └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
              │
┌─────────────┼───────────────────────────────────────────────────────────────┐
│             │                  Data Layer                                    │
│  ┌──────────▼────────────────────────────────────────────────────────────┐  │
│  │  [AccountNo].[DocumentTemplate] - JSON Column Storage                 │  │
│  │  Follows existing Repository pattern with RentalDataContext           │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| **Template Designer** | Canvas UI, drag-drop, element selection | DesignerStateService, TemplateService |
| **Designer Canvas** | Visual rendering of elements, click/drag handling | DesignerStateService (selection, position) |
| **Elements Palette** | Available element types for drag-drop | DesignerStateService (add element) |
| **Properties Panel** | Element configuration, data binding UI | DesignerStateService (update element) |
| **DesignerStateService** | In-memory editor state, undo/redo stack | N/A (holds state) |
| **TemplateService** | CRUD operations for templates | RentalDataContext, Repository |
| **DocumentRenderService** | Merge template + data model → HTML | DataModelService |
| **DataModelService** | Build context models from entities | RentalDataContext (loads entities) |
| **PdfExportService** | Convert rendered HTML → PDF | External library |
| **ClauseService** | AI-suggested agreement clauses | Gemini API |

## Data Flow

### 1. Template Design Flow

```
User Action (drag element)
       │
       ▼
┌─────────────────┐
│ Elements Palette│ ──drag──▶ ┌─────────────────┐
└─────────────────┘           │  Designer Canvas │
                              └────────┬────────┘
                                       │ drop event
                                       ▼
                         ┌─────────────────────────┐
                         │  DesignerStateService   │
                         │  • AddElement()         │
                         │  • Updates ElementList  │
                         │  • Pushes to UndoStack  │
                         └────────────┬────────────┘
                                      │ StateChanged event
                                      ▼
              ┌───────────────────────┴───────────────────────┐
              │                                               │
              ▼                                               ▼
     ┌────────────────┐                           ┌────────────────────┐
     │ Designer Canvas│ re-renders elements       │ Properties Panel   │ shows
     └────────────────┘                           │ selected element   │
                                                  └────────────────────┘
```

### 2. Template Storage Flow

```
Save Button Click
       │
       ▼
┌─────────────────────────┐
│  DesignerStateService   │
│  • GetTemplateSnapshot()│
└────────────┬────────────┘
             │ DocumentTemplate entity
             ▼
┌─────────────────────────┐
│    TemplateService      │
│  • ValidateTemplate()   │
│  • SaveTemplateAsync()  │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   RentalDataContext     │
│   • OpenSession()       │
│   • Attach(template)    │
│   • SubmitChanges()     │
└────────────┬────────────┘
             │ JSON serialization
             ▼
┌─────────────────────────────────────────────────┐
│  [AccountNo].[DocumentTemplate]                 │
│  Json column stores full template definition    │
└─────────────────────────────────────────────────┘
```

### 3. Document Rendering Flow

```
Print/Preview Request
       │
       ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│  Rental Detail Page     │────▶│    DocumentRenderService │
│  (Print button)         │     │    • RenderAsync()       │
└─────────────────────────┘     └────────────┬────────────┘
                                             │
        ┌────────────────────────────────────┼─────────────────────────────┐
        │                                    │                             │
        ▼                                    ▼                             ▼
┌───────────────────┐          ┌─────────────────────────┐    ┌──────────────────┐
│  TemplateService  │          │   DataModelService      │    │ Render Template  │
│  • LoadTemplate() │          │   • BuildAgreementModel │    │ • Merge elements │
└────────┬──────────┘          │   • Load related entities│    │ • Resolve bindings│
         │                     └────────────┬────────────┘    └─────────┬────────┘
         │                                  │                           │
         │  DocumentTemplate                │ AgreementModel            │ HTML string
         │                                  │                           │
         └──────────────────────────────────┴───────────────────────────┘
                                             │
                                             ▼
                               ┌─────────────────────────┐
                               │      Output Options     │
                               │  • HTML Preview         │
                               │  • Browser Print (Ctrl+P)│
                               │  • PDF Download         │
                               └─────────────────────────┘
```

## Entity Design

### DocumentTemplate Entity

```csharp
public class DocumentTemplate : Entity
{
    public int DocumentTemplateId { get; set; }

    // Identity
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "RentalAgreement"; // RentalAgreement, Receipt, BookingConfirmation

    // State
    public bool IsDefault { get; set; }
    public bool IsApproved { get; set; } // Staff can only use approved templates
    public string Status { get; set; } = "Draft"; // Draft, Approved, Archived

    // Page Settings
    public PageSettings Page { get; set; } = new();

    // Template Definition (main content)
    public List<TemplateElement> Elements { get; set; } = [];

    // Multi-page support
    public List<TemplatePage> Pages { get; set; } = [];

    public override int GetId() => DocumentTemplateId;
    public override void SetId(int value) => DocumentTemplateId = value;
}

public class PageSettings
{
    public string Size { get; set; } = "A4"; // A4, Letter
    public string Orientation { get; set; } = "Portrait"; // Portrait, Landscape
    public decimal MarginTop { get; set; } = 20; // mm
    public decimal MarginBottom { get; set; } = 20;
    public decimal MarginLeft { get; set; } = 15;
    public decimal MarginRight { get; set; } = 15;
}

public class TemplatePage
{
    public int PageNumber { get; set; }
    public List<TemplateElement> Elements { get; set; } = [];
}
```

### Element Model (Polymorphic JSON)

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$elementType")]
[JsonDerivedType(typeof(TextElement), "Text")]
[JsonDerivedType(typeof(ImageElement), "Image")]
[JsonDerivedType(typeof(TwoColumnElement), "TwoColumn")]
[JsonDerivedType(typeof(DividerElement), "Divider")]
[JsonDerivedType(typeof(SignatureElement), "Signature")]
[JsonDerivedType(typeof(DateElement), "Date")]
[JsonDerivedType(typeof(ContainerElement), "Container")]
[JsonDerivedType(typeof(RepeaterElement), "Repeater")]
public abstract class TemplateElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    // Layout
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal? Height { get; set; } // null = auto

    // Common styling
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public decimal? BorderWidth { get; set; }
    public decimal? Padding { get; set; }
    public decimal? Margin { get; set; }
}

public class TextElement : TemplateElement
{
    public string Content { get; set; } = string.Empty;
    public string? DataBinding { get; set; } // e.g., "Rental.RenterName"
    public string? Format { get; set; } // e.g., "d" for short date, "C" for currency

    // Typography
    public string FontFamily { get; set; } = "Arial";
    public decimal FontSize { get; set; } = 12;
    public string FontWeight { get; set; } = "Normal"; // Normal, Bold
    public string FontStyle { get; set; } = "Normal"; // Normal, Italic
    public string TextAlign { get; set; } = "Left"; // Left, Center, Right
    public string? Color { get; set; }
}

public class ImageElement : TemplateElement
{
    public string? Src { get; set; } // Static image path
    public string? DataBinding { get; set; } // Dynamic: "Organization.LogoStoreId"
    public string ObjectFit { get; set; } = "Contain"; // Contain, Cover, Fill
}

public class TwoColumnElement : TemplateElement
{
    public decimal LeftRatio { get; set; } = 0.5m; // 50% default
    public List<TemplateElement> LeftColumn { get; set; } = [];
    public List<TemplateElement> RightColumn { get; set; } = [];
    public decimal? Gap { get; set; } // Space between columns
}

public class DividerElement : TemplateElement
{
    public string Style { get; set; } = "Solid"; // Solid, Dashed, Dotted
    public decimal Thickness { get; set; } = 1;
    public string? Color { get; set; }
}

public class SignatureElement : TemplateElement
{
    public string Label { get; set; } = "Signature";
    public bool ShowDateLine { get; set; } = true;
}

public class DateElement : TemplateElement
{
    public string? DataBinding { get; set; } // "DateTime.Now" or "Rental.StartDate"
    public string Format { get; set; } = "dd MMMM yyyy";
}

public class ContainerElement : TemplateElement
{
    public List<TemplateElement> Children { get; set; } = [];
    public string Layout { get; set; } = "Vertical"; // Vertical, Horizontal
    public decimal? Gap { get; set; }
}

public class RepeaterElement : TemplateElement
{
    public string DataSource { get; set; } = string.Empty; // e.g., "Rental.Accessories"
    public List<TemplateElement> ItemTemplate { get; set; } = [];
    public decimal? ItemSpacing { get; set; }
}
```

### Data Context Models

```csharp
/// <summary>
/// Context model for Rental Agreement documents.
/// Flattens related entities for easy data binding.
/// </summary>
public class AgreementModel
{
    // Document metadata
    public DateTimeOffset GeneratedOn { get; set; } = DateTimeOffset.Now;
    public string AgreementNumber { get; set; } = string.Empty;

    // Organization (tenant)
    public OrganizationInfo Organization { get; set; } = new();

    // Shop
    public ShopInfo Shop { get; set; } = new();

    // Renter
    public RenterInfo Renter { get; set; } = new();

    // Vehicle
    public VehicleInfo Vehicle { get; set; } = new();

    // Rental details
    public RentalInfo Rental { get; set; } = new();

    // Collections for repeaters
    public List<AccessoryInfo> Accessories { get; set; } = [];
    public List<ClauseInfo> Clauses { get; set; } = [];

    // Computed/convenience
    public string FormattedTotal => Rental.TotalAmount.ToString("C");
    public int RentalDays => Math.Max(1, (int)(Rental.EndDate - Rental.StartDate).TotalDays + 1);
}

public class OrganizationInfo
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxNo { get; set; }
    public string FullAddress { get; set; } = string.Empty;
}

public class ShopInfo
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class RenterInfo
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PassportNo { get; set; }
    public string? NationalIdNo { get; set; }
    public string? Nationality { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? HotelName { get; set; }
}

public class VehicleInfo
{
    public string Type { get; set; } = string.Empty; // "Motorbike", "Car", etc.
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public string DisplayName => $"{Brand} {Model}".Trim();
}

public class RentalInfo
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string DepositType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AccessoryInfo
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DailyRate { get; set; }
}

public class ClauseInfo
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
```

## Designer State Management

### DesignerStateService Pattern

```csharp
/// <summary>
/// Manages in-memory state for the template designer.
/// Scoped service - one instance per SignalR circuit/user session.
/// </summary>
public class DesignerStateService
{
    // Current template being edited
    public DocumentTemplate? Template { get; private set; }
    public TemplatePage CurrentPage { get; private set; } = new();
    public int CurrentPageIndex { get; private set; }

    // Selection
    public TemplateElement? SelectedElement { get; private set; }
    public List<string> SelectedElementIds { get; } = []; // For multi-select

    // Undo/Redo stacks
    private readonly Stack<DesignerAction> m_undoStack = new();
    private readonly Stack<DesignerAction> m_redoStack = new();

    // Clipboard
    public List<TemplateElement>? Clipboard { get; private set; }

    // Dirty tracking
    public bool IsDirty { get; private set; }

    // Events for component reactivity
    public event Action? OnStateChanged;
    public event Action<TemplateElement?>? OnSelectionChanged;

    // Actions
    public void LoadTemplate(DocumentTemplate template) { ... }
    public void SelectElement(string? elementId) { ... }
    public void AddElement(TemplateElement element) { ... }
    public void UpdateElement(string elementId, Action<TemplateElement> update) { ... }
    public void DeleteElement(string elementId) { ... }
    public void MoveElement(string elementId, decimal x, decimal y) { ... }
    public void ResizeElement(string elementId, decimal width, decimal? height) { ... }

    // Undo/Redo
    public void Undo() { ... }
    public void Redo() { ... }
    public bool CanUndo => m_undoStack.Count > 0;
    public bool CanRedo => m_redoStack.Count > 0;

    // Multi-page
    public void AddPage() { ... }
    public void DeletePage(int pageIndex) { ... }
    public void GoToPage(int pageIndex) { ... }

    // Clipboard
    public void Copy() { ... }
    public void Paste() { ... }
    public void Cut() { ... }

    // Snapshot for save
    public DocumentTemplate GetTemplateSnapshot() { ... }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}

public abstract class DesignerAction
{
    public abstract void Execute(DesignerStateService state);
    public abstract void Undo(DesignerStateService state);
}
```

## Rendering Pipeline

### DocumentRenderService

```csharp
public class DocumentRenderService
{
    private readonly DataModelService m_dataModelService;

    public async Task<string> RenderAgreementAsync(int rentalId, int templateId)
    {
        // 1. Load template
        var template = await LoadTemplateAsync(templateId);

        // 2. Build data model
        var model = await m_dataModelService.BuildAgreementModelAsync(rentalId);

        // 3. Render to HTML
        var html = RenderTemplate(template, model);

        return html;
    }

    private string RenderTemplate(DocumentTemplate template, object dataModel)
    {
        var sb = new StringBuilder();

        // Page wrapper with CSS for print
        sb.AppendLine(RenderPageStyles(template.Page));

        foreach (var page in template.Pages)
        {
            sb.AppendLine("<div class=\"page\">");
            foreach (var element in page.Elements)
            {
                sb.AppendLine(RenderElement(element, dataModel));
            }
            sb.AppendLine("</div>");
        }

        return sb.ToString();
    }

    private string RenderElement(TemplateElement element, object dataModel)
    {
        return element switch
        {
            TextElement text => RenderTextElement(text, dataModel),
            ImageElement img => RenderImageElement(img, dataModel),
            TwoColumnElement cols => RenderTwoColumnElement(cols, dataModel),
            RepeaterElement rep => RenderRepeaterElement(rep, dataModel),
            DividerElement div => RenderDividerElement(div),
            SignatureElement sig => RenderSignatureElement(sig),
            ContainerElement container => RenderContainerElement(container, dataModel),
            _ => $"<!-- Unknown element type: {element.GetType().Name} -->"
        };
    }

    private string RenderTextElement(TextElement text, object dataModel)
    {
        var content = text.Content;

        // Resolve data binding
        if (!string.IsNullOrEmpty(text.DataBinding))
        {
            var value = ResolveBinding(dataModel, text.DataBinding);
            content = FormatValue(value, text.Format);
        }

        // Apply styles
        var style = BuildTextStyle(text);

        return $"<div style=\"{style}\">{HtmlEncode(content)}</div>";
    }

    private string RenderRepeaterElement(RepeaterElement repeater, object dataModel)
    {
        var collection = ResolveBinding(dataModel, repeater.DataSource) as IEnumerable;
        if (collection == null) return "";

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"repeater\">");

        foreach (var item in collection)
        {
            sb.AppendLine("<div class=\"repeater-item\">");
            foreach (var child in repeater.ItemTemplate)
            {
                sb.AppendLine(RenderElement(child, item));
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private object? ResolveBinding(object dataModel, string binding)
    {
        // Handle special bindings
        if (binding == "DateTime.Now") return DateTimeOffset.Now;
        if (binding == "DateTime.Today") return DateOnly.FromDateTime(DateTime.Today);

        // Navigate property path: "Rental.RenterName" -> dataModel.Rental.RenterName
        var parts = binding.Split('.');
        object? current = dataModel;

        foreach (var part in parts)
        {
            if (current == null) return null;
            var prop = current.GetType().GetProperty(part);
            if (prop == null) return null;
            current = prop.GetValue(current);
        }

        return current;
    }
}
```

## PDF Generation Strategy

### Recommended Approach: HTML + Browser Print (Phase 1)

For v1, use browser print (CSS `@media print`) - no external library needed:

```csharp
// DocumentRenderService.cs
public string RenderForPrint(DocumentTemplate template, object dataModel)
{
    return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        @page {{ size: {template.Page.Size} {template.Page.Orientation.ToLower()}; margin: 0; }}
        @media print {{
            .no-print {{ display: none !important; }}
            body {{ margin: 0; padding: 0; }}
        }}
        .page {{
            width: {GetPageWidth(template.Page)}mm;
            min-height: {GetPageHeight(template.Page)}mm;
            padding: {template.Page.MarginTop}mm {template.Page.MarginRight}mm
                     {template.Page.MarginBottom}mm {template.Page.MarginLeft}mm;
            box-sizing: border-box;
            page-break-after: always;
        }}
        /* Element styles... */
    </style>
</head>
<body>
    {RenderTemplate(template, dataModel)}
    <script>window.print();</script>
</body>
</html>";
}
```

### PDF Library Options (Phase 2/3)

| Library | Pros | Cons | Recommendation |
|---------|------|------|----------------|
| **QuestPDF** | .NET native, fluent API, free | Learning curve, different from HTML | Good for simple invoices |
| **PuppeteerSharp** | True HTML→PDF, exact rendering | Heavy (Chromium), slower | Best fidelity, complex setup |
| **wkhtmltopdf** | Fast, mature | Binary dependency, security updates | Legacy, avoid |
| **IronPDF** | Easy, good support | Commercial license | If budget allows |

**Recommendation:** Start with browser print (Phase 1). Add PuppeteerSharp in Phase 3 for true PDF download.

## Database Schema

```sql
-- Template storage in tenant schema
CREATE TABLE [<schema>].[DocumentTemplate]
(
    [DocumentTemplateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [DocumentType] AS CAST(JSON_VALUE([Json], '$.DocumentType') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [IsDefault] AS CAST(JSON_VALUE([Json], '$.IsDefault') AS BIT),
    [IsApproved] AS CAST(JSON_VALUE([Json], '$.IsApproved') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_DocumentTemplate_Type_Status
    ON [<schema>].[DocumentTemplate]([DocumentType], [Status])

CREATE INDEX IX_DocumentTemplate_Default
    ON [<schema>].[DocumentTemplate]([DocumentType], [IsDefault])
    WHERE [IsDefault] = 1
```

## Patterns to Follow

### Pattern 1: Polymorphic Element Serialization

**What:** Use System.Text.Json polymorphism for element types.
**When:** Serializing/deserializing template elements.
**Example:**
```csharp
// Already configured in Entity.cs pattern
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$elementType")]
[JsonDerivedType(typeof(TextElement), "Text")]
// ... other types
public abstract class TemplateElement { }

// Serialization includes type discriminator
// {"$elementType":"Text","Content":"Hello","DataBinding":"Renter.FullName",...}
```

### Pattern 2: Scoped Designer State

**What:** One DesignerStateService per user session.
**When:** Managing editor state across components.
**Example:**
```csharp
// Registration in Program.cs
builder.Services.AddScoped<DesignerStateService>();

// Component injection
@inject DesignerStateService Designer

// Subscribe to changes
protected override void OnInitialized()
{
    Designer.OnStateChanged += StateHasChanged;
    Designer.OnSelectionChanged += OnSelectionChanged;
}
```

### Pattern 3: Command Pattern for Undo/Redo

**What:** Encapsulate actions as reversible commands.
**When:** Any user action that modifies template state.
**Example:**
```csharp
public class MoveElementAction : DesignerAction
{
    private readonly string m_elementId;
    private readonly decimal m_oldX, m_oldY;
    private readonly decimal m_newX, m_newY;

    public override void Execute(DesignerStateService state)
    {
        state.SetElementPosition(m_elementId, m_newX, m_newY);
    }

    public override void Undo(DesignerStateService state)
    {
        state.SetElementPosition(m_elementId, m_oldX, m_oldY);
    }
}
```

## Anti-Patterns to Avoid

### Anti-Pattern 1: Component-Level State

**What:** Storing template state in individual Blazor components.
**Why bad:** State lost on re-render, hard to coordinate undo/redo.
**Instead:** Use centralized DesignerStateService.

### Anti-Pattern 2: Rendering in Designer

**What:** Using DocumentRenderService inside the designer canvas.
**Why bad:** Different rendering needs - designer needs interactive elements, print needs static HTML.
**Instead:** Separate designer rendering (interactive) from document rendering (static).

### Anti-Pattern 3: Deep Entity Nesting

**What:** Loading entire entity graph (Rental→Renter→Documents→...) for rendering.
**Why bad:** Performance, over-fetching, circular references in JSON.
**Instead:** Use flat DataModel classes (AgreementModel) that cherry-pick needed fields.

### Anti-Pattern 4: String Concatenation for HTML

**What:** Building HTML with string concatenation in render service.
**Why bad:** XSS vulnerabilities, messy code.
**Instead:** Use proper HTML encoding, consider Razor templating for complex layouts.

## Suggested Build Order

Based on dependencies, build in this order:

```
Phase 1: Foundation (No dependencies)
├── 1.1 TemplateElement classes (polymorphic JSON)
├── 1.2 DocumentTemplate entity
├── 1.3 Database schema
├── 1.4 Repository registration
└── 1.5 TemplateService (CRUD)

Phase 2: Data Binding Models (Depends on Phase 1)
├── 2.1 AgreementModel, ReceiptModel, BookingConfirmationModel
├── 2.2 DataModelService (builds models from entities)
└── 2.3 Binding path resolution

Phase 3: Designer Core (Depends on Phase 1, 2)
├── 3.1 DesignerStateService (state management)
├── 3.2 Designer Canvas (element rendering)
├── 3.3 Elements Palette (drag source)
└── 3.4 Properties Panel (element configuration)

Phase 4: Designer UX (Depends on Phase 3)
├── 4.1 Drag-and-drop (JS interop)
├── 4.2 Element selection and resize
├── 4.3 Undo/Redo
├── 4.4 Multi-page navigation
└── 4.5 Data binding picker UI

Phase 5: Rendering (Depends on Phase 2)
├── 5.1 DocumentRenderService (template → HTML)
├── 5.2 Print preview page
├── 5.3 Browser print integration
└── 5.4 Template selector in print dialogs

Phase 6: AI Features (Depends on Phase 1, existing Gemini integration)
├── 6.1 ClauseService (AI clause suggestions)
└── 6.2 Clause picker UI in designer

Phase 7: PDF Export (Depends on Phase 5)
├── 7.1 PuppeteerSharp integration
└── 7.2 PDF download endpoint
```

## Integration Points

### Existing Codebase Integration

| Integration Point | Location | Change Required |
|-------------------|----------|-----------------|
| **Entity.cs** | `src/MotoRent.Domain/Entities/Entity.cs` | Add `[JsonDerivedType(typeof(DocumentTemplate), nameof(DocumentTemplate))]` |
| **Repository Registration** | `src/MotoRent.Server/Program.cs` | Add `services.AddSingleton<IRepository<DocumentTemplate>, ...>()` |
| **Organization Settings** | `Pages/Settings/` | Add "Document Templates" menu item |
| **Print Dialogs** | `Pages/Rentals/`, `Pages/InvoiceDialog.razor` | Add template selector dropdown |
| **RentalService** | `src/MotoRent.Services/RentalService.cs` | No change - accessed via DataModelService |

### New Files to Create

```
src/MotoRent.Domain/
├── Entities/
│   ├── DocumentTemplate.cs
│   └── TemplateElements/
│       ├── TemplateElement.cs
│       ├── TextElement.cs
│       ├── ImageElement.cs
│       ├── TwoColumnElement.cs
│       ├── RepeaterElement.cs
│       └── ... (other elements)
├── Models/
│   └── Documents/
│       ├── AgreementModel.cs
│       ├── ReceiptModel.cs
│       └── BookingConfirmationModel.cs

src/MotoRent.Services/
├── TemplateService.cs
├── DataModelService.cs
├── DocumentRenderService.cs
└── ClauseService.cs

src/MotoRent.Client/
├── Pages/
│   └── Settings/
│       ├── TemplateList.razor
│       └── TemplateDesigner.razor
├── Components/
│   └── Designer/
│       ├── DesignerCanvas.razor
│       ├── ElementsPalette.razor
│       ├── PropertiesPanel.razor
│       ├── PageNavigator.razor
│       └── DataBindingPicker.razor
├── Services/
│   └── DesignerStateService.cs

database/
└── tables/
    └── MotoRent.DocumentTemplate.sql
```

## Scalability Considerations

| Concern | At 10 Templates | At 100 Templates | At 1000 Templates |
|---------|-----------------|------------------|-------------------|
| Query performance | N/A | Index on DocumentType | Pagination, search |
| JSON size | ~10KB each | ~100KB total | Split large templates |
| Render time | Instant | <100ms | Cache rendered HTML |
| Memory (designer) | ~1MB | ~10MB | Virtualized element list |

## Sources

- Existing MotoRent codebase patterns (Entity, Repository, DataContext)
- PROJECT.md requirements
- PrintAgreement.razor (existing hardcoded template pattern)
- InvoiceService.cs (data model building pattern)
- System.Text.Json polymorphism documentation

---

*Architecture research: 2026-01-23*
