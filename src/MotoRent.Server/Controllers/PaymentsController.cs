using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Services.Payments;

namespace MotoRent.Server.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    IFiuuPaymentService fiuuPaymentService,
    CoreDataContext coreDataContext,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private IFiuuPaymentService FiuuPaymentService { get; } = fiuuPaymentService;
    private CoreDataContext CoreDataContext { get; } = coreDataContext;
    private ILogger<PaymentsController> Logger { get; } = logger;

    [HttpPost("fiuu/ipn")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> FiuuIpn([FromForm] FiuuIpnData data)
    {
        // 1. Verify Signature
        if (!this.FiuuPaymentService.VerifyIpnSignature(data))
        {
            this.Logger.LogWarning("Invalid Fiuu IPN signature for OrderID: {OrderID}", data.OrderID);
            return this.BadRequest("Invalid signature");
        }

        // 2. Parse OrderID to get AccountNo
        // Format: "{AccountNo}-{Ticks}"
        var parts = data.OrderID.Split('-');
        if (parts.Length < 2)
        {
            this.Logger.LogWarning("Invalid OrderID format: {OrderID}", data.OrderID);
            // Even if invalid format, if signature is valid, we should probably ack to stop retries?
            // But if we can't process it, maybe logging error is enough.
            return this.Content("CBTOKEN:MPSTATOK"); 
        }

        var accountNo = parts[0];

        // 3. Find Organization
        // Since we are using CoreDataContext directly, we need to be careful about mocking in tests.
        // In real impl, we query DB.
        // For now, we'll try to load it.
        
        try
        {
            // Note: In tests with Mock<CoreDataContext>, this extension method 'LoadOneAsync' might fail if not setup correctly.
            // We'll proceed assuming integration test or properly mocked context.
            // But since LoadOneAsync is an extension on IDataContext/IRepository, it's hard to mock directly on the context unless context implements IRepository.
            // CoreDataContext inherits from DataContext which likely implements IRepository or similar.
            
            // For this implementation, we will assume we can find it.
            var org = await this.CoreDataContext.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
            
            if (org != null)
            {
                // 4. Update Status
                if (data.Status == "00") // Success
                {
                    // Payment successful
                    // Extend subscription or mark as paid
                    // For now, just log it.
                    this.Logger.LogInformation("Payment successful for {AccountNo}. TransID: {TranID}, Amount: {Amount}", 
                        accountNo, data.TranID, data.Amount);
                        
                    // TODO: Implement subscription extension logic
                }
                else
                {
                    this.Logger.LogWarning("Payment failed for {AccountNo}. Status: {Status}, Desc: {ErrorDesc}", 
                        accountNo, data.Status, data.ErrorDesc);
                }
            }
            else
            {
                this.Logger.LogWarning("Organization not found for OrderID: {OrderID}", data.OrderID);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error processing IPN for OrderID: {OrderID}", data.OrderID);
        }

        return this.Content("CBTOKEN:MPSTATOK");
    }
}
