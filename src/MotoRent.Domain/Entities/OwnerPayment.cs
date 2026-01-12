using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Tracks payments due/paid to third-party vehicle owners.
/// Created automatically when rentals complete (checkout).
/// </summary>
public class OwnerPayment : Entity
{
    public int OwnerPaymentId { get; set; }

    #region References

    /// <summary>
    /// The vehicle owner receiving this payment.
    /// </summary>
    public int VehicleOwnerId { get; set; }

    /// <summary>
    /// The vehicle that generated this payment.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// The rental that triggered this payment calculation.
    /// </summary>
    public int RentalId { get; set; }

    #endregion

    #region Calculation Details

    /// <summary>
    /// Payment model used for calculation (DailyRate or RevenueShare).
    /// </summary>
    public OwnerPaymentModel PaymentModel { get; set; }

    /// <summary>
    /// Number of rental days.
    /// </summary>
    public int RentalDays { get; set; }

    /// <summary>
    /// Gross rental amount before calculation (RentalRate × Days).
    /// This is NOT the total with insurance/accessories.
    /// </summary>
    public decimal GrossRentalAmount { get; set; }

    /// <summary>
    /// Rate used for calculation:
    /// - DailyRate: the daily rate (e.g., 200)
    /// - RevenueShare: the percentage (e.g., 0.30 for 30%)
    /// </summary>
    public decimal CalculationRate { get; set; }

    /// <summary>
    /// Calculated amount due to owner.
    /// - DailyRate: RentalDays × OwnerDailyRate
    /// - RevenueShare: GrossRentalAmount × OwnerRevenueSharePercent
    /// </summary>
    public decimal Amount { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Payment status: Pending, Paid, Cancelled.
    /// </summary>
    public OwnerPaymentStatus Status { get; set; } = OwnerPaymentStatus.Pending;

    /// <summary>
    /// When the payment was made (if Status == Paid).
    /// </summary>
    public DateTimeOffset? PaidOn { get; set; }

    /// <summary>
    /// Reference number for the payment (bank transfer ref, etc.).
    /// </summary>
    public string? PaymentRef { get; set; }

    /// <summary>
    /// Payment method used: BankTransfer, Cash, PromptPay.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Additional notes about this payment.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Denormalized Fields

    /// <summary>
    /// Owner name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleOwnerName { get; set; }

    /// <summary>
    /// Vehicle description for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleName { get; set; }

    /// <summary>
    /// Rental start date for display/sorting.
    /// </summary>
    public DateTimeOffset RentalStartDate { get; set; }

    /// <summary>
    /// Rental end date for display/sorting.
    /// </summary>
    public DateTimeOffset RentalEndDate { get; set; }

    #endregion

    public override int GetId() => this.OwnerPaymentId;
    public override void SetId(int value) => this.OwnerPaymentId = value;
}
