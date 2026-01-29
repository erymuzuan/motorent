using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

public class FleetModel : Entity
{
    public int FleetModelId { get; set; }

    public int? VehicleModelId { get; set; }

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleType VehicleType { get; set; } = VehicleType.Motorbike;

    // Engine specs
    public int? EngineCC { get; set; }
    public decimal? EngineLiters { get; set; }

    // Classification
    public CarSegment? Segment { get; set; }
    public string? Transmission { get; set; }

    // Capacity
    public int? SeatCount { get; set; }
    public int? PassengerCapacity { get; set; }
    public int? MaxRiderWeight { get; set; }

    // Pricing
    public decimal DailyRate { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? Rate15Min { get; set; }
    public decimal? Rate30Min { get; set; }
    public decimal? Rate1Hour { get; set; }
    public decimal DepositAmount { get; set; }
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;

    // Driver/Guide fees (Boat/Van)
    public decimal? DriverDailyFee { get; set; }
    public decimal? GuideDailyFee { get; set; }

    public string? ImageStoreId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public override int GetId() => this.FleetModelId;
    public override void SetId(int value) => this.FleetModelId = value;

    [JsonIgnore]
    public string DisplayName => $"{this.Brand} {this.Model} {this.Year}".Trim();

    [JsonIgnore]
    public string EngineDisplay => this.VehicleType switch
    {
        VehicleType.Motorbike when this.EngineCC.HasValue => $"{this.EngineCC}cc",
        VehicleType.Car when this.EngineLiters.HasValue => $"{this.EngineLiters:F1}L",
        _ => string.Empty
    };

    [JsonIgnore]
    public string DisplayNameWithEngine
    {
        get
        {
            var engine = this.EngineDisplay;
            return string.IsNullOrEmpty(engine)
                ? this.DisplayName
                : $"{this.Brand} {this.Model} {engine} {this.Year}".Trim();
        }
    }

    public string GetGroupKey()
    {
        var engine = this.VehicleType switch
        {
            VehicleType.Motorbike => this.EngineCC?.ToString() ?? "0",
            VehicleType.Car => this.EngineLiters?.ToString("F1") ?? "0",
            _ => "0"
        };
        return $"{this.Brand}|{this.Model}|{this.Year}|{this.VehicleType}|{engine}";
    }
}
