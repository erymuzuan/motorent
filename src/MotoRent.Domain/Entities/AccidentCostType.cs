namespace MotoRent.Domain.Entities;

/// <summary>
/// Type of cost associated with an accident.
/// </summary>
public enum AccidentCostType
{
    /// <summary>
    /// Repair costs for company vehicle.
    /// </summary>
    VehicleRepair,

    /// <summary>
    /// Medical bills and treatment costs.
    /// </summary>
    MedicalTreatment,

    /// <summary>
    /// Compensation paid to third parties.
    /// </summary>
    ThirdPartyCompensation,

    /// <summary>
    /// Attorney and legal costs.
    /// </summary>
    LegalFees,

    /// <summary>
    /// Revenue lost during repair period.
    /// </summary>
    LostRentalRevenue,

    /// <summary>
    /// Insurance deductible payment.
    /// </summary>
    InsuranceDeductible,

    /// <summary>
    /// Insurance payment received (credit).
    /// </summary>
    InsurancePayout,

    /// <summary>
    /// Settlement amount paid.
    /// </summary>
    SettlementPayment,

    /// <summary>
    /// Towing and storage fees.
    /// </summary>
    TowingStorage,

    /// <summary>
    /// Replacement vehicle for renter.
    /// </summary>
    RentalCarReplacement,

    /// <summary>
    /// Processing and administrative costs.
    /// </summary>
    AdministrativeFees
}
