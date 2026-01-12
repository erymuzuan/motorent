using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing vehicle pools - groups of shops that share vehicle inventory.
/// </summary>
public class VehiclePoolService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    /// <summary>
    /// Gets all vehicle pools with optional filtering.
    /// </summary>
    public async Task<LoadOperation<VehiclePool>> GetPoolsAsync(
        string? searchTerm = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<VehiclePool>().OrderBy(p => p.Name);

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(p =>
                    (p.Name?.ToLowerInvariant().Contains(term) ?? false) ||
                    (p.Description?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Gets all active pools.
    /// </summary>
    public async Task<List<VehiclePool>> GetActivePoolsAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehiclePool>().Where(p => p.IsActive),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection;
    }

    /// <summary>
    /// Gets a pool by ID.
    /// </summary>
    public async Task<VehiclePool?> GetPoolByIdAsync(int vehiclePoolId)
    {
        return await this.Context.LoadOneAsync<VehiclePool>(p => p.VehiclePoolId == vehiclePoolId);
    }

    /// <summary>
    /// Gets pools that a specific shop belongs to.
    /// </summary>
    public async Task<List<VehiclePool>> GetPoolsForShopAsync(int shopId)
    {
        var allPools = await GetActivePoolsAsync();
        return allPools.Where(p => p.ShopIds.Contains(shopId)).ToList();
    }

    /// <summary>
    /// Gets all shops in a pool.
    /// </summary>
    public async Task<List<Shop>> GetShopsInPoolAsync(int vehiclePoolId)
    {
        var pool = await GetPoolByIdAsync(vehiclePoolId);

        if (pool == null || pool.ShopIds.Count == 0)
            return [];

        var shops = await this.Context.LoadAsync(
            this.Context.CreateQuery<Shop>().Where(s => s.IsActive),
            page: 1, size: 100, includeTotalRows: false);

        return shops.ItemCollection
            .Where(s => pool.ShopIds.Contains(s.ShopId))
            .ToList();
    }

    /// <summary>
    /// Gets all shop IDs that share pools with the given shop.
    /// This returns the shop itself plus all shops in any pool it belongs to.
    /// </summary>
    public async Task<List<int>> GetPooledShopIdsAsync(int shopId)
    {
        var pools = await GetPoolsForShopAsync(shopId);
        var shopIds = new HashSet<int> { shopId };

        foreach (var pool in pools)
        {
            foreach (var id in pool.ShopIds)
            {
                shopIds.Add(id);
            }
        }

        return shopIds.ToList();
    }

    /// <summary>
    /// Creates a new vehicle pool.
    /// </summary>
    public async Task<SubmitOperation> CreatePoolAsync(VehiclePool pool, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(pool);
        return await session.SubmitChanges("Create");
    }

    /// <summary>
    /// Updates an existing vehicle pool.
    /// </summary>
    public async Task<SubmitOperation> UpdatePoolAsync(VehiclePool pool, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(pool);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Deletes a vehicle pool.
    /// </summary>
    public async Task<SubmitOperation> DeletePoolAsync(VehiclePool pool, string username)
    {
        // Check if any vehicles are assigned to this pool
        var vehiclesInPool = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => v.VehiclePoolId == pool.VehiclePoolId));

        if (vehiclesInPool > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete pool with {vehiclesInPool} assigned vehicle(s). Remove vehicles first.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(pool);
        return await session.SubmitChanges("Delete");
    }

    /// <summary>
    /// Adds a shop to a pool.
    /// </summary>
    public async Task<SubmitOperation> AddShopToPoolAsync(int vehiclePoolId, int shopId, string username)
    {
        var pool = await GetPoolByIdAsync(vehiclePoolId);

        if (pool == null)
            return SubmitOperation.CreateFailure("Pool not found");

        if (pool.ShopIds.Contains(shopId))
            return SubmitOperation.CreateFailure("Shop already in pool");

        // Verify the shop exists
        var shop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
        if (shop == null)
            return SubmitOperation.CreateFailure("Shop not found");

        pool.ShopIds.Add(shopId);

        using var session = this.Context.OpenSession(username);
        session.Attach(pool);
        return await session.SubmitChanges("AddShopToPool");
    }

    /// <summary>
    /// Removes a shop from a pool.
    /// </summary>
    public async Task<SubmitOperation> RemoveShopFromPoolAsync(int vehiclePoolId, int shopId, string username)
    {
        var pool = await GetPoolByIdAsync(vehiclePoolId);

        if (pool == null)
            return SubmitOperation.CreateFailure("Pool not found");

        if (!pool.ShopIds.Contains(shopId))
            return SubmitOperation.CreateFailure("Shop not in pool");

        // Check if any vehicles at this shop are in this pool
        var vehiclesAtShop = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.VehiclePoolId == vehiclePoolId)
                .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId));

        if (vehiclesAtShop > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot remove shop with {vehiclesAtShop} pool vehicle(s). Reassign vehicles first.");
        }

        pool.ShopIds.Remove(shopId);

        using var session = this.Context.OpenSession(username);
        session.Attach(pool);
        return await session.SubmitChanges("RemoveShopFromPool");
    }

    /// <summary>
    /// Gets pool statistics including vehicle counts per shop.
    /// </summary>
    public async Task<PoolStatistics> GetPoolStatisticsAsync(int vehiclePoolId)
    {
        var pool = await GetPoolByIdAsync(vehiclePoolId);
        if (pool == null)
            return new PoolStatistics();

        var vehicles = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => v.VehiclePoolId == vehiclePoolId),
            page: 1, size: 1000, includeTotalRows: false);

        var stats = new PoolStatistics
        {
            TotalVehicles = vehicles.ItemCollection.Count,
            AvailableVehicles = vehicles.ItemCollection.Count(v => v.Status == VehicleStatus.Available),
            RentedVehicles = vehicles.ItemCollection.Count(v => v.Status == VehicleStatus.Rented),
            MaintenanceVehicles = vehicles.ItemCollection.Count(v => v.Status == VehicleStatus.Maintenance)
        };

        // Group by current shop
        foreach (var shopId in pool.ShopIds)
        {
            var shopVehicles = vehicles.ItemCollection.Where(v => v.CurrentShopId == shopId).ToList();
            stats.VehiclesByShop[shopId] = new ShopVehicleCount
            {
                Total = shopVehicles.Count,
                Available = shopVehicles.Count(v => v.Status == VehicleStatus.Available),
                Rented = shopVehicles.Count(v => v.Status == VehicleStatus.Rented),
                Maintenance = shopVehicles.Count(v => v.Status == VehicleStatus.Maintenance)
            };
        }

        return stats;
    }
}

/// <summary>
/// Statistics for a vehicle pool.
/// </summary>
public class PoolStatistics
{
    public int TotalVehicles { get; set; }
    public int AvailableVehicles { get; set; }
    public int RentedVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public Dictionary<int, ShopVehicleCount> VehiclesByShop { get; set; } = new();
}

/// <summary>
/// Vehicle counts for a single shop in a pool.
/// </summary>
public class ShopVehicleCount
{
    public int Total { get; set; }
    public int Available { get; set; }
    public int Rented { get; set; }
    public int Maintenance { get; set; }
}
