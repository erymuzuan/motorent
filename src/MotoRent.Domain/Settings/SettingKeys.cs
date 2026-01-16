namespace MotoRent.Domain.Settings;

/// <summary>
/// Constants for organization setting keys.
/// Follows naming convention: Category.SettingName
/// </summary>
public static class SettingKeys
{
    #region Rental Workflow Settings

    /// <summary>
    /// Require pre-inspection during check-in (bool).
    /// </summary>
    public const string Rental_RequirePreInspection = "Rental.RequirePreInspection";

    /// <summary>
    /// Require damage photos during check-in/check-out (bool).
    /// </summary>
    public const string Rental_RequireDamagePhotos = "Rental.RequireDamagePhotos";

    /// <summary>
    /// Default rental period in days (int).
    /// </summary>
    public const string Rental_DefaultRentalPeriod = "Rental.DefaultRentalPeriod";

    /// <summary>
    /// Require deposit collection (bool).
    /// </summary>
    public const string Rental_RequireDeposit = "Rental.RequireDeposit";

    /// <summary>
    /// Default insurance package ID (int).
    /// </summary>
    public const string Rental_DefaultInsuranceId = "Rental.DefaultInsuranceId";

    /// <summary>
    /// Allow online reservations from tourist portal (bool).
    /// </summary>
    public const string Rental_AllowOnlineReservation = "Rental.AllowOnlineReservation";

    /// <summary>
    /// Require ID verification during check-in (bool).
    /// </summary>
    public const string Rental_RequireIdVerification = "Rental.RequireIdVerification";

    /// <summary>
    /// Auto-generate rental agreement (bool).
    /// </summary>
    public const string Rental_AutoGenerateAgreement = "Rental.AutoGenerateAgreement";

    #endregion

    #region Notification Settings

    /// <summary>
    /// Send rental confirmation email (bool).
    /// </summary>
    public const string Notification_SendRentalConfirmation = "Notification.SendRentalConfirmation";

    /// <summary>
    /// Send payment receipt email (bool).
    /// </summary>
    public const string Notification_SendPaymentReceipt = "Notification.SendPaymentReceipt";

    /// <summary>
    /// Days before return to send reminder (int).
    /// </summary>
    public const string Notification_ReminderDaysBeforeReturn = "Notification.ReminderDaysBeforeReturn";

    /// <summary>
    /// Email provider (string: smtp, sendgrid, etc.).
    /// </summary>
    public const string Notification_EmailProvider = "Notification.EmailProvider";

    /// <summary>
    /// SMS provider (string: twilio, etc.).
    /// </summary>
    public const string Notification_SmsProvider = "Notification.SmsProvider";

    /// <summary>
    /// Send late return alert (bool).
    /// </summary>
    public const string Notification_SendLateReturnAlert = "Notification.SendLateReturnAlert";

    #endregion

    #region Payment Settings

    /// <summary>
    /// Default payment method (string: Cash, Card, PromptPay, BankTransfer).
    /// </summary>
    public const string Payment_DefaultMethod = "Payment.DefaultMethod";

    /// <summary>
    /// Accepted payment methods (string array: Cash,Card,PromptPay,BankTransfer).
    /// </summary>
    public const string Payment_AcceptedMethods = "Payment.AcceptedMethods";

    /// <summary>
    /// Default currency (string: THB, USD, etc.).
    /// </summary>
    public const string Payment_DefaultCurrency = "Payment.DefaultCurrency";

    /// <summary>
    /// PromptPay ID (string: phone number or citizen ID).
    /// </summary>
    public const string Payment_PromptPayId = "Payment.PromptPayId";

    /// <summary>
    /// Bank account number (string).
    /// </summary>
    public const string Payment_BankAccountNo = "Payment.BankAccountNo";

    /// <summary>
    /// Bank name (string).
    /// </summary>
    public const string Payment_BankName = "Payment.BankName";

    /// <summary>
    /// Bank account holder name (string).
    /// </summary>
    public const string Payment_BankAccountName = "Payment.BankAccountName";

    #endregion

    #region Deposit Settings

    /// <summary>
    /// Default deposit amount (decimal).
    /// </summary>
    public const string Deposit_DefaultAmount = "Deposit.DefaultAmount";

    /// <summary>
    /// Deposit refund policy (string: immediate, end-of-day, manual).
    /// </summary>
    public const string Deposit_RefundPolicy = "Deposit.RefundPolicy";

    #endregion

    #region Tourist Portal Settings

    /// <summary>
    /// Enable online booking from tourist portal (bool).
    /// </summary>
    public const string Portal_EnableOnlineBooking = "Portal.EnableOnlineBooking";

    /// <summary>
    /// Require ID verification before online booking (bool).
    /// </summary>
    public const string Portal_RequireIdVerification = "Portal.RequireIdVerification";

    /// <summary>
    /// Show pricing on tourist portal (bool).
    /// </summary>
    public const string Portal_ShowPricing = "Portal.ShowPricing";

    /// <summary>
    /// Allow guest checkout without account (bool).
    /// </summary>
    public const string Portal_AllowGuestCheckout = "Portal.AllowGuestCheckout";

    #endregion

    #region Booking Settings

    /// <summary>
    /// Payment model for bookings (string: Flexible, DepositRequired, FullPrepay, PayAtPickup).
    /// </summary>
    public const string Booking_PaymentModel = "Booking.PaymentModel";

    /// <summary>
    /// Deposit percentage required at booking (int: 0-100).
    /// </summary>
    public const string Booking_DepositPercent = "Booking.DepositPercent";

    /// <summary>
    /// Minimum deposit amount (decimal).
    /// </summary>
    public const string Booking_MinDepositAmount = "Booking.MinDepositAmount";

    /// <summary>
    /// Allow multiple vehicles in one booking (bool).
    /// </summary>
    public const string Booking_AllowMultiVehicle = "Booking.AllowMultiVehicle";

    /// <summary>
    /// Cancellation policy (string: Free, TimeBased, NonRefundable).
    /// </summary>
    public const string Booking_CancellationPolicy = "Booking.CancellationPolicy";

    /// <summary>
    /// Hours before pickup for free cancellation (int).
    /// Only applicable when CancellationPolicy is TimeBased.
    /// </summary>
    public const string Booking_FreeCancelHours = "Booking.FreeCancelHours";

    /// <summary>
    /// Penalty percentage for late cancellation (int: 0-100).
    /// </summary>
    public const string Booking_LateCancelPenaltyPercent = "Booking.LateCancelPenaltyPercent";

    /// <summary>
    /// Penalty percentage for no-show (int: 0-100).
    /// </summary>
    public const string Booking_NoShowPenaltyPercent = "Booking.NoShowPenaltyPercent";

    /// <summary>
    /// Send booking confirmation notification (bool).
    /// </summary>
    public const string Booking_SendConfirmation = "Booking.SendConfirmation";

    /// <summary>
    /// Send booking reminder notification (bool).
    /// </summary>
    public const string Booking_SendReminder = "Booking.SendReminder";

    /// <summary>
    /// Hours before pickup to send reminder (int).
    /// </summary>
    public const string Booking_ReminderHours = "Booking.ReminderHours";

    #endregion

    #region Fleet Settings

    /// <summary>
    /// Default maintenance interval in days (int).
    /// </summary>
    public const string Fleet_MaintenanceIntervalDays = "Fleet.MaintenanceIntervalDays";

    /// <summary>
    /// Default maintenance interval in kilometers (int).
    /// </summary>
    public const string Fleet_MaintenanceIntervalKm = "Fleet.MaintenanceIntervalKm";

    /// <summary>
    /// Enable vehicle pool sharing across shops (bool).
    /// </summary>
    public const string Fleet_EnablePoolSharing = "Fleet.EnablePoolSharing";

    /// <summary>
    /// Enabled vehicle types (string array: Motorbike,Car,JetSki,Boat,Van).
    /// </summary>
    public const string Fleet_EnabledVehicleTypes = "Fleet.EnabledVehicleTypes";

    /// <summary>
    /// Enable motorbike rentals (bool).
    /// </summary>
    public const string Fleet_EnableMotorbike = "Fleet.EnableMotorbike";

    /// <summary>
    /// Enable car rentals (bool).
    /// </summary>
    public const string Fleet_EnableCar = "Fleet.EnableCar";

    /// <summary>
    /// Enable jet ski rentals (bool).
    /// </summary>
    public const string Fleet_EnableJetSki = "Fleet.EnableJetSki";

    /// <summary>
    /// Enable boat rentals (bool).
    /// </summary>
    public const string Fleet_EnableBoat = "Fleet.EnableBoat";

    /// <summary>
    /// Enable van rentals (bool).
    /// </summary>
    public const string Fleet_EnableVan = "Fleet.EnableVan";

    #endregion
}
