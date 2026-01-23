---
phase: 06-denomination-counting
plan: 03
subsystem: till-ui
tags: [blazor, denomination-counting, closing-count, variance, localization]
dependency-graph:
  requires: [06-01]
  provides: [closing-count-panel, denomination-variance-ui]
  affects: [06-04, 07-eod-reports]
tech-stack:
  added: []
  patterns: [variance-calculation-ui, sticky-footer, multi-currency-display]
file-tracking:
  key-files:
    created:
      - src/MotoRent.Client/Components/Till/ClosingCountPanel.razor
      - src/MotoRent.Client/Components/Till/ClosingCountPanel.razor.css
      - src/MotoRent.Client/Resources/Components/Till/ClosingCountPanel.resx
      - src/MotoRent.Client/Resources/Components/Till/ClosingCountPanel.en.resx
      - src/MotoRent.Client/Resources/Components/Till/ClosingCountPanel.th.resx
      - src/MotoRent.Client/Resources/Components/Till/ClosingCountPanel.ms.resx
    modified:
      - src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor
decisions:
  - id: closing-count-variance-inline
    choice: "Inline variance display per currency section"
    rationale: "Immediate feedback shows staff where discrepancies exist"
  - id: expected-from-session
    choice: "Use CurrencyBalances from TillSession"
    rationale: "Tracked balances reflect actual cash movements during session"
  - id: variance-color-coding
    choice: "Green (balanced), Red (short), Blue (over)"
    rationale: "Standard variance indicator colors; blue for over distinguishes from red danger"
  - id: sticky-footer-summary
    choice: "Sticky footer with overall variance and grand total"
    rationale: "Summary visible while scrolling through denomination entries"
metrics:
  duration: "~15 minutes"
  completed: 2026-01-20
---

# Phase 6 Plan 3: Closing Count Panel Summary

**One-liner:** ClosingCountPanel component with expected/actual/variance display per currency, integrated into TillCloseSessionDialog.

## What Was Done

### Task 1: Create ClosingCountPanel Component
**Commit:** ffe419e

Created `ClosingCountPanel.razor` (315 lines) - a denomination entry panel specifically designed for closing count workflow with variance tracking.

**Component Features:**
- Vertical denomination entry layout matching DenominationEntryPanel style
- Per-currency sections with expected balance badge in header
- Inline variance display: Expected, Actual, Variance per currency
- Variance color coding: green (balanced), red (short), blue (over)
- Increment/decrement buttons with 44px touch targets
- THB always shown; foreign currencies only if expected balance > 0
- Sticky footer with overall variance and grand total in THB
- Exchange rate loading for foreign currency THB conversion

**CSS Styling (220 lines):**
- `.mr-variance-balanced`, `.mr-variance-short`, `.mr-variance-over` classes
- `.mr-expected-badge` for header expected amount
- `.mr-section-summary` for per-currency Expected/Actual/Variance
- `.mr-sticky-footer` with overall variance and grand total
- Mobile-responsive adjustments

### Task 2: Update TillCloseSessionDialog
**Commit:** b65655c

Updated `TillCloseSessionDialog.razor` to use the new ClosingCountPanel:
- Replaced simple amount input with ClosingCountPanel
- Uses `CurrencyBalances` from TillSession for expected values
- Falls back to `ExpectedCash` for backward compatibility
- Compact session summary (Opening, CashIn, CashOut)
- Saves denomination breakdown via `SaveDenominationCountAsync`
- Variance acknowledgment triggers on any currency variance
- Scrollable modal body for long denomination lists

### Task 3: Create Localization Resources
**Commit:** 3ea3987

Created 4 resource files with 16 keys each:
- `ClosingCountPanel.resx` (default)
- `ClosingCountPanel.en.resx` (English)
- `ClosingCountPanel.th.resx` (Thai)
- `ClosingCountPanel.ms.resx` (Malay)

**Keys:** ThailandBaht, USDollar, Euro, ChineseYuan, Expected, Actual, Variance, Balanced, Over, Short, Total, GrandTotal, OverallVariance, NoForeignCurrency, CountAllDenominations, VarianceWarning

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Inline variance per section | Immediate feedback where discrepancies exist |
| Expected from CurrencyBalances | Tracked balances reflect actual movements |
| Green/Red/Blue variance colors | Standard indicators; blue distinguishes over from error |
| Sticky footer always visible | Summary accessible while scrolling denominations |
| THB always shown | Required for closing; foreign only if balance > 0 |
| Backward compatible ActualCash | Maintains CloseSessionAsync contract |

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

1. **Build:** Full solution builds successfully
2. **Component structure:** ClosingCountPanel renders with expected/actual/variance
3. **Expected balance:** Shows per currency from TillSession.CurrencyBalances
4. **Variance calculation:** Displayed inline per currency section
5. **Currency visibility:** THB always, foreign only if balance > 0
6. **Overall variance:** Shown in sticky footer
7. **Localization:** EN/TH/MS complete

## Files Changed

| File | Lines | Change |
|------|-------|--------|
| ClosingCountPanel.razor | 315 | Created |
| ClosingCountPanel.razor.css | 220 | Created |
| TillCloseSessionDialog.razor | 180 | Updated |
| ClosingCountPanel.resx | 63 | Created |
| ClosingCountPanel.en.resx | 63 | Created |
| ClosingCountPanel.th.resx | 63 | Created |
| ClosingCountPanel.ms.resx | 63 | Created |

## Next Phase Readiness

**Ready for:**
- 06-04: History/Detail Views for denomination counts

**Prerequisites met:**
- ClosingCountPanel saves to TillDenominationCount via TillService
- Variance data stored with breakdowns for reporting
- UI patterns established for denomination display
