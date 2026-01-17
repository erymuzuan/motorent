# Cashier Till System Implementation Plan

## Overview
A dedicated cashier till system for MotoRent staff with individual sessions, cash drawer management, payouts, and shift reconciliation. **The till becomes the central context for all rental operations**, replacing the header shop selector.

## User Requirements
- **Individual staff sessions** - Each staff opens/closes their own session
- **Payouts** - Fuel reimbursements, agent commissions, petty cash
- **Reconciliation** - End of each shift with variance tracking
- **Manager EOD** - End of day verification for all sessions, cash drops, card payments
- **Till-centric architecture** - All rental check-in/out must be associated with an open till
- **Shop selection via Till** - Staff selects shop when opening till, not via header

---

## PHASE 2: Till-Centric Architecture (NEW)

### Key Changes

1. **Remove Header Shop Selector**
   - File: `src/MotoRent.Client/Layout/MainLayout.razor`
   - Remove the shop dropdown (lines 66-105)
   - Replace with Till Status Button:
     - If till open: Shows "Till: {ShopName}" → links to `/staff/till`
     - If no till: Shows "Open Till" button → opens inline dialog

2. **Till Session Constraints**
   - One open till per staff per shop per day
   - Staff can have multiple tills for different shops (if multi-shop access)
   - Cannot open new till for same shop until previous one is closed

3. **Rental-Till Association**
   - Add `TillSessionId` (nullable int) to `Rental` entity
   - `RentedFromShopId` is derived from `TillSession.ShopId`
   - All rental operations require active till context

4. **Check-In Flow Changes**
   - Remove shop requirement check from `RequestContext.GetShopId()`
   - Instead check for active till session for current user
   - If no till: Show inline `OpenTillDialog` with shop selection
   - Once till selected, proceed with check-in using till's ShopId

5. **Check-Out Flow Changes**
   - Get till context from the rental's associated till or user's active till
   - Record cash refunds to the active till session

### New/Modified Files

| File | Change |
|------|--------|
| `src/MotoRent.Client/Layout/MainLayout.razor` | Replace shop selector with Till status button |
| `src/MotoRent.Domain/Entities/Rental.cs` | Add `TillSessionId` property |
| `src/MotoRent.Services/TillService.cs` | Add `GetActiveSessionForUserAsync(userName)` |
| `src/MotoRent.Client/Pages/Rentals/CheckIn.razor` | Use till context instead of shop context |
| `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` | Use till context for refunds |
| `src/MotoRent.Client/Pages/Staff/TillOpenSessionDialog.razor` | Add shop selector dropdown |
| `src/MotoRent.Server/Controllers/AccountController.cs` | Remove/deprecate SwitchShop endpoint |

### TillService New Methods
```csharp
// Get any active till for user (across all shops)
Task<TillSession?> GetActiveSessionForUserAsync(string userName);

// Check if user can open till for shop (one per day rule)
Task<bool> CanOpenSessionAsync(int shopId, string userName, DateOnly date);

// Get user's accessible shops for till opening
Task<List<Shop>> GetShopsForTillOpeningAsync(string userName);
```

### Header Till Button Component
```
File: src/MotoRent.Client/Components/Shared/TillStatusButton.razor
```
- Shows current till status with shop name
- Clicking navigates to `/staff/till`
- If no till, shows "Open Till" with shop selection modal
- Badge shows expected cash amount

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

## 6. Integration Points (Till-Centric)

### Check-In Flow (CheckIn.razor)

**Step 1: Validate Till Context**
```csharp
// Instead of RequestContext.GetShopId()
var activeTill = await TillService.GetActiveSessionForUserAsync(UserName);
if (activeTill == null)
{
    // Show OpenTillPrompt component with shop selection
    m_showOpenTillPrompt = true;
    return;
}
m_tillSession = activeTill;
m_shopId = activeTill.ShopId; // Derive shop from till
```

**Step 2: Create Rental with Till Reference**
```csharp
var request = new CheckInRequest
{
    ShopId = m_tillSession.ShopId,        // From till
    TillSessionId = m_tillSession.TillSessionId, // NEW: Link rental to till
    RenterId = ...,
    VehicleId = ...,
};
```

**Step 3: Record Payment to Till**
```csharp
// Cash payment
await TillService.RecordCashInAsync(m_tillSession.TillSessionId,
    TillTransactionType.RentalPayment, amount,
    $"Check-in #{rentalId}", paymentId: paymentId, rentalId: rentalId);

// Non-cash (tracking only)
await TillService.RecordCardPaymentAsync(m_tillSession.TillSessionId,
    amount, $"Card payment #{rentalId}",
    paymentId: paymentId, rentalId: rentalId);
```

### Check-Out Flow (CheckOutDialog.razor)

**Step 1: Get Till Context**
```csharp
// Get user's active till (may be different shop for pooled vehicles)
var activeTill = await TillService.GetActiveSessionForUserAsync(UserName);
if (activeTill == null)
{
    // For check-out, show warning but allow if rental has associated till
    ShowWarning("No active till - cash refunds unavailable");
}
```

**Step 2: Record Refund to Till**
```csharp
if (refundMethod == "Cash" && activeTill != null)
{
    await TillService.RecordPayoutAsync(activeTill.TillSessionId,
        TillTransactionType.DepositRefund, refundAmount,
        $"Deposit refund #{rentalId}", depositId: depositId, rentalId: rentalId);
}
```

### Header Till Button (MainLayout.razor)

**Replace shop selector with:**
```razor
<TillStatusButton />
```

**TillStatusButton behavior:**
- Loads user's active till on render
- If till exists: Shows "📦 {ShopName} | ฿{ExpectedCash}" → links to /staff/till
- If no till: Shows "🔓 Open Till" button → opens OpenTillPrompt modal

---

## 7. Files to Modify

### Core Changes (Already Implemented)
| File | Change |
|------|--------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType for TillSession, TillTransaction |
| `src/MotoRent.Server/Program.cs` | Register TillService and repositories |
| `src/MotoRent.Client/Layout/NavMenu.razor` | Add Till menu item under Staff section, EOD under Manager |

### Phase 2: Till-Centric Architecture
| File | Change |
|------|--------|
| `src/MotoRent.Client/Layout/MainLayout.razor` | **Remove shop selector**, add TillStatusButton component |
| `src/MotoRent.Domain/Entities/Rental.cs` | Add `TillSessionId` (nullable int) property |
| `database/tables/MotoRent.Rental.sql` | Add computed column for TillSessionId |
| `src/MotoRent.Services/TillService.cs` | Add `GetActiveSessionForUserAsync`, `CanOpenSessionAsync` |
| `src/MotoRent.Client/Pages/Staff/TillOpenSessionDialog.razor` | Add shop picker dropdown |
| `src/MotoRent.Client/Pages/Rentals/CheckIn.razor` | Replace shop check with till check, show open-till prompt if needed |
| `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` | Use till context for refunds |
| `src/MotoRent.Client/Components/Shared/TillStatusButton.razor` | **New**: Header till status/button |
| `src/MotoRent.Client/Components/Shared/OpenTillPrompt.razor` | **New**: Inline prompt for opening till |

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

### Phase 1 - Staff Workflow (DONE)
1. ✅ **Open Session**: Staff can open till with float amount
2. ✅ **Record Payout**: Can record fuel/agent/petty cash payouts with receipts
3. ✅ **Cash Drop/TopUp**: Can move cash to/from safe
4. ✅ **Reconciliation**: Can close shift with actual count, variance tracked
5. ✅ **History**: Can view past sessions and transactions

### Phase 2 - Till-Centric Architecture

**Test Setup:**
```bash
# Start watch mode
dotnet watch --project src/MotoRent.Server

# Browser: Navigate to http://localhost:5092
# Impersonate: admin@krabirentals.com
```

**Verification Steps:**
1. **Shop Selection**: Staff selects shop when opening till (not header dropdown)
2. **One Till Constraint**: Cannot open second till for same shop same day
3. **Header Button**: Shows "Till: {ShopName}" when open, "Open Till" when not
4. **All Payments Recorded**: Cash, Card, PromptPay, BankTransfer all appear in till history
5. **Cash vs Non-Cash**: Only cash transactions affect drawer balance (AffectsCash=true)
6. **Rental-Till Link**: Rentals have TillSessionId, ShopId derived from till
7. **Check-In Flow**: Must have active till to process check-in
8. **Check-Out Flow**: Deposit refunds recorded to active till

### Manager Workflow (Future)
1. **EOD View**: Manager can see all sessions for a date
2. **Summary**: Daily totals for cash, card, drops displayed correctly
3. **Verify Session**: Can verify individual sessions with notes
4. **Variance Tracking**: Sessions with variance clearly flagged
5. **Drop Verification**: Cash drops can be verified against safe count
6. **Card Reconciliation**: Card payment totals match POS/terminal

### Technical
1. **Build**: `dotnet build` succeeds
2. **Code Standards**: All files use m_ prefix for private fields
3. **Localization**: All UI text uses Localizer[], .th.resx and .en.resx present

---

## 11. Security Considerations

- Only staff assigned to a shop can open sessions for that shop
- Only managers (ShopManager, OrgAdmin) can access EOD verification
- Sessions can only be closed by the staff who opened them
- Verified sessions cannot be modified
- All transactions are audited with timestamps and usernames

---

## 12. Receipt System (DONE)

### Overview
Comprehensive Receipt entity for consolidating all charges into printable documents for each transaction type.

### Receipt Types
1. **Booking Deposit** - When customer pays deposit for a booking
2. **Check-In** - Combined receipt for rental + deposit + insurance + accessories
3. **Settlement** - Check-out receipt showing deposit, deductions, refund

### Entities Created
| File | Purpose |
|------|---------|
| `src/MotoRent.Domain/Entities/Receipt.cs` | Main receipt entity with customer/shop info |
| `src/MotoRent.Domain/Entities/ReceiptItem.cs` | Line items (rental, insurance, accessories, etc.) |
| `src/MotoRent.Domain/Entities/ReceiptPayment.cs` | Payment records with multi-currency support |
| `src/MotoRent.Domain/Entities/ReceiptStatus.cs` | Status/type constants |

### Key Features
- **Split Payments**: Multiple payment methods per receipt (cash + card, etc.)
- **Multi-Currency**: Cash payments in THB, USD, EUR, GBP, CNY, JPY, AUD, RUB with exchange rates
- **A4 Print Format**: Print-optimized layout via `ReceiptDocument.razor`
- **Reprint Tracking**: Count of reprints tracked per receipt
- **Void Capability**: Receipts can be voided with reason tracking
- **Receipt Number Format**: `RCP-YYMMDD-XXXXX` (e.g., RCP-260117-00042)

### Files Created
| File | Purpose |
|------|---------|
| `database/tables/MotoRent.Receipt.sql` | SQL schema with computed columns |
| `src/MotoRent.Services/ReceiptService.cs` | Business logic + `ReceiptAccessoryInfo` DTO |
| `src/MotoRent.Client/Components/Receipts/ReceiptDocument.razor` | A4 print layout |
| `src/MotoRent.Client/Components/Receipts/ReceiptPrintDialog.razor` | Print dialog |
| `src/MotoRent.Client/Pages/Finance/Receipts.razor` | Receipt history page |

### Integration Points
- **CheckIn.razor**: Generates check-in receipt after successful rental creation
- **CheckOutDialog.razor**: Generates settlement receipt after check-out

### ReceiptService Methods
```csharp
// Generate receipts
Task<Receipt> GenerateCheckInReceiptAsync(rentalId, tillSessionId, rental, renter, vehicle, deposit, insurance, accessories, payments, username);
Task<Receipt> GenerateSettlementReceiptAsync(rentalId, tillSessionId, rental, renter, vehicle, depositHeld, extraDaysCharge, extraDays, damageCharge, damages, locationFee, locationName, refundAmount, amountDue, payments, username);
Task<Receipt> GenerateBookingDepositReceiptAsync(bookingId, tillSessionId, booking, renter, payments, username);

// CRUD
Task<Receipt?> GetByIdAsync(receiptId);
Task<Receipt?> GetByReceiptNoAsync(receiptNo);
Task<LoadOperation<Receipt>> GetReceiptsAsync(shopId, filters...);

// Actions
Task<SubmitOperation> VoidReceiptAsync(receiptId, reason, username);
Task<SubmitOperation> RecordReprintAsync(receiptId, username);
```

### Verification Checklist
1. ✅ **Check-In Receipt**: Shows rental, deposit, insurance, accessories line items
2. ✅ **Settlement Receipt**: Shows deposit held, deductions, refund amount
3. ✅ **Print Dialog**: Opens with A4 layout, Print button works
4. ✅ **Receipt History**: `/finance/receipts` shows all receipts with filters
5. ✅ **Void Receipt**: Can void with reason, status changes to Voided
6. ✅ **Multi-Currency**: Payment records track currency and exchange rate
7. ✅ **Localization**: English and Thai resource files created
