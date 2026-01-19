# Roadmap: MotoRent Cashier Till

**Created:** 2026-01-19
**Milestone:** Cashier Till & End of Day Reconciliation
**Depth:** Standard
**Total Phases:** 6
**Total Requirements:** 26

---

## Overview

Transform MotoRent's existing single-currency till system into a full multi-currency cash management solution for Thailand's tourist rental market. The roadmap progresses from foundational exchange rate infrastructure through operational workflows, manager oversight, and end-of-day reconciliation. Each phase delivers a complete, verifiable capability.

---

## Phase 1: Exchange Rate Foundation

**Goal:** Operators can configure and manage exchange rates for foreign currency acceptance.

**Dependencies:** None (foundation phase)

**Plans:** 3 plans

Plans:
- [ ] 01-01-PLAN.md — Entity, service, SQL table foundation
- [ ] 01-02-PLAN.md — Manager settings page for rate management
- [ ] 01-03-PLAN.md — Staff exchange rate panel with calculator

**Requirements:**
- RATE-01: System fetches exchange rates from Forex POS API as default
- RATE-02: Manager can override exchange rate per currency (custom rate takes precedence)
- RATE-03: Exchange rate source (API vs manual) is tracked for audit
- RATE-04: Exchange rate is stored on each transaction for audit trail
- RATE-05: Staff can view current exchange rates during payment acceptance

**Success Criteria:**
1. Manager can view current exchange rates for THB, USD, EUR, CNY on settings page
2. Manager can override an API-fetched rate with a custom rate and see "Manual" source indicator
3. Staff can see today's rates displayed prominently when receiving payment
4. System stores rate source and value on test transaction for later audit retrieval

**Research Flag:** None - standard patterns

---

## Phase 2: Multi-Currency Till Operations

**Goal:** Staff can operate a till with per-currency balance tracking throughout their shift.

**Dependencies:** Phase 1 (exchange rates must exist)

**Requirements:**
- TILL-01: Staff can open till with starting float per currency (THB, USD, EUR, CNY)
- TILL-02: Staff can receive payment in any supported currency with exchange rate applied
- TILL-03: System displays THB change amount when customer pays with foreign currency
- TILL-04: Staff can view current till balance per currency during shift
- TILL-05: Staff can perform cash drop per currency with amount recorded

**Success Criteria:**
1. Staff can open a till by entering float amounts for THB, USD, EUR, CNY separately
2. Staff can accept USD payment for a THB-priced rental and see correct THB change to give
3. Till page shows real-time balance breakdown by currency (not just THB total)
4. Staff can record a cash drop of 10,000 THB to safe and see till balance decrease by that amount

**Research Flag:** None - extends existing TillSession/TillTransaction patterns

---

## Phase 3: Denomination Counting

**Goal:** Staff can count cash by denomination for accurate float verification and closing counts.

**Dependencies:** Phase 2 (till session must support per-currency tracking)

**Requirements:**
- DENOM-01: Staff enters opening float by denomination (bills and coins per currency)
- DENOM-02: Staff enters closing count by denomination (bills and coins per currency)
- DENOM-03: System auto-calculates total from denomination breakdown

**Success Criteria:**
1. Staff can enter THB opening float as: 5x1000 + 10x500 + 20x100 and see calculated total of 12,000 THB
2. Staff can switch between currencies and enter denominations for USD, EUR, CNY
3. At close, staff enters denomination count and system shows calculated total vs expected balance
4. Denomination breakdown is stored and visible in session history

**Research Flag:** None - UI component pattern

---

## Phase 4: Till Closing and Reconciliation

**Goal:** Staff can close their till with per-currency variance tracking for accountability.

**Dependencies:** Phase 2, Phase 3 (till operations and denomination counting)

**Requirements:**
- TILL-06: Staff can close till with counted amount per currency
- TILL-07: System calculates variance (expected vs actual) per currency at close

**Success Criteria:**
1. Staff can close till by entering final counted amounts per currency
2. System displays variance per currency (e.g., THB: -50, USD: +2, EUR: 0)
3. Variance is logged with staff member attribution for accountability
4. Closed session cannot accept new transactions

**Research Flag:** None - extends existing close workflow

---

## Phase 5: Manager Oversight

**Goal:** Managers have visibility into all till sessions and can verify reconciliation.

**Dependencies:** Phase 4 (till sessions must be closable with variance)

**Requirements:**
- MGR-01: Manager can view dashboard of all open and closed till sessions
- MGR-02: Manager can verify closed till sessions (sign-off workflow)
- MGR-03: Manager receives alert when till variance exceeds threshold
- MGR-04: Manager can generate shift handover report

**Success Criteria:**
1. Manager can see list of all open sessions (staff name, start time, current balances) and closed sessions (variance, status)
2. Manager can click "Verify" on a closed session, review details, and mark as verified
3. When staff closes with >100 THB variance, manager sees visual alert on dashboard
4. Manager can generate PDF/print shift handover report showing all transactions and balances

**Research Flag:** May need UX research for dashboard patterns

---

## Phase 6: End of Day Operations

**Goal:** Managers can perform daily close with full audit trail and cash verification.

**Dependencies:** Phase 5 (manager oversight tools must exist)

**Requirements:**
- EOD-01: Manager can verify cash drops from all tills against safe contents
- EOD-02: Manager can perform daily sales close (lock the day)
- EOD-03: Daily close generates summary of all sales, deposits, payouts, and variances
- EOD-04: Closed days cannot have new transactions added
- RCPT-01: System generates receipt for till transactions
- RCPT-02: Staff can search and view past till transactions
- RCPT-03: Staff can reprint receipts

**Success Criteria:**
1. Manager can view aggregate cash drops for the day per currency and enter safe count to verify
2. Manager can perform "Daily Close" action that locks the day after verification
3. Daily summary report shows: total sales by currency, deposit collections, payouts, staff variances
4. Attempting to add a transaction to a closed day shows clear error message
5. Staff can search till transactions by date, type, or amount and reprint any receipt

**Research Flag:** None - reporting patterns exist

---

## Progress

| Phase | Name | Requirements | Status | Completion |
|-------|------|--------------|--------|------------|
| 1 | Exchange Rate Foundation | RATE-01, RATE-02, RATE-03, RATE-04, RATE-05 | Planned | 0% |
| 2 | Multi-Currency Till Operations | TILL-01, TILL-02, TILL-03, TILL-04, TILL-05 | Not Started | 0% |
| 3 | Denomination Counting | DENOM-01, DENOM-02, DENOM-03 | Not Started | 0% |
| 4 | Till Closing and Reconciliation | TILL-06, TILL-07 | Not Started | 0% |
| 5 | Manager Oversight | MGR-01, MGR-02, MGR-03, MGR-04 | Not Started | 0% |
| 6 | End of Day Operations | EOD-01, EOD-02, EOD-03, EOD-04, RCPT-01, RCPT-02, RCPT-03 | Not Started | 0% |

**Overall Progress:** 0/26 requirements complete (0%)

---

## Coverage Validation

| Category | Requirements | Phase | Count |
|----------|--------------|-------|-------|
| RATE | RATE-01, RATE-02, RATE-03, RATE-04, RATE-05 | Phase 1 | 5 |
| TILL (Operations) | TILL-01, TILL-02, TILL-03, TILL-04, TILL-05 | Phase 2 | 5 |
| DENOM | DENOM-01, DENOM-02, DENOM-03 | Phase 3 | 3 |
| TILL (Closing) | TILL-06, TILL-07 | Phase 4 | 2 |
| MGR | MGR-01, MGR-02, MGR-03, MGR-04 | Phase 5 | 4 |
| EOD | EOD-01, EOD-02, EOD-03, EOD-04 | Phase 6 | 4 |
| RCPT | RCPT-01, RCPT-02, RCPT-03 | Phase 6 | 3 |

**Total Mapped:** 26/26
**Orphaned:** 0

---

*Last updated: 2026-01-20 - Phase 1 planned (3 plans)*
