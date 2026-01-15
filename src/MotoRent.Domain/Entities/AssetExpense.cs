using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Tracks expenses associated with an asset.
/// Categories: Maintenance, Insurance, Financing, Accident, Registration, Consumables.
/// </summary>
public class AssetExpense : Entity
{
    public int AssetExpenseId { get; set; }

    /// <summary>
    /// The asset this expense belongs to.
    /// </summary>
    public int AssetId { get; set; }

    #region Expense Details

    /// <summary>
    /// Category of expense.
    /// </summary>
    public AssetExpenseCategory Category { get; set; }

    /// <summary>
    /// Sub-category for more detailed tracking.
    /// </summary>
    public string? SubCategory { get; set; }

    /// <summary>
    /// Description of the expense.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expense amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date the expense was incurred.
    /// </summary>
    public DateTimeOffset ExpenseDate { get; set; }

    /// <summary>
    /// Invoice or receipt reference number.
    /// </summary>
    public string? ReferenceNo { get; set; }

    /// <summary>
    /// Vendor or payee name.
    /// </summary>
    public string? VendorName { get; set; }

    #endregion

    #region Related Entities

    /// <summary>
    /// If this expense relates to a rental.
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// If this expense relates to an accident.
    /// </summary>
    public int? AccidentId { get; set; }

    /// <summary>
    /// If this expense relates to a maintenance schedule.
    /// </summary>
    public int? MaintenanceScheduleId { get; set; }

    /// <summary>
    /// If this expense relates to a loan payment.
    /// </summary>
    public int? AssetLoanPaymentId { get; set; }

    #endregion

    #region Payment Status

    /// <summary>
    /// Whether this expense has been paid.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Date the expense was paid.
    /// </summary>
    public DateTimeOffset? PaidDate { get; set; }

    /// <summary>
    /// Payment method used.
    /// </summary>
    public string? PaymentMethod { get; set; }

    #endregion

    #region Tax and Accounting

    /// <summary>
    /// Whether this expense is tax-deductible.
    /// </summary>
    public bool IsTaxDeductible { get; set; } = true;

    /// <summary>
    /// Tax amount if applicable.
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Accounting period (YYYY-MM format).
    /// </summary>
    public string? AccountingPeriod { get; set; }

    /// <summary>
    /// Notes about this expense.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Denormalized

    /// <summary>
    /// Vehicle name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleName { get; set; }

    #endregion

    public override int GetId() => this.AssetExpenseId;
    public override void SetId(int value) => this.AssetExpenseId = value;
}
