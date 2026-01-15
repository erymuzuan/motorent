namespace MotoRent.Domain.Entities;

/// <summary>
/// Individual loan payment record.
/// </summary>
public class AssetLoanPayment : Entity
{
    public int AssetLoanPaymentId { get; set; }

    /// <summary>
    /// The loan this payment belongs to.
    /// </summary>
    public int AssetLoanId { get; set; }

    /// <summary>
    /// Payment number in sequence.
    /// </summary>
    public int PaymentNumber { get; set; }

    /// <summary>
    /// Payment due date.
    /// </summary>
    public DateTimeOffset DueDate { get; set; }

    /// <summary>
    /// Actual payment date.
    /// </summary>
    public DateTimeOffset? PaidDate { get; set; }

    /// <summary>
    /// Total payment amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Principal portion of payment.
    /// </summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>
    /// Interest portion of payment.
    /// </summary>
    public decimal InterestAmount { get; set; }

    /// <summary>
    /// Balance after this payment.
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Payment status: Pending, Paid, Late, Missed.
    /// </summary>
    public LoanPaymentStatus Status { get; set; } = LoanPaymentStatus.Pending;

    /// <summary>
    /// Late fee if applicable.
    /// </summary>
    public decimal? LateFee { get; set; }

    /// <summary>
    /// Payment reference number.
    /// </summary>
    public string? PaymentRef { get; set; }

    /// <summary>
    /// Notes about this payment.
    /// </summary>
    public string? Notes { get; set; }

    public override int GetId() => this.AssetLoanPaymentId;
    public override void SetId(int value) => this.AssetLoanPaymentId = value;
}
