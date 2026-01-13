using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

public partial class Renter : Entity
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
}