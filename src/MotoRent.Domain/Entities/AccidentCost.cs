namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a cost item associated with an accident.
/// Tracks estimated vs actual costs for financial forecasting.
/// </summary>
public class AccidentCost : Entity
{
    public int AccidentCostId { get; set; }

    /// <summary>
    /// The accident this cost belongs to.
    /// </summary>
    public int AccidentId { get; set; }

    /// <summary>
    /// Type of cost.
    /// </summary>
    public AccidentCostType CostType { get; set; }

    #region Cost Information

    /// <summary>
    /// Description of the cost.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estimated cost amount.
    /// </summary>
    public decimal EstimatedAmount { get; set; }

    /// <summary>
    /// Actual cost incurred (null until paid).
    /// </summary>
    public decimal? ActualAmount { get; set; }

    /// <summary>
    /// Whether this is a credit (insurance payout) rather than debit.
    /// </summary>
    public bool IsCredit { get; set; }

    /// <summary>
    /// Date when cost was incurred/paid.
    /// </summary>
    public DateTimeOffset? PaidDate { get; set; }

    /// <summary>
    /// Reference number (invoice, receipt, etc.).
    /// </summary>
    public string? ReferenceNo { get; set; }

    #endregion

    #region Vendor/Payee

    /// <summary>
    /// Vendor or payee name.
    /// </summary>
    public string? VendorName { get; set; }

    /// <summary>
    /// Related party ID if cost relates to a specific party.
    /// </summary>
    public int? AccidentPartyId { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Whether this cost has been approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Who approved this cost.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Notes about this cost.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    public override int GetId() => this.AccidentCostId;
    public override void SetId(int value) => this.AccidentCostId = value;
}
