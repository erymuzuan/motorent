using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class RenterService
{
    private readonly RentalDataContext m_context;

    public RenterService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<LoadOperation<Renter>> GetRentersAsync(
        int shopId,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Renters
            .Where(r => r.ShopId == shopId)
            .OrderByDescending(r => r.RenterId);

        var result = await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(r =>
                    (r.FullName?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.Phone?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.PassportNo?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.Email?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<Renter?> GetRenterByIdAsync(int renterId)
    {
        return await m_context.LoadOneAsync<Renter>(r => r.RenterId == renterId);
    }

    public async Task<Renter?> GetRenterByPassportAsync(string passportNo)
    {
        var query = m_context.Renters.Where(r => r.PassportNo == passportNo);
        var result = await m_context.LoadAsync(query, page: 1, size: 1, includeTotalRows: false);
        return result.ItemCollection.FirstOrDefault();
    }

    public async Task<SubmitOperation> CreateRenterAsync(Renter renter, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(renter);
        return await session.SubmitChanges("Registration");
    }

    public async Task<SubmitOperation> UpdateRenterAsync(Renter renter, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(renter);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteRenterAsync(Renter renter, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(renter);
        return await session.SubmitChanges("Delete");
    }

    public async Task<List<Document>> GetRenterDocumentsAsync(int renterId)
    {
        var query = m_context.Documents
            .Where(d => d.RenterId == renterId)
            .OrderByDescending(d => d.DocumentId);

        var result = await m_context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<int> GetActiveRentalCountAsync(int renterId)
    {
        var query = m_context.Rentals
            .Where(r => r.RenterId == renterId && r.Status == "Active");

        var result = await m_context.LoadAsync(query, page: 1, size: 1, includeTotalRows: true);
        return result.TotalRows;
    }
}
