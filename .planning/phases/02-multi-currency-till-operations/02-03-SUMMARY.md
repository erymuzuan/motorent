---
phase: 02-multi-currency-till-operations
plan: 03
subsystem: till-ui
tags: [blazor, ui-component, multi-currency, cash-management]
completed: 2026-01-20
duration: 15m

requires:
  - 02-01: Domain extensions for multi-currency
  - 02-02: DenominationEntryPanel component

provides:
  - CurrencyBalancePanel: Collapsible per-currency balance display
  - Multi-currency cash drop: Tab-based denomination entry per currency
  - Till integration: CurrencyBalancePanel replaces simple hero display

affects:
  - 02-04: EOD reconciliation will use CurrencyBalances for per-currency verification

tech-stack:
  added: []
  patterns:
    - Collapsible panel with lazy-load THB equivalents
    - Tab navigation for multi-currency input
    - Denomination-based cash counting

key-files:
  created:
    - src/MotoRent.Client/Components/Till/CurrencyBalancePanel.razor
    - src/MotoRent.Client/Components/Till/CurrencyBalancePanel.razor.css
    - src/MotoRent.Client/Resources/Components/Till/CurrencyBalancePanel.resx
    - src/MotoRent.Client/Resources/Components/Till/CurrencyBalancePanel.en.resx
    - src/MotoRent.Client/Resources/Components/Till/CurrencyBalancePanel.th.resx
  modified:
    - src/MotoRent.Client/Pages/Staff/Till.razor
    - src/MotoRent.Client/Pages/Staff/TillCashDropDialog.razor
    - src/MotoRent.Client/Resources/Pages/Staff/TillCashDropDialog.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillCashDropDialog.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillCashDropDialog.th.resx

decisions:
  - id: collapsible-panel-default
    choice: Collapsed by default showing THB total
    reason: Mobile-first - minimize screen space, expand for details
  - id: backward-compatibility
    choice: GetCurrencyBalances() helper with fallback to ExpectedCash
    reason: Existing sessions without CurrencyBalances still work

metrics:
  tasks-completed: 3/3
  files-created: 5
  files-modified: 5
  loc-added: ~600
  loc-removed: ~80
---

# Phase 02 Plan 03: Currency Balance Display & Drop Dialog Summary

**One-liner:** Collapsible CurrencyBalancePanel shows per-currency balances with THB equivalents; TillCashDropDialog supports multi-currency drops with denomination entry and validation.

## What Was Built

### 1. CurrencyBalancePanel Component
New collapsible component displaying per-currency till balances:

- **Collapsed state:** Shows total THB equivalent for quick glance
- **Expanded state:** Lists each currency with balance and THB equivalent
- **Dynamic calculation:** Fetches exchange rates and computes THB equivalents on expand
- **Filtered display:** Only shows currencies with balance > 0
- **CSS styling:** Gradient header matching app theme

### 2. Till.razor Integration
Updated main till page:

- Replaced static "CASH BALANCE HERO" section with CurrencyBalancePanel
- Added `GetCurrencyBalances()` helper for backward compatibility
- Session metadata (opening float, dropped amount) shown in compact summary
- CashDropDialog now receives CurrencyBalances parameter

### 3. Multi-Currency Cash Drop Dialog
Complete rewrite of TillCashDropDialog:

- **Currency tabs:** Dynamically generated for currencies with balance > 0
- **Denomination entry:** Uses DenominationEntryPanel per currency
- **Per-currency validation:** Cannot drop more than available balance
- **Drop summary:** Shows all currencies being dropped at once
- **RecordMultiCurrencyDropAsync:** Calls service method for multi-currency support
- **Backward compatible:** Falls back to AvailableCash if CurrencyBalances empty

## Requirements Satisfied

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| TILL-04: View balance per currency | Done | CurrencyBalancePanel with collapsible display |
| TILL-05: Cash drop per currency | Done | Multi-currency TillCashDropDialog with tabs |

## Key Implementation Details

### CurrencyBalancePanel Parameters
```csharp
[Parameter] public Dictionary<string, decimal> CurrencyBalances { get; set; }
[Parameter] public EventCallback OnRefresh { get; set; }
```

### TillCashDropDialog Parameters
```csharp
[Parameter] public int SessionId { get; set; }
[Parameter] public bool IsDrop { get; set; }
[Parameter] public Dictionary<string, decimal> CurrencyBalances { get; set; }
[Parameter] public decimal AvailableCash { get; set; } // Legacy fallback
```

### Validation Logic
```csharp
// Per-currency drop validation
foreach (var currency in m_availableCurrencies)
{
    if (GetDropTotal(currency) > GetAvailableBalance(currency))
        return false;  // Cannot save
}
```

## Localization

### CurrencyBalancePanel (new)
| Key | English | Thai |
|-----|---------|------|
| TillBalance | Till Balance | ยอดเงินในลิ้นชัก |
| TotalThbEquivalent | Total THB Equivalent | รวมเทียบเท่า THB |

### TillCashDropDialog (added)
| Key | English | Thai |
|-----|---------|------|
| Available | Available | คงเหลือ |
| DropSummary | Drop Summary | สรุปการนำส่ง |
| EnterDropAmount | Enter amount to drop | กรุณาระบุจำนวนเงินที่จะนำส่ง |
| NoCurrenciesToDrop | No currencies with balance available to drop | ไม่มีสกุลเงินที่มียอดคงเหลือให้นำส่ง |
| FailedToRecordTransaction | Failed to record transaction | ไม่สามารถบันทึกรายการได้ |

## Commits

| Hash | Message | Files |
|------|---------|-------|
| d6e787b | feat(02-03): create CurrencyBalancePanel component | 5 new files |
| 8aed045 | feat(02-03): integrate CurrencyBalancePanel into Till.razor | Till.razor |
| 91deaa6 | feat(02-02): update dialogs with multi-currency denomination entry | TillCashDropDialog + localization |

## Deviations from Plan

**None - plan executed exactly as written.**

Note: Task 3 (TillCashDropDialog update) was previously implemented as part of 02-02 commit `91deaa6`. The code met all Task 3 requirements so no additional commit was needed.

## Next Phase Readiness

Ready for 02-04 (End of Day Reconciliation):
- CurrencyBalances dictionary tracked on TillSession
- Per-currency balance display available for EOD verification
- Multi-currency drop creates individual transactions per currency
- RecordMultiCurrencyDropAsync handles THB equivalents correctly

## Testing Notes

To verify implementation:
1. Open a till session
2. Receive foreign currency payment (USD, EUR, or CNY)
3. CurrencyBalancePanel should show THB + foreign currency balances
4. Click "Cash Drop" - tabs should appear for currencies with balance
5. Enter denomination counts - total should update in real-time
6. Summary should show all currencies being dropped
7. Drop should fail if exceeding available balance
