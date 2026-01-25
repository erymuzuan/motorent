using Microsoft.AspNetCore.Components;
using MotoRent.Domain.Core;

namespace MotoRent.Client.Pages.Onboarding;

public partial class OnboardingWizard
{
    private readonly string[] m_stepKeys = ["Authentication", "ShopDetails", "FleetSetup", "SelectPlan"];
    private int m_activeStep;
    private bool m_processing;
    private bool m_isAuthenticated;

    private OnboardingRequest m_request = new();

    // OAuth callback query parameters
    [SupplyParameterFromQuery(Name = "provider")]
    public string? Provider { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public string? ProviderId { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? Email { get; set; }

    [SupplyParameterFromQuery(Name = "name")]
    public string? Name { get; set; }

    protected override void OnInitialized()
    {
        // Pre-populate from OAuth callback if available
        if (!string.IsNullOrWhiteSpace(Provider) && !string.IsNullOrWhiteSpace(ProviderId))
        {
            m_request.Provider = Provider;
            m_request.ProviderId = ProviderId;
            m_request.Email = Email ?? "";
            m_request.FullName = Name ?? "";
            m_request.UserName = Provider == "Line" ? ProviderId : (Email ?? ProviderId);
            m_isAuthenticated = true;
            m_activeStep = 1; // Skip auth step
        }
    }

    private bool CanProceedToNextStep()
    {
        return m_activeStep switch
        {
            0 => m_isAuthenticated && !string.IsNullOrWhiteSpace(m_request.Email),
            1 => !string.IsNullOrWhiteSpace(m_request.ShopName) &&
                 !string.IsNullOrWhiteSpace(m_request.Location),
            2 => m_request.Fleet.Count > 0,
            3 => true, // Plan selection validation handled in PlanSelectionStep
            _ => false
        };
    }

    private void NextStep()
    {
        if (m_activeStep < 3 && CanProceedToNextStep())
            m_activeStep++;
    }

    private void PreviousStep()
    {
        if (m_activeStep > 0)
            m_activeStep--;
    }

    private void GoToStep(int step)
    {
        // Only allow going back to previous steps
        if (step < m_activeStep)
            m_activeStep = step;
    }

    private void OnAuthenticatedChanged(bool isAuthenticated)
    {
        m_isAuthenticated = isAuthenticated;
        if (isAuthenticated)
        {
            // Auto-advance to next step after authentication
            m_activeStep = 1;
        }
    }

    private async Task CompleteOnboarding()
    {
        if (m_processing)
            return;

        m_processing = true;
        try
        {
            var result = await OnboardingService.OnboardAsync(m_request);

            if (result != null)
            {
                ShowSuccess(Localizer["AccountCreatedSuccess"]);
                NavigationManager.NavigateTo("/quick-start");
            }
            else
            {
                ShowError(Localizer["AccountCreationFailed"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Onboarding failed");
            ShowError(GetLocalizedText("OnboardingError", ex.Message));
        }
        finally
        {
            m_processing = false;
        }
    }
}
