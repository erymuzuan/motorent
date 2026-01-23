# Exchange Rate Provider Integration Plan

## Status: IMPLEMENTED

## Overview
Port exchange rate functionality to MotoRent with denomination-specific rates from MamyExchange and SuperRich providers, including delta adjustments per shop.

## Key Design Decisions

### Denomination Grouping
Denominations are grouped together sharing the same rate:
- **USD Large Bills**: 100, 50 (same rate)
- **USD Small Bills**: 20, 10, 5, 1 (same rate)
- **EUR Large Notes**: 500, 200, 100
- **EUR Standard Notes**: 50, 20, 10, 5

Groups are stored in `DenominationGroup` entity and **configurable by admins** through the settings UI. Default seed data provides initial groups that can be customized.

### Shop Override Pattern
- `ShopId = null` → Organization default rate
- `ShopId = N` → Shop-specific override (takes precedence)

---

## Implementation Summary

### Wave 1: Domain Entities (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.Domain/Entities/RateProviderCodes.cs` | Constants for Manual, MamyExchange, SuperRich |
| `src/MotoRent.Domain/Entities/RawExchangeRate.cs` | DTO for provider API responses |
| `src/MotoRent.Domain/Entities/DenominationGroup.cs` | Groups of denominations sharing same rate |
| `src/MotoRent.Domain/Entities/DenominationRate.cs` | Rates with provider data, deltas, computed finals |
| `src/MotoRent.Domain/Entities/RateDelta.cs` | Persisted delta configurations |
| `src/MotoRent.Domain/Entities/Entity.cs` | Added JsonDerivedType attributes |

### Wave 2: Database Scripts (COMPLETED)
| File | Description |
|------|-------------|
| `database/tables/MotoRent.DenominationGroup.sql` | Table with computed columns |
| `database/tables/MotoRent.DenominationRate.sql` | Rate storage with indexes |
| `database/tables/MotoRent.RateDelta.sql` | Delta configuration table |
| `database/tables/MotoRent.DenominationGroup.seed.sql` | Default groups for all currencies |

### Wave 3: Provider Layer (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.Services/ExchangeRateProviders/IExchangeRateProvider.cs` | Provider interface |
| `src/MotoRent.Services/ExchangeRateProviders/MamyExchangeProvider.cs` | Mamy Exchange API (15-min cache) |
| `src/MotoRent.Services/ExchangeRateProviders/SuperRichProvider.cs` | Super Rich API (30-min cache) |

### Wave 4: Service Layer (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.Services/ExchangeRateService.cs` | Changed to partial class |
| `src/MotoRent.Services/ExchangeRateService.Providers.cs` | Provider fetch and rate application |
| `src/MotoRent.Services/ExchangeRateService.Deltas.cs` | Delta CRUD and application |
| `src/MotoRent.Services/ExchangeRateService.Denominations.cs` | Group and rate queries |

### Wave 5: UI Components (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.Client/Components/ExchangeRates/DeltaEditor.razor` | Inline delta value editor |
| `src/MotoRent.Client/Components/ExchangeRates/RateDetailsFlyout.razor` | Rate breakdown and calculator |
| `src/MotoRent.Client/Pages/Settings/ExchangeRateSettings.razor` | Redesigned settings page |

### Wave 6: Admin UI (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.Client/Pages/Settings/DenominationGroupSettings.razor` | Admin page for groups |
| `src/MotoRent.Client/Components/ExchangeRates/DenominationGroupDialog.razor` | Add/edit group dialog |

### Wave 7: DI Registration (COMPLETED)
| File | Description |
|------|-------------|
| `src/MotoRent.SqlServerRepository/ServiceCollectionExtensions.cs` | Repository registrations |
| `src/MotoRent.Server/Program.cs` | HttpClient and provider DI |
| `src/MotoRent.Client/_Imports.razor` | Component namespace import |

---

## API Endpoints

### MamyExchange
- URL: `https://mamyexchange.com/wp-json/currencyxrates/v1/rates`
- Cache TTL: 15 minutes
- Returns: Array of rate objects with currency, buying, selling

### SuperRich
- URL: `https://www.superrichthailand.com/web/api/v1/rates`
- Cache TTL: 30 minutes
- Returns: Object with buy/sell arrays

---

## Key Service Methods

### ExchangeRateService.Providers.cs
```csharp
Task<RawExchangeRate[]> FetchFromProviderAsync(string providerCode);
Task<FetchRatesResult> RefreshRatesFromProviderAsync(string providerCode, int? shopId, string username);
```

### ExchangeRateService.Deltas.cs
```csharp
Task<List<RateDelta>> GetDeltasAsync(int? shopId);
Task<SubmitOperation> SaveDeltaAsync(RateDelta delta, string username);
Task<SubmitOperation> UpdateGroupDeltaAsync(string currency, int groupId, int? shopId, decimal buyDelta, decimal sellDelta, string username);
```

### ExchangeRateService.Denominations.cs
```csharp
Task<List<DenominationGroup>> LoadDenominationGroupsAsync();
Task<List<DenominationRate>> GetDenominationRatesAsync(int? shopId = null);
Task<DenominationRate?> GetRateForDenominationAsync(string currency, decimal denomination, int? shopId = null);
Task<List<RateSummary>> GetRateSummaryAsync(int? shopId = null);
```

---

## UI Features

### ExchangeRateSettings Page
- Provider dropdown (MamyExchange, SuperRich, Manual)
- Refresh rates button
- Buy/Sell toggle (segmented control)
- Shop tabs (if multiple shops)
- Rate cards grouped by currency
- Inline delta editing with badge display
- Rate details flyout on row click

### DenominationGroupSettings Page
- List of groups by currency
- Add/Edit/Delete functionality
- Denomination chips with add/remove
- Sort order configuration

---

## Database Deployment

Run these SQL scripts in order:
1. `database/tables/MotoRent.DenominationGroup.sql`
2. `database/tables/MotoRent.DenominationRate.sql`
3. `database/tables/MotoRent.RateDelta.sql`
4. `database/tables/MotoRent.DenominationGroup.seed.sql`

---

## Verification Checklist

- [ ] Provider connectivity: Fetch rates from MamyExchange and SuperRich APIs
- [ ] Denomination grouping: Verify USD 100+50 get same rate, USD 20+10+5+1 get different rate
- [ ] Delta persistence: Save deltas, refresh rates, verify deltas reapply correctly
- [ ] Shop override: Verify shop-specific deltas override org defaults
- [ ] UI workflow: Test provider switch, refresh, delta editing, flyout display
- [ ] Admin config: Test adding/editing/deleting denomination groups
