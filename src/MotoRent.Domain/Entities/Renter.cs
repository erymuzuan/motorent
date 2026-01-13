using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

public class Renter : Entity, ISearchable
{
    public int RenterId { get; set; }
    public int ShopId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Nationality { get; set; }
    public string? PassportNo { get; set; }
    public string? NationalIdNo { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? DrivingLicenseCountry { get; set; }
    public DateTimeOffset? DrivingLicenseExpiry { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? HotelName { get; set; }
    public string? HotelAddress { get; set; }
    public string? EmergencyContact { get; set; }
    public string? ProfilePhotoPath { get; set; }

    public override int GetId() => this.RenterId;
    public override void SetId(int value) => this.RenterId = value;

    #region ISearchable Implementation

    int ISearchable.Id => RenterId;
    string ISearchable.Title => FullName;
    string ISearchable.Status => "Active";
    string ISearchable.Text => string.Join(" ", FullName, Phone, Email,
        PassportNo, NationalIdNo, DrivingLicenseNo, HotelName, Nationality);
    string ISearchable.Summary => $"{FullName} - {Phone ?? Email}";
    string ISearchable.Type => "Renter";
    bool ISearchable.IsSearchResult { get; set; }

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["Phone"] = Phone ?? "",
        ["Email"] = Email ?? "",
        ["Nationality"] = Nationality ?? "",
        ["HotelName"] = HotelName ?? "",
        ["ShopId"] = ShopId
    };

    #endregion
}
