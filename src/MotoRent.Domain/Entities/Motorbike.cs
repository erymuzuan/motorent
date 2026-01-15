namespace MotoRent.Domain.Entities;

public class Motorbike : Entity
{
    public int MotorbikeId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;     // Honda, Yamaha, etc.
    public string Model { get; set; } = string.Empty;     // Click, PCX, Aerox
    public int EngineCC { get; set; }                     // 110, 125, 150, etc.
    public string? Color { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = "Available";     // Available, Rented, Maintenance
    public decimal DailyRate { get; set; }
    public decimal DepositAmount { get; set; }
    public string? ImagePath { get; set; }
    public string? Notes { get; set; }
    public int Mileage { get; set; }
    public DateTimeOffset? LastServiceDate { get; set; }

    public override int GetId() => this.MotorbikeId;
    public override void SetId(int value) => this.MotorbikeId = value;
}
