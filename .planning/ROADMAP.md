# Roadmap: Document Template Editor

**Project:** MotoRent Document Template Editor
**Created:** 2026-01-23
**Depth:** Quick (3-5 phases)
**Coverage:** 15/15 requirements mapped

---

## Overview

This roadmap delivers a tenant-customizable document template system for MotoRent in 4 core phases plus 1 optional advanced phase. The structure follows a natural dependency chain: build the designer first, then add data binding, then rendering, then management and integration. Each phase delivers a verifiable capability that unlocks the next.

---

## Phase 1: Designer Foundation

**Goal:** OrgAdmin can visually place and configure elements on a document canvas.

**Dependencies:** None (foundation phase)

**Requirements:**
| REQ-ID | Description |
|--------|-------------|
| REQ-001 | Designer Canvas with Drag-and-Drop |
| REQ-002 | Properties Panel |

**Success Criteria:**
1. User can drag Text, Image, Two Columns, Divider, Signature, Date, and Container elements from palette onto canvas
2. User can reorder elements by dragging and delete elements
3. User can select an element and see its configurable properties in the side panel
4. User can change element properties (font size, alignment, dimensions) and see changes immediately on canvas
5. Canvas visually shows element boundaries and selection state

**Key Deliverables:**
- `DocumentTemplate.cs` entity with polymorphic element model (JSON serialization)
- `TemplateElement` base class with TextElement, ImageElement, ContainerElement, etc.
- SortableJS interop for drag-and-drop (`sortable.js` integration)
- `DesignerCanvas.razor` component
- `ElementsPalette.razor` component
- `PropertiesPanel.razor` component
- Database schema: `[AccountNo].[DocumentTemplate]` table

**Critical Risks:**
- Blazor/JavaScript state desync during drag operations (mitigate: JS is source of truth during drag)

---

## Phase 2: Data Binding

**Goal:** OrgAdmin can bind document elements to live rental/customer/payment data.

**Dependencies:** Phase 1 (needs canvas and elements to bind)

**Requirements:**
| REQ-ID | Description |
|--------|-------------|
| REQ-004 | Data Binding Display |
| REQ-005 | Property Picker |
| REQ-012 | Per-Field Formatting |

**Success Criteria:**
1. User can see unbound text elements displayed as `[....]` and bound elements as `[Rental.RenterName]`
2. User can open a property picker showing available fields organized by context (Renter, Rental, Vehicle, Organization, etc.)
3. User can click a field in the picker to bind it to the selected text element
4. User can set format specifiers for dates (dd/MM/yyyy) and currency (Baht symbol, thousands)
5. Bound fields resolve to actual values when previewed with sample data

**Key Deliverables:**
- `AgreementModel.cs` - context model for rental agreements
- `ReceiptModel.cs` - context model for receipts
- `BookingConfirmationModel.cs` - context model for booking confirmations
- `DataModelService.cs` - builds context models from entities
- `DataBindingPicker.razor` - tree view of available fields
- Null-safe property path resolution (graceful handling of missing data)
- Format specifier support in element properties

**Critical Risks:**
- Null reference exceptions on incomplete data (mitigate: null-safe expression evaluator from day one)

---

## Phase 3: Rendering and Print

**Goal:** Staff can print documents using templates with live data.

**Dependencies:** Phase 2 (needs data binding to render meaningful content)

**Requirements:**
| REQ-ID | Description |
|--------|-------------|
| REQ-006 | Browser Print |

**Success Criteria:**
1. User can render a template with live data from an actual rental/booking/payment
2. User can open browser print dialog (Ctrl+P) from the rendered document
3. Rendered document respects A4 page sizing with proper margins
4. Thai text renders correctly in both preview and print output
5. CSS print media styles produce clean output without browser chrome

**Key Deliverables:**
- `DocumentRenderService.cs` - template-to-HTML rendering engine
- Print preview page/dialog with rendered template
- CSS print media styles (`@media print` rules)
- A4 page layout styles
- Thai font support (TH Sarabun PSK, Noto Sans Thai)

**Critical Risks:**
- Preview not matching print output (mitigate: test with actual print early)

---

## Phase 4: Template Management and Integration

**Goal:** OrgAdmin can manage templates and staff can use them from existing print flows.

**Dependencies:** Phase 3 (needs rendering to be useful)

**Requirements:**
| REQ-ID | Description |
|--------|-------------|
| REQ-003 | Template Persistence |
| REQ-007 | Default Templates |
| REQ-008 | Template Approval Workflow |
| REQ-014 | Staff Print Integration |
| REQ-015 | Designer Access Control |

**Success Criteria:**
1. OrgAdmin can create, save, load, and delete templates from Organization Settings
2. OrgAdmin can set template status (Draft/Approved) and designate default template per document type
3. Staff sees existing print buttons unchanged but output now uses the default template
4. Staff can select from a dropdown of approved templates when printing
5. First-time tenants receive professional default templates for Agreement, Receipt, and Booking Confirmation
6. Non-OrgAdmin users cannot access the template designer pages

**Key Deliverables:**
- `TemplateList.razor` - template management page in Organization Settings
- Template CRUD operations with tenant isolation
- Template status field (Draft/Approved) with approval workflow
- Default template per document type setting
- Default template seeds (based on industry-standard rental documents)
- Updated `RentalDetail.razor` print flow with template selection
- Updated receipt print flow with template selection
- OrgAdmin role authorization on designer routes

**Critical Risks:**
- Staff adoption (mitigate: drop-in replacement, same button locations, auto-select default)

---

## Phase 5: Advanced Features (Optional)

**Goal:** Enhanced document capabilities for power users.

**Dependencies:** Phase 4 (core system must be complete)

**Requirements:**
| REQ-ID | Description |
|--------|-------------|
| REQ-009 | PDF Download |
| REQ-010 | Repeater Element |
| REQ-011 | Multi-Page Support |
| REQ-013 | AI Clause Suggester |

**Success Criteria:**
1. User can download rendered document as a PDF file with Thai fonts embedded correctly
2. User can add Repeater element that renders collections (rental accessories, agreement clauses)
3. User can create multi-page templates with page navigation in designer and page breaks in output
4. User can access AI Clause Suggester that recommends industry-standard rental clauses

**Key Deliverables:**
- `PdfExportService.cs` with QuestPDF integration
- Thai font embedding (TH Sarabun PSK)
- Repeater element type with collection binding
- Multi-page support in designer and renderer
- AI Clause Suggester using existing Gemini API integration

**Critical Risks:**
- Thai text rendering in PDF (mitigate: early prototype testing)
- QuestPDF .NET 10 compatibility (mitigate: verify before starting)

---

## Coverage Validation

| REQ-ID | Description | Phase | Priority |
|--------|-------------|-------|----------|
| REQ-001 | Designer Canvas with Drag-and-Drop | 1 | P0 |
| REQ-002 | Properties Panel | 1 | P0 |
| REQ-003 | Template Persistence | 4 | P0 |
| REQ-004 | Data Binding Display | 2 | P0 |
| REQ-005 | Property Picker | 2 | P0 |
| REQ-006 | Browser Print | 3 | P0 |
| REQ-007 | Default Templates | 4 | P1 |
| REQ-008 | Template Approval Workflow | 4 | P1 |
| REQ-009 | PDF Download | 5 | P1 |
| REQ-010 | Repeater Element | 5 | P1 |
| REQ-011 | Multi-Page Support | 5 | P1 |
| REQ-012 | Per-Field Formatting | 2 | P1 |
| REQ-013 | AI Clause Suggester | 5 | P2 |
| REQ-014 | Staff Print Integration | 4 | P0 |
| REQ-015 | Designer Access Control | 4 | P0 |

**P0 Requirements by Phase:**
- Phase 1: 2 (REQ-001, REQ-002)
- Phase 2: 2 (REQ-004, REQ-005)
- Phase 3: 1 (REQ-006)
- Phase 4: 3 (REQ-003, REQ-014, REQ-015)

**Coverage:** 15/15 requirements mapped - No orphans

---

## Progress

| Phase | Status | Requirements |
|-------|--------|--------------|
| 1 - Designer Foundation | Pending | REQ-001, REQ-002 |
| 2 - Data Binding | Pending | REQ-004, REQ-005, REQ-012 |
| 3 - Rendering and Print | Pending | REQ-006 |
| 4 - Template Management and Integration | Pending | REQ-003, REQ-007, REQ-008, REQ-014, REQ-015 |
| 5 - Advanced Features (Optional) | Pending | REQ-009, REQ-010, REQ-011, REQ-013 |

---

*Roadmap derived from REQUIREMENTS.md and research/SUMMARY.md. Depth: quick.*
