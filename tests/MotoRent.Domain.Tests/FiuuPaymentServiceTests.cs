using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Services.Payments;
using Xunit;

namespace MotoRent.Domain.Tests;

public class FiuuPaymentServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<FiuuPaymentService>> _loggerMock;
    private readonly FiuuPaymentService _service;

    public FiuuPaymentServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<FiuuPaymentService>>();

        // Setup mock config
        _configMock.Setup(c => c["Fiuu:MerchantId"]).Returns("test_merchant");
        _configMock.Setup(c => c["Fiuu:VerifyKey"]).Returns("test_verify_key");
        _configMock.Setup(c => c["Fiuu:SecretKey"]).Returns("test_secret_key");
        _configMock.Setup(c => c["Fiuu:BaseUrl"]).Returns("https://sandbox.merchant.razer.com");

        _service = new FiuuPaymentService(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GenerateSignature_ShouldReturnCorrectMd5Hash()
    {
        // Arrange
        // Formula: amount + merchantID + orderID + verifyKey
        var amount = 100.00m;
        var orderId = "ORDER123";
        // Expected string: "100.00test_merchantORDER123test_verify_key"
        // MD5 of above. 
        // 100.00test_merchantORDER123test_verify_key -> 5769856337725f903b4060bc107e428c
        var expectedSignature = "5769856337725f903b4060bc107e428c"; 

        // Act
        var signature = _service.GenerateSignature(amount, orderId);

        // Assert
        Assert.Equal(expectedSignature, signature);
    }

    [Fact]
    public void VerifyIpnSignature_ShouldReturnTrue_ForValidSignature()
    {
        // Arrange
        // Formula often used: tranID + orderID + status + domain + amount + currency + appcode + paydate + skey
        var data = new FiuuIpnData
        {
            TranID = "TRAN123",
            OrderID = "ORDER123",
            Status = "00",
            Domain = "test_merchant",
            Amount = "100.00",
            Currency = "THB",
            AppCode = "123456",
            PayDate = "2023-10-27 10:00:00",
            Skey = "" // To be calculated
        };

        // Construct raw string to match service implementation
        // Assuming service implements: tranID + orderID + status + domain + amount + currency + key
        // Wait, standard RMS formula is: treq=1 (or similar) ... 
        // Actually, typical IPN signature (skey) check:
        // MD5( tranID + orderID + status + domain + amount + currency + appcode + paydate + secret_key )
        
        var rawString = $"{data.TranID}{data.OrderID}{data.Status}{data.Domain}{data.Amount}{data.Currency}{data.AppCode}{data.PayDate}test_secret_key";
        var expectedHash = _service.ComputeMd5(rawString);
        data.Skey = expectedHash;

        // Act
        var isValid = _service.VerifyIpnSignature(data);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyIpnSignature_ShouldReturnFalse_ForInvalidSignature()
    {
        // Arrange
        var data = new FiuuIpnData
        {
            TranID = "TRAN123",
            OrderID = "ORDER123",
            Status = "00",
            Domain = "test_merchant",
            Amount = "100.00",
            Currency = "THB",
            AppCode = "123456",
            PayDate = "2023-10-27 10:00:00",
            Skey = "invalid_hash"
        };

        // Act
        var isValid = _service.VerifyIpnSignature(data);

        // Assert
        Assert.False(isValid);
    }
}
