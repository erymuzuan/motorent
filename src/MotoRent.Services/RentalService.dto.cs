using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Services;

public class CheckInRequest
{
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int VehicleId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public int MileageStart { get; set; }
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }
    public int? InsuranceId { get; set; }
    public string? Notes { get; set; }

    // Duration type for different vehicle types
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;
    public int? IntervalMinutes { get; set; }

    // Driver/Guide options
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }

    // Payment info
    public string PaymentMethod { get; set; } = "Cash";
    public string? PaymentTransactionRef { get; set; }

    // Deposit info
    public string DepositType { get; set; } = "Cash";
    public decimal DepositAmount { get; set; }
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }

    // Accessories
    public List<AccessorySelection> Accessories { get; set; } = [];

    // Agreement
    public string? AgreementText { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? SignedByIp { get; set; }

    // Pick-up location and fees
    public int? PickupLocationId { get; set; }
    public string? PickupLocationName { get; set; }
    public TimeSpan? ScheduledPickupTime { get; set; }
    public decimal PickupLocationFee { get; set; }
    public decimal OutOfHoursPickupFee { get; set; }
    public bool IsOutOfHoursPickup { get; set; }
    public string? OutOfHoursPickupBand { get; set; }

    // Expected drop-off location and fees (for reservations)
    public int? DropoffLocationId { get; set; }
    public string? DropoffLocationName { get; set; }
    public TimeSpan? ScheduledDropoffTime { get; set; }
    public decimal DropoffLocationFee { get; set; }
    public decimal OutOfHoursDropoffFee { get; set; }
    public bool IsOutOfHoursDropoff { get; set; }
    public string? OutOfHoursDropoffBand { get; set; }

    // Pre-rental inspection
    public InspectionInfo? PreRentalInspection { get; set; }

    // Till session for recording payment
    public int? TillSessionId { get; set; }

    // Backward compatibility
    [Obsolete("Use VehicleId instead")]
    public int MotorbikeId { get => VehicleId; set => VehicleId = value; }

    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate { get => RentalRate; set => RentalRate = value; }
}

public class AccessorySelection
{
    public int AccessoryId { get; set; }
    public int Quantity { get; set; }
    public decimal ChargedAmount { get; set; }
}

public class CheckOutRequest
{
    public int RentalId { get; set; }
    public DateTimeOffset ActualEndDate { get; set; }
    public int MileageEnd { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
    public bool RefundDeposit { get; set; } = true;
    public decimal? DeductionAmount { get; set; }

    // For cross-shop returns (pooled vehicles)
    public int? ReturnShopId { get; set; }

    // Drop-off location and fees
    public int? DropoffLocationId { get; set; }
    public string? DropoffLocationName { get; set; }
    public TimeSpan? ScheduledDropoffTime { get; set; }
    public decimal DropoffLocationFee { get; set; }
    public decimal OutOfHoursDropoffFee { get; set; }
    public bool IsOutOfHoursDropoff { get; set; }
    public string? OutOfHoursDropoffBand { get; set; }

    // Post-rental inspection
    public InspectionInfo? PostRentalInspection { get; set; }

    public List<DamageReportInfo>? DamageReports { get; set; }
}

public class DamageReportInfo
{
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Minor";
    public decimal EstimatedCost { get; set; }
    public List<string>? PhotoPaths { get; set; }
}

public class CheckInResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }

    public static CheckInResult CreateSuccess(int rentalId) => new()
    {
        Success = true,
        RentalId = rentalId
    };

    public static CheckInResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class CheckOutResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }
    public decimal AdditionalCharges { get; set; }
    public decimal RefundAmount { get; set; }
    public int ExtraDays { get; set; }
    public bool IsCrossShopReturn { get; set; }

    public static CheckOutResult CreateSuccess(int rentalId, decimal additionalCharges, decimal refundAmount, int extraDays, bool isCrossShopReturn = false) => new()
    {
        Success = true,
        RentalId = rentalId,
        AdditionalCharges = additionalCharges,
        RefundAmount = refundAmount,
        ExtraDays = extraDays,
        IsCrossShopReturn = isCrossShopReturn
    };

    public static CheckOutResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class ReservationRequest
{
    public int ShopId { get; set; }
    public int VehicleId { get; set; }

    /// <summary>
    /// For group-based reservations: the vehicle group key (Brand|Model|Year|Type|Engine).
    /// When set, VehicleId may be 0 until a specific vehicle is assigned at check-in.
    /// </summary>
    public string? VehicleGroupKey { get; set; }

    /// <summary>
    /// Optional color preference for group reservations (not guaranteed).
    /// </summary>
    public string? PreferredColor { get; set; }

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int? InsuranceId { get; set; }
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }

    // Duration type
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;
    public int? IntervalMinutes { get; set; }

    // Driver/Guide options
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }

    // Contact info
    public string RenterName { get; set; } = string.Empty;
    public string RenterPhone { get; set; } = string.Empty;
    public string RenterEmail { get; set; } = string.Empty;
    public string? RenterNationality { get; set; }
    public string? RenterPassport { get; set; }
    public string? HotelName { get; set; }
    public string? Notes { get; set; }

    // Backward compatibility
    [Obsolete("Use VehicleId instead")]
    public int MotorbikeId { get => VehicleId; set => VehicleId = value; }

    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate { get => RentalRate; set => RentalRate = value; }
}

public class ReservationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }
    public string? ConfirmationCode { get; set; }

    public static ReservationResult CreateSuccess(int rentalId, string confirmationCode) => new()
    {
        Success = true,
        RentalId = rentalId,
        ConfirmationCode = confirmationCode
    };

    public static ReservationResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// Statistics about dynamic pricing usage and revenue impact.
/// </summary>
public class DynamicPricingStats
{
    /// <summary>
    /// Total number of rentals in the period.
    /// </summary>
    public int TotalRentals { get; set; }

    /// <summary>
    /// Number of rentals with dynamic pricing applied.
    /// </summary>
    public int RentalsWithDynamicPricing { get; set; }

    /// <summary>
    /// What revenue would have been without dynamic pricing.
    /// </summary>
    public decimal BaseRevenue { get; set; }

    /// <summary>
    /// Actual revenue with dynamic pricing.
    /// </summary>
    public decimal ActualRevenue { get; set; }

    /// <summary>
    /// Additional revenue earned from dynamic pricing (ActualRevenue - BaseRevenue).
    /// </summary>
    public decimal DynamicPricingPremium { get; set; }

    /// <summary>
    /// Average multiplier applied across all dynamic pricing rentals.
    /// </summary>
    public decimal AverageMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Breakdown by pricing rule name (e.g., "High Season" -> count of days applied).
    /// </summary>
    public Dictionary<string, int> RuleBreakdown { get; set; } = [];

    /// <summary>
    /// Percentage of rentals using dynamic pricing.
    /// </summary>
    public decimal DynamicPricingUsageRate => TotalRentals > 0
        ? (decimal)RentalsWithDynamicPricing / TotalRentals * 100
        : 0;

    /// <summary>
    /// Percentage increase in revenue from dynamic pricing.
    /// </summary>
    public decimal RevenueIncrease => BaseRevenue > 0
        ? DynamicPricingPremium / BaseRevenue * 100
        : 0;
}
