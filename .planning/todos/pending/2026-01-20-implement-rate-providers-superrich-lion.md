---
created: 2026-01-20T12:45
title: Implement IRateProvider for SuperRich, Lion exchange services
area: api
files:
  - src/MotoRent.Services/ExchangeRateService.cs
  - src/MotoRent.Client/Pages/Settings/ExchangeRates.razor
---

## Problem

The exchange rate Source column shows "-" instead of the actual rate source. The current `FetchRatesFromApiAsync` is a stub. Need to implement real rate providers for Thailand's popular forex services:

- SuperRich (green/orange)
- Lion (The Rich)
- K-Plus rates
- Bangkok Bank rates

Users should be able to:
1. Select which rate provider to use (organization setting)
2. See the source name when viewing rates (e.g., "SuperRich", "Manual")
3. Have rates auto-update from selected provider

## Solution

1. Create `IRateProvider` interface with `GetRatesAsync()` method
2. Implement concrete providers:
   - `SuperRichRateProvider` - scrape/API from superrichthailand.com
   - `LionRateProvider` - scrape/API from lionthailand.com
   - `ManualRateProvider` - for manual entry (default)
3. Add provider selection in organization settings
4. Store provider name in ExchangeRate.Source field
5. Display source in Exchange Rates settings page
