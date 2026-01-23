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
        // Get rental IDs for this shop
        var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
            r => r.ShopId == shopId,
            r => r.RentalId);

        var query = this.Context.CreateQuery<Deposit>()
            .Where(d => rentalIds.IsInList(d.RentalId));

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
        // Get rental IDs for this shop
        var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
            r => r.ShopId == shopId,
            r => r.RentalId);

        // Get status counts via SQL GROUP BY
        var query = this.Context.CreateQuery<Deposit>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        var groupCounts = await this.Context.GetGroupByCountAsync(query, d => d.Status ?? "Unknown");

        return groupCounts.ToDictionary(g => g.Key, g => g.Count);
    }

    public async Task<decimal> GetTotalHeldDepositsAsync(int shopId)
    {
        // Get rental IDs for this shop
        var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
            r => r.ShopId == shopId,
            r => r.RentalId);

        // Sum held deposits via SQL SUM
        var query = this.Context.CreateQuery<Deposit>()
            .Where(d => rentalIds.IsInList(d.RentalId))
            .Where(d => d.Status == "Held");

        return await this.Context.GetSumAsync(query, d => d.Amount);
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
        // Get all rentals for the shop
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>().Where(r => r.ShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToList();

        // Get deposits
        var depositQuery = this.Context.CreateQuery<Deposit>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            depositQuery = depositQuery.Where(d => d.Status == status);
        }

        depositQuery = depositQuery.OrderByDescending(d => d.DepositId);

        var deposits = await this.Context.LoadAsync(depositQuery, page, pageSize, includeTotalRows: false);

        // Get renter info
        var renterIds = rentals.ItemCollection.Select(r => r.RenterId).Distinct().ToList();
        var rentersResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Renter>().Where(r => renterIds.IsInList(r.RenterId)),
            page: 1, size: 10000, includeTotalRows: false);

        // Get motorbike info
        var motorbikeIds = rentals.ItemCollection.Select(r => r.MotorbikeId).Distinct().ToList();
        var motorbikesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Motorbike>().Where(m => motorbikeIds.IsInList(m.MotorbikeId)),
            page: 1, size: 10000, includeTotalRows: false);

        var rentersDict = rentersResult.ItemCollection.ToDictionary(r => r.RenterId);
        var motorbikesDict = motorbikesResult.ItemCollection.ToDictionary(m => m.MotorbikeId);
        var rentalsDict = rentals.ItemCollection.ToDictionary(r => r.RentalId);

        return deposits.ItemCollection.Select(d =>
        {
            var rental = rentalsDict.GetValueOrDefault(d.RentalId);
            var renter = rental != null ? rentersDict.GetValueOrDefault(rental.RenterId) : null;
            var motorbike = rental != null ? motorbikesDict.GetValueOrDefault(rental.MotorbikeId) : null;

            return new DepositWithRentalInfo
            {
                Deposit = d,
                Rental = rental,
                RenterName = renter?.FullName ?? "Unknown",
                MotorbikeName = motorbike != null ? $"{motorbike.Brand} {motorbike.Model} ({motorbike.LicensePlate})" : "Unknown"
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
