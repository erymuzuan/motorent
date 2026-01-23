using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a rentable vehicle in the fleet.
/// Supports multiple vehicle types: Motorbike, Car, JetSki, Boat, Van.
/// </summary>
public partial class Vehicle : Entity
{
    public int VehicleId { get; set; }

    // Pool and Location

    /// <summary>
    /// The vehicle's "home" shop - where it was originally registered.
    /// For non-pooled vehicles, this is the only shop where it can be rented.
    /// </summary>
    public int HomeShopId { get; set; }

    /// <summary>
    /// If set, this vehicle participates in a pool and can be rented/returned
    /// at any shop in the pool.
    /// </summary>
    public int? VehiclePoolId { get; set; }

    /// <summary>
    /// Current physical location of the vehicle (which shop it's at).
    /// Updated when vehicle is returned to a different shop.
    /// For non-pooled vehicles, this always equals HomeShopId.
    /// </summary>
    public int CurrentShopId { get; set; }

    // Vehicle Type and Duration

    /// <summary>
    /// Type of vehicle (determines which properties are applicable).
    /// </summary>
    public VehicleType VehicleType { get; set; } = VehicleType.Motorbike;

    /// <summary>
    /// How rental duration and pricing is calculated for this vehicle.
    /// </summary>
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;

    // Common Properties (All Vehicle Types)

    /// <summary>
    /// License plate or registration number.
    /// </summary>
    public string LicensePlate { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle brand/manufacturer (Honda, Yamaha, Toyota, Kawasaki, etc.).
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle model name (Click, PCX, City, Fortuner, etc.).
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle color.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Year of manufacture.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Current status of the vehicle.
    /// </summary>
    public string Status { get; set; } = VehicleStatus.Available;

    /// <summary>
    /// Path or URL to vehicle image.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Additional notes about the vehicle.
    /// </summary>
    public string? Notes { get; set; }

    // Pricing - Daily

    /// <summary>
    /// Daily rental rate (for DurationType == Daily).
    /// </summary>
    public decimal DailyRate { get; set; }

    /// <summary>
    /// Required deposit amount.
    /// </summary>
    public decimal DepositAmount { get; set; }

    // Pricing - Interval (JetSki)

    /// <summary>
    /// 15-minute interval rate (jet ski only).
    /// </summary>
    public decimal? Rate15Min { get; set; }

    /// <summary>
    /// 30-minute interval rate (jet ski only).
    /// </summary>
    public decimal? Rate30Min { get; set; }

    /// <summary>
    /// 1-hour interval rate (jet ski only).
    /// </summary>
    public decimal? Rate1Hour { get; set; }

    // Motorbike-Specific Properties

    /// <summary>
    /// Engine displacement in CC (motorbikes only: 110, 125, 150, etc.).
    /// Nullable - only applicable for VehicleType == Motorbike.
    /// </summary>
    public int? EngineCC { get; set; }

    /// <summary>
    /// Current mileage/odometer reading in km (motorbikes and cars).
    /// </summary>
    public int? Mileage { get; set; }

    /// <summary>
    /// Last service date (motorbikes and cars).
    /// </summary>
    public DateTimeOffset? LastServiceDate { get; set; }

    // Car-Specific Properties

    /// <summary>
    /// Car segment classification (cars only).
    /// Nullable - only applicable for VehicleType == Car.
    /// </summary>
    public CarSegment? Segment { get; set; }

    /// <summary>
    /// Number of seats (cars and vans).
    /// </summary>
    public int? SeatCount { get; set; }

    /// <summary>
    /// Transmission type: "Automatic" or "Manual" (cars only).
    /// </summary>
    public string? Transmission { get; set; }

    /// <summary>
    /// Engine size in liters (cars only, e.g., 1.5, 2.0).
    /// </summary>
    public decimal? EngineLiters { get; set; }

    // Boat/Van Driver and Guide

    /// <summary>
    /// Daily fee for a driver (boats and vans).
    /// </summary>
    public decimal? DriverDailyFee { get; set; }

    /// <summary>
    /// Daily fee for a tour guide (boats only).
    /// </summary>
    public decimal? GuideDailyFee { get; set; }

    /// <summary>
    /// Passenger capacity (boats and vans).
    /// </summary>
    public int? PassengerCapacity { get; set; }

    // JetSki/Boat-Specific Properties

    /// <summary>
    /// Maximum rider weight in kg (jet skis and boats).
    /// </summary>
    public int? MaxRiderWeight { get; set; }

    /// <summary>
    /// Engine hours (jet skis and boats - used for maintenance tracking).
    /// </summary>
    public int? EngineHours { get; set; }

    // Third-Party Owner

    /// <summary>
    /// If this vehicle is owned by a third party, the owner's ID.
    /// Null for company-owned vehicles.
    /// </summary>
    public int? VehicleOwnerId { get; set; }

    /// <summary>
    /// How the owner is compensated (DailyRate or RevenueShare).
    /// Only applicable when VehicleOwnerId is set.
    /// </summary>
    public OwnerPaymentModel? OwnerPaymentModel { get; set; }

    /// <summary>
    /// For DailyRate model: fixed amount paid per rental day (e.g., 200 THB).
    /// </summary>
    public decimal? OwnerDailyRate { get; set; }

    /// <summary>
    /// For RevenueShare model: percentage of GROSS rental amount (e.g., 0.30 for 30%).
    /// Applied only to rental rate, not insurance/accessories/damage/etc.
    /// </summary>
    public decimal? OwnerRevenueSharePercent { get; set; }

    /// <summary>
    /// Denormalized owner name for display purposes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleOwnerName { get; set; }

    /// <summary>
    /// Checks if this vehicle is owned by a third party.
    /// Uses C# pattern matching: is { VehicleOwnerId: > 0 }
    /// </summary>
    [JsonIgnore]
    public bool IsThirdPartyOwned => this is { VehicleOwnerId: > 0 };

    // Denormalized Fields (for display)

    /// <summary>
    /// Pool name for display purposes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehiclePoolName { get; set; }

    /// <summary>
    /// Current shop name for display purposes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CurrentShopName { get; set; }

    public override int GetId() => this.VehicleId;
    public override void SetId(int value) => this.VehicleId = value;

    // Helper Properties

    /// <summary>
    /// Gets the display name for the vehicle (Brand + Model).
    /// </summary>
    [JsonIgnore]
    public string DisplayName => $"{this.Brand} {this.Model}".Trim();

    /// <summary>
    /// Checks if this vehicle type supports mileage tracking.
    /// </summary>
    [JsonIgnore]
    public bool SupportsMileageTracking => this.VehicleType is VehicleType.Motorbike or VehicleType.Car;

    /// <summary>
    /// Checks if this vehicle type supports driver/guide options.
    /// </summary>
    [JsonIgnore]
    public bool SupportsDriverGuide => this.VehicleType is VehicleType.Boat or VehicleType.Van;

    /// <summary>
    /// Checks if this vehicle has driver option available.
    /// </summary>
    [JsonIgnore]
    public bool HasDriverOption => this.VehicleType is VehicleType.Car or VehicleType.Van or VehicleType.Boat;

    /// <summary>
    /// Checks if this vehicle has guide option available.
    /// </summary>
    [JsonIgnore]
    public bool HasGuideOption => this.VehicleType == VehicleType.Boat;

    /// <summary>
    /// Checks if this vehicle uses interval pricing.
    /// </summary>
    [JsonIgnore]
    public bool UsesIntervalPricing => this.VehicleType == VehicleType.JetSki;

    /// <summary>
    /// Whether this vehicle is part of a shared pool.
    /// </summary>
    [JsonIgnore]
    public bool IsPooled => this.VehiclePoolId.HasValue && this.VehiclePoolId > 0;

    /// <summary>
    /// Gets the group key for matching vehicles across shops.
    /// Format: "Brand|Model|Year|VehicleType|EngineCC" (e.g., "Honda|Click|2024|Motorbike|125")
    /// Used for cross-shop booking fulfillment.
    /// </summary>
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

    /// <summary>
    /// Gets a display-friendly vehicle name with engine size.
    /// </summary>
    [JsonIgnore]
    public string DisplayNameWithEngine
    {
        get
        {
            var engine = this.VehicleType switch
            {
                VehicleType.Motorbike when this.EngineCC.HasValue => $"{this.EngineCC}cc",
                VehicleType.Car when this.EngineLiters.HasValue => $"{this.EngineLiters:F1}L",
                _ => ""
            };
            return string.IsNullOrEmpty(engine)
                ? this.DisplayName
                : $"{this.DisplayName} {engine}";
        }
    }
}
