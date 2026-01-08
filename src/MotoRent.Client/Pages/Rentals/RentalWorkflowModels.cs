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
    public decimal MotorbikeTotal { get; set; }
    public decimal InsuranceTotal { get; set; }
    public decimal AccessoriesTotal { get; set; }
    public decimal TotalAmount => MotorbikeTotal + InsuranceTotal + AccessoriesTotal;
    public int Days => Math.Max(1, (int)(EndDate.Date - StartDate.Date).TotalDays + 1);

    public bool IsValid => EndDate > StartDate;

    public class AccessorySelection
    {
        public Accessory Accessory { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal TotalAmount => Accessory.DailyRate * Quantity;
    }
}

/// <summary>
/// Information about the deposit collected.
/// </summary>
public class DepositInfo
{
    public string DepositType { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }
    public bool IsCollected { get; set; }
}
