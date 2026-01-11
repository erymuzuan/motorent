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
        var query = this.Context.Insurances
            .Where(i => i.ShopId == shopId)
            .OrderByDescending(i => i.InsuranceId);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Insurance?> GetInsuranceByIdAsync(int insuranceId)
    {
        return await this.Context.LoadOneAsync<Insurance>(i => i.InsuranceId == insuranceId);
    }

    public async Task<List<Insurance>> GetActiveInsurancesAsync(int shopId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.Insurances
                .Where(i => i.ShopId == shopId && i.IsActive),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
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
        var allInsurance = await this.Context.LoadAsync(
            this.Context.Insurances.Where(i => i.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: false);

        return allInsurance.ItemCollection
            .GroupBy(i => i.IsActive)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
