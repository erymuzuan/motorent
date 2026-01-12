using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing third-party vehicle owners.
/// </summary>
public class VehicleOwnerService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region Query Methods

    public async Task<LoadOperation<VehicleOwner>> GetOwnersAsync(
        bool? isActive = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<VehicleOwner>().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        query = query.OrderBy(o => o.Name);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(o =>
                    (o.Name?.ToLowerInvariant().Contains(term) ?? false) ||
                    (o.Phone?.Contains(term) ?? false) ||
                    (o.Email?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<List<VehicleOwner>> GetActiveOwnersAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<VehicleOwner>().Where(o => o.IsActive),
            page: 1, size: 500, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<VehicleOwner?> GetOwnerByIdAsync(int ownerId)
    {
        return await this.Context.LoadOneAsync<VehicleOwner>(o => o.VehicleOwnerId == ownerId);
    }

    public async Task<List<Vehicle>> GetOwnerVehiclesAsync(int ownerId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => v.VehicleOwnerId == ownerId),
            page: 1, size: 500, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<int> GetOwnerVehicleCountAsync(int ownerId)
    {
        return await this.Context.GetCountAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => v.VehicleOwnerId == ownerId));
    }

    #endregion

    #region CRUD Operations

    public async Task<SubmitOperation> CreateOwnerAsync(VehicleOwner owner, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(owner);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateOwnerAsync(VehicleOwner owner, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(owner);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteOwnerAsync(VehicleOwner owner, string username)
    {
        // Check for vehicles assigned to this owner
        var vehicleCount = await this.GetOwnerVehicleCountAsync(owner.VehicleOwnerId);

        if (vehicleCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete owner with {vehicleCount} assigned vehicle(s). Reassign vehicles first.");
        }

        // Check for pending payments
        var pendingPayments = await this.Context.GetCountAsync(
            this.Context.CreateQuery<OwnerPayment>().Where(p =>
                p.VehicleOwnerId == owner.VehicleOwnerId &&
                p.Status == OwnerPaymentStatus.Pending));

        if (pendingPayments > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete owner with {pendingPayments} pending payment(s). Process payments first.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(owner);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> SetActiveStatusAsync(int ownerId, bool isActive, string username)
    {
        var owner = await this.GetOwnerByIdAsync(ownerId);
        if (owner is null)
            return SubmitOperation.CreateFailure("Owner not found");

        owner.IsActive = isActive;

        using var session = this.Context.OpenSession(username);
        session.Attach(owner);
        return await session.SubmitChanges("StatusUpdate");
    }

    #endregion
}
