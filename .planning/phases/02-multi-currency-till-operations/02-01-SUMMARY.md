---
phase: 02-multi-currency-till-operations
plan: 01
subsystem: till-operations
tags: [multi-currency, till, exchange-rate, domain]

dependency-graph:
  requires:
    - phase-01: ExchangeRate entity and ExchangeRateService
  provides:
    - TillSession with CurrencyBalances tracking
    - TillTransaction with currency and exchange rate fields
    - CurrencyDenominations helper for denomination counting
    - TillService multi-currency methods
  affects:
    - 02-02: Will use CurrencyBalances for balance display
    - 02-03: Will use CurrencyDenominations for payment UI
    - 02-04: Will use RecordMultiCurrencyDropAsync for drop UI

tech-stack:
  added: []
  patterns:
    - Dictionary<string, decimal> for per-currency tracking
    - Record types for conversion results (ExchangeConversionResult)

key-files:
  created:
    - src/MotoRent.Domain/Entities/CurrencyDenominations.cs
  modified:
    - src/MotoRent.Domain/Entities/TillSession.cs
    - src/MotoRent.Domain/Entities/TillTransaction.cs
    - src/MotoRent.Services/TillService.cs

decisions:
  - id: D1
    what: Use Dictionary<string,decimal> for CurrencyBalances
    why: Flexible, allows any currency; initialized with supported currencies on session open
  - id: D2
    what: Defaults ensure backward compatibility
    why: Existing THB-only transactions continue to work with Currency="THB", ExchangeRate=1.0

metrics:
  duration: ~4 minutes
  completed: 2026-01-20
---

# Phase 2 Plan 1: Domain Extensions for Multi-Currency Till Summary

**One-liner:** Extended TillSession/TillTransaction entities and TillService with per-currency balance tracking and foreign currency payment methods.

## What Was Built

### Entity Extensions

**TillSession.cs:**
- Added `CurrencyBalances` property (`Dictionary<string, decimal>`) to track actual foreign currency amounts in drawer
- Added `GetCurrencyBalance(string currency)` helper method that returns 0 if currency not tracked
- XML documentation explains THB = drawer balance, foreign = un-converted amounts received

**TillTransaction.cs:**
- Added `Currency` property (default: THB) for backward compatibility
- Added `ExchangeRate` property (default: 1.0m) for THB equivalent calculation
- Added `AmountInBaseCurrency` property for THB equivalent amount
- Added `ExchangeRateSource` property for audit trail
- Added `ExchangeRateId` property to link to ExchangeRate entity

### New Helper Class

**CurrencyDenominations.cs:**
- Static class with denomination arrays for THB, USD, EUR, CNY
- `GetDenominations(string currency)` returns denomination array for cash counting
- `GetCurrencySymbol(string currency)` returns currency symbols for display
- `CurrenciesWithDenominations` list of currencies supporting denomination counting

### Service Extensions

**TillService.cs:**
- Injected `ExchangeRateService` dependency for currency conversion
- Updated `OpenSessionAsync` to initialize CurrencyBalances with THB = openingFloat, foreign = 0
- Updated `RecordCashInAsync` to set currency fields and update CurrencyBalances for THB
- Added `RecordForeignCurrencyPaymentAsync` for foreign currency with conversion and audit trail
- Added `RecordMultiCurrencyDropAsync` for dropping multiple currencies in single operation
- Added `CurrencyDropAmount` DTO for drop operations

## Technical Decisions

1. **CurrencyBalances as Dictionary** - Flexible approach allows tracking any currency without schema changes. Initialized with supported currencies on session open.

2. **Defaults for Backward Compatibility** - All new properties have defaults (Currency=THB, ExchangeRate=1.0, Source=Base) so existing THB transactions work without modification.

3. **THB Equivalent for Totals** - TotalCashIn/TotalDropped always use THB equivalent for reconciliation math, while CurrencyBalances track actual foreign amounts.

4. **Single SubmitChanges for Multi-Currency Drop** - All drop transactions and session update in one atomic operation.

## Verification Checks

| Check | Status |
|-------|--------|
| `dotnet build` passes | Pass |
| TillSession.CurrencyBalances exists | Pass |
| TillTransaction currency fields exist | Pass |
| CurrencyDenominations.GetDenominations works | Pass |
| TillService has multi-currency methods | Pass |
| Existing RecordCashInAsync still works | Pass |

## Deviations from Plan

None - plan executed exactly as written.

## Commits

| Hash | Message |
|------|---------|
| ca8a459 | feat(02-01): extend entities for multi-currency till tracking |
| 9255f35 | feat(02-01): extend TillService with multi-currency methods |

## Next Steps

Plan 02-02: Balance Display & Summary Panel - Build UI to show per-currency balances using CurrencyBalances property.
