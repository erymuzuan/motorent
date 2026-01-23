# Architecture Patterns: Cashier Till System

**Domain:** Point-of-sale cashier till for multi-tenant motorbike rental
**Researched:** 2026-01-19
**Context:** Existing MotoRent system with Blazor Server + SQL Server JSON columns

## Executive Summary

The MotoRent system already has a well-architected cashier till implementation that follows established patterns. This document analyzes the existing architecture, identifies its strengths, and recommends enhancements based on the current state of the codebase.

The architecture follows a **session-based till model** where staff members open individual sessions tied to shops, record transactions (cash in/out), and close with reconciliation. The implementation integrates cleanly with the existing rental, deposit, and receipt workflows.

---

## Existing Architecture Overview

### Entity Model

The till system uses three primary entities:

```
TillSession (1) -----> (*) TillTransaction
     |
     v
     (*) Receipt
```

| Entity | Purpose | Key Relationships |
|--------|---------|-------------------|
| `TillSession` | Represents a staff member's shift at a shop | Links to Shop, Staff, Transactions |
| `TillTransaction` | Individual cash/non-cash movements | Links to Session, Payment, Deposit, Rental |
| `Receipt` | Formal document for customer transactions | Links to Session, Rental, Booking, Renter |

### Current Entity Structure

#### TillSession

```csharp
public class TillSession : Entity
{
    public int TillSessionId { get; set; }
    public int ShopId { get; set; }
    public string StaffUserName { get; set; }
    public string StaffDisplayName { get; set; }
    public TillSessionStatus Status { get; set; }

    // Opening
    public decimal OpeningFloat { get; set; }
    public DateTimeOffset OpenedAt { get; set; }

    // Running totals (denormalized for quick display)
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalToppedUp { get; set; }

    // Non-cash totals
    public decimal TotalCardPayments { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal TotalPromptPay { get; set; }

    // Closing & verification
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? VerifiedByUserName { get; set; }

    // Computed
    public decimal ExpectedCash => OpeningFloat + TotalCashIn - TotalCashOut - TotalDropped + TotalToppedUp;
}
```

**Design Rationale:**
- **Denormalized totals** on session enable fast UI rendering without aggregating transactions
- **Expected vs Actual** supports end-of-day variance tracking
- **Status workflow** enables manager verification process

#### TillTransaction

```csharp
public class TillTransaction : Entity
{
    public int TillTransactionId { get; set; }
    public int TillSessionId { get; set; }

    public TillTransactionType TransactionType { get; set; }
    public TillTransactionDirection Direction { get; set; }

    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }

    // Cross-references
    public int? PaymentId { get; set; }
    public int? DepositId { get; set; }
    public int? RentalId { get; set; }

    // Payout details
    public string? RecipientName { get; set; }
    public string? ReceiptNumber { get; set; }

    // Attachments (photos of receipts)
    public List<TillAttachment> Attachments { get; set; }

    // Computed
    public bool AffectsCash => TransactionType switch
    {
        TillTransactionType.CardPayment => false,
        TillTransactionType.BankTransfer => false,
        TillTransactionType.PromptPay => false,
        _ => true
    };
}
```

**Design Rationale:**
- **Optional cross-references** link till transactions back to business entities
- **AffectsCash computed property** distinguishes cash vs electronic payments
- **Attachment support** for petty cash receipt photos

#### Receipt

```csharp
public class Receipt : Entity
{
    public int ReceiptId { get; set; }
    public string ReceiptNo { get; set; }  // RCP-YYMMDD-XXXXX
    public string ReceiptType { get; set; } // BookingDeposit, CheckIn, Settlement
    public string Status { get; set; }      // Issued, Voided

    // References
    public int? BookingId { get; set; }
    public int? RentalId { get; set; }
    public int? TillSessionId { get; set; }
    public int ShopId { get; set; }

    // Denormalized customer info
    public string CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    // Line items and payments
    public List<ReceiptItem> Items { get; set; }
    public List<ReceiptPayment> Payments { get; set; }

    // Totals
    public decimal Subtotal { get; set; }
    public decimal GrandTotal { get; set; }
}
```

**Design Rationale:**
- **Denormalized customer/shop info** enables printing without joins
- **Embedded Items/Payments** stored in JSON column for flexibility
- **Multi-currency support** via `ReceiptPayment.Currency` and `ExchangeRate`

---

## Component Boundaries

### Service Layer

```
TillService
    - Session lifecycle (Open, Close, Verify)
    - Transaction recording (CashIn, CashOut, Drop, TopUp)
    - Integration methods (link to rental payments)
    - EOD reports and summaries

ReceiptService
    - Receipt generation (CheckIn, Settlement, BookingDeposit)
    - Receipt queries and filtering
    - Void/reprint operations
    - Statistics

ShopService
    - Shop context for till operations
```

### Current TillService Responsibilities

```csharp
public class TillService(RentalDataContext context)
{
    // Session Management
    OpenSessionAsync(shopId, staffUserName, openingFloat)
    GetActiveSessionAsync(shopId, staffUserName)
    GetActiveSessionForUserAsync(staffUserName)
    CanOpenSessionAsync(shopId, staffUserName)
    CloseSessionAsync(sessionId, actualCash, notes)

    // Transaction Recording
    RecordCashInAsync(sessionId, type, amount, description, ...)
    RecordPayoutAsync(sessionId, type, amount, description, ...)
    RecordDropAsync(sessionId, amount)
    RecordTopUpAsync(sessionId, amount)

    // Integration (link to rental workflow)
    RecordRentalPaymentToTillAsync(tillSessionId, paymentMethod, ...)
    RecordDepositToTillAsync(tillSessionId, depositId, ...)
    RecordDepositRefundFromTillAsync(shopId, staffUserName, ...)

    // Manager EOD
    GetSessionsForVerificationAsync(shopId, date)
    GetDailySummaryAsync(shopId, date)
    VerifySessionAsync(sessionId, managerUserName)
    VerifyTransactionAsync(transactionId, managerUserName)

    // Reports
    GetSessionHistoryAsync(shopId, filters)
    GetSessionsWithVarianceAsync(shopId, dateRange)
}
```

---

## Data Flow

### Flow 1: Staff Opens Till Session

```
┌─────────────────────────────────────────────────────────────────────┐
│ Till.razor → TillOpenSessionDialog                                  │
│     ↓                                                               │
│ TillService.OpenSessionAsync(shopId, staffUserName, openingFloat)   │
│     ↓                                                               │
│ Create TillSession {Status: Open, OpeningFloat: X}                  │
│     ↓                                                               │
│ PersistenceSession.SubmitChanges() → SQL INSERT                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Flow 2: Receive Rental Payment (Quick Payment)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Till.razor → TillReceivePaymentDialog                               │
│     ↓                                                               │
│ Select active rental                                                │
│ Enter amount, select payment method (Cash/Card/PromptPay)           │
│     ↓                                                               │
│ TillService.RecordRentalPaymentToTillAsync(sessionId, method, ...)  │
│     ↓                                                               │
│ Determine TransactionType from paymentMethod:                       │
│   - Cash → TillTransactionType.RentalPayment (AffectsCash = true)   │
│   - Card → TillTransactionType.CardPayment (AffectsCash = false)    │
│   - PromptPay → TillTransactionType.PromptPay (AffectsCash = false) │
│     ↓                                                               │
│ Create TillTransaction                                              │
│ Update TillSession totals (TotalCashIn or TotalCardPayments, etc.)  │
│     ↓                                                               │
│ Generate Receipt if applicable                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Flow 3: Close Till Session (EOD)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Till.razor → TillCloseSessionDialog                                 │
│     ↓                                                               │
│ Display ExpectedCash: OpeningFloat + CashIn - CashOut - Dropped     │
│ Staff enters ActualCash (physical count)                            │
│     ↓                                                               │
│ TillService.CloseSessionAsync(sessionId, actualCash, notes)         │
│     ↓                                                               │
│ Calculate Variance = ActualCash - ExpectedCash                      │
│ Set Status:                                                         │
│   - Variance == 0 → Closed                                          │
│   - Variance != 0 → ClosedWithVariance                              │
│     ↓                                                               │
│ PersistenceSession.SubmitChanges()                                  │
└─────────────────────────────────────────────────────────────────────┘
```

### Flow 4: Manager Verification

```
┌─────────────────────────────────────────────────────────────────────┐
│ Manager EOD Page (not yet implemented)                              │
│     ↓                                                               │
│ TillService.GetSessionsForVerificationAsync(shopId, date)           │
│ TillService.GetDailySummaryAsync(shopId, date)                      │
│     ↓                                                               │
│ Review sessions with variances                                      │
│ Review unverified cash drops                                        │
│     ↓                                                               │
│ TillService.VerifySessionAsync(sessionId, managerUserName)          │
│     ↓                                                               │
│ Set Status → Verified, record VerifiedByUserName, VerifiedAt        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Integration Points

### 1. Rental Check-In Integration

The existing rental check-in workflow should integrate with the till:

```
Check-In Wizard (Step 5: Payment)
    ↓
Record payment → TillService.RecordRentalPaymentToTillAsync()
Record deposit → TillService.RecordDepositToTillAsync()
    ↓
Generate receipt → ReceiptService.GenerateCheckInReceiptAsync()
    ↓
Link receipt to TillSession via Receipt.TillSessionId
```

**Current Integration Point:**
- `Rental.TillSessionId` - Links rental to the till session that processed it
- `Receipt.TillSessionId` - Links receipt to till session for reconciliation

### 2. Rental Check-Out Integration

```
Check-Out Process
    ↓
Calculate settlement (deposit - damages - late fees)
    ↓
If refund needed:
    TillService.RecordDepositRefundFromTillAsync()
    ↓
Generate settlement receipt → ReceiptService.GenerateSettlementReceiptAsync()
```

### 3. Booking Deposit Integration

```
TillBookingDepositDialog
    ↓
Select pending booking
Enter payment amount/method
    ↓
TillService.RecordCashInAsync(sessionId, TillTransactionType.BookingDeposit, ...)
    ↓
ReceiptService.GenerateBookingDepositReceiptAsync()
```

---

## Multi-Currency Support

### Current Implementation

Multi-currency is supported at the **receipt payment level**:

```csharp
public class ReceiptPayment
{
    public string Method { get; set; }        // Cash, Card, PromptPay
    public decimal Amount { get; set; }        // Amount in payment currency
    public string Currency { get; set; }       // THB, USD, EUR, etc.
    public decimal ExchangeRate { get; set; }  // Rate to THB
    public decimal AmountInBaseCurrency { get; set; } // THB equivalent
}
```

**Supported Currencies:**
- THB (base), USD, EUR, GBP, CNY, JPY, AUD, RUB

### Till Session Currency Strategy

The `TillSession` totals are in **base currency (THB)** only. Multi-currency cash receipts are converted at point of entry.

**Recommendation:** This is the correct approach for a Thai business. Foreign currency accepted as cash should be immediately converted to THB equivalent for till tracking purposes.

---

## Session Status Workflow

```
Open
  ↓ (staff counting)
Reconciling (optional intermediate state)
  ↓ (close)
Closed (variance = 0) ──or── ClosedWithVariance (variance != 0)
  ↓ (manager review)
PendingVerification (optional)
  ↓ (manager approves)
Verified
```

**Status Enum:**
```csharp
public enum TillSessionStatus
{
    Open,              // Active, accepting transactions
    Reconciling,       // Staff counting (optional)
    Closed,            // Closed with no variance
    ClosedWithVariance,// Closed but had variance
    PendingVerification, // Awaiting manager
    Verified           // Manager approved
}
```

---

## SQL Schema Pattern

Following existing MotoRent patterns with JSON columns:

```sql
CREATE TABLE [<schema>].[TillSession]
(
    [TillSessionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [StaffUserName] AS CAST(JSON_VALUE([Json], '$.StaffUserName') AS NVARCHAR(100)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(30)),
    [OpenedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.OpenedAt'), 127) PERSISTED,
    [ClosedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ClosedAt'), 127) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
```

**Key Indexes:**
- `IX_TillSession_ShopId_Status` - Active session lookups
- `IX_TillSession_StaffUserName` - User's sessions
- `IX_TillSession_OpenedAt` - Date range queries

---

## Patterns to Follow

### Pattern 1: Denormalized Totals with Transaction Source of Truth

**What:** Store running totals on session for fast reads; transactions are source of truth

**When:** High-frequency reads (till balance display), moderate-frequency writes

**Implementation:**
```csharp
// On RecordCashInAsync:
if (transaction.AffectsCash)
    session.TotalCashIn += amount;

// On session display:
public decimal ExpectedCash => OpeningFloat + TotalCashIn - TotalCashOut - TotalDropped + TotalToppedUp;
```

**Validation:** If totals get out of sync, can recalculate from transactions:
```csharp
var transactions = await GetTransactionsAsync(sessionId);
var recalculatedCashIn = transactions
    .Where(t => t.Direction == In && t.AffectsCash)
    .Sum(t => t.Amount);
```

### Pattern 2: Cross-Reference Linking

**What:** Link till transactions to source entities without tight coupling

**Implementation:**
```csharp
public class TillTransaction
{
    public int? PaymentId { get; set; }  // Optional link to Payment
    public int? DepositId { get; set; }  // Optional link to Deposit
    public int? RentalId { get; set; }   // Optional link to Rental
}
```

**Why:** Enables audit trail and reconciliation without requiring all transactions to have sources.

### Pattern 3: Embedded Collections in JSON

**What:** Store related items (ReceiptItem, ReceiptPayment, TillAttachment) as JSON arrays

**When:** Items are always loaded/saved with parent; no independent querying needed

**Implementation:**
```csharp
public class Receipt : Entity
{
    public List<ReceiptItem> Items { get; set; } = [];
    public List<ReceiptPayment> Payments { get; set; } = [];
}
```

**SQL:** Single row, JSON column contains serialized lists.

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Session Per Transaction

**What:** Creating a new TillSession for each transaction

**Why bad:** Defeats purpose of shift-based cash tracking; no meaningful EOD reconciliation

**Instead:** One session per staff per shop per shift

### Anti-Pattern 2: Real-Time Transaction Aggregation

**What:** Calculating totals by summing all transactions on every display

**Why bad:** Performance degrades as transaction count grows

**Instead:** Denormalize running totals on session; update atomically with each transaction

### Anti-Pattern 3: Tight Coupling to Payment Entity

**What:** Requiring all till transactions to have a Payment entity

**Why bad:** Petty cash, fuel reimbursements, agent commissions have no Payment entity

**Instead:** Optional references; TillTransaction stands alone when needed

---

## Scalability Considerations

| Concern | At 100 tx/day | At 1K tx/day | At 10K tx/day |
|---------|---------------|--------------|---------------|
| Session queries | No issue | No issue | Consider index on (ShopId, OpenedAt) |
| Transaction list | Load all in memory | Pagination | Pagination + lazy loading |
| Daily summary | Calculate on demand | Calculate on demand | Pre-aggregate nightly |
| Receipt search | Full-text search | Add filters | Consider separate search index |

**Current Implementation:** Suitable for small-to-medium shops (up to 1K transactions/day per shop).

---

## Gaps and Enhancement Opportunities

### Gap 1: Manager EOD Dashboard

**Status:** Service methods exist; UI not implemented

**Needed:**
- `/manager/till-verification` page
- View all sessions for a date
- Review variances
- Verify sessions and drops
- Daily summary report

### Gap 2: Multi-Currency Balance Tracking

**Current:** Till tracks THB only; foreign currency converted at receipt level

**If Needed:** Add per-currency balances to TillSession:
```csharp
public class CurrencyBalance
{
    public string Currency { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
}

public List<CurrencyBalance> CurrencyBalances { get; set; }
```

**Recommendation:** Only implement if business requirement exists. Current THB-only is simpler.

### Gap 3: Safe/Bank Deposit Tracking

**Current:** Cash drops recorded but no centralized safe tracking

**If Needed:** Add `Safe` entity to track total cash dropped from all sessions:
```csharp
public class Safe : Entity
{
    public int ShopId { get; set; }
    public decimal Balance { get; set; }
    public List<SafeTransaction> Transactions { get; set; }
}
```

### Gap 4: Historical Exchange Rates

**Current:** Exchange rate captured at payment time on ReceiptPayment

**If Needed:** Add ExchangeRate entity for historical rate lookups:
```csharp
public class ExchangeRate : Entity
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
}
```

---

## Recommended Phase Structure

Based on the existing implementation, suggested roadmap phases:

### Phase 1: Core Integration Verification
- Verify TillSession links in rental check-in/check-out flows
- Ensure all payment methods route through till
- Test variance calculation accuracy

### Phase 2: Manager EOD Dashboard
- Build verification UI
- Daily summary report
- Variance investigation workflow

### Phase 3: Reporting Enhancements
- Cash flow reports by date range
- Staff performance (variance history)
- Payment method breakdown

### Phase 4: Multi-Currency (If Required)
- Per-currency balance tracking
- Exchange rate management
- Multi-currency reconciliation

---

## Sources

- Existing codebase analysis:
  - `src/MotoRent.Domain/Entities/TillSession.cs`
  - `src/MotoRent.Domain/Entities/TillTransaction.cs`
  - `src/MotoRent.Domain/Entities/Receipt.cs`
  - `src/MotoRent.Services/TillService.cs`
  - `src/MotoRent.Services/ReceiptService.cs`
  - `src/MotoRent.Client/Pages/Staff/Till.razor`
  - `database/tables/MotoRent.TillSession.sql`
  - `database/tables/MotoRent.TillTransaction.sql`
  - `database/tables/MotoRent.Receipt.sql`

**Confidence Level:** HIGH - Based on direct analysis of existing implementation.
