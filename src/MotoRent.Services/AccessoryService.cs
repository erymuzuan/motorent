using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class AccessoryService
{
    private readonly RentalDataContext m_context;

    public AccessoryService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<LoadOperation<Accessory>> GetAccessoriesAsync(
        int shopId,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Accessories
            .Where(a => a.ShopId == shopId)
            .OrderByDescending(a => a.AccessoryId);

        var result = await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);

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
        return await m_context.LoadOneAsync<Accessory>(a => a.AccessoryId == accessoryId);
    }

    public async Task<List<Accessory>> GetAvailableAccessoriesAsync(int shopId)
    {
        var result = await m_context.LoadAsync(
            m_context.Accessories
                .Where(a => a.ShopId == shopId && a.QuantityAvailable > 0),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }

    public async Task<SubmitOperation> CreateAccessoryAsync(Accessory accessory, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(accessory);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateAccessoryAsync(Accessory accessory, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(accessory);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteAccessoryAsync(Accessory accessory, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(accessory);
        return await session.SubmitChanges("Delete");
    }

    public async Task<(int Total, int IncludedFree)> GetAccessoryCountsAsync(int shopId)
    {
        var allAccessories = await m_context.LoadAsync(
            m_context.Accessories.Where(a => a.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: false);

        var total = allAccessories.ItemCollection.Count;
        var includedFree = allAccessories.ItemCollection.Count(a => a.IsIncluded);

        return (total, includedFree);
    }
}
