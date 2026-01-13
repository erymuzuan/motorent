using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

/// <summary>
/// ISearchable implementation for Shop entity.
/// </summary>
public partial class Shop : ISearchable
{
    int ISearchable.Id => this.ShopId;
    string ISearchable.Title => this.Name;
    string ISearchable.Status => this.IsActive ? "Active" : "Inactive";
    string ISearchable.Text => string.Join(" ", this.Name, this.Location, this.Address, this.Phone, this.Email);
    string ISearchable.Summary => $"{this.Name} - {this.Location}";
    string ISearchable.Type => "Shop";
    bool ISearchable.IsSearchResult { get; set; }

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["Location"] = this.Location ?? "",
        ["Phone"] = this.Phone ?? ""
    };
}
