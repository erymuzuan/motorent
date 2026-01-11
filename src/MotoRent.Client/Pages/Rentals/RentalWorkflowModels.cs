using MotoRent.Domain.Entities;

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

    // Pricing
    public decimal VehicleTotal { get; set; }
    public decimal InsuranceTotal { get; set; }
    public decimal AccessoriesTotal { get; set; }
    public decimal DriverTotal => IncludeDriver ? DriverDailyFee * Days : 0;
    public decimal GuideTotal => IncludeGuide ? GuideDailyFee * Days : 0;
    public decimal TotalAmount => VehicleTotal + InsuranceTotal + AccessoriesTotal + DriverTotal + GuideTotal;
    public int Days => Math.Max(1, (int)(EndDate.Date - StartDate.Date).TotalDays + 1);

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
