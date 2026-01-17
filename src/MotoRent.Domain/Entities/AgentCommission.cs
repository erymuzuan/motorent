namespace MotoRent.Domain.Entities;

/// <summary>
/// Tracks commission for an agent booking.
/// Commission becomes eligible only after rental is completed.
/// Workflow: (Created) → Pending (rental completed) → Approved → Paid
/// If booking cancelled: Voided
/// </summary>
public class AgentCommission : Entity
{
    public int AgentCommissionId { get; set; }
    public int AgentId { get; set; }
    public int BookingId { get; set; }

    /// <summary>
    /// Set when rental completes - this is when commission becomes eligible.
    /// </summary>
    public int? RentalId { get; set; }

    // Calculation Details

    /// <summary>
    /// How commission was calculated (Percentage, FixedPerBooking, etc.).
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// Booking total used for calculation.
    /// </summary>
    public decimal BookingTotal { get; set; }

    /// <summary>
    /// Rate used for calculation.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Calculated commission amount.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    // Status Workflow

    /// <summary>
    /// Current status: Pending, Approved, Paid, Voided.
    /// </summary>
    public string Status { get; set; } = AgentCommissionStatus.Pending;

    /// <summary>
    /// When rental completed and commission became eligible.
    /// </summary>
    public DateTimeOffset? EligibleDate { get; set; }

    /// <summary>
    /// When commission was approved for payment.
    /// </summary>
    public DateTimeOffset? ApprovedDate { get; set; }

    /// <summary>
    /// Who approved the commission.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// When commission was paid.
    /// </summary>
    public DateTimeOffset? PaidDate { get; set; }

    /// <summary>
    /// Who processed the payment.
    /// </summary>
    public string? PaidBy { get; set; }

    /// <summary>
    /// Bank transfer reference or receipt number.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Payment method: Cash, BankTransfer, PromptPay.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// When commission was voided (if cancelled).
    /// </summary>
    public DateTimeOffset? VoidedDate { get; set; }

    /// <summary>
    /// Reason for voiding.
    /// </summary>
    public string? VoidedReason { get; set; }

    // Notes

    /// <summary>
    /// Additional notes about this commission.
    /// </summary>
    public string? Notes { get; set; }

    // Denormalized for Reporting

    /// <summary>
    /// Agent name for display.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Agent code for display.
    /// </summary>
    public string? AgentCode { get; set; }

    /// <summary>
    /// Booking reference for display.
    /// </summary>
    public string? BookingRef { get; set; }

    /// <summary>
    /// Customer name for display.
    /// </summary>
    public string? CustomerName { get; set; }

    // Calculated Properties

    /// <summary>
    /// Whether commission is eligible for approval (rental completed).
    /// </summary>
    public bool IsEligible => RentalId.HasValue && Status == AgentCommissionStatus.Pending;

    /// <summary>
    /// Whether commission can be paid.
    /// </summary>
    public bool CanBePaid => Status == AgentCommissionStatus.Approved;

    public override int GetId() => AgentCommissionId;
    public override void SetId(int value) => AgentCommissionId = value;
}
