---
phase: 04-payment-terminal
plan: 03
completed: 2026-01-20
duration: ~8 minutes

subsystem: till
tags: [blazor, payment-terminal, till-integration, multi-currency]

dependency-graph:
  requires:
    - 04-01 (PaymentTerminalPanel layout)
    - 04-02 (THB keypad and payment input)
  provides:
    - Complete 3-step transaction workflow
    - Till recording for all payment methods
    - TransactionSearchResult with payments and change
  affects:
    - 04-04 (Receipt generation)
    - Receipt integration

tech-stack:
  patterns:
    - Step-based dialog flow
    - Component composition with parameters
    - TillService integration for payment recording

key-files:
  modified:
    - src/MotoRent.Client/Pages/Staff/TillTransactionDialog.razor
    - src/MotoRent.Client/Components/Till/PaymentTerminalPanel.razor
    - src/MotoRent.Client/Pages/Staff/Till.razor
    - src/MotoRent.Domain/Models/TransactionSearchResult.cs
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.th.resx

decisions:
  - decision: "Step tracking via m_currentStep integer"
    rationale: "Simple state machine for 3-step flow"
  - decision: "PaymentTerminalPanel handles its own Complete button"
    rationale: "Encapsulation - panel controls completion logic"
  - decision: "Back button in dialog footer for Step 3"
    rationale: "Consistent navigation pattern across steps"
  - decision: "Record payments before invoking completion callback"
    rationale: "Ensure till is updated before dialog closes"

metrics:
  commits: 3
  files-modified: 7
---

# Phase 4 Plan 3: Complete Payment Flow Summary

Wired PaymentTerminalPanel into TillTransactionDialog as Step 3 and implemented complete payment flow with till recording and dialog result.

## What Was Built

### 1. Three-Step Dialog Flow (TillTransactionDialog)
- Added `m_currentStep` state (1=Search, 2=Items, 3=Payment)
- `SelectBookingAsync`/`SelectRentalAsync` now set step to 2
- `ProceedToPayment` transitions to step 3 (was: close dialog)
- `GoBackToItemConfirmation` navigates back to step 2
- `OnPaymentComplete` builds final result and closes dialog

### 2. Payment Terminal Integration
- PaymentTerminalPanel rendered in Step 3 with full context:
  - `AmountDue` - grand total from line items
  - `SessionId` - for till recording
  - `RentalId`/`BookingId` - for transaction linking
  - `TransactionType` - for proper categorization
  - `UserName` - for audit trail

### 3. Till Recording on Complete Payment
- Each payment entry recorded to till:
  - THB cash and non-cash: `TillService.RecordCashInAsync`
  - Foreign currency cash: `TillService.RecordForeignCurrencyPaymentAsync`
- Payment method mapped to appropriate TillTransactionType:
  - Card -> CardPayment
  - PromptPay -> PromptPay
  - BankTransfer -> BankTransfer
  - Cash -> Use passed TransactionType
- Spinner shown during completion, buttons disabled

### 4. TransactionSearchResult Extended
- Added `Payments: List<ReceiptPayment>` - collected payments
- Added `Change: decimal` - overpayment amount in THB

### 5. Till Page Updates
- Passes `UserName` parameter to TillTransactionDialog
- Pattern matches result as `TransactionSearchResult`
- Reloads till data after completed transaction

## Commits

| Commit | Description |
|--------|-------------|
| b880d97 | Add Step 3 (Payment Terminal) to TillTransactionDialog |
| 72657d6 | Implement Complete Payment flow with till recording |
| 96954d5 | Wire payment completion and pass UserName to dialog |

## Key Implementation Details

### Step Flow Logic
```csharp
// Step tracking
private int m_currentStep = 1; // 1 = Search, 2 = Items, 3 = Payment

// Selection sets step 2
private async Task SelectBookingAsync(Booking booking)
{
    m_selectedBooking = booking;
    m_currentStep = 2;
    // ...
}

// Proceed transitions to step 3
private void ProceedToPayment()
{
    m_currentStep = 3;
}

// Completion closes dialog with result
private void OnPaymentComplete(List<ReceiptPayment> payments)
{
    var result = new TransactionSearchResult
    {
        Payments = payments,
        Change = payments.Sum(p => p.AmountInBaseCurrency) - m_grandTotal
    };
    this.ModalService.Close(ModalResult.Ok(result));
}
```

### Till Recording Logic
```csharp
foreach (var entry in m_paymentEntries)
{
    if (entry.Currency == SupportedCurrencies.THB || entry.Method != PaymentMethods.Cash)
    {
        // THB or non-cash -> RecordCashInAsync
        await TillService.RecordCashInAsync(SessionId, type, amount, ...);
    }
    else
    {
        // Foreign currency cash -> RecordForeignCurrencyPaymentAsync
        await TillService.RecordForeignCurrencyPaymentAsync(
            SessionId, TransactionType, entry.Currency, entry.Amount, ...);
    }
}
```

## Localization Keys Added

| Key | English | Thai |
|-----|---------|------|
| BackToItems | Back to Items | กลับไปรายการ |
| PaymentComplete | Payment Complete | ชำระเงินเสร็จสิ้น |

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

- `dotnet build src/MotoRent.Client` - 0 errors
- Step navigation: Search -> Item Confirmation -> Payment Terminal
- Back button in Step 3 returns to Step 2
- Complete Payment records to till via TillService
- Dialog closes with TransactionSearchResult containing payments

## Next Phase Readiness

**Plan 04-04: Receipt Generation** - Ready to proceed:
- TransactionSearchResult now includes Payments list
- Change amount calculated for display
- Till recording complete before receipt generation

**Integration points ready:**
- `result.Payments` - list of ReceiptPayment for receipt
- `result.Change` - change amount for receipt display
- `result.LineItems` - line items for receipt items
- `result.GrandTotal` - total for receipt
