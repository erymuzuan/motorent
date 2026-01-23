# Phase 6: Denomination Counting - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Staff can count cash by denomination for accurate float verification and closing counts. This extends the existing till session with denomination-level granularity for opening float and closing count workflows. The existing `DenominationEntryPanel` from Phase 4 is for payment collection only; this phase creates separate components for open/close workflows.

</domain>

<decisions>
## Implementation Decisions

### Denomination panel reuse
- Create **separate components** for float/closing (not reuse DenominationEntryPanel)
- Opening float: THB required, foreign currencies optional (can add during shift)
- Closing count: THB required only; foreign currencies optional if no transactions
- Layout: **Vertical list** style (single column, larger touch targets for sequential counting)

### Currency switching UX
- **Single scrollable form** (all currencies in one form, scroll between sections)
- For closing: **Hide currencies with zero expected balance** (only show currencies that have balance)
- For opening: **Hidden until added** (only THB visible; "Add currency" button reveals foreign sections)
- **Sticky footer** with running total summary (THB + foreign equivalents) updates as denominations entered

### Float vs closing workflow
- Closing count: **Show expected balance as reference**; staff enters actual count separately
- Variance display: **Inline per currency** (each section shows Expected X, Actual Y, Variance Z)
- Variance reason: **Optional always** (notes field available but not required)
- **Can save draft** and return later to finish (partial progress supported)

### Storage & history
- **Separate entity** (`TillDenominationCount`) linked to TillSession
- History view: **Expandable detail** (summary with click-to-expand showing full breakdown)
- Manager view: **Totals and variances only** (no denomination breakdown in oversight dashboard)
- Draft tracking: **Overwrite until final** (only final count stored; drafts just update in-progress)

### Claude's Discretion
- Exact denomination values per currency (bills/coins)
- Visual styling and spacing of vertical list
- Add Currency button placement and behavior
- Draft save indicator/feedback
- Error states and validation messages

</decisions>

<specifics>
## Specific Ideas

- Vertical list should feel easy for sequential counting (count 1000s, then 500s, then 100s...)
- Sticky footer keeps total visible as staff scrolls through denominations
- "Hide if zero" for closing makes the form focused on what actually needs counting
- Expected vs Actual side-by-side makes variance obvious at a glance

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 06-denomination-counting*
*Context gathered: 2026-01-20*
