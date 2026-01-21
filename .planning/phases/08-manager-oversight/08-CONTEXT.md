# Phase 8: Manager Oversight - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Managers have visibility into all till sessions and can verify reconciliation. This phase delivers:
- Dashboard showing all open and closed till sessions
- Sign-off workflow for closed sessions
- Variance threshold alerts
- Shift handover report in sales clearing journal format

Does NOT include: Daily close (Phase 9), cross-shop aggregation, or safe verification.

</domain>

<decisions>
## Implementation Decisions

### Dashboard Layout
- Dedicated page at `/manager/dashboard` (not a tab on Till page)
- Two-section layout: "Active Sessions" cards at top, "Recent Closed" table below
- Active session cards show: staff photo, name, start time, per-currency balances, last activity, variance indicator (rich display)
- Closed sessions table columns: Staff, Date, Duration, Total Variance, Status (Pending/Verified)

### Variance Alerts
- Badge + inline approach: Bell icon with count badge + red highlight on affected session rows/cards
- Count badge shows number of sessions with variance issues (not just a dot)
- Threshold configured in Organization Settings ("Alert when variance exceeds ___")
- Single THB-equivalent threshold (all currency variances converted to THB for comparison)

### Handover Report
- Sales clearing journal format (like the reference image)
- On-screen preview + "Download PDF" button
- **Credit column (inflows):** Payment methods only
  - Cash THB, Cash USD, Cash EUR, Cash CNY
  - Card
  - PromptPay
  - AliPay
- **Debit column (outflows):** All outflows
  - Cash shortages by currency
  - Customer refunds
  - Cash drops to safe
  - Expenses

### Verification Workflow
- Simple sign-off: Manager clicks Verify, confirms review — done (no notes required)
- Role-based authorization only (no PIN required for verification)
- No self-verification: Staff cannot verify sessions they operated
- High variance sessions verify same as normal — manager's judgment, no special handling

### Claude's Discretion
- Exact card layout and responsive breakpoints
- Filter/sort options for closed sessions table
- PDF report styling and layout
- Bell icon placement (header vs sidebar)
- How "last activity" is determined and displayed

</decisions>

<specifics>
## Specific Ideas

- Sales clearing journal reference: Two-column ledger with icons, Description on left, Credit/Debit amounts on right (see sc.png in project root)
- Pattern: Clean journal-style rows with icon + description + amount in appropriate column
- For MotoRent: Credits = payment collections by method, Debits = shortages/refunds/drops/expenses

</specifics>

<deferred>
## Deferred Ideas

- End of day operations / daily close — Phase 9
- Collection of all tills shop-by-shop — Phase 9 (EOD-01: verify cash drops from all tills against safe)
- Cross-shop aggregation — Phase 9 (daily summary across all locations)
- Safe contents verification — Phase 9 (EOD-01)

</deferred>

---

*Phase: 08-manager-oversight*
*Context gathered: 2026-01-21*
