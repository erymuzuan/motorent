namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a party (person or organization) involved in an accident.
/// </summary>
public class AccidentParty : Entity
{
    public int AccidentPartyId { get; set; }

    /// <summary>
    /// The accident this party is involved in.
    /// </summary>
    public int AccidentId { get; set; }

    /// <summary>
    /// Type of party.
    /// </summary>
    public AccidentPartyType PartyType { get; set; }

    #region Contact Information

    /// <summary>
    /// Full name of person or organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Physical address.
    /// </summary>
    public string? Address { get; set; }

    #endregion

    #region Identification

    /// <summary>
    /// ID number (passport, national ID, license).
    /// </summary>
    public string? IdNumber { get; set; }

    /// <summary>
    /// Company/organization name if applicable.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Badge number for police officers.
    /// </summary>
    public string? BadgeNumber { get; set; }

    /// <summary>
    /// License plate of third party vehicle if applicable.
    /// </summary>
    public string? VehicleLicensePlate { get; set; }

    /// <summary>
    /// Insurance policy number.
    /// </summary>
    public string? InsurancePolicyNo { get; set; }

    #endregion

    #region Involvement Details

    /// <summary>
    /// Role or involvement description.
    /// </summary>
    public string? InvolvementDescription { get; set; }

    /// <summary>
    /// Whether this party was injured.
    /// </summary>
    public bool IsInjured { get; set; }

    /// <summary>
    /// Injury description if injured.
    /// </summary>
    public string? InjuryDescription { get; set; }

    /// <summary>
    /// Whether this party is at fault.
    /// </summary>
    public bool? IsAtFault { get; set; }

    /// <summary>
    /// Fault percentage (0-100).
    /// </summary>
    public int? FaultPercentage { get; set; }

    #endregion

    #region Additional Info

    /// <summary>
    /// Notes about this party.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Link to existing Renter if party is a known renter.
    /// </summary>
    public int? RenterId { get; set; }

    #endregion

    public override int GetId() => this.AccidentPartyId;
    public override void SetId(int value) => this.AccidentPartyId = value;
}
