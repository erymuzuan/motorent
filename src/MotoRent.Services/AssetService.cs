using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing vehicle assets and depreciation.
/// </summary>
public class AssetService(RentalDataContext context, DepreciationCalculator calculator)
{
    private RentalDataContext Context { get; } = context;
    private DepreciationCalculator Calculator { get; } = calculator;

    #region Asset CRUD

    /// <summary>
    /// Get all assets with optional status filter.
    /// </summary>
    public async Task<LoadOperation<Asset>> GetAssetsAsync(
        AssetStatus? status = null,
        int page = 1,
        int size = 50)
    {
        var query = this.Context.CreateQuery<Asset>();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = query.OrderByDescending(a => a.AcquisitionDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Get asset by ID.
    /// </summary>
    public async Task<Asset?> GetAssetByIdAsync(int assetId)
    {
        return await this.Context.LoadOneAsync<Asset>(a => a.AssetId == assetId);
    }

    /// <summary>
    /// Get asset by vehicle ID.
    /// </summary>
    public async Task<Asset?> GetAssetByVehicleIdAsync(int vehicleId)
    {
        return await this.Context.LoadOneAsync<Asset>(a => a.VehicleId == vehicleId);
    }

    /// <summary>
    /// Create a new asset record for a vehicle.
    /// </summary>
    public async Task<SubmitOperation> CreateAssetAsync(Asset asset, string username)
    {
        // Set initial book value
        if (asset.IsPreExisting && asset.InitialBookValue.HasValue)
        {
            asset.CurrentBookValue = asset.InitialBookValue.Value;
            asset.SystemEntryDate = DateTimeOffset.Now;
        }
        else
        {
            asset.CurrentBookValue = asset.AcquisitionCost;
        }

        asset.AccumulatedDepreciation = 0;
        asset.TotalExpenses = 0;
        asset.TotalRevenue = 0;
        asset.Status = AssetStatus.Active;

        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("CreateAsset");
    }

    /// <summary>
    /// Update an existing asset.
    /// </summary>
    public async Task<SubmitOperation> UpdateAssetAsync(Asset asset, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("UpdateAsset");
    }

    #endregion

    #region Depreciation Operations

    /// <summary>
    /// Calculate and record depreciation for an asset.
    /// </summary>
    public async Task<SubmitOperation> RecordDepreciationAsync(
        int assetId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string username,
        decimal? overrideAmount = null,
        string? overrideReason = null,
        string? approvedBy = null)
    {
        var asset = await this.GetAssetByIdAsync(assetId);
        if (asset == null)
            return SubmitOperation.CreateFailure("Asset not found");

        // Calculate depreciation
        var calculation = this.Calculator.Calculate(asset, periodStart, periodEnd);

        // Create entry
        var entry = new DepreciationEntry
        {
            AssetId = assetId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Method = calculation.Method,
            BookValueStart = calculation.BookValueStart,
            OriginalCalculatedAmount = calculation.Amount
        };

        // Apply override if provided
        if (overrideAmount.HasValue)
        {
            entry.Amount = overrideAmount.Value;
            entry.IsManualOverride = true;
            entry.OverrideReason = overrideReason;
            entry.ApprovedBy = approvedBy;
            entry.EntryType = DepreciationEntryType.Manual;
        }
        else
        {
            entry.Amount = calculation.Amount;
            entry.IsManualOverride = false;
            entry.EntryType = DepreciationEntryType.System;
        }

        entry.BookValueEnd = calculation.BookValueStart - entry.Amount;

        // Update asset
        asset.AccumulatedDepreciation += entry.Amount;
        asset.CurrentBookValue = entry.BookValueEnd;
        asset.LastDepreciationDate = periodEnd;

        using var session = this.Context.OpenSession(username);
        session.Attach(entry);
        session.Attach(asset);
        return await session.SubmitChanges("RecordDepreciation");
    }

    /// <summary>
    /// Record depreciation triggered by first rental (Day Out of Door).
    /// </summary>
    public async Task<SubmitOperation> RecordFirstRentalAsync(
        int assetId,
        DateTimeOffset firstRentalDate,
        string username)
    {
        var asset = await this.GetAssetByIdAsync(assetId);
        if (asset == null)
            return SubmitOperation.CreateFailure("Asset not found");

        // Check if day-out-of-door applies
        var needsDayOutOfDoor = asset.DepreciationMethod == DepreciationMethod.DayOutOfDoor ||
                                asset.DepreciationMethod == DepreciationMethod.HybridDayOutThenStraightLine ||
                                asset.DepreciationMethod == DepreciationMethod.HybridDayOutThenDeclining;

        if (!needsDayOutOfDoor)
            return SubmitOperation.CreateSuccess(0, 0, 0); // Nothing to do

        // Check if already has first rental
        if (asset.FirstRentalDate.HasValue)
            return SubmitOperation.CreateSuccess(0, 0, 0); // Already triggered

        // Set first rental date
        asset.FirstRentalDate = firstRentalDate;

        // Record the day-out-of-door depreciation
        return await this.RecordDepreciationAsync(
            assetId,
            firstRentalDate,
            firstRentalDate,
            username);
    }

    /// <summary>
    /// Run batch depreciation for all assets needing it.
    /// </summary>
    public async Task<BatchDepreciationResult> RunMonthlyDepreciationAsync(
        DateTimeOffset periodEnd,
        string username)
    {
        var result = new BatchDepreciationResult();

        // Get all active assets
        var assetsResult = await this.GetAssetsAsync(AssetStatus.Active, 1, 10000);

        var periodStart = new DateTimeOffset(periodEnd.Year, periodEnd.Month, 1, 0, 0, 0, periodEnd.Offset);

        foreach (var asset in assetsResult.ItemCollection)
        {
            // Skip if already depreciated this month
            if (asset.LastDepreciationDate.HasValue &&
                asset.LastDepreciationDate.Value >= periodStart)
            {
                result.Skipped++;
                continue;
            }

            // Skip if fully depreciated
            if (asset.CurrentBookValue <= asset.ResidualValue)
            {
                result.FullyDepreciated++;
                continue;
            }

            try
            {
                var submitResult = await this.RecordDepreciationAsync(
                    asset.AssetId,
                    periodStart,
                    periodEnd,
                    username);

                if (!submitResult.Success)
                {
                    result.Failed++;
                    result.Errors.Add($"Asset {asset.AssetId}: {submitResult.Message}");
                }
                else
                {
                    result.Processed++;
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Asset {asset.AssetId}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Get depreciation entries for an asset.
    /// </summary>
    public async Task<List<DepreciationEntry>> GetDepreciationEntriesAsync(int assetId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<DepreciationEntry>()
                .Where(e => e.AssetId == assetId)
                .OrderByDescending(e => e.PeriodEnd),
            page: 1, size: 500, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Project future depreciation for an asset.
    /// </summary>
    public List<DepreciationProjection> GetDepreciationProjections(Asset asset, int monthsAhead = 24)
    {
        return this.Calculator.ProjectDepreciation(asset, monthsAhead);
    }

    #endregion

    #region Disposal Operations

    /// <summary>
    /// Dispose of an asset (sell, trade-in, etc.).
    /// </summary>
    public async Task<SubmitOperation> DisposeAssetAsync(
        int assetId,
        decimal disposalAmount,
        DateTimeOffset disposalDate,
        string? notes,
        string username)
    {
        var asset = await this.GetAssetByIdAsync(assetId);
        if (asset == null)
            return SubmitOperation.CreateFailure("Asset not found");

        asset.Status = AssetStatus.Disposed;
        asset.DisposalDate = disposalDate;
        asset.DisposalAmount = disposalAmount;
        asset.DisposalGainLoss = disposalAmount - asset.CurrentBookValue;

        if (!string.IsNullOrEmpty(notes))
            asset.Notes = (asset.Notes ?? "") + $"\n[Disposal: {notes}]";

        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("DisposeAsset");
    }

    /// <summary>
    /// Write off an asset (total loss, theft, etc.).
    /// </summary>
    public async Task<SubmitOperation> WriteOffAssetAsync(
        int assetId,
        string reason,
        DateTimeOffset writeOffDate,
        string username)
    {
        var asset = await this.GetAssetByIdAsync(assetId);
        if (asset == null)
            return SubmitOperation.CreateFailure("Asset not found");

        asset.Status = AssetStatus.WriteOff;
        asset.DisposalDate = writeOffDate;
        asset.DisposalAmount = 0;
        asset.DisposalGainLoss = -asset.CurrentBookValue;
        asset.Notes = (asset.Notes ?? "") + $"\n[Write-off: {reason}]";

        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("WriteOffAsset");
    }

    #endregion

    #region Revenue & Expense Tracking

    /// <summary>
    /// Add revenue from a rental to the asset.
    /// </summary>
    public async Task<SubmitOperation> AddRevenueFromRentalAsync(
        int vehicleId,
        decimal amount,
        string username)
    {
        var asset = await this.GetAssetByVehicleIdAsync(vehicleId);
        if (asset == null)
            return SubmitOperation.CreateSuccess(0, 0, 0); // No asset record, skip

        asset.TotalRevenue += amount;

        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("AddRevenue");
    }

    /// <summary>
    /// Update total expenses on an asset.
    /// </summary>
    public async Task<SubmitOperation> UpdateExpenseTotalAsync(
        int assetId,
        decimal expenseAmount,
        string username)
    {
        var asset = await this.GetAssetByIdAsync(assetId);
        if (asset == null)
            return SubmitOperation.CreateFailure("Asset not found");

        asset.TotalExpenses += expenseAmount;

        using var session = this.Context.OpenSession(username);
        session.Attach(asset);
        return await session.SubmitChanges("UpdateExpenseTotal");
    }

    #endregion

    #region Summary & Reporting

    /// <summary>
    /// Get fleet-wide summary.
    /// </summary>
    public async Task<AssetSummary> GetSummaryAsync()
    {
        var assetsResult = await this.GetAssetsAsync(page: 1, size: 10000);
        var assets = assetsResult.ItemCollection;

        var activeAssets = assets.Where(a => a.Status == AssetStatus.Active).ToList();

        return new AssetSummary
        {
            TotalAssets = assets.Count,
            ActiveAssets = activeAssets.Count,
            DisposedAssets = assets.Count(a => a.Status == AssetStatus.Disposed),
            WrittenOffAssets = assets.Count(a => a.Status == AssetStatus.WriteOff),
            TotalAcquisitionCost = activeAssets.Sum(a => a.AcquisitionCost),
            TotalCurrentBookValue = activeAssets.Sum(a => a.CurrentBookValue),
            TotalAccumulatedDepreciation = activeAssets.Sum(a => a.AccumulatedDepreciation),
            TotalExpenses = activeAssets.Sum(a => a.TotalExpenses),
            TotalRevenue = activeAssets.Sum(a => a.TotalRevenue),
            NetProfitLoss = activeAssets.Sum(a => a.NetProfitLoss)
        };
    }

    /// <summary>
    /// Get vehicle profitability report.
    /// </summary>
    public async Task<List<VehicleProfitability>> GetVehicleProfitabilityAsync()
    {
        var assetsResult = await this.GetAssetsAsync(AssetStatus.Active, 1, 1000);

        return assetsResult.ItemCollection
            .Select(a => new VehicleProfitability
            {
                AssetId = a.AssetId,
                VehicleId = a.VehicleId,
                VehicleName = a.VehicleName ?? $"Vehicle {a.VehicleId}",
                LicensePlate = a.LicensePlate ?? "",
                AcquisitionCost = a.AcquisitionCost,
                CurrentBookValue = a.CurrentBookValue,
                AccumulatedDepreciation = a.AccumulatedDepreciation,
                TotalRevenue = a.TotalRevenue,
                TotalExpenses = a.TotalExpenses,
                NetProfitLoss = a.NetProfitLoss,
                ROIPercent = a.ROIPercent
            })
            .OrderByDescending(v => v.ROIPercent)
            .ToList();
    }

    #endregion

    #region Dashboard Support

    /// <summary>
    /// Get count of assets needing depreciation this month.
    /// </summary>
    public async Task<int> GetAssetsNeedingDepreciationCountAsync()
    {
        var currentMonthStart = new DateTimeOffset(
            DateTimeOffset.Now.Year,
            DateTimeOffset.Now.Month,
            1, 0, 0, 0,
            DateTimeOffset.Now.Offset);

        var assetsResult = await this.GetAssetsAsync(AssetStatus.Active, 1, 10000);

        return assetsResult.ItemCollection.Count(a =>
            a.CurrentBookValue > a.ResidualValue &&
            (!a.LastDepreciationDate.HasValue || a.LastDepreciationDate.Value < currentMonthStart));
    }

    /// <summary>
    /// Get count of underperforming assets (negative ROI).
    /// </summary>
    public async Task<int> GetUnderperformingAssetsCountAsync()
    {
        var assetsResult = await this.GetAssetsAsync(AssetStatus.Active, 1, 10000);
        return assetsResult.ItemCollection.Count(a => a.ROIPercent < 0);
    }

    /// <summary>
    /// Get recent activity for the dashboard.
    /// </summary>
    public async Task<List<AssetActivityItem>> GetRecentActivityAsync(int count = 10)
    {
        var activities = new List<AssetActivityItem>();

        // Get recent depreciation entries
        var depreciationEntries = await this.Context.LoadAsync(
            this.Context.CreateQuery<DepreciationEntry>()
                .OrderByDescending(e => e.CreatedTimestamp),
            page: 1, size: count, includeTotalRows: false);

        foreach (var entry in depreciationEntries.ItemCollection)
        {
            activities.Add(new AssetActivityItem
            {
                Type = "Depreciation",
                Title = $"Depreciation recorded",
                Description = $"Asset #{entry.AssetId} - {entry.Method}",
                Timestamp = entry.CreatedTimestamp,
                Amount = -entry.Amount
            });
        }

        // Get recent expenses
        var expenses = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetExpense>()
                .OrderByDescending(e => e.CreatedTimestamp),
            page: 1, size: count, includeTotalRows: false);

        foreach (var expense in expenses.ItemCollection)
        {
            activities.Add(new AssetActivityItem
            {
                Type = "Expense",
                Title = $"{expense.Category} expense",
                Description = expense.Description ?? $"Asset #{expense.AssetId}",
                Timestamp = expense.CreatedTimestamp,
                Amount = -expense.Amount
            });
        }

        // Sort by timestamp and take top N
        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }

    #endregion
}

#region DTOs

/// <summary>
/// Fleet-wide asset summary.
/// </summary>
public class AssetSummary
{
    public int TotalAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int DisposedAssets { get; set; }
    public int WrittenOffAssets { get; set; }
    public decimal TotalAcquisitionCost { get; set; }
    public decimal TotalCurrentBookValue { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal NetProfitLoss { get; set; }
}

/// <summary>
/// Vehicle profitability report item.
/// </summary>
public class VehicleProfitability
{
    public int AssetId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public decimal AcquisitionCost { get; set; }
    public decimal CurrentBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfitLoss { get; set; }
    public decimal ROIPercent { get; set; }
}

/// <summary>
/// Batch depreciation result.
/// </summary>
public class BatchDepreciationResult
{
    public int Processed { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int FullyDepreciated { get; set; }
    public List<string> Errors { get; set; } = [];
    public int TotalProcessed => this.Processed + this.Skipped + this.Failed + this.FullyDepreciated;
}

/// <summary>
/// Activity item for the dashboard.
/// </summary>
public class AssetActivityItem
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public decimal? Amount { get; set; }
}

#endregion
