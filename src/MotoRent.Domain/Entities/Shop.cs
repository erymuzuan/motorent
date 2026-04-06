using MotoRent.Domain.Search;
using MotoRent.Domain.Spatial;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a shop/outlet location within a tenant's organization.
/// Shop data is stored in the tenant's schema (e.g., [AccountNo].[Shop]),
/// so tenant isolation is provided by the schema itself - no AccountNo property needed.
/// A tenant (Organization) can have one or more Shops.
/// </summary>
public partial class Shop : Entity
{
    public int ShopId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// State or federal territory where the shop is located.
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Specific area or neighborhood within the state or federal territory.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// District where the shop is located.
    /// </summary>
    public string District { get; set; } = string.Empty;

    /// <summary>
    /// Subdistrict or locality where the shop is located.
    /// </summary>
    public string Subdistrict { get; set; } = string.Empty;

    /// <summary>
    /// Postal code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    /// <summary>
    /// Rental terms and conditions in English.
    /// </summary>
    public string? TermsAndConditionsEn { get; set; }

    /// <summary>
    /// Rental terms and conditions in Thai.
    /// </summary>
    public string? TermsAndConditionsTh { get; set; }

    /// <summary>
    /// Rental terms and conditions in Bahasa Melayu.
    /// </summary>
    public string? TermsAndConditionsMs { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// GPS coordinates of the shop location for map display and directions.
    /// </summary>
    public LatLng? GpsLocation { get; set; }

    /// <summary>
    /// Default weekly hours template for auto-populating ShopSchedule entries.
    /// Index by DayOfWeek (0 = Sunday, 6 = Saturday).
    /// </summary>
    public List<DailyHoursTemplate> DefaultHours { get; set; } = [];

    /// <summary>
    /// Out-of-hours fee bands for pickup/dropoff outside operating hours.
    /// Each band defines a time range and associated fee.
    /// </summary>
    public List<OutOfHoursBand> OutOfHoursBands { get; set; } = [];

    public override int GetId() => this.ShopId;
    public override void SetId(int value) => this.ShopId = value;
}
