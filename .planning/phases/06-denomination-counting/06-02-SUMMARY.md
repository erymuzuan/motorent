---
phase: 06
plan: 02
subsystem: till-denomination
tags: [blazor, component, ui, denomination-counting, opening-float, localization]

# Dependency graph
requires:
  - phase-06-01 (TillDenominationCount entity and service methods)
  - phase-02 (DenominationEntryPanel concept)
provides:
  - OpeningFloatPanel vertical denomination entry component
  - TillOpenSessionDialog integration with denomination breakdown
  - Denomination counts saved on session open
affects:
  - phase-06-03 (Closing Count Dialog will follow similar pattern)
  - phase-07 (EOD Reconciliation will read opening denomination counts)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Vertical list layout for denomination entry (touch-optimized)
    - Sticky footer for running total display
    - Add/remove currency sections dynamically
    - Exchange rate conversion for grand total

# File tracking
key-files:
  created:
    - src/MotoRent.Client/Components/Till/OpeningFloatPanel.razor
    - src/MotoRent.Client/Components/Till/OpeningFloatPanel.razor.css
    - src/MotoRent.Client/Resources/Components/Till/OpeningFloatPanel.resx
    - src/MotoRent.Client/Resources/Components/Till/OpeningFloatPanel.en.resx
    - src/MotoRent.Client/Resources/Components/Till/OpeningFloatPanel.th.resx
    - src/MotoRent.Client/Resources/Components/Till/OpeningFloatPanel.ms.resx
  modified:
    - src/MotoRent.Client/Pages/Staff/TillOpenSessionDialog.razor
    - src/MotoRent.Client/Resources/Pages/Staff/TillOpenSessionDialog.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillOpenSessionDialog.th.resx

# Decisions
decisions:
  - id: vertical-list-layout
    decision: "Single column vertical list for denomination entry"
    rationale: "Sequential counting workflow, larger touch targets for mobile"
  - id: thb-always-visible
    decision: "THB section always visible, foreign currencies added on demand"
    rationale: "THB is base currency, most sessions only count THB"
  - id: sticky-footer-total
    decision: "Sticky footer shows grand total with breakdown"
    rationale: "Always visible running total during counting"
  - id: increment-decrement-buttons
    decision: "+-buttons with 44px touch target plus manual input"
    rationale: "Fast counting with tap, precise entry with keyboard"
  - id: breakdowns-callback
    decision: "EventCallback for breakdowns change notification"
    rationale: "Parent component controls saving, panel is reusable"

# Metrics
metrics:
  duration: ~8 minutes
  completed: 2026-01-20
---

# Phase 06 Plan 02: Opening Float Dialog Summary

OpeningFloatPanel component and TillOpenSessionDialog integration for denomination-based opening float entry.

## One-liner

Vertical denomination entry panel with touch-friendly increment/decrement buttons, dynamic foreign currency support, and sticky grand total footer integrated into session opening workflow.

## What Was Built

### OpeningFloatPanel Component (317 lines)
- Vertical denomination list layout (single column)
- THB section always visible with all denominations (1000, 500, 100, 50, 20, 10, 5, 2, 1)
- Foreign currency sections added via "Add Currency" buttons (USD, EUR, CNY)
- Remove button on foreign currency sections
- Increment/decrement buttons with 44px touch targets
- Manual count input with number validation
- Per-denomination subtotal display
- Per-currency total with THB equivalent for foreign
- Sticky footer with grand total breakdown and THB value

### OpeningFloatPanel CSS
- `.mr-opening-float-panel` container with padding for sticky footer
- `.mr-currency-section` bordered sections per currency
- `.mr-denom-row` flex layout with value, counter, subtotal
- `.mr-denom-btn` 44px touch-friendly buttons
- `.mr-denom-count` centered number input (60px)
- `.mr-sticky-footer` fixed position with grand total display
- Mobile-responsive adjustments for smaller screens

### TillOpenSessionDialog Updates
- Replaced simple amount input with OpeningFloatPanel
- Added breakdowns callback handler
- Computed THB total from breakdowns
- Save denomination count after session creation
- Scrollable modal body for vertical list
- THB required validation with warning message

### Localization Resources (4 files)
All keys in EN, TH, MS:
- ThailandBaht, USDollar, Euro, ChineseYuan
- Total, GrandTotal, ThbEquivalent
- AddCurrency, RemoveCurrency, EnterCount
- NoForeignCurrency, TapToAddCurrency

## Commits

| Hash | Type | Description |
|------|------|-------------|
| b4294f8 | feat | Create OpeningFloatPanel component for denomination entry |
| bb0aac3 | feat | Update TillOpenSessionDialog to use OpeningFloatPanel |
| 6e63adf | feat | Add localization resources for OpeningFloatPanel |

## Key Patterns Established

### Vertical Denomination Entry Pattern
```razor
<div class="mr-denom-row">
    <span class="mr-denom-value">THB1,000</span>
    <div class="mr-denom-counter">
        <button class="mr-denom-btn" @onclick="() => Decrement(currency, denom)">-</button>
        <input type="number" class="mr-denom-count" @bind="count" />
        <button class="mr-denom-btn" @onclick="() => Increment(currency, denom)">+</button>
    </div>
    <span class="mr-denom-subtotal">THB5,000</span>
</div>
```

### Callback-based Parent Notification
```csharp
[Parameter]
public EventCallback<List<CurrencyDenominationBreakdown>> OnBreakdownsChanged { get; set; }

private async void NotifyChanged()
{
    var breakdowns = BuildBreakdowns();
    await OnBreakdownsChanged.InvokeAsync(breakdowns);
}
```

### Save Denomination Count After Session Open
```csharp
var result = await TillService.OpenSessionAsync(..., OpeningFloatThb, ...);
if (result.Success)
{
    var session = await TillService.GetActiveSessionAsync(shopId, userName);
    await TillService.SaveDenominationCountAsync(
        session.TillSessionId,
        DenominationCountType.Opening,
        m_currencyBreakdowns,
        userName,
        isFinal: true);
}
```

## Deviations from Plan

None - plan executed exactly as written.

## Testing Notes

- Full solution builds successfully (0 errors)
- OpeningFloatPanel renders vertical denomination list
- THB section visible by default
- Can add USD/EUR/CNY sections dynamically
- Running total updates in real-time
- Sticky footer shows grand total in THB
- TillOpenSessionDialog saves denomination breakdown on open

## Next Phase Readiness

Ready for Plan 06-03 (Closing Count Panel):
- OpeningFloatPanel provides reusable denomination entry pattern
- CSS classes can be shared or extended
- Closing panel will add expected balance display and variance calculation
- Similar pattern with additional closing-specific UI
