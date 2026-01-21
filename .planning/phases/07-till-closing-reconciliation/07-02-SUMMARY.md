---
phase: 07
plan: 02
subsystem: till-ui
tags: [close-dialog, summary-step, variance-display, localization]

dependency-graph:
  requires:
    - "07-01: CloseSessionAsync overload with multi-currency breakdowns"
    - "06-03: ClosingCountPanel with denomination entry"
  provides:
    - "Two-step close workflow: Count -> Summary -> Confirm"
    - "Per-currency variance table with color coding"
    - "Overall variance in THB display"
  affects:
    - "07-03: Session history will show close variance data"

tech-stack:
  added: []
  patterns:
    - "Two-step dialog workflow with state toggle"
    - "Color-coded variance display (green/blue/red)"
    - "Exchange rate-based THB conversion"

key-files:
  created:
    - "src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.ms.resx"
  modified:
    - "src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor"
    - "src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.resx"
    - "src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.en.resx"
    - "src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.th.resx"

decisions:
  - id: "two-step-workflow"
    choice: "State toggle (m_showSummary) for step navigation"
    reason: "Simple state machine, no complex wizard framework needed"
  - id: "variance-colors"
    choice: "text-success (0), text-info (over), text-danger (short)"
    reason: "Standard Bootstrap colors, consistent with ClosingCountPanel"
  - id: "overall-variance-thb"
    choice: "Sum of per-currency variances converted to THB"
    reason: "Single comparable metric for owner/manager review"

metrics:
  duration: "~6 minutes"
  completed: "2026-01-21"
---

# Phase 7 Plan 02: Close Dialog UI Integration Summary

Two-step close workflow with summary review before confirmation

## What Was Built

### Two-Step Close Workflow
Modified `TillCloseSessionDialog.razor` to implement:

**Step 1: Denomination Entry**
- Session summary (Opening, Cash In, Cash Out)
- ClosingCountPanel for denomination counting
- Closing notes textarea
- "Review Summary" button to proceed

**Step 2: Summary Review**
- Per-currency variance table (Currency | Expected | Counted | Variance)
- Color-coded variance values:
  - Green (`text-success`) for balanced (variance = 0)
  - Blue (`text-info`) for over (variance > 0)
  - Red (`text-danger`) for short (variance < 0)
- Overall variance in THB with alert styling
- Variance acknowledgment checkbox (if any variance exists)
- "Back to Count" button to edit
- "Close Session" button to confirm

### Updated SaveAsync
Changed from old single-currency API to new multi-currency overload:
```csharp
// Old: CloseSessionAsync(sessionId, actualCash, notes, username)
// New: CloseSessionAsync(sessionId, breakdowns, notes, username)
```

### Localization
Added 8 new keys to all resource files:
- `ClosingSummary` - "Closing Summary"
- `Currency` - "Currency"
- `Expected` - "Expected"
- `Counted` - "Counted"
- `Variance` - "Variance"
- `OverallVarianceThb` - "Overall Variance (THB)"
- `ReviewSummary` - "Review Summary"
- `BackToCount` - "Back to Count"

Created new `TillCloseSessionDialog.ms.resx` with full Malay translations.

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Add summary step to TillCloseSessionDialog | f0a6289 |
| 2 | Add localization resources | ab83950 |

## Deviations from Plan

None - plan executed exactly as written.

## Technical Notes

### State Management
Simple boolean toggle controls which step is displayed:
```csharp
private bool m_showSummary;

private void ReviewSummary() { m_showSummary = true; }
private void BackToCount() { m_showSummary = false; }
```

### Variance Formatting
Helper methods for consistent display:
- `GetVarianceClass(decimal)` - Returns CSS class for color coding
- `FormatVariance(decimal, string)` - Returns "+/- symbol + amount"
- `GetOverallVarianceThb()` - Sums variances with exchange rate conversion

### Exchange Rate Loading
Rates loaded in `OnParametersSetAsync` for THB conversion:
```csharp
private async Task LoadExchangeRatesAsync()
{
    var currencies = new[] { USD, EUR, CNY };
    foreach (var currency in currencies)
    {
        var rate = await ExchangeRateService.GetCurrentRateAsync(currency);
        if (rate != null) m_exchangeRates[currency] = rate.BuyRate;
    }
}
```

## Next Phase Readiness

**Ready for 07-03**: Session Close Summary View
- Close dialog now stores per-currency variances via new API
- All variance data available in TillSession for history display
- Force close continues to work independently

## Files Changed

```
src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor      +267 lines (refactored)
src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.resx      +24 lines
src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.en.resx   +24 lines
src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.th.resx   +24 lines
src/MotoRent.Client/Resources/Pages/Staff/TillCloseSessionDialog.ms.resx   (new, 74 lines)
```

---

*Plan: 07-02-PLAN.md | Completed: 2026-01-21*
