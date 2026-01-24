using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Data transfer object for initial fleet setup during onboarding.
/// </summary>
public class InitialFleetDto
{
    public string VehicleType { get; set; } = "Motorbike";
    public int Quantity { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// Data transfer object for onboarding request.
/// </summary>
public class OnboardingRequest
{
    public string Provider { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    
    public string ShopName { get; set; } = "";
    public string Location { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PreferredLanguage { get; set; } = "th-TH";
    
    public List<InitialFleetDto> Fleet { get; set; } = [];
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Pro;
}

/// <summary>
/// Service for handling the multi-step onboarding and initial setup of a new organization.
/// </summary>
public interface IOnboardingService
{
    /// <summary>
    /// Processes an onboarding request, creating the organization, user, shop, and initial vehicles.
    /// </summary>
    /// <param name="request">The onboarding details.</param>
    /// <returns>The created organization.</returns>
    Task<Organization> OnboardAsync(OnboardingRequest request);
}
