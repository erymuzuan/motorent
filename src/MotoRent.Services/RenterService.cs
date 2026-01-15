using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class RenterService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Renter>> GetRentersAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Renter>()
            .OrderByDescending(r => r.RenterId);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

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
        return await this.Context.LoadOneAsync<Renter>(r => r.RenterId == renterId);
    }

    public async Task<Renter?> GetRenterByPassportAsync(string passportNo)
    {
        var query = this.Context.CreateQuery<Renter>().Where(r => r.PassportNo == passportNo);
        var result = await this.Context.LoadAsync(query, page: 1, size: 1, includeTotalRows: false);
        return result.ItemCollection.FirstOrDefault();
    }

    public async Task<SubmitOperation> CreateRenterAsync(Renter renter, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(renter);
        return await session.SubmitChanges("Registration");
    }

    public async Task<SubmitOperation> UpdateRenterAsync(Renter renter, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(renter);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteRenterAsync(Renter renter, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(renter);
        return await session.SubmitChanges("Delete");
    }

    public async Task<List<Document>> GetRenterDocumentsAsync(int renterId)
    {
        var query = this.Context.CreateQuery<Document>()
            .Where(d => d.RenterId == renterId)
            .OrderByDescending(d => d.DocumentId);

        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<int> GetActiveRentalCountAsync(int renterId)
    {
        var query = this.Context.CreateQuery<Rental>()
            .Where(r => r.RenterId == renterId && r.Status == "Active");

        return await this.Context.GetCountAsync(query);
    }
}
