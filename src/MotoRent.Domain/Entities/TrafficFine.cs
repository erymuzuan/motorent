using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a traffic or parking fine issued against a company vehicle.
/// Can be linked to a rental (optional) if the fine occurred during a rental period.
/// </summary>
public class TrafficFine : Entity
{
    public int TrafficFineId { get; set; }

    /// <summary>
    /// The vehicle that received the fine.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Optional: The rental active when the fine was incurred.
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Type of fine (e.g., Speeding, Parking, RedLight, NoHelmet, Other).
    /// </summary>
    public string FineType { get; set; } = "Other";

    /// <summary>
    /// Fine amount in THB.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date when the fine was issued.
    /// </summary>
    public DateTimeOffset FineDate { get; set; }

    /// <summary>
    /// Description of the fine/violation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status: Pending, Paid, Disputed, ChargedToDeposit, Waived.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Image store IDs for evidence photos (comma-separated or JSON array).
    /// </summary>
    public string? EvidenceImageIds { get; set; }

    /// <summary>
    /// How the fine was settled: Cash, BankTransfer, DeductFromDeposit, etc.
    /// </summary>
    public string? SettlementMethod { get; set; }

    /// <summary>
    /// If charged against a deposit, the deposit ID.
    /// </summary>
    public int? DepositId { get; set; }

    /// <summary>
    /// Date when the fine was resolved/paid.
    /// </summary>
    public DateTimeOffset? ResolvedDate { get; set; }

    /// <summary>
    /// Official reference number on the fine ticket.
    /// </summary>
    public string? ReferenceNo { get; set; }

    /// <summary>
    /// Location where the violation occurred.
    /// </summary>
    public string? Location { get; set; }

    // Denormalized display fields

    /// <summary>
    /// Vehicle display name for UI.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleName { get; set; }

    /// <summary>
    /// Vehicle license plate for quick reference.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleLicensePlate { get; set; }

    /// <summary>
    /// Renter name if linked to a rental.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RenterName { get; set; }

    public override int GetId() => this.TrafficFineId;
    public override void SetId(int value) => this.TrafficFineId = value;
}
