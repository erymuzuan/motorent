namespace MotoRent.Domain.Entities;

/// <summary>
/// Type of document attached to an accident.
/// </summary>
public enum AccidentDocumentType
{
    /// <summary>
    /// Accident scene photos.
    /// </summary>
    Photo,

    /// <summary>
    /// Video evidence.
    /// </summary>
    Video,

    /// <summary>
    /// Official police report.
    /// </summary>
    PoliceReport,

    /// <summary>
    /// Medical records or bills.
    /// </summary>
    MedicalRecord,

    /// <summary>
    /// Insurance claim form.
    /// </summary>
    InsuranceClaim,

    /// <summary>
    /// Repair cost estimate.
    /// </summary>
    RepairQuote,

    /// <summary>
    /// Repair or service invoice.
    /// </summary>
    Invoice,

    /// <summary>
    /// Payment receipt.
    /// </summary>
    Receipt,

    /// <summary>
    /// Court documents or legal notices.
    /// </summary>
    LegalDocument,

    /// <summary>
    /// Settlement agreement.
    /// </summary>
    SettlementAgreement,

    /// <summary>
    /// Written witness statement.
    /// </summary>
    WitnessStatement,

    /// <summary>
    /// Accident diagram or sketch.
    /// </summary>
    DiagramSketch
}
