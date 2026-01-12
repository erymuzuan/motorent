using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

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
        // Get rentals for the shop to filter damage reports
        var rentals = await Context.LoadAsync(
            Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToHashSet();

        // Load all damage reports and filter in memory (custom query provider doesn't support Contains)
        var allDamageReports = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>(),
            page: 1, size: 10000, includeTotalRows: false);

        var filteredReports = allDamageReports.ItemCollection
            .Where(d => rentalIds.Contains(d.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            filteredReports = filteredReports.Where(d => d.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            filteredReports = filteredReports.Where(d => d.Severity == severity);
        }

        if (fromDate.HasValue)
        {
            filteredReports = filteredReports.Where(d => d.ReportedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filteredReports = filteredReports.Where(d => d.ReportedOn <= toDate.Value);
        }

        var orderedReports = filteredReports.OrderByDescending(d => d.DamageReportId).ToList();
        var pagedReports = orderedReports.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new LoadOperation<DamageReport>
        {
            ItemCollection = pagedReports,
            TotalRows = orderedReports.Count
        };
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
        // Get rentals for the shop
        var rentals = await Context.LoadAsync(
            Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToHashSet();

        // Load all damage reports and filter in memory (custom query provider doesn't support Contains)
        var allDamageReports = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>(),
            page: 1, size: 10000, includeTotalRows: false);

        var filteredReports = allDamageReports.ItemCollection
            .Where(d => rentalIds.Contains(d.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            filteredReports = filteredReports.Where(d => d.Status == status);
        }

        var damageReportsList = filteredReports
            .OrderByDescending(d => d.DamageReportId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        if (damageReportsList.Count == 0)
        {
            return [];
        }

        // Get renter info - load all and filter in memory
        var renterIds = rentals.ItemCollection.Select(r => r.RenterId).Distinct().ToHashSet();
        var allRenters = await Context.LoadAsync(
            Context.CreateQuery<Renter>(),
            page: 1, size: 10000, includeTotalRows: false);
        var rentersResult = allRenters.ItemCollection.Where(r => renterIds.Contains(r.RenterId)).ToList();

        // Get vehicle info - load all and filter in memory
        var vehicleIds = rentals.ItemCollection.Select(r => r.VehicleId).Distinct().ToHashSet();
        var allVehicles = await Context.LoadAsync(
            Context.CreateQuery<Vehicle>(),
            page: 1, size: 10000, includeTotalRows: false);
        var vehiclesResult = allVehicles.ItemCollection.Where(v => vehicleIds.Contains(v.VehicleId)).ToList();

        // Get damage photos - load all and filter in memory
        var damageReportIds = damageReportsList.Select(d => d.DamageReportId).ToHashSet();
        var allPhotos = await Context.LoadAsync(
            Context.CreateQuery<DamagePhoto>(),
            page: 1, size: 10000, includeTotalRows: false);
        var photosResult = allPhotos.ItemCollection.Where(p => damageReportIds.Contains(p.DamageReportId)).ToList();

        var rentersDict = rentersResult.ToDictionary(r => r.RenterId);
        var vehiclesDict = vehiclesResult.ToDictionary(v => v.VehicleId);
        var rentalsDict = rentals.ItemCollection.ToDictionary(r => r.RentalId);
        var photosDict = photosResult.GroupBy(p => p.DamageReportId)
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
        var rentals = await Context.LoadAsync(
            Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToHashSet();

        // Load all damage reports and filter in memory (custom query provider doesn't support Contains)
        var allDamageReports = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>(),
            page: 1, size: 10000, includeTotalRows: false);

        var shopDamageReports = allDamageReports.ItemCollection
            .Where(d => rentalIds.Contains(d.RentalId))
            .ToList();

        return shopDamageReports
            .GroupBy(d => d.Status ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets total estimated damage costs by status.
    /// </summary>
    public async Task<DamageCostSummary> GetCostSummaryAsync(int shopId)
    {
        var rentals = await Context.LoadAsync(
            Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToHashSet();

        // Load all damage reports and filter in memory (custom query provider doesn't support Contains)
        var allDamageReports = await Context.LoadAsync(
            Context.CreateQuery<DamageReport>(),
            page: 1, size: 10000, includeTotalRows: false);

        var shopDamageReports = allDamageReports.ItemCollection
            .Where(d => rentalIds.Contains(d.RentalId))
            .ToList();

        var pending = shopDamageReports.Where(d => d.Status == "Pending").ToList();
        var charged = shopDamageReports.Where(d => d.Status == "Charged").ToList();
        var waived = shopDamageReports.Where(d => d.Status == "Waived").ToList();
        var insuranceClaim = shopDamageReports.Where(d => d.Status == "InsuranceClaim").ToList();

        return new DamageCostSummary
        {
            PendingCount = pending.Count,
            PendingEstimatedCost = pending.Sum(d => d.EstimatedCost),
            ChargedCount = charged.Count,
            ChargedAmount = charged.Sum(d => d.ActualCost ?? d.EstimatedCost),
            WaivedCount = waived.Count,
            WaivedAmount = waived.Sum(d => d.EstimatedCost),
            InsuranceClaimCount = insuranceClaim.Count,
            InsuranceClaimAmount = insuranceClaim.Sum(d => d.EstimatedCost)
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
