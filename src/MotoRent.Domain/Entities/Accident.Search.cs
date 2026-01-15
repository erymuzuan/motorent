using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

/// <summary>
/// ISearchable implementation for Accident entity.
/// </summary>
public partial class Accident : ISearchable
{
    int ISearchable.Id => this.AccidentId;
    string ISearchable.Title => $"{this.ReferenceNo} - {this.Title}";
    string ISearchable.Status => this.Status.ToString();
    string ISearchable.Text => string.Join(" ", this.ReferenceNo, this.Title, this.Description,
        this.Location, this.VehicleName, this.VehicleLicensePlate, this.RenterName);
    string ISearchable.Summary => $"{this.ReferenceNo}: {this.Title} ({this.Status})";
    string ISearchable.Type => "Accident";
    bool ISearchable.IsSearchResult { get; set; }

    static bool ISearchable.HasDate => true;
    DateOnly? ISearchable.Date => DateOnly.FromDateTime(this.AccidentDate.DateTime);

    Dictionary<string, object>? ISearchable.CustomFields => new()
    {
        ["Severity"] = this.Severity.ToString(),
        ["VehicleId"] = this.VehicleId,
        ["RentalId"] = this.RentalId ?? 0,
        ["AccidentDate"] = this.AccidentDate.ToString("yyyy-MM-dd")
    };
}
