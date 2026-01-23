using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class InsuranceService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Insurance>> GetInsurancesAsync(
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Insurance>()
            .OrderByDescending(i => i.InsuranceId);
        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Insurance?> GetInsuranceByIdAsync(int insuranceId)
    {
        return await this.Context.LoadOneAsync<Insurance>(i => i.InsuranceId == insuranceId);
    }

    public async Task<List<Insurance>> GetActiveInsurancesAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Insurance>().Where(i => i.IsActive),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> CreateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("CreateInsurance");
    }

    public async Task<SubmitOperation> UpdateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("UpdateInsurance");
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
        return await session.SubmitChanges("DeleteInsurance");
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

    public async Task<Dictionary<bool, int>> GetActiveCountsAsync()
    {
        // Use SQL GROUP BY COUNT instead of loading all records
        var query = this.Context.CreateQuery<Insurance>();
        var groupCounts = await this.Context.GetGroupByCountAsync(query, i => i.IsActive);

        return groupCounts.ToDictionary(g => g.Key, g => g.Count);
    }
}
