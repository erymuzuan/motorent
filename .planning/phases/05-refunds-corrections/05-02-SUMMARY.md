---
phase: 05-refunds-corrections
plan: 02
subsystem: services
tags: [pin-service, void-operations, refund-operations, security]

dependencies:
  requires: [05-01]
  provides: [manager-pin-service, void-transaction-method, overpayment-refund-method]
  affects: [05-03, 05-04]

tech-stack:
  added: []
  patterns: [pbkdf2-hashing, in-memory-lockout, compensating-entry]

key-files:
  created:
    - src/MotoRent.Services/ManagerPinService.cs
  modified:
    - src/MotoRent.Services/TillService.cs
    - src/MotoRent.Server/Program.cs

decisions:
  - id: dec-05-02-01
    choice: "PBKDF2 with static Pbkdf2 method for PIN hashing"
    reason: ".NET 10 deprecates Rfc2898DeriveBytes constructors; static method is recommended"
  - id: dec-05-02-02
    choice: "In-memory lockout tracking with static dictionary"
    reason: "Acceptable for MVP; can migrate to Redis/SQL for distributed deployments"
  - id: dec-05-02-03
    choice: "Bidirectional linking for voids after save"
    reason: "Compensating entry needs ID before linking; separate save operation required"

metrics:
  duration: "~10 minutes"
  completed: "2026-01-20"
---

# Phase 5 Plan 2: Manager PIN Service Summary

**One-liner:** PBKDF2 PIN hashing with 3-attempt lockout, void/refund operations with compensating entries.

## What Was Built

### ManagerPinService (202 lines)

New service for secure manager PIN management:

```
src/MotoRent.Services/ManagerPinService.cs
```

**Public API:**
- `SetPinAsync(userName, pin, changedBy)` - Set/update 4-6 digit PIN with PBKDF2 hashing
- `VerifyPin(manager, enteredPin)` - Verify PIN with lockout enforcement, returns tuple (IsValid, Error, RemainingAttempts)
- `IsLockedOut(userName)` - Check if user is currently locked out
- `GetLockoutSecondsRemaining(userName)` - Get lockout countdown
- `RemovePinAsync(userName, changedBy)` - Clear PIN (admin/offboarding)

**Security Implementation:**
- PBKDF2 with SHA256, 10,000 iterations, 32-byte hash, 16-byte salt
- 3 failed attempts triggers 5-minute lockout (per CONTEXT.md)
- Thread-safe lockout tracking with static dictionary and lock

### TillService Void/Refund Operations (+233 lines)

Extended `src/MotoRent.Services/TillService.cs` with new region:

**Void Operations:**
- `VoidTransactionAsync(transactionId, staffUserName, managerUserName, reason)` - Void with manager approval
  - Creates compensating VoidReversal entry with reversed direction
  - Marks original as voided with audit fields (VoidedAt, VoidedByUserName, VoidReason, VoidApprovedByUserName)
  - Reverses session balance effects (cash and non-cash)
  - Links original <-> compensating bidirectionally
  - Prevents self-approval (staff != manager)
  - Requires open session

- `CanVoidTransactionAsync(transactionId)` - Pre-check validation
  - Cannot void already-voided transactions
  - Cannot void VoidReversal entries
  - Requires open session

- `GetVoidedTransactionsAsync(sessionId)` - Audit view for managers
- `GetTransactionByIdAsync(transactionId)` - Helper method

**Refund Operations:**
- `RecordOverpaymentRefundAsync(sessionId, refundAmountThb, reason, originalPaymentIds, rentalId, username)`
  - Always issues THB cash (per CONTEXT.md)
  - Updates session TotalCashOut and CurrencyBalances
  - Links to original payment IDs in notes

### DI Registration

Added `builder.Services.AddScoped<ManagerPinService>();` to Program.cs in Cashier till services section.

## Key Implementation Details

### Void Logic Flow

```
1. Staff selects transaction to void
2. System checks: not already voided, session open, not a VoidReversal
3. Staff enters reason
4. Manager enters PIN for approval
5. System verifies PIN (lockout if 3 failures)
6. System creates VoidReversal entry (opposite direction, same amounts)
7. System marks original as voided
8. System reverses session balances
9. System links original <-> compensating
```

### Balance Reversal Logic

| Original Transaction | Session Balance Effect |
|---------------------|------------------------|
| Cash In | Subtract from TotalCashIn, subtract from CurrencyBalances[currency] |
| Cash Out | Subtract from TotalCashOut, add back to CurrencyBalances[currency] |
| Card Payment | Subtract from TotalCardPayments |
| Bank Transfer | Subtract from TotalBankTransfers |
| PromptPay | Subtract from TotalPromptPay |
| Drop | Subtract from TotalDropped |
| Top Up | Subtract from TotalToppedUp |

### Foreign Currency Void

Voids preserve exact foreign currency amounts:
- Original: 100 USD at 35.5 THB/USD = 3,550 THB
- VoidReversal: 100 USD at 35.5 THB/USD = 3,550 THB
- Both amounts reversed exactly (no rate recalculation)

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 322f675 | feat | Create ManagerPinService with PIN hashing and lockout |
| c74323d | feat | Extend TillService with void and refund operations |
| d2a26cf | chore | Register ManagerPinService in DI container |

## Files Changed

| File | Change | Lines |
|------|--------|-------|
| `src/MotoRent.Services/ManagerPinService.cs` | Created | +202 |
| `src/MotoRent.Services/TillService.cs` | Modified | +233 |
| `src/MotoRent.Server/Program.cs` | Modified | +1 |

## Verification

- [x] `dotnet build src/MotoRent.Services` - Build succeeded
- [x] `dotnet build src/MotoRent.Server` - Build succeeded
- [x] ManagerPinService has SetPinAsync, VerifyPin, IsLockedOut, GetLockoutSecondsRemaining
- [x] TillService has VoidTransactionAsync, RecordOverpaymentRefundAsync methods
- [x] Void logic creates compensating entry with reversed direction
- [x] Self-approval is prevented

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Updated Rfc2898DeriveBytes to static Pbkdf2 method**
- **Found during:** Task 1 build verification
- **Issue:** SYSLIB0060 warning - constructor is obsolete in .NET 10
- **Fix:** Used static `Rfc2898DeriveBytes.Pbkdf2()` method instead
- **Files modified:** `src/MotoRent.Services/ManagerPinService.cs`
- **Commit:** 322f675

## Next Phase Readiness

Ready for Plan 05-03: Void Dialog & Workflow

**Prerequisites met:**
- ManagerPinService available for PIN verification in dialogs
- TillService.VoidTransactionAsync ready for void workflow
- TillService.CanVoidTransactionAsync for pre-validation

**Dependencies provided:**
- `ManagerPinService.VerifyPin()` for void approval PIN entry
- `TillService.VoidTransactionAsync()` for executing voids
- `TillService.GetVoidedTransactionsAsync()` for audit view

---

*Completed: 2026-01-20*
