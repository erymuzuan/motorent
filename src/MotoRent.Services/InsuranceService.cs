using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class InsuranceService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Insurance>> GetInsurancesAsync(
        int shopId,
        int page = 1,
        int pageSize = 20)
    {
        // Load all and filter in memory (Repository doesn't handle complex expressions)
        var all = await this.Context.LoadAsync(this.Context.CreateQuery<Insurance>(), 1, 1000, false);
        var filtered = all.ItemCollection
            .Where(i => i.ShopId == shopId || i.ShopId == 0)
            .OrderByDescending(i => i.InsuranceId)
            .ToList();

        var result = new LoadOperation<Insurance>
        {
            Page = page,
            PageSize = pageSize,
            TotalRows = filtered.Count,
            ItemCollection = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
        return result;
    }

    public async Task<Insurance?> GetInsuranceByIdAsync(int insuranceId)
    {
        return await this.Context.LoadOneAsync<Insurance>(i => i.InsuranceId == insuranceId);
    }

    public async Task<List<Insurance>> GetActiveInsurancesAsync(int shopId)
    {
        // Load all and filter in memory (Repository doesn't handle && expressions)
        var all = await this.Context.LoadAsync(this.Context.CreateQuery<Insurance>(), 1, 1000, false);
        return all.ItemCollection
            .Where(i => (i.ShopId == shopId || i.ShopId == 0) && i.IsActive)
            .ToList();
    }

    public async Task<SubmitOperation> CreateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteInsuranceAsync(Insurance insurance, string username)
    {
        // Check if any rentals use this insurance
        var rentalCount = await this.Context.GetCountAsync(
            this.Context.CreateQuery<Rental>().Where(r => r.InsuranceId == insurance.InsuranceId));
        if (rentalCount > 0)
        {
            return SubmitOperation.CreateFailure(
                $"Cannot delete insurance with {rentalCount} rental(s). Please deactivate instead.");
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(insurance);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> ToggleActiveAsync(int insuranceId, string username)
    {
        var insurance = await this.GetInsuranceByIdAsync(insuranceId);
        if (insurance == null)
            return SubmitOperation.CreateFailure("Insurance not found");

        insurance.IsActive = !insurance.IsActive;

        using var session = this.Context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("ToggleActive");
    }

    public async Task<Dictionary<bool, int>> GetActiveCountsAsync(int shopId)
    {
        // Load all and filter in memory
        var all = await this.Context.LoadAsync(this.Context.CreateQuery<Insurance>(), 1, 1000, false);
        return all.ItemCollection
            .Where(i => i.ShopId == shopId || i.ShopId == 0)
            .GroupBy(i => i.IsActive)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
