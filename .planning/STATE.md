# State: MotoRent Cashier Till

**Milestone:** Cashier Till & End of Day Reconciliation
**Started:** 2026-01-19

---

## Project Reference

**Core Value:** Business visibility and cash control - owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly.

**Current Focus:** Multi-currency cash management for Thailand's tourist rental market. Extend existing till system to support per-currency balance tracking, exchange rate management, and end-of-day reconciliation.

**Key Constraints:**
- Tech stack: Blazor Server + WASM, .NET 10, SQL Server
- Multi-tenancy: Schema-per-tenant isolation
- Localization: English/Thai/Malay
- Mobile-first: PWA on tablets at desk

---

## Current Position

**Phase:** 1 - Exchange Rate Foundation
**Plan:** Not started
**Status:** Awaiting planning

```
Milestone Progress: [..........] 0%
Phase 1 Progress:   [..........] 0%
```

**Next Action:** Run `/gsd:plan-phase 1` to create implementation plans for Exchange Rate Foundation.

---

## Performance Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| Plans completed | 0 | - |
| Requirements done | 0/26 | - |
| Phases done | 0/6 | - |
| Blockers hit | 0 | - |

---

## Accumulated Context

### Key Decisions

| Decision | Rationale | Date |
|----------|-----------|------|
| Base currency THB | Thailand market, rental rates always in THB | 2026-01-19 |
| Change always in THB | Simplifies till reconciliation, matches tourist expectations | 2026-01-19 |
| Per-currency till tracking | Matches forex expertise, enables accurate reconciliation | 2026-01-19 |
| Shortage logged not enforced | Policy decision left to manager, system provides visibility | 2026-01-19 |
| Extend existing entities | TillSession/TillTransaction already exist; add currency fields | 2026-01-19 |
| New ExchangeRate entity | Organization-scoped with BuyRate/SellRate and effective dates | 2026-01-19 |

### Architecture Notes

**Existing Entities to Extend:**
- `TillSession` - Add per-currency balance tracking (CurrencyBalances, OpeningFloatByCurrency, ActualCashByCurrency)
- `TillTransaction` - Add Currency, ExchangeRate, AmountInBaseCurrency fields
- `ReceiptPayment` - Already supports multi-currency (Currency, ExchangeRate, AmountInBaseCurrency)

**New Entities:**
- `ExchangeRate` - Organization-scoped with BuyRate, SellRate, Source, EffectiveDate
- `CurrencyBalance` - Embedded class for per-currency tracking

**Services to Extend:**
- `TillService` - Multi-currency transaction recording, per-currency reconciliation
- New `ExchangeRateService` - Rate management (get current buy/sell, set new rates, history)

### TODOs

- [ ] Confirm with user: Is offline/PWA support critical for MVP?
- [ ] Confirm with user: What is the variance tolerance for auto-acceptance?
- [ ] Confirm with user: Do shops need full denomination tracking or per-currency total?
- [ ] Confirm with user: Should exchange rates expire automatically?

### Blockers

None currently.

---

## Session Continuity

**Last Session:** 2026-01-19 - Initial roadmap creation

**Context for Next Session:**
- Roadmap created with 6 phases covering all 26 requirements
- Phase 1 focuses on exchange rate foundation (RATE-01 through RATE-05)
- Existing codebase has TillSession, TillTransaction, ReceiptPayment entities
- ReceiptPayment already has multi-currency fields; TillSession needs extension
- Research summary suggests ExchangeRate entity with BuyRate/SellRate per currency

**Files to Review:**
- `.planning/ROADMAP.md` - Phase structure and success criteria
- `.planning/REQUIREMENTS.md` - Requirement details and traceability
- `.planning/research/SUMMARY.md` - Technical research findings

---

*Last updated: 2026-01-19*
