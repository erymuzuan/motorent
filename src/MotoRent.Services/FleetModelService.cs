using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class FleetModelService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<FleetModel>> GetFleetModelsAsync(
        VehicleType? vehicleType = null,
        string? searchTerm = null,
        bool activeOnly = true,
        int page = 1,
        int pageSize = 50)
    {
        var query = this.Context.CreateQuery<FleetModel>();

        if (vehicleType.HasValue)
        {
            query = query.Where(fm => fm.VehicleType == vehicleType.Value);
        }

        if (activeOnly)
        {
            query = query.Where(fm => fm.IsActive);
        }

        query = query.OrderBy(fm => fm.Brand).ThenBy(fm => fm.Model).ThenByDescending(fm => fm.Year);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(fm =>
                    (fm.Brand?.ToLowerInvariant().Contains(term) ?? false) ||
                    (fm.Model?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<List<FleetModel>> GetAllActiveFleetModelsAsync(VehicleType? vehicleType = null)
    {
        var query = this.Context.CreateQuery<FleetModel>()
            .Where(fm => fm.IsActive);

        if (vehicleType.HasValue)
        {
            query = query.Where(fm => fm.VehicleType == vehicleType.Value);
        }

        query = query.OrderBy(fm => fm.Brand).ThenBy(fm => fm.Model).ThenByDescending(fm => fm.Year);

        var result = await this.Context.LoadAsync(query, 1, 500, includeTotalRows: false);
        return result.ItemCollection;
    }

    public Task<FleetModel?> GetFleetModelByIdAsync(int fleetModelId)
    {
        return this.Context.LoadOneAsync<FleetModel>(fm => fm.FleetModelId == fleetModelId);
    }

    public async Task<FleetModel?> GetFleetModelForVehicleAsync(Vehicle vehicle)
    {
        var query = this.Context.CreateQuery<FleetModel>()
            .Where(fm => fm.Brand == vehicle.Brand)
            .Where(fm => fm.Model == vehicle.Model)
            .Where(fm => fm.Year == vehicle.Year)
            .Where(fm => fm.VehicleType == vehicle.VehicleType);

        var result = await this.Context.LoadAsync(query, 1, 10, includeTotalRows: false);
        return result.ItemCollection.FirstOrDefault();
    }

    public async Task<Dictionary<int, int>> GetFleetModelCountsAsync()
    {
        var query = this.Context.CreateQuery<Vehicle>()
            .Where(v => v.FleetModelId > 0);

        var groupCounts = await this.Context.GetGroupByCountAsync<Vehicle, int>(query, v => v.FleetModelId);

        return groupCounts
            .Where(g => g.Key > 0)
            .ToDictionary(g => g.Key, g => g.Count);
    }

    public async Task<SubmitOperation> CreateFleetModelAsync(FleetModel fleetModel, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(fleetModel);
        return await session.SubmitChanges("CreateFleetModel");
    }

    public async Task<SubmitOperation> UpdateFleetModelAsync(FleetModel fleetModel, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(fleetModel);
        return await session.SubmitChanges("UpdateFleetModel");
    }

    public async Task<SubmitOperation> DeleteFleetModelAsync(FleetModel fleetModel, string username)
    {
        var linkedCount = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.FleetModelId == fleetModel.FleetModelId));

        if (linkedCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete fleet model with {linkedCount} linked vehicle(s). Unlink or delete vehicles first.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(fleetModel);
        return await session.SubmitChanges("DeleteFleetModel");
    }
}
