using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

/// <summary>
/// ISearchable implementation for Vehicle entity.
/// </summary>
public partial class Vehicle : ISearchable
{
    int ISearchable.Id => this.VehicleId;
    string ISearchable.Title => $"{this.LicensePlate} - {this.DisplayName}";
    string ISearchable.Status => this.Status.ToString();
    string ISearchable.Text => string.Join(" ", this.LicensePlate, this.Brand, this.Model,
        this.DisplayName, this.Color, this.VehiclePoolName, this.CurrentShopName, this.Notes);
    string ISearchable.Summary => $"{this.DisplayName} ({this.LicensePlate}) - {this.Status}";
    string ISearchable.Type => "Vehicle";
    bool ISearchable.IsSearchResult { get; set; }

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["VehicleType"] = this.VehicleType.ToString(),
        ["Brand"] = this.Brand ?? "",
        ["Model"] = this.Model ?? "",
        ["ShopId"] = this.CurrentShopId,
        ["VehiclePoolId"] = this.VehiclePoolId ?? 0
    };
}
