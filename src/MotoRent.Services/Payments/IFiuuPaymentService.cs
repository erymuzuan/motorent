namespace MotoRent.Services.Payments;

public interface IFiuuPaymentService
{
    string GenerateSignature(decimal amount, string orderId);
    bool VerifyIpnSignature(FiuuIpnData data);
    string ComputeMd5(string input);
    FiuuPaymentRequest CreatePaymentRequest(decimal amount, string orderId, string billName, string billEmail, string billMobile, string billDesc);
}
