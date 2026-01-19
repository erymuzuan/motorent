# Requirements: MotoRent Cashier Till

**Defined:** 2026-01-19
**Core Value:** Business visibility and cash control — owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly.

## v1 Requirements

Requirements for the Cashier Till milestone. Each maps to roadmap phases.

### Multi-Currency Till Operations

- [ ] **TILL-01**: Staff can open till with starting float per currency (THB, USD, EUR, CNY)
- [ ] **TILL-02**: Staff can receive payment in any supported currency with exchange rate applied
- [ ] **TILL-03**: System displays THB change amount when customer pays with foreign currency
- [ ] **TILL-04**: Staff can view current till balance per currency during shift
- [ ] **TILL-05**: Staff can perform cash drop per currency with amount recorded
- [ ] **TILL-06**: Staff can close till with counted amount per currency
- [ ] **TILL-07**: System calculates variance (expected vs actual) per currency at close

### Denomination Counting

- [ ] **DENOM-01**: Staff enters opening float by denomination (bills and coins per currency)
- [ ] **DENOM-02**: Staff enters closing count by denomination (bills and coins per currency)
- [ ] **DENOM-03**: System auto-calculates total from denomination breakdown

### Exchange Rate Management

- [ ] **RATE-01**: System fetches exchange rates from Forex POS API as default
- [ ] **RATE-02**: Manager can override exchange rate per currency (custom rate takes precedence)
- [ ] **RATE-03**: Exchange rate source (API vs manual) is tracked for audit
- [ ] **RATE-04**: Exchange rate is stored on each transaction for audit trail
- [ ] **RATE-05**: Staff can view current exchange rates during payment acceptance

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
| RATE-01 | Phase 1 | Pending |
| RATE-02 | Phase 1 | Pending |
| RATE-03 | Phase 1 | Pending |
| RATE-04 | Phase 1 | Pending |
| RATE-05 | Phase 1 | Pending |
| TILL-01 | Phase 2 | Pending |
| TILL-02 | Phase 2 | Pending |
| TILL-03 | Phase 2 | Pending |
| TILL-04 | Phase 2 | Pending |
| TILL-05 | Phase 2 | Pending |
| DENOM-01 | Phase 3 | Pending |
| DENOM-02 | Phase 3 | Pending |
| DENOM-03 | Phase 3 | Pending |
| TILL-06 | Phase 4 | Pending |
| TILL-07 | Phase 4 | Pending |
| MGR-01 | Phase 5 | Pending |
| MGR-02 | Phase 5 | Pending |
| MGR-03 | Phase 5 | Pending |
| MGR-04 | Phase 5 | Pending |
| EOD-01 | Phase 6 | Pending |
| EOD-02 | Phase 6 | Pending |
| EOD-03 | Phase 6 | Pending |
| EOD-04 | Phase 6 | Pending |
| RCPT-01 | Phase 6 | Pending |
| RCPT-02 | Phase 6 | Pending |
| RCPT-03 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 26 total
- Mapped to phases: 26
- Unmapped: 0

---
*Requirements defined: 2026-01-19*
*Last updated: 2026-01-19 after roadmap creation*
