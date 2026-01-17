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
    /// Geographic location/area (e.g., Phuket, Krabi, Koh Samui).
    /// </summary>
    public string Location { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LogoPath { get; set; }

    #region Easy Contact (Tourist App)

    /// <summary>
    /// WhatsApp number in international format without + (e.g., 66812345678).
    /// Used to generate wa.me links for tourists.
    /// </summary>
    public string? WhatsAppNumber { get; set; }

    /// <summary>
    /// LINE ID for the shop (e.g., @adammoto or adammoto).
    /// </summary>
    public string? LineId { get; set; }

    /// <summary>
    /// Full LINE URL if different from standard format.
    /// e.g., line.me/ti/p/~adammoto or line.me/R/ti/p/@adammoto
    /// </summary>
    public string? LineUrl { get; set; }

    /// <summary>
    /// Facebook Messenger page name for m.me links.
    /// </summary>
    public string? FacebookMessenger { get; set; }

    /// <summary>
    /// Whether the shop operates 24 hours.
    /// </summary>
    public bool IsOpen24Hours { get; set; }

    #endregion
    public string? TermsAndConditions { get; set; }
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