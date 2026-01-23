using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

/// <summary>
/// Service for managing damage reports and their lifecycle.
/// </summary>
public class DamageReportService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    /// <summary>
    /// Gets paginated list of damage reports for a shop.
    /// </summary>
    public async Task<LoadOperation<DamageReport>> GetDamageReportsAsync(
        int shopId,
        string? status = null,
        string? severity = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        // Get rental IDs for the shop
        var rentalIds = await Context.GetDistinctAsync<Rental, int>(
            r => r.RentedFromShopId == shopId,
            r => r.RentalId);

        // Build query with all filters at SQL level
        var query = Context.CreateQuery<DamageReport>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(d => d.Severity == severity);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.ReportedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.ReportedOn <= toDate.Value);
        }

        query = query.OrderByDescending(d => d.DamageReportId);

        return await Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Gets a damage report by ID.
    /// </summary>
    public async Task<DamageReport?> GetByIdAsync(int damageReportId)
    {
        return await Context.LoadOneAsync<DamageReport>(d => d.DamageReportId == damageReportId);
    }

    /// <summary>
    /// Gets all damage reports for a specific rental.
    /// </summary>
    public async Task<List<DamageReport>> GetByRentalAsync(int rentalId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>().Where(d => d.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }

    /// <summary>
    /// Gets damage history for a vehicle.
    /// </summary>
    public async Task<List<DamageReport>> GetByVehicleAsync(int vehicleId)
    {
        // DamageReport may have MotorbikeId for backwards compatibility
        var result = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>().Where(d => d.MotorbikeId == vehicleId),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.OrderByDescending(d => d.ReportedOn).ToList();
    }

    /// <summary>
    /// Gets all pending damage reports for a shop.
    /// </summary>
    public async Task<List<DamageReportWithDetails>> GetPendingAsync(int shopId)
    {
        return await GetWithDetailsAsync(shopId, status: "Pending");
    }

    /// <summary>
    /// Gets damage reports with rental, renter, and vehicle details.
    /// </summary>
    public async Task<List<DamageReportWithDetails>> GetWithDetailsAsync(
        int shopId,
        string? status = null,
        int page = 1,
        int pageSize = 50)
    {
        // Get rental IDs for the shop
        var rentalIds = await Context.GetDistinctAsync<Rental, int>(
            r => r.RentedFromShopId == shopId,
            r => r.RentalId);

        // Build damage reports query with SQL-level filters
        var damageReportsQuery = Context.CreateQuery<DamageReport>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            damageReportsQuery = damageReportsQuery.Where(d => d.Status == status);
        }

        damageReportsQuery = damageReportsQuery.OrderByDescending(d => d.DamageReportId);

        var damageReportsResult = await Context.LoadAsync(damageReportsQuery, page, pageSize, includeTotalRows: false);
        var damageReportsList = damageReportsResult.ItemCollection;

        if (damageReportsList.Count == 0)
        {
            return [];
        }

        // Get the relevant rentals for the damage reports
        var relevantRentalIds = damageReportsList.Select(d => d.RentalId).Distinct().ToList();
        var rentalsResult = await Context.LoadAsync(
            Context.CreateQuery<Rental>().Where(r => relevantRentalIds.IsInList(r.RentalId)),
            page: 1, size: 1000, includeTotalRows: false);

        // Get renter info using IsInList
        var renterIds = rentalsResult.ItemCollection.Select(r => r.RenterId).Distinct().ToList();
        var rentersResult = await Context.LoadAsync(
            Context.CreateQuery<Renter>().Where(r => renterIds.IsInList(r.RenterId)),
            page: 1, size: 1000, includeTotalRows: false);

        // Get vehicle info using IsInList
        var vehicleIds = rentalsResult.ItemCollection.Select(r => r.VehicleId).Distinct().ToList();
        var vehiclesResult = await Context.LoadAsync(
            Context.CreateQuery<Vehicle>().Where(v => vehicleIds.IsInList(v.VehicleId)),
            page: 1, size: 1000, includeTotalRows: false);

        // Get damage photos using IsInList
        var damageReportIds = damageReportsList.Select(d => d.DamageReportId).ToList();
        var photosResult = await Context.LoadAsync(
            Context.CreateQuery<DamagePhoto>().Where(p => damageReportIds.IsInList(p.DamageReportId)),
            page: 1, size: 1000, includeTotalRows: false);

        var rentersDict = rentersResult.ItemCollection.ToDictionary(r => r.RenterId);
        var vehiclesDict = vehiclesResult.ItemCollection.ToDictionary(v => v.VehicleId);
        var rentalsDict = rentalsResult.ItemCollection.ToDictionary(r => r.RentalId);
        var photosDict = photosResult.ItemCollection.GroupBy(p => p.DamageReportId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return damageReportsList.Select(d =>
        {
            var rental = rentalsDict.GetValueOrDefault(d.RentalId);
            var renter = rental != null ? rentersDict.GetValueOrDefault(rental.RenterId) : null;
            var vehicle = rental != null ? vehiclesDict.GetValueOrDefault(rental.VehicleId) : null;
            var photos = photosDict.GetValueOrDefault(d.DamageReportId) ?? [];

            return new DamageReportWithDetails
            {
                DamageReport = d,
                Rental = rental,
                RenterName = renter?.FullName ?? "Unknown",
                VehicleName = vehicle != null ? $"{vehicle.Brand} {vehicle.Model} ({vehicle.LicensePlate})" : "Unknown",
                Photos = photos
            };
        }).ToList();
    }

    /// <summary>
    /// Updates the status of a damage report.
    /// </summary>
    public async Task<SubmitOperation> UpdateStatusAsync(
        int damageReportId,
        string newStatus,
        decimal? actualCost,
        string? notes,
        string username)
    {
        var damageReport = await GetByIdAsync(damageReportId);
        if (damageReport == null)
            return SubmitOperation.CreateFailure("Damage report not found");

        var validStatuses = new[] { "Pending", "Charged", "Waived", "InsuranceClaim" };
        if (!validStatuses.Contains(newStatus))
            return SubmitOperation.CreateFailure($"Invalid status. Valid values: {string.Join(", ", validStatuses)}");

        using var session = Context.OpenSession(username);

        damageReport.Status = newStatus;
        if (actualCost.HasValue)
        {
            damageReport.ActualCost = actualCost.Value;
        }

        session.Attach(damageReport);

        return await session.SubmitChanges("UpdateDamageReportStatus");
    }

    /// <summary>
    /// Gets damage photos for a specific damage report.
    /// </summary>
    public async Task<List<DamagePhoto>> GetPhotosAsync(int damageReportId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<DamagePhoto>().Where(p => p.DamageReportId == damageReportId),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }

    /// <summary>
    /// Adds a photo to a damage report.
    /// </summary>
    public async Task<SubmitOperation> AddPhotoAsync(
        int damageReportId,
        string photoType,
        string imagePath,
        string? notes,
        string username)
    {
        var damageReport = await GetByIdAsync(damageReportId);
        if (damageReport == null)
            return SubmitOperation.CreateFailure("Damage report not found");

        using var session = Context.OpenSession(username);

        var photo = new DamagePhoto
        {
            DamageReportId = damageReportId,
            PhotoType = photoType,
            ImagePath = imagePath,
            Notes = notes,
            CapturedOn = DateTimeOffset.Now
        };

        session.Attach(photo);

        return await session.SubmitChanges("AddDamagePhoto");
    }

    /// <summary>
    /// Gets status counts for dashboard display.
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        // Get rental IDs for the shop
        var rentalIds = await Context.GetDistinctAsync<Rental, int>(
            r => r.RentedFromShopId == shopId,
            r => r.RentalId);

        // Get status counts via SQL GROUP BY
        var query = Context.CreateQuery<DamageReport>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        var groupCounts = await Context.GetGroupByCountAsync(query, d => d.Status ?? "Unknown");

        return groupCounts.ToDictionary(g => g.Key, g => g.Count);
    }

    /// <summary>
    /// Gets total estimated damage costs by status.
    /// </summary>
    public async Task<DamageCostSummary> GetCostSummaryAsync(int shopId)
    {
        // Get rental IDs for the shop
        var rentalIds = await Context.GetDistinctAsync<Rental, int>(
            r => r.RentedFromShopId == shopId,
            r => r.RentalId);

        var baseQuery = Context.CreateQuery<DamageReport>()
            .Where(d => rentalIds.IsInList(d.RentalId));

        // Get counts and sums for each status using SQL aggregates
        var pendingQuery = baseQuery.Where(d => d.Status == "Pending");
        var chargedQuery = baseQuery.Where(d => d.Status == "Charged");
        var waivedQuery = baseQuery.Where(d => d.Status == "Waived");
        var insuranceClaimQuery = baseQuery.Where(d => d.Status == "InsuranceClaim");

        var pendingCount = await Context.GetCountAsync(pendingQuery);
        var pendingSum = await Context.GetSumAsync(pendingQuery, d => d.EstimatedCost);

        var chargedCount = await Context.GetCountAsync(chargedQuery);
        // For charged, we need ActualCost if available, otherwise EstimatedCost
        // This requires loading the data since we can't do COALESCE in the expression tree
        var chargedResult = await Context.LoadAsync(chargedQuery, 1, 10000, false);
        var chargedAmount = chargedResult.ItemCollection.Sum(d => d.ActualCost ?? d.EstimatedCost);

        var waivedCount = await Context.GetCountAsync(waivedQuery);
        var waivedSum = await Context.GetSumAsync(waivedQuery, d => d.EstimatedCost);

        var insuranceClaimCount = await Context.GetCountAsync(insuranceClaimQuery);
        var insuranceClaimSum = await Context.GetSumAsync(insuranceClaimQuery, d => d.EstimatedCost);

        return new DamageCostSummary
        {
            PendingCount = pendingCount,
            PendingEstimatedCost = pendingSum,
            ChargedCount = chargedCount,
            ChargedAmount = chargedAmount,
            WaivedCount = waivedCount,
            WaivedAmount = waivedSum,
            InsuranceClaimCount = insuranceClaimCount,
            InsuranceClaimAmount = insuranceClaimSum
        };
    }
}

/// <summary>
/// Damage report with related rental, renter, and vehicle information.
/// </summary>
public class DamageReportWithDetails
{
    public DamageReport DamageReport { get; set; } = null!;
    public Rental? Rental { get; set; }
    public string RenterName { get; set; } = "";
    public string VehicleName { get; set; } = "";
    public List<DamagePhoto> Photos { get; set; } = [];
}

/// <summary>
/// Summary of damage costs by status.
/// </summary>
public class DamageCostSummary
{
    public int PendingCount { get; set; }
    public decimal PendingEstimatedCost { get; set; }
    public int ChargedCount { get; set; }
    public decimal ChargedAmount { get; set; }
    public int WaivedCount { get; set; }
    public decimal WaivedAmount { get; set; }
    public int InsuranceClaimCount { get; set; }
    public decimal InsuranceClaimAmount { get; set; }
}
