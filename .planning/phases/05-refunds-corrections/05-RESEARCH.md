# Phase 5: Refunds & Corrections - Research

**Researched:** 2026-01-20
**Domain:** POS refund processing, transaction voids, manager authorization
**Confidence:** HIGH

## Summary

This phase implements refunds and void capabilities for the existing cashier till system. The codebase already has solid foundations:
- TillTransaction entity with direction (In/Out), type categorization, and currency support
- TillService with RecordPayoutAsync for cash-out transactions including DepositRefund type
- TillSession with per-currency balance tracking (CurrencyBalances dictionary)
- CheckOutDialog already implements security deposit refunds during the check-out flow
- Existing ThbKeypadPanel provides the visual pattern for a PIN entry dialog
- User entity needs extension for manager PIN storage

The primary work is: (1) adding void-related fields to TillTransaction, (2) creating a manager PIN dialog, (3) building refund initiation from original transactions, and (4) implementing void workflow with authorization.

**Primary recommendation:** Extend TillTransaction with void metadata, add ManagerPinHash to User, create ManagerPinDialog based on ThbKeypadPanel pattern, and build RefundDialog/VoidDialog components.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MudBlazor | Existing | UI Components | Already used throughout codebase |
| System.Security.Cryptography | Built-in | PIN hashing | .NET standard for secure hashing |
| Entity Pattern | Custom | Data persistence | Existing repository pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tabler Icons | Existing | UI Icons | ti-* icon classes |
| CSS Scoped | Built-in | Component styling | .razor.css files |

### No New Dependencies Required
This phase leverages existing patterns entirely. No new packages needed.

## Architecture Patterns

### Recommended Additions to TillTransaction

```csharp
// Add to TillTransaction.cs
public bool IsVoided { get; set; }
public DateTimeOffset? VoidedAt { get; set; }
public string? VoidedByUserName { get; set; }
public string? VoidReason { get; set; }
public string? VoidApprovedByUserName { get; set; }
public int? OriginalTransactionId { get; set; }  // For compensating entries
public int? RelatedTransactionId { get; set; }   // Links void to original
```

### Recommended Addition to User Entity

```csharp
// Add to User.cs
/// <summary>
/// Hashed manager PIN for void approvals (4-6 digits).
/// Null if user doesn't have void approval privilege.
/// </summary>
public string? ManagerPinHash { get; set; }

/// <summary>
/// Salt for manager PIN hash.
/// </summary>
public string? ManagerPinSalt { get; set; }
```

### Manager PIN Dialog Component Structure

```
Components/
  Auth/
    ManagerPinDialog.razor       # PIN entry with numeric keypad
    ManagerPinDialog.razor.css   # Keypad styling (based on ThbKeypadPanel)
```

### Refund/Void Dialog Structure

```
Pages/
  Staff/
    TillRefundDialog.razor       # Refund from original payment
    TillVoidDialog.razor         # Void with manager approval
```

### Pattern 1: Void Transaction Flow

**What:** Creates compensating entry, marks original as voided
**When to use:** Staff clicks "Void" on a transaction
**Example:**

```csharp
// In TillService.cs
public async Task<SubmitOperation> VoidTransactionAsync(
    int transactionId,
    string staffUserName,
    string managerUserName,
    string reason)
{
    var original = await GetTransactionByIdAsync(transactionId);
    if (original == null || original.IsVoided)
        return SubmitOperation.CreateFailure("Transaction not found or already voided");

    // Create compensating entry (reverse direction)
    var compensating = new TillTransaction
    {
        TillSessionId = original.TillSessionId,
        TransactionType = original.TransactionType,
        Direction = original.Direction == TillTransactionDirection.In
            ? TillTransactionDirection.Out
            : TillTransactionDirection.In,
        Amount = original.Amount,
        Currency = original.Currency,
        ExchangeRate = original.ExchangeRate,
        AmountInBaseCurrency = original.AmountInBaseCurrency,
        Description = $"VOID: {original.Description}",
        OriginalTransactionId = transactionId,
        TransactionTime = DateTimeOffset.Now,
        RecordedByUserName = staffUserName,
        Notes = $"Voided by {managerUserName}: {reason}"
    };

    // Mark original as voided
    original.IsVoided = true;
    original.VoidedAt = DateTimeOffset.Now;
    original.VoidedByUserName = staffUserName;
    original.VoidReason = reason;
    original.VoidApprovedByUserName = managerUserName;
    original.RelatedTransactionId = compensating.TillTransactionId; // Set after save

    // Update session balances (reverse the original effect)
    var session = await GetSessionByIdAsync(original.TillSessionId);
    // ... update TotalCashIn/TotalCashOut/CurrencyBalances
}
```

### Pattern 2: Refund from Original Payment

**What:** Issues THB cash refund based on original payment details
**When to use:** Processing overpayment refund, security deposit refund outside check-out
**Example:**

```csharp
// Calculate THB equivalent from split payments
public decimal CalculateRefundAmount(List<ReceiptPayment> originalPayments)
{
    return originalPayments.Sum(p => p.AmountInBaseCurrency);
}

// Record refund to till (always THB cash out)
public async Task<SubmitOperation> RecordRefundAsync(
    int sessionId,
    decimal refundAmountThb,
    string reason,
    int originalPaymentId,
    string username)
{
    return await RecordPayoutAsync(
        sessionId,
        TillTransactionType.DepositRefund, // or new OverpaymentRefund type
        refundAmountThb,
        $"Refund: {reason}",
        username,
        notes: $"Original payment #{originalPaymentId}");
}
```

### Pattern 3: Manager PIN Verification

**What:** Verifies 4-6 digit PIN against stored hash
**When to use:** Before approving void
**Example:**

```csharp
public bool VerifyManagerPin(User manager, string enteredPin)
{
    if (string.IsNullOrEmpty(manager.ManagerPinHash))
        return false;

    using var pbkdf2 = new Rfc2898DeriveBytes(
        enteredPin,
        Convert.FromBase64String(manager.ManagerPinSalt!),
        10000,
        HashAlgorithmName.SHA256);

    var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
    return hash == manager.ManagerPinHash;
}
```

### Anti-Patterns to Avoid

- **Storing PIN in plaintext:** Always hash with salt using PBKDF2 or bcrypt
- **Deleting voided transactions:** Preserve for audit trail, use IsVoided flag
- **Allowing self-approval:** Staff cannot approve their own voids
- **Skipping session balance update:** Void must reverse effect on CurrencyBalances

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Numeric keypad | Custom HTML buttons | Adapt ThbKeypadPanel pattern | Consistent touch-friendly UX |
| PIN hashing | Simple MD5/SHA1 | PBKDF2 with salt | Security best practice |
| Dialog management | Custom modal | IModalService (existing) | Consistent dialog patterns |
| Currency formatting | String concatenation | FormatThb() helper (existing) | Thai Baht symbol handling |

**Key insight:** The codebase has mature patterns for dialogs, currency handling, and touch-friendly keypads. Reuse these patterns for consistency.

## Common Pitfalls

### Pitfall 1: Void Timing Issues

**What goes wrong:** Void processed after session closed
**Why it happens:** User holds dialog open, session closes in another tab
**How to avoid:** Re-check session status before finalizing void
**Warning signs:** Session.Status != Open at void time

### Pitfall 2: Currency Balance Mismatch

**What goes wrong:** Till balance goes negative or doesn't match actual cash
**Why it happens:** Refund in THB but original was foreign currency
**How to avoid:** Context decision says: void foreign currency reverses exact currencies
**Warning signs:** CurrencyBalances[currency] going negative unexpectedly

### Pitfall 3: Lockout State Persistence

**What goes wrong:** Lockout bypassed by page refresh
**Why it happens:** Lockout stored only in component state
**How to avoid:** Store lockout timestamp in session storage or server-side
**Warning signs:** User can retry PIN immediately after refresh

### Pitfall 4: Self-Approval Loophole

**What goes wrong:** Manager approves their own void
**Why it happens:** No check that approver != initiator
**How to avoid:** Validate VoidApprovedByUserName != VoidedByUserName
**Warning signs:** Same username appears as both void initiator and approver

## Code Examples

### Manager PIN Dialog (based on ThbKeypadPanel)

```razor
@* ManagerPinDialog.razor - Touch-friendly numeric keypad *@
<div class="pin-dialog">
    <div class="pin-header">
        <h5>Manager Approval Required</h5>
        <p class="text-muted">Enter your 4-digit PIN to approve this void</p>
    </div>

    @if (m_lockoutUntil.HasValue && m_lockoutUntil > DateTimeOffset.Now)
    {
        <div class="alert alert-danger">
            <i class="ti ti-lock me-2"></i>
            Too many attempts. Try again in @GetLockoutSeconds() seconds.
        </div>
    }
    else
    {
        <div class="pin-display">
            @for (int i = 0; i < 4; i++)
            {
                <div class="pin-dot @(m_pin.Length > i ? "filled" : "")"></div>
            }
        </div>

        @if (m_error != null)
        {
            <div class="text-danger text-center mb-2">@m_error</div>
        }

        <div class="keypad-grid">
            @foreach (var digit in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "C" })
            {
                @if (string.IsNullOrEmpty(digit))
                {
                    <div class="keypad-spacer"></div>
                }
                else if (digit == "C")
                {
                    <button type="button" class="keypad-btn keypad-btn-action" @onclick="OnClear">
                        <i class="ti ti-backspace"></i>
                    </button>
                }
                else
                {
                    <button type="button" class="keypad-btn" @onclick="@(() => OnDigit(digit))">
                        @digit
                    </button>
                }
            }
        </div>
    }
</div>

@code {
    [Parameter] public EventCallback<string> OnPinVerified { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string m_pin = "";
    private string? m_error;
    private int m_attempts;
    private DateTimeOffset? m_lockoutUntil;

    private void OnDigit(string digit)
    {
        if (m_pin.Length >= 4) return;
        m_pin += digit;
        m_error = null;

        if (m_pin.Length == 4)
        {
            _ = VerifyPinAsync();
        }
    }

    private void OnClear()
    {
        m_pin = "";
        m_error = null;
    }

    private async Task VerifyPinAsync()
    {
        m_attempts++;
        if (m_attempts >= 3)
        {
            m_lockoutUntil = DateTimeOffset.Now.AddMinutes(5);
            m_pin = "";
            return;
        }

        await OnPinVerified.InvokeAsync(m_pin);
        // If callback doesn't close dialog, PIN was wrong
        m_error = $"Incorrect PIN. {3 - m_attempts} attempts remaining.";
        m_pin = "";
    }
}
```

### Voided Transaction Display

```razor
@* In transaction list *@
<tr class="@(txn.IsVoided ? "voided-row" : "")">
    <td>
        @if (txn.IsVoided)
        {
            <span class="badge bg-danger me-1">VOID</span>
        }
        <span class="@(txn.IsVoided ? "text-decoration-line-through text-muted" : "")">
            @txn.Description
        </span>
    </td>
    <td class="text-end @(txn.IsVoided ? "text-decoration-line-through text-muted" : "")">
        @FormatThb(txn.Amount)
    </td>
    <td>
        @if (!txn.IsVoided && CanVoidTransaction(txn))
        {
            <button type="button" class="btn btn-sm btn-ghost-danger" @onclick="() => VoidTransaction(txn)">
                <i class="ti ti-x"></i>
            </button>
        }
    </td>
</tr>

<style>
    .voided-row {
        background-color: rgba(var(--tblr-danger-rgb), 0.05);
    }
</style>
```

### Refund Dialog Summary

```razor
@* TillRefundDialog.razor - Summary before confirm *@
<div class="refund-summary">
    <h5 class="mb-3">Refund Summary</h5>

    <div class="card mb-3">
        <div class="card-header">Original Payment</div>
        <div class="card-body">
            @foreach (var payment in m_originalPayments)
            {
                <div class="d-flex justify-content-between mb-2">
                    <span>
                        <i class="ti @GetMethodIcon(payment.Method) me-1"></i>
                        @payment.Method (@payment.Currency)
                    </span>
                    <span>
                        @FormatCurrency(payment.Amount, payment.Currency)
                        @if (payment.Currency != SupportedCurrencies.THB)
                        {
                            <span class="text-muted"> = @FormatThb(payment.AmountInBaseCurrency)</span>
                        }
                    </span>
                </div>
            }
            <hr />
            <div class="d-flex justify-content-between fw-bold">
                <span>Total (THB equivalent)</span>
                <span>@FormatThb(m_totalThb)</span>
            </div>
        </div>
    </div>

    <div class="alert alert-info mb-3">
        <i class="ti ti-cash me-2"></i>
        Refund will be issued as <strong>@FormatThb(m_refundAmount) THB Cash</strong>
    </div>

    @if (m_requiresReason)
    {
        <div class="mb-3">
            <label class="form-label required">Reason for Refund</label>
            <textarea class="form-control" @bind="m_reason" rows="2"></textarea>
        </div>
    }

    @if (m_tillThbBalance < m_refundAmount)
    {
        <div class="alert alert-warning">
            <i class="ti ti-alert-triangle me-2"></i>
            Warning: Till THB balance (@FormatThb(m_tillThbBalance)) is less than refund amount.
            Refund can still be processed but till will show negative balance.
        </div>
    }
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Delete voided records | Mark with IsVoided flag | Best practice | Audit compliance |
| PIN in plaintext | PBKDF2 hash with salt | Security standard | Data protection |
| Single currency till | Multi-currency CurrencyBalances | Phase 3 | Foreign currency support |

**Deprecated/outdated:**
- None relevant to this phase

## Open Questions

Things that couldn't be fully resolved:

1. **Lockout Persistence**
   - What we know: 3 attempts locks for 5 minutes per CONTEXT.md
   - What's unclear: Where to persist lockout state (component, session storage, server)
   - Recommendation: Use browser sessionStorage for MVP, consider server-side for production

2. **Void History Access**
   - What we know: Only managers can view void history per CONTEXT.md
   - What's unclear: UI location for void history (separate page vs filter on transaction list)
   - Recommendation: Add filter toggle on Till.razor transaction list, check user role

## Sources

### Primary (HIGH confidence)
- TillTransaction.cs - Entity structure, existing fields
- TillService.cs - RecordPayoutAsync, session management
- User.cs - Existing user entity structure
- CheckOutDialog.razor - Security deposit refund pattern
- ThbKeypadPanel.razor - Touch-friendly keypad pattern
- UserAccount.cs - Role definitions (ManagementRoles)

### Secondary (MEDIUM confidence)
- CONTEXT.md - User decisions for this phase

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All patterns exist in codebase
- Architecture: HIGH - Direct extension of existing entities/services
- Pitfalls: HIGH - Based on actual code analysis

**Research date:** 2026-01-20
**Valid until:** 2026-02-20 (stable domain, 30 days)
