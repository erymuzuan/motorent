# Roadmap: MotoRent Cashier Till

**Created:** 2026-01-19
**Milestone:** Cashier Till & End of Day Reconciliation
**Depth:** Standard
**Total Phases:** 9
**Total Requirements:** 40

---

## Overview

Transform MotoRent's existing single-currency till system into a full multi-currency cash management solution for Thailand's tourist rental market. The roadmap progresses from foundational exchange rate infrastructure through operational workflows, manager oversight, and end-of-day reconciliation. Each phase delivers a complete, verifiable capability.

**Till Workflow Vision:** Staff use a unified transaction flow where they search for a booking/rental, review and edit items in a fullscreen confirmation screen, then process multi-currency split payments through a dedicated payment terminal.

---

## Phase 1: Exchange Rate Foundation

**Goal:** Operators can configure and manage exchange rates for foreign currency acceptance.

**Dependencies:** None (foundation phase)

**Plans:** 4 plans

Plans:
- [x] 01-01-PLAN.md — Entity, service, SQL table foundation
- [x] 01-02-PLAN.md — Manager settings page for rate management
- [x] 01-03-PLAN.md — Staff exchange rate panel with calculator
- [x] 01-04-PLAN.md — Gap closure: Wire panel to Till, add nav link

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

**Plans:** 3 plans

Plans:
- [x] 02-01-PLAN.md — Entity extensions and TillService multi-currency methods
- [x] 02-02-PLAN.md — Payment dialog with currency selection and denomination entry
- [x] 02-03-PLAN.md — Currency balance panel and multi-currency cash drop

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

## Phase 3: Transaction Search & Item Confirmation

**Goal:** Staff can start a transaction by searching for a booking/rental and reviewing/editing items before payment.

**Dependencies:** Phase 2 (till session must be open for transaction entry)

**Plans:** 2 plans

Plans:
- [x] 03-01-PLAN.md — Transaction search dialog with booking/rental search and auto-detect
- [x] 03-02-PLAN.md — Fullscreen item confirmation with accessory/insurance/discount editing

**Requirements:**
- TXSEARCH-01: Staff can search for bookings or rentals by reference, customer name, or phone
- TXSEARCH-02: System auto-detects transaction type from entity status (booking deposit, check-in, check-out)
- ITEMS-01: Staff sees full summary (customer, vehicle, dates, line items) in fullscreen dialog
- ITEMS-02: Staff can add/remove accessories from the transaction
- ITEMS-03: Staff can change insurance package
- ITEMS-04: Staff can apply percentage or fixed discounts with reason
- ITEMS-05: Item confirmation uses responsive layout (two columns tablet/PC, stacked mobile)

**Success Criteria:**
1. Staff clicks "New Transaction", searches "John", finds his Reserved booking
2. System shows check-in items (rental charges, security deposit, optional accessories)
3. Staff adds a helmet (฿50/day x 3 days = ฿150) to the line items
4. Staff applies 10% discount with reason "Returning customer"
5. Total updates and staff can proceed to payment

**Research Flag:** May need UX research for search result presentation

---

## Phase 4: Payment Terminal Redesign

**Goal:** Staff can receive multi-currency split payments through a unified payment terminal.

**Dependencies:** Phase 3 (item confirmation must provide total amount due)

**Plans:** 3 plans

Plans:
- [x] 04-01-PLAN.md — Payment terminal panel UI foundation with layout and tabs
- [x] 04-02-PLAN.md — THB keypad and foreign currency denomination input
- [x] 04-03-PLAN.md — Payment completion flow with till recording

**Requirements:**
- PAY-01: Payment terminal shows amount due in THB prominently
- PAY-02: THB input uses numeric keypad with quick amounts (฿100, ฿500, ฿1,000, exact amount)
- PAY-03: Foreign currency input (USD, EUR, CNY) uses denomination counting
- PAY-04: Staff can mix cash payments across multiple currencies in same transaction
- PAY-05: Staff can mix cash + non-cash (card, PromptPay, bank transfer) in same transaction
- PAY-06: Running total shows all payment entries with THB equivalents
- PAY-07: Change calculation always in THB
- PAY-08: Green indicators show which currencies/methods have entries

**Success Criteria:**
1. Customer owes ฿4,550 for rental
2. Staff accepts: ฿1,000 THB cash + $50 USD (rate 35.5 = ฿1,775) + €50 EUR (rate 38.0 = ฿1,900)
3. System shows: Total received ฿4,675, Change ฿125 (THB)
4. Staff completes payment, receipt is generated

**Research Flag:** None - builds on existing denomination counting components

---

## Phase 5: Refunds & Corrections

**Goal:** Staff can process refunds and void transactions with manager PIN approval.

**Dependencies:** Phase 4 (payment terminal must support receiving payments first)

**Plans:** 5 plans

Plans:
- [x] 05-01-PLAN.md — Domain entity extensions (TillTransaction void fields, User PIN fields)
- [x] 05-02-PLAN.md — ManagerPinService and TillService void/refund methods
- [x] 05-03-PLAN.md — ManagerPinDialog component for PIN entry
- [x] 05-04-PLAN.md — VoidTransactionDialog for void initiation
- [x] 05-05-PLAN.md — Till.razor void workflow integration

**Requirements:**
- REFUND-01: Security deposit refunds at check-out (existing, enhance if needed)
- REFUND-02: Overpayment refunds when customer pays too much
- VOID-01: Transaction reversals require manager approval
- VOID-02: Manager can approve void via PIN entry or session authentication
- VOID-03: Voided transactions are marked but preserved for audit trail

**Success Criteria:**
1. Staff realizes they recorded wrong payment, clicks "Void"
2. System prompts for manager approval
3. Manager enters PIN or authenticates
4. Transaction is marked void, compensating entry created
5. Till balance reflects the correction

**Research Flag:** None - research completed in 05-RESEARCH.md

---

## Phase 6: Denomination Counting

**Goal:** Staff can count cash by denomination for accurate float verification and closing counts.

**Dependencies:** Phase 4 (payment terminal already uses denomination counting; this phase adds opening/closing counts)

**Plans:** 3 plans

Plans:
- [x] 06-01-PLAN.md — Domain entity and TillService extension for denomination counts
- [x] 06-02-PLAN.md — Opening float denomination panel and dialog integration
- [x] 06-03-PLAN.md — Closing count denomination panel with variance display

**Requirements:**
- DENOM-01: Staff enters opening float by denomination (bills and coins per currency)
- DENOM-02: Staff enters closing count by denomination (bills and coins per currency)
- DENOM-03: System auto-calculates total from denomination breakdown

**Success Criteria:**
1. Staff can enter THB opening float as: 5x1000 + 10x500 + 20x100 and see calculated total of 12,000 THB
2. Staff can switch between currencies and enter denominations for USD, EUR, CNY
3. At close, staff enters denomination count and system shows calculated total vs expected balance
4. Denomination breakdown is stored and visible in session history

**Research Flag:** None - UI component pattern exists from Phase 4

---

## Phase 7: Till Closing and Reconciliation

**Goal:** Staff can close their till with per-currency variance tracking for accountability.

**Dependencies:** Phase 6 (denomination counting for accurate close)

**Plans:** 2 plans

Plans:
- [x] 07-01-PLAN.md — Domain layer: TillSession close metadata, TillService close methods
- [x] 07-02-PLAN.md — UI layer: Summary review step, localization

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

## Phase 8: Manager Oversight

**Goal:** Managers have visibility into all till sessions and can verify reconciliation.

**Dependencies:** Phase 7 (till sessions must be closable with variance)

**Plans:** 3 plans

Plans:
- [x] 08-01-PLAN.md — TillService manager extensions (variance threshold, query methods)
- [x] 08-02-PLAN.md — Manager dashboard UI (active sessions, closed sessions table)
- [x] 08-03-PLAN.md — Shift handover report (sales clearing journal format)

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

**Research Flag:** None - research completed in 08-RESEARCH.md

---

## Phase 9: End of Day Operations

**Goal:** Managers can perform daily close with full audit trail and cash verification.

**Dependencies:** Phase 8 (manager oversight tools must exist)

**Plans:** 4 plans

Plans:
- [x] 09-01-PLAN.md — Domain entities (DailyClose, ShortageLog) and EOD service methods
- [x] 09-02-PLAN.md — Cash drop verification dialog and EOD page integration
- [x] 09-03-PLAN.md — Daily close page with shortage logging and summary report
- [x] 09-04-PLAN.md — Staff receipt search and reprint page

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
| 1 | Exchange Rate Foundation | RATE-01 to RATE-05 | Complete | 100% |
| 2 | Multi-Currency Till Operations | TILL-01 to TILL-05 | Complete | 100% |
| 3 | Transaction Search & Item Confirmation | TXSEARCH-01, TXSEARCH-02, ITEMS-01 to ITEMS-05 | Complete | 100% |
| 4 | Payment Terminal Redesign | PAY-01 to PAY-08 | Complete | 100% |
| 5 | Refunds & Corrections | REFUND-01, REFUND-02, VOID-01 to VOID-03 | Complete | 100% |
| 6 | Denomination Counting | DENOM-01 to DENOM-03 | Complete | 100% |
| 7 | Till Closing and Reconciliation | TILL-06, TILL-07 | Complete | 100% |
| 8 | Manager Oversight | MGR-01 to MGR-04 | Complete | 100% |
| 9 | End of Day Operations | EOD-01 to EOD-04, RCPT-01 to RCPT-03 | Complete | 100% |

**Overall Progress:** 40/40 requirements complete (100%)

---

## Coverage Validation

| Category | Requirements | Phase | Count |
|----------|--------------|-------|-------|
| RATE | RATE-01, RATE-02, RATE-03, RATE-04, RATE-05 | Phase 1 | 5 |
| TILL (Operations) | TILL-01, TILL-02, TILL-03, TILL-04, TILL-05 | Phase 2 | 5 |
| TXSEARCH | TXSEARCH-01, TXSEARCH-02 | Phase 3 | 2 |
| ITEMS | ITEMS-01, ITEMS-02, ITEMS-03, ITEMS-04, ITEMS-05 | Phase 3 | 5 |
| PAY | PAY-01 to PAY-08 | Phase 4 | 8 |
| REFUND | REFUND-01, REFUND-02 | Phase 5 | 2 |
| VOID | VOID-01, VOID-02, VOID-03 | Phase 5 | 3 |
| DENOM | DENOM-01, DENOM-02, DENOM-03 | Phase 6 | 3 |
| TILL (Closing) | TILL-06, TILL-07 | Phase 7 | 2 |
| MGR | MGR-01, MGR-02, MGR-03, MGR-04 | Phase 8 | 4 |
| EOD | EOD-01, EOD-02, EOD-03, EOD-04 | Phase 9 | 4 |
| RCPT | RCPT-01, RCPT-02, RCPT-03 | Phase 9 | 3 |

**Total Mapped:** 40/40
**Orphaned:** 0

---

## Deferred Ideas (Future TODO)

- Receipt designer (A4 layout customization)
- Walk-in sales mode (general POS without booking/rental)
- Configurable currencies per organization
- GBP and JPY currency support

---

*Last updated: 2026-01-21 — Phase 9 complete (End of Day Operations) — Milestone complete*
