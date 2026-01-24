# Fix Exchange Rate Lookup for Till Payments

## Problem
Exchange rates configured in "Organization Defaults" (ShopId = null) are not found when processing Till payments. The error "No exchange rate configured for USD" appears despite rates being visible in the Exchange Rate Settings page.

## Root Cause Analysis

**Database State:**
- `DenominationRate` table: Contains USD rates with `ShopId = NULL` (org defaults) and `IsActive = 1`
- `ExchangeRate` table: **EMPTY** - no records

**Code Flow:**
1. `TillService.currency.cs:53` calls `ExchangeRateService.ConvertToThbAsync(currency, foreignAmount)`
2. `ExchangeRateService.cs:167` calls `GetCurrentRateAsync(currency)`
3. `GetCurrentRateAsync` queries the **`ExchangeRate` table** (which is empty)
4. Returns `null` → "No exchange rate configured"

**The Mismatch:**
- The Exchange Rate Settings page uses `DenominationRate` system (supports multiple denomination groups with different rates)
- The Till currency conversion uses the legacy `ExchangeRate` system (single rate per currency)

## Solution

Update `ConvertToThbAsync` to use the `DenominationRate` system with proper shop fallback:

### Changes Required

#### 1. `src/MotoRent.Services/ExchangeRateService.cs`

Add new overload `ConvertToThbAsync(currency, amount, shopId)` that:
- Queries `DenominationRate` table for the currency
- Falls back to org defaults (ShopId = null) if no shop-specific rate exists
- Uses the rate from the first denomination group if available
- **Fallback**: If no rates for specific denominations, use the **lowest rate** for the currency (most conservative for the business)
- Returns `ExchangeConversionResult` with the rate details

```csharp
public async Task<ExchangeConversionResult?> ConvertToThbAsync(string currency, decimal foreignAmount, int? shopId = null)
{
    if (currency == SupportedCurrencies.THB)
        return new ExchangeConversionResult(foreignAmount, 1.0m, "Base", null);

    // Get all denomination rates for this currency (with shop fallback)
    var rates = await this.GetEffectiveRatesForCurrencyAsync(currency, shopId);

    if (!rates.Any())
        return null;

    // Use the lowest buy rate (most conservative - protects business from overpaying)
    var rate = rates.OrderBy(r => r.BuyRate).First();

    var thbAmount = foreignAmount * rate.BuyRate;
    return new ExchangeConversionResult(thbAmount, rate.BuyRate, rate.ProviderCode, rate.DenominationRateId);
}
```

#### 2. `src/MotoRent.Services/TillService.currency.cs`

Update `RecordForeignCurrencyPaymentAsync` to pass `ShopId` from the session:

```csharp
// Line 53 - change from:
var conversion = await this.ExchangeRateService.ConvertToThbAsync(currency, foreignAmount);
// To:
var conversion = await this.ExchangeRateService.ConvertToThbAsync(currency, foreignAmount, session.ShopId);
```

Also update `RecordMultiCurrencyDropAsync` (line 150).

#### 3. `src/MotoRent.Services/TillService.denomination.cs`

Update `SaveDenominationCountAsync` to pass shop ID:

```csharp
// Line 61 - need to get shopId from session first
var conversion = await this.ExchangeRateService.ConvertToThbAsync(breakdown.Currency, breakdown.Total, session.ShopId);
```

#### 4. Update Client Components (Optional - for preview)

These call `ConvertToThbAsync` directly for UI preview. They need shop context:
- `PaymentTerminalPanel.razor:558`
- `TillReceivePaymentDialog.razor:371`
- `TillBookingDepositDialog.razor:557`

These can use a default `null` shopId (org defaults) for preview, or get shopId from their context.

## Files to Modify

1. `src/MotoRent.Services/ExchangeRateService.cs` - Add shopId parameter to `ConvertToThbAsync`
2. `src/MotoRent.Services/TillService.currency.cs` - Pass session.ShopId to conversion
3. `src/MotoRent.Services/TillService.denomination.cs` - Pass session.ShopId to conversion

## Verification

1. Run the application: `dotnet watch --project src/MotoRent.Server`
2. Navigate to `/settings/exchange-rates` and verify USD rates are configured at Organization Defaults
3. Navigate to Till page and try to receive a USD payment
4. Verify the exchange rate is displayed and conversion works
5. Complete a payment and verify transaction records correctly
