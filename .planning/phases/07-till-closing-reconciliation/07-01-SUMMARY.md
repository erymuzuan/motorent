---
phase: 07
plan: 01
subsystem: till-service
tags: [close-metadata, multi-currency, variance-tracking, force-close]

dependency-graph:
  requires:
    - "06-01: TillDenominationCount entity with CurrencyDenominationBreakdown"
  provides:
    - "TillSession extended with close metadata (ClosedByUserName, ActualBalances, ClosingVariances)"
    - "CloseSessionAsync overload for multi-currency reconciliation"
    - "ForceCloseSessionAsync for manager-approved emergency close"
  affects:
    - "07-02: Close dialog will use new CloseSessionAsync overload"
    - "07-03: Summary view will read ClosingVariances for display"

tech-stack:
  added: []
  patterns:
    - "Multi-currency variance tracking via dictionaries"
    - "Force-close with manager attribution"
    - "Backward-compatible overloads"

key-files:
  created: []
  modified:
    - "src/MotoRent.Domain/Entities/TillSession.cs"
    - "database/tables/MotoRent.TillSession.sql"
    - "src/MotoRent.Services/TillService.Session.cs"

decisions:
  - id: "per-currency-variance"
    choice: "Store variances as Dictionary<string, decimal>"
    reason: "Flexible per-currency tracking, matches CurrencyBalances pattern"
  - id: "force-close-zero-variance"
    choice: "Force close sets actual = expected (zero variance)"
    reason: "Manager approved bypass should not create phantom variances"
  - id: "backward-compatible-close"
    choice: "Keep original CloseSessionAsync, add overload"
    reason: "Existing code continues to work, new code uses richer API"

metrics:
  duration: "~8 minutes"
  completed: "2026-01-21"
---

# Phase 7 Plan 01: Session Close Metadata Summary

Per-currency variance tracking at till close with manager force-close capability

## What Was Built

### TillSession Entity Extensions
Extended `TillSession` with 5 new fields for close metadata:
- `ClosedByUserName` - Staff who closed the session (accountability)
- `IsForceClose` - Whether manager bypass was used
- `ForceCloseApprovedBy` - Manager who approved force close
- `ActualBalances` - Dictionary of actual counted amounts per currency
- `ClosingVariances` - Dictionary of variance (actual - expected) per currency

### SQL Table Updates
Added 2 computed columns for indexing/querying:
- `ClosedByUserName` - For querying sessions by closer
- `IsForceClose` - For filtering force-closed sessions

### TillService Methods
1. **CloseSessionAsync (new overload)** - Accepts `List<CurrencyDenominationBreakdown>`:
   - Calculates per-currency actuals and variances
   - Maintains backward compatibility with existing THB-only fields
   - Sets status to `ClosedWithVariance` if ANY currency has variance

2. **ForceCloseSessionAsync** - Manager-approved emergency close:
   - Sets all actuals equal to expected (zero variance by definition)
   - Marks session as force-closed with manager attribution
   - Useful when physical count is impractical (emergency, end-of-day)

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Extend TillSession entity with close metadata | 202edfc |
| 2 | Add SQL computed columns for close metadata | df45b84 |
| 3 | Add CloseSessionAsync overload with multi-currency support | 35419c1 |
| 4 | Implement ForceCloseSessionAsync | b505e45 |

## Deviations from Plan

None - plan executed exactly as written.

## Technical Notes

### Variance Calculation
- Variance = Actual - Expected
- Positive = over (extra cash found)
- Negative = short (cash missing)
- Zero = balanced (exact match)

### Status Determination
The new `CloseSessionAsync` overload checks ALL currencies:
```csharp
var hasAnyVariance = closingVariances.Values.Any(v => v != 0);
session.Status = hasAnyVariance
    ? TillSessionStatus.ClosedWithVariance
    : TillSessionStatus.Closed;
```

### Force Close Semantics
Force close assumes manager verified and accepted the expected balance:
```csharp
session.ActualBalances = new Dictionary<string, decimal>(session.CurrencyBalances);
session.ClosingVariances = session.CurrencyBalances.ToDictionary(
    kvp => kvp.Key,
    kvp => 0m
);
```

## Next Phase Readiness

**Ready for 07-02**: Close Dialog UI Integration
- `CloseSessionAsync` overload ready to accept breakdowns from ClosingCountPanel
- `ForceCloseSessionAsync` ready for manager PIN integration
- All metadata fields will be populated at close time

## Files Changed

```
src/MotoRent.Domain/Entities/TillSession.cs   +20 lines
database/tables/MotoRent.TillSession.sql      +2 lines
src/MotoRent.Services/TillService.Session.cs  +88 lines
```

---

*Plan: 07-01-PLAN.md | Completed: 2026-01-21*
