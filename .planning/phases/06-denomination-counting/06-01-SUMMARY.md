---
phase: 06
plan: 01
subsystem: till-denomination
tags: [entity, sql, service, denomination-counting, cash-reconciliation]

# Dependency graph
requires:
  - phase-02 (TillSession and multi-currency support)
  - phase-04 (DenominationEntryPanel concept)
provides:
  - TillDenominationCount entity with denomination breakdown storage
  - SQL table for persistence
  - TillService methods for saving/loading denomination counts
affects:
  - phase-06-02 (Opening Float Dialog will use TillDenominationCount)
  - phase-06-03 (Closing Count Dialog will use TillDenominationCount)
  - phase-07 (EOD Reconciliation will read denomination counts)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - CurrencyDenominationBreakdown as value object with computed Total
    - Variance calculation at closing (Expected - Actual)
    - Draft vs final count distinction

# File tracking
key-files:
  created:
    - src/MotoRent.Domain/Entities/TillDenominationCount.cs
    - database/tables/MotoRent.TillDenominationCount.sql
  modified:
    - src/MotoRent.Domain/Entities/Entity.cs (JSON polymorphism registration)
    - src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs (repository DI)
    - src/MotoRent.Services/TillService.cs (denomination count methods)

# Decisions
decisions:
  - id: denom-breakdown-dict
    decision: "Use Dictionary<decimal, int> for denominations (key=value, value=count)"
    rationale: "Flexible for any currency denomination structure, easy serialization"
  - id: computed-total
    decision: "Total property computed from denominations sum (key * value)"
    rationale: "Always accurate, no sync issues between denominations and total"
  - id: variance-at-closing
    decision: "ExpectedBalance and Variance only populated for closing counts"
    rationale: "Opening counts have no expected balance; variance only meaningful at close"
  - id: draft-overwrite
    decision: "Drafts can be overwritten; final counts are immutable"
    rationale: "Allow partial counting progress while preserving audit trail"

# Metrics
metrics:
  duration: ~4 minutes
  completed: 2026-01-20
---

# Phase 06 Plan 01: Domain Entity for Denomination Counts Summary

TillDenominationCount entity and data layer for per-denomination cash tracking in till sessions.

## One-liner

Created TillDenominationCount entity with denomination dictionary, SQL table, and TillService CRUD methods for opening/closing cash counts.

## What Was Built

### TillDenominationCount Entity
- `DenominationCountType` enum: Opening, Closing
- `CurrencyDenominationBreakdown` class with:
  - Currency code (THB, USD, EUR, CNY)
  - Denominations dictionary (key=denomination value, value=count)
  - Computed Total from denominations
  - ExpectedBalance and Variance for closing counts
- `TillDenominationCount` entity with:
  - TillSessionId foreign key
  - CountType (Opening/Closing)
  - CountedAt timestamp
  - CountedByUserName
  - CurrencyBreakdowns list
  - TotalInThb (grand total converted)
  - IsFinal draft/final flag

### SQL Table Script
- Standard JSON column pattern with computed columns
- Indexes for session+type, user, and date lookups
- Uses `<schema>` placeholder for multi-tenant support

### TillService Extensions
- `SaveDenominationCountAsync`: Validates session, populates expected balances for closing, converts foreign currency to THB, supports draft overwriting
- `GetDenominationCountAsync`: Retrieves count by session and type, optional draft inclusion
- `GetDenominationCountsAsync`: Gets all counts for a session (history view)
- `GetExpectedBalanceForCurrency`: Helper to get expected balance from session

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 5dd180a | feat | Create TillDenominationCount entity with enum and breakdown class |
| e37c5ab | chore | Create TillDenominationCount SQL table script |
| d5451a4 | feat | Extend TillService with denomination count methods |

## Key Patterns Established

### Denomination Storage Pattern
```csharp
// Denominations stored as Dictionary<decimal, int>
// Key = denomination value (1000, 500, 100...)
// Value = count of that denomination
Denominations = new Dictionary<decimal, int>
{
    [1000] = 5,  // 5 x 1000 = 5000
    [500] = 3,   // 3 x 500 = 1500
    [100] = 10   // 10 x 100 = 1000
};
// Total computed: 7500
```

### Variance Calculation Pattern
```csharp
// Only for closing counts
ExpectedBalance = session.GetCurrencyBalance(currency);
Variance = Total - ExpectedBalance; // Positive = over, Negative = short
```

### Draft vs Final Pattern
```csharp
// Save as draft (can be overwritten)
await tillService.SaveDenominationCountAsync(sessionId, countType, breakdowns,
    username, isFinal: false);

// Save as final (immutable)
await tillService.SaveDenominationCountAsync(sessionId, countType, breakdowns,
    username, isFinal: true);
```

## Deviations from Plan

None - plan executed exactly as written.

## Testing Notes

- Full solution builds successfully
- TillDenominationCount entity registered in JSON polymorphism
- Repository registered in DI container
- Service methods follow existing TillService patterns

## Next Phase Readiness

Ready for Plan 06-02 (Opening Float Dialog):
- Entity and service layer complete
- Can store and retrieve denomination counts
- ExpectedBalance calculation ready for closing counts
- Draft support enables partial counting progress
