using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// ExchangeRateService - Denomination group and rate queries.
/// </summary>
public partial class ExchangeRateService
{
    // Denomination Groups

    /// <summary>
    /// Loads all active denomination groups, ordered by currency and sort order.
    /// </summary>
    public async Task<List<DenominationGroup>> LoadDenominationGroupsAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<DenominationGroup>()
                .Where(g => g.IsActive)
                .OrderBy(g => g.Currency)
                .ThenBy(g => g.SortOrder),
            page: 1, size: 200, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets denomination groups for a specific currency.
    /// </summary>
    public async Task<List<DenominationGroup>> GetDenominationGroupsForCurrencyAsync(string currency)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<DenominationGroup>()
                .Where(g => g.Currency == currency && g.IsActive)
                .OrderBy(g => g.SortOrder),
            page: 1, size: 20, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets a specific denomination group by ID.
    /// </summary>
    public async Task<DenominationGroup?> GetDenominationGroupAsync(int groupId)
    {
        return await this.Context.LoadOneAsync<DenominationGroup>(g => g.DenominationGroupId == groupId);
    }

    /// <summary>
    /// Saves or updates a denomination group.
    /// </summary>
    public async Task<SubmitOperation> SaveDenominationGroupAsync(DenominationGroup group, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(group);
        return await session.SubmitChanges("SaveDenominationGroup");
    }

    /// <summary>
    /// Deletes a denomination group (soft delete).
    /// </summary>
    public async Task<SubmitOperation> DeleteDenominationGroupAsync(int groupId, string username)
    {
        var group = await this.GetDenominationGroupAsync(groupId);
        if (group == null)
        {
            return new SubmitOperation { Message = "Group not found" };
        }

        group.IsActive = false;
        return await this.SaveDenominationGroupAsync(group, username);
    }

    /// <summary>
    /// Finds the denomination group that contains a specific denomination value.
    /// </summary>
    public async Task<DenominationGroup?> FindGroupForDenominationAsync(string currency, decimal denomination)
    {
        var groups = await this.GetDenominationGroupsForCurrencyAsync(currency);
        return groups.FirstOrDefault(g => g.Denominations.Contains(denomination));
    }

    // Denomination Rates

    /// <summary>
    /// Gets all denomination rates for a shop (or org defaults if shopId is null).
    /// </summary>
    public async Task<List<DenominationRate>> GetDenominationRatesAsync(int? shopId = null)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<DenominationRate>()
                .Where(r => r.ShopId == shopId && r.IsActive)
                .OrderBy(r => r.Currency),
            page: 1, size: 200, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets the current rate for a specific denomination and shop.
    /// Falls back to org default if no shop-specific rate exists.
    /// </summary>
    public async Task<DenominationRate?> GetRateForDenominationAsync(
        string currency,
        decimal denomination,
        int? shopId = null)
    {
        // Find the group containing this denomination
        var group = await this.FindGroupForDenominationAsync(currency, denomination);
        if (group == null)
            return null;

        // Try shop-specific rate first
        if (shopId.HasValue)
        {
            var shopRate = await this.Context.LoadOneAsync<DenominationRate>(r =>
                r.ShopId == shopId &&
                r.DenominationGroupId == group.DenominationGroupId &&
                r.IsActive);

            if (shopRate != null)
                return shopRate;
        }

        // Fall back to org default
        return await this.Context.LoadOneAsync<DenominationRate>(r =>
            r.ShopId == null &&
            r.DenominationGroupId == group.DenominationGroupId &&
            r.IsActive);
    }

    /// <summary>
    /// Gets all denomination rates for a currency and shop.
    /// Returns shop-specific rates with org defaults as fallback.
    /// </summary>
    public async Task<List<DenominationRate>> GetEffectiveRatesForCurrencyAsync(string currency, int? shopId = null)
    {
        var groups = await this.GetDenominationGroupsForCurrencyAsync(currency);
        var rates = new List<DenominationRate>();

        foreach (var group in groups)
        {
            var rate = await this.GetRateForGroupAsync(group.DenominationGroupId, shopId);
            if (rate != null)
                rates.Add(rate);
        }

        return rates;
    }

    /// <summary>
    /// Gets the rate for a specific denomination group and shop.
    /// </summary>
    private async Task<DenominationRate?> GetRateForGroupAsync(int groupId, int? shopId)
    {
        // Try shop-specific rate first
        if (shopId.HasValue)
        {
            var shopRate = await this.Context.LoadOneAsync<DenominationRate>(r =>
                r.ShopId == shopId &&
                r.DenominationGroupId == groupId &&
                r.IsActive);

            if (shopRate != null)
                return shopRate;
        }

        // Fall back to org default
        return await this.Context.LoadOneAsync<DenominationRate>(r =>
            r.ShopId == null &&
            r.DenominationGroupId == groupId &&
            r.IsActive);
    }

    /// <summary>
    /// Saves or updates a denomination rate.
    /// </summary>
    public async Task<SubmitOperation> SaveDenominationRateAsync(DenominationRate rate, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rate);
        return await session.SubmitChanges("SaveDenominationRate");
    }

    /// <summary>
    /// Sets a manual rate for a denomination group.
    /// Used when not using a provider.
    /// </summary>
    public async Task<SubmitOperation> SetManualRateAsync(
        string currency,
        int groupId,
        int? shopId,
        decimal buyRate,
        decimal sellRate,
        string username)
    {
        var group = await this.GetDenominationGroupAsync(groupId);
        if (group == null)
        {
            return new SubmitOperation { Message = "Denomination group not found" };
        }

        // Deactivate existing rate
        var existingRate = await this.GetRateForGroupAsync(groupId, shopId);
        if (existingRate != null)
        {
            existingRate.IsActive = false;
            existingRate.ExpiresOn = DateTimeOffset.Now;
        }

        // Create new rate
        var newRate = new DenominationRate
        {
            ShopId = shopId,
            Currency = currency,
            DenominationGroupId = groupId,
            GroupName = group.GroupName,
            DenominationsDisplay = group.DenominationsDisplay,
            ProviderCode = RateProviderCodes.Manual,
            ProviderBuyRate = buyRate,
            ProviderSellRate = sellRate,
            ProviderUpdatedOn = DateTimeOffset.Now,
            BuyDelta = 0,
            SellDelta = 0,
            EffectiveDate = DateTimeOffset.Now,
            IsActive = true
        };

        using var session = this.Context.OpenSession(username);
        if (existingRate != null)
            session.Attach(existingRate);
        session.Attach(newRate);
        return await session.SubmitChanges("SetManualRate");
    }

    // Rate Display Helpers

    /// <summary>
    /// Gets a summary of current rates for display.
    /// Groups rates by currency with shop override indicators.
    /// </summary>
    public async Task<List<RateSummary>> GetRateSummaryAsync(int? shopId = null)
    {
        var groups = await this.LoadDenominationGroupsAsync();
        var orgRates = await this.GetDenominationRatesAsync(null);
        var shopRates = shopId.HasValue ? await this.GetDenominationRatesAsync(shopId) : [];

        var summaries = new List<RateSummary>();

        foreach (var group in groups)
        {
            var shopRate = shopRates.FirstOrDefault(r => r.DenominationGroupId == group.DenominationGroupId);
            var orgRate = orgRates.FirstOrDefault(r => r.DenominationGroupId == group.DenominationGroupId);
            var effectiveRate = shopRate ?? orgRate;

            if (effectiveRate != null)
            {
                summaries.Add(new RateSummary(
                    effectiveRate.Currency,
                    group.GroupName,
                    group.DenominationsDisplay,
                    effectiveRate.BuyRate,
                    effectiveRate.SellRate,
                    effectiveRate.ProviderCode,
                    effectiveRate.ProviderUpdatedOn,
                    shopRate != null,
                    effectiveRate.BuyDelta,
                    effectiveRate.SellDelta));
            }
        }

        return summaries;
    }
}

/// <summary>
/// Summary of a denomination rate for display purposes.
/// </summary>
public record RateSummary(
    string Currency,
    string GroupName,
    string Denominations,
    decimal BuyRate,
    decimal SellRate,
    string ProviderCode,
    DateTimeOffset? ProviderUpdatedOn,
    bool IsShopOverride,
    decimal BuyDelta,
    decimal SellDelta);
