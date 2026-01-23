using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Integration methods for connecting till with rental and deposit workflows.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Records a rental payment to the till using tillSessionId directly.
    /// All payment methods are recorded (cash affects drawer, others for reporting).
    /// </summary>
    public async Task<SubmitOperation> RecordRentalPaymentToTillAsync(
        int tillSessionId,
        string paymentMethod,
        int? paymentId,
        int rentalId,
        decimal amount,
        string description,
        string recordedByUserName)
    {
        var type = paymentMethod.ToLower() switch
        {
            "cash" => TillTransactionType.RentalPayment,
            "card" => TillTransactionType.CardPayment,
            "banktransfer" => TillTransactionType.BankTransfer,
            "promptpay" => TillTransactionType.PromptPay,
            "qrcode" => TillTransactionType.PromptPay, // QRCode is typically PromptPay in Thailand
            _ => TillTransactionType.RentalPayment
        };

        return await this.RecordCashInAsync(
            tillSessionId,
            type,
            amount,
            description,
            recordedByUserName,
            paymentId: paymentId,
            rentalId: rentalId);
    }

    /// <summary>
    /// Records a deposit collection to the till using tillSessionId directly.
    /// Only cash deposits affect the drawer; card pre-auth is recorded for reporting.
    /// </summary>
    public Task<SubmitOperation> RecordDepositToTillAsync(
        int tillSessionId,
        int? depositId,
        int rentalId,
        decimal amount,
        string description,
        string recordedByUserName) =>
        this.RecordCashInAsync(
            tillSessionId,
            TillTransactionType.SecurityDeposit,
            amount,
            description,
            recordedByUserName,
            depositId: depositId,
            rentalId: rentalId);

    /// <summary>
    /// Records a rental payment to the till (legacy method using shopId/staffUserName).
    /// Call this when processing cash/card payments during check-in/check-out.
    /// </summary>
    public async Task<SubmitOperation> RecordRentalPaymentToTillAsync(
        int shopId,
        string staffUserName,
        int paymentId,
        int rentalId,
        decimal amount,
        string description,
        string paymentMethod)
    {
        var session = await this.GetActiveSessionAsync(shopId, staffUserName);
        if (session is null)
            return SubmitOperation.CreateFailure("No active till session. Please open a session first.");

        var type = paymentMethod.ToLower() switch
        {
            "cash" => TillTransactionType.RentalPayment,
            "card" => TillTransactionType.CardPayment,
            "banktransfer" => TillTransactionType.BankTransfer,
            "promptpay" => TillTransactionType.PromptPay,
            _ => TillTransactionType.RentalPayment
        };

        return await this.RecordCashInAsync(
            session.TillSessionId,
            type,
            amount,
            description,
            staffUserName,
            paymentId: paymentId,
            rentalId: rentalId);
    }

    /// <summary>
    /// Records a deposit refund from the till.
    /// Call this when refunding deposits during check-out.
    /// </summary>
    public async Task<SubmitOperation> RecordDepositRefundFromTillAsync(
        int shopId,
        string staffUserName,
        int depositId,
        int rentalId,
        decimal amount,
        string description)
    {
        var session = await this.GetActiveSessionAsync(shopId, staffUserName);
        if (session is null)
            return SubmitOperation.CreateFailure("No active till session. Please open a session first.");

        return await this.RecordPayoutAsync(
            session.TillSessionId,
            TillTransactionType.DepositRefund,
            amount,
            description,
            staffUserName,
            depositId: depositId,
            rentalId: rentalId);
    }
}
