using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class InsuranceService
{
    private readonly RentalDataContext m_context;

    public InsuranceService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<LoadOperation<Insurance>> GetInsurancesAsync(
        int shopId,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Insurances
            .Where(i => i.ShopId == shopId)
            .OrderByDescending(i => i.InsuranceId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Insurance?> GetInsuranceByIdAsync(int insuranceId)
    {
        return await m_context.LoadOneAsync<Insurance>(i => i.InsuranceId == insuranceId);
    }

    public async Task<List<Insurance>> GetActiveInsurancesAsync(int shopId)
    {
        var result = await m_context.LoadAsync(
            m_context.Insurances
                .Where(i => i.ShopId == shopId && i.IsActive),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }

    public async Task<SubmitOperation> CreateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateInsuranceAsync(Insurance insurance, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteInsuranceAsync(Insurance insurance, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(insurance);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> ToggleActiveAsync(int insuranceId, string username)
    {
        var insurance = await GetInsuranceByIdAsync(insuranceId);
        if (insurance == null)
            return SubmitOperation.CreateFailure("Insurance not found");

        insurance.IsActive = !insurance.IsActive;

        using var session = m_context.OpenSession(username);
        session.Attach(insurance);
        return await session.SubmitChanges("ToggleActive");
    }

    public async Task<Dictionary<bool, int>> GetActiveCountsAsync(int shopId)
    {
        var allInsurance = await m_context.LoadAsync(
            m_context.Insurances.Where(i => i.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: false);

        return allInsurance.ItemCollection
            .GroupBy(i => i.IsActive)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
