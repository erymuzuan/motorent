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

    protected override void OnParametersSet()
    {
        // Pre-populate from OAuth callback if available
        if (!string.IsNullOrWhiteSpace(this.Provider) && !string.IsNullOrWhiteSpace(this.ProviderId))
        {
            this.m_request.Provider = this.Provider;
            this.m_request.ProviderId = this.ProviderId;
            this.m_request.Email = this.Email ?? "";
            this.m_request.FullName = this.Name ?? "";
            this.m_request.UserName = this.Provider == "Line" ? this.ProviderId : (this.Email ?? this.ProviderId);
            this.m_isAuthenticated = true;
            
            // If we just got authenticated, move to Shop Details step
            if (this.m_activeStep == 0)
            {
                this.m_activeStep = 1;
            }
        }
    }

    private bool CanProceedToNextStep()
    {
        return this.m_activeStep switch
        {
            0 => this.m_isAuthenticated && !string.IsNullOrWhiteSpace(this.m_request.Email),
            1 => !string.IsNullOrWhiteSpace(this.m_request.ShopName) &&
                 !string.IsNullOrWhiteSpace(this.m_request.Location),
            2 => this.m_request.Fleet.Count > 0,
            3 => true, // Plan selection validation handled in PlanSelectionStep
            _ => false
        };
    }

    private void NextStep()
    {
        if (this.m_activeStep < 3 && this.CanProceedToNextStep())
            this.m_activeStep++;
    }

    private void PreviousStep()
    {
        if (this.m_activeStep > 0)
            this.m_activeStep--;
    }

    private void GoToStep(int step)
    {
        // Only allow going back to previous steps
        if (step < this.m_activeStep)
            this.m_activeStep = step;
    }

    private void OnAuthenticatedChanged(bool isAuthenticated)
    {
        this.m_isAuthenticated = isAuthenticated;
        if (isAuthenticated)
        {
            // Auto-advance to next step after authentication
            this.m_activeStep = 1;
        }
    }

    private async Task CompleteOnboarding()
    {
        if (this.m_processing)
            return;

        this.m_processing = true;
        try
        {
            var result = await this.OnboardingService.OnboardAsync(this.m_request);

            if (result != null)
            {
                this.ShowSuccess(Localizer["AccountCreatedSuccess"]);
                this.NavigationManager.NavigateTo("/quick-start");
            }
            else
            {
                this.ShowError(Localizer["AccountCreationFailed"]);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Onboarding failed");
            this.ShowError(this.GetLocalizedText("OnboardingError", ex.Message));
        }
        finally
        {
            this.m_processing = false;
        }
    }
}