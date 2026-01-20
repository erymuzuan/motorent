# Phase 4: Payment Terminal Redesign - Research

**Researched:** 2026-01-20
**Domain:** Multi-currency payment processing UI for rental cashier till
**Confidence:** HIGH

## Summary

Phase 4 extends the existing TillTransactionDialog (Step 1: Search, Step 2: Item Confirmation) with a new Step 3: Payment Terminal. The terminal must support split payments across multiple currencies (THB, USD, EUR, CNY) and payment methods (Cash, Credit Card, PromptPay, AliPay).

The codebase already provides all required infrastructure:
- `ExchangeRateService` for currency conversion with audit trail
- `TillService` with `RecordForeignCurrencyPaymentAsync` for multi-currency recording
- `DenominationEntryPanel` component for foreign currency denomination counting
- `ReceiptPayment` model supporting multi-currency with exchange rate metadata
- `CurrencyDenominations` static helper with denomination arrays for THB/USD/EUR/CNY

**Primary recommendation:** Build the payment terminal as Step 3 of `TillTransactionDialog`, consuming the `TransactionSearchResult.GrandTotal` from Step 2 and producing a `List<ReceiptPayment>` for receipt generation.

## Standard Stack

The phase uses existing infrastructure - no new libraries required.

### Core (Existing)
| Component | Location | Purpose | Why Standard |
|-----------|----------|---------|--------------|
| `TillTransactionDialog` | Pages/Staff/ | Parent dialog (Steps 1-2 exist) | Extend with Step 3 |
| `DenominationEntryPanel` | Components/Till/ | Foreign currency counting | Reusable, tested |
| `TillService` | Services/ | Till transaction recording | Multi-currency support exists |
| `ExchangeRateService` | Services/ | Currency conversion | Audit trail, rate tracking |
| `ReceiptPayment` | Domain/Entities/ | Payment record model | Supports split payments |

### Supporting (Existing)
| Component | Location | Purpose | When to Use |
|-----------|----------|---------|-------------|
| `CurrencyDenominations` | Domain/Entities/ | Denomination arrays, symbols | THB/USD/EUR/CNY constants |
| `SupportedCurrencies` | Domain/Entities/ | Currency code constants | Currency identification |
| `PaymentMethods` | Domain/Entities/ | Payment method constants | Cash/Card/PromptPay |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extend TillTransactionDialog | New PaymentTerminalDialog | Would break workflow; prefer single flow |
| New PaymentEntry model | Reuse ReceiptPayment | ReceiptPayment already has all fields needed |

**Installation:** None - all components exist in codebase.

## Architecture Patterns

### Recommended Project Structure
```
src/MotoRent.Client/
├── Pages/Staff/
│   └── TillTransactionDialog.razor    # Extend with Step 3
├── Components/Till/
│   ├── PaymentTerminalPanel.razor     # NEW: Main payment UI
│   ├── ThbKeypadPanel.razor           # NEW: THB numeric keypad
│   ├── PaymentMethodTabs.razor        # NEW: Cash/Card/PromptPay/AliPay tabs
│   └── DenominationEntryPanel.razor   # EXISTING: Foreign currency counting
└── Resources/
    └── Pages/Staff/
        └── TillTransactionDialog.*.resx  # Extend with payment strings
```

### Pattern 1: Step-Based Dialog Flow

**What:** TillTransactionDialog progresses through steps: Search -> Item Confirmation -> Payment
**When to use:** Multi-step workflows where each step produces input for the next

**Example:**
```csharp
// Step 1: Search returns selected entity
// Step 2: Item confirmation returns GrandTotal
// Step 3: Payment terminal receives GrandTotal, collects payments

@if (m_selectedBooking is null && m_selectedRental is null)
{
    // Step 1: Search UI
}
else if (m_paymentEntries.Count == 0 || m_remaining > 0)
{
    // Step 2: Item Confirmation (existing)
    // Step 3: Payment Terminal (new)
}
else
{
    // Complete - generate receipt
}
```

### Pattern 2: Payment Entry Collection

**What:** Accumulate payment entries until total >= amount due
**When to use:** Split payment scenarios

**Example:**
```csharp
// Model for tracking payment entries during input
private List<PaymentEntry> m_paymentEntries = [];
private decimal m_totalReceived => m_paymentEntries.Sum(p => p.AmountInBaseCurrency);
private decimal m_remaining => m_grandTotal - m_totalReceived;
private decimal m_change => m_totalReceived > m_grandTotal ? m_totalReceived - m_grandTotal : 0;

public class PaymentEntry
{
    public string Method { get; set; }      // Cash, Card, PromptPay, AliPay
    public string Currency { get; set; }    // THB, USD, EUR, CNY
    public decimal Amount { get; set; }     // In payment currency
    public decimal AmountInBaseCurrency { get; set; }  // THB equivalent
    public decimal ExchangeRate { get; set; }
    public string? Reference { get; set; }  // Card auth, PromptPay ref
}
```

### Pattern 3: THB Keypad with Quick Amounts

**What:** Numeric keypad for THB with quick-amount buttons
**When to use:** THB cash entry (faster than denomination counting)

**Example:**
```csharp
// State
private string m_thbInput = "";
private decimal ThbAmount => decimal.TryParse(m_thbInput, out var v) ? v : 0;

// Keypad digits
private void OnKeyPress(char key)
{
    if (key == 'C') { m_thbInput = ""; return; }
    if (key == '<') { m_thbInput = m_thbInput.Length > 0 ? m_thbInput[..^1] : ""; return; }
    m_thbInput += key;
}

// Quick amounts
private void SetQuickAmount(decimal amount) => m_thbInput = amount.ToString("0");
```

### Pattern 4: Currency Tab with Entry Indicator

**What:** Tab shows green dot when currency has payment entry
**When to use:** Visual feedback for multi-currency payments

**Example:**
```razor
<div class="currency-tabs">
    @foreach (var currency in new[] { "THB", "USD", "EUR", "CNY" })
    {
        var hasEntry = m_paymentEntries.Any(p => p.Currency == currency && p.Method == "Cash");
        <button class="currency-tab @(m_selectedCurrency == currency ? "active" : "")"
                @onclick="() => SelectCurrency(currency)">
            <span class="flag-icon flag-@GetFlagCode(currency)"></span>
            @currency
            @if (hasEntry)
            {
                <span class="entry-indicator"></span>
            }
        </button>
    }
</div>
```

### Anti-Patterns to Avoid

- **Mixing payment collection with till recording:** Collect all payments first, then record to till in batch on "Complete Payment"
- **Recording partial payments:** Wait until fully paid before creating receipt and till transactions
- **Calculating change per-payment:** Change is calculated once from total THB received minus total due

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Currency conversion | Manual rate lookup | `ExchangeRateService.ConvertToThbAsync()` | Includes audit trail, rate validation |
| Denomination totaling | Sum loop | `DenominationEntryPanel.Total` property | Already calculated with binding |
| Currency symbols | Switch statement | `CurrencyDenominations.GetCurrencySymbol()` | All currencies covered |
| Denomination arrays | Hardcoded arrays | `CurrencyDenominations.GetDenominations()` | Consistent across app |
| Till recording | Direct context update | `TillService.RecordForeignCurrencyPaymentAsync()` | Handles THB + foreign, updates session |

**Key insight:** The TillBookingDepositDialog already implements foreign currency cash handling with denomination counting. Follow its patterns rather than inventing new ones.

## Common Pitfalls

### Pitfall 1: Recording Payments Before Complete

**What goes wrong:** Till balance updated but transaction cancelled or fails
**Why it happens:** Recording to till on each "Add Payment" click
**How to avoid:** Collect payments in memory, record all at once on "Complete Payment"
**Warning signs:** Till balance changes before receipt generated

### Pitfall 2: Change Calculation Per Currency

**What goes wrong:** Trying to give change in foreign currency
**Why it happens:** Calculating change for each payment entry separately
**How to avoid:** Sum all AmountInBaseCurrency, calculate change once in THB
**Warning signs:** Change shown in USD/EUR instead of THB

### Pitfall 3: Overpayment in Foreign Currency

**What goes wrong:** Customer pays $100 USD when only $50 equivalent needed
**Why it happens:** Not pre-filling reasonable amounts or showing THB equivalent
**How to avoid:** Show "THB Equivalent" as user enters foreign currency
**Warning signs:** Large change amounts frequently

### Pitfall 4: Exchange Rate Changes Mid-Transaction

**What goes wrong:** Rate used for display differs from rate recorded
**Why it happens:** Rate fetched at different times
**How to avoid:** Cache conversion result when payment entry added, use same rate for recording
**Warning signs:** Receipt shows different THB amount than terminal showed

### Pitfall 5: Forgetting GBP Tab Disabled State

**What goes wrong:** Users confused by non-functional GBP tab
**Why it happens:** Not implementing disabled state from CONTEXT.md
**How to avoid:** Add `disabled` class and `disabled` attribute to GBP tab
**Warning signs:** GBP tab clickable but produces no UI

## Code Examples

Verified patterns from existing codebase:

### Currency Conversion with Audit Trail
```csharp
// Source: TillBookingDepositDialog.razor lines 555-568
var conversion = await ExchangeRateService.ConvertToThbAsync(m_selectedCurrency, m_receivedAmount);
if (conversion is null)
{
    m_conversionError = $"No exchange rate configured for {m_selectedCurrency}";
    m_thbEquivalent = 0;
    m_changeAmount = 0;
    return;
}

m_conversionRate = conversion.RateUsed;
m_thbEquivalent = conversion.ThbAmount;

// conversion.ExchangeRateId available for ReceiptPayment audit
```

### Recording Foreign Currency Payment
```csharp
// Source: TillBookingDepositDialog.razor lines 654-663
tillResult = await TillService.RecordForeignCurrencyPaymentAsync(
    SessionId,
    type,
    m_selectedCurrency,
    m_receivedAmount,
    description,
    this.UserName,
    notes: m_notes);
```

### Creating ReceiptPayment with Multi-Currency
```csharp
// Based on ReceiptPayment.cs structure
var payment = new ReceiptPayment
{
    Method = PaymentMethods.Cash,
    Amount = foreignAmount,
    Currency = selectedCurrency,
    ExchangeRate = conversion.RateUsed,
    AmountInBaseCurrency = conversion.ThbAmount,
    ExchangeRateSource = conversion.RateSource,
    ExchangeRateId = conversion.ExchangeRateId,
    PaidAt = DateTimeOffset.Now
};
```

### THB Formatting
```csharp
// Source: TillTransactionDialog.razor line 1140-1143
private static string FormatAmount(decimal amount)
{
    return $"\u0e3f{amount:N0}"; // Thai Baht symbol
}
```

### Payment Method Tabs UI Pattern
```razor
@* Based on TillBookingDepositDialog payment method selection *@
<div class="mb-3">
    <label class="form-label required">@Localizer["PaymentMethod"]</label>
    <select class="form-select" @bind="m_paymentMethod" @bind:after="OnPaymentMethodChanged">
        <option value="Cash">@Localizer["Cash"]</option>
        <option value="Card">@Localizer["Card"]</option>
        <option value="PromptPay">@Localizer["PromptPay"]</option>
        <option value="BankTransfer">@Localizer["BankTransfer"]</option>
    </select>
</div>
```

### Dialog Footer with Conditional Buttons
```razor
@* Source: TillTransactionDialog.razor lines 439-459 *@
<div class="modal-footer sticky-bottom bg-white border-top" style="z-index: 10;">
    @if (m_selectedBooking is not null || m_selectedRental is not null)
    {
        <button type="button" class="btn btn-ghost-secondary" @onclick="GoBackToItemConfirmation">
            <i class="ti ti-arrow-left"></i> @Localizer["Back"]
        </button>
        <button type="button" class="btn btn-purple btn-lg" @onclick="CompletePayment"
                disabled="@(m_remaining > 0)">
            <i class="ti ti-check"></i>
            @Localizer["CompletePayment"]
        </button>
    }
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single payment method | Split payments | Phase 4 | Multiple entries per transaction |
| THB only | Multi-currency | Phase 2 | Foreign currency support exists |
| Simple amount input | Denomination counting | Phase 2 | `DenominationEntryPanel` for accuracy |

**Deprecated/outdated:**
- None in current stack - all patterns are current

## UI Layout Requirements

From CONTEXT.md, the payment terminal must follow specific layout:

### Two-Column Layout
- **Left Panel (Summary):** Total Amount Due, progress indicator, Payment Details list, Total Received, Change, Remaining, Complete Payment button
- **Right Panel (Input):** Payment method tabs -> context-specific input area

### Payment Method Tabs
| Tab | Icon | Sub-Content |
|-----|------|-------------|
| Cash | ti-cash | Currency tabs (THB, USD, GBP*, EUR, CNY) |
| Credit Card | ti-credit-card | Amount input + reference |
| PromptPay | ti-qrcode | Amount input + Generate QR button |
| AliPay | ti-brand-alipay | Amount input + reference |

*GBP tab visible but disabled

### Cash Currency Tabs
| Currency | Input Type | Notes |
|----------|------------|-------|
| THB | Numeric keypad + quick amounts | Fast entry |
| USD | Denomination counting | DenominationEntryPanel |
| EUR | Denomination counting | DenominationEntryPanel |
| CNY | Denomination counting | DenominationEntryPanel |
| GBP | N/A - disabled | Show but non-functional |

### Visual Indicators
- Green dot on currency tabs with entries
- Red text for "Remaining" when balance due
- "Complete Payment" disabled until remaining = 0
- Progress indicator (blue line with dot) between amount due and payment details

## Open Questions

Things that couldn't be fully resolved:

1. **PromptPay QR Generation**
   - What we know: Organization-level PromptPay ID from settings
   - What's unclear: QR code generation library/approach
   - Recommendation: Defer QR generation to future - use manual confirmation flow for MVP

2. **AliPay Integration**
   - What we know: Manual confirmation flow (customer shows confirmation)
   - What's unclear: AliPay reference number format
   - Recommendation: Free-text reference field for MVP

3. **Receipt Generation Timing**
   - What we know: ReceiptService exists with `GenerateBookingDepositReceiptAsync`
   - What's unclear: Should split payments generate one receipt or multiple?
   - Recommendation: One receipt with multiple ReceiptPayment entries

## Sources

### Primary (HIGH confidence)
- `TillBookingDepositDialog.razor` - Complete multi-currency payment workflow
- `TillService.cs` - Multi-currency recording methods
- `ExchangeRateService.cs` - Conversion with audit trail
- `DenominationEntryPanel.razor` - Reusable denomination counting
- `ReceiptPayment.cs` - Payment model with currency fields

### Secondary (MEDIUM confidence)
- `04-CONTEXT.md` - UI layout decisions from user discussion
- `STATE.md` - Project decisions and architecture notes

### Tertiary (LOW confidence)
- None - all patterns verified in codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components exist and are tested
- Architecture: HIGH - Patterns extracted from existing dialogs
- Pitfalls: HIGH - Observed from existing payment flows

**Research date:** 2026-01-20
**Valid until:** 2026-02-20 (stable - no external dependencies)
