using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class AccessoryService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Accessory>> GetAccessoriesAsync(
        int shopId,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Accessory>();

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            query = query.Where(a => a.ShopId == shopId);
        }

        query = query.OrderByDescending(a => a.AccessoryId);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory (for name)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(a => a.Name?.ToLowerInvariant().Contains(term) ?? false)
                .ToList();
        }

        return result;
    }

    public async Task<Accessory?> GetAccessoryByIdAsync(int accessoryId)
    {
        return await this.Context.LoadOneAsync<Accessory>(a => a.AccessoryId == accessoryId);
    }

    public async Task<List<Accessory>> GetAvailableAccessoriesAsync(int shopId)
    {
        var query = this.Context.CreateQuery<Accessory>()
            .Where(a => a.QuantityAvailable > 0);

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            query = query.Where(a => a.ShopId == shopId);
        }

        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }

    public async Task<SubmitOperation> CreateAccessoryAsync(Accessory accessory, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(accessory);
        return await session.SubmitChanges("CreateAccessory");
    }

    public async Task<SubmitOperation> UpdateAccessoryAsync(Accessory accessory, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(accessory);
        return await session.SubmitChanges("UpdateAccessory");
    }

    public async Task<SubmitOperation> DeleteAccessoryAsync(Accessory accessory, string username)
    {
        // Check if any rentals use this accessory
        var rentalCount = await this.Context.GetCountAsync(
            this.Context.CreateQuery<RentalAccessory>().Where(ra => ra.AccessoryId == accessory.AccessoryId));
        if (rentalCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete accessory with {rentalCount} rental usage(s). Please remove from inventory instead.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(accessory);
        return await session.SubmitChanges("DeleteAccessory");
    }

    public async Task<(int Total, int IncludedFree)> GetAccessoryCountsAsync(int shopId)
    {
        int total;
        int includedFree;

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            total = await this.Context.GetCountAsync<Accessory>(a => a.ShopId == shopId);
            includedFree = await this.Context.GetCountAsync<Accessory>(
                a => a.ShopId == shopId && a.IsIncluded);
        }
        else
        {
            total = await this.Context.GetCountAsync<Accessory>(a => true);
            includedFree = await this.Context.GetCountAsync<Accessory>(a => a.IsIncluded);
        }

        return (total, includedFree);
    }
}
