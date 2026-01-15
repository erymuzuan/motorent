using MotoRent.Domain.Entities;
using MotoRent.Services;

namespace MotoRent.Client.Pages.Rentals;

/// <summary>
/// Configuration for a rental (dates, insurance, accessories, pricing).
/// </summary>
public class RentalConfig
{
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.Now.AddDays(3);
    public int? SelectedInsuranceId { get; set; }
    public Insurance? SelectedInsurance { get; set; }
    public List<AccessorySelection> SelectedAccessories { get; set; } = [];

    // Driver and Guide options (for boats/vans)
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverDailyFee { get; set; }
    public decimal GuideDailyFee { get; set; }

    // Pick-up/Drop-off location options
    public int? PickupLocationId { get; set; }
    public ServiceLocation? PickupLocation { get; set; }
    public TimeSpan PickupTime { get; set; } = new(10, 0, 0);

    public int? DropoffLocationId { get; set; }
    public ServiceLocation? DropoffLocation { get; set; }
    public TimeSpan DropoffTime { get; set; } = new(10, 0, 0);

    // Location and out-of-hours pricing (calculated by RentalPricingService)
    public LocationPricing? LocationPricing { get; set; }

    // Pricing
    public decimal VehicleTotal { get; set; }
    public decimal InsuranceTotal { get; set; }
    public decimal AccessoriesTotal { get; set; }
    public decimal DriverTotal => IncludeDriver ? DriverDailyFee * Days : 0;
    public decimal GuideTotal => IncludeGuide ? GuideDailyFee * Days : 0;
    public decimal LocationFeesTotal => LocationPricing?.TotalLocationFees ?? 0;
    public decimal TotalAmount => VehicleTotal + InsuranceTotal + AccessoriesTotal + DriverTotal + GuideTotal + LocationFeesTotal;
    public int Days => Math.Max(1, (int)(EndDate.Date - StartDate.Date).TotalDays + 1);

    // Dynamic Pricing
    /// <summary>Whether dynamic pricing was applied.</summary>
    public bool DynamicPricingApplied { get; set; }

    /// <summary>Base vehicle total before dynamic pricing adjustment.</summary>
    public decimal BaseVehicleTotal { get; set; }

    /// <summary>Total adjustment amount from dynamic pricing.</summary>
    public decimal DynamicPricingAdjustment => VehicleTotal - BaseVehicleTotal;

    /// <summary>Average multiplier applied across all rental days.</summary>
    public decimal AverageMultiplier { get; set; } = 1.0m;

    /// <summary>Summary of applied pricing rules (e.g., "High Season +30%").</summary>
    public string? AppliedRuleSummary { get; set; }

    /// <summary>Per-day pricing breakdown with dynamic adjustments.</summary>
    public List<DayPricingInfo> DayBreakdown { get; set; } = [];

    /// <summary>Formatted percentage change from dynamic pricing.</summary>
    public string PercentageChange
    {
        get
        {
            if (!DynamicPricingApplied) return "";
            var pct = (AverageMultiplier - 1.0m) * 100;
            return pct >= 0 ? $"+{pct:N0}%" : $"{pct:N0}%";
        }
    }

    // Backward compatibility
    [Obsolete("Use VehicleTotal instead")]
    public decimal MotorbikeTotal
    {
        get => VehicleTotal;
        set => VehicleTotal = value;
    }

    public bool IsValid => EndDate >= StartDate;

    public class AccessorySelection
    {
        public Accessory Accessory { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal TotalAmount => Accessory.DailyRate * Quantity;
    }
}

/// <summary>
/// Configuration for interval-based rentals (jet skis).
/// </summary>
public class IntervalRentalConfig
{
    public DateTimeOffset StartDateTime { get; set; } = DateTimeOffset.Now;
    public int IntervalMinutes { get; set; } = 60;
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTimeOffset EndDateTime => StartDateTime.AddMinutes(IntervalMinutes);
    public bool IsValid => IntervalMinutes > 0 && RentalRate > 0;
}

/// <summary>
/// Information about the deposit collected and rental payment.
/// </summary>
public class DepositInfo
{
    public string DepositType { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }
    public bool IsCollected { get; set; }

    // Rental payment info
    public string PaymentMethod { get; set; } = "Cash";
    public string? PaymentTransactionRef { get; set; }
}

/// <summary>
/// Per-day pricing information for display in UI.
/// </summary>
public class DayPricingInfo
{
    public DateOnly Date { get; set; }
    public decimal BaseRate { get; set; }
    public decimal AdjustedRate { get; set; }
    public decimal Multiplier { get; set; } = 1.0m;
    public string? RuleName { get; set; }
    public bool HasAdjustment => Multiplier != 1.0m;
}
