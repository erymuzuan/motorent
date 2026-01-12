namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a shop/outlet location within a tenant's organization.
/// Shop data is stored in the tenant's schema (e.g., [AccountNo].[Shop]),
/// so tenant isolation is provided by the schema itself - no AccountNo property needed.
/// A tenant (Organization) can have one or more Shops.
/// </summary>
public class Shop : Entity
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
    public string? TermsAndConditions { get; set; }
    public bool IsActive { get; set; } = true;

    #region Operating Hours

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

    #endregion

    public override int GetId() => this.ShopId;
    public override void SetId(int value) => this.ShopId = value;
}
