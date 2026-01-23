# Feature Landscape: Cashier Till / POS System

**Domain:** Vehicle Rental POS with Multi-Currency Cash Management
**Context:** MotoRent SaaS for Thailand tourist areas (Phuket, Krabi, etc.)
**Target Users:** Desk staff, Shop managers, Business owners
**Researched:** 2026-01-19
**Confidence:** MEDIUM-HIGH (based on industry research and existing codebase analysis)

---

## Executive Summary

Vehicle rental in Thailand tourist areas has unique characteristics:
1. **Cash-heavy operations** - Thailand is still predominantly cash-based (62% of in-store payments)
2. **Multi-currency acceptance** - Tourists pay in USD, EUR, CNY, etc. with change given in THB
3. **Deposit-intensive** - Security deposits are critical (1,000-4,000 THB typical)
4. **Shift-based accountability** - Staff must reconcile their own till at end of shift

The existing codebase already implements core till session management, transaction recording, and basic reconciliation. This research identifies gaps and priorities for completing the feature set.

---

## Table Stakes

Features users expect. Missing = product feels incomplete.

### Session Management

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Open Till Session | Staff must declare opening float before processing transactions | Low | IMPLEMENTED |
| Close Till Session | Staff counts cash and declares closing amount | Low | IMPLEMENTED |
| Single Assignment | One staff per till session for accountability | Low | IMPLEMENTED |
| Session History | View past sessions by date, staff, status | Low | IMPLEMENTED |

### Cash-In Operations

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Receive Rental Payment | Core business operation - collect payment for rental | Low | IMPLEMENTED |
| Collect Security Deposit | Every rental requires deposit collection | Low | IMPLEMENTED |
| Collect Booking Deposit | Advance payments for reservations | Low | IMPLEMENTED |
| Record Damage Charges | Collect payment for vehicle damage | Low | IMPLEMENTED |
| Record Late Fees | Collect payment for late returns | Low | IMPLEMENTED |
| Record Surcharges | Fuel, delivery, accessories | Low | IMPLEMENTED |
| Cash Top-Up | Add change from safe when till runs low | Low | IMPLEMENTED |

### Cash-Out Operations

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Refund Security Deposit | Return deposit at checkout | Low | IMPLEMENTED |
| Cash Drop | Move excess cash to safe for security | Low | IMPLEMENTED |
| Petty Cash Payout | Small expenses (office supplies, tips, etc.) | Low | IMPLEMENTED |
| Agent Commission | Pay booking agents their commission | Low | IMPLEMENTED |

### Non-Cash Payment Tracking

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Card Payment Recording | Track card payments for reconciliation | Low | IMPLEMENTED |
| PromptPay Recording | Thailand's national QR payment system | Low | IMPLEMENTED |
| Bank Transfer Recording | Track wire transfers | Low | IMPLEMENTED |

### Reconciliation

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Expected vs Actual Cash | Calculate variance at close | Low | IMPLEMENTED |
| Variance Recording | Track over/short amounts | Low | IMPLEMENTED |
| Session Verification | Manager sign-off on closed sessions | Low | IMPLEMENTED |
| Daily Summary | Aggregate all sessions for a day | Low | IMPLEMENTED |

### Receipt Generation

| Feature | Why Expected | Complexity | Current Status |
|---------|--------------|------------|----------------|
| Check-In Receipt | Document showing rental terms, deposit collected | Medium | IMPLEMENTED |
| Settlement Receipt | Document showing deposit refund/deductions | Medium | IMPLEMENTED |
| Receipt Reprint | Reprint receipts when needed | Low | PARTIAL |
| Receipt Voiding | Void incorrect receipts with reason | Low | PARTIAL |

---

## Table Stakes - GAPS TO ADDRESS

Features expected but not yet implemented.

### Multi-Currency Cash Handling

| Feature | Why Expected | Complexity | Priority |
|---------|--------------|------------|----------|
| Multi-Currency Float Tracking | Track opening/closing by currency (THB, USD, EUR, etc.) | Medium | HIGH |
| Currency-Specific Cash Count | Count and declare amounts per currency at close | Medium | HIGH |
| Exchange Rate Configuration | Operator sets their buy/sell rates per currency | Medium | HIGH |
| Change Calculation in THB | Accept foreign currency, calculate change in THB | Medium | HIGH |
| Multi-Currency Variance | Track over/short per currency | Medium | HIGH |
| Foreign Currency Running Total | Display current foreign currency amounts in till | Medium | HIGH |

**Rationale:** The existing codebase tracks payments in multiple currencies (ReceiptPayment has Currency/ExchangeRate) but TillSession only tracks aggregate THB amounts. Real-world operators need to count and reconcile each currency separately.

### Denomination Counting

| Feature | Why Expected | Complexity | Priority |
|---------|--------------|------------|----------|
| Denomination-Based Counting | Count by bill/coin denomination at close | Medium | MEDIUM |
| Quick Count Templates | Pre-defined denomination breakdown forms | Low | LOW |
| Auto-Calculate Total | Sum denominations to get total | Low | MEDIUM |

**Rationale:** Professional till reconciliation always involves counting by denomination (e.g., "10 x 1000THB + 5 x 500THB + ..."). This prevents counting errors and creates better audit trail. However, basic amount entry works for MVP.

### Quick Payment Reception

| Feature | Why Expected | Complexity | Priority |
|---------|--------------|------------|----------|
| Quick Cash-In (Non-Rental) | Receive payment not tied to specific rental | Low | MEDIUM |
| Payment Search | Find past payments by amount, date, reference | Low | MEDIUM |
| Payment Categorization | Categorize miscellaneous income | Low | LOW |

### Audit & Security

| Feature | Why Expected | Complexity | Priority |
|---------|--------------|------------|----------|
| Payout Attachment Upload | Photo of receipt for petty cash payouts | Low | IMPLEMENTED |
| Manager Override | Manager can override restrictions | Low | MEDIUM |
| Spot Check Support | Mid-shift till count without closing | Medium | LOW |
| Blind Close Option | Enter counted amount without seeing expected | Low | LOW |

---

## Differentiators

Features that set the product apart. Not expected, but valued.

### Multi-Currency Excellence

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Real-Time Rate Display | Show current exchange rates at point of payment | Low | Builds trust with tourists |
| Rate Margin Configuration | Set buy vs sell rate spread | Low | Operators profit from exchange |
| Multi-Currency Receipt | Print receipt showing original currency and THB equivalent | Medium | Professional documentation |
| Cross-Border Payment QR | Accept Alipay/WeChat Pay via PromptPay bridge | High | Chinese tourist market |

### Advanced Reconciliation

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Photo-Based Cash Count | Take photo of cash during count for audit | Medium | Dispute resolution evidence |
| Variance Trend Analysis | Track which staff have repeated variances | Medium | Loss prevention |
| Auto-Variance Alerts | Notify manager when variance exceeds threshold | Low | Proactive management |
| Scheduled Spot Checks | Random mid-shift count prompts | Medium | Theft deterrence |

### Operational Efficiency

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Till Dashboard Widget | Show current till status in main dashboard | Low | Staff awareness |
| Quick Actions Panel | Common operations in one click | Low | Speed at checkout |
| Shift Handover Report | Summary document for shift change | Low | Communication tool |
| Safe Reconciliation | Track safe balance alongside tills | Medium | Complete cash picture |

### Reporting & Analytics

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Cash Flow Report | Daily/weekly/monthly cash movement | Medium | Owner visibility |
| Payment Method Breakdown | % cash vs card vs PromptPay | Low | Business insights |
| Currency Mix Report | Which currencies customers pay with | Low | Inventory planning |
| Staff Performance Report | Transaction counts, variance history | Medium | HR tool |

### Mobile-First Operations

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Mobile Till Access | Open/close till from mobile device | Medium | Flexibility for staff |
| Barcode/QR Receipt Lookup | Scan to find receipt | Low | Fast retrieval |
| Push Notification for Variance | Alert manager on mobile | Medium | Immediate awareness |

---

## Anti-Features

Features to explicitly NOT build. Common mistakes in this domain.

### Over-Engineering

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Complex Split-Till Sharing | Multiple staff sharing one till creates accountability nightmare | Enforce one-staff-per-session |
| Automatic Rate Fetching | External API dependency, rates change too frequently for cash business | Operator manually sets rates daily |
| Complex Tip Pooling | Accounting complexity, regulatory issues | Out of scope - operators handle tips offline |
| Integrated Accounting | Creates tight coupling, operators have existing accounting systems | Export data, don't replace accounting |

### Security Theater

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Biometric Authentication | Hardware dependency, privacy concerns, overkill for rental shops | PIN + username is sufficient |
| Cash Drawer Sensors | Hardware complexity, most rental shops use simple drawers | Manual count is fine |
| Video Integration | Complex, privacy laws, storage costs | Recommend security cameras but don't integrate |

### Unnecessary Complexity

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Multiple Till Per Staff | Creates confusion, reconciliation nightmare | One till per staff at a time |
| Complex Cash Pooling | Pooled tills lose individual accountability | Keep tills separate |
| Detailed Change Tracking | Tracking every bill/coin is tedious | Track by denomination group at most |
| POS Inventory Management | Rental business doesn't need retail inventory POS | Focus on rental-specific flows |

### Scope Creep

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Full Accounting System | Operators have QuickBooks, Xero, etc. | Export-friendly reports |
| Payroll Integration | Complex labor laws vary by country | Out of scope |
| Customer Loyalty Program | Different product entirely | Focus on till operations |
| Invoice Financing | Financial product, not POS feature | Out of scope |

---

## Feature Dependencies

```
Multi-Currency Float Tracking
    |
    +-- Currency-Specific Cash Count (requires knowing what currencies to count)
    |
    +-- Multi-Currency Variance (requires currency-specific expected vs actual)

Exchange Rate Configuration
    |
    +-- Change Calculation in THB (requires rates to calculate)
    |
    +-- Multi-Currency Receipt (requires rates for display)

Session Management (EXISTING)
    |
    +-- All transaction recording flows through sessions
    |
    +-- Manager verification depends on closed sessions
```

---

## MVP Recommendation

For the cashier till milestone, prioritize in this order:

### Phase 1: Multi-Currency Core (HIGH Priority)

1. **Multi-Currency Float Tracking** - Essential for real-world operation
2. **Exchange Rate Configuration** - Operators need to set their rates
3. **Currency-Specific Cash Count** - Proper reconciliation by currency
4. **Change Calculation Display** - Help staff give correct change

### Phase 2: Operational Polish (MEDIUM Priority)

5. **Denomination Counting UI** - Better accuracy at close
6. **Quick Payment Reception** - Non-rental cash handling
7. **Manager Override** - Flexibility for edge cases
8. **Variance Alerts** - Proactive management

### Defer to Post-MVP

- Cross-border payment integration (Alipay/WeChat)
- Photo-based cash counting
- Variance trend analysis
- Safe reconciliation
- Mobile till access
- Advanced reporting beyond daily summary

---

## Existing Implementation Analysis

Based on codebase review, here's what already exists:

### Well-Implemented
- TillSession entity with full lifecycle (Open -> Close -> Verify)
- TillTransaction with comprehensive transaction types
- TillService with session management, transaction recording, reconciliation
- Receipt entity with multi-currency payment support
- ReceiptPayment with Currency, ExchangeRate, AmountInBaseCurrency
- SupportedCurrencies constant (THB, USD, EUR, GBP, CNY, JPY, AUD, RUB)
- Daily summary and manager verification workflows

### Gap: Multi-Currency Till Tracking
The current implementation tracks:
- `TillSession.TotalCashIn` - single THB amount
- `TillSession.TotalCashOut` - single THB amount
- `TillSession.ActualCash` - single THB amount
- `TillSession.ExpectedCash` - calculated THB

But real-world operation needs:
- Per-currency cash tracking in the till
- Per-currency opening and closing float
- Per-currency variance calculation
- Exchange rate lookup when accepting foreign cash

**Recommended Addition:** Add `TillCurrencyBalance` embedded collection to track per-currency amounts in the till.

---

## Sources

### POS System Features
- [SelectHub: Top 16 POS System Features 2025](https://www.selecthub.com/pos/pos-system-features/)
- [Shopify: Cash Register vs POS System](https://www.shopify.com/retail/cash-register-pos)
- [Business.com: Guide to Choosing a POS Cash Register](https://www.business.com/articles/pos-cash-register/)

### Cash Drawer Management
- [Star Micronics: How to Balance Cash Drawers](https://starmicronics.com/blog/how-to-balance-cash-drawers-quickly-and-accurately/)
- [Lightspeed: Opening and Closing a Register](https://x-series-support.lightspeedhq.com/hc/en-us/articles/25534185400347-Opening-and-closing-a-register)
- [Microsoft Learn: Shift and Cash Drawer Management](https://learn.microsoft.com/en-us/dynamics365/commerce/shift-drawer-management)
- [Retail Dogma: Balancing a Cash Drawer Best Practices](https://www.retaildogma.com/balancing-a-cash-drawer/)
- [Shopify: Tips for Balancing a Cash Drawer](https://www.shopify.com/retail/balancing-a-cash-drawer)

### Multi-Currency POS
- [Posytude: How Does a POS Machine Handle Multiple Currency Transactions](https://posytude.com/multi-currency-pos-system/)
- [ERPLY: Feature Friday Multi Currency Sales](https://erply.com/feature-friday-multi-currency-sales/)
- [ConnectPOS: Going Global with Multi Currency POS](https://www.connectpos.com/going-global-you-need-multi-currency-operation/)
- [Core Payment Solutions: How to Choose POS for Multi-Currency Business](https://corepaymentsolutions.com/how-to-choose-a-pos-system-for-a-multi-currency-business/)

### Vehicle Rental POS
- [ARM Software: Cash Management for Rental Companies](https://www.armsoftware.com/features/cash-management/)
- [RentMy: POS System for Rental Business](https://rentmy.co/blog/pos-system-for-rental-business/)
- [Booqable: Equipment Rental Security Deposits](https://booqable.com/blog/authorize-credit-card-holds/)

### Thailand Payments Context
- [ConnectPOS: 5 POS Systems in Thailand](https://www.connectpos.com/top-5-pos-systems-in-thailand/)
- [Statista: Biggest POS Payment Methods Thailand](https://www.statista.com/statistics/1296919/preferred-payment-methods-thailand/)
- [ConnectPOS: How to Manage Cash Float in POS Systems](https://www.connectpos.com/how-to-manage-cash-float-in-pos/)

### Petty Cash & Payouts
- [DizLog: Pay-in/Pay-out Petty Cash Tracker](https://helpcenter.dizlog.com/en/articles/6939831-pay-in-pay-out-petty-cash-and-expenses-tracker-cash-management)
- [Lightspeed: Tracking Float, Petty Cash, and Cash Back](https://o-series-support.lightspeedhq.com/hc/en-us/articles/31329389716251-Tracking-your-float-petty-cash-and-cash-back-with-Money-In-Out)

### Thailand Motorbike Rental Practices
- [Thailand Holiday Group: How to Rent a Motorbike in Thailand](https://www.thailandholidaygroup.com/infocentre/motorcycle-rental-thailand/)
- [Traveltomtom: Tips on How to Rent a Motorbike in Thailand](https://www.traveltomtom.net/destinations/asia/thailand/tips-on-how-to-rent-a-motorbike-in-thailand)
