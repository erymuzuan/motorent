using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public partial class RentalService
{
    public async Task<LoadOperation<Rental>> GetRentalsAsync(
        int shopId,
        string? status = null,
        string? searchTerm = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Rental>()
            .Where(r => r.RentedFromShopId == shopId || r.ReturnedToShopId == shopId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.StartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ExpectedEndDate <= toDate.Value);
        }

        query = query.OrderByDescending(r => r.RentalId);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Rental?> GetRentalByIdAsync(int rentalId)
    {
        return await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
    }

    public async Task<SubmitOperation> CreateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(rental);
        return await session.SubmitChanges("Delete");
    }
}
