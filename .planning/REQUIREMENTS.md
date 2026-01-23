# Requirements: Document Template Editor

**Project:** MotoRent Document Template Editor
**Created:** 2026-01-23
**Status:** Active

---

## REQ-001: Designer Canvas with Drag-and-Drop
**Priority:** P0 (Must Have)
**Category:** Designer Core

Users can drag elements from a palette onto a canvas and arrange them visually to design document templates.

**Acceptance Criteria:**
- Elements palette with: Text, Image, Two Columns, Divider, Signature, Date, Container
- Canvas accepts dropped elements
- Elements can be reordered via drag
- Elements can be deleted
- Canvas shows element boundaries visually
- Works on desktop browsers (Chrome, Edge)

---

## REQ-002: Properties Panel
**Priority:** P0 (Must Have)
**Category:** Designer Core

Selected elements show configurable properties in a side panel.

**Acceptance Criteria:**
- Panel shows properties for selected element
- Text element: font size, alignment, bold/italic
- Image element: source URL, alt text, dimensions
- Container element: border, padding, background
- Changes apply immediately to canvas preview

---

## REQ-003: Template Persistence
**Priority:** P0 (Must Have)
**Category:** Template Management

Templates can be saved and loaded per tenant.

**Acceptance Criteria:**
- Save template with name and document type
- Load template by ID
- Templates stored in tenant schema
- Template list shows all templates for tenant
- Delete template (soft delete or hard delete with confirmation)

---

## REQ-004: Data Binding Display
**Priority:** P0 (Must Have)
**Category:** Data Binding

Text elements show binding state visually.

**Acceptance Criteria:**
- Unbound placeholder shows as `[....]`
- Bound field shows as `[Rental.RenterName]`
- Bound fields render actual data in preview mode

---

## REQ-005: Property Picker
**Priority:** P0 (Must Have)
**Category:** Data Binding

Users can browse and select available fields for data binding.

**Acceptance Criteria:**
- Property tree shows available fields per document type
- Agreement: Renter, Rental, Vehicle, Organization, Shop, Staff
- Receipt: Payment, Renter, Rental, Organization, Shop
- Booking Confirmation: Booking, Renter, Vehicle, Organization, Shop
- Global values: DateTime.Now, DateTime.Today
- Clicking a field binds it to selected element

---

## REQ-006: Browser Print
**Priority:** P0 (Must Have)
**Category:** Document Generation

Templates can be rendered and printed via browser print dialog.

**Acceptance Criteria:**
- Render template with live data
- Open print preview/dialog (Ctrl+P)
- A4 page sizing
- CSS print media styles applied
- Thai text renders correctly

---

## REQ-007: Default Templates
**Priority:** P1 (Should Have)
**Category:** Template Management

Each document type has a pre-built default template.

**Acceptance Criteria:**
- Rental Agreement default with standard clauses
- Receipt default with line items
- Booking Confirmation default with reservation details
- Default templates created on tenant first use
- Based on industry-standard rental documents

---

## REQ-008: Template Approval Workflow
**Priority:** P1 (Should Have)
**Category:** Template Management

OrgAdmin can mark templates as approved for staff use.

**Acceptance Criteria:**
- Templates have Draft/Approved status
- Only approved templates appear in staff print dropdown
- Default template per document type setting

---

## REQ-009: PDF Download
**Priority:** P1 (Should Have)
**Category:** Document Generation

Users can download rendered documents as PDF files.

**Acceptance Criteria:**
- Download PDF button on preview
- PDF matches browser preview layout
- Thai fonts embedded correctly
- File named appropriately (e.g., `Rental_Agreement_R001.pdf`)

---

## REQ-010: Repeater Element
**Priority:** P1 (Should Have)
**Category:** Designer Core

Templates can repeat sections for collections (line items, clauses).

**Acceptance Criteria:**
- Repeater element in palette
- Bind to collection property (e.g., `Rental.Accessories`)
- Child template repeated per item
- Works for receipts (line items) and agreements (clauses)

---

## REQ-011: Multi-Page Support
**Priority:** P1 (Should Have)
**Category:** Designer Core

Templates can have multiple pages.

**Acceptance Criteria:**
- Pages list in designer sidebar
- Add/remove pages
- Navigate between pages
- Page breaks in rendered output

---

## REQ-012: Per-Field Formatting
**Priority:** P1 (Should Have)
**Category:** Data Binding

Bound fields can have display format specified.

**Acceptance Criteria:**
- Date formats: dd/MM/yyyy, MMMM d, yyyy, etc.
- Currency format: ฿ symbol, thousand separators
- Number format: decimal places
- Format selector in properties panel

---

## REQ-013: AI Clause Suggester
**Priority:** P2 (Nice to Have)
**Category:** AI Features

AI suggests standard rental clauses when designing agreements.

**Acceptance Criteria:**
- Clause Suggester button/panel in designer
- Suggests industry-standard clauses
- User can accept and insert clause as text block
- Uses existing Gemini API integration

---

## REQ-014: Staff Print Integration
**Priority:** P0 (Must Have)
**Category:** Integration

Existing print buttons use the template system.

**Acceptance Criteria:**
- Rental detail page print uses default template
- Receipt print uses default receipt template
- Dropdown shows alternative approved templates
- Staff workflow unchanged (same button location)

---

## REQ-015: Designer Access Control
**Priority:** P0 (Must Have)
**Category:** Integration

Only OrgAdmin can access template designer.

**Acceptance Criteria:**
- Designer in Organization Settings section
- OrgAdmin role required
- Staff cannot access designer pages

---

## Traceability Matrix

| REQ-ID | Description | Phase | Status |
|--------|-------------|-------|--------|
| REQ-001 | Designer Canvas with Drag-and-Drop | 1 | Pending |
| REQ-002 | Properties Panel | 1 | Pending |
| REQ-003 | Template Persistence | 4 | Pending |
| REQ-004 | Data Binding Display | 2 | Pending |
| REQ-005 | Property Picker | 2 | Pending |
| REQ-006 | Browser Print | 3 | Pending |
| REQ-007 | Default Templates | 4 | Pending |
| REQ-008 | Template Approval Workflow | 4 | Pending |
| REQ-009 | PDF Download | 5 | Pending |
| REQ-010 | Repeater Element | 5 | Pending |
| REQ-011 | Multi-Page Support | 5 | Pending |
| REQ-012 | Per-Field Formatting | 2 | Pending |
| REQ-013 | AI Clause Suggester | 5 | Pending |
| REQ-014 | Staff Print Integration | 4 | Pending |
| REQ-015 | Designer Access Control | 4 | Pending |

---

*Requirements derived from PROJECT.md and research findings.*
