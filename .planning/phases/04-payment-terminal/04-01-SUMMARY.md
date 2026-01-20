---
phase: 04
plan: 01
subsystem: till-payment
tags: [blazor, payment, ui, localization, multi-currency]

dependency-graph:
  requires:
    - phase-03 (TransactionSearchResult with GrandTotal)
    - ReceiptPayment model
    - CurrencyDenominations helper
    - PaymentMethods constants
  provides:
    - PaymentTerminalPanel component
    - Payment method tab navigation
    - Cash currency tab navigation
    - Payment entry list display
    - Summary calculations
  affects:
    - 04-02 (THB keypad will integrate into content area)
    - 04-03 (Foreign currency input will integrate)
    - 04-04 (Card/PromptPay/AliPay will integrate)
    - TillTransactionDialog (will embed PaymentTerminalPanel as Step 3)

tech-stack:
  added: []
  patterns:
    - Two-column responsive layout
    - Tab-based navigation with entry indicators
    - Internal PaymentEntry model for working state
    - Conversion to ReceiptPayment on completion

key-files:
  created:
    - src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor
    - src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor.css
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.resx
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.en.resx
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.th.resx
  modified: []

decisions:
  - decision: "PaymentEntry as internal class"
    rationale: "Cleaner separation from ReceiptPayment, allows working state without audit fields"
  - decision: "AliPay as constant string"
    rationale: "Not in PaymentMethods enum, but needed for this UI; kept as private const"
  - decision: "GBP tab visible but disabled"
    rationale: "Per CONTEXT.md, GBP support is deferred but tab should be visible"
  - decision: "Task 3 merged into Task 1"
    rationale: "Payment entry list and summary naturally part of component structure"

metrics:
  duration: ~15 minutes
  completed: 2026-01-20
---

# Phase 4 Plan 1: Payment Terminal Layout Summary

**One-liner:** PaymentTerminalPanel component with two-column layout, payment method tabs, and cash currency tabs for multi-currency split payment collection.

## What Was Built

### PaymentTerminalPanel Component (415 lines)
Created a new Blazor component that provides the visual foundation for the payment terminal:

**Left Column (col-md-4) - Payment Summary:**
- Amount Due card with prominent display
- Progress indicator (animated dot on line)
- Payment Details list with entry display and remove buttons
- Summary section: Total Received, Change, Remaining
- Complete Payment button (disabled until fully paid)
- Cancel button

**Right Column (col-md-8) - Payment Input:**
- Payment method tabs: Cash, Credit Card, PromptPay, AliPay
- Cash currency tabs: THB, USD, GBP (disabled), EUR, CNY
- Content area placeholder for method-specific input

### Key Features
- **Tab navigation** with visual indicators (green dots) for methods/currencies with entries
- **Summary calculations** computed from payment entries (m_totalReceived, m_remaining, m_change)
- **PaymentEntry internal class** for tracking working state before conversion to ReceiptPayment
- **Responsive design** with mobile adjustments (smaller fonts/padding on small screens)

### Localization
18 keys localized for English and Thai:
- UI labels (AmountDue, PaymentMethod, Cash, CreditCard, etc.)
- Action buttons (CompletePayment, Cancel, Remove)
- Status text (NoPaymentsYet, ComingSoon, Remaining, Change)

## Technical Decisions

### PaymentEntry Internal Class
Rather than directly using ReceiptPayment, created an internal PaymentEntry class:
```csharp
private class PaymentEntry
{
    public string EntryId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Method { get; set; } = PaymentMethods.Cash;
    public string Currency { get; set; } = SupportedCurrencies.THB;
    public decimal Amount { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public decimal ExchangeRate { get; set; } = 1.0m;
    public string ExchangeRateSource { get; set; } = "Base";
    public int? ExchangeRateId { get; set; }
    public string? Reference { get; set; }
}
```
This keeps the working model separate from the persisted model and allows easy modification during payment collection.

### AliPay Handling
AliPay is not in the PaymentMethods constants, so defined as a private constant:
```csharp
private const string c_aliPay = "AliPay";
```

### GBP Tab Disabled
Per CONTEXT.md requirements, GBP tab is visible but disabled with "Coming Soon" badge and proper disabled styling.

## Commits

| Hash | Type | Description |
|------|------|-------------|
| a0141d9 | feat | Create PaymentTerminalPanel with two-column layout |
| 8dfb476 | feat | Add localization resources for PaymentTerminalPanel |

## Files Changed

### Created (5 files)
- `src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor` - Main component (415 lines)
- `src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor.css` - Scoped styles
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.resx` - Default resources
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.en.resx` - English
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.th.resx` - Thai

## Deviations from Plan

None - plan executed exactly as written. Task 3 was already implemented within Task 1 as the payment entry list and summary functionality was naturally part of the component structure.

## Next Steps (Plan 04-02)

The payment terminal layout is complete. Next plan will implement:
- THB numeric keypad in the content area
- Quick amount buttons (100, 500, 1000, Remaining)
- THB cash entry workflow

## Verification Checklist

- [x] `dotnet build src/MotoRent.Client` - No errors
- [x] PaymentTerminalPanel.razor exists with 415 lines (exceeds 150 minimum)
- [x] Two-column Bootstrap layout (col-md-4 + col-md-8)
- [x] Payment method tabs (Cash, Card, PromptPay, AliPay)
- [x] Cash currency tabs (THB, USD, GBP disabled, EUR, CNY)
- [x] Payment entry list with remove capability
- [x] Summary section with Total Received, Change, Remaining
- [x] Complete Payment button disabled until remaining = 0
- [x] Localization files for EN/TH
