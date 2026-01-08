using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class ShopService
{
    private readonly RentalDataContext m_context;

    public ShopService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<LoadOperation<Shop>> GetShopsAsync(
        string? searchTerm = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Shops.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        query = query.OrderBy(s => s.Name);

        var result = await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(s =>
                    (s.Name?.ToLowerInvariant().Contains(term) ?? false) ||
                    (s.Location?.ToLowerInvariant().Contains(term) ?? false) ||
                    (s.Address?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<List<Shop>> GetActiveShopsAsync()
    {
        var result = await m_context.LoadAsync(
            m_context.Shops.Where(s => s.IsActive),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<Shop?> GetShopByIdAsync(int shopId)
    {
        return await m_context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
    }

    public async Task<SubmitOperation> CreateShopAsync(Shop shop, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateShopAsync(Shop shop, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteShopAsync(Shop shop, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(shop);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> SetActiveStatusAsync(int shopId, bool isActive, string username)
    {
        var shop = await GetShopByIdAsync(shopId);
        if (shop == null)
            return SubmitOperation.CreateFailure("Shop not found");

        shop.IsActive = isActive;

        using var session = m_context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("StatusUpdate");
    }

    public async Task<Dictionary<string, int>> GetShopStatisticsAsync(int shopId)
    {
        var stats = new Dictionary<string, int>();

        // Count motorbikes
        var bikes = await m_context.LoadAsync(
            m_context.Motorbikes.Where(m => m.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: true);
        stats["TotalMotorbikes"] = bikes.TotalRows;

        // Count active rentals
        var rentals = await m_context.LoadAsync(
            m_context.Rentals.Where(r => r.ShopId == shopId && r.Status == "Active"),
            page: 1, size: 1000, includeTotalRows: true);
        stats["ActiveRentals"] = rentals.TotalRows;

        // Count renters
        var renters = await m_context.LoadAsync(
            m_context.Renters.Where(r => r.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: true);
        stats["TotalRenters"] = renters.TotalRows;

        return stats;
    }
}
