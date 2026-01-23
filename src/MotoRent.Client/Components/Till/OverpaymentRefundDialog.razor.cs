namespace MotoRent.Client.Components.Till;

/// <summary>
/// Result from overpayment refund dialog.
/// </summary>
public class OverpaymentRefundResult
{
    /// <summary>
    /// Refund amount in THB.
    /// </summary>
    public decimal RefundAmountThb { get; set; }

    /// <summary>
    /// Reason for refund.
    /// Required field with minimum 5 characters.
    /// </summary>
    public string Reason { get; set; } = "";

    /// <summary>
    /// Related rental ID if any.
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Original payment IDs (string type to match ReceiptPayment.PaymentId).
    /// </summary>
    public List<string>? OriginalPaymentIds { get; set; }
}
