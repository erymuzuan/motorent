using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing traffic and parking fines.
/// </summary>
public class TrafficFineService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<TrafficFine>> GetFinesAsync(
        string? status = null,
        string? fineType = null,
        int? vehicleId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<TrafficFine>();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(f => f.Status == status);
        if (!string.IsNullOrEmpty(fineType))
            query = query.Where(f => f.FineType == fineType);
        if (vehicleId.HasValue)
            query = query.Where(f => f.VehicleId == vehicleId.Value);
        if (fromDate.HasValue)
            query = query.Where(f => f.FineDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(f => f.FineDate <= toDate.Value);

        query = query.OrderByDescending(f => f.TrafficFineId);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
        }

        var result = await this.Context.LoadAsync(query, page, pageSize * 2, includeTotalRows: true);
        var term = searchTerm.ToLowerInvariant();
        result.ItemCollection = result.ItemCollection
            .Where(f =>
                (f.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                (f.ReferenceNo?.ToLowerInvariant().Contains(term) ?? false) ||
                (f.VehicleLicensePlate?.ToLowerInvariant().Contains(term) ?? false) ||
                (f.RenterName?.ToLowerInvariant().Contains(term) ?? false) ||
                (f.Location?.ToLowerInvariant().Contains(term) ?? false))
            .Take(pageSize)
            .ToList();

        return result;
    }

    public async Task<TrafficFine?> GetByIdAsync(int trafficFineId)
    {
        return await this.Context.LoadOneAsync<TrafficFine>(f => f.TrafficFineId == trafficFineId);
    }

    public async Task<TrafficFine> CreateAsync(TrafficFine fine, string userName)
    {
        // Ensure FineDate is set (standalone DATE column in DB, not computed from JSON)
        if (fine.FineDate == default)
            fine.FineDate = DateTimeOffset.UtcNow;

        // Denormalize vehicle info
        var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == fine.VehicleId);
        if (vehicle != null)
        {
            fine.VehicleName = $"{vehicle.Brand} {vehicle.Model}";
            fine.VehicleLicensePlate = vehicle.LicensePlate;
        }

        // Denormalize renter info if linked to a rental
        if (fine.RentalId.HasValue)
        {
            var rental = await this.Context.LoadOneAsync<Rental>(r => r.RentalId == fine.RentalId.Value);
            if (rental != null)
            {
                var renter = await this.Context.LoadOneAsync<Renter>(r => r.RenterId == rental.RenterId);
                fine.RenterName = renter?.FullName;
            }
        }

        using var session = this.Context.OpenSession(userName);
        session.Attach(fine);
        await session.SubmitChanges("CreateTrafficFine");
        return fine;
    }

    public async Task<TrafficFine> UpdateAsync(TrafficFine fine, string userName)
    {
        // Ensure FineDate is set (standalone DATE column in DB, not computed from JSON)
        if (fine.FineDate == default)
            fine.FineDate = DateTimeOffset.UtcNow;

        using var session = this.Context.OpenSession(userName);
        session.Attach(fine);
        await session.SubmitChanges("UpdateTrafficFine");
        return fine;
    }

    public async Task<TrafficFine?> ChargeAgainstDepositAsync(int trafficFineId, int depositId, string userName)
    {
        var fine = await this.GetByIdAsync(trafficFineId);
        if (fine == null) return null;

        fine.Status = "ChargedToDeposit";
        fine.SettlementMethod = "DeductFromDeposit";
        fine.DepositId = depositId;
        fine.ResolvedDate = DateTimeOffset.Now;

        using var session = this.Context.OpenSession(userName);
        session.Attach(fine);
        await session.SubmitChanges("ChargeTrafficFineToDeposit");
        return fine;
    }

    public async Task<List<TrafficFine>> GetUnresolvedByVehicleAsync(int vehicleId)
    {
        var query = this.Context.CreateQuery<TrafficFine>()
            .Where(f => f.VehicleId == vehicleId && f.Status == "Pending")
            .OrderByDescending(f => f.TrafficFineId);

        var result = await this.Context.LoadAsync(query, 1, 100);
        return result.ItemCollection;
    }

    public async Task<List<TrafficFine>> GetUnresolvedByRentalAsync(int rentalId)
    {
        var query = this.Context.CreateQuery<TrafficFine>()
            .Where(f => f.RentalId == rentalId && f.Status == "Pending")
            .OrderByDescending(f => f.TrafficFineId);

        var result = await this.Context.LoadAsync(query, 1, 100);
        return result.ItemCollection;
    }
}
