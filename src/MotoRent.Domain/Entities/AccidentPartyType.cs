namespace MotoRent.Domain.Entities;

/// <summary>
/// Type of party involved in an accident.
/// </summary>
public enum AccidentPartyType
{
    /// <summary>
    /// Customer renting the vehicle.
    /// </summary>
    Renter,

    /// <summary>
    /// Company employee.
    /// </summary>
    Staff,

    /// <summary>
    /// Driver of another vehicle involved.
    /// </summary>
    ThirdPartyDriver,

    /// <summary>
    /// Pedestrian involved in the accident.
    /// </summary>
    ThirdPartyPedestrian,

    /// <summary>
    /// Passenger in another vehicle.
    /// </summary>
    ThirdPartyPassenger,

    /// <summary>
    /// Witness to the accident.
    /// </summary>
    Witness,

    /// <summary>
    /// Insurance company handling the claim.
    /// </summary>
    InsuranceCompany,

    /// <summary>
    /// Police officer handling the case.
    /// </summary>
    PoliceOfficer,

    /// <summary>
    /// Hospital or medical facility.
    /// </summary>
    Hospital,

    /// <summary>
    /// Lawyer or legal counsel.
    /// </summary>
    LegalRepresentative
}
