# Phase 1: Exchange Rate Foundation - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Operators can configure and manage exchange rates for foreign currency acceptance. This includes:
- Fetching rates from Forex POS API as default source
- Manager override capability for custom rates
- Rate source tracking (API vs manual) for audit
- Rate storage on transactions for audit trail
- Staff viewing of current rates during payment acceptance

</domain>

<decisions>
## Implementation Decisions

### Staff Rate Viewing
- Floating button always visible on till screen opens rate panel
- Panel shows on-demand AND automatically during payment acceptance
- Display buy rate only (shop buys foreign currency from tourists)
- Include quick calculator — staff enters amount, sees THB conversion
- Show rates for all supported currencies: THB, USD, EUR, CNY

### Claude's Discretion
- Rate display format (decimal places, layout)
- Override workflow details (immediate vs scheduled)
- Rate source visibility (badges, indicators)
- Manager settings page layout
- API fetch frequency and caching

</decisions>

<specifics>
## Specific Ideas

- "Buy rate only since we're in Thailand, and tourists come with foreign currency — we're going to buy that at that rate"
- Rate panel should be quick to access during customer interactions

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-exchange-rate-foundation*
*Context gathered: 2026-01-20*
