# Document Template Editor

## What This Is

A drag-and-drop document template designer for MotoRent that lets tenant admins (OrgAdmin) create custom layouts for rental agreements, receipts, and booking confirmations. Templates use data binding to pull live rental/customer/payment data when rendered. Staff use existing print flows with template selection. Replaces hardcoded document templates with tenant-customizable designs.

## Core Value

Tenants can design their own branded documents without code changes — their receipts, agreements, and confirmations look professional and match their business identity.

## Requirements

### Validated

Existing MotoRent capabilities this feature builds upon:

- ✓ Multi-tenant architecture with schema-based data isolation — existing
- ✓ OrgAdmin role with organization settings access — existing
- ✓ Rental, Renter, Booking, Payment entities with full data — existing
- ✓ Print functionality for documents (hardcoded templates) — existing
- ✓ Blazor component architecture with MudBlazor — existing
- ✓ Gemini API integration (for AI features) — existing

### Active

**Designer Core:**
- [ ] Canvas with drag-and-drop element placement
- [ ] Elements palette: Text, Image, Two Columns, Divider, Signature, Date, Container
- [ ] Repeater element for collections (line items, clauses)
- [ ] Multi-page support with page navigation
- [ ] Properties panel for element configuration
- [ ] Page settings (margins, size, orientation)

**Data Binding:**
- [ ] Property picker showing available fields per document type
- [ ] Visual binding display: `[....]` unbound, `[Rental.RenterName]` bound
- [ ] Context models: AgreementModel, ReceiptModel, BookingConfirmationModel
- [ ] Per-field formatting options for dates and currency
- [ ] Global values (DateTime.Now, DateTime.Today)

**Document Types (v1):**
- [ ] Rental Agreement templates
- [ ] Receipt templates
- [ ] Booking Confirmation templates

**AI Features:**
- [ ] Clause Suggester for agreements
- [ ] Suggests industry-standard rental clauses based on context

**Default Templates:**
- [ ] Professional default templates per document type
- [ ] Based on Avis/Hertz/Sixt document research
- [ ] Standard rental agreement clauses included

**Template Management:**
- [ ] Template list in Organization Settings
- [ ] Create/edit/delete templates
- [ ] Set default template per document type
- [ ] Approve templates for staff use
- [ ] Multiple templates per document type

**Document Generation:**
- [ ] Render template with live data model
- [ ] HTML view with browser print (Ctrl+P)
- [ ] PDF download option
- [ ] Print button shows default template
- [ ] Dropdown to select alternative approved templates

**Integration:**
- [ ] Designer accessible from Organization Settings (OrgAdmin only)
- [ ] Enhanced print button on rental/booking detail pages
- [ ] Template storage per tenant schema

### Out of Scope

- Invoice templates — deferred, receipts cover payment confirmation for v1
- ShopManager template design access — OrgAdmin only for v1
- Conditional show/hide based on data values — v2 feature
- Template versioning/history — v2 feature
- Template sharing between tenants — each tenant designs their own
- Email templates — separate feature, different delivery mechanism

## Context

**Existing System:**
MotoRent is a motorbike rental SaaS for Thai tourist areas. Currently has hardcoded Razor templates for printing receipts and agreements. Tenants cannot customize document appearance.

**Target Users:**
- OrgAdmin: Designs templates in Organization Settings
- Staff: Uses existing print flow, can select from approved templates

**Reference Design:**
Sample designer UI with left sidebar (pages + elements), center canvas, right sidebar (properties/page/data). Similar to contract/document builders.

**Data Models Available:**
- Renter: FullName, Telephone, Email, PassportNo, Nationality, Address
- Rental: StartDate, EndDate, DailyRate, TotalAmount, Status, Notes
- Vehicle: Make, Model, Year, LicensePlate, Color
- Booking: ReservationNo, Status, PickupDate, DropoffDate
- Payment: Amount, Method, Reference, Date
- Organization: Name, Logo, Address, TaxId, Phone
- Shop: Name, Location, Phone
- User (Staff): Name processing the transaction

## Constraints

- **Tech Stack**: Blazor Server + WASM (existing MotoRent architecture) — must integrate seamlessly
- **Multi-tenant**: Templates stored in tenant schema `[AccountNo].[DocumentTemplate]`
- **PDF Generation**: Need PDF library compatible with .NET 10 (research required)
- **Drag-and-drop**: Need Blazor-compatible drag-and-drop library or JS interop

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Repeater over Dynamic Table | More flexible — can repeat any layout, not just rows | — Pending |
| Per-field formatting | Users need control over date/currency display | — Pending |
| OrgAdmin only for v1 | Simpler access control, can expand later | — Pending |
| AI clause suggester included | High value for agreements, Gemini API already integrated | — Pending |

---
*Last updated: 2026-01-23 after initialization*
