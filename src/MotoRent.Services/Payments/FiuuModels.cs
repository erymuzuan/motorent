namespace MotoRent.Services.Payments;

public class FiuuIpnData
{
    public string TranID { get; set; } = string.Empty;
    public string OrderID { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string AppCode { get; set; } = string.Empty;
    public string PayDate { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Skey { get; set; } = string.Empty;
    public string ExtraP { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorDesc { get; set; } = string.Empty;
}

public class FiuuPaymentRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string BillName { get; set; } = string.Empty;
    public string BillEmail { get; set; } = string.Empty;
    public string BillMobile { get; set; } = string.Empty;
    public string BillDesc { get; set; } = string.Empty;
    public string Country { get; set; } = "TH";
    public string Currency { get; set; } = "THB";
    public string VCode { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty; // IPN
    public string CancelUrl { get; set; } = string.Empty;
}
