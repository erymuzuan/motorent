# Fix: AssetEdit "Save" Not Persisting Financing Data

## Root Cause

In `AssetDialog.razor` line 328, the loan save logic is gated by:
```csharp
if (m_hasLoan && IsNew)
```
This means financing data is **only saved when creating a new asset**. When editing an existing asset, three scenarios are broken:
1. Adding a loan to an asset that has none → loan never created
2. Updating an existing loan's fields → changes never saved
3. Removing a loan (unchecking "Financed with Loan") → loan reference never cleared

## Fix

### File: `src/MotoRent.Client/Pages/Finance/AssetDialog.razor`

Replace the loan save block (lines 327-344) with logic handling all cases:

```csharp
// Handle loan create/update
if (m_hasLoan)
{
    m_loan.AssetId = Entity.AssetId;
    m_loan.StartDate = new DateTimeOffset(m_loanStartDate);
    m_loan.AnnualInterestRate = m_annualInterestRate / 100;

    if (m_loan.AssetLoanId == 0)
    {
        // Create new loan (new asset or existing asset adding loan)
        var loanResult = await LoanService.CreateLoanAsync(m_loan, UserName);
        if (!loanResult.Success)
        {
            ShowError($"Asset saved but loan failed: {loanResult.Message}");
            return;
        }
        Entity.AssetLoanId = m_loan.AssetLoanId;
        await AssetService.UpdateAssetAsync(Entity, UserName);
    }
    else
    {
        // Update existing loan
        var loanResult = await LoanService.UpdateLoanAsync(m_loan, UserName);
        if (!loanResult.Success)
        {
            ShowError($"Asset saved but loan update failed: {loanResult.Message}");
            return;
        }
    }
}
```

**Key change**: Condition switches from `m_hasLoan && IsNew` to just `m_hasLoan`, then branches on `m_loan.AssetLoanId == 0` (new loan) vs existing loan.

## Verification Steps

1. Run `dotnet watch` with impersonation as `admin@krabirentals.com` (KrabiBeachRentals test data)
2. Navigate to `https://localhost:7103/finance/assets/lbrew6xY`
3. Fill in financing fields (Lender: GSB, Principal: 40000, Rate: 5%, Term: 60, Start: 01/02/2026, Notes: Test)
4. Click Save → verify no errors
5. Query database to confirm `AssetLoan` record created and `Asset.AssetLoanId` is set
6. Navigate to `https://localhost:7103/finance/assets/lbrew6xY/details` → verify loan amortization table displays
7. Go back to edit page → verify loan fields are populated → change a field → save → verify update persisted

## Files Modified
- `src/MotoRent.Client/Pages/Finance/AssetDialog.razor` (lines 327-344)
