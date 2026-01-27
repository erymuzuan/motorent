using Microsoft.AspNetCore.Components;
using MotoRent.Domain.Core;

namespace MotoRent.Client.Pages.Onboarding.Steps;

public partial class AuthStep
{
    [Parameter] public OnboardingRequest Request { get; set; } = new();
    [Parameter] public EventCallback<OnboardingRequest> RequestChanged { get; set; }
    [Parameter] public EventCallback<bool> OnAuthenticatedChanged { get; set; }

    private string m_mockName = "Dev Tester";
    private string m_mockEmail = "dev@motorent.app";
    private string m_mockProvider = "Google";

    private bool IsLocalHost => this.NavigationManager.BaseUri.Contains("localhost");

    private void SimulateLogin()
    {
        var mockId = Guid.NewGuid().ToString("N");
        // Construct URI manually to avoid overload issues
        // Note: The parent OnboardingWizard will pick up these query parameters in its OnParametersSet
        var uri = $"/onboarding?provider={this.m_mockProvider}&id={mockId}&email={Uri.EscapeDataString(this.m_mockEmail)}&name={Uri.EscapeDataString(this.m_mockName)}";
        
        this.NavigationManager.NavigateTo(uri);
    }
}