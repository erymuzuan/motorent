using MotoRent.Domain.Entities;
using MotoRent.Services.ExchangeRateProviders;

namespace MotoRent.Services;

/// <summary>
/// ExchangeRateService - Provider fetch and rate application logic.
/// </summary>
public partial class ExchangeRateService
{
    private IEnumerable<IExchangeRateProvider>? m_providers;

    /// <summary>
    /// Sets the available exchange rate providers.
    /// Called during DI configuration.
    /// </summary>
    public void SetProviders(IEnumerable<IExchangeRateProvider> providers)
    {
        this.m_providers = providers;
    }

    /// <summary>
    /// Gets all registered exchange rate providers.
    /// </summary>
    public IEnumerable<IExchangeRateProvider> GetProviders() => this.m_providers ?? [];

    /// <summary>
    /// Gets a provider by its code.
    /// </summary>
    public IExchangeRateProvider? GetProvider(string providerCode)
    {
        return this.m_providers?.FirstOrDefault(p => p.Code == providerCode);
    }

    /// <summary>
    /// Fetches raw rates from a specific provider.
    /// Does not persist - use RefreshRatesFromProviderAsync to apply and save.
    /// </summary>
    public async Task<RawExchangeRate[]> FetchFromProviderAsync(string providerCode, CancellationToken ct = default)
    {
        var provider = this.GetProvider(providerCode);
        if (provider == null)
            return [];

        return await provider.GetRatesAsync(ct);
    }

    /// <summary>
    /// Refreshes denomination rates from a provider.
    /// Applies existing deltas and saves to database.
    /// </summary>
    /// <param name="providerCode">Provider to fetch from</param>
    /// <param name="shopId">Shop ID (null for org default)</param>
    /// <param name="username">User performing the refresh</param>
    /// <returns>Result with success status and count of rates updated</returns>
    public async Task<FetchRatesResult> RefreshRatesFromProviderAsync(
        string providerCode,
        int? shopId,
        string username)
    {
        // Fetch raw rates from provider
        var rawRates = await this.FetchFromProviderAsync(providerCode);
        if (rawRates.Length == 0)
        {
            return new FetchRatesResult(false, "No rates returned from provider", 0);
        }

        // Load denomination groups
        var groups = await this.LoadDenominationGroupsAsync();
        if (groups.Count == 0)
        {
            return new FetchRatesResult(false, "No denomination groups configured", 0);
        }

        // Load existing deltas for reapplication
        var deltas = await this.GetDeltasAsync(shopId);

        // Get existing rates for this shop to deactivate
        var existingRates = await this.GetDenominationRatesAsync(shopId);

        using var session = this.Context.OpenSession(username);
        var updatedCount = 0;

        foreach (var group in groups.Where(g => g.IsActive))
        {
            // Find matching raw rate for this currency/denomination group
            var matchingRaw = FindMatchingRawRate(rawRates, group);
            if (matchingRaw == null)
                continue;

            // Find applicable delta
            var delta = FindApplicableDelta(deltas, group.Currency, group.DenominationGroupId, shopId);

            // Deactivate existing rate for this group
            var existingRate = existingRates.FirstOrDefault(r =>
                r.DenominationGroupId == group.DenominationGroupId && r.ShopId == shopId);
            if (existingRate != null)
            {
                existingRate.IsActive = false;
                existingRate.ExpiresOn = DateTimeOffset.Now;
                session.Attach(existingRate);
            }

            // Create new denomination rate
            var newRate = new DenominationRate
            {
                ShopId = shopId,
                Currency = group.Currency,
                DenominationGroupId = group.DenominationGroupId,
                GroupName = group.GroupName,
                DenominationsDisplay = group.DenominationsDisplay,
                ProviderCode = providerCode,
                ProviderBuyRate = matchingRaw.Buying,
                ProviderSellRate = matchingRaw.Selling,
                ProviderUpdatedOn = matchingRaw.UpdatedOn,
                BuyDelta = delta?.BuyDelta ?? 0,
                SellDelta = delta?.SellDelta ?? 0,
                EffectiveDate = DateTimeOffset.Now,
                IsActive = true
            };

            session.Attach(newRate);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            var result = await session.SubmitChanges("RefreshRatesFromProvider");
            if (!result.Success)
            {
                return new FetchRatesResult(false, result.Message ?? "Failed to save rates", 0);
            }
        }

        return new FetchRatesResult(true, $"Updated {updatedCount} rates from {providerCode}", updatedCount);
    }

    /// <summary>
    /// Finds the best matching raw rate for a denomination group.
    /// Matches by currency, then by denomination overlap.
    /// </summary>
    private static RawExchangeRate? FindMatchingRawRate(RawExchangeRate[] rawRates, DenominationGroup group)
    {
        // Filter to same currency
        var currencyRates = rawRates.Where(r => r.Currency == group.Currency).ToList();
        if (currencyRates.Count == 0)
            return null;

        // If only one rate for this currency, use it
        if (currencyRates.Count == 1)
            return currencyRates[0];

        // Try to match by denomination overlap
        foreach (var rate in currencyRates)
        {
            if (rate.Denominations.Count > 0)
            {
                var overlap = rate.Denominations.Intersect(group.Denominations).Any();
                if (overlap)
                    return rate;
            }
        }

        // Try to match by group hint
        foreach (var rate in currencyRates)
        {
            if (!string.IsNullOrEmpty(rate.GroupHint) &&
                group.GroupName.Contains(rate.GroupHint, StringComparison.OrdinalIgnoreCase))
            {
                return rate;
            }
        }

        // Fallback: for "large bills" groups, prefer higher rates; for "small bills", prefer lower
        var groupNameLower = group.GroupName.ToLowerInvariant();
        if (groupNameLower.Contains("large"))
        {
            return currencyRates.OrderByDescending(r => r.Buying).First();
        }
        else if (groupNameLower.Contains("small"))
        {
            return currencyRates.OrderBy(r => r.Buying).First();
        }

        // Default to first rate
        return currencyRates[0];
    }

    /// <summary>
    /// Finds the most specific applicable delta for a currency/group/shop combination.
    /// Priority: Shop+Group specific > Shop+Currency > Org+Group > Org+Currency
    /// </summary>
    private static RateDelta? FindApplicableDelta(
        List<RateDelta> deltas,
        string currency,
        int groupId,
        int? shopId)
    {
        // Shop + Group specific (most specific)
        if (shopId.HasValue)
        {
            var shopGroupDelta = deltas.FirstOrDefault(d =>
                d.ShopId == shopId && d.Currency == currency && d.DenominationGroupId == groupId);
            if (shopGroupDelta != null)
                return shopGroupDelta;

            // Shop + Currency (all groups)
            var shopCurrencyDelta = deltas.FirstOrDefault(d =>
                d.ShopId == shopId && d.Currency == currency && d.DenominationGroupId == null);
            if (shopCurrencyDelta != null)
                return shopCurrencyDelta;
        }

        // Org + Group specific
        var orgGroupDelta = deltas.FirstOrDefault(d =>
            d.ShopId == null && d.Currency == currency && d.DenominationGroupId == groupId);
        if (orgGroupDelta != null)
            return orgGroupDelta;

        // Org + Currency (all groups)
        var orgCurrencyDelta = deltas.FirstOrDefault(d =>
            d.ShopId == null && d.Currency == currency && d.DenominationGroupId == null);
        return orgCurrencyDelta;
    }
}
