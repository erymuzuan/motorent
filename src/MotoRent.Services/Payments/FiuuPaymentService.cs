using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MotoRent.Services.Payments;

public class FiuuPaymentService : IFiuuPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FiuuPaymentService> _logger;
    private readonly string _merchantId;
    private readonly string _verifyKey;
    private readonly string _secretKey;
    private readonly string _baseUrl;

    public FiuuPaymentService(IConfiguration configuration, ILogger<FiuuPaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _merchantId = _configuration["Fiuu:MerchantId"] ?? "";
        _verifyKey = _configuration["Fiuu:VerifyKey"] ?? "";
        _secretKey = _configuration["Fiuu:SecretKey"] ?? "";
        _baseUrl = _configuration["Fiuu:BaseUrl"] ?? "https://sandbox.merchant.razer.com";
    }

    public string GenerateSignature(decimal amount, string orderId)
    {
        // Formula: amount + merchantID + orderID + verifyKey
        var amountStr = amount.ToString("0.00");
        var raw = $"{amountStr}{_merchantId}{orderId}{_verifyKey}";
        return ComputeMd5(raw);
    }

    public bool VerifyIpnSignature(FiuuIpnData data)
    {
        // Formula: tranID + orderID + status + domain + amount + currency + appcode + paydate + secret_key
        var raw = $"{data.TranID}{data.OrderID}{data.Status}{data.Domain}{data.Amount}{data.Currency}{data.AppCode}{data.PayDate}{_secretKey}";
        var computedHash = ComputeMd5(raw);
        
        return string.Equals(computedHash, data.Skey, StringComparison.OrdinalIgnoreCase);
    }

    public string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        var sb = new StringBuilder();
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
    
    public FiuuPaymentRequest CreatePaymentRequest(decimal amount, string orderId, string billName, string billEmail, string billMobile, string billDesc)
    {
        var vcode = GenerateSignature(amount, orderId);
        
        return new FiuuPaymentRequest
        {
            MerchantId = _merchantId,
            Amount = amount.ToString("0.00"),
            OrderId = orderId,
            BillName = billName,
            BillEmail = billEmail,
            BillMobile = billMobile,
            BillDesc = billDesc,
            Country = "TH",
            Currency = "THB",
            VCode = vcode,
            // URLs should be set by the caller or config
        };
    }
}
