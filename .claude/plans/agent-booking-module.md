# MotoRent Agent Booking Module - Implementation Plan

## Overview
Extend the booking system to support agents (tour guides, hotels, travel agencies) who make bookings on behalf of their customers. Track commissions, handle surcharges, and allow agents to manage their own invoicing while customers experience a seamless check-in/out process.

## Requirements Summary
- **Agent Types**: Tour guides, hotels, travel agencies, concierges
- **Commission**: Agents earn commissions on referrals (percentage or fixed)
- **Surcharge**: Optional markup that can be hidden from end customers
- **Agent Invoicing**: Agents may produce receipts/invoices for their customers
- **Customer Experience**: Normal check-in/out, unaware of agent involvement
- **Tracking**: Commission tracking, payout management, agent performance

---

## Phase 1: Domain Layer - Agent Entities

### 1.1 Agent Entity
**File**: `src/MotoRent.Domain/Entities/Agent.cs`

```csharp
public class Agent : Entity
{
    public int AgentId { get; set; }
    public string AgentCode { get; set; }              // Unique code (e.g., "AGT-001", "HTL-PATONG")
    public string Name { get; set; }                   // Business name
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Type & Status
    public string AgentType { get; set; }              // TourGuide, Hotel, TravelAgency, Concierge
    public string Status { get; set; }                 // Active, Inactive, Suspended

    // Commission Settings
    public string CommissionType { get; set; }         // Percentage, FixedPerBooking, FixedPerVehicle, FixedPerDay
    public decimal CommissionRate { get; set; }        // Rate value (e.g., 10 for 10%, or 200 for ฿200)
    public decimal? MinCommission { get; set; }        // Minimum commission per booking
    public decimal? MaxCommission { get; set; }        // Maximum commission cap

    // Surcharge Settings (markup agent adds to customer)
    public bool AllowSurcharge { get; set; }
    public string? SurchargeType { get; set; }         // Percentage, FixedPerBooking, FixedPerVehicle, FixedPerDay
    public decimal? DefaultSurchargeRate { get; set; }
    public bool SurchargeHiddenFromCustomer { get; set; } // If true, customer sees total only

    // Invoicing
    public bool CanGenerateInvoice { get; set; }       // Agent produces their own invoices
    public string? InvoicePrefix { get; set; }         // e.g., "HTL-INV-"
    public string? InvoiceNotes { get; set; }          // Default notes on agent invoices

    // Payment Details
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? BankAccountName { get; set; }
    public string? PromptPayId { get; set; }

    // Statistics (denormalized for quick access)
    public int TotalBookings { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal TotalCommissionPaid { get; set; }
    public decimal CommissionBalance { get; set; }     // Earned - Paid

    // Metadata
    public string? Notes { get; set; }
    public DateTimeOffset? LastBookingDate { get; set; }
}
```

### 1.2 AgentType Constants
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
}
```

### 1.3 AgentCommission Entity
**File**: `src/MotoRent.Domain/Entities/AgentCommission.cs`

```csharp
public class AgentCommission : Entity
{
    public int AgentCommissionId { get; set; }
    public int AgentId { get; set; }
    public int BookingId { get; set; }

    // Calculation
    public string CalculationType { get; set; }        // How it was calculated
    public decimal BookingTotal { get; set; }          // Original booking total
    public decimal CommissionRate { get; set; }        // Rate used
    public decimal CommissionAmount { get; set; }      // Calculated commission

    // Status
    public string Status { get; set; }                 // Pending, Approved, Paid, Cancelled
    public DateTimeOffset? ApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? PaidDate { get; set; }
    public string? PaidBy { get; set; }
    public string? PaymentReference { get; set; }      // Bank transfer ref, etc.
    public string? PaymentMethod { get; set; }         // Cash, BankTransfer, PromptPay

    // Notes
    public string? Notes { get; set; }

    // Denormalized for reporting
    public string? AgentName { get; set; }
    public string? BookingRef { get; set; }
    public string? CustomerName { get; set; }
}
```

### 1.4 AgentCommissionStatus Constants
**File**: `src/MotoRent.Domain/Entities/AgentCommissionStatus.cs`

```csharp
public static class AgentCommissionStatus
{
    public const string Pending = "Pending";           // Booking confirmed, commission calculated
    public const string Approved = "Approved";         // Approved for payment
    public const string Paid = "Paid";                 // Commission paid to agent
    public const string Cancelled = "Cancelled";       // Booking cancelled, commission voided
    public const string Disputed = "Disputed";         // Under review
}
```

### 1.5 AgentInvoice Entity
**File**: `src/MotoRent.Domain/Entities/AgentInvoice.cs`

```csharp
public class AgentInvoice : Entity
{
    public int AgentInvoiceId { get; set; }
    public int AgentId { get; set; }
    public int BookingId { get; set; }

    // Invoice Details
    public string InvoiceNo { get; set; }              // Agent's invoice number
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    // Customer (as billed by agent)
    public string CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }              // Our rental amount
    public decimal SurchargeAmount { get; set; }       // Agent's markup
    public decimal TotalAmount { get; set; }           // What customer pays agent
    public string? Currency { get; set; }

    // Payment Status
    public string PaymentStatus { get; set; }          // Unpaid, PartiallyPaid, Paid
    public decimal AmountPaid { get; set; }

    // Content
    public string? Notes { get; set; }
    public string? Terms { get; set; }

    // Denormalized
    public string? AgentName { get; set; }
    public string? BookingRef { get; set; }
}
```

### 1.6 Update Booking Entity
**File**: `src/MotoRent.Domain/Entities/Booking.cs`

Add fields:
```csharp
// Agent Booking
public int? AgentId { get; set; }
public string? AgentCode { get; set; }                 // Denormalized
public string? AgentName { get; set; }                 // Denormalized
public bool IsAgentBooking { get; set; }

// Agent Financials
public decimal AgentCommission { get; set; }           // Commission amount
public decimal AgentSurcharge { get; set; }            // Surcharge amount
public bool SurchargeHidden { get; set; }              // Is surcharge hidden from customer?

// What customer sees vs actual
public decimal CustomerVisibleTotal { get; set; }      // Total shown to customer (may include hidden surcharge)
public decimal ShopReceivableAmount { get; set; }      // What shop actually receives (Total - Commission)
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
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(50)),
    [CommissionAmount] AS CAST(JSON_VALUE([Json], '$.CommissionAmount') AS DECIMAL(18,2)),
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
// CRUD
Task<Agent?> GetAgentByIdAsync(int agentId)
Task<Agent?> GetAgentByCodeAsync(string agentCode)
Task<List<Agent>> GetAgentsAsync(string? status, string? agentType, string? search)
Task<Agent> CreateAgentAsync(Agent agent, string username)
Task<Agent> UpdateAgentAsync(Agent agent, string username)
Task<string> GenerateAgentCodeAsync(string agentType)  // "TG-001", "HTL-001"

// Commission Calculation
Task<decimal> CalculateCommissionAsync(Agent agent, Booking booking)
Task<decimal> CalculateSurchargeAsync(Agent agent, Booking booking, decimal? customRate)

// Statistics
Task<AgentStatistics> GetAgentStatisticsAsync(int agentId, DateTimeOffset? from, DateTimeOffset? to)
Task UpdateAgentStatisticsAsync(int agentId)  // Recalculate totals
```

### 3.2 AgentCommissionService
**File**: `src/MotoRent.Services/AgentCommissionService.cs`

```csharp
// CRUD
Task<AgentCommission> CreateCommissionAsync(int agentId, int bookingId, decimal amount, string username)
Task<List<AgentCommission>> GetCommissionsAsync(int? agentId, string? status, DateTimeOffset? from, DateTimeOffset? to)
Task<AgentCommission?> GetCommissionByBookingAsync(int bookingId)

// Workflow
Task ApproveCommissionAsync(int commissionId, string username)
Task ApproveCommissionsAsync(int[] commissionIds, string username)  // Bulk approve
Task PayCommissionAsync(int commissionId, string paymentMethod, string? reference, string username)
Task PayCommissionsAsync(int[] commissionIds, string paymentMethod, string? reference, string username)  // Bulk pay
Task CancelCommissionAsync(int commissionId, string reason, string username)

// Reporting
Task<CommissionSummary> GetCommissionSummaryAsync(int? agentId, DateTimeOffset? from, DateTimeOffset? to)
Task<List<CommissionPayoutReport>> GetPayoutReportAsync(DateTimeOffset from, DateTimeOffset to)
```

### 3.3 AgentInvoiceService
**File**: `src/MotoRent.Services/AgentInvoiceService.cs`

```csharp
// CRUD
Task<AgentInvoice> CreateInvoiceAsync(int agentId, int bookingId, AgentInvoiceRequest request, string username)
Task<AgentInvoice?> GetInvoiceByIdAsync(int invoiceId)
Task<AgentInvoice?> GetInvoiceByBookingAsync(int bookingId)
Task<List<AgentInvoice>> GetInvoicesAsync(int? agentId, string? status)

// Invoice Generation
Task<string> GenerateInvoiceNoAsync(Agent agent)
Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)

// Payment
Task RecordPaymentAsync(int invoiceId, decimal amount, string username)
```

### 3.4 Update BookingService
**File**: `src/MotoRent.Services/BookingService.cs`

Add methods:
```csharp
// Agent Booking
Task<Booking> CreateAgentBookingAsync(CreateAgentBookingRequest request, string username)
Task<List<Booking>> GetAgentBookingsAsync(int agentId, string? status, DateTimeOffset? from, DateTimeOffset? to)

// Helpers
Task ApplyAgentToBookingAsync(int bookingId, int agentId, decimal? customSurcharge, string username)
Task RemoveAgentFromBookingAsync(int bookingId, string username)
```

---

## Phase 4: UI - Agent Management

### 4.1 Page Structure
```
Pages/Agents/
├── AgentList.razor              # List with search/filters
├── AgentDetails.razor           # Agent profile + bookings + commissions
├── AgentDialog.razor            # Create/Edit agent
├── Components/
│   ├── AgentBookingsTab.razor   # Bookings by this agent
│   ├── AgentCommissionsTab.razor # Commission history
│   └── AgentStatisticsCard.razor
└── Dialogs/
    └── PayCommissionDialog.razor
```

### 4.2 AgentList.razor
- Route: `/agents`
- Filters: Agent type, Status, Search
- Table: Code, Name, Type, Commission Rate, Balance, Status, Actions
- Header action: "Add Agent" button

### 4.3 AgentDetails.razor
- Route: `/agents/{AgentId:int}`
- Left (col-4): Agent info card, Statistics card, Quick actions
- Right (col-8): Tabs
  - **Bookings**: Agent's bookings with status
  - **Commissions**: Commission history with pay action
  - **Invoices**: Agent-generated invoices
  - **Settings**: Commission/surcharge settings

### 4.4 Commission Management Page
**File**: `Pages/Finance/AgentCommissions.razor`
- Route: `/finance/agent-commissions`
- View pending commissions across all agents
- Bulk approve and pay
- Filter by agent, status, date range

---

## Phase 5: UI - Booking Integration

### 5.1 Update CreateBooking.razor
Add "Agent Booking" option:
- Toggle: "This is an agent booking"
- Agent selector (search by code or name)
- Auto-calculate commission and surcharge
- Option to customize surcharge
- Show breakdown: Base Total + Surcharge = Customer Pays

### 5.2 Update BookingDetails.razor
Add Agent section (if agent booking):
- Agent info card
- Commission status
- Surcharge breakdown
- Link to agent profile

### 5.3 Agent Quick Booking
**File**: `Pages/Agents/AgentQuickBooking.razor`
- Route: `/agents/{AgentCode}/book`
- Pre-filled with agent info
- Streamlined flow for frequent agent bookings

---

## Phase 6: Customer Experience

### 6.1 Hidden Surcharge Handling
When `SurchargeHidden = true`:
- Customer sees `CustomerVisibleTotal` (includes hidden surcharge)
- Receipt shows single total, no surcharge line
- MyBooking page shows unified price

When `SurchargeHidden = false`:
- Customer sees base price + service fee
- Can be labeled as "Booking Service Fee" or "Concierge Fee"

### 6.2 Check-in Flow
- Staff sees agent info in booking
- Commission auto-created when booking confirmed
- Check-in proceeds normally
- No additional steps for customer

### 6.3 Agent Invoice Option
- Agent can generate invoice before/after check-in
- Invoice shows agent's branding/details
- Customer pays agent directly (not tracked in system)
- OR customer pays shop, agent invoices shop

---

## Phase 7: Reporting & Analytics

### 7.1 Agent Performance Report
- Bookings by agent
- Revenue generated
- Commission earned vs paid
- Average booking value
- Cancellation rate

### 7.2 Commission Payout Report
- Outstanding commissions by agent
- Payment history
- Export for accounting

### 7.3 Surcharge Analysis
- Revenue from surcharges
- Surcharge patterns by agent type

---

## Data Flow Examples

### Example 1: Hotel Books for Guest (Hidden Surcharge)
```
Rental Base Price: ฿3,000
Agent Commission (10%): ฿300
Agent Surcharge (15%): ฿450 (hidden)

Customer Sees: ฿3,450 total
Shop Receives: ฿3,000 (from customer) - ฿300 (commission) = ฿2,700
Agent Receives: ฿300 (commission) + ฿450 (surcharge from customer) = ฿750
```

### Example 2: Tour Guide Books (Visible Service Fee)
```
Rental Base Price: ฿2,000
Agent Commission (฿200 fixed): ฿200
Agent Surcharge (visible): ฿300

Customer Sees: ฿2,000 + ฿300 service fee = ฿2,300
Shop Receives: ฿2,000 - ฿200 = ฿1,800
Agent Receives: ฿200 (commission) + ฿300 (surcharge) = ฿500
```

### Example 3: Agent Issues Own Invoice
```
Rental Base Price: ฿5,000
Agent Commission (8%): ฿400

Agent invoices customer: ฿6,500 (their own markup)
Customer pays agent: ฿6,500
Agent pays shop: ฿5,000 (or shop invoices agent)
Agent keeps: ฿6,500 - ฿5,000 + ฿400 = ฿1,900
```

---

## Implementation Order

1. **Phase 1**: Entities (Agent, AgentCommission, AgentInvoice, Booking updates)
2. **Phase 2**: Database tables
3. **Phase 3**: Services (AgentService, AgentCommissionService)
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
| `src/MotoRent.Domain/Entities/AgentCommission.cs` | Commission tracking |
| `src/MotoRent.Domain/Entities/AgentCommissionStatus.cs` | Commission status constants |
| `src/MotoRent.Domain/Entities/AgentInvoice.cs` | Agent invoices |
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
| `src/MotoRent.Server/Program.cs` | Register new services |
| `src/MotoRent.Client/Shared/NavMenu.razor` | Add Agents menu item |
| `src/MotoRent.Client/Pages/Bookings/CreateBooking.razor` | Add agent booking option |
| `src/MotoRent.Client/Pages/Bookings/BookingDetails.razor` | Show agent info |

---

## Questions to Clarify

1. **Commission Payment Timing**: Pay when booking confirmed, or when rental completed?
2. **Cancellation Handling**: How to handle commission if booking is cancelled?
3. **Agent Portal**: Do agents need their own login to view bookings/commissions?
4. **Invoice Flow**: Does customer pay shop or agent directly?
5. **Currency**: Support multiple currencies for international agents?
