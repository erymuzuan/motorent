# Phase 2: Multi-Currency Till Operations - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Staff can operate a till with per-currency balance tracking throughout their shift. Covers till opening, receiving payments in foreign currency, viewing balances, and performing cash drops. Till closing is Phase 4.

</domain>

<decisions>
## Implementation Decisions

### Opening Workflow
- Opening float is always in THB only
- Foreign currency balances start at zero
- Staff enters THB float amount (denomination counting is Phase 3)

### Payment Acceptance
- Currency selection via row of buttons (THB, USD, EUR, CNY) — tap to select before entering
- Amount entry by denomination: staff enters how many of each bill/coin received
- Change display shows full breakdown: amount received, rate used, THB equivalent, then change amount
- ExchangeRatePanel FAB remains separate — staff can open if needed, not embedded in payment flow

### Balance Display
- Collapsible summary: collapsed by default showing total THB equivalent
- Expand to see per-currency breakdown
- Hide currencies with zero balance — only show those with actual amounts
- Show THB equivalent of all foreign currency converted at current rates
- Real-time updates after each transaction (no manual refresh needed)

### Cash Drop Flow
- Any currency can be dropped (THB, USD, EUR, CNY)
- Single drop can contain multiple currencies
- One screen shows denomination fields for all currencies
- Enter drop by denomination count (matches opening/closing consistency)
- Optional note field (not required)
- No physical receipt printed — digital record only

### Claude's Discretion
- Exact layout of denomination entry fields
- Animation/transition for collapsible balance
- Loading states during balance calculation
- Validation error presentation

</decisions>

<specifics>
## Specific Ideas

- Denomination entry should feel consistent across opening, drops, and closing (Phase 3-4)
- Balance panel should not take up too much screen space when collapsed
- Currency buttons should be easily tappable on tablet (mobile-first)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-multi-currency-till-operations*
*Context gathered: 2026-01-20*
