using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Calculates depreciation amounts using various methods.
/// </summary>
public class DepreciationCalculator
{
    /// <summary>
    /// Calculate depreciation for a specific period.
    /// </summary>
    public DepreciationCalculation Calculate(
        Asset asset,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        var result = new DepreciationCalculation
        {
            Asset = asset,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Method = asset.DepreciationMethod,
            BookValueStart = asset.CurrentBookValue,
            Amount = 0
        };

        // Check if asset should depreciate
        if (asset.Status != AssetStatus.Active)
        {
            result.Message = "Asset is not active";
            return result;
        }

        if (asset.CurrentBookValue <= asset.ResidualValue)
        {
            result.Message = "Asset is fully depreciated";
            return result;
        }

        // Calculate number of months in period
        var months = CalculateMonthsInPeriod(periodStart, periodEnd);
        if (months <= 0)
        {
            result.Message = "Invalid period";
            return result;
        }

        // Calculate based on method
        decimal amount = asset.DepreciationMethod switch
        {
            DepreciationMethod.DayOutOfDoor => this.CalculateDayOutOfDoor(asset),
            DepreciationMethod.StraightLine => this.CalculateStraightLine(asset, months),
            DepreciationMethod.DecliningBalance => this.CalculateDecliningBalance(asset, months),
            DepreciationMethod.Custom => this.CalculateCustom(asset, periodStart),
            DepreciationMethod.HybridDayOutThenStraightLine => this.CalculateHybridStraightLine(asset, months),
            DepreciationMethod.HybridDayOutThenDeclining => this.CalculateHybridDeclining(asset, months),
            _ => 0
        };

        // Ensure we don't depreciate below residual value
        var maxDepreciation = asset.CurrentBookValue - asset.ResidualValue;
        result.Amount = Math.Min(amount, maxDepreciation);
        result.BookValueEnd = asset.CurrentBookValue - result.Amount;

        return result;
    }

    /// <summary>
    /// Project future depreciation for a number of months.
    /// </summary>
    public List<DepreciationProjection> ProjectDepreciation(Asset asset, int monthsAhead)
    {
        var projections = new List<DepreciationProjection>();

        // Create a working copy of book value
        var workingBookValue = asset.CurrentBookValue;
        var currentDate = DateTimeOffset.Now;

        for (int i = 0; i < monthsAhead; i++)
        {
            var periodStart = currentDate.AddMonths(i);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            // Stop if fully depreciated
            if (workingBookValue <= asset.ResidualValue)
                break;

            // Calculate for this period
            var tempAsset = new Asset
            {
                AcquisitionCost = asset.AcquisitionCost,
                CurrentBookValue = workingBookValue,
                ResidualValue = asset.ResidualValue,
                UsefulLifeMonths = asset.UsefulLifeMonths,
                DepreciationMethod = asset.DepreciationMethod,
                DayOutOfDoorPercent = asset.DayOutOfDoorPercent,
                DecliningBalanceRate = asset.DecliningBalanceRate,
                FirstRentalDate = asset.FirstRentalDate,
                Status = AssetStatus.Active
            };

            var calc = this.Calculate(tempAsset, periodStart, periodEnd);

            projections.Add(new DepreciationProjection
            {
                Month = i + 1,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Amount = calc.Amount,
                BookValueStart = workingBookValue,
                BookValueEnd = calc.BookValueEnd,
                AccumulatedDepreciation = (asset.AcquisitionCost - calc.BookValueEnd)
            });

            workingBookValue = calc.BookValueEnd;
        }

        return projections;
    }

    /// <summary>
    /// Generate full amortization schedule from acquisition to residual value.
    /// </summary>
    public List<DepreciationProjection> GenerateFullSchedule(Asset asset)
    {
        // Reset to initial state
        var initialBookValue = asset.IsPreExisting && asset.InitialBookValue.HasValue
            ? asset.InitialBookValue.Value
            : asset.AcquisitionCost;

        var tempAsset = new Asset
        {
            AcquisitionCost = asset.AcquisitionCost,
            CurrentBookValue = initialBookValue,
            ResidualValue = asset.ResidualValue,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            DepreciationMethod = asset.DepreciationMethod,
            DayOutOfDoorPercent = asset.DayOutOfDoorPercent,
            DecliningBalanceRate = asset.DecliningBalanceRate,
            FirstRentalDate = asset.FirstRentalDate,
            Status = AssetStatus.Active
        };

        // Project for useful life + buffer
        return this.ProjectDepreciation(tempAsset, asset.UsefulLifeMonths + 12);
    }

    #region Method-Specific Calculations

    /// <summary>
    /// Day Out of Door: Immediate depreciation on first rental.
    /// </summary>
    private decimal CalculateDayOutOfDoor(Asset asset)
    {
        // Day out of door only applies once (when first rental happens)
        if (!asset.FirstRentalDate.HasValue)
            return 0; // No first rental yet

        var percent = asset.DayOutOfDoorPercent ?? 0.20m; // Default 20%

        // If accumulated depreciation is less than the day-out-of-door amount,
        // we need to apply it
        var dayOutAmount = asset.AcquisitionCost * percent;

        if (asset.AccumulatedDepreciation < dayOutAmount)
        {
            // Return the remaining day-out-of-door amount
            return dayOutAmount - asset.AccumulatedDepreciation;
        }

        // Day out of door already fully applied
        return 0;
    }

    /// <summary>
    /// Straight Line: Equal amounts over useful life.
    /// </summary>
    private decimal CalculateStraightLine(Asset asset, int months)
    {
        if (asset.UsefulLifeMonths <= 0)
            return 0;

        var monthlyAmount = asset.DepreciableBase / asset.UsefulLifeMonths;
        return monthlyAmount * months;
    }

    /// <summary>
    /// Declining Balance: Percentage of book value.
    /// </summary>
    private decimal CalculateDecliningBalance(Asset asset, int months)
    {
        var annualRate = asset.DecliningBalanceRate ?? 0.25m; // Default 25% per year
        var monthlyRate = annualRate / 12;

        decimal totalDepreciation = 0;
        var workingBookValue = asset.CurrentBookValue;

        for (int i = 0; i < months; i++)
        {
            var monthlyAmount = workingBookValue * monthlyRate;

            // Don't depreciate below residual
            if (workingBookValue - monthlyAmount < asset.ResidualValue)
            {
                totalDepreciation += workingBookValue - asset.ResidualValue;
                break;
            }

            totalDepreciation += monthlyAmount;
            workingBookValue -= monthlyAmount;
        }

        return totalDepreciation;
    }

    /// <summary>
    /// Custom: User-defined schedule.
    /// </summary>
    private decimal CalculateCustom(Asset asset, DateTimeOffset periodStart)
    {
        if (asset.CustomSchedule == null || asset.CustomSchedule.Count == 0)
            return 0;

        // Determine which month we're in
        var startDate = asset.IsPreExisting && asset.SystemEntryDate.HasValue
            ? asset.SystemEntryDate.Value
            : asset.AcquisitionDate;

        var monthNumber = (int)((periodStart - startDate).TotalDays / 30.44) + 1;

        // Find the scheduled amount for this month
        var entry = asset.CustomSchedule.FirstOrDefault(e => e.MonthNumber == monthNumber);
        return entry?.Amount ?? 0;
    }

    /// <summary>
    /// Hybrid: Day Out of Door, then Straight Line.
    /// </summary>
    private decimal CalculateHybridStraightLine(Asset asset, int months)
    {
        // Check if day-out-of-door needs to be applied
        var dayOutAmount = this.CalculateDayOutOfDoor(asset);
        if (dayOutAmount > 0)
            return dayOutAmount;

        // Otherwise, continue with straight line (adjusted for post-day-out basis)
        return this.CalculateStraightLine(asset, months);
    }

    /// <summary>
    /// Hybrid: Day Out of Door, then Declining Balance.
    /// </summary>
    private decimal CalculateHybridDeclining(Asset asset, int months)
    {
        // Check if day-out-of-door needs to be applied
        var dayOutAmount = this.CalculateDayOutOfDoor(asset);
        if (dayOutAmount > 0)
            return dayOutAmount;

        // Otherwise, continue with declining balance
        return this.CalculateDecliningBalance(asset, months);
    }

    #endregion

    #region Utility Methods

    private static int CalculateMonthsInPeriod(DateTimeOffset start, DateTimeOffset end)
    {
        var totalDays = (end - start).TotalDays;
        return Math.Max(1, (int)Math.Round(totalDays / 30.44));
    }

    /// <summary>
    /// Calculate the book value at a specific date based on depreciation method.
    /// </summary>
    public decimal CalculateBookValueAtDate(Asset asset, DateTimeOffset targetDate)
    {
        var startDate = asset.IsPreExisting && asset.SystemEntryDate.HasValue
            ? asset.SystemEntryDate.Value
            : asset.AcquisitionDate;

        if (targetDate <= startDate)
            return asset.IsPreExisting && asset.InitialBookValue.HasValue
                ? asset.InitialBookValue.Value
                : asset.AcquisitionCost;

        var months = CalculateMonthsInPeriod(startDate, targetDate);

        // Create temp asset at starting state
        var tempAsset = new Asset
        {
            AcquisitionCost = asset.AcquisitionCost,
            CurrentBookValue = asset.IsPreExisting && asset.InitialBookValue.HasValue
                ? asset.InitialBookValue.Value
                : asset.AcquisitionCost,
            ResidualValue = asset.ResidualValue,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            DepreciationMethod = asset.DepreciationMethod,
            DayOutOfDoorPercent = asset.DayOutOfDoorPercent,
            DecliningBalanceRate = asset.DecliningBalanceRate,
            FirstRentalDate = asset.FirstRentalDate,
            AccumulatedDepreciation = 0,
            Status = AssetStatus.Active
        };

        var calc = this.Calculate(tempAsset, startDate, targetDate);
        return calc.BookValueEnd;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Result of a depreciation calculation.
/// </summary>
public class DepreciationCalculation
{
    public Asset Asset { get; set; } = null!;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public DepreciationMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal BookValueStart { get; set; }
    public decimal BookValueEnd { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// A projected depreciation entry.
/// </summary>
public class DepreciationProjection
{
    public int Month { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public decimal BookValueStart { get; set; }
    public decimal BookValueEnd { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
}

#endregion
