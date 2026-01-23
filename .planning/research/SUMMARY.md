# Research Summary: Document Template Editor

**Project:** MotoRent Document Template Editor
**Synthesized:** 2026-01-23
**Overall Confidence:** MEDIUM-HIGH

---

## Executive Summary

The document template editor for MotoRent requires a visual drag-and-drop designer that produces professional rental agreements, receipts, and booking confirmations in both HTML (browser print) and PDF formats. The research confirms this is a well-understood problem domain with established patterns, but several critical challenges must be addressed: Blazor/JavaScript state synchronization during drag operations, Thai text rendering in PDF output, and the WYSIWYG-to-PDF fidelity gap.

The recommended approach leverages existing project patterns (Entity base class, JSON columns, Repository pattern) with minimal new dependencies: SortableJS for drag-and-drop enhancement and QuestPDF for PDF generation. The project has already migrated away from MudBlazor to Tabler CSS, so the designer will use custom Blazor components rather than MudBlazor extensions.

Key success factors are: (1) solving drag-and-drop state sync early in Phase 1, (2) keeping the element model simple with maximum 2 levels of nesting, (3) testing Thai font rendering before declaring PDF complete, and (4) ensuring the print workflow is a drop-in replacement for staff adoption.

---

## Key Recommendations

### 1. Use SortableJS + Native HTML5 Drag for Designer Interaction
**Rationale:** Project already uses native drag events (VehicleRecognitionPanel.razor). SortableJS (10KB) handles edge cases like touch support and scroll zones. Avoids MudBlazor which the project migrated away from.

### 2. Use QuestPDF for PDF Generation
**Rationale:** Pure .NET, no browser dependency (unlike Puppeteer at 300MB+). MIT licensed for revenue under $1M. Fluent C# API integrates cleanly with existing patterns.

### 3. Keep Element Model Flat (Max 2 Nesting Levels)
**Rationale:** Prevents over-engineering. Template -> Container -> Elements is sufficient for all document types. Avoids complex JSON serialization edge cases and performance issues.

### 4. Implement Null-Safe Data Binding from Day One
**Rationale:** Production data has gaps. Template references to `Rental.Vehicle.Owner.Name` must gracefully return empty string if any part is null. Critical for staff usability.

### 5. Browser Print First, PDF Download Later
**Rationale:** Browser print (Ctrl+P) requires no external library and covers 90% of use cases. PDF download can be Phase 3 addition. Reduces initial scope and risk.

---

## Technology Choices

| Category | Choice | Rationale |
|----------|--------|-----------|
| **Drag-and-Drop** | SortableJS 1.15.x + Native HTML5 | Lightweight, touch support, matches existing interop patterns |
| **PDF Generation** | QuestPDF 2024.12.x | .NET native, fluent API, MIT licensed |
| **Template Storage** | JSON column (existing pattern) | Matches Entity/Repository pattern, no new infrastructure |
| **Template Engine** | Custom expression evaluator | Simple path resolution (`Rental.RenterName`), no overkill libraries |
| **Canvas/Designer** | Custom Blazor components | Full control, matches Tabler CSS styling |
| **State Management** | Scoped DesignerStateService | Command pattern for undo/redo, event-driven reactivity |

### Dependencies to Add
```xml
<!-- MotoRent.Services.csproj -->
<PackageReference Include="QuestPDF" Version="2024.12.0" />
```

```html
<!-- wwwroot -->
<script src="lib/sortablejs/Sortable.min.js"></script>
```

### Thai Fonts Required
- TH Sarabun PSK (government standard)
- Noto Sans Thai (Google fallback)

---

## Build Order (Recommended Phases)

### Phase 1: Foundation and Designer Core
**Delivers:** Element models, database schema, basic canvas with drag-and-drop
**Duration:** 3-4 days
**Key Files:**
- `DocumentTemplate.cs` entity with polymorphic elements
- `TemplateService.cs` for CRUD
- `DesignerCanvas.razor` with SortableJS interop
- `ElementsPalette.razor` with draggable elements
- `PropertiesPanel.razor` for element configuration

**Must Solve:**
- Blazor/JS state sync during drag (Critical Pitfall 1)
- Coordinate system (points internal, pixels display)
- Element model simplicity (max 2 nesting levels)

**Research Flag:** Standard patterns - no additional research needed

---

### Phase 2: Data Binding System
**Delivers:** Property picker, context models, binding resolution
**Duration:** 2-3 days
**Key Files:**
- `AgreementModel.cs`, `ReceiptModel.cs`, `BookingConfirmationModel.cs`
- `DataModelService.cs` for building models from entities
- `DataBindingPicker.razor` UI component

**Must Solve:**
- Null-safe property access (Critical Pitfall 5)
- Format specifiers (date, currency)

**Research Flag:** Standard patterns

---

### Phase 3: Rendering and Browser Print
**Delivers:** Template-to-HTML rendering, live preview, browser print
**Duration:** 2-3 days
**Key Files:**
- `DocumentRenderService.cs`
- Print preview page
- CSS print media styles

**Must Solve:**
- Preview accuracy
- Page sizing (A4)

**Research Flag:** Standard patterns

---

### Phase 4: Template Management
**Delivers:** Template CRUD, default templates, approval workflow
**Duration:** 2 days
**Key Files:**
- `TemplateList.razor` page
- Database seed with default templates

**Must Solve:**
- Tenant isolation (filter by AccountNo)
- Template validation on save

**Research Flag:** Standard patterns

---

### Phase 5: PDF Export
**Delivers:** PDF download button, QuestPDF integration
**Duration:** 2-3 days
**Key Files:**
- `PdfExportService.cs`
- Thai font embedding

**Must Solve:**
- Thai text rendering (Critical Pitfall 4)
- WYSIWYG-to-PDF fidelity (Critical Pitfall 2)

**Research Flag:** NEEDS RESEARCH - Thai font embedding verification, QuestPDF .NET 10 compatibility

---

### Phase 6: Integration and Staff Workflow
**Delivers:** Print buttons in rental/receipt dialogs, template selector
**Duration:** 1-2 days
**Key Files:**
- Updated `RentalDetail.razor` print flow
- Updated `PaymentDialog.razor` receipt print

**Must Solve:**
- Staff workflow preservation (adoption blocker)
- Default template auto-selection

**Research Flag:** Standard patterns - but needs staff testing

---

### Phase 7 (Optional): Advanced Features
**Delivers:** Repeater elements, multi-page, AI clause suggester
**Duration:** 3-4 days

**Research Flag:** NEEDS RESEARCH if implemented - Repeater performance, page break handling

---

## Critical Risks and Mitigations

### Risk 1: Blazor/JavaScript State Desync During Drag
**Impact:** HIGH - Elements snap back, drag fails on mobile
**Mitigation:** JavaScript is source of truth during drag; Blazor only learns final position on drop. Use `@key` directives. Test with artificial SignalR latency.

### Risk 2: Thai Text Rendering Failures in PDF
**Impact:** HIGH - Legal documents unreadable for Thai market
**Mitigation:** Embed TH Sarabun font (not subset). Test specific patterns: "กรุงเทพมหานคร", "ไม่มี". Validate in Phase 5 before declaring complete.

### Risk 3: WYSIWYG Preview Does Not Match PDF Output
**Impact:** MEDIUM-HIGH - User frustration, wasted design time
**Mitigation:** Start with browser print only. When adding PDF, use same layout approach (absolute positioning). Show "PDF Preview" button before save.

### Risk 4: Staff Abandon Feature Due to Workflow Change
**Impact:** MEDIUM - Feature built but not used
**Mitigation:** Drop-in replacement strategy. Same print button location, default template auto-selected, template selection optional.

### Risk 5: Over-Engineered Element Model
**Impact:** MEDIUM - Development delays, maintenance burden
**Mitigation:** Hardcode element types for v1 (Text, Image, TwoColumns, Container, Divider, Signature, Date). No element registry or plugin system. Maximum 2 levels of nesting.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Stack** | MEDIUM | QuestPDF .NET 10 compatibility unverified. SortableJS touch behavior needs testing. |
| **Features** | MEDIUM | Field requirements based on domain knowledge. Recommend validation against actual rental company agreements. |
| **Architecture** | HIGH | Follows established MotoRent patterns. Entity, Repository, JSON columns all proven. |
| **Pitfalls** | MEDIUM | Based on training data and domain knowledge. Limited external verification. |

### Gaps to Address During Planning

1. **QuestPDF .NET 10 Compatibility** - Check NuGet for net10.0 target before Phase 5
2. **Thai Font Embedding** - Prototype early in Phase 2 with test document
3. **Touch Device Testing** - Test SortableJS on actual tablet before Phase 1 completion
4. **Staff Workflow Validation** - Demo to actual rental desk users before Phase 6 deployment

---

## Quick Reference

| Topic | One-Liner |
|-------|-----------|
| **Drag-and-Drop** | SortableJS + native HTML5, JS source of truth during drag |
| **PDF Engine** | QuestPDF (pure .NET, MIT license) |
| **Storage** | JSON column in tenant schema, existing Repository pattern |
| **Binding** | Custom expression evaluator, null-safe property paths |
| **Elements** | 8 types: Text, Image, TwoColumns, Container, Divider, Signature, Date, Repeater |
| **Nesting** | Max 2 levels (Template -> Container -> Elements) |
| **Critical Phase** | Phase 1 (drag sync) and Phase 5 (Thai PDF) |
| **Adoption Risk** | Staff workflow - must be drop-in replacement |

---

## Aggregated Sources

### High Confidence
- Existing MotoRent codebase patterns (Entity, Repository, DataContext)
- `VehicleRecognitionPanel.razor` - native drag events working
- `GoogleMapJsInterop.cs` - ES module interop pattern
- `mudblazor-to-tabler-migration.md` - confirms MudBlazor removal

### Medium Confidence
- QuestPDF recommendation (training data 2024-2025)
- SortableJS recommendation (stable library, wide adoption)
- Thai font requirements (domain knowledge)
- Rental document field requirements (industry practice)

### Verification Needed
- QuestPDF .NET 10 compatibility (NuGet check)
- SortableJS touch support (device testing)
- Thai combining character rendering (prototype test)

---

*Synthesis complete. Ready for roadmap creation.*
