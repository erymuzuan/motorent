namespace MotoRent.Domain.Entities;

public class Rental : Entity
{
    public int RentalId { get; set; }
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int MotorbikeId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public DateTimeOffset? ActualEndDate { get; set; }
    public int MileageStart { get; set; }
    public int? MileageEnd { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Reserved";  // Reserved, Active, Completed, Cancelled
    public string? Notes { get; set; }

    // Related
    public int? InsuranceId { get; set; }
    public int? DepositId { get; set; }

    public override int GetId() => this.RentalId;
    public override void SetId(int value) => this.RentalId = value;
}
