# Phase 9: End of Day Operations - Context

**Gathered:** 2026-01-21
**Status:** Ready for planning

<domain>
## Phase Boundary

End of day operations for managers — verify cash drops against safe contents, perform daily close to lock the day, and generate daily summary reports. Staff can search and reprint transaction receipts. Receipt generation for till transactions is included.

</domain>

<decisions>
## Implementation Decisions

### Daily Close Workflow
- Any Manager (OrgAdmin or ShopManager) can perform daily close
- Unresolved variances: Manager can either suspend the session OR post variance as shortage to a log
- Shortage log records: Amount + Staff + Reason (manager-provided explanation)
- Closed days: Soft block with override — blocked by default, manager can unlock with reason
- Shortage log becomes viewable as a staff shortages table

### Cash Drop Verification
- Verification is per-till-session (not per-staff aggregate)
- If staff has 2 tills in a day, each session's drops verified separately
- Each session shows drops by currency (THB, USD, EUR, CNY) as originally dropped
- Safe mismatch: Log variance and continue (no hard block)
- Manager confirms drops are correct, OR enters actual count if different and provides reason

### Daily Summary Report
- Till-focused structure: Start with each till session, then aggregate
- Each session shows breakdown by payment method (cash/card/PromptPay/etc.)
- Day totals include grand totals plus per-payment-method aggregates
- Export via browser print only (like handover report)

### Receipt Search & Reprint
- Full search: Date range, transaction type, customer name/phone, amount range, receipt number
- Any staff can search and reprint receipts
- No audit trail for reprints
- Receipt format: A4 invoice style with company header and itemized details

### Claude's Discretion
- Daily close UI layout and flow
- Shortage log table design and filtering
- Receipt layout and styling details
- Search results pagination and display

</decisions>

<specifics>
## Specific Ideas

- Shortage log should function as an accountability table showing staff variance history
- Till session verification should show drops as they were recorded (currency-specific)
- Daily summary follows same browser print pattern as handover report (Phase 8)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 09-end-of-day-operations*
*Context gathered: 2026-01-21*
