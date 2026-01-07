namespace MotoRent.Domain.Entities;

public class DamageReport : Entity
{
    public int DamageReportId { get; set; }
    public int RentalId { get; set; }
    public int MotorbikeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Minor";  // Minor, Moderate, Major
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Charged, Waived, InsuranceClaim
    public DateTimeOffset ReportedOn { get; set; }

    public override int GetId() => this.DamageReportId;
    public override void SetId(int value) => this.DamageReportId = value;
}
