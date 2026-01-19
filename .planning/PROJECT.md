# MotoRent

## What This Is

A SaaS platform for vehicle rental operators in Thailand's tourist areas (Phuket, Pattaya, Bangkok, etc.). Targets small-to-mid size operators still running on carbon copies and spreadsheets. Handles the complete rental lifecycle — from tourist check-in with document verification, through the rental period, to check-out with damage assessment and settlement. Beyond operations, it gives owners visibility into business profitability: asset depreciation, maintenance costs, cash flow, and staff accountability.

## Core Value

**Business visibility and cash control** — owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly. The rental workflow is table stakes; the profitability intelligence is the differentiator.

## Requirements

### Validated

<!-- Shipped and confirmed working — existing codebase capabilities -->

- ✓ Multi-tenant architecture with schema-per-tenant isolation — existing
- ✓ OAuth authentication (Google, Microsoft, LINE) with role-based access — existing
- ✓ Super admin impersonation for support — existing
- ✓ Multi-shop management with vehicle pooling across locations — existing
- ✓ Vehicle inventory management (motorbikes, scooters, cars, boats, jet skis, vans) — existing
- ✓ Insurance packages and accessories management — existing
- ✓ Renter management with passport/license OCR verification (Gemini Flash) — existing
- ✓ Rental check-in wizard (5 steps: vehicle, documents, deposit, insurance, accessories) — existing
- ✓ Rental check-out with damage assessment — existing
- ✓ Active rentals dashboard — existing
- ✓ Payment processing (Cash, Card, PromptPay, BankTransfer) — existing
- ✓ Invoice generation — existing
- ✓ Deposit tracking and refunds — existing
- ✓ Daily/weekly/monthly financial reports — existing
- ✓ Owner payments tracking — existing
- ✓ Accident/incident reporting — existing
- ✓ Maintenance tracking and scheduling — existing
- ✓ Service locations with drop-off fees — existing
- ✓ Tourist portal with online reservations — existing
- ✓ PWA with offline support and camera access — existing

### Active

<!-- Current milestone: Cashier Till & End of Day Reconciliation -->

- [ ] Cashier till opening with starting float per currency
- [ ] Multi-currency payment acceptance (THB, USD, EUR, CNY) with operator-set exchange rates
- [ ] Change calculation in THB when paying with foreign currency
- [ ] Till payment receiving linked to rental transactions
- [ ] Till payouts (deposit refunds, expenses) with reason tracking
- [ ] Cash drops to safe during shift
- [ ] End of day till closing and reconciliation
- [ ] Per-currency balance tracking (expected vs actual)
- [ ] Cash shortage/overage logging against staff member
- [ ] Till receipts and transaction history
- [ ] Manager oversight of all tills and shifts

### Out of Scope

- Currency conversion between till currencies — handled by separate forex department
- Real-time currency rate feeds — operators set their own rates
- Credit card terminal integration — record payments only, no POS hardware integration
- Accounting system export (QuickBooks, Xero) — future milestone
- Push notifications — infrastructure ready but not in this milestone

## Context

**Existing business:** We already serve money changers in Thai tourist areas with a forex POS system. Vehicle rental is an adjacent market opportunity using the same relationships and geographic presence.

**Target market:** Small-to-mid size rental operators. Large operators have existing software. Our sweet spot is digitizing paper-based operations.

**Multi-currency expertise:** Our forex background means multi-currency cash handling is a natural fit. Most rental software doesn't handle this well.

**Current codebase:** Blazor Server + WASM hybrid, .NET 10, SQL Server with JSON columns, custom repository pattern. Substantial functionality already built (see Validated requirements).

## Constraints

- **Tech stack**: Blazor Server + WASM, .NET 10, SQL Server — must integrate with existing architecture
- **Multi-tenancy**: Schema-per-tenant isolation — till tables must follow this pattern
- **Localization**: English/Thai/Malay — all new UI must be localized
- **Mobile-first**: PWA already supports offline/camera — till UI should work on tablets at desk

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Base currency THB | Thailand market, rental rates always in THB | — Pending |
| Change always in THB | Simplifies till reconciliation, matches tourist expectations | — Pending |
| Per-currency till tracking | Matches forex expertise, enables accurate reconciliation | — Pending |
| Shortage logged, not enforced | Policy decision left to manager, system just provides visibility | — Pending |

---
*Last updated: 2026-01-19 after initialization*
