using MotoRent.Domain.Search;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents an accident/incident involving a company vehicle.
/// Can be linked to a rental (optional) or be a standalone incident.
/// </summary>
public partial class Accident : Entity
{
    public int AccidentId { get; set; }

    /// <summary>
    /// The vehicle involved in the accident.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Optional: The rental associated with this accident.
    /// Null if accident occurred during staff use or while parked.
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Unique accident reference number for external communication.
    /// Format: ACC-{YYYYMMDD}-{Sequence}
    /// </summary>
    public string ReferenceNo { get; set; } = string.Empty;

    /// <summary>
    /// Brief title/summary of the accident.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what happened.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the accident occurred.
    /// </summary>
    public DateTimeOffset AccidentDate { get; set; }

    /// <summary>
    /// When the accident was reported.
    /// </summary>
    public DateTimeOffset ReportedDate { get; set; }

    /// <summary>
    /// Location/address where the accident occurred.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// GPS coordinates if available (lat,lng format).
    /// </summary>
    public string? GpsCoordinates { get; set; }

    /// <summary>
    /// Severity level of the accident.
    /// </summary>
    public AccidentSeverity Severity { get; set; } = AccidentSeverity.Minor;

    /// <summary>
    /// Current status of the accident case.
    /// </summary>
    public AccidentStatus Status { get; set; } = AccidentStatus.Reported;

    /// <summary>
    /// Whether police are involved.
    /// </summary>
    public bool PoliceInvolved { get; set; }

    /// <summary>
    /// Police case/report number if applicable.
    /// </summary>
    public string? PoliceCaseNumber { get; set; }

    /// <summary>
    /// Whether insurance claim has been filed.
    /// </summary>
    public bool InsuranceClaimFiled { get; set; }

    /// <summary>
    /// Insurance claim reference number.
    /// </summary>
    public string? InsuranceClaimNumber { get; set; }

    /// <summary>
    /// Total estimated costs across all cost items.
    /// </summary>
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>
    /// Total actual costs incurred.
    /// </summary>
    public decimal TotalActualCost { get; set; }

    /// <summary>
    /// Reserve amount set aside for this accident.
    /// </summary>
    public decimal ReserveAmount { get; set; }

    /// <summary>
    /// Total insurance payout received.
    /// </summary>
    public decimal InsurancePayoutReceived { get; set; }

    /// <summary>
    /// Net financial impact (ActualCost - InsurancePayout).
    /// </summary>
    public decimal NetCost { get; set; }

    /// <summary>
    /// Date when accident was fully resolved.
    /// </summary>
    public DateTimeOffset? ResolvedDate { get; set; }

    /// <summary>
    /// Resolution notes/summary.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Who resolved/closed this accident.
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Vehicle display name for UI.
    /// </summary>
    public string? VehicleName { get; set; }

    /// <summary>
    /// Vehicle license plate for quick reference.
    /// </summary>
    public string? VehicleLicensePlate { get; set; }

    /// <summary>
    /// Renter name if linked to a rental.
    /// </summary>
    public string? RenterName { get; set; }

    public override int GetId() => this.AccidentId;
    public override void SetId(int value) => this.AccidentId = value;
}