using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// ExchangeRateService - Delta CRUD and application.
/// </summary>
public partial class ExchangeRateService
{
    /// <summary>
    /// Gets all rate deltas for a shop (or org defaults if shopId is null).
    /// </summary>
    public async Task<List<RateDelta>> GetDeltasAsync(int? shopId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<RateDelta>()
                .Where(d => d.ShopId == shopId && d.IsActive)
                .OrderBy(d => d.Currency),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets a specific delta by ID.
    /// </summary>
    public async Task<RateDelta?> GetDeltaAsync(int deltaId)
    {
        return await this.Context.LoadOneAsync<RateDelta>(d => d.RateDeltaId == deltaId);
    }

    /// <summary>
    /// Saves or updates a rate delta.
    /// </summary>
    public async Task<SubmitOperation> SaveDeltaAsync(RateDelta delta, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(delta);
        return await session.SubmitChanges("SaveDelta");
    }

    /// <summary>
    /// Deletes a rate delta (soft delete by setting IsActive = false).
    /// </summary>
    public async Task<SubmitOperation> DeleteDeltaAsync(int deltaId, string username)
    {
        var delta = await this.GetDeltaAsync(deltaId);
        if (delta == null)
        {
            return new SubmitOperation { Message = "Delta not found" };
        }

        delta.IsActive = false;
        return await this.SaveDeltaAsync(delta, username);
    }

    /// <summary>
    /// Updates delta for a specific denomination group.
    /// Creates new delta if one doesn't exist, updates existing otherwise.
    /// </summary>
    public async Task<SubmitOperation> UpdateGroupDeltaAsync(
        string currency,
        int groupId,
        int? shopId,
        decimal buyDelta,
        decimal sellDelta,
        string username)
    {
        // Find existing delta
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<RateDelta>()
                .Where(d => d.Currency == currency
                    && d.DenominationGroupId == groupId
                    && d.ShopId == shopId
                    && d.IsActive),
            page: 1, size: 1, includeTotalRows: false);

        var delta = result.ItemCollection.FirstOrDefault();

        if (delta == null)
        {
            // Create new delta
            delta = new RateDelta
            {
                ShopId = shopId,
                Currency = currency,
                DenominationGroupId = groupId,
                BuyDelta = buyDelta,
                SellDelta = sellDelta,
                IsActive = true
            };
        }
        else
        {
            // Update existing
            delta.BuyDelta = buyDelta;
            delta.SellDelta = sellDelta;
        }

        return await this.SaveDeltaAsync(delta, username);
    }

    /// <summary>
    /// Applies deltas to existing denomination rates.
    /// Used when deltas are updated after rates have been fetched.
    /// </summary>
    public async Task<SubmitOperation> ApplyDeltasToRatesAsync(int? shopId, string username)
    {
        var rates = await this.GetDenominationRatesAsync(shopId);
        if (rates.Count == 0)
        {
            return new SubmitOperation { Message = "No rates to update" };
        }

        var deltas = await this.GetDeltasAsync(shopId);
        if (deltas.Count == 0)
        {
            // Also check org deltas if this is a shop
            if (shopId.HasValue)
            {
                deltas = await this.GetDeltasAsync(null);
            }
        }

        if (deltas.Count == 0)
        {
            return new SubmitOperation { Message = "No deltas configured" };
        }

        using var session = this.Context.OpenSession(username);
        var updatedCount = 0;

        foreach (var rate in rates.Where(r => r.IsActive))
        {
            var delta = FindApplicableDelta(deltas, rate.Currency, rate.DenominationGroupId, shopId);
            if (delta == null)
                continue;

            // Only update if delta changed
            if (rate.BuyDelta != delta.BuyDelta || rate.SellDelta != delta.SellDelta)
            {
                rate.BuyDelta = delta.BuyDelta;
                rate.SellDelta = delta.SellDelta;
                session.Attach(rate);
                updatedCount++;
            }
        }

        if (updatedCount == 0)
        {
            return new SubmitOperation { Message = "No rates needed updating" };
        }

        var submitResult = await session.SubmitChanges("ApplyDeltasToRates");
        submitResult.Message = $"Updated {updatedCount} rate(s)";
        return submitResult;
    }

    /// <summary>
    /// Copies org default deltas to a specific shop.
    /// Useful when setting up a new shop with org baseline.
    /// </summary>
    public async Task<SubmitOperation> CopyOrgDeltasToShopAsync(int shopId, string username)
    {
        var orgDeltas = await this.GetDeltasAsync(null);
        if (orgDeltas.Count == 0)
        {
            return new SubmitOperation { Message = "No organization deltas to copy" };
        }

        using var session = this.Context.OpenSession(username);

        foreach (var orgDelta in orgDeltas)
        {
            var shopDelta = new RateDelta
            {
                ShopId = shopId,
                Currency = orgDelta.Currency,
                DenominationGroupId = orgDelta.DenominationGroupId,
                BuyDelta = orgDelta.BuyDelta,
                SellDelta = orgDelta.SellDelta,
                IsActive = true
            };
            session.Attach(shopDelta);
        }

        var result = await session.SubmitChanges("CopyOrgDeltasToShop");
        result.Message = $"Copied {orgDeltas.Count} delta(s) to shop";
        return result;
    }
}
