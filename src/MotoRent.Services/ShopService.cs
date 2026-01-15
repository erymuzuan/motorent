using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class ShopService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Shop>> GetShopsAsync(
        string? searchTerm = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Shop>().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        query = query.OrderBy(s => s.Name);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

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
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Shop>().Where(s => s.IsActive),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<Shop?> GetShopByIdAsync(int shopId)
    {
        return await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
    }

    public async Task<SubmitOperation> CreateShopAsync(Shop shop, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateShopAsync(Shop shop, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteShopAsync(Shop shop, string username)
    {
        // Check if shop has any vehicles (either registered at or currently at this shop)
        var vehicleCount = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => v.HomeShopId == shop.ShopId || v.CurrentShopId == shop.ShopId));
        if (vehicleCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete shop with {vehicleCount} vehicle(s). Please deactivate instead or reassign vehicles first.");
        }

        // Check if shop has any rentals
        var rentalCount = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shop.ShopId || r.ReturnedToShopId == shop.ShopId));
        if (rentalCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete shop with {rentalCount} rental record(s). Please deactivate instead.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(shop);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> SetActiveStatusAsync(int shopId, bool isActive, string username)
    {
        var shop = await this.GetShopByIdAsync(shopId);
        if (shop == null)
            return SubmitOperation.CreateFailure("Shop not found");

        shop.IsActive = isActive;

        using var session = this.Context.OpenSession(username);
        session.Attach(shop);
        return await session.SubmitChanges("StatusUpdate");
    }

    public async Task<Dictionary<string, int>> GetShopStatisticsAsync(int shopId)
    {
        var stats = new Dictionary<string, int>();

        // Count motorbikes (organization-wide)
        var bikes = await this.Context.LoadAsync(
            this.Context.CreateQuery<Motorbike>(),
            page: 1, size: 1000, includeTotalRows: true);
        stats["TotalMotorbikes"] = bikes.TotalRows;

        // Count active rentals
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>().Where(r => r.ShopId == shopId && r.Status == "Active"),
            page: 1, size: 1000, includeTotalRows: true);
        stats["ActiveRentals"] = rentals.TotalRows;

        // Count renters (universal - not shop-specific)
        var renters = await this.Context.LoadAsync(
            this.Context.CreateQuery<Renter>(),
            page: 1, size: 1000, includeTotalRows: true);
        stats["TotalRenters"] = renters.TotalRows;

        return stats;
    }
}
