# Phase 1: Designer Foundation - Research

**Researched:** 2026-01-23
**Domain:** Drag-and-Drop Designer UI with Blazor/JavaScript Interop
**Confidence:** HIGH

## Summary

Phase 1 establishes the visual document template designer - a canvas where OrgAdmin users can drag elements from a palette, reorder them, and configure their properties. The core challenge is synchronizing state between Blazor's virtual DOM and JavaScript's DOM manipulation during drag operations.

The research confirms that the existing project patterns (ES module JS interop, native HTML5 drag events, JSON polymorphism) provide a solid foundation. SortableJS 1.15.x is the recommended drag-and-drop library due to its lightweight footprint (10KB), touch support, and proven integration patterns with Blazor via the BlazorSortable approach.

**Primary recommendation:** Use SortableJS with Blazor interop following the "JS is source of truth during drag, Blazor is source of truth at rest" pattern. Cancel JS DOM mutations and let Blazor re-render based on updated list state.

## Standard Stack

The established libraries and tools for this phase:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SortableJS | 1.15.3 | Drag-and-drop reordering | 10KB, framework-agnostic, MIT license, touch support |
| System.Text.Json | .NET 10 built-in | Element serialization with polymorphism | Already used in project for Entity serialization |
| Tabler CSS | 1.0.0-beta20 | UI framework | Project standard (migrated from MudBlazor) |
| Tabler Icons | N/A | Element palette icons | Project standard |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| CSS Flexbox/Grid | N/A | Canvas layout | Element positioning on canvas |
| Blazor ElementReference | .NET 10 | JS interop element targeting | Passing DOM elements to JS |
| DotNetObjectReference | .NET 10 | Blazor callback from JS | JS notifying Blazor of drag events |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SortableJS | Native HTML5 Drag API only | SortableJS handles edge cases (touch, scrolling, animations) better |
| SortableJS | Blazored.DragDrop | Limited maintenance, .NET 10 unverified |
| SortableJS | MudBlazor.Extensions | Project migrated away from MudBlazor |
| Custom interop | BlazorSortableList NuGet | More control with custom interop, matches existing patterns |

**Installation:**
```html
<!-- In _Host.cshtml or index.html -->
<!-- Download to wwwroot/lib/sortablejs/Sortable.min.js -->
<script src="lib/sortablejs/Sortable.min.js"></script>
```

Download from: https://cdn.jsdelivr.net/npm/sortablejs@1.15.3/Sortable.min.js

## Architecture Patterns

### Recommended Project Structure
```
src/MotoRent.Client/
├── Components/Designer/
│   ├── DesignerCanvas.razor           # Main canvas with drop zones
│   ├── DesignerCanvas.razor.cs        # Code-behind
│   ├── DesignerCanvas.razor.css       # Scoped styles
│   ├── ElementsPalette.razor          # Left sidebar with draggable element types
│   ├── ElementsPalette.razor.cs
│   ├── PropertiesPanel.razor          # Right sidebar for element config
│   ├── PropertiesPanel.razor.cs
│   └── Elements/                      # Individual element renderers
│       ├── TextElementView.razor
│       ├── ImageElementView.razor
│       ├── ContainerElementView.razor
│       ├── DividerElementView.razor
│       ├── DateElementView.razor
│       └── SignatureElementView.razor
├── Interops/
│   └── DesignerJsInterop.cs           # SortableJS interop (follows GoogleMapJsInterop pattern)
├── Pages/Settings/
│   └── TemplateDesigner.razor         # Full designer page
└── wwwroot/scripts/
    └── template-designer.js           # ES module for SortableJS

src/MotoRent.Domain/
└── Templates/                         # New folder for template entities
    ├── DocumentTemplate.cs            # Main entity
    ├── TemplateElement.cs             # Polymorphic element base
    ├── Elements/
    │   ├── TextElement.cs
    │   ├── ImageElement.cs
    │   ├── ContainerElement.cs
    │   ├── DividerElement.cs
    │   ├── DateElement.cs
    │   └── SignatureElement.cs
    └── PageSettings.cs                # Paper size, margins
```

### Pattern 1: JS is Source of Truth During Drag
**What:** During active drag operations, JavaScript manages DOM state. Blazor only updates when drag completes.
**When to use:** All drag-and-drop interactions
**Example:**
```javascript
// Source: BlazorSortable pattern - https://github.com/the-urlist/BlazorSortable
export function initCanvas(canvasElement, dotNetRef) {
    new Sortable(canvasElement, {
        group: 'template-elements',
        animation: 150,
        ghostClass: 'element-ghost',
        chosenClass: 'element-chosen',
        onEnd: async (evt) => {
            // CRITICAL: Cancel the DOM move - let Blazor re-render
            if (evt.from === evt.to) {
                // Reorder within canvas
                await dotNetRef.invokeMethodAsync('OnElementReordered', {
                    elementId: evt.item.dataset.elementId,
                    oldIndex: evt.oldIndex,
                    newIndex: evt.newIndex
                });
            }
        },
        onAdd: async (evt) => {
            // Element added from palette
            const elementType = evt.item.dataset.elementType;
            const newIndex = evt.newIndex;

            // Remove the cloned DOM element - Blazor will re-render
            evt.item.remove();

            await dotNetRef.invokeMethodAsync('OnElementAdded', {
                elementType: elementType,
                index: newIndex
            });
        }
    });
}
```

### Pattern 2: Clone from Palette (Source List)
**What:** Palette elements are cloned (not moved) when dragged to canvas
**When to use:** Element palette to canvas drops
**Example:**
```javascript
// Source: SortableJS docs - https://github.com/SortableJS/Sortable
export function initPalette(paletteElement, canvasElement) {
    new Sortable(paletteElement, {
        group: {
            name: 'template-elements',
            pull: 'clone',      // Clone items, don't move
            put: false          // Don't allow drops into palette
        },
        sort: false,            // Disable reordering in palette
        animation: 150
    });
}
```

### Pattern 3: C# Interop Class (Following Existing Pattern)
**What:** Typed C# wrapper for JS interop
**When to use:** All JS interactions
**Example:**
```csharp
// Source: Existing GoogleMapJsInterop.cs pattern in project
public class DesignerJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> m_moduleTask;
    private bool m_disposed;

    public DesignerJsInterop(IJSRuntime jsRuntime)
    {
        m_moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./scripts/template-designer.js").AsTask());
    }

    public async Task InitCanvasAsync(ElementReference canvas, ElementReference palette,
        DotNetObjectReference<DesignerCanvas> dotNetRef)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("initDesigner", canvas, palette, dotNetRef);
    }

    public async Task SelectElementAsync(string elementId)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("selectElement", elementId);
    }

    public async ValueTask DisposeAsync()
    {
        if (m_disposed) return;
        m_disposed = true;

        if (m_moduleTask.IsValueCreated)
        {
            try
            {
                var module = await m_moduleTask.Value;
                await module.InvokeVoidAsync("dispose");
                await module.DisposeAsync();
            }
            catch { /* Ignore disposal errors */ }
        }
    }
}
```

### Pattern 4: Polymorphic Element Model
**What:** Separate class hierarchy for template elements with JSON type discriminator
**When to use:** Element type serialization
**Example:**
```csharp
// Source: Existing Entity.cs polymorphism pattern in project
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextElement), nameof(TextElement))]
[JsonDerivedType(typeof(ImageElement), nameof(ImageElement))]
[JsonDerivedType(typeof(ContainerElement), nameof(ContainerElement))]
[JsonDerivedType(typeof(DividerElement), nameof(DividerElement))]
[JsonDerivedType(typeof(DateElement), nameof(DateElement))]
[JsonDerivedType(typeof(SignatureElement), nameof(SignatureElement))]
public abstract class TemplateElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? Label { get; set; }

    // Common positioning/sizing
    public int? Width { get; set; }           // null = 100%
    public int? Height { get; set; }          // null = auto
    public string? Alignment { get; set; }    // left, center, right
    public int MarginTop { get; set; }
    public int MarginBottom { get; set; }
}
```

### Anti-Patterns to Avoid
- **Mutating Blazor-controlled DOM from JS:** Never add/remove DOM elements that Blazor tracks. Always let Blazor re-render by updating C# state.
- **Storing selection state in JS:** Keep all application state (selected element, property values) in Blazor components.
- **Using global JS variables:** Use ES modules with Map-based instance tracking (see google-map.js pattern).
- **Deeply nested containers:** Limit to 2 levels (Template -> Container -> Elements) for v1.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Drag-and-drop reordering | Custom mouse event handlers | SortableJS | Touch support, animations, scroll handling, edge cases |
| Element unique IDs | Sequential integers | `Guid.NewGuid().ToString("N")[..8]` | Collision-free during drag operations |
| JSON type discrimination | Manual `$type` field | `[JsonPolymorphic]` attributes | Framework handles serialization/deserialization |
| Property panel forms | Custom input bindings | Standard `@bind` with `StateHasChanged` | Blazor's built-in binding is sufficient |
| CSS scoped styling | BEM naming conventions | `.razor.css` isolation | Blazor automatically scopes styles |

**Key insight:** The project already has proven patterns for JS interop (GoogleMapJsInterop, file-upload.js), polymorphic JSON (Entity.cs), and component state management. Follow these patterns rather than inventing new ones.

## Common Pitfalls

### Pitfall 1: Blazor/JS State Desync
**What goes wrong:** Dragging an element causes JS to move DOM nodes, but Blazor's virtual DOM doesn't know about it. Next render produces duplicates or missing elements.
**Why it happens:** SortableJS physically moves DOM elements; Blazor expects to control DOM based on component state.
**How to avoid:**
1. In `onAdd` event: Remove the cloned DOM element immediately (`evt.item.remove()`)
2. In `onEnd` event: Don't physically move elements - just notify Blazor of the new index
3. Let Blazor re-render by calling `StateHasChanged()` after updating the element list
**Warning signs:** Elements duplicating on drag, elements disappearing, drag ghost staying visible

### Pitfall 2: Lost Element Selection on Re-render
**What goes wrong:** User selects an element, edits a property, element list re-renders, selection visual state is lost.
**Why it happens:** Selection is stored in JS/CSS, not in Blazor state.
**How to avoid:** Store `SelectedElementId` as C# property, apply selection CSS class via Blazor rendering (`class="@(IsSelected ? "selected" : "")"`)
**Warning signs:** Blue selection border disappears after property change

### Pitfall 3: Nested Sortable Conflicts
**What goes wrong:** Dropping an element into a container also triggers the parent canvas drop handler.
**Why it happens:** Event bubbling in nested Sortable instances.
**How to avoid:** Use `fallbackOnBody: true` and `swapThreshold: 0.65` for nested sortables. Use different group names if movement between levels should be restricted.
**Warning signs:** Element appears in both container and canvas, drop targets highlight incorrectly

### Pitfall 4: Touch Drag Delay Misinterpreted as Click
**What goes wrong:** On mobile, tapping an element to select it sometimes starts a drag instead.
**Why it happens:** SortableJS touch drag starts immediately by default.
**How to avoid:** Set `delay: 150` and `touchStartThreshold: 5` to distinguish tap from drag.
**Warning signs:** Users report elements "jumping" when they try to tap/select on tablets

### Pitfall 5: CSS Isolation and ::deep
**What goes wrong:** Styles defined in parent component's `.razor.css` don't apply to child sortable elements.
**Why it happens:** Blazor CSS isolation scopes styles to the declaring component only.
**How to avoid:** Wrap SortableList in a container element and use `::deep` modifier for child styling.
**Warning signs:** Drop ghost has wrong styling, selected elements don't show border

## Code Examples

Verified patterns from official sources and existing project code:

### DesignerCanvas.razor - Core Component Structure
```razor
@* Source: Follows VehicleRecognitionPanel.razor drag pattern *@
@inherits LocalizedComponentBase<DesignerCanvas>
@implements IAsyncDisposable
@inject IJSRuntime JsRuntime

<div class="designer-layout">
    <div class="elements-palette" @ref="m_paletteRef">
        @foreach (var elementType in ElementTypes)
        {
            <div class="palette-item" data-element-type="@elementType.TypeName">
                <i class="@elementType.Icon"></i>
                <span>@Localizer[elementType.Label]</span>
            </div>
        }
    </div>

    <div class="designer-canvas" @ref="m_canvasRef">
        @foreach (var element in Template.Elements)
        {
            <div class="canvas-element @(element.Id == SelectedElementId ? "selected" : "")"
                 data-element-id="@element.Id"
                 @onclick="() => SelectElement(element.Id)">
                @RenderElement(element)
            </div>
        }
    </div>

    <div class="properties-panel">
        @if (SelectedElement is not null)
        {
            <PropertiesPanel Element="@SelectedElement"
                            OnPropertyChanged="OnPropertyChanged" />
        }
    </div>
</div>

@code {
    private ElementReference m_canvasRef;
    private ElementReference m_paletteRef;
    private DesignerJsInterop? m_jsInterop;
    private DotNetObjectReference<DesignerCanvas>? m_dotNetRef;

    [Parameter] public DocumentTemplate Template { get; set; } = new();
    [Parameter] public EventCallback<DocumentTemplate> TemplateChanged { get; set; }

    private string? SelectedElementId { get; set; }
    private TemplateElement? SelectedElement =>
        Template.Elements.FirstOrDefault(e => e.Id == SelectedElementId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            m_jsInterop = new DesignerJsInterop(JsRuntime);
            m_dotNetRef = DotNetObjectReference.Create(this);
            await m_jsInterop.InitCanvasAsync(m_canvasRef, m_paletteRef, m_dotNetRef);
        }
    }

    [JSInvokable]
    public async Task OnElementReordered(ElementReorderArgs args)
    {
        var element = Template.Elements.First(e => e.Id == args.ElementId);
        Template.Elements.Remove(element);
        Template.Elements.Insert(args.NewIndex, element);
        await TemplateChanged.InvokeAsync(Template);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnElementAdded(ElementAddArgs args)
    {
        var element = CreateElement(args.ElementType);
        Template.Elements.Insert(args.Index, element);
        SelectedElementId = element.Id;
        await TemplateChanged.InvokeAsync(Template);
        StateHasChanged();
    }

    private void SelectElement(string elementId)
    {
        SelectedElementId = elementId;
    }

    private void OnPropertyChanged()
    {
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        m_dotNetRef?.Dispose();
        if (m_jsInterop is not null)
            await m_jsInterop.DisposeAsync();
    }
}
```

### template-designer.js - ES Module Pattern
```javascript
// Source: Follows google-map.js pattern in project
let designers = new Map();

export function initDesigner(canvasElement, paletteElement, dotNetRef) {
    if (!canvasElement || !paletteElement) {
        console.warn('Designer elements not found');
        return;
    }

    const config = {
        canvas: new Sortable(canvasElement, {
            group: 'template-elements',
            animation: 150,
            ghostClass: 'element-ghost',
            chosenClass: 'element-chosen',
            dragClass: 'element-drag',
            handle: '.drag-handle',  // Optional: restrict drag to handle
            fallbackOnBody: true,    // For nested support
            swapThreshold: 0.65,
            delay: 150,              // Touch drag delay
            touchStartThreshold: 5,
            onEnd: async (evt) => {
                if (evt.from === evt.to) {
                    await dotNetRef.invokeMethodAsync('OnElementReordered', {
                        elementId: evt.item.dataset.elementId,
                        oldIndex: evt.oldIndex,
                        newIndex: evt.newIndex
                    });
                }
            },
            onAdd: async (evt) => {
                const elementType = evt.item.dataset.elementType;
                const newIndex = evt.newIndex;

                // Remove cloned element - Blazor will re-render
                evt.item.remove();

                await dotNetRef.invokeMethodAsync('OnElementAdded', {
                    elementType: elementType,
                    index: newIndex
                });
            }
        }),
        palette: new Sortable(paletteElement, {
            group: {
                name: 'template-elements',
                pull: 'clone',
                put: false
            },
            sort: false,
            animation: 150
        }),
        dotNetRef: dotNetRef
    };

    designers.set(canvasElement, config);
}

export function selectElement(canvasElement, elementId) {
    // CSS-based selection is handled by Blazor
    // This function reserved for future JS-side selection effects
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

### DocumentTemplate Entity
```csharp
// Source: Follows existing Entity pattern with JSON columns
public class DocumentTemplate : Entity
{
    public int DocumentTemplateId { get; set; }

    // Indexed properties (computed columns in SQL)
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "Agreement"; // Agreement, Receipt, BookingConfirmation
    public string Status { get; set; } = "Draft";           // Draft, Approved, Archived
    public bool IsDefault { get; set; }

    // Full template definition (stored in JSON column)
    public PageSettings Page { get; set; } = new();
    public List<TemplateElement> Elements { get; set; } = [];

    public override int GetId() => DocumentTemplateId;
    public override void SetId(int value) => DocumentTemplateId = value;
}

public class PageSettings
{
    public string Size { get; set; } = "A4";        // A4, Letter
    public string Orientation { get; set; } = "Portrait"; // Portrait, Landscape
    public int MarginTop { get; set; } = 20;        // mm
    public int MarginRight { get; set; } = 20;
    public int MarginBottom { get; set; } = 20;
    public int MarginLeft { get; set; } = 20;
}
```

### SQL Table Schema
```sql
-- Source: Follows MotoRent.Rental.sql pattern
CREATE TABLE [<schema>].[DocumentTemplate]
(
    [DocumentTemplateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [DocumentType] AS CAST(JSON_VALUE([Json], '$.DocumentType') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [IsDefault] AS CAST(JSON_VALUE([Json], '$.IsDefault') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_DocumentTemplate_Type_Default
    ON [<schema>].[DocumentTemplate]([DocumentType], [IsDefault])

CREATE INDEX IX_DocumentTemplate_Status
    ON [<schema>].[DocumentTemplate]([Status])
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| MudBlazor drag-drop | SortableJS + Tabler | 2025-2026 (project migration) | Project uses Tabler CSS, not MudBlazor |
| HTML5 Drag API only | SortableJS wrapper | SortableJS 1.x stable | Better touch support, animations |
| jQuery UI Sortable | SortableJS | ~2018 | No jQuery dependency |

**Deprecated/outdated:**
- **Blazored.DragDrop**: Limited maintenance, no verified .NET 10 support
- **MudBlazor.Extensions DragDrop**: Project migrated away from MudBlazor
- **jQuery UI**: Heavy dependency, not recommended for modern Blazor

## Open Questions

Things that couldn't be fully resolved:

1. **Container Nesting Depth**
   - What we know: Max 2 levels recommended (Template -> Container -> Elements)
   - What's unclear: User expectation for nested containers (columns inside columns)
   - Recommendation: Start with 2 levels, add depth if user feedback requests it

2. **Touch Device Testing**
   - What we know: SortableJS supports touch with delay/threshold options
   - What's unclear: Exact behavior on MotoRent's target tablets
   - Recommendation: Test on actual tablet device before Phase 1 completion

3. **Element Selection UX**
   - What we know: Click to select, CSS class for visual feedback
   - What's unclear: Should double-click edit text inline? Keyboard shortcuts?
   - Recommendation: Start with click-to-select, evaluate UX after user testing

## Sources

### Primary (HIGH confidence)
- **Existing project code**: `GoogleMapJsInterop.cs`, `file-upload.js`, `Entity.cs` - verified patterns
- **VehicleRecognitionPanel.razor**: Native drag events working in project
- **JsonSerializerService.cs**: Polymorphic JSON serialization setup
- **MotoRent.Rental.sql**: SQL table schema pattern

### Secondary (MEDIUM confidence)
- [BlazorSortable GitHub](https://github.com/the-urlist/BlazorSortable) - Blazor integration patterns
- [SortableJS GitHub](https://github.com/SortableJS/Sortable) - API options, group configuration
- [.NET Blog: Blazor Sortable](https://devblogs.microsoft.com/dotnet/introducing-blazor-sortable/) - DOM mutation warning
- [Blazor Bootstrap Sortable List](https://docs.blazorbootstrap.com/components/sortable-list) - Event handling patterns

### Tertiary (LOW confidence)
- Training data on SortableJS best practices (verify with current docs)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - SortableJS is well-documented, project patterns are verified
- Architecture: HIGH - Follows existing project patterns exactly
- Pitfalls: MEDIUM - Based on documented issues and community feedback

**Research date:** 2026-01-23
**Valid until:** 2026-02-23 (30 days - stable domain)
