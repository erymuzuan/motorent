# Implementation Plan: Agent Selection in Booking Creation

## Summary
Add optional agent selection when staff creates a booking, enabling commission tracking for agent-referred bookings.

## Files to Modify

### 1. `src/MotoRent.Services/BookingService.cs`
**Extend CreateBookingRequest** (line ~1171):
- Add agent fields: `AgentId`, `AgentCode`, `AgentName`, `AgentCommission`, `AgentSurcharge`, `SurchargeHidden`, `AgentPaymentFlow`

**Modify CreateBookingAsync** (line ~28):
- Map agent fields from request to booking entity
- After `RecalculateTotals()`, calculate `CustomerVisibleTotal` and `ShopReceivableAmount`
- After successful save, create `AgentCommission` record via `AgentCommissionService`

**Update constructor**:
- Inject `AgentCommissionService`

### 2. `src/MotoRent.Client/Pages/Bookings/CreateBooking.razor`
**Add service injection**:
- `@inject AgentService AgentService`

**Add member variables** (in @code block):
- `m_agents` - List of active agents
- `m_selectedAgentId` - Selected agent ID
- `m_selectedAgent` - Selected agent entity
- `m_agentSurcharge` - Editable surcharge amount
- `m_surchargeHidden` - Hide surcharge from customer

**Modify LoadInitialDataAsync**:
- Load agents in parallel with shops/insurance

**Add agent selection UI** (in Step 1 - Customer Info, after hotel name):
- Agent dropdown with `@bind:after` callback
- Surcharge input when agent allows it
- Hide-from-customer checkbox

**Add commission display** (in Step 4 - Payment summary):
- Show agent commission deduction
- Show surcharge if applicable
- Show shop receivable amount

**Add agent info** (in Step 5 - Confirmation):
- Display selected agent name/code
- Display commission amount

**Update booking creation call**:
- Set agent fields on `m_request` before calling service

### 3. Localization Files
**CreateBooking.resx** (default/en):
- AgentReferral, NoAgent, AgentCommission, AgentSurcharge, HideSurchargeFromCustomer, ShopReceives, AgentInfo

**CreateBooking.th.resx**:
- Thai translations

**CreateBooking.ms.resx** (create if missing):
- Malay translations

## Implementation Order
1. Extend `CreateBookingRequest` with agent fields
2. Update `BookingService` constructor to inject `AgentCommissionService`
3. Modify `CreateBookingAsync` to handle agent data and create commission record
4. Add agent UI elements to `CreateBooking.razor`
5. Add localization resources

## Key Code Patterns

### Agent Selection Handler
```csharp
private void OnAgentChanged()
{
    m_selectedAgent = m_selectedAgentId.HasValue
        ? m_agents.FirstOrDefault(a => a.AgentId == m_selectedAgentId)
        : null;

    if (m_selectedAgent != null)
    {
        m_surchargeHidden = m_selectedAgent.SurchargeHiddenFromCustomer;
        m_agentSurcharge = m_selectedAgent.DefaultSurchargeRate ?? 0;
    }
}
```

### Commission Calculation
```csharp
private decimal m_agentCommission => m_selectedAgent != null
    ? AgentService.CalculateCommission(m_selectedAgent, m_totalAmount, m_selectedVehicles.Count, m_days)
    : 0;
```

## Verification
1. **Build**: `dotnet build` should succeed
2. **Manual test**:
   - Create booking without agent → BookingSource = "Staff", no AgentCommission record
   - Create booking with agent → BookingSource = "Agent", AgentCommission record created
   - Check commission calculation matches agent settings
   - Verify Agent Commissions page shows the new record
