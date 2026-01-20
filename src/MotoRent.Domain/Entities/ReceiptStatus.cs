namespace MotoRent.Domain.Entities;

/// <summary>
/// Receipt type constants
/// </summary>
public static class ReceiptTypes
{
    public const string BookingDeposit = "BookingDeposit";
    public const string CheckIn = "CheckIn";
    public const string Settlement = "Settlement";
}

/// <summary>
/// Receipt status constants
/// </summary>
public static class ReceiptStatus
{
    public const string Issued = "Issued";
    public const string Voided = "Voided";
}

/// <summary>
/// Receipt item category constants
/// </summary>
public static class ReceiptItemCategory
{
    public const string Rental = "Rental";
    public const string Insurance = "Insurance";
    public const string Accessory = "Accessory";
    public const string Deposit = "Deposit";
    public const string DepositRefund = "DepositRefund";
    public const string Damage = "Damage";
    public const string ExtraDays = "ExtraDays";
    public const string LateFee = "LateFee";
    public const string LocationFee = "LocationFee";
    public const string DriverFee = "DriverFee";
    public const string GuideFee = "GuideFee";
    public const string Discount = "Discount";
    public const string Tax = "Tax";
}
