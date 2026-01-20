# State: MotoRent Cashier Till

**Milestone:** Cashier Till & End of Day Reconciliation
**Started:** 2026-01-19

---

## Project Reference

**Core Value:** Business visibility and cash control - owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly.

**Current Focus:** Unified transaction workflow with item confirmation and multi-currency payment terminal. Staff search for bookings/rentals, review items in fullscreen dialog, then process split payments.

**Key Constraints:**
- Tech stack: Blazor Server + WASM, .NET 10, SQL Server
- Multi-tenancy: Schema-per-tenant isolation
- Localization: English/Thai/Malay
- Mobile-first: PWA on tablets at desk

---

## Current Position

**Phase:** 2 of 9 (Multi-Currency Till Operations) - COMPLETE
**Next Phase:** 3 of 9 (Transaction Search & Item Confirmation)
**Status:** Ready for Phase 3 planning

```
Milestone Progress: [###.......] 25%
Phase 2 Progress:   [##########] 100%
Phase 3 Progress:   [..........] 0%
```

**Last Activity:** 2026-01-20 - Roadmap restructured with 3 new phases for till redesign

**Next Action:** Run `/gsd:discuss-phase 3` to gather context for Phase 3 (Transaction Search & Item Confirmation).

---

## Roadmap Update Summary

### Phases Inserted (2026-01-20)

| Phase | Name | Goal |
|-------|------|------|
| 3 | Transaction Search & Item Confirmation | Staff search for booking/rental, edit items in fullscreen dialog |
| 4 | Payment Terminal Redesign | Multi-currency split payments with THB keypad and denomination counting |
| 5 | Refunds & Corrections | Deposit refunds, overpayment refunds, voids with manager approval |

### Renumbered Phases

| Old | New | Name |
|-----|-----|------|
| 3 | 6 | Denomination Counting |
| 4 | 7 | Till Closing and Reconciliation |
| 5 | 8 | Manager Oversight |
| 6 | 9 | End of Day Operations |

---

## Performance Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| Plans completed | 7 | Phase 1: 4 plans; Phase 2: 3 plans |
| Requirements done | 10/40 | RATE-01-05, TILL-01-05 |
| Phases done | 2/9 | Phases 1-2 verified complete |
| Blockers hit | 0 | - |

---

## Accumulated Context

### Key Decisions

| Decision | Rationale | Date |
|----------|-----------|------|
| Base currency THB | Thailand market, rental rates always in THB | 2026-01-19 |
| Change always in THB | Simplifies till reconciliation, matches tourist expectations | 2026-01-19 |
| Per-currency till tracking | Matches forex expertise, enables accurate reconciliation | 2026-01-19 |
| Scoped cart (1 booking/rental per receipt) | Not general POS, focused on rental workflow | 2026-01-20 |
| Single "New Transaction" entry point | Staff search for booking/rental rather than manual item entry | 2026-01-20 |
| Auto-detect transaction type | System determines booking deposit, check-in, or check-out from entity status | 2026-01-20 |
| Fullscreen item confirmation | Dedicated focus, reduces errors, shows all details before payment | 2026-01-20 |
| THB keypad, foreign denomination counting | Speed for THB, accuracy for foreign currency | 2026-01-20 |
| Split payments across methods/currencies | Tourists often mix cash, card, and currencies | 2026-01-20 |
| Manager PIN for void approval | Quick authorization without full login swap | 2026-01-20 |
| Voids preserved for audit trail | Accountability and reconciliation | 2026-01-20 |

### Architecture Notes

**Entities Extended (Phase 2):**
- `TillSession` - Added CurrencyBalances dictionary, GetCurrencyBalance helper
- `TillTransaction` - Added Currency, ExchangeRate, AmountInBaseCurrency, ExchangeRateSource, ExchangeRateId

**New Classes Created (Phase 2):**
- `CurrencyDenominations` - Static helper with denomination arrays for THB, USD, EUR, CNY
- `CurrencyDropAmount` - DTO for multi-currency drop operations

**New Components Created (Phase 2):**
- `DenominationEntryPanel` - Reusable denomination counting panel
- `CurrencyBalancePanel` - Collapsible per-currency balance display with THB equivalents

**Services Extended (Phase 2):**
- `TillService` - Added RecordForeignCurrencyPaymentAsync, RecordMultiCurrencyDropAsync; injected ExchangeRateService

**Existing from Phase 1:**
- `ExchangeRate` - Organization-scoped with BuyRate, Source, EffectiveDate, IsActive
- `ExchangeRateService` - Rate management (get current, set new, convert, history, API stub)
- `/settings/exchange-rates` - Manager settings page for rate configuration
- `ExchangeRatePanel` - Staff-facing panel with FAB, rate list, and calculator

### Deferred Ideas (Future TODO)

- Receipt designer (A4 layout customization)
- Walk-in sales mode (general POS without booking/rental)
- Configurable currencies per organization
- GBP and JPY currency support

### TODOs

- [ ] Confirm with user: Is offline/PWA support critical for MVP?
- [ ] Confirm with user: What is the variance tolerance for auto-acceptance?
- [ ] Confirm with user: Should exchange rates expire automatically?

### Blockers

None currently.

---

## Session Continuity

**Last Session:** 2026-01-20 - Roadmap restructured with 3 new phases for till redesign

**Context for Next Session:**
- Roadmap updated: 9 phases total (was 6)
- Phases 3, 4, 5 inserted for till workflow redesign:
  - Phase 3: Transaction Search & Item Confirmation
  - Phase 4: Payment Terminal Redesign
  - Phase 5: Refunds & Corrections
- Original phases 3-6 renumbered to 6-9
- Context files created for each new phase in `.planning/phases/`
- Phase 2 infrastructure (multi-currency tracking, denomination counting) ready to support new phases
- Ready to begin Phase 3 planning

**Files to Review:**
- `.planning/ROADMAP.md` - Updated roadmap with 9 phases
- `.planning/phases/03-transaction-search-items/03-CONTEXT.md` - Phase 3 context
- `.planning/phases/04-payment-terminal/04-CONTEXT.md` - Phase 4 context
- `.planning/phases/05-refunds-corrections/05-CONTEXT.md` - Phase 5 context

---

*Last updated: 2026-01-20*
