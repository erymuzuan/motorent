# MotoRent

## What This Is

A SaaS platform for vehicle rental operators in Thailand's tourist areas (Phuket, Pattaya, Bangkok, etc.). Targets small-to-mid size operators still running on carbon copies and spreadsheets. Handles the complete rental lifecycle — from tourist check-in with document verification, through the rental period, to check-out with damage assessment and settlement. Beyond operations, it gives owners visibility into business profitability: asset depreciation, maintenance costs, cash flow, and staff accountability.

## Core Value

**Business visibility and cash control** — owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly. The rental workflow is table stakes; the profitability intelligence is the differentiator.

## Current State

**v1.0 Shipped:** 2026-01-21

The Cashier Till & End of Day Reconciliation milestone is complete. Staff can open/close tills with multi-currency support, process payments in THB/USD/EUR/CNY with denomination counting, and managers have full oversight with variance alerts and daily close operations.

**Tech Stack:**
- Blazor Server + WASM hybrid (PWA)
- .NET 10
- SQL Server with JSON columns
- 30,215 LOC (C#, Razor, SQL)

## Requirements

### Validated

<!-- Shipped and confirmed working -->

**Pre-existing capabilities:**
- Multi-tenant architecture with schema-per-tenant isolation — existing
- OAuth authentication (Google, Microsoft, LINE) with role-based access — existing
- Super admin impersonation for support — existing
- Multi-shop management with vehicle pooling across locations — existing
- Vehicle inventory management (motorbikes, scooters, cars, boats, jet skis, vans) — existing
- Insurance packages and accessories management — existing
- Renter management with passport/license OCR verification (Gemini Flash) — existing
- Rental check-in wizard (5 steps: vehicle, documents, deposit, insurance, accessories) — existing
- Rental check-out with damage assessment — existing
- Active rentals dashboard — existing
- Payment processing (Cash, Card, PromptPay, BankTransfer) — existing
- Invoice generation — existing
- Deposit tracking and refunds — existing
- Daily/weekly/monthly financial reports — existing
- Owner payments tracking — existing
- Accident/incident reporting — existing
- Maintenance tracking and scheduling — existing
- Service locations with drop-off fees — existing
- Tourist portal with online reservations — existing
- PWA with offline support and camera access — existing

**v1.0 Cashier Till & End of Day Reconciliation:**
- Cashier till opening with starting float per currency — v1.0
- Multi-currency payment acceptance (THB, USD, EUR, CNY) with operator-set exchange rates — v1.0
- Change calculation in THB when paying with foreign currency — v1.0
- Till payment receiving linked to rental transactions — v1.0
- Till payouts (deposit refunds, expenses) with reason tracking — v1.0
- Cash drops to safe during shift — v1.0
- End of day till closing and reconciliation — v1.0
- Per-currency balance tracking (expected vs actual) — v1.0
- Cash shortage/overage logging against staff member — v1.0
- Till receipts and transaction history — v1.0
- Manager oversight of all tills and shifts — v1.0
- Denomination counting at open and close — v1.0
- Transaction voids with manager PIN approval — v1.0
- Daily close with day locking — v1.0

### Active

<!-- Next milestone scope - define in /gsd:new-milestone -->

(No active requirements - run `/gsd:new-milestone` to define v1.1 or v2.0 scope)

### Out of Scope

- Currency conversion between till currencies — handled by separate forex department
- Real-time currency rate feeds — operators set their own rates
- Credit card terminal integration — record payments only, no POS hardware integration
- Accounting system export (QuickBooks, Xero) — future milestone
- Push notifications — infrastructure ready but deferred
- Alipay/WeChat Pay integration — future milestone for Chinese tourists

## Context

**Existing business:** We already serve money changers in Thai tourist areas with a forex POS system. Vehicle rental is an adjacent market opportunity using the same relationships and geographic presence.

**Target market:** Small-to-mid size rental operators. Large operators have existing software. Our sweet spot is digitizing paper-based operations.

**Multi-currency expertise:** Our forex background means multi-currency cash handling is a natural fit. Most rental software doesn't handle this well.

**Current codebase:** Blazor Server + WASM hybrid, .NET 10, SQL Server with JSON columns, custom repository pattern. v1.0 milestone complete.

## Constraints

- **Tech stack**: Blazor Server + WASM, .NET 10, SQL Server — must integrate with existing architecture
- **Multi-tenancy**: Schema-per-tenant isolation — till tables must follow this pattern
- **Localization**: English/Thai/Malay — all new UI must be localized
- **Mobile-first**: PWA already supports offline/camera — till UI should work on tablets at desk

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Base currency THB | Thailand market, rental rates always in THB | Good |
| Change always in THB | Simplifies till reconciliation, matches tourist expectations | Good |
| Per-currency till tracking | Matches forex expertise, enables accurate reconciliation | Good |
| Shortage logged, not enforced | Policy decision left to manager, system just provides visibility | Good |
| Scoped cart (1 booking/rental per receipt) | Not general POS, focused on rental workflow | Good |
| Manager PIN for void approval | Quick authorization without full login swap | Good |
| Denomination counting at open/close | Accurate verification, reduces counting errors | Good |
| Daily close locking | Prevents backdating transactions | Good |

---
*Last updated: 2026-01-21 after v1.0 milestone*
