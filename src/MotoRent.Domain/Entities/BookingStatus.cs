namespace MotoRent.Domain.Entities;

/// <summary>
/// Booking status constants.
/// </summary>
public static class BookingStatus
{
    /// <summary>
    /// Booking created, awaiting confirmation or payment.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Booking confirmed (payment received or no payment required).
    /// </summary>
    public const string Confirmed = "Confirmed";

    /// <summary>
    /// Customer has checked in (at least one item).
    /// </summary>
    public const string CheckedIn = "CheckedIn";

    /// <summary>
    /// All rentals completed and returned.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// Booking cancelled.
    /// </summary>
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// All valid status values.
    /// </summary>
    public static readonly string[] AllStatuses =
    [
        Pending,
        Confirmed,
        CheckedIn,
        Completed,
        Cancelled
    ];

    /// <summary>
    /// Statuses where booking is active (not cancelled/completed).
    /// </summary>
    public static readonly string[] ActiveStatuses =
    [
        Pending,
        Confirmed,
        CheckedIn
    ];
}

/// <summary>
/// Booking item status constants.
/// </summary>
public static class BookingItemStatus
{
    /// <summary>
    /// Item not yet checked in.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Item checked in, rental created.
    /// </summary>
    public const string CheckedIn = "CheckedIn";

    /// <summary>
    /// Item cancelled (not rented).
    /// </summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Booking payment status constants.
/// </summary>
public static class BookingPaymentStatus
{
    /// <summary>
    /// No payment received.
    /// </summary>
    public const string Unpaid = "Unpaid";

    /// <summary>
    /// Partial payment received (deposit or partial amount).
    /// </summary>
    public const string PartiallyPaid = "PartiallyPaid";

    /// <summary>
    /// Full payment received.
    /// </summary>
    public const string FullyPaid = "FullyPaid";
}

/// <summary>
/// Booking source constants.
/// </summary>
public static class BookingSource
{
    /// <summary>
    /// Booking made via tourist portal (online).
    /// </summary>
    public const string TouristPortal = "TouristPortal";

    /// <summary>
    /// Booking made by staff (phone, walk-in, etc.).
    /// </summary>
    public const string Staff = "Staff";
}
