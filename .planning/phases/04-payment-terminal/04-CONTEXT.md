# Phase 4: Payment Terminal Redesign - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Staff can receive multi-currency split payments through a unified payment terminal. The terminal handles THB, USD, EUR, CNY cash (with denomination counting for foreign), plus non-cash methods (Credit Card, PromptPay, AliPay). Change is always in THB.

</domain>

<decisions>
## Implementation Decisions

### Layout Structure
- Two-column layout: Left (summary + payment details), Right (input area)
- Left panel shows: Total Amount Due, progress indicator, Payment Details list, Total Received, Change, Remaining, Complete Payment button
- Right panel shows: Payment method tabs → context-specific input
- "Remaining" shown in red when balance due
- "Complete Payment" button disabled until fully paid (remaining = ฿0)

### Payment Method Tabs
- Four tabs: Cash, Credit Card, PromptPay, AliPay
- Tab icons: Cash (bills icon), Credit Card (card icon), PromptPay (QR icon), AliPay (phone icon)
- Selected tab has blue border/highlight

### Cash → Currency Tabs
- Secondary tabs under Cash: THB, USD, GBP, EUR, CNY
- Each tab shows flag icon + currency code
- Green dot indicator on currencies that have entries
- GBP tab shown but disabled (deferred)

### THB Cash Input
- Large display field showing entered amount (e.g., ฿2,500)
- Numeric keypad: 1-9, C (clear all), 0, ⌫ (backspace)
- Quick amount buttons: ฿100, ฿500, ฿1,000, [Remaining amount]
- "Clear THB" button to reset THB entry
- "Reset All" button to clear all currencies

### Foreign Currency Cash Input (USD, EUR, CNY)
- "Count [Currency] Notes" header
- "Total [Currency] Entered" display showing sum
- Denomination grid showing notes and coins:
  - Notes with bill icon, coins with coin icon
  - Each denomination has COUNT input field
- "Reset All" button to clear all currencies
- Denominations per currency:
  - USD: 100, 50, 20, 10, 5, 1
  - EUR: 100, 50, 20, 10, 5, 2, 1
  - CNY: 100, 50, 20, 10, 5, 1

### PromptPay
- QR icon centered
- "Enter amount to generate PromptPay QR" instruction
- Amount input field (pre-filled with remaining balance)
- "Pay Remaining: ฿X,XXX" quick-fill link
- "Generate Payment" button (blue, prominent)
- QR code uses organization-level PromptPay ID (from settings)
- Also supports manual confirmation flow (customer pays via app, staff confirms)

### Credit Card
- Same layout as PromptPay (enter amount style)
- Amount input field
- Optional reference field (for approval code)
- No last 4 digits capture required
- Manual entry for MVP (terminal integration planned for future)

### AliPay
- Manual confirmation flow
- Customer pays via their AliPay app
- Staff enters reference number from customer's confirmation
- Amount field + reference field

### Payment Details List
- Running list of payment entries
- Each entry shows: Method + Currency, Amount (e.g., "Cash (THB) ฿2,500")
- Entries can be removed (X button)
- Shows: Total Received (THB), Change (THB), Remaining (THB)

### Claude's Discretion
- Exact button sizing and spacing
- Animation/feedback when payment added
- Error message presentation
- Keyboard vs touch optimization

</decisions>

<specifics>
## Specific Ideas

- Layout follows the attached mockup screenshots exactly
- Progress indicator (blue line with dot) between amount due and payment details
- Settings gear icon next to amount due (for exchange rate view?)
- "System Online" indicator in top-right corner
- Currency flags should use standard flag icons (Thai, US, UK, EU, China flags)

</specifics>

<deferred>
## Deferred Ideas

- GBP currency support — show tab but disabled, implement later
- Integrated payment terminal — manual for now, API integration planned
- Bank Transfer payment method — not in current mockup, consider later
- JPY currency support — mentioned in roadmap deferred list

</deferred>

---

*Phase: 04-payment-terminal*
*Context gathered: 2026-01-20*
