using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Server.Controllers;
using MotoRent.Services.Payments;
using Xunit;

namespace MotoRent.Server.Tests;

public class PaymentsControllerTests
{
    private readonly Mock<IFiuuPaymentService> _fiuuServiceMock;
    private readonly Mock<CoreDataContext> _coreContextMock;
    private readonly Mock<ILogger<PaymentsController>> _loggerMock;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _fiuuServiceMock = new Mock<IFiuuPaymentService>();
        _coreContextMock = new Mock<CoreDataContext>(new Mock<IServiceProvider>().Object);
        _loggerMock = new Mock<ILogger<PaymentsController>>();

        _controller = new PaymentsController(
            _fiuuServiceMock.Object,
            _coreContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Ipn_ShouldReturnCbToken_WhenSignatureIsValid()
    {
        // Arrange
        var ipnData = new FiuuIpnData
        {
            TranID = "TRAN123",
            OrderID = "shop1-123",
            Status = "00",
            Amount = "100.00"
        };

        _fiuuServiceMock.Setup(s => s.VerifyIpnSignature(ipnData))
            .Returns(true);

        // We also need to mock looking up the organization by AccountNo (parsed from OrderID)
        // OrderID format: "{AccountNo}-{Ticks}"
        // Mock DB context logic would be complex here without an in-memory DB or strict repository pattern.
        // For this unit test, we'll assume the controller handles "Org not found" gracefully or we mock the lookup.
        // Since CoreDataContext is mocked, we might need to mock the `Query<Organization>()` method or similar if the controller uses it directly.
        // Or if the controller uses a service, we'd mock that.
        // Assuming controller uses CoreDataContext directly (as is common in this codebase for simple lookups).

        // Act
        var result = await _controller.FiuuIpn(ipnData);

        // Assert
        // Expecting ContentResult with "CBTOKEN:MPSTATOK"
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("CBTOKEN:MPSTATOK", contentResult.Content);
    }

    [Fact]
    public async Task Ipn_ShouldReturnError_WhenSignatureIsInvalid()
    {
        // Arrange
        var ipnData = new FiuuIpnData { OrderID = "shop1-123" };
        _fiuuServiceMock.Setup(s => s.VerifyIpnSignature(ipnData))
            .Returns(false);

        // Act
        var result = await _controller.FiuuIpn(ipnData);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid signature", badRequestResult.Value);
    }
}
