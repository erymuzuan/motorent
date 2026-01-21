# Requirements: MotoRent Cashier Till

**Defined:** 2026-01-19
**Core Value:** Business visibility and cash control — owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly.

## v1 Requirements

Requirements for the Cashier Till milestone. Each maps to roadmap phases.

### Multi-Currency Till Operations

- [x] **TILL-01**: Staff can open till with starting float per currency (THB, USD, EUR, CNY)
- [x] **TILL-02**: Staff can receive payment in any supported currency with exchange rate applied
- [x] **TILL-03**: System displays THB change amount when customer pays with foreign currency
- [x] **TILL-04**: Staff can view current till balance per currency during shift
- [x] **TILL-05**: Staff can perform cash drop per currency with amount recorded
- [x] **TILL-06**: Staff can close till with counted amount per currency
- [x] **TILL-07**: System calculates variance (expected vs actual) per currency at close

### Denomination Counting

- [x] **DENOM-01**: Staff enters opening float by denomination (bills and coins per currency)
- [x] **DENOM-02**: Staff enters closing count by denomination (bills and coins per currency)
- [x] **DENOM-03**: System auto-calculates total from denomination breakdown

### Exchange Rate Management

- [x] **RATE-01**: System fetches exchange rates from Forex POS API as default
- [x] **RATE-02**: Manager can override exchange rate per currency (custom rate takes precedence)
- [x] **RATE-03**: Exchange rate source (API vs manual) is tracked for audit
- [x] **RATE-04**: Exchange rate is stored on each transaction for audit trail
- [x] **RATE-05**: Staff can view current exchange rates during payment acceptance

### Transaction Search

- [x] **TXSEARCH-01**: Staff can search for bookings or rentals by reference, customer name, or phone
- [x] **TXSEARCH-02**: System auto-detects transaction type from entity status (booking deposit, check-in, check-out)

### Item Confirmation

- [x] **ITEMS-01**: Staff sees full summary (customer, vehicle, dates, line items) in fullscreen dialog
- [x] **ITEMS-02**: Staff can add/remove accessories from the transaction
- [x] **ITEMS-03**: Staff can change insurance package
- [x] **ITEMS-04**: Staff can apply percentage or fixed discounts with reason
- [x] **ITEMS-05**: Item confirmation uses responsive layout (two columns tablet/PC, stacked mobile)

### Payment Terminal

- [x] **PAY-01**: Payment terminal shows amount due in THB prominently
- [x] **PAY-02**: THB input uses numeric keypad with quick amounts
- [x] **PAY-03**: Foreign currency input uses denomination counting
- [x] **PAY-04**: Staff can mix cash payments across multiple currencies in same transaction
- [x] **PAY-05**: Staff can mix cash + non-cash (card, PromptPay, bank transfer) in same transaction
- [x] **PAY-06**: Running total shows all payment entries with THB equivalents
- [x] **PAY-07**: Change calculation always in THB
- [x] **PAY-08**: Green indicators show which currencies/methods have entries

### Refunds & Corrections

- [x] **REFUND-01**: Security deposit refunds at check-out
- [x] **REFUND-02**: Overpayment refunds when customer pays too much
- [x] **VOID-01**: Transaction reversals require manager approval
- [x] **VOID-02**: Manager can approve void via PIN entry or session authentication
- [x] **VOID-03**: Voided transactions are marked but preserved for audit trail

### Manager Oversight

- [ ] **MGR-01**: Manager can view dashboard of all open and closed till sessions
- [ ] **MGR-02**: Manager can verify closed till sessions (sign-off workflow)
- [ ] **MGR-03**: Manager receives alert when till variance exceeds threshold
- [ ] **MGR-04**: Manager can generate shift handover report

### End of Day (EOD)

- [ ] **EOD-01**: Manager can verify cash drops from all tills against safe contents
- [ ] **EOD-02**: Manager can perform daily sales close (lock the day)
- [ ] **EOD-03**: Daily close generates summary of all sales, deposits, payouts, and variances
- [ ] **EOD-04**: Closed days cannot have new transactions added

### Till Receipts

- [ ] **RCPT-01**: System generates receipt for till transactions
- [ ] **RCPT-02**: Staff can search and view past till transactions
- [ ] **RCPT-03**: Staff can reprint receipts

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Quick Payments

- **QUICK-01**: Quick cash-in for non-rental payments
- **QUICK-02**: Payment categorization for miscellaneous income
- **QUICK-03**: Payment search by amount/date/reference

### Advanced Analytics

- **ANLYT-01**: Variance trend analysis per staff
- **ANLYT-02**: Currency mix reporting
- **ANLYT-03**: Cash flow forecasting

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| External exchange rate APIs | Rates come from internal Forex POS API |
| Cash drawer sensor integration | Hardware dependency, manual count sufficient |
| Full accounting export (QuickBooks/Xero) | Future milestone |
| Alipay/WeChat Pay integration | Future milestone for Chinese tourists |
| Multiple tills per staff | Creates accountability issues |
| Safe inventory tracking | Track till only, safe reconciliation is manual |
| Biometric authentication | Overkill for rental shops, PIN sufficient |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| RATE-01 | Phase 1 | Complete |
| RATE-02 | Phase 1 | Complete |
| RATE-03 | Phase 1 | Complete |
| RATE-04 | Phase 1 | Complete |
| RATE-05 | Phase 1 | Complete |
| TILL-01 | Phase 2 | Complete |
| TILL-02 | Phase 2 | Complete |
| TILL-03 | Phase 2 | Complete |
| TILL-04 | Phase 2 | Complete |
| TILL-05 | Phase 2 | Complete |
| TXSEARCH-01 | Phase 3 | Complete |
| TXSEARCH-02 | Phase 3 | Complete |
| ITEMS-01 | Phase 3 | Complete |
| ITEMS-02 | Phase 3 | Complete |
| ITEMS-03 | Phase 3 | Complete |
| ITEMS-04 | Phase 3 | Complete |
| ITEMS-05 | Phase 3 | Complete |
| PAY-01 | Phase 4 | Complete |
| PAY-02 | Phase 4 | Complete |
| PAY-03 | Phase 4 | Complete |
| PAY-04 | Phase 4 | Complete |
| PAY-05 | Phase 4 | Complete |
| PAY-06 | Phase 4 | Complete |
| PAY-07 | Phase 4 | Complete |
| PAY-08 | Phase 4 | Complete |
| REFUND-01 | Phase 5 | Complete |
| REFUND-02 | Phase 5 | Complete |
| VOID-01 | Phase 5 | Complete |
| VOID-02 | Phase 5 | Complete |
| VOID-03 | Phase 5 | Complete |
| DENOM-01 | Phase 6 | Complete |
| DENOM-02 | Phase 6 | Complete |
| DENOM-03 | Phase 6 | Complete |
| TILL-06 | Phase 7 | Complete |
| TILL-07 | Phase 7 | Complete |
| MGR-01 | Phase 8 | Pending |
| MGR-02 | Phase 8 | Pending |
| MGR-03 | Phase 8 | Pending |
| MGR-04 | Phase 8 | Pending |
| EOD-01 | Phase 9 | Pending |
| EOD-02 | Phase 9 | Pending |
| EOD-03 | Phase 9 | Pending |
| EOD-04 | Phase 9 | Pending |
| RCPT-01 | Phase 9 | Pending |
| RCPT-02 | Phase 9 | Pending |
| RCPT-03 | Phase 9 | Pending |

**Coverage:**
- v1 requirements: 40 total
- Mapped to phases: 40
- Unmapped: 0

---
*Requirements defined: 2026-01-19*
*Last updated: 2026-01-21 — Phase 7 requirements marked complete*
