using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

public class Rental : Entity
{
    public int RentalId { get; set; }

    #region Shop and Location

    private int m_rentedFromShopId;

    /// <summary>
    /// Shop where the rental was initiated (check-in location).
    /// </summary>
    public int RentedFromShopId
    {
        get => m_rentedFromShopId;
        set => m_rentedFromShopId = value;
    }

    /// <summary>
    /// Backward compatibility - reads ShopId from old JSON data.
    /// Only used for deserialization, not serialized.
    /// </summary>
    [JsonPropertyName("ShopId")]
    [JsonInclude]
    private int ShopIdFromJson
    {
        set => m_rentedFromShopId = value;
    }

    /// <summary>
    /// Shop where the vehicle was returned (check-out location).
    /// Null while rental is active.
    /// </summary>
    public int? ReturnedToShopId { get; set; }

    /// <summary>
    /// If this rental was for a pooled vehicle, tracks the pool.
    /// Used for reporting on cross-shop rentals.
    /// </summary>
    public int? VehiclePoolId { get; set; }

    /// <summary>
    /// Backward compatibility - maps to RentedFromShopId.
    /// </summary>
    [JsonIgnore]
    [Obsolete("Use RentedFromShopId instead")]
    public int ShopId
    {
        get => RentedFromShopId;
        set => RentedFromShopId = value;
    }

    #endregion

    #region Renter and Vehicle

    public int RenterId { get; set; }

    private int m_vehicleId;

    /// <summary>
    /// The vehicle being rented.
    /// </summary>
    public int VehicleId
    {
        get => m_vehicleId;
        set => m_vehicleId = value;
    }

    /// <summary>
    /// Backward compatibility - reads MotorbikeId from old JSON data.
    /// Only used for deserialization, not serialized.
    /// </summary>
    [JsonPropertyName("MotorbikeId")]
    [JsonInclude]
    private int MotorbikeIdFromJson
    {
        set => m_vehicleId = value;
    }

    /// <summary>
    /// Backward compatibility - maps to VehicleId.
    /// </summary>
    [JsonIgnore]
    [Obsolete("Use VehicleId instead")]
    public int MotorbikeId
    {
        get => VehicleId;
        set => VehicleId = value;
    }

    #endregion

    #region Duration and Timing

    /// <summary>
    /// Type of rental duration (Daily or FixedInterval).
    /// </summary>
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;

    /// <summary>
    /// For FixedInterval rentals: the interval in minutes (15, 30, or 60).
    /// Null for Daily rentals.
    /// </summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Start date/time of the rental.
    /// For Daily: date portion is significant.
    /// For FixedInterval: full timestamp is significant.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Expected end date/time.
    /// For Daily: date portion is significant.
    /// For FixedInterval: calculated as StartDate + IntervalMinutes.
    /// </summary>
    public DateTimeOffset ExpectedEndDate { get; set; }

    public DateTimeOffset? ActualEndDate { get; set; }

    #endregion

    #region Mileage (for motorized vehicles)

    public int MileageStart { get; set; }
    public int? MileageEnd { get; set; }

    #endregion

    #region Pricing

    private decimal m_rentalRate;

    /// <summary>
    /// Rate used for this rental.
    /// For Daily: daily rate.
    /// For FixedInterval: interval rate (Rate15Min, Rate30Min, or Rate1Hour).
    /// </summary>
    public decimal RentalRate
    {
        get => m_rentalRate;
        set => m_rentalRate = value;
    }

    /// <summary>
    /// Backward compatibility - reads DailyRate from old JSON data.
    /// Only used for deserialization, not serialized.
    /// </summary>
    [JsonPropertyName("DailyRate")]
    [JsonInclude]
    private decimal DailyRateFromJson
    {
        set => m_rentalRate = value;
    }

    /// <summary>
    /// Backward compatibility - maps to RentalRate.
    /// </summary>
    [JsonIgnore]
    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate
    {
        get => RentalRate;
        set => RentalRate = value;
    }

    public decimal TotalAmount { get; set; }

    #endregion

    #region Driver/Guide Add-ons

    /// <summary>
    /// Whether a driver is included in the rental.
    /// </summary>
    public bool IncludeDriver { get; set; }

    /// <summary>
    /// Whether a guide is included in the rental.
    /// </summary>
    public bool IncludeGuide { get; set; }

    /// <summary>
    /// Driver fee charged for this rental (captured at rental time).
    /// </summary>
    public decimal DriverFee { get; set; }

    /// <summary>
    /// Guide fee charged for this rental (captured at rental time).
    /// </summary>
    public decimal GuideFee { get; set; }

    #endregion

    #region Status and Notes

    public string Status { get; set; } = "Reserved";  // Reserved, Active, Completed, Cancelled
    public string? Notes { get; set; }

    #endregion

    #region Denormalized for display

    public string? RenterName { get; set; }

    /// <summary>
    /// Backward compatibility - maps to VehicleName.
    /// </summary>
    [JsonIgnore]
    [Obsolete("Use VehicleName instead")]
    public string? MotorbikeName
    {
        get => VehicleName;
        set => VehicleName = value;
    }

    /// <summary>
    /// Vehicle display name for UI.
    /// </summary>
    public string? VehicleName { get; set; }

    /// <summary>
    /// Name of the shop where rental was initiated.
    /// </summary>
    public string? RentedFromShopName { get; set; }

    /// <summary>
    /// Name of the shop where vehicle was returned.
    /// </summary>
    public string? ReturnedToShopName { get; set; }

    #endregion

    #region Related Entities

    public int? InsuranceId { get; set; }
    public int? DepositId { get; set; }

    #endregion

    #region Pick-up/Drop-off Locations

    /// <summary>
    /// Where the vehicle will be picked up. Null = at shop.
    /// </summary>
    public int? PickupLocationId { get; set; }

    /// <summary>
    /// Where the vehicle will be dropped off. Null = at shop.
    /// </summary>
    public int? DropoffLocationId { get; set; }

    /// <summary>
    /// Denormalized pickup location name for display.
    /// </summary>
    public string? PickupLocationName { get; set; }

    /// <summary>
    /// Denormalized drop-off location name for display.
    /// </summary>
    public string? DropoffLocationName { get; set; }

    /// <summary>
    /// Scheduled pickup time (for out-of-hours tracking).
    /// </summary>
    public TimeSpan? ScheduledPickupTime { get; set; }

    /// <summary>
    /// Scheduled drop-off time (for out-of-hours tracking).
    /// </summary>
    public TimeSpan? ScheduledDropoffTime { get; set; }

    #endregion

    #region Location and Out-of-Hours Fees

    /// <summary>
    /// Fee captured for pickup location at rental time.
    /// </summary>
    public decimal PickupLocationFee { get; set; }

    /// <summary>
    /// Fee captured for drop-off location at rental time.
    /// </summary>
    public decimal DropoffLocationFee { get; set; }

    /// <summary>
    /// Fee captured for out-of-hours pickup.
    /// </summary>
    public decimal OutOfHoursPickupFee { get; set; }

    /// <summary>
    /// Fee captured for out-of-hours drop-off.
    /// </summary>
    public decimal OutOfHoursDropoffFee { get; set; }

    /// <summary>
    /// Whether pickup was scheduled outside operating hours.
    /// </summary>
    public bool IsOutOfHoursPickup { get; set; }

    /// <summary>
    /// Whether drop-off was scheduled outside operating hours.
    /// </summary>
    public bool IsOutOfHoursDropoff { get; set; }

    /// <summary>
    /// Name of the out-of-hours band for pickup (e.g., "Late Evening").
    /// </summary>
    public string? OutOfHoursPickupBand { get; set; }

    /// <summary>
    /// Name of the out-of-hours band for drop-off (e.g., "Night").
    /// </summary>
    public string? OutOfHoursDropoffBand { get; set; }

    #endregion

    public override int GetId() => this.RentalId;
    public override void SetId(int value) => this.RentalId = value;

    #region Calculated Properties

    /// <summary>
    /// Number of rental days (for Daily duration type).
    /// </summary>
    [JsonIgnore]
    public int RentalDays => DurationType == RentalDurationType.Daily
        ? Math.Max(1, (int)(ExpectedEndDate.Date - StartDate.Date).TotalDays + 1)
        : 0;

    /// <summary>
    /// Human-readable duration display.
    /// </summary>
    [JsonIgnore]
    public string DurationDisplay => DurationType == RentalDurationType.Daily
        ? $"{RentalDays} day{(RentalDays > 1 ? "s" : "")}"
        : $"{IntervalMinutes} minutes";

    /// <summary>
    /// Whether this was a cross-shop return.
    /// </summary>
    [JsonIgnore]
    public bool IsCrossShopReturn => ReturnedToShopId.HasValue && ReturnedToShopId != RentedFromShopId;

    #endregion
}
