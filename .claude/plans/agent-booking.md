# MotoRent Agent Booking Module - Implementation Plan

## Overview
Extend the booking system to support agents (tour guides, hotels, travel agencies) who make bookings on behalf of their customers. Track commissions, handle surcharges, and allow agents to manage their own invoicing while customers experience a seamless check-in/out process.

## Requirements Summary

Based on clarification:
- **Commission Timing**: Paid on rental completed (not on booking confirmation)
- **Payment Flow**: Both options - customer pays shop OR customer pays agent
- **Agent Portal**: No - staff managed, agents receive reports/payments
- **Cancellation**: Commission voided if booking cancelled

Additional requirements:
- **Agent Types**: Tour guides, hotels, travel agencies, concierges
- **Commission**: Agents earn commissions on referrals (percentage or fixed)
- **Surcharge**: Optional markup that can be hidden from end customers
- **Agent Invoicing**: Agents may produce receipts/invoices for their customers
- **Customer Experience**: Normal check-in/out, unaware of agent involvement

---

## Phase 1: Domain Layer - Agent Entities

### 1.1 AgentType Constants
**File**: `src/MotoRent.Domain/Entities/AgentType.cs`

```csharp
public static class AgentType
{
    public const string TourGuide = "TourGuide";
    public const string Hotel = "Hotel";
    public const string TravelAgency = "TravelAgency";
    public const string Concierge = "Concierge";
    public const string Resort = "Resort";
    public const string TransportCompany = "TransportCompany";
    public const string Other = "Other";

    public static readonly string[] All = [TourGuide, Hotel, TravelAgency, Concierge, Resort, TransportCompany, Other];
}
```

### 1.2 AgentStatus Constants
**File**: `src/MotoRent.Domain/Entities/AgentStatus.cs`

```csharp
public static class AgentStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Suspended = "Suspended";

    public static readonly string[] All = [Active, Inactive, Suspended];
}
```

### 1.3 AgentCommissionStatus Constants
**File**: `src/MotoRent.Domain/Entities/AgentCommissionStatus.cs`

```csharp
public static class AgentCommissionStatus
{
    public const string Pending = "Pending";       // Rental completed, awaiting approval
    public const string Approved = "Approved";     // Approved for payment
    public const string Paid = "Paid";             // Commission paid to agent
    public const string Voided = "Voided";         // Booking cancelled, commission voided

    public static readonly string[] All = [Pending, Approved, Paid, Voided];
}
```

### 1.4 AgentCommissionType Constants
**File**: `src/MotoRent.Domain/Entities/AgentCommissionType.cs`

```csharp
public static class AgentCommissionType
{
    public const string Percentage = "Percentage";           // % of booking total
    public const string FixedPerBooking = "FixedPerBooking"; // Fixed amount per booking
    public const string FixedPerVehicle = "FixedPerVehicle"; // Fixed per vehicle
    public const string FixedPerDay = "FixedPerDay";         // Fixed per rental day

    public static readonly string[] All = [Percentage, FixedPerBooking, FixedPerVehicle, FixedPerDay];
}
```

### 1.5 Agent Entity
**File**: `src/MotoRent.Domain/Entities/Agent.cs`

```csharp
public class Agent : Entity
{
    public int AgentId { get; set; }
    public string AgentCode { get; set; } = string.Empty;     // "TG-001", "HTL-PATONG"
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Type & Status
    public string AgentType { get; set; } = Entities.AgentType.Other;
    public string Status { get; set; } = AgentStatus.Active;

    // Commission Settings
    public string CommissionType { get; set; } = AgentCommissionType.Percentage;
    public decimal CommissionRate { get; set; }
    public decimal? MinCommission { get; set; }
    public decimal? MaxCommission { get; set; }

    // Surcharge Settings
    public bool AllowSurcharge { get; set; }
    public string? SurchargeType { get; set; }              // Same options as CommissionType
    public decimal? DefaultSurchargeRate { get; set; }
    public bool SurchargeHiddenFromCustomer { get; set; }   // If true, customer sees total only

    // Invoicing
    public bool CanGenerateInvoice { get; set; }
    public string? InvoicePrefix { get; set; }              // "HTL-INV-"
    public string? InvoiceNotes { get; set; }

    // Payment Details
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? BankAccountName { get; set; }
    public string? PromptPayId { get; set; }

    // Statistics (denormalized)
    public int TotalBookings { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal TotalCommissionPaid { get; set; }
    public decimal CommissionBalance { get; set; }          // Earned - Paid

    // Metadata
    public string? Notes { get; set; }
    public DateTimeOffset? LastBookingDate { get; set; }

    public override int GetId() => AgentId;
    public override void SetId(int value) => AgentId = value;
}
```

### 1.6 AgentCommission Entity
**File**: `src/MotoRent.Domain/Entities/AgentCommission.cs`

```csharp
public class AgentCommission : Entity
{
    public int AgentCommissionId { get; set; }
    public int AgentId { get; set; }
    public int BookingId { get; set; }
    public int? RentalId { get; set; }                       // Set when rental completes

    // Calculation
    public string CalculationType { get; set; } = string.Empty;
    public decimal BookingTotal { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }

    // Status (commission only eligible after rental completed)
    public string Status { get; set; } = AgentCommissionStatus.Pending;
    public DateTimeOffset? EligibleDate { get; set; }        // When rental completed
    public DateTimeOffset? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? PaidDate { get; set; }
    public string? PaidBy { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Denormalized
    public string? AgentName { get; set; }
    public string? AgentCode { get; set; }
    public string? BookingRef { get; set; }
    public string? CustomerName { get; set; }

    public override int GetId() => AgentCommissionId;
    public override void SetId(int value) => AgentCommissionId = value;
}
```

### 1.7 AgentInvoice Entity
**File**: `src/MotoRent.Domain/Entities/AgentInvoice.cs`

```csharp
public class AgentInvoice : Entity
{
    public int AgentInvoiceId { get; set; }
    public int AgentId { get; set; }
    public int BookingId { get; set; }

    // Invoice Details
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    // Customer (as billed by agent)
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }                    // Base rental amount
    public decimal SurchargeAmount { get; set; }             // Agent's markup
    public decimal TotalAmount { get; set; }                 // What customer pays agent
    public string Currency { get; set; } = "THB";

    // Payment Status
    public string PaymentStatus { get; set; } = BookingPaymentStatus.Unpaid;
    public decimal AmountPaid { get; set; }

    // Content
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public List<AgentInvoiceItem> Items { get; set; } = [];

    // Denormalized
    public string? AgentName { get; set; }
    public string? BookingRef { get; set; }

    public override int GetId() => AgentInvoiceId;
    public override void SetId(int value) => AgentInvoiceId = value;
}

public class AgentInvoiceItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
```

### 1.8 Update Booking Entity
**File**: `src/MotoRent.Domain/Entities/Booking.cs`

Add fields:
```csharp
// Agent Booking
public int? AgentId { get; set; }
public string? AgentCode { get; set; }                      // Denormalized
public string? AgentName { get; set; }                      // Denormalized
public bool IsAgentBooking { get; set; }

// Agent Financials
public decimal AgentCommission { get; set; }                // Calculated commission amount
public decimal AgentSurcharge { get; set; }                 // Surcharge amount
public bool SurchargeHidden { get; set; }                   // Is surcharge hidden from customer?

// Payment Flow
public string PaymentFlow { get; set; } = "CustomerPaysShop"; // "CustomerPaysShop" or "CustomerPaysAgent"

// What customer sees vs actual
public decimal CustomerVisibleTotal { get; set; }           // Total shown to customer
public decimal ShopReceivableAmount { get; set; }           // What shop receives after commission
```

### 1.9 PaymentFlow Constants
**File**: `src/MotoRent.Domain/Entities/PaymentFlow.cs`

```csharp
public static class PaymentFlow
{
    public const string CustomerPaysShop = "CustomerPaysShop";   // Shop collects, pays agent commission
    public const string CustomerPaysAgent = "CustomerPaysAgent"; // Agent collects, settles with shop

    public static readonly string[] All = [CustomerPaysShop, CustomerPaysAgent];
}
```

---

## Phase 2: Database Tables

### 2.1 MotoRent.Agent.sql
```sql
CREATE TABLE [MotoRent].[Agent]
(
    [AgentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AgentCode] AS CAST(JSON_VALUE([Json], '$.AgentCode') AS NVARCHAR(50)) PERSISTED,
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [AgentType] AS CAST(JSON_VALUE([Json], '$.AgentType') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(50)),
    [CommissionBalance] AS CAST(JSON_VALUE([Json], '$.CommissionBalance') AS DECIMAL(18,2)),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE UNIQUE INDEX IX_Agent_AgentCode ON [MotoRent].[Agent]([AgentCode])
CREATE INDEX IX_Agent_Status ON [MotoRent].[Agent]([Status])
CREATE INDEX IX_Agent_AgentType ON [MotoRent].[Agent]([AgentType])
```

### 2.2 MotoRent.AgentCommission.sql
```sql
CREATE TABLE [MotoRent].[AgentCommission]
(
    [AgentCommissionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AgentId] AS CAST(JSON_VALUE([Json], '$.AgentId') AS INT),
    [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(50)),
    [CommissionAmount] AS CAST(JSON_VALUE([Json], '$.CommissionAmount') AS DECIMAL(18,2)),
    [EligibleDate] AS CAST(JSON_VALUE([Json], '$.EligibleDate') AS DATETIMEOFFSET),
    [PaidDate] AS CAST(JSON_VALUE([Json], '$.PaidDate') AS DATETIMEOFFSET),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_AgentCommission_AgentId ON [MotoRent].[AgentCommission]([AgentId])
CREATE INDEX IX_AgentCommission_BookingId ON [MotoRent].[AgentCommission]([BookingId])
CREATE INDEX IX_AgentCommission_Status ON [MotoRent].[AgentCommission]([Status])
CREATE INDEX IX_AgentCommission_AgentId_Status ON [MotoRent].[AgentCommission]([AgentId], [Status])
```

### 2.3 MotoRent.AgentInvoice.sql
```sql
CREATE TABLE [MotoRent].[AgentInvoice]
(
    [AgentInvoiceId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AgentId] AS CAST(JSON_VALUE([Json], '$.AgentId') AS INT),
    [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
    [InvoiceNo] AS CAST(JSON_VALUE([Json], '$.InvoiceNo') AS NVARCHAR(50)),
    [PaymentStatus] AS CAST(JSON_VALUE([Json], '$.PaymentStatus') AS NVARCHAR(50)),
    [TotalAmount] AS CAST(JSON_VALUE([Json], '$.TotalAmount') AS DECIMAL(18,2)),
    [InvoiceDate] AS CAST(JSON_VALUE([Json], '$.InvoiceDate') AS DATETIMEOFFSET),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_AgentInvoice_AgentId ON [MotoRent].[AgentInvoice]([AgentId])
CREATE INDEX IX_AgentInvoice_BookingId ON [MotoRent].[AgentInvoice]([BookingId])
```

---

## Phase 3: Services

### 3.1 AgentService
**File**: `src/MotoRent.Services/AgentService.cs`

```csharp
public class AgentService
{
    private readonly RentalDataContext m_context;

    // CRUD
    Task<Agent?> GetAgentByIdAsync(int agentId);
    Task<Agent?> GetAgentByCodeAsync(string agentCode);
    Task<List<Agent>> GetAgentsAsync(string? status, string? agentType, string? search);
    Task<Agent> CreateAgentAsync(Agent agent, string username);
    Task<Agent> UpdateAgentAsync(Agent agent, string username);
    Task<string> GenerateAgentCodeAsync(string agentType);  // "TG-001", "HTL-001"

    // Commission Calculation
    Task<decimal> CalculateCommissionAsync(Agent agent, Booking booking);
    Task<decimal> CalculateSurchargeAsync(Agent agent, Booking booking, decimal? customRate);

    // Statistics
    Task<AgentStatistics> GetAgentStatisticsAsync(int agentId, DateTimeOffset? from, DateTimeOffset? to);
    Task UpdateAgentStatisticsAsync(int agentId);
}
```

### 3.2 AgentCommissionService
**File**: `src/MotoRent.Services/AgentCommissionService.cs`

```csharp
public class AgentCommissionService
{
    private readonly RentalDataContext m_context;
    private readonly AgentService m_agentService;

    // Create commission record (called when booking created, but Pending status)
    Task<AgentCommission> CreateCommissionAsync(int agentId, int bookingId, decimal amount, string username);

    // List commissions
    Task<List<AgentCommission>> GetCommissionsAsync(int? agentId, string? status, DateTimeOffset? from, DateTimeOffset? to);
    Task<AgentCommission?> GetCommissionByBookingAsync(int bookingId);

    // Workflow - commission becomes eligible only after rental completed
    Task MakeEligibleAsync(int commissionId, int rentalId, string username);  // Called when rental completed
    Task ApproveCommissionAsync(int commissionId, string username);
    Task ApproveCommissionsAsync(int[] commissionIds, string username);
    Task PayCommissionAsync(int commissionId, string paymentMethod, string? reference, string username);
    Task PayCommissionsAsync(int[] commissionIds, string paymentMethod, string? reference, string username);
    Task VoidCommissionAsync(int commissionId, string reason, string username);  // Called when booking cancelled

    // Reporting
    Task<CommissionSummary> GetCommissionSummaryAsync(int? agentId, DateTimeOffset? from, DateTimeOffset? to);
}
```

### 3.3 AgentInvoiceService
**File**: `src/MotoRent.Services/AgentInvoiceService.cs`

```csharp
public class AgentInvoiceService
{
    private readonly RentalDataContext m_context;

    // CRUD
    Task<AgentInvoice> CreateInvoiceAsync(int agentId, int bookingId, AgentInvoiceRequest request, string username);
    Task<AgentInvoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<AgentInvoice?> GetInvoiceByBookingAsync(int bookingId);
    Task<List<AgentInvoice>> GetInvoicesAsync(int? agentId, string? status);

    // Invoice Generation
    Task<string> GenerateInvoiceNoAsync(Agent agent);
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);

    // Payment
    Task RecordPaymentAsync(int invoiceId, decimal amount, string username);
}
```

### 3.4 Update BookingService
**File**: `src/MotoRent.Services/BookingService.cs`

Add methods:
```csharp
// Agent Booking
Task<Booking> CreateAgentBookingAsync(CreateAgentBookingRequest request, string username);
Task<List<Booking>> GetAgentBookingsAsync(int agentId, string? status, DateTimeOffset? from, DateTimeOffset? to);

// Helpers
Task ApplyAgentToBookingAsync(int bookingId, int agentId, decimal? customSurcharge, string paymentFlow, string username);
Task RemoveAgentFromBookingAsync(int bookingId, string username);
```

### 3.5 Update RentalService
**File**: `src/MotoRent.Services/RentalService.cs`

Modify checkout flow:
```csharp
// When rental completed, make commission eligible
// In CompleteRentalAsync:
if (rental.BookingId.HasValue)
{
    var booking = await m_bookingService.GetBookingByIdAsync(rental.BookingId.Value);
    if (booking?.IsAgentBooking == true)
    {
        var commission = await m_commissionService.GetCommissionByBookingAsync(booking.BookingId);
        if (commission != null)
        {
            await m_commissionService.MakeEligibleAsync(commission.AgentCommissionId, rental.RentalId, username);
        }
    }
}
```

---

## Phase 4: UI - Agent Management

### 4.1 AgentList.razor
**File**: `src/MotoRent.Client/Pages/Agents/AgentList.razor`
**Route**: `/agents`

Layout (2-column: col-3 filters + col-9 table):
- Filters: Agent type, Status, Search
- Table: Code, Name, Type, Commission Rate, Balance, Status, Actions
- Header action: "Add Agent" button

### 4.2 AgentDialog.razor
**File**: `src/MotoRent.Client/Pages/Agents/AgentDialog.razor`

Create/Edit dialog with tabs:
- **General**: Code, Name, Contact, Type, Status
- **Commission**: Type, Rate, Min/Max
- **Surcharge**: Enable, Type, Rate, Hidden from customer
- **Invoicing**: Enable, Prefix, Notes
- **Payment**: Bank details, PromptPay

### 4.3 AgentDetails.razor
**File**: `src/MotoRent.Client/Pages/Agents/AgentDetails.razor`
**Route**: `/agents/{AgentId:int}`

Layout:
- Left (col-4): Agent info card, Statistics card
- Right (col-8): Tabs
  - **Bookings**: Agent's bookings
  - **Commissions**: Commission history
  - **Invoices**: Agent-generated invoices

### 4.4 AgentCommissions.razor (Finance page)
**File**: `src/MotoRent.Client/Pages/Finance/AgentCommissions.razor`
**Route**: `/finance/agent-commissions`

- View pending/approved commissions across all agents
- Bulk approve and pay
- Filter by agent, status, date range

---

## Phase 5: UI - Booking Integration

### 5.1 Update CreateBooking.razor
Add "Agent Booking" section:
- Toggle: "This is an agent booking"
- Agent selector (search by code or name)
- Payment flow dropdown
- Auto-calculate commission and surcharge
- Option to customize surcharge
- Show breakdown: Base Total + Surcharge = Customer Pays

### 5.2 Update BookingDetails.razor
Add Agent section (if agent booking):
- Agent info card with link
- Commission status and amount
- Surcharge breakdown
- Payment flow indicator
- Generate Invoice button (if agent can invoice)

---

## Phase 6: Customer Experience

### 6.1 Hidden Surcharge Handling
When `SurchargeHidden = true`:
- Customer sees `CustomerVisibleTotal` (includes hidden surcharge)
- Receipt shows single total, no surcharge line
- MyBooking page shows unified price

When `SurchargeHidden = false`:
- Customer sees base price + service fee
- Labeled as "Booking Service Fee" or "Concierge Fee"

### 6.2 Check-in Flow
- Staff sees agent info in booking
- Commission record created when booking confirmed
- Check-in proceeds normally
- No additional steps for customer

### 6.3 Check-out Flow
- When rental completed, commission becomes eligible (Pending status)
- Staff can approve and pay commissions from Finance page

---

## Phase 7: Reporting

### 7.1 Agent Performance Report
- Bookings by agent
- Revenue generated
- Commission earned vs paid
- Average booking value

### 7.2 Commission Payout Report
- Outstanding commissions by agent
- Payment history
- Export for accounting

---

## Data Flow Examples

### Example 1: Customer Pays Shop (Hidden Surcharge)
```
Hotel books for guest, surcharge hidden:

Rental Base: ฿3,000
Agent Commission (10%): ฿300 (paid after rental completed)
Agent Surcharge (15%): ฿450 (hidden)

Customer Sees: ฿3,450 total (no breakdown shown)
Customer Pays Shop: ฿3,450
Shop Keeps: ฿3,150 (after paying ฿300 commission)
Agent Gets: ฿300 commission
```

### Example 2: Customer Pays Agent
```
Tour guide books, customer pays agent:

Rental Base: ฿2,000
Agent Commission (฿200 fixed): ฿200
Agent Markup: ฿500 (their own)

Customer Pays Agent: ฿2,500
Agent Pays Shop: ฿2,000 (or shop invoices agent)
Shop Pays Commission: ฿200 (after rental completed)
Agent Total Profit: ฿500 markup + ฿200 commission = ฿700
```

### Example 3: Booking Cancelled
```
Agent books, customer cancels:

Commission was calculated: ฿300
Booking cancelled before check-in
Commission status: Voided
Agent receives: ฿0
```

---

## Implementation Order

1. **Phase 1**: Domain entities and constants
2. **Phase 2**: Database tables
3. **Phase 3**: Services (AgentService, AgentCommissionService, AgentInvoiceService)
4. **Phase 4**: Agent management UI
5. **Phase 5**: Booking integration
6. **Phase 6**: Customer-facing adjustments
7. **Phase 7**: Reporting

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/MotoRent.Domain/Entities/Agent.cs` | Agent entity |
| `src/MotoRent.Domain/Entities/AgentType.cs` | Agent type constants |
| `src/MotoRent.Domain/Entities/AgentStatus.cs` | Agent status constants |
| `src/MotoRent.Domain/Entities/AgentCommission.cs` | Commission tracking |
| `src/MotoRent.Domain/Entities/AgentCommissionStatus.cs` | Commission status constants |
| `src/MotoRent.Domain/Entities/AgentCommissionType.cs` | Commission type constants |
| `src/MotoRent.Domain/Entities/AgentInvoice.cs` | Agent invoices |
| `src/MotoRent.Domain/Entities/PaymentFlow.cs` | Payment flow constants |
| `database/tables/MotoRent.Agent.sql` | Agent table |
| `database/tables/MotoRent.AgentCommission.sql` | Commission table |
| `database/tables/MotoRent.AgentInvoice.sql` | Invoice table |
| `src/MotoRent.Services/AgentService.cs` | Agent CRUD + calculations |
| `src/MotoRent.Services/AgentCommissionService.cs` | Commission workflow |
| `src/MotoRent.Services/AgentInvoiceService.cs` | Invoice management |
| `src/MotoRent.Client/Pages/Agents/AgentList.razor` | Agent list page |
| `src/MotoRent.Client/Pages/Agents/AgentDetails.razor` | Agent detail page |
| `src/MotoRent.Client/Pages/Agents/AgentDialog.razor` | Create/edit dialog |
| `src/MotoRent.Client/Pages/Finance/AgentCommissions.razor` | Commission management |

## Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Entities/Booking.cs` | Add agent fields |
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType attributes |
| `src/MotoRent.Services/BookingService.cs` | Add agent booking methods |
| `src/MotoRent.Services/RentalService.cs` | Make commission eligible on completion |
| `src/MotoRent.Server/Program.cs` | Register new services |
| `src/MotoRent.Client/Shared/NavMenu.razor` | Add Agents menu item |
| `src/MotoRent.Client/Pages/Bookings/CreateBooking.razor` | Add agent booking option |
| `src/MotoRent.Client/Pages/Bookings/BookingDetails.razor` | Show agent info |

---

## Key Design Decisions

1. **Commission on Completion**: Commission only becomes eligible after rental is completed, not when booking is confirmed. This protects against cancellations.

2. **Commission Voided on Cancel**: If booking is cancelled at any point, the commission is voided. No partial commission.

3. **Dual Payment Flow**: Support both customer-pays-shop and customer-pays-agent models to accommodate different agent agreements.

4. **Staff Managed**: No agent portal - staff handles all commission approvals and payments. Agents receive reports.

5. **Hidden Surcharge Option**: Surcharge can be hidden from customer receipt, showing only a total amount.
