# Technology Stack: Document Template Editor

**Project:** MotoRent Document Template Editor
**Researched:** 2026-01-23
**Confidence:** MEDIUM (based on training data + project analysis; web verification unavailable)

## Executive Summary

The document template editor requires four key capabilities:
1. **Drag-and-drop designer** - For visual template layout
2. **Template storage/binding** - For storing templates and binding data
3. **HTML rendering** - For screen preview and browser print
4. **PDF generation** - For downloadable documents

The recommended stack leverages existing project patterns while adding minimal new dependencies.

---

## Recommended Stack

### Drag-and-Drop: Native HTML5 + SortableJS Interop

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| Native HTML5 Drag API | N/A | Element repositioning on canvas | HIGH |
| SortableJS | 1.15.x | List reordering, palette-to-canvas drops | MEDIUM |
| Custom JS Interop | N/A | Blazor-JS bridge for drag operations | HIGH |

**Rationale:**
- The project already uses native drag events (see `VehicleRecognitionPanel.razor` with `@ondragenter`, `@ondrop`)
- Existing patterns: `file-upload.js`, `google-map.js` demonstrate ES module interop
- SortableJS is lightweight (10KB gzipped), framework-agnostic, MIT licensed
- Works identically in Blazor Server and WASM modes

**Why NOT MudBlazor.Extensions or Blazored.DragDrop:**
- Project has migrated AWAY from MudBlazor to Tabler CSS (see `mudblazor-to-tabler-migration.md`)
- Adding MudBlazor back would conflict with Tabler CSS styling
- Blazored.DragDrop has limited maintenance and no .NET 10 verification

**Implementation Pattern (matching existing interops):**
```javascript
// wwwroot/scripts/template-designer.js - ES module pattern
let designers = new Map();

export function initDesigner(canvasElement, paletteElement, dotNetRef) {
    if (!canvasElement || !paletteElement) return;

    const config = {
        canvas: new Sortable(canvasElement, {
            group: 'template-elements',
            animation: 150,
            onEnd: async (evt) => {
                await dotNetRef.invokeMethodAsync('OnElementDropped', {
                    elementId: evt.item.dataset.elementId,
                    fromPalette: evt.from === paletteElement,
                    newIndex: evt.newIndex
                });
            }
        }),
        palette: new Sortable(paletteElement, {
            group: { name: 'template-elements', pull: 'clone', put: false },
            sort: false
        })
    };

    designers.set(canvasElement, config);
}

export function updateElementPosition(canvasElement, elementId, x, y) {
    const element = canvasElement.querySelector(`[data-element-id="${elementId}"]`);
    if (element) {
        element.style.left = `${x}px`;
        element.style.top = `${y}px`;
    }
}

export function dispose(canvasElement) {
    const config = designers.get(canvasElement);
    if (config) {
        config.canvas.destroy();
        config.palette.destroy();
        designers.delete(canvasElement);
    }
}
```

---

### PDF Generation: QuestPDF

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| QuestPDF | 2024.12.x+ | Server-side PDF generation | MEDIUM |

**Rationale:**
- Fluent C# API - no HTML-to-PDF conversion complexity
- MIT licensed for revenue under $1M USD/year (Community License)
- Pure .NET - no native dependencies, works on Windows/Linux Docker
- Excellent for structured documents (receipts, agreements)
- Strong community adoption in .NET ecosystem as of 2024-2025

**Why NOT iText:**
- AGPL license requires open-sourcing code OR expensive commercial license
- Overkill for template-based documents

**Why NOT PdfSharp:**
- Lower-level API, more code for same result
- Less active development, .NET 10 compatibility uncertain

**Why NOT Syncfusion/Telerik:**
- Commercial licensing costs
- Heavy dependencies, not suitable for this project scale

**Why NOT Puppeteer/Playwright HTML-to-PDF:**
- Requires headless Chromium browser (heavy deployment, 300MB+ image size)
- Complex in containerized environments
- Slower generation (browser startup overhead)
- But provides exact WYSIWYG fidelity if needed in Phase 3

**Implementation Pattern:**
```csharp
public class PdfExportService
{
    private readonly ILogger<PdfExportService> m_logger;

    public byte[] GeneratePdf(DocumentTemplate template, AgreementModel data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(GetPageSize(template.Page.Size));
                page.Margin(template.Page.MarginTop, Unit.Millimetre);

                page.Content().Column(col =>
                {
                    foreach (var element in template.Elements)
                    {
                        RenderElement(col.Item(), element, data);
                    }
                });
            });
        }).GeneratePdf();
    }

    private PageSize GetPageSize(string size) => size switch
    {
        "Letter" => PageSizes.Letter,
        _ => PageSizes.A4
    };

    private void RenderElement(IContainer container, TemplateElement element, object data)
    {
        switch (element)
        {
            case TextElement text:
                var content = ResolveBinding(data, text.DataBinding) ?? text.Content;
                container.Text(content)
                    .FontSize((float)text.FontSize)
                    .FontFamily(text.FontFamily);
                break;
            // ... other element types
        }
    }
}
```

**Thai Font Support:**
```csharp
// Configure Thai fonts at startup
FontManager.RegisterFontFromFile("wwwroot/fonts/THSarabunNew.ttf");
FontManager.RegisterFontFromFile("wwwroot/fonts/NotoSansThai-Regular.ttf");
```

---

### Template Storage: JSON in SQL Server

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| System.Text.Json | .NET 10 built-in | Template serialization | HIGH |
| SQL Server JSON column | Existing | Template persistence | HIGH |

**Rationale:**
- Matches existing project patterns exactly (see `CLAUDE.md` Entity Pattern)
- JSON column with computed columns for indexing
- System.Text.Json polymorphism for element type hierarchy
- No new dependencies - uses existing `RentalDataContext` and Repository pattern

**Entity Design (matching existing patterns):**
```csharp
public class DocumentTemplate : Entity
{
    public int DocumentTemplateId { get; set; }

    // Indexed via computed columns
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "Agreement"; // Agreement, Receipt, BookingConfirmation
    public bool IsDefault { get; set; }
    public bool IsApproved { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, Archived

    // Full template definition stored in JSON
    public PageSettings Page { get; set; } = new();
    public List<TemplateElement> Elements { get; set; } = [];

    public override int GetId() => DocumentTemplateId;
    public override void SetId(int value) => DocumentTemplateId = value;
}
```

**SQL Table (matching existing convention):**
```sql
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

CREATE INDEX IX_DocumentTemplate_Type_Default
    ON [<schema>].[DocumentTemplate]([DocumentType], [IsDefault])
```

---

### Template Engine: Expression-Based Data Binding

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| Custom expression evaluator | N/A | Data binding resolution | HIGH |
| Reflection-based property access | .NET 10 | Dynamic property lookup | HIGH |

**Rationale:**
- Simple path expressions (`Rental.RenterName`, `Vehicle.LicensePlate`)
- No need for full template engine (Handlebars, Scriban, etc.)
- Faster and simpler than external libraries
- Type-safe at design time via context models

**Why NOT Handlebars.NET or Scriban:**
- Overkill for property path resolution
- Additional parsing overhead per render
- Less control over error handling and null propagation

**Implementation Pattern:**
```csharp
public class DataBinder
{
    private readonly ConcurrentDictionary<string, Func<object, object?>> m_accessorCache = new();

    public string? ResolveBinding(string path, object context)
    {
        // Handle special bindings
        if (path == "DateTime.Now")
            return DateTimeOffset.Now.ToString("dd MMMM yyyy");
        if (path == "DateTime.Today")
            return DateOnly.FromDateTime(DateTime.Today).ToString("dd MMMM yyyy");

        var accessor = m_accessorCache.GetOrAdd(path, BuildAccessor);
        var value = accessor(context);
        return value?.ToString() ?? "";
    }

    private Func<object, object?> BuildAccessor(string path)
    {
        var parts = path.Split('.');
        return (context) =>
        {
            object? current = context;
            foreach (var part in parts)
            {
                if (current == null) return null;
                var prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }
            return current;
        };
    }

    public string FormatValue(object? value, string? format)
    {
        if (value == null) return "";
        return format switch
        {
            "d" => value is DateTimeOffset dto ? dto.ToString("dd/MM/yyyy") : value.ToString()!,
            "D" => value is DateTimeOffset dto ? dto.ToString("dd MMMM yyyy") : value.ToString()!,
            "C" => value is decimal d ? $"{d:N0} THB" : value.ToString()!,
            _ => value.ToString()!
        };
    }
}
```

---

### Canvas/WYSIWYG: Custom Blazor Components

| Technology | Version | Purpose | Confidence |
|------------|---------|---------|------------|
| Blazor components | .NET 10 | Designer UI | HIGH |
| CSS Grid/Flexbox | N/A | Layout engine | HIGH |
| Tabler CSS | 1.4.0 | Styling (existing) | HIGH |

**Rationale:**
- No external WYSIWYG library needed
- Designer is specialized, not general rich text editing
- Blazor components provide full control
- Matches existing project patterns exactly

**Why NOT TinyMCE/CKEditor/Quill:**
- These are rich text editors, not document designers
- Wrong tool for the job
- Heavy dependencies and licensing complexity

**Component Architecture:**
```
src/MotoRent.Client/
├── Pages/Settings/
│   ├── TemplateList.razor          # Template CRUD list
│   └── TemplateDesigner.razor      # Main designer page (full-screen)
├── Components/Designer/
│   ├── DesignerCanvas.razor        # Center canvas with drop zones
│   ├── ElementsPalette.razor       # Left sidebar with draggable elements
│   ├── PropertiesPanel.razor       # Right sidebar for element config
│   ├── PageNavigator.razor         # Multi-page navigation
│   ├── DataBindingPicker.razor     # Field picker dialog
│   └── Elements/
│       ├── TextElementDesigner.razor
│       ├── ImageElementDesigner.razor
│       ├── ContainerDesigner.razor
│       ├── RepeaterDesigner.razor
│       ├── DividerDesigner.razor
│       └── SignatureDesigner.razor
├── Services/
│   └── DesignerStateService.cs     # Scoped state management
```

---

## Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| SortableJS | 1.15.x | Drag-drop sorting | Palette-to-canvas, element reordering |
| QuestPDF | 2024.12.x | PDF generation | Download button, email attachments |
| (existing) signature-pad.js | N/A | Signature capture | Signature element if needed |

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Drag-Drop | SortableJS + Native | MudBlazor.Extensions | Project migrated away from MudBlazor |
| Drag-Drop | SortableJS + Native | Blazored.DragDrop | Limited maintenance, .NET 10 unverified |
| Drag-Drop | SortableJS + Native | Native-only | SortableJS handles edge cases (touch, scrolling) better |
| PDF | QuestPDF | iText | AGPL license issues |
| PDF | QuestPDF | Puppeteer | Heavy browser dependency (300MB+) |
| PDF | QuestPDF | PdfSharp | Lower-level API, less active |
| Template Engine | Custom binding | Scriban | Overkill for property paths |
| Template Engine | Custom binding | Handlebars.NET | Overkill, less control |
| WYSIWYG | Custom components | TinyMCE | Wrong tool - text editor not doc designer |

---

## Installation

### NuGet Packages (Server project)
```xml
<!-- Add to MotoRent.Services.csproj -->
<PackageReference Include="QuestPDF" Version="2024.12.0" />
```

### NPM/CDN for SortableJS
```html
<!-- Option 1: CDN (quick start) -->
<script src="https://cdn.jsdelivr.net/npm/sortablejs@1.15.3/Sortable.min.js"></script>

<!-- Option 2: Local (recommended for production) -->
<!-- Download to wwwroot/lib/sortablejs/Sortable.min.js -->
<script src="lib/sortablejs/Sortable.min.js"></script>
```

### Thai Fonts for PDF
```
wwwroot/fonts/
├── THSarabunNew.ttf           # Thai government standard font
├── THSarabunNew-Bold.ttf
├── NotoSansThai-Regular.ttf   # Google Noto (fallback)
└── NotoSansThai-Bold.ttf
```

---

## Integration Points

### With Existing Project Patterns

| Existing Pattern | How Template Editor Uses It |
|------------------|---------------------------|
| `Entity` base class | `DocumentTemplate : Entity` |
| `RentalDataContext` | Template CRUD, session management |
| `Repository<T>` pattern | `IRepository<DocumentTemplate>` |
| `DialogService` | Properties panel dialogs, data picker |
| `ToastService` | Save confirmation, error messages |
| JSON polymorphism | Element type hierarchy |
| `LocalizedComponentBase<T>` | Designer UI localization |
| Print styling (`@media print`) | Document preview |
| JS Interop pattern | `DesignerJsInterop.cs` matching `GoogleMapJsInterop.cs` |

### With Existing Entities

| Entity | Template Context Model |
|--------|----------------------|
| Rental | `AgreementModel.Rental.*` |
| Renter | `AgreementModel.Renter.*` |
| Vehicle | `AgreementModel.Vehicle.*` |
| Shop | `AgreementModel.Shop.*` |
| Organization | `AgreementModel.Organization.*` |
| Payment | `ReceiptModel.Payment.*` |
| Booking | `BookingConfirmationModel.Booking.*` |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SortableJS Blazor interop complexity | Medium | Medium | Follow existing patterns (GoogleMapJsInterop, file-upload.js) |
| QuestPDF licensing change | Low | High | Community license covers <$1M revenue; monitor announcements |
| Thai font rendering in PDF | Medium | High | Test with Thai samples early (Phase 2), embed full fonts |
| Complex element nesting | Medium | Medium | Limit nesting to 2 levels for v1 |
| Preview vs PDF mismatch | Medium | Medium | Use same data binding engine; test side-by-side |

---

## Verification Needed

Before implementation, verify:

1. **QuestPDF .NET 10 compatibility** - Check NuGet for net10.0 target
2. **SortableJS touch support** - Test on mobile/tablet for staff PWA use
3. **Thai font embedding** - Test "กรุงเทพมหานคร" with tone marks renders correctly
4. **PDF rendering accuracy** - Prototype complex layouts early (Phase 1 spike)

---

## Sources

| Claim | Source | Confidence |
|-------|--------|------------|
| Project uses Tabler CSS (not MudBlazor) | `.claude/plans/mudblazor-to-tabler-migration.md` | HIGH |
| Native drag events work in project | `VehicleRecognitionPanel.razor` lines 24-29 | HIGH |
| JS interop ES module pattern | `GoogleMapJsInterop.cs`, `file-upload.js` | HIGH |
| JSON polymorphism available | Entity.cs pattern, CLAUDE.md | HIGH |
| Repository pattern | `RentalDataContext`, existing services | HIGH |
| QuestPDF recommendation | Training data (2024-2025 .NET ecosystem) | MEDIUM |
| SortableJS recommendation | Training data (stable library, wide adoption) | MEDIUM |

---

## Summary

**New dependencies (minimal):**
- QuestPDF (NuGet) - PDF generation
- SortableJS (JS, 10KB) - Drag-drop enhancement

**Leverages existing patterns:**
- Native HTML5 drag events (already in codebase)
- ES module JS interop (GoogleMapJsInterop pattern)
- Entity + JSON columns (existing convention)
- DialogService/ToastService (existing services)
- Tabler CSS styling (existing framework)
- LocalizedComponentBase (existing localization)

**Key architectural decisions:**
1. **Dual-render approach:** Blazor for preview, QuestPDF for PDF (different but complementary)
2. **Expression-based binding:** Simple property paths, not full template engine
3. **Custom designer components:** No external WYSIWYG library
4. **SortableJS for complex drag-drop:** Native for simple cases, SortableJS for lists and cross-container
5. **Flat element list with containers:** Max 2 levels of nesting (Template -> Container -> Elements)
