using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Models;

/// <summary>
/// Represents a group of vehicles with the same make, model, year, type, and engine specification.
/// Used for displaying fleet inventory as model groups rather than individual units.
/// </summary>
public class VehicleGroup
{
    /// <summary>
    /// Unique key for this group: "Brand|Model|Year|VehicleType|Engine".
    /// Engine is EngineCC for motorbikes/jetskis or EngineLiters for cars.
    /// </summary>
    public string GroupKey { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle brand/manufacturer (Honda, Yamaha, Toyota, etc.).
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle model name (Click, PCX, Civic, etc.).
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Year of manufacture (vehicles grouped by year).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Type of vehicle in this group.
    /// </summary>
    public VehicleType VehicleType { get; set; }

    /// <summary>
    /// Engine displacement in CC (motorbikes and jet skis).
    /// </summary>
    public int? EngineCC { get; set; }

    /// <summary>
    /// Engine size in liters (cars).
    /// </summary>
    public decimal? EngineLiters { get; set; }

    /// <summary>
    /// Car segment (Sedan, SUV, etc.) - only for cars.
    /// </summary>
    public CarSegment? Segment { get; set; }

    /// <summary>
    /// Lowest daily rate in the group.
    /// </summary>
    public decimal MinDailyRate { get; set; }

    /// <summary>
    /// Highest daily rate in the group.
    /// </summary>
    public decimal MaxDailyRate { get; set; }

    /// <summary>
    /// Lowest deposit amount in the group.
    /// </summary>
    public decimal MinDepositAmount { get; set; }

    /// <summary>
    /// Path or URL to representative vehicle image.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Total number of units in this group.
    /// </summary>
    public int TotalUnits { get; set; }

    /// <summary>
    /// Number of available units.
    /// </summary>
    public int AvailableUnits { get; set; }

    /// <summary>
    /// Number of currently rented units.
    /// </summary>
    public int RentedUnits { get; set; }

    /// <summary>
    /// Number of units under maintenance.
    /// </summary>
    public int MaintenanceUnits { get; set; }

    /// <summary>
    /// Number of reserved units.
    /// </summary>
    public int ReservedUnits { get; set; }

    /// <summary>
    /// Colors available for reservation (from available units only).
    /// </summary>
    public List<string> AvailableColors { get; set; } = [];

    /// <summary>
    /// All individual vehicles in this group.
    /// </summary>
    public List<Vehicle> Vehicles { get; set; } = [];

    /// <summary>
    /// Display name for this vehicle group: "Brand Model Year".
    /// </summary>
    public string DisplayName => $"{Brand} {Model} {Year}".Trim();

    /// <summary>
    /// Whether prices vary within the group.
    /// </summary>
    public bool HasPriceRange => MinDailyRate != MaxDailyRate;

    /// <summary>
    /// Price display string: "$500-600" or "$500" if uniform.
    /// </summary>
    public string PriceDisplay => HasPriceRange
        ? $"{MinDailyRate:N0}-{MaxDailyRate:N0}"
        : $"{MinDailyRate:N0}";

    /// <summary>
    /// Engine specification display: "125cc" or "1.5L".
    /// </summary>
    public string EngineDisplay => EngineCC.HasValue
        ? $"{EngineCC}cc"
        : EngineLiters.HasValue
            ? $"{EngineLiters}L"
            : string.Empty;

    /// <summary>
    /// Creates the group key from vehicle properties.
    /// </summary>
    public static string CreateGroupKey(Vehicle vehicle)
    {
        var engine = vehicle.EngineCC?.ToString() ?? vehicle.EngineLiters?.ToString("0.0") ?? "0";
        return $"{vehicle.Brand}|{vehicle.Model}|{vehicle.Year}|{vehicle.VehicleType}|{engine}";
    }

    /// <summary>
    /// Creates a VehicleGroup from a collection of vehicles that share the same grouping key.
    /// </summary>
    public static VehicleGroup FromVehicles(IEnumerable<Vehicle> vehicles)
    {
        var vehicleList = vehicles.ToList();
        if (vehicleList.Count == 0)
            throw new ArgumentException("Cannot create VehicleGroup from empty collection", nameof(vehicles));

        var first = vehicleList[0];

        return new VehicleGroup
        {
            GroupKey = CreateGroupKey(first),
            Brand = first.Brand,
            Model = first.Model,
            Year = first.Year,
            VehicleType = first.VehicleType,
            EngineCC = first.EngineCC,
            EngineLiters = first.EngineLiters,
            Segment = first.Segment,
            TotalUnits = vehicleList.Count,
            AvailableUnits = vehicleList.Count(v => v.Status == VehicleStatus.Available),
            RentedUnits = vehicleList.Count(v => v.Status == VehicleStatus.Rented),
            MaintenanceUnits = vehicleList.Count(v => v.Status == VehicleStatus.Maintenance),
            ReservedUnits = vehicleList.Count(v => v.Status == VehicleStatus.Reserved),
            MinDailyRate = vehicleList.Min(v => v.DailyRate),
            MaxDailyRate = vehicleList.Max(v => v.DailyRate),
            MinDepositAmount = vehicleList.Min(v => v.DepositAmount),
            ImagePath = vehicleList.FirstOrDefault(v => !string.IsNullOrEmpty(v.ImagePath))?.ImagePath,
            AvailableColors = vehicleList
                .Where(v => v.Status == VehicleStatus.Available && !string.IsNullOrEmpty(v.Color))
                .Select(v => v.Color!)
                .Distinct()
                .OrderBy(c => c)
                .ToList(),
            Vehicles = vehicleList
        };
    }
}
