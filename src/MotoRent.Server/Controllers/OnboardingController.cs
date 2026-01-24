using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for the multi-step onboarding wizard.
/// </summary>
[ApiController]
[Route("api/onboarding")]
public class OnboardingController(
    IOnboardingService onboardingService,
    CoreDataContext coreDataContext,
    ILogger<OnboardingController> logger) : ControllerBase
{
    private IOnboardingService OnboardingService { get; } = onboardingService;
    private CoreDataContext CoreDataContext { get; } = coreDataContext;
    private ILogger<OnboardingController> Logger { get; } = logger;

    /// <summary>
    /// Checks if an account number or shop name slug is available.
    /// </summary>
    [HttpGet("check-availability")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability([FromQuery] string accountNo)
    {
        if (string.IsNullOrWhiteSpace(accountNo))
            return this.BadRequest("Account number is required");

        bool exists = await this.CoreDataContext.ExistAsync<Organization>(o => o.AccountNo == accountNo);
        return this.Ok(new { available = !exists });
    }

    /// <summary>
    /// Completes the onboarding process by creating the organization, shop, and user.
    /// </summary>
    [HttpPost("submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] OnboardingRequest request)
    {
        if (!this.ModelState.IsValid)
            return this.BadRequest(this.ModelState);

        try
        {
            var org = await this.OnboardingService.OnboardAsync(request);
            this.Logger.LogInformation("Successfully onboarded new organization {AccountNo} ({ShopName})", 
                org.AccountNo, request.ShopName);
            
            return this.Ok(new { 
                success = true, 
                accountNo = org.AccountNo,
                message = "Welcome to MotoRent! Your shop is being prepared."
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to complete onboarding for {ShopName}", request.ShopName);
            return this.StatusCode(500, new { 
                success = false, 
                error = "An internal error occurred during onboarding. Please contact support." 
            });
        }
    }
}
