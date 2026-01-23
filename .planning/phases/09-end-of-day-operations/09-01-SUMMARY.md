---
phase: 09-end-of-day-operations
plan: 01
subsystem: till-eod
tags: [domain, entity, service, sql, daily-close, shortage-log]

dependency-graph:
  requires: [07-01, 08-01]
  provides: [eod-entities, eod-service-methods, shortage-logging]
  affects: [09-02, 09-03, 09-04]

tech-stack:
  added: []
  patterns: [partial-class-service, entity-inheritance, json-computed-columns]

file-tracking:
  key-files:
    created:
      - src/MotoRent.Domain/Entities/DailyClose.cs
      - src/MotoRent.Domain/Entities/ShortageLog.cs
      - database/tables/MotoRent.DailyClose.sql
      - database/tables/MotoRent.ShortageLog.sql
      - src/MotoRent.Services/TillService.eod.cs
    modified:
      - src/MotoRent.Domain/Entities/Entity.cs
      - src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs

decisions:
  - id: date-only-daily-close
    choice: DateTime with Date property (time ignored)
    rationale: One DailyClose per shop per calendar day
  - id: shortage-always-positive
    choice: Store shortage amount as Math.Abs(amount)
    rationale: Consistent representation regardless of variance direction
  - id: repository-in-domain-extensions
    choice: Repository<T> in ServiceCollectionExtensions.cs
    rationale: Follows existing pattern for tenant-specific entities

metrics:
  duration: ~5 minutes
  completed: 2026-01-21
---

# Phase 9 Plan 01: Domain Entities & Service Methods Summary

**One-liner:** DailyClose and ShortageLog entities with TillService EOD methods for daily close workflow, shortage logging, and cash drop verification.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Create DailyClose and ShortageLog entities | bbf92d2 | DailyClose.cs, ShortageLog.cs, Entity.cs |
| 2 | Create SQL table scripts | d8a866b | MotoRent.DailyClose.sql, MotoRent.ShortageLog.sql |
| 3 | Create TillService.eod.cs with EOD methods | d1c5900 | TillService.eod.cs, ServiceCollectionExtensions.cs |

## Key Deliverables

### DailyClose Entity (97 lines)
- `DailyCloseId`, `ShopId`, `Date` for identification
- `Status` enum: Open, Closed, Reconciled
- Denormalized summary totals: TotalCashIn, TotalCashOut, TotalDropped, TotalVariance, TotalElectronicPayments
- Session tracking: SessionCount, SessionsWithVariance
- Reopen tracking: WasReopened, ReopenReason, ReopenedAt, ReopenedByUserName
- Unique constraint on ShopId + Date (one record per day per shop)

### ShortageLog Entity (64 lines)
- Links to TillSession and optional DailyClose
- Staff attribution: StaffUserName, StaffDisplayName
- Amount tracking: Currency, Amount, AmountInThb
- Manager logging: LoggedByUserName, LoggedAt, Reason
- Amount always stored as positive value

### SQL Tables
- `MotoRent.DailyClose.sql` with computed columns and unique index on ShopId+Date
- `MotoRent.ShortageLog.sql` with indexes for staff accountability queries

### TillService.eod.cs (290 lines)
- **Daily Close Operations:**
  - `GetOrCreateDailyCloseAsync(shopId, date)` - Lazy create daily close records
  - `GetDailyCloseAsync(shopId, date)` - Query existing daily close
  - `PerformDailyCloseAsync(shopId, date, manager)` - Execute daily close with summary capture
  - `IsDayClosedAsync(shopId, date)` - Check if day is blocked
  - `ReopenDayAsync(shopId, date, reason, manager)` - Reopen with reason tracking

- **Shortage Logging:**
  - `LogShortageAsync(...)` - Record variance with THB conversion
  - `GetShortageLogsAsync(shopId, fromDate?, toDate?)` - Query with date filtering
  - `GetShortageLogsByStaffAsync(shopId, staffUserName)` - Staff accountability queries

- **Cash Drop Verification:**
  - `GetDropTotalsByCurrencyAsync(sessionId)` - Currency-grouped drop totals
  - `GetDropTransactionsAsync(sessionId)` - Individual drop transactions

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Repository registration location | ServiceCollectionExtensions.cs in MotoRent.Domain | Follows existing pattern for tenant-specific entities |
| Date storage | DateTime with .Date property | One DailyClose per calendar day, time component ignored |
| Shortage amount sign | Always positive (Math.Abs) | Consistent representation regardless of variance direction |

## Verification Results

1. All files compile without errors: `dotnet build src/MotoRent.Services/MotoRent.Services.csproj` - SUCCESS
2. Entities follow existing patterns (TillSession.cs) - VERIFIED
3. SQL files use JSON computed columns pattern - VERIFIED
4. TillService.eod.cs is a proper partial class - VERIFIED
5. Repository registrations added - VERIFIED

## Next Phase Readiness

**Ready for 09-02:** Daily close UI and workflow
- DailyClose entity provides state tracking
- PerformDailyCloseAsync captures summary totals
- IsDayClosedAsync enables closed day blocking
- ReopenDayAsync provides override capability

**Ready for 09-03:** Shortage log display
- ShortageLog entity ready for UI queries
- GetShortageLogsAsync with date filtering
- GetShortageLogsByStaffAsync for staff accountability

**Ready for 09-04:** Cash drop verification
- GetDropTotalsByCurrencyAsync for verification display
- GetDropTransactionsAsync for individual drop review

---

*Plan completed: 2026-01-21*
*Duration: ~5 minutes*
