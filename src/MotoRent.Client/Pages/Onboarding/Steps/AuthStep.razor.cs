using Microsoft.AspNetCore.Components;
using MotoRent.Domain.Core;

namespace MotoRent.Client.Pages.Onboarding.Steps;

public partial class AuthStep
{
    [Parameter] public OnboardingRequest Request { get; set; } = new();
    [Parameter] public EventCallback<OnboardingRequest> RequestChanged { get; set; }
    [Parameter] public EventCallback<bool> OnAuthenticatedChanged { get; set; }

    private bool m_isAuthenticated;

    [SupplyParameterFromQuery(Name = "provider")]
    public string? Provider { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public string? ProviderId { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? Email { get; set; }

    [SupplyParameterFromQuery(Name = "name")]
    public string? Name { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        // Check if we have OAuth callback data
        if (!string.IsNullOrWhiteSpace(Provider) && !string.IsNullOrWhiteSpace(ProviderId))
        {
            Request.Provider = Provider;
            Request.ProviderId = ProviderId;
            Request.Email = Email ?? "";
            Request.FullName = Name ?? "";
            Request.UserName = Provider == "Line" ? ProviderId : (Email ?? ProviderId);

            m_isAuthenticated = true;
            await RequestChanged.InvokeAsync(Request);
            await OnAuthenticatedChanged.InvokeAsync(true);
        }
    }
}
