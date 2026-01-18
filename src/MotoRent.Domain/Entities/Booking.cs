namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a booking/reservation made by a customer (tourist portal or staff-assisted).
/// A booking can contain multiple vehicles (group booking) and is converted to Rental(s) at check-in.
/// Supports cross-shop flexibility: customer can check-in at ANY shop with matching vehicles.
/// </summary>
public class Booking : Entity
{
    public int BookingId { get; set; }

    /// <summary>
    /// Unique 6-character alphanumeric booking reference (e.g., "ABC123").
    /// Used for customer communication and lookup.
    /// </summary>
    public string BookingRef { get; set; } = string.Empty;

    /// <summary>
    /// Current status: Pending, Confirmed, CheckedIn, Completed, Cancelled.
    /// </summary>
    public string Status { get; set; } = BookingStatus.Pending;

    // Shop Flexibility: Customer can check-in at ANY shop with matching vehicle

    /// <summary>
    /// Shop where customer originally made the booking (optional).
    /// Customer is NOT restricted to this shop - they can check-in anywhere.
    /// </summary>
    public int? PreferredShopId { get; set; }

    /// <summary>
    /// Shop where customer actually checked in.
    /// Set when any item is checked in.
    /// </summary>
    public int? CheckedInAtShopId { get; set; }

    // Customer Contact (may not have RenterId yet)

    /// <summary>
    /// Optional link to existing Renter record.
    /// Created/linked at check-in when customer presents documents.
    /// </summary>
    public int? RenterId { get; set; }

    /// <summary>
    /// Customer's full name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's phone number (required for contact).
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address (for confirmations).
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Customer's nationality (optional).
    /// </summary>
    public string? CustomerNationality { get; set; }

    /// <summary>
    /// Customer's passport number (optional, can be added later).
    /// </summary>
    public string? CustomerPassportNo { get; set; }

    /// <summary>
    /// Customer's hotel or accommodation name.
    /// </summary>
    public string? HotelName { get; set; }

    /// <summary>
    /// Additional notes or special requests.
    /// </summary>
    public string? Notes { get; set; }

    // Dates

    /// <summary>
    /// Rental start date.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Rental end date.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Scheduled pickup time (optional).
    /// </summary>
    public TimeSpan? PickupTime { get; set; }

    // Pickup/Dropoff Locations

    /// <summary>
    /// Pickup location ID (null = at shop).
    /// </summary>
    public int? PickupLocationId { get; set; }

    /// <summary>
    /// Dropoff location ID (null = at shop).
    /// </summary>
    public int? DropoffLocationId { get; set; }

    /// <summary>
    /// Denormalized pickup location name.
    /// </summary>
    public string? PickupLocationName { get; set; }

    /// <summary>
    /// Denormalized dropoff location name.
    /// </summary>
    public string? DropoffLocationName { get; set; }

    /// <summary>
    /// Fee for pickup location.
    /// </summary>
    public decimal PickupLocationFee { get; set; }

    /// <summary>
    /// Fee for dropoff location.
    /// </summary>
    public decimal DropoffLocationFee { get; set; }

    // Pricing

    /// <summary>
    /// Total amount for all items (vehicle + insurance + accessories + location fees).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Required deposit amount.
    /// </summary>
    public decimal DepositRequired { get; set; }

    /// <summary>
    /// Amount already paid (deposit + any advance payments).
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Payment status: Unpaid, PartiallyPaid, FullyPaid.
    /// </summary>
    public string PaymentStatus { get; set; } = BookingPaymentStatus.Unpaid;

    // Cancellation

    /// <summary>
    /// When the booking was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledOn { get; set; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Amount refunded upon cancellation.
    /// </summary>
    public decimal? RefundAmount { get; set; }

    // Booking Items (multi-vehicle)

    /// <summary>
    /// List of vehicles in this booking.
    /// </summary>
    public List<BookingItem> Items { get; set; } = [];

    // Payments

    /// <summary>
    /// Payment records for this booking.
    /// </summary>
    public List<BookingPayment> Payments { get; set; } = [];

    // Source tracking

    /// <summary>
    /// Where booking originated: "TouristPortal", "Staff", or "Agent".
    /// </summary>
    public string BookingSource { get; set; } = "Staff";

    // Agent Booking

    /// <summary>
    /// Agent who made this booking (if agent booking).
    /// </summary>
    public int? AgentId { get; set; }

    /// <summary>
    /// Agent code for display (denormalized).
    /// </summary>
    public string? AgentCode { get; set; }

    /// <summary>
    /// Agent name for display (denormalized).
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Whether this is an agent booking.
    /// </summary>
    public bool IsAgentBooking { get; set; }

    // Agent Financials

    /// <summary>
    /// Calculated commission amount for agent.
    /// </summary>
    public decimal AgentCommission { get; set; }

    /// <summary>
    /// Surcharge amount added by agent.
    /// </summary>
    public decimal AgentSurcharge { get; set; }

    /// <summary>
    /// Whether surcharge is hidden from customer (shows unified total).
    /// </summary>
    public bool SurchargeHidden { get; set; }

    /// <summary>
    /// Payment flow: CustomerPaysShop or CustomerPaysAgent.
    /// </summary>
    public string AgentPaymentFlow { get; set; } = PaymentFlow.CustomerPaysShop;

    /// <summary>
    /// Total shown to customer (may include hidden surcharge).
    /// </summary>
    public decimal CustomerVisibleTotal { get; set; }

    /// <summary>
    /// What shop receives after commission (TotalAmount - AgentCommission).
    /// </summary>
    public decimal ShopReceivableAmount { get; set; }

    // Change History (for staff audit trail)

    /// <summary>
    /// History of changes made to this booking.
    /// </summary>
    public List<BookingChange> ChangeHistory { get; set; } = [];

    // Denormalized

    /// <summary>
    /// Name of preferred shop for display.
    /// </summary>
    public string? PreferredShopName { get; set; }

    /// <summary>
    /// Name of shop where customer checked in.
    /// </summary>
    public string? CheckedInAtShopName { get; set; }

    /// <summary>
    /// Name of linked renter for display.
    /// </summary>
    public string? RenterName { get; set; }

    // Calculated Properties

    /// <summary>
    /// Number of rental days.
    /// </summary>
    public int Days => Math.Max(1, (int)Math.Ceiling((EndDate - StartDate).TotalDays));

    /// <summary>
    /// Number of vehicles in booking.
    /// </summary>
    public int VehicleCount => Items.Count;

    /// <summary>
    /// Outstanding balance to be paid.
    /// </summary>
    public decimal BalanceDue => TotalAmount - AmountPaid;

    /// <summary>
    /// Whether all items have been checked in.
    /// </summary>
    public bool AllItemsCheckedIn => Items.Count > 0 && Items.All(i => i.ItemStatus == BookingItemStatus.CheckedIn);

    /// <summary>
    /// Whether any item has been checked in.
    /// </summary>
    public bool AnyItemCheckedIn => Items.Any(i => i.ItemStatus == BookingItemStatus.CheckedIn);

    /// <summary>
    /// Whether booking can be cancelled (not already checked in or cancelled).
    /// </summary>
    public bool CanBeCancelled => Status != BookingStatus.CheckedIn &&
                                   Status != BookingStatus.Completed &&
                                   Status != BookingStatus.Cancelled;

    /// <summary>
    /// Whether this booking can be checked in (Pending or Confirmed status).
    /// </summary>
    public bool CanCheckIn => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;

    public override int GetId() => BookingId;
    public override void SetId(int value) => BookingId = value;
}
