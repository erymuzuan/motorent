namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a receipt for a transaction in the rental system.
/// Supports booking deposits, check-in receipts, and settlement receipts.
/// </summary>
public class Receipt : Entity
{
    public int ReceiptId { get; set; }

    /// <summary>
    /// Unique receipt number (format: RCP-YYMMDD-XXXXX)
    /// </summary>
    public string ReceiptNo { get; set; } = string.Empty;

    /// <summary>
    /// Type of receipt (BookingDeposit, CheckIn, Settlement)
    /// </summary>
    public string ReceiptType { get; set; } = ReceiptTypes.CheckIn;

    /// <summary>
    /// Receipt status (Issued, Voided)
    /// </summary>
    public string Status { get; set; } = ReceiptStatus.Issued;

    #region References

    /// <summary>
    /// Reference to the booking (for BookingDeposit receipts)
    /// </summary>
    public int? BookingId { get; set; }

    /// <summary>
    /// Booking reference code (denormalized from Booking.BookingRef for display)
    /// </summary>
    public string? BookingRef { get; set; }

    /// <summary>
    /// Reference to the rental (for CheckIn and Settlement receipts)
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Reference to the till session when the receipt was issued
    /// </summary>
    public int? TillSessionId { get; set; }

    /// <summary>
    /// Shop where the receipt was issued
    /// </summary>
    public int ShopId { get; set; }

    #endregion

    #region Customer Information (denormalized for printing)

    /// <summary>
    /// Reference to the renter
    /// </summary>
    public int? RenterId { get; set; }

    /// <summary>
    /// Customer's full name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's phone number
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Customer's passport or ID number
    /// </summary>
    public string? CustomerPassportNo { get; set; }

    #endregion

    #region Line Items

    /// <summary>
    /// Receipt line items (rental, insurance, accessories, deposits, etc.)
    /// </summary>
    public List<ReceiptItem> Items { get; set; } = [];

    #endregion

    #region Totals

    /// <summary>
    /// Subtotal before any adjustments
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Grand total amount
    /// </summary>
    public decimal GrandTotal { get; set; }

    #endregion

    #region Settlement Fields (for check-out receipts)

    /// <summary>
    /// Total deposit held from check-in
    /// </summary>
    public decimal DepositHeld { get; set; }

    /// <summary>
    /// Total deductions (damage, extra days, etc.)
    /// </summary>
    public decimal DeductionsTotal { get; set; }

    /// <summary>
    /// Amount to be refunded to customer
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Amount due from customer (if deductions exceed deposit)
    /// </summary>
    public decimal AmountDue { get; set; }

    #endregion

    #region Payments

    /// <summary>
    /// Payment records (supports split payments with multi-currency)
    /// </summary>
    public List<ReceiptPayment> Payments { get; set; } = [];

    #endregion

    #region Audit

    /// <summary>
    /// When the receipt was issued
    /// </summary>
    public DateTimeOffset IssuedOn { get; set; }

    /// <summary>
    /// Username of the staff who issued the receipt
    /// </summary>
    public string IssuedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// When the receipt was voided (if applicable)
    /// </summary>
    public DateTimeOffset? VoidedOn { get; set; }

    /// <summary>
    /// Reason for voiding the receipt
    /// </summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Number of times the receipt has been reprinted
    /// </summary>
    public int ReprintCount { get; set; }

    #endregion

    #region Shop Information (denormalized for printing)

    /// <summary>
    /// Shop name
    /// </summary>
    public string? ShopName { get; set; }

    /// <summary>
    /// Shop address
    /// </summary>
    public string? ShopAddress { get; set; }

    /// <summary>
    /// Shop phone number
    /// </summary>
    public string? ShopPhone { get; set; }

    #endregion

    #region Vehicle Information (denormalized for printing)

    /// <summary>
    /// Vehicle name/description
    /// </summary>
    public string? VehicleName { get; set; }

    /// <summary>
    /// Vehicle license plate
    /// </summary>
    public string? LicensePlate { get; set; }

    #endregion

    public override int GetId() => ReceiptId;
    public override void SetId(int value) => ReceiptId = value;
}
