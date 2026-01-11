using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing vehicles (replaces MotorbikeService with multi-vehicle type support).
/// </summary>
public class VehicleService(RentalDataContext context, VehiclePoolService poolService)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService PoolService { get; } = poolService;

    #region Query Methods

    /// <summary>
    /// Gets vehicles with filtering options.
    /// </summary>
    public async Task<LoadOperation<Vehicle>> GetVehiclesAsync(
        int shopId,
        VehicleType? vehicleType = null,
        VehicleStatus? status = null,
        string? searchTerm = null,
        bool includePooled = false,
        int page = 1,
        int pageSize = 20)
    {
        if (includePooled)
        {
            return await GetVehiclesWithPooledAsync(shopId, vehicleType, status, searchTerm, page, pageSize);
        }

        // Non-pooled: only vehicles at this specific shop
        var query = this.Context.Vehicles
            .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId);

        if (vehicleType.HasValue)
        {
            query = query.Where(v => v.VehicleType == vehicleType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        query = query.OrderByDescending(v => v.VehicleId);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            result.ItemCollection = ApplySearchFilter(result.ItemCollection, searchTerm);
        }

        return result;
    }

    /// <summary>
    /// Gets vehicles including those from shared pools.
    /// </summary>
    private async Task<LoadOperation<Vehicle>> GetVehiclesWithPooledAsync(
        int shopId,
        VehicleType? vehicleType,
        VehicleStatus? status,
        string? searchTerm,
        int page,
        int pageSize)
    {
        // Get all shop IDs that share pools with this shop
        var pooledShopIds = await PoolService.GetPooledShopIdsAsync(shopId);

        // Query vehicles at any of the pooled shops OR vehicles in pools accessible to this shop
        var query = this.Context.Vehicles
            .Where(v =>
                pooledShopIds.Contains(v.CurrentShopId) ||
                (v.VehiclePoolId != null && v.VehiclePoolId > 0));

        if (vehicleType.HasValue)
        {
            query = query.Where(v => v.VehicleType == vehicleType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        query = query.OrderByDescending(v => v.VehicleId);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Filter to only include pooled vehicles whose pools include this shop
        var pools = await PoolService.GetPoolsForShopAsync(shopId);
        var accessiblePoolIds = pools.Select(p => p.VehiclePoolId).ToHashSet();

        result.ItemCollection = result.ItemCollection
            .Where(v =>
                // Vehicle is at this shop
                v.CurrentShopId == shopId ||
                // Vehicle is in an accessible pool
                (v.VehiclePoolId.HasValue && accessiblePoolIds.Contains(v.VehiclePoolId.Value)))
            .ToList();

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            result.ItemCollection = ApplySearchFilter(result.ItemCollection, searchTerm);
        }

        return result;
    }

    /// <summary>
    /// Gets available vehicles for rental at a shop, including pooled vehicles.
    /// This is the primary query for staff when creating a new rental.
    /// </summary>
    public async Task<List<VehicleAvailability>> GetAvailableVehiclesForShopAsync(
        int shopId,
        VehicleType? vehicleType = null,
        bool includePooledVehicles = true)
    {
        var results = new List<VehicleAvailability>();

        // 1. Get vehicles at this shop (non-pooled)
        var localQuery = this.Context.Vehicles
            .Where(v => v.CurrentShopId == shopId)
            .Where(v => v.VehiclePoolId == null || v.VehiclePoolId == 0)
            .Where(v => v.Status == VehicleStatus.Available);

        if (vehicleType.HasValue)
        {
            localQuery = localQuery.Where(v => v.VehicleType == vehicleType.Value);
        }

        var localVehicles = await this.Context.LoadAsync(localQuery, 1, 500, false);
        results.AddRange(localVehicles.ItemCollection.Select(v => new VehicleAvailability
        {
            Vehicle = v,
            IsPooled = false,
            IsAtCurrentShop = true,
            CurrentLocationShopId = shopId
        }));

        if (!includePooledVehicles)
            return results;

        // 2. Get pooled vehicles accessible to this shop
        var pools = await PoolService.GetPoolsForShopAsync(shopId);
        var accessiblePoolIds = pools.Select(p => p.VehiclePoolId).ToHashSet();

        if (accessiblePoolIds.Count > 0)
        {
            var pooledQuery = this.Context.Vehicles
                .Where(v => v.VehiclePoolId != null && v.VehiclePoolId > 0)
                .Where(v => v.Status == VehicleStatus.Available);

            if (vehicleType.HasValue)
            {
                pooledQuery = pooledQuery.Where(v => v.VehicleType == vehicleType.Value);
            }

            var pooledVehicles = await this.Context.LoadAsync(pooledQuery, 1, 500, false);

            foreach (var vehicle in pooledVehicles.ItemCollection)
            {
                // Check if this vehicle's pool is accessible to the current shop
                if (vehicle.VehiclePoolId.HasValue && accessiblePoolIds.Contains(vehicle.VehiclePoolId.Value))
                {
                    results.Add(new VehicleAvailability
                    {
                        Vehicle = vehicle,
                        IsPooled = true,
                        IsAtCurrentShop = vehicle.CurrentShopId == shopId,
                        CurrentLocationShopId = vehicle.CurrentShopId
                    });
                }
            }
        }

        return results
            .OrderBy(r => !r.IsAtCurrentShop)  // Local vehicles first
            .ThenBy(r => r.Vehicle.VehicleType)
            .ThenBy(r => r.Vehicle.Brand)
            .ThenBy(r => r.Vehicle.Model)
            .ToList();
    }

    /// <summary>
    /// Gets a vehicle by ID.
    /// </summary>
    public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId)
    {
        return await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);
    }

    /// <summary>
    /// Gets vehicles by type at a specific shop.
    /// </summary>
    public async Task<List<Vehicle>> GetVehiclesByTypeAsync(int shopId, VehicleType vehicleType)
    {
        var result = await this.Context.LoadAsync(
            this.Context.Vehicles
                .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId)
                .Where(v => v.VehicleType == vehicleType),
            page: 1, size: 500, includeTotalRows: false);

        return result.ItemCollection;
    }

    /// <summary>
    /// Gets status counts for vehicles at a shop.
    /// </summary>
    public async Task<Dictionary<VehicleStatus, int>> GetStatusCountsAsync(int shopId, VehicleType? vehicleType = null)
    {
        var query = this.Context.Vehicles
            .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId);

        if (vehicleType.HasValue)
        {
            query = query.Where(v => v.VehicleType == vehicleType.Value);
        }

        var allVehicles = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        return allVehicles.ItemCollection
            .GroupBy(v => v.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets vehicle type counts at a shop.
    /// </summary>
    public async Task<Dictionary<VehicleType, int>> GetTypeCountsAsync(int shopId)
    {
        var allVehicles = await this.Context.LoadAsync(
            this.Context.Vehicles.Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId),
            page: 1, size: 1000, includeTotalRows: false);

        return allVehicles.ItemCollection
            .GroupBy(v => v.VehicleType)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Creates a new vehicle.
    /// </summary>
    public async Task<SubmitOperation> CreateVehicleAsync(Vehicle vehicle, string username)
    {
        // Set CurrentShopId to HomeShopId if not set
        if (vehicle.CurrentShopId == 0)
        {
            vehicle.CurrentShopId = vehicle.HomeShopId;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("Create");
    }

    /// <summary>
    /// Updates an existing vehicle.
    /// </summary>
    public async Task<SubmitOperation> UpdateVehicleAsync(Vehicle vehicle, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Deletes a vehicle.
    /// </summary>
    public async Task<SubmitOperation> DeleteVehicleAsync(Vehicle vehicle, string username)
    {
        // Check for active rentals
        var activeRentals = await this.Context.GetCountAsync(
            this.Context.Rentals
                .Where(r => r.VehicleId == vehicle.VehicleId)
                .Where(r => r.Status == "Active" || r.Status == "Reserved"));

        if (activeRentals > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete vehicle with {activeRentals} active/reserved rental(s).");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(vehicle);
        return await session.SubmitChanges("Delete");
    }

    /// <summary>
    /// Updates vehicle status.
    /// </summary>
    public async Task<SubmitOperation> UpdateStatusAsync(int vehicleId, VehicleStatus status, string username)
    {
        var vehicle = await GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        vehicle.Status = status;

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("StatusUpdate");
    }

    #endregion

    #region Pool Operations

    /// <summary>
    /// Assigns a vehicle to a pool.
    /// </summary>
    public async Task<SubmitOperation> AssignToPoolAsync(int vehicleId, int vehiclePoolId, string username)
    {
        var vehicle = await GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        if (vehicle.Status == VehicleStatus.Rented)
            return SubmitOperation.CreateFailure("Cannot assign rented vehicle to pool");

        // Verify the home shop is part of this pool
        var pool = await PoolService.GetPoolByIdAsync(vehiclePoolId);
        if (pool == null)
            return SubmitOperation.CreateFailure("Pool not found");

        if (!pool.ShopIds.Contains(vehicle.HomeShopId))
            return SubmitOperation.CreateFailure("Vehicle's home shop must be part of the pool");

        vehicle.VehiclePoolId = vehiclePoolId;

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("AssignToPool");
    }

    /// <summary>
    /// Removes a vehicle from its pool.
    /// </summary>
    public async Task<SubmitOperation> RemoveFromPoolAsync(int vehicleId, string username)
    {
        var vehicle = await GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        if (vehicle.Status == VehicleStatus.Rented)
            return SubmitOperation.CreateFailure("Cannot remove rented vehicle from pool");

        // If not at home shop, warn user
        if (vehicle.CurrentShopId != vehicle.HomeShopId)
        {
            return SubmitOperation.CreateFailure(
                "Vehicle is currently at a different shop. Return to home shop first.");
        }

        vehicle.VehiclePoolId = null;

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("RemoveFromPool");
    }

    /// <summary>
    /// Updates vehicle location (called when returned to different shop).
    /// </summary>
    public async Task<SubmitOperation> UpdateLocationAsync(int vehicleId, int newShopId, string username)
    {
        var vehicle = await GetVehicleByIdAsync(vehicleId);
        if (vehicle == null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        // Verify new shop is in same pool (if pooled)
        if (vehicle.VehiclePoolId.HasValue && vehicle.VehiclePoolId > 0)
        {
            var pool = await PoolService.GetPoolByIdAsync(vehicle.VehiclePoolId.Value);
            if (pool != null && !pool.ShopIds.Contains(newShopId))
            {
                return SubmitOperation.CreateFailure("Cannot move vehicle to shop outside its pool");
            }
        }
        else
        {
            // Non-pooled vehicle can only be at its home shop
            if (newShopId != vehicle.HomeShopId)
            {
                return SubmitOperation.CreateFailure("Non-pooled vehicle can only be at its home shop");
            }
        }

        vehicle.CurrentShopId = newShopId;

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("UpdateLocation");
    }

    #endregion

    #region Helper Methods

    private static List<Vehicle> ApplySearchFilter(List<Vehicle> vehicles, string searchTerm)
    {
        var term = searchTerm.ToLowerInvariant();
        return vehicles
            .Where(v =>
                (v.LicensePlate?.ToLowerInvariant().Contains(term) ?? false) ||
                (v.Brand?.ToLowerInvariant().Contains(term) ?? false) ||
                (v.Model?.ToLowerInvariant().Contains(term) ?? false))
            .ToList();
    }

    #endregion
}

/// <summary>
/// DTO for available vehicle query results.
/// </summary>
public class VehicleAvailability
{
    public Vehicle Vehicle { get; set; } = null!;
    public bool IsPooled { get; set; }
    public bool IsAtCurrentShop { get; set; }
    public int CurrentLocationShopId { get; set; }
    public string? CurrentLocationShopName { get; set; }
}
