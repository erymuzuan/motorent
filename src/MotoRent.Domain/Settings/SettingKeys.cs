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

    #endregion
}
