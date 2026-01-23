using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

public partial class RentalService
{
    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        // Use database-side grouping instead of loading all rentals
        var groups = await this.Context.GetGroupByCountAsync<Rental, string?>(
            r => r.RentedFromShopId == shopId,
            r => r.Status);

        // Handle null keys by converting to "Unknown"
        return groups.ToDictionary(g => g.Key ?? "Unknown", g => g.Count);
    }

    /// <summary>
    /// Gets dynamic pricing statistics for completed rentals within a date range.
    /// </summary>
    public async Task<DynamicPricingStats> GetDynamicPricingStatsAsync(int shopId, DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        // Filter at database level: only load completed/active rentals in date range
        var statuses = new[] { "Completed", "Active" };
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId)
                .Where(r => r.StartDate >= fromDate)
                .Where(r => r.StartDate <= toDate)
                .Where(r => statuses.IsInList(r.Status)),
            page: 1, size: 10000, includeTotalRows: false);

        var completedRentals = rentals.ItemCollection.ToList();
        var dynamicPricingRentals = completedRentals.Where(r => r.DynamicPricingApplied).ToList();

        if (dynamicPricingRentals.Count == 0)
        {
            return new DynamicPricingStats
            {
                TotalRentals = completedRentals.Count,
                RentalsWithDynamicPricing = 0,
                BaseRevenue = completedRentals.Sum(r => r.BaseRentalRate * r.DayPricingBreakdown.Count),
                ActualRevenue = completedRentals.Sum(r => r.TotalAmount),
                DynamicPricingPremium = 0,
                AverageMultiplier = 1.0m
            };
        }

        var baseRevenue = dynamicPricingRentals.Sum(r => r.BaseRentalRate * r.DayPricingBreakdown.Count);
        var adjustedRevenue = dynamicPricingRentals.Sum(r => r.DayPricingBreakdown.Sum(d => d.AdjustedRate));
        var dynamicPricingPremium = adjustedRevenue - baseRevenue;
        var avgMultiplier = dynamicPricingRentals.Average(r => r.AverageMultiplier);

        return new DynamicPricingStats
        {
            TotalRentals = completedRentals.Count,
            RentalsWithDynamicPricing = dynamicPricingRentals.Count,
            BaseRevenue = baseRevenue,
            ActualRevenue = adjustedRevenue,
            DynamicPricingPremium = dynamicPricingPremium,
            AverageMultiplier = avgMultiplier,
            RuleBreakdown = dynamicPricingRentals
                .SelectMany(r => r.DayPricingBreakdown.Where(d => d.HasAdjustment))
                .GroupBy(d => d.RuleName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<List<Rental>> GetTodaysDueReturnsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;
        var todayEnd = todayStart.AddDays(1);

        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId)
                .Where(r => r.Status == "Active")
                .Where(r => r.ExpectedEndDate >= todayStart)
                .Where(r => r.ExpectedEndDate < todayEnd),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetOverdueRentalsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;

        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId)
                .Where(r => r.Status == "Active")
                .Where(r => r.ExpectedEndDate < todayStart),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetActiveRentalsForVehicleAsync(int vehicleId)
    {
        var activeStatuses = new[] { "Active", "Reserved" };
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.VehicleId == vehicleId)
                .Where(r => activeStatuses.IsInList(r.Status)),
            page: 1, size: 100, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    [Obsolete("Use GetActiveRentalsForVehicleAsync instead")]
    public async Task<List<Rental>> GetActiveRentalsForMotorbikeAsync(int motorbikeId)
    {
        return await GetActiveRentalsForVehicleAsync(motorbikeId);
    }

    /// <summary>
    /// Searches active rentals by renter name or rental ID.
    /// Uses database-side filtering via SQL LIKE for efficient search.
    /// </summary>
    /// <param name="searchTerm">Search term to match against renter name or rental ID</param>
    /// <param name="shopId">Optional shop ID to filter by</param>
    /// <returns>List of matching active rentals</returns>
    public async Task<List<Rental>> SearchActiveRentalsAsync(string searchTerm, int? shopId = null)
    {
        var query = this.Context.CreateQuery<Rental>()
            .Where(r => r.Status == "Active");

        if (shopId.HasValue)
        {
            query = query.Where(r => r.RentedFromShopId == shopId.Value);
        }

        // Check if searching by RentalId (numeric search)
        if (int.TryParse(searchTerm, out var rentalId))
        {
            query = query.Where(r => r.RentalId == rentalId);
        }
        else
        {
            // Use database-side LIKE for name search (case-insensitive in SQL Server)
            query = query.Where(r => r.RenterName != null && r.RenterName.Contains(searchTerm));
        }

        var result = await this.Context.LoadAsync(query, 1, 50, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }
}
