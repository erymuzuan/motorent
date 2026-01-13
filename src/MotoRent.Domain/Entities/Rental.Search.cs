using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

public partial class Rental : ISearchable
{
    int ISearchable.Id => RentalId;
    string ISearchable.Title => $"{RenterName} - {VehicleName}";
    string ISearchable.Status => Status;
    string ISearchable.Text => string.Join(" ", RenterName, VehicleName,
        RentedFromShopName, PickupLocationName, DropoffLocationName, Notes);
    string ISearchable.Summary => $"{VehicleName} rented by {RenterName} ({Status})";
    string ISearchable.Type => "Rental";
    bool ISearchable.IsSearchResult { get; set; }

    static bool ISearchable.HasDate => true;
    DateOnly? ISearchable.Date => DateOnly.FromDateTime(StartDate.DateTime);
    bool ISearchable.SplitYear => true;

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["RenterId"] = RenterId,
        ["VehicleId"] = VehicleId,
        ["ShopId"] = RentedFromShopId,
        ["StartDate"] = StartDate.ToString("yyyy-MM-dd"),
        ["EndDate"] = ExpectedEndDate.ToString("yyyy-MM-dd")
    };
}
