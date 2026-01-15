using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

/// <summary>
/// ISearchable implementation for Renter entity.
/// </summary>
public partial class Renter : ISearchable
{
    int ISearchable.Id => this.RenterId;
    string ISearchable.Title => this.FullName;
    string ISearchable.Status => "Active";
    string ISearchable.Text => string.Join(" ", this.FullName, this.Phone, this.Email,
        this.PassportNo, this.NationalIdNo, this.DrivingLicenseNo, this.HotelName, this.Nationality);
    string ISearchable.Summary => $"{this.FullName} - {this.Phone ?? this.Email}";
    string ISearchable.Type => "Renter";
    bool ISearchable.IsSearchResult { get; set; }

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["Phone"] = this.Phone ?? "",
        ["Email"] = this.Email ?? "",
        ["Nationality"] = this.Nationality ?? "",
        ["HotelName"] = this.HotelName ?? ""
    };
}
