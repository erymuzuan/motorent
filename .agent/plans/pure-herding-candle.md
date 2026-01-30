# Multi-Page A4 Overflow Support for Document Templates

## Problem
The HTML renderer doesn't declare `@page { size: A4 }`, so browsers don't know the target page size during print. Content that overflows a designed page just extends the div instead of creating proper continuation pages.

## Changes

### 1. HtmlTemplateRenderer.cs (`src/MotoRent.Services/HtmlTemplateRenderer.cs`)
- Add `@page { size: A4; margin: ...pt; }` CSS rule using `LayoutSettings` margins
- Remove `padding` from `body` style (margins now handled by `@page` rule)
- Keep existing `.page-break` class between designed pages (browser auto-paginates overflow content within each designed page)

### 2. designer.css (`src/MotoRent.Client/wwwroot/css/designer.css`)
- Add `.a4-page-boundary` class: absolute-positioned dashed line with label
- Subtle red dashed line at page boundary so designers see where content will overflow

### 3. DocumentCanvas.razor (`src/MotoRent.Client/Components/Templates/DocumentCanvas.razor`)
- Add `<div class="a4-page-boundary" style="top: 29.7cm" data-label="Page break">` inside `.a4-canvas`

### 4. Localization (`DocumentCanvas.resx`, `.en.resx`, `.th.resx`, `.ms.resx`)
- Add "PageBreak" key

### No changes needed:
- **QuestPdfGenerator.cs** - QuestPDF already handles overflow natively
- **PrintAgreement.razor** - Will inherit `@page` rule from rendered HTML

## Verification
1. Create a template with content exceeding one A4 page
2. Preview and print - verify browser creates continuation pages for overflow
3. Create 2-page template where page 1 overflows → should print as 3 pages
4. Verify dashed boundary line appears at 29.7cm in designer canvas
