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
        var query = this.Context.CreateQuery<Rental>();

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            query = query.Where(r => r.RentedFromShopId == shopId || r.ReturnedToShopId == shopId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status == "Overdue")
            {
                var todayStart = DateTimeOffset.Now.Date;
                query = query.Where(r => r.Status == "Active")
                             .Where(r => r.ExpectedEndDate < todayStart);
            }
            else if (status == "DueToday")
            {
                var todayStart = DateTimeOffset.Now.Date;
                var todayEnd = todayStart.AddDays(1);
                query = query.Where(r => r.Status == "Active")
                             .Where(r => r.ExpectedEndDate >= todayStart)
                             .Where(r => r.ExpectedEndDate < todayEnd);
            }
            else
            {
                query = query.Where(r => r.Status == status);
            }
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

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
        }

        // For search term, load more and filter in memory
        var result = await this.Context.LoadAsync(query, page, pageSize * 2, includeTotalRows: true);

        var term = searchTerm.ToLowerInvariant();
        result.ItemCollection = result.ItemCollection
            .Where(r =>
                (r.RenterName?.ToLowerInvariant().Contains(term) ?? false) ||
                (r.VehicleName?.ToLowerInvariant().Contains(term) ?? false) ||
                (r.VehicleLicensePlate?.ToLowerInvariant().Contains(term) ?? false))
            .Take(pageSize)
            .ToList();

        return result;
    }

    public async Task<Rental?> GetRentalByIdAsync(int rentalId)
    {
        return await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
    }

    public async Task<SubmitOperation> CreateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("CreateRental");
    }

    public async Task<SubmitOperation> UpdateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("UpdateRental");
    }

    public async Task<SubmitOperation> DeleteRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(rental);
        return await session.SubmitChanges("DeleteRental");
    }
}
