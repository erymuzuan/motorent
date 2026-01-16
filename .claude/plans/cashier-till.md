# Cashier Till System Implementation Plan

## Overview
A dedicated cashier till system for MotoRent staff with individual sessions, cash drawer management, payouts, and shift reconciliation. Includes manager EOD verification workflow.

## User Requirements
- **Individual staff sessions** - Each staff opens/closes their own session
- **Payouts** - Fuel reimbursements, agent commissions, petty cash
- **Reconciliation** - End of each shift with variance tracking
- **Manager EOD** - End of day verification for all sessions, cash drops, card payments
- **UI** - Dedicated `/staff/till` page and `/manager/eod` page

---

## 1. Domain Entities

### TillSession
```
File: src/MotoRent.Domain/Entities/TillSession.cs
```
- TillSessionId, ShopId, StaffUserName, StaffDisplayName
- Status: Open, Reconciling, Closed, ClosedWithVariance, PendingVerification, Verified
- OpeningFloat, OpenedAt, OpeningNotes
- TotalCashIn, TotalCashOut (denormalized running totals)
- TotalDropped, TotalToppedUp
- ActualCash, Variance, ClosedAt, ClosingNotes (populated on close)
- VerifiedByUserName, VerifiedAt, VerificationNotes (for manager verification)

### TillTransaction
```
File: src/MotoRent.Domain/Entities/TillTransaction.cs
```
- TillTransactionId, TillSessionId
- TransactionType (enum), Direction (In/Out)
- Amount, Category, SubCategory, Description
- PaymentId, DepositId, RentalId (optional references)
- RecipientName, ReceiptNumber (for payouts)
- TransactionTime, RecordedByUserName, Notes
- IsVerified, VerifiedByUserName, VerifiedAt (for manager verification)

### TillEnums
```
File: src/MotoRent.Domain/Entities/TillEnums.cs
```
**TillSessionStatus:** Open, Reconciling, Closed, ClosedWithVariance, PendingVerification, Verified

**TillTransactionType (Inflows):**
- RentalPayment, BookingDeposit, SecurityDeposit
- DamageCharge, LateFee, Surcharge, MiscellaneousIncome
- TopUp, CardPayment, BankTransfer, PromptPay

**TillTransactionType (Outflows):**
- DepositRefund, FuelReimbursement, AgentCommission
- PettyCash, Drop, CashShortage

**TillTransactionDirection:** In, Out

---

## 2. Database Schema

### TillSession Table
```
File: database/tables/MotoRent.TillSession.sql
```
Computed columns: ShopId, StaffUserName, Status, OpenedAt, ClosedAt, VerifiedByUserName, VerifiedAt
Indexes: ShopId, Status, StaffUserName, OpenedAt

### TillTransaction Table
```
File: database/tables/MotoRent.TillTransaction.sql
```
Computed columns: TillSessionId, TransactionType, Direction, Amount, Category, PaymentId, DepositId, RentalId, TransactionTime, IsVerified
Indexes: TillSessionId, TransactionType, TransactionTime

---

## 3. Service Layer

### TillService
```
File: src/MotoRent.Services/TillService.cs
```

**Session Management:**
- `OpenSessionAsync(shopId, staffUserName, staffDisplayName, openingFloat, notes)`
- `GetActiveSessionAsync(shopId, staffUserName)`
- `GetActiveSessionsAsync(shopId)`
- `CloseSessionAsync(sessionId, actualCash, notes)`
- `GetSessionHistoryAsync(shopId, filters...)`

**Transaction Recording:**
- `RecordCashInAsync(sessionId, type, amount, description, references...)`
- `RecordPayoutAsync(sessionId, type, amount, description, category, recipientName, receiptNumber...)`
- `RecordDropAsync(sessionId, amount, notes)`
- `RecordTopUpAsync(sessionId, amount, notes)`
- `RecordCardPaymentAsync(sessionId, amount, description, paymentId, rentalId)` - Non-cash tracking
- `GetTransactionsAsync(sessionId)`

**Integration Methods:**
- `RecordRentalPaymentToTillAsync(shopId, staffUserName, paymentId, rentalId, amount, description, paymentMethod)`
- `RecordDepositRefundFromTillAsync(shopId, staffUserName, depositId, rentalId, amount, description)`

**Manager EOD Methods:**
- `GetSessionsForVerificationAsync(shopId, date)` - Get all closed sessions for a date
- `GetDailySummaryAsync(shopId, date)` - Aggregated totals for EOD
- `VerifySessionAsync(sessionId, managerUserName, notes)` - Mark session as verified
- `VerifyTransactionAsync(transactionId, managerUserName)` - Mark individual transaction as verified
- `GetUnverifiedDropsAsync(shopId, date)` - Cash drops pending verification
- `GetCardPaymentsSummaryAsync(shopId, date)` - Card/electronic payments for reconciliation

**Reports:**
- `GetSessionSummaryAsync(sessionId)` - Returns TillSessionSummary DTO
- `GetDailyReportAsync(shopId, date)`
- `GetSessionsWithVarianceAsync(shopId, fromDate, toDate)`

---

## 4. UI Components

### Staff Till Page
```
File: src/MotoRent.Client/Pages/Staff/Till.razor
Route: /staff/till
```
- Session status header (staff name, opened time, float)
- Cash In / Cash Out totals (large touch-friendly cards)
- Expected balance display
- Quick payout buttons: Fuel, Agent Commission, Petty Cash
- Till operations: Cash Drop, Top Up, Close Shift
- Recent transactions list (last 10)

### Manager EOD Page
```
File: src/MotoRent.Client/Pages/Manager/EndOfDay.razor
Route: /manager/eod
```
- Date picker (defaults to today)
- Daily summary cards:
  - Total Cash In / Out
  - Total Card Payments
  - Total Cash Drops
  - Expected vs Actual variance
- Session list with status indicators
- Cash drops requiring verification
- Card payment reconciliation section
- Verify All / Verify Individual buttons

### Staff Till Dialogs
```
Directory: src/MotoRent.Client/Pages/Staff/Till/
```
- `OpenSessionDialog.razor` - Enter opening float
- `CloseSessionDialog.razor` - Reconciliation with cash count, variance acknowledgment
- `RecordPayoutDialog.razor` - Payout entry with category, recipient, receipt
- `CashDropDialog.razor` - Drop/TopUp amount entry
- `TillHistoryDialog.razor` - View past sessions and transactions

### Manager EOD Dialogs
```
Directory: src/MotoRent.Client/Pages/Manager/EndOfDay/
```
- `SessionDetailDialog.razor` - View full session details with all transactions
- `VerifySessionDialog.razor` - Add verification notes
- `CashDropVerificationDialog.razor` - Verify cash drops with safe count

### Components
```
Directory: src/MotoRent.Client/Components/Staff/
```
- `TillSessionCard.razor` - Session status display
- `TillTransactionList.razor` - Transaction history with direction badges
- `DailySummaryCard.razor` - EOD summary display
- `PaymentMethodBreakdown.razor` - Cash vs Card vs Electronic breakdown

---

## 5. DTOs and View Models

### TillSessionSummary
```csharp
public class TillSessionSummary
{
    public int TillSessionId { get; set; }
    public string StaffDisplayName { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalToppedUp { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public TillSessionStatus Status { get; set; }
    public bool IsVerified { get; set; }
}
```

### DailyTillSummary
```csharp
public class DailyTillSummary
{
    public DateTime Date { get; set; }
    public int ShopId { get; set; }
    public int TotalSessions { get; set; }
    public int VerifiedSessions { get; set; }
    public int SessionsWithVariance { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalCardPayments { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal TotalPromptPay { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalVariance { get; set; }
    public List<TillSessionSummary> Sessions { get; set; }
}
```

---

## 6. Integration Points

### Check-In (CollectDepositStep.razor)
When PaymentMethod = "Cash", record to active till session:
```csharp
await TillService.RecordCashInAsync(sessionId,
    TillTransactionType.RentalPayment, amount,
    $"Check-in #{rentalId}", paymentId: paymentId);
```

For Card/PromptPay/BankTransfer, also record (for tracking, not cash):
```csharp
await TillService.RecordCardPaymentAsync(sessionId,
    amount, $"Card payment for rental #{rentalId}",
    paymentId: paymentId, rentalId: rentalId);
```

### Check-Out (CheckOutDialog.razor)
When refund method = "Cash", record from active till session:
```csharp
await TillService.RecordPayoutAsync(sessionId,
    TillTransactionType.DepositRefund, refundAmount,
    $"Deposit refund #{rentalId}", depositId: depositId);
```

---

## 7. Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType for TillSession, TillTransaction |
| `src/MotoRent.Server/Program.cs` | Register TillService and repositories |
| `src/MotoRent.Client/Layout/NavMenu.razor` | Add Till menu item under Staff section, EOD under Manager |
| `src/MotoRent.Client/Pages/Rentals/CheckIn.razor` | Call TillService for cash payments |
| `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` | Call TillService for cash refunds |

---

## 8. Localization Files

Create for each component:
- `Resources/Pages/Staff/Till.resx` (.th.resx, .en.resx, .ms.resx)
- `Resources/Pages/Staff/Till/OpenSessionDialog.resx` (+ .th, .en, .ms)
- `Resources/Pages/Staff/Till/CloseSessionDialog.resx` (+ .th, .en, .ms)
- `Resources/Pages/Staff/Till/RecordPayoutDialog.resx` (+ .th, .en, .ms)
- `Resources/Pages/Manager/EndOfDay.resx` (+ .th, .en, .ms)
- `Resources/Pages/Manager/EndOfDay/SessionDetailDialog.resx` (+ .th, .en, .ms)
- `Resources/Pages/Manager/EndOfDay/VerifySessionDialog.resx` (+ .th, .en, .ms)

Key terms: CashierTill, OpenSession, CloseSession, OpeningFloat, ExpectedCash, ActualCash, Variance, CashIn, CashOut, CashDrop, TopUp, FuelReimbursement, AgentCommission, PettyCash, Short, Over, EndOfDay, Verify, CardPayments, BankTransfer, PromptPay, SafeCount, Reconciliation

---

## 9. Implementation Sequence

1. **Entities** - TillSession, TillTransaction, TillEnums, update Entity.cs
2. **Database** - SQL table scripts
3. **Service** - TillService with all methods including EOD
4. **Staff UI** - Till.razor page with session card and transaction list
5. **Staff Dialogs** - Open, Close, Payout, Drop dialogs
6. **Manager EOD UI** - EndOfDay.razor page
7. **Manager EOD Dialogs** - Session detail, verification dialogs
8. **Integration** - Connect to check-in/check-out flows
9. **Localization** - All resource files

---

## 10. Verification Checklist

### Staff Workflow
1. **Open Session**: Staff can open till with float amount
2. **Record Payout**: Can record fuel/agent/petty cash payouts with receipts
3. **Cash Drop/TopUp**: Can move cash to/from safe
4. **Integration**: Cash payments in check-in/out appear in till
5. **Reconciliation**: Can close shift with actual count, variance tracked
6. **History**: Can view past sessions and transactions

### Manager Workflow
1. **EOD View**: Manager can see all sessions for a date
2. **Summary**: Daily totals for cash, card, drops displayed correctly
3. **Verify Session**: Can verify individual sessions with notes
4. **Variance Tracking**: Sessions with variance clearly flagged
5. **Drop Verification**: Cash drops can be verified against safe count
6. **Card Reconciliation**: Card payment totals match POS/terminal

### Technical
1. **Build**: `dotnet build` succeeds
2. **Manual Test**: Complete full staff workflow in browser
3. **Manual Test**: Complete full manager EOD workflow in browser

---

## 11. Security Considerations

- Only staff assigned to a shop can open sessions for that shop
- Only managers (ShopManager, OrgAdmin) can access EOD verification
- Sessions can only be closed by the staff who opened them
- Verified sessions cannot be modified
- All transactions are audited with timestamps and usernames
