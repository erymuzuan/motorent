# Fix "Closing Till" Dialog - THB Denominations Bug + Fullscreen UI

## Problem Summary

1. **Bug**: THB denominations other than 1,000 don't update when typing directly in input fields
2. **Dialog Size**: Need fullscreen layout instead of medium modal
3. **UI Optimization**: Optimize layout for fullscreen display

## Root Cause Analysis

In `ClosingCountPanel.razor` line 47, the `@oninput` handler passes a stale `count` variable from the foreach loop closure instead of extracting the new value from `ChangeEventArgs`:

```razor
// BUGGY - count is captured at render time, not when input fires
@oninput="@(() => OnCountChanged(currency, denomination, count))"
```

**Why 1,000 works**: Users click +/- buttons (which use correct `Increment`/`Decrement` methods). Direct typing fails because the handler receives the old value.

---

## Implementation Plan

### 1. Fix Input Binding Bug

**File**: `src/MotoRent.Client/Components/Till/ClosingCountPanel.razor`

**Change line 47** - Fix event handler to capture actual input value:
```razor
// Before
@oninput="@(() => OnCountChanged(currency, denomination, count))"

// After
@oninput="@(e => this.OnCountChanged(currency, denomination, e))"
```

**Change lines 403-415** - Update method to extract value from ChangeEventArgs:
```csharp
// Before
private void OnCountChanged(string currency, decimal denomination, int count)

// After
private void OnCountChanged(string currency, decimal denomination, ChangeEventArgs e)
{
    var breakdown = this.GetOrCreateBreakdown(currency);

    if (int.TryParse(e.Value?.ToString(), out var count) && count >= 0)
    {
        breakdown[denomination] = count;
    }
    else
    {
        breakdown[denomination] = 0;
    }

    this.NotifyChanged();
}
```

### 2. Change Dialog to Fullscreen

**File**: `src/MotoRent.Client/Pages/Staff/Till.razor` (line 589-592)

```csharp
// Before
var result = await this.DialogService.Create<TillCloseSessionDialog>("Close Till Session")
    .WithParameter(x => x.Entity, this.m_session)
    .WithSize(ModalSize.Medium)
    .ShowDialogAsync();

// After
var result = await this.DialogService.Create<TillCloseSessionDialog>(this.Localizer["CloseSession"])
    .WithParameter(x => x.Entity, this.m_session)
    .WithFullscreen()
    .WithHeader(false)
    .ShowDialogAsync();
```

### 3. Restructure TillCloseSessionDialog for Fullscreen

**File**: `src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor`

Restructure using sidebar pattern (like TillOpenSessionDialog):
- Left side: Main content area with denomination panel
- Right sidebar: Totals, variance, and action buttons
- Session summary as horizontal bar instead of card

Key structural changes:
- Wrap content in `.mr-till-close-fullscreen` container with flex layout
- Move action buttons to sidebar
- Add `GetTimeEmoji()`, `GetTotalCountedThb()`, `FormatVarianceAmount()` helper methods
- Apply `this.` prefix per code-standards

### 4. Update ClosingCountPanel for Grid Layout

**File**: `src/MotoRent.Client/Components/Till/ClosingCountPanel.razor`

Add CSS class for grid layout:
```razor
<div class="mr-count-sections mr-count-grid">
```

### 5. CSS Updates

**File**: `src/MotoRent.Client/Components/Till/ClosingCountPanel.razor.css`

Add grid layout for fullscreen:
```css
.mr-count-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 1.5rem;
}

@media (min-width: 992px) {
    .mr-count-grid { grid-template-columns: repeat(2, 1fr); }
}

@media (min-width: 1200px) {
    .mr-count-grid { grid-template-columns: repeat(3, 1fr); }
}
```

**File**: `src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor.css` (new file)

Create fullscreen layout styles matching TillOpenSessionDialog pattern with:
- `.mr-till-close-fullscreen` - Flex container
- `.mr-till-main-area` - Left content area
- `.mr-till-sidebar` - Right sidebar with totals/actions
- `.mr-total-card` - Large total display
- `.mr-comparison-card` - Expected vs actual comparison
- Responsive breakpoints for mobile

---

## Files to Modify

| File | Change |
|------|--------|
| `ClosingCountPanel.razor` | Fix `@oninput` bug + add grid class |
| `ClosingCountPanel.razor.css` | Add grid layout styles |
| `Till.razor` | Change to `.WithFullscreen().WithHeader(false)` |
| `TillCloseSessionDialog.razor` | Restructure for fullscreen with sidebar |
| `TillCloseSessionDialog.razor.css` | Create new file for fullscreen styles |

---

## Verification

1. **Bug fix**: Type values directly into 500, 100, 50, 20, 10, 5, 2, 1 THB inputs - totals should update
2. **+/- buttons**: Still work correctly
3. **Fullscreen**: Dialog opens fullscreen with sidebar layout
4. **Mobile**: Stacks vertically on small screens
5. **Multi-currency**: Grid shows THB + foreign currencies when expected balance > 0
