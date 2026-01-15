using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Tracks loan/financing details for an asset.
/// Supports full amortization tracking with interest calculations.
/// </summary>
public class AssetLoan : Entity
{
    public int AssetLoanId { get; set; }

    /// <summary>
    /// The asset this loan is for.
    /// </summary>
    public int AssetId { get; set; }

    #region Loan Details

    /// <summary>
    /// Lender/bank name.
    /// </summary>
    public string LenderName { get; set; } = string.Empty;

    /// <summary>
    /// Loan account or contract number.
    /// </summary>
    public string? LoanAccountNo { get; set; }

    /// <summary>
    /// Original principal amount borrowed.
    /// </summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>
    /// Annual interest rate (e.g., 0.08 for 8%).
    /// </summary>
    public decimal AnnualInterestRate { get; set; }

    /// <summary>
    /// Loan term in months.
    /// </summary>
    public int TermMonths { get; set; }

    /// <summary>
    /// Monthly payment amount.
    /// </summary>
    public decimal MonthlyPayment { get; set; }

    /// <summary>
    /// Down payment made at purchase.
    /// </summary>
    public decimal DownPayment { get; set; }

    /// <summary>
    /// Loan start date.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Expected end date.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    #endregion

    #region Current Balances

    /// <summary>
    /// Remaining principal balance.
    /// </summary>
    public decimal RemainingPrincipal { get; set; }

    /// <summary>
    /// Total interest paid to date.
    /// </summary>
    public decimal TotalInterestPaid { get; set; }

    /// <summary>
    /// Total principal paid to date.
    /// </summary>
    public decimal TotalPrincipalPaid { get; set; }

    /// <summary>
    /// Number of payments made.
    /// </summary>
    public int PaymentsMade { get; set; }

    /// <summary>
    /// Next payment due date.
    /// </summary>
    public DateTimeOffset? NextPaymentDue { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Loan status: Active, PaidOff, Defaulted.
    /// </summary>
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    /// <summary>
    /// Date loan was paid off (if applicable).
    /// </summary>
    public DateTimeOffset? PaidOffDate { get; set; }

    /// <summary>
    /// Notes about this loan.
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

    public override int GetId() => this.AssetLoanId;
    public override void SetId(int value) => this.AssetLoanId = value;

    #region Calculated Properties

    /// <summary>
    /// Remaining payments.
    /// </summary>
    [JsonIgnore]
    public int RemainingPayments => Math.Max(0, this.TermMonths - this.PaymentsMade);

    /// <summary>
    /// Total cost of loan (Principal + Total Interest).
    /// </summary>
    [JsonIgnore]
    public decimal TotalLoanCost => this.PrincipalAmount + this.TotalInterestPaid +
        CalculateRemainingInterest();

    /// <summary>
    /// Monthly interest rate.
    /// </summary>
    [JsonIgnore]
    public decimal MonthlyInterestRate => this.AnnualInterestRate / 12;

    private decimal CalculateRemainingInterest()
    {
        // Simplified estimate of remaining interest
        decimal remaining = 0;
        decimal balance = this.RemainingPrincipal;
        for (int i = 0; i < this.RemainingPayments && balance > 0; i++)
        {
            var interest = balance * this.MonthlyInterestRate;
            remaining += interest;
            balance -= (this.MonthlyPayment - interest);
        }
        return remaining;
    }

    #endregion
}
