using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

public class DepositService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Deposit>> GetDepositsAsync(
        int shopId,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Deposit>();

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
                r => r.ShopId == shopId,
                r => r.RentalId);
            query = query.Where(d => rentalIds.IsInList(d.RentalId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.CollectedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.CollectedOn <= toDate.Value);
        }

        query = query.OrderByDescending(d => d.DepositId);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Deposit?> GetDepositByIdAsync(int depositId)
    {
        return await this.Context.LoadOneAsync<Deposit>(d => d.DepositId == depositId);
    }

    public async Task<Deposit?> GetDepositByRentalIdAsync(int rentalId)
    {
        return await this.Context.LoadOneAsync<Deposit>(d => d.RentalId == rentalId);
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        var query = this.Context.CreateQuery<Deposit>();

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
                r => r.ShopId == shopId,
                r => r.RentalId);
            query = query.Where(d => rentalIds.IsInList(d.RentalId));
        }

        var groupCounts = await this.Context.GetGroupByCountAsync(query, d => d.Status);

        return groupCounts.ToDictionary(g => g.Key ?? "Unknown", g => g.Count);
    }

    public async Task<decimal> GetTotalHeldDepositsAsync(int shopId)
    {
        var query = this.Context.CreateQuery<Deposit>()
            .Where(d => d.Status == "Held");

        // Only filter by shop if a specific shopId is provided (> 0)
        if (shopId > 0)
        {
            var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
                r => r.ShopId == shopId,
                r => r.RentalId);
            query = query.Where(d => rentalIds.IsInList(d.RentalId));
        }

        // Amount is stored in JSON, not as a computed column, so calculate sum in memory
        var deposits = await this.Context.LoadAsync(query, page: 1, size: 10000, includeTotalRows: false);
        return deposits.ItemCollection.Sum(d => d.Amount);
    }

    public async Task<SubmitOperation> RefundDepositAsync(int depositId, string refundMethod, string username)
    {
        var deposit = await this.GetDepositByIdAsync(depositId);
        if (deposit == null)
            return SubmitOperation.CreateFailure("Deposit not found");

        if (deposit.Status != "Held")
            return SubmitOperation.CreateFailure("Deposit is not in Held status");

        using var session = this.Context.OpenSession(username);

        deposit.Status = "Refunded";
        deposit.RefundedOn = DateTimeOffset.Now;
        session.Attach(deposit);

        // Create refund payment record
        var refundPayment = new Payment
        {
            RentalId = deposit.RentalId,
            PaymentType = "Refund",
            PaymentMethod = refundMethod,
            Amount = deposit.Amount,
            Status = "Completed",
            PaidOn = DateTimeOffset.Now,
            Notes = $"Deposit refund via {refundMethod}"
        };
        session.Attach(refundPayment);

        return await session.SubmitChanges("RefundDeposit");
    }

    public async Task<SubmitOperation> ForfeitDepositAsync(int depositId, string reason, string username)
    {
        var deposit = await this.GetDepositByIdAsync(depositId);
        if (deposit == null)
            return SubmitOperation.CreateFailure("Deposit not found");

        if (deposit.Status != "Held")
            return SubmitOperation.CreateFailure("Deposit is not in Held status");

        using var session = this.Context.OpenSession(username);

        deposit.Status = "Forfeited";
        session.Attach(deposit);

        return await session.SubmitChanges("ForfeitDeposit");
    }

    public async Task<List<DepositWithRentalInfo>> GetDepositsWithRentalInfoAsync(
        int shopId,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        // Get all rentals (optionally filtered by shop)
        var rentalsQuery = this.Context.CreateQuery<Rental>();
        if (shopId > 0)
        {
            rentalsQuery = rentalsQuery.Where(r => r.ShopId == shopId);
        }
        var rentals = await this.Context.LoadAsync(rentalsQuery, page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToList();

        // Get deposits
        var depositQuery = this.Context.CreateQuery<Deposit>();
        if (rentalIds.Count > 0)
        {
            depositQuery = depositQuery.Where(d => rentalIds.IsInList(d.RentalId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            depositQuery = depositQuery.Where(d => d.Status == status);
        }

        depositQuery = depositQuery.OrderByDescending(d => d.DepositId);

        var deposits = await this.Context.LoadAsync(depositQuery, page, pageSize, includeTotalRows: false);

        if (!deposits.ItemCollection.Any())
        {
            return [];
        }

        // Get related rental IDs from deposits
        var depositRentalIds = deposits.ItemCollection.Select(d => d.RentalId).Distinct().ToList();

        // Load rentals for these deposits
        var rentalsResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>().Where(r => depositRentalIds.IsInList(r.RentalId)),
            page: 1, size: 10000, includeTotalRows: false);

        // Get renter info
        var renterIds = rentalsResult.ItemCollection.Select(r => r.RenterId).Distinct().ToList();
        var rentersResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Renter>().Where(r => renterIds.IsInList(r.RenterId)),
            page: 1, size: 10000, includeTotalRows: false);

        // Get vehicle info
        var vehicleIds = rentalsResult.ItemCollection.Select(r => r.VehicleId).Distinct().ToList();
        var vehiclesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>().Where(v => vehicleIds.IsInList(v.VehicleId)),
            page: 1, size: 10000, includeTotalRows: false);

        var rentersDict = rentersResult.ItemCollection.ToDictionary(r => r.RenterId);
        var vehiclesDict = vehiclesResult.ItemCollection.ToDictionary(v => v.VehicleId);
        var rentalsDict = rentalsResult.ItemCollection.ToDictionary(r => r.RentalId);

        return deposits.ItemCollection.Select(d =>
        {
            var rental = rentalsDict.GetValueOrDefault(d.RentalId);
            var renter = rental != null ? rentersDict.GetValueOrDefault(rental.RenterId) : null;
            var vehicle = rental != null ? vehiclesDict.GetValueOrDefault(rental.VehicleId) : null;

            return new DepositWithRentalInfo
            {
                Deposit = d,
                Rental = rental,
                RenterName = renter?.FullName ?? "Unknown",
                MotorbikeName = vehicle != null ? $"{vehicle.Brand} {vehicle.Model} ({vehicle.LicensePlate})" : "Unknown"
            };
        }).ToList();
    }
}

public class DepositWithRentalInfo
{
    public Deposit Deposit { get; set; } = null!;
    public Rental? Rental { get; set; }
    public string RenterName { get; set; } = "";
    public string MotorbikeName { get; set; } = "";
}
