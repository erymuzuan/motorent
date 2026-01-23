---
phase: 04
plan: 02
subsystem: till-payment
tags: [blazor, payment, ui, keypad, currency, exchange-rate]

dependency-graph:
  requires:
    - 04-01 (PaymentTerminalPanel layout and structure)
    - DenominationEntryPanel component
    - ExchangeRateService for currency conversion
    - CurrencyDenominations helper
  provides:
    - ThbKeypadPanel component
    - THB cash input via numeric keypad
    - Foreign currency input via denomination counting
    - Non-cash payment panels (Card, PromptPay, AliPay)
    - Real-time exchange rate conversion display
  affects:
    - 04-03 (complete payment flow)
    - 04-04 (void/refund handling)
    - TillTransactionDialog (will embed PaymentTerminalPanel)

tech-stack:
  added: []
  patterns:
    - Component composition (ThbKeypadPanel in PaymentTerminalPanel)
    - Exchange rate service integration
    - Split payment tracking with audit trail

key-files:
  created:
    - src/MotoRent.Client/Components/Till/ThbKeypadPanel.razor
    - src/MotoRent.Client/Components/Till/ThbKeypadPanel.razor.css
    - src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.resx
    - src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.en.resx
    - src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.th.resx
  modified:
    - src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.resx
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.en.resx
    - src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.th.resx

decisions:
  - decision: "Component composition for THB keypad"
    rationale: "ThbKeypadPanel as separate component for reusability and cleaner code"
  - decision: "Pre-fill remaining on method switch"
    rationale: "Faster workflow - staff often want to pay exact remaining"
  - decision: "Required reference for AliPay only"
    rationale: "Card/PromptPay can verify via terminal/app; AliPay needs manual confirmation"

metrics:
  duration: ~20 minutes
  completed: 2026-01-20
---

# Phase 4 Plan 2: THB Keypad & Payment Input Summary

**One-liner:** THB numeric keypad with quick amounts, DenominationEntryPanel integration for foreign currency with exchange rate display, and input panels for Card/PromptPay/AliPay payments.

## What Was Built

### ThbKeypadPanel Component (144 lines)
New Blazor component for Thai Baht cash entry:

**Display:**
- Large amount display with currency symbol
- Right-aligned, responsive font sizing

**Numeric Keypad:**
- 3x4 grid layout: 1-9, C (clear), 0, backspace
- Touch-friendly button sizing (60px min height)
- Visual feedback on press

**Quick Amounts:**
- Preset buttons: 100, 500, 1,000
- "Remaining" button showing actual balance due
- Quick amounts auto-submit payment

**Add Button:**
- Green "Add Payment" button
- Disabled when amount is zero
- Shows amount being added

### PaymentTerminalPanel Updates (806 lines total)

**Cash - THB Integration:**
- ThbKeypadPanel embedded when THB currency selected
- Passes remaining amount for quick fill
- Handles OnAmountSubmit callback

**Cash - Foreign Currency Integration:**
- DenominationEntryPanel for USD/EUR/CNY
- Exchange rate display with current rate
- Loading indicator while fetching rate
- Warning when no rate configured
- Real-time THB equivalent calculation
- Add button with foreign amount display

**Non-Cash Payment Panels:**

*Credit Card:*
- Large card icon header
- Amount input with THB symbol
- "Pay Remaining" quick-fill link
- Optional authorization code field
- "Add Card Payment" button

*PromptPay:*
- QR code icon header
- Instruction text
- Amount input with remaining link
- Optional transaction reference
- "Add PromptPay Payment" button

*AliPay:*
- AliPay icon header
- Instruction about customer app
- Amount input with remaining link
- Required confirmation number field
- Help text explaining reference
- Button disabled until reference entered

**State Additions:**
- `m_nonCashAmount` - decimal for card/promptpay/alipay
- `m_nonCashReference` - string for reference fields

**Method Additions:**
- `FillRemaining()` - sets non-cash amount to remaining
- `ResetNonCashInput()` - clears non-cash state
- `AddCardPayment()` - creates card entry
- `AddPromptPayPayment()` - creates PromptPay entry
- `AddAliPayPayment()` - creates AliPay entry (requires reference)

### Localization
22 new keys added across all components:

**ThbKeypadPanel (4 keys):**
- EnterThbAmount, Clear, AddPayment, Remaining

**PaymentTerminalPanel (18 keys):**
- CountNotes, Rate, LoadingRate, NoRateConfigured, ThbEquivalent, AddPayment
- Amount, EnterCardAmount, CardReference, AddCardPayment
- PromptPayInstruction, PromptPayReference, AddPromptPayPayment
- AliPayInstruction, AliPayReference, AliPayReferenceHelp, AddAliPayPayment
- PayRemaining, Optional, Required

## Technical Decisions

### Component Composition
ThbKeypadPanel extracted as separate component rather than inline in PaymentTerminalPanel:
- Reusable for other THB input scenarios
- Cleaner separation of concerns
- Easier to test and maintain

### Pre-fill Remaining Balance
When switching to non-cash methods, the amount field is pre-filled with remaining balance:
- Speeds up common case (pay exact remaining)
- User can easily modify if needed
- Remaining link still available for re-fill

### Reference Field Requirements
Different requirements per payment method based on real-world usage:
- **Card:** Optional - terminal provides authorization
- **PromptPay:** Optional - can verify via bank app
- **AliPay:** Required - no other way to verify payment

### Exchange Rate Audit Trail
Foreign currency payments capture full audit information:
- `ExchangeRate` - rate used for conversion
- `ExchangeRateSource` - where rate came from (Manual, API)
- `ExchangeRateId` - reference to ExchangeRate entity

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 15a7075 | feat | Create ThbKeypadPanel component |
| 6bc8064 | feat | Integrate THB keypad and DenominationEntryPanel |
| b073784 | feat | Add non-cash payment panels for Card, PromptPay, AliPay |

## Files Changed

### Created (5 files)
- `src/MotoRent.Client/Components/Till/ThbKeypadPanel.razor` - THB keypad component (144 lines)
- `src/MotoRent.Client/Components/Till/ThbKeypadPanel.razor.css` - Keypad styles
- `src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.resx` - Default resources
- `src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.en.resx` - English
- `src/MotoRent.Client/Resources/Components/Till/ThbKeypadPanel.th.resx` - Thai

### Modified (4 files)
- `src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor` - Added all input panels (806 lines)
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.resx` - 18 new keys
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.en.resx` - English translations
- `src/MotoRent.Client/Resources/Components/Till/PaymentTerminalPanel.th.resx` - Thai translations

## Deviations from Plan

None - plan executed exactly as written.

## Verification Checklist

- [x] `dotnet build src/MotoRent.Client` - No errors
- [x] ThbKeypadPanel.razor exists with keypad grid (144 lines, exceeds 80 minimum)
- [x] Numeric keypad: 1-9, C, 0, backspace buttons
- [x] Quick amount buttons: 100, 500, 1,000, Remaining
- [x] Add button calls OnAmountSubmit
- [x] PaymentTerminalPanel updated (806 lines)
- [x] THB keypad shows for THB currency
- [x] DenominationEntryPanel shows for USD/EUR/CNY
- [x] Exchange rate displayed with conversion
- [x] THB equivalent updates on count change
- [x] Card panel: amount + optional reference
- [x] PromptPay panel: amount + optional reference
- [x] AliPay panel: amount + required reference
- [x] All payment methods add entries to list
- [x] Localization files updated for EN/TH

## Next Steps (Plan 04-03)

With all payment input complete, the next plan will focus on:
- Complete payment flow integration
- Receipt generation
- Till transaction recording
- Summary updates after payment
