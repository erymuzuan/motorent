# Phase 7: Till Closing and Reconciliation - Context

**Gathered:** 2026-01-21
**Status:** Complete

<domain>
## Phase Boundary

Staff can close their till session with per-currency variance tracking. System calculates expected vs actual variance per currency. Closed sessions are logged for accountability. Manager oversight features are Phase 8.

</domain>

<decisions>
## Implementation Decisions

### Variance handling
- Always show all variances regardless of size (no auto-accept threshold)
- Staff can close with variance without manager approval — logged for later review
- Display variance per-currency breakdown (THB: -50, USD: +2, EUR: 0)
- Optional note field when closing with variance (not required)

### Close workflow
- Summary review then confirm: show expected, counted, variance per currency with single Confirm button
- Auto-save progress: system saves as staff enters, can resume if interrupted
- Manager can force close: manager PIN allows closing without full denomination count
- Warning then allow: pending transactions trigger warning but don't block close

### Accountability logging
- Log session close event with staff ID, timestamp, all expected/actual/variance per currency
- Extend TillSession entity with close metadata (ClosedAt, ClosedByUserId, VarianceNote)
- Keeps data with session entity, not separate table
- Variance note is optional free text captured at close time

### Session state transitions
- States: Open → Closed (one-way transition)
- Closed sessions cannot accept new transactions (immutable)
- Staff can view their closed sessions (read-only)
- Reopening not allowed without manager action (deferred to Phase 8)

### Claude's Discretion
- Use existing TillCloseSessionDialog as the close workflow container
- ClosingCountPanel already captures denomination counts (Phase 6)
- Add summary step showing all currencies before confirm
- Wire up TillService.CloseSessionAsync with variance persistence

</decisions>

<specifics>
## Specific Ideas

- Summary view shows table: Currency | Expected | Counted | Variance with color coding
- Green = exact match, Red = short, Blue = over
- Grand total variance in THB at bottom (converted at current rates)
- Confirm button disabled until all currencies counted OR manager force-close used

</specifics>

<deferred>
## Deferred Ideas

- Session reopening workflow (Phase 8 - manager oversight)
- Variance threshold alerts (Phase 8 - MGR-03)
- Shift handover report (Phase 8 - MGR-04)

</deferred>

---

*Phase: 07-till-closing-reconciliation*
*Context gathered: 2026-01-21*
