namespace MotoRent.Domain.Entities;

public class Insurance : Entity
{
    public int InsuranceId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;      // Basic, Premium, Full Coverage
    public string Description { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public decimal MaxCoverage { get; set; }
    public decimal Deductible { get; set; }
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.InsuranceId;
    public override void SetId(int value) => this.InsuranceId = value;
}
