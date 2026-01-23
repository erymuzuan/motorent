---
status: testing
phase: 02-multi-currency-till-operations
source: 02-01-SUMMARY.md, 02-02-SUMMARY.md, 02-03-SUMMARY.md
started: 2026-01-20T12:00:00Z
updated: 2026-01-20T12:00:00Z
---

## Current Test

number: 1
name: Open Till with Multi-Currency Float
expected: |
  Opening a till session initializes CurrencyBalances with THB = opening float and foreign currencies (USD, EUR, CNY) = 0. The session tracks balances per currency.
awaiting: user response

## Tests

### 1. Open Till with Multi-Currency Float
expected: Opening a till session initializes CurrencyBalances with THB = opening float and foreign currencies (USD, EUR, CNY) = 0.
result: [pending]

### 2. View Per-Currency Balance (CurrencyBalancePanel)
expected: CurrencyBalancePanel shows collapsed THB total by default. Expanding shows each currency with balance > 0 and its THB equivalent.
result: [pending]

### 3. Receive Foreign Currency Payment
expected: In TillReceivePaymentDialog, selecting USD/EUR/CNY shows denomination entry panel with correct denominations. Entering counts calculates total received.
result: [pending]

### 4. Exchange Rate Preview on Payment
expected: When receiving foreign currency, dialog shows: exchange rate used, THB equivalent, and change to give in THB.
result: [pending]

### 5. Currency Balance Updates After Payment
expected: After accepting foreign currency payment, CurrencyBalancePanel shows updated balance for that currency and updated THB equivalent.
result: [pending]

### 6. Multi-Currency Cash Drop - Currency Tabs
expected: TillCashDropDialog shows tabs for each currency with balance > 0. Each tab has denomination entry panel.
result: [pending]

### 7. Multi-Currency Cash Drop - Validation
expected: Cannot drop more than available balance per currency. Attempting to exceed shows validation error or disabled save.
result: [pending]

### 8. Multi-Currency Cash Drop - Summary
expected: After entering drop amounts, dialog shows summary of all currencies being dropped before confirming.
result: [pending]

### 9. Denomination Entry Panel - Auto Calculate
expected: DenominationEntryPanel shows denominations for selected currency. Entering counts auto-calculates total (denomination * count summed).
result: [pending]

### 10. Localization (English/Thai)
expected: All new UI elements (CurrencyBalancePanel, DenominationEntryPanel, dialog labels) display correctly in both English and Thai.
result: [pending]

## Summary

total: 10
passed: 0
issues: 0
pending: 10
skipped: 0

## Gaps

[none yet]
