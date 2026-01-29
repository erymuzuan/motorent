# Fix: AssetEdit Save Button Not Submitting Form

## Changes Already Applied

### 1. AssetEdit.razor - FormId null on first render
- `form="@m_dialog?.FormId"` → `form="assetdialog"` (constant)
- `disabled="@m_saving"` → `disabled="@(m_dialog?.Saving ?? false)"`
- Spinner condition updated to `m_dialog?.Saving`
- Removed dead `m_saving` field

### 2. AssetDialog.razor - Refactored from dialog to component
- `@inherits LocalizedDialogBase<Asset, AssetDialog>` → `@inherits LocalizedComponentBase<AssetDialog>`
- Added `Entity`, `IsNew`, `FormId`, `Saving` properties directly
- Removed modal `Close()` fallback from SaveAsync

## Remaining: Manual Verification

1. Impersonate as `admin@krabirentals.com` (KrabiBeachRentals)
2. Navigate to `/finance/assets/lbrew6xY` - fill loan fields, click Save
3. Verify database: `AssetLoan` record created, `Asset.AssetLoanId` set
4. Navigate to `/finance/assets/lbrew6xY/details` - verify loan amortization table
