# DocumentTemplateDesigner Full-Screen Redesign with Multi-Page Support

## Goal
Transform the DocumentTemplateDesigner from a page within MainLayout (navbar/sidebar/footer) into a full-screen dedicated designer with its own layout, multi-page A4 canvas support, and a UI inspired by the rodin.contract-canvas React reference.

## Target Layout
```
+------------------------------------------------------------------+
| [Logo] [Template Name___________]   [Import] [AI] [Preview] [Save] [Dashboard] |
+----------+--------------------------------------+-----------+
| Pages    |                                      | Settings  |
| [Page 1] |                                      | [Props][Page][Data] |
| [Page 2] |        A4 Canvas (21cm x 29.7cm)     |           |
|----------|        current page content           | (tab      |
| Elements |                                      |  content) |
| Heading  |                                      |           |
| Text     |                                      |           |
| Table    |                                      |           |
| Image    |   [< Prev] Page 1 of 2 [Next >]      |           |
| ...      |   [+ Add Page]  [Delete Page]         |           |
+----------+--------------------------------------+-----------+
```

---

## Phase 1: Domain Model - Multi-Page Support

### Task 1.1: Add `DocumentPage` class and update `DocumentLayout`
**File**: `src/MotoRent.Domain/Models/DocumentLayout.cs`

- Add `DocumentPage` class:
  ```csharp
  public class DocumentPage
  {
      public string Id { get; set; } = Guid.NewGuid().ToString("N");
      public string Name { get; set; } = "Page 1";
      public List<LayoutSection> Sections { get; set; } = [new LayoutSection()];
  }
  ```
- Add `Pages` property to `DocumentLayout`:
  ```csharp
  public List<DocumentPage> Pages { get; set; } = [];
  ```
- Keep existing `Sections` property for backward compatibility
- Add `EnsurePages()` method that migrates legacy `Sections`-only layouts into a single `DocumentPage`

### Task 1.2: Update DesignerState for page navigation
**File**: `src/MotoRent.Client/Services/DesignerState.cs`

- Add `CurrentPageIndex` (int), `ActiveRightTab` (string: "properties"/"page"/"data")
- Add methods: `SetCurrentPage(int)`, `SetRightTab(string)`

**Depends on**: nothing

---

## Phase 2: Service Updates

### Task 2.1: Update DocumentTemplateService
**File**: `src/MotoRent.Services/DocumentTemplateService.cs`
- Call `layout.EnsurePages()` after deserialization in `GetTemplateLayoutAsync`

### Task 2.2: Update IHtmlTemplateRenderer
**File**: `src/MotoRent.Services/HtmlTemplateRenderer.cs`
- Iterate `layout.Pages` instead of `layout.Sections`
- Add page-break CSS between pages

### Task 2.3: Update QuestPdfGenerator (if exists)
**File**: `src/MotoRent.Services/QuestPdfGenerator.cs`
- Map each `DocumentPage` to a separate PDF page

### Task 2.4: Update DocumentTemplateAiService
**File**: `src/MotoRent.Services/DocumentTemplateAiService.cs`
- Ensure `EnsurePages()` is called on AI-extracted layouts

**Depends on**: Phase 1

---

## Phase 3: DesignerLayout - Full-Screen Layout

### Task 3.1: Create DesignerLayout.razor
**New file**: `src/MotoRent.Client/Layout/DesignerLayout.razor`
- Minimal layout: `position: fixed; inset: 0` full-screen container
- No navbar, sidebar, or footer
- Just renders `@Body` in a flex column container

### Task 3.2: Create DesignerLayout.razor.css
**New file**: `src/MotoRent.Client/Layout/DesignerLayout.razor.css`

### Task 3.3: Update Routes.razor
**File**: `src/MotoRent.Client/Routes.razor`
- Add check before the default MainLayout return:
  ```csharp
  if (pageName.Contains("DocumentTemplateDesigner"))
      return typeof(Layout.DesignerLayout);
  ```

**Depends on**: nothing (parallel with Phase 1-2)

---

## Phase 4: Redesign Designer Page (use frontend-design skill)

### Task 4.1: Redesign top bar
**File**: `src/MotoRent.Client/Pages/Settings/DocumentTemplateDesigner.razor`
- Slim 48px header bar with: logo/back icon, inline name input, action buttons (Import, AI, Preview, Save), Dashboard link
- Replace existing `<TablerHeader>` with custom designer header

### Task 4.2: Redesign left panel - Pages section
- Top of left panel: list of pages with names ("Page 1", "Page 2")
- Click to switch page, drag handle for reorder
- Simple list items (not full thumbnails for v1)

### Task 4.3: Redesign left panel - Elements section
- Below pages: existing block library buttons (heading, text, table, etc.)
- Keep drag-to-canvas and click-to-add

### Task 4.4: Add page navigation bar below canvas
- Rendered below the `<DocumentCanvas>` component
- Contains: Prev/Next buttons, "Page X of Y" label, Add Page button, Delete Page button

### Task 4.5: Redesign right panel with tabs
- Tab bar: Properties | Page | Data
- Properties tab: existing `<BlockPropertyEditor>`
- Page tab: current page name, page-level settings
- Data tab: placeholder picker / available bindings, AI suggester

### Task 4.6: Update code-behind logic
- Replace all `m_layout.Sections` references with `CurrentPage.Sections`
- Add `CurrentPage` property: `m_layout.Pages[DesignerState.CurrentPageIndex]`
- Add page management: `AddPage()`, `DeleteCurrentPage()`, `NavigatePage(int)`
- Call `m_layout.EnsurePages()` in `LoadDataAsync`
- Update `GetMainSection()`, `DeleteSelectedBlock()`, `AddClauseToLayout()` to use current page

### Task 4.7: Rewrite designer CSS
**File**: `src/MotoRent.Client/Pages/Settings/DocumentTemplateDesigner.razor.css`
- Full-height layout (no more `calc(100vh - navbar)`)
- New classes: `.designer-topbar`, `.designer-pages-list`, `.designer-page-item`, `.designer-canvas-footer`, `.designer-right-tabs`
- Use the **frontend-design** skill for polished, distinctive styling

**Depends on**: Phase 1, 2, 3

---

## Phase 5: Update DocumentCanvas Component

### Task 5.1: Make canvas page-aware
**File**: `src/MotoRent.Client/Components/Templates/DocumentCanvas.razor`
- Change `Layout` parameter to accept `DocumentPage` (or add `Page` parameter)
- Iterate `Page.Sections` instead of `Layout.Sections`
- Update `AddSection` to work with current page

**Depends on**: Phase 1, Phase 4

---

## Phase 6: Localization

### Task 6.1: Add new resource keys
**Files**: `src/MotoRent.Client/Resources/Pages/Settings/DocumentTemplateDesigner.{resx,en.resx,th.resx,ms.resx}`

New keys: `Pages`, `Elements`, `PageXOfY`, `AddPage`, `DeletePage`, `PreviousPage`, `NextPage`, `PropertiesTab`, `PageTab`, `DataTab`, `PageName`, `PageSettings`, `Dashboard`, `ConfirmDeletePage`, `StandardElements`

**Depends on**: Phase 4

---

## Phase 7: JavaScript Updates

### Task 7.1: Update designer-splitter.js
**File**: `src/MotoRent.Client/wwwroot/js/designer-splitter.js`
- Adjust height calculations for full-viewport layout (no navbar offset)

**Depends on**: Phase 3

---

## Verification Plan
1. `dotnet build` - ensure no compile errors
2. Navigate to `/settings/templates/designer` - verify full-screen layout loads (no navbar/sidebar)
3. Create a new template - verify single page with default section
4. Add blocks to the canvas - verify they appear on the A4 page
5. Add a second page - verify page navigation works (prev/next, page counter)
6. Switch between pages - verify blocks are page-specific
7. Delete a page - verify confirmation and correct page removal
8. Save and reload - verify multi-page layout persists correctly
9. Load an existing (legacy) template - verify backward compat (auto-migrates to single page)
10. Preview - verify multi-page HTML preview renders with page breaks
11. Right panel tabs - verify Properties/Page/Data tabs switch correctly
12. Test responsive splitter panels still drag correctly

## Critical Files
- `src/MotoRent.Domain/Models/DocumentLayout.cs`
- `src/MotoRent.Client/Services/DesignerState.cs`
- `src/MotoRent.Client/Routes.razor`
- `src/MotoRent.Client/Layout/DesignerLayout.razor` (new)
- `src/MotoRent.Client/Pages/Settings/DocumentTemplateDesigner.razor`
- `src/MotoRent.Client/Pages/Settings/DocumentTemplateDesigner.razor.css`
- `src/MotoRent.Client/Components/Templates/DocumentCanvas.razor`
- `src/MotoRent.Client/wwwroot/css/designer.css`
- `src/MotoRent.Client/wwwroot/js/designer-splitter.js`
- `src/MotoRent.Services/DocumentTemplateService.cs`
- `src/MotoRent.Services/HtmlTemplateRenderer.cs`
