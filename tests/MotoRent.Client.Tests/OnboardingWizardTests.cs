using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Configuration;
using Moq;
using MotoRent.Client.Pages.Onboarding;
using MotoRent.Client.Services;
using MotoRent.Client.Controls;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Settings;
using HashidsNet;
using Xunit;

namespace MotoRent.Client.Tests;

public class OnboardingWizardTests : BunitContext
{
    public OnboardingWizardTests()
    {
        // Mock all dependencies for MotoRentComponentBase
        Services.AddScoped(_ => new Mock<ILogger<OnboardingWizard>>().Object);
        Services.AddScoped(_ => new Mock<ILogger<MotoRentComponentBase>>().Object);
        
        // Provide a real RentalDataContext instance with null dependencies
        Services.AddScoped(_ => new RentalDataContext(null!, null, null, null));
        
        Services.AddScoped(_ => new Mock<IOnboardingService>().Object);
        Services.AddScoped(_ => new Mock<DialogService>(
            new Mock<IModalService>().Object, 
            new Mock<ToastService>().Object, 
            new Mock<IConfiguration>().Object).Object);
        Services.AddScoped(_ => new Mock<ToastService>().Object);
        Services.AddScoped(_ => new Mock<IRequestContext>().Object);
        Services.AddScoped(_ => new Mock<IHashids>().Object);
        Services.AddScoped(_ => new Mock<ISettingConfig>().Object);
        
        Services.AddLocalization();
        
        // Mock IStringLocalizer<OnboardingWizard>
        var localizerMock = new Mock<IStringLocalizer<OnboardingWizard>>();
        localizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        // Mock IStringLocalizer<CommonResources>
        var commonLocalizerMock = new Mock<IStringLocalizer<CommonResources>>();
        commonLocalizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton(commonLocalizerMock.Object);

        // Mock IStringLocalizer for steps
        var authStepLocalizerMock = new Mock<IStringLocalizer<MotoRent.Client.Pages.Onboarding.Steps.AuthStep>>();
        authStepLocalizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton(authStepLocalizerMock.Object);

        var shopStepLocalizerMock = new Mock<IStringLocalizer<MotoRent.Client.Pages.Onboarding.Steps.ShopDetailsStep>>();
        shopStepLocalizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton(shopStepLocalizerMock.Object);
    }

    [Fact]
    public void OnboardingWizard_ShouldStartAtStep1()
    {
        // Act
        var cut = Render<OnboardingWizard>();

        // Assert
        // Check if AuthStep is rendered (contains sign-in buttons)
        Assert.Contains("ContinueWithGoogle", cut.Markup);
    }

    [Fact]
    public void OnboardingWizard_ShouldAdvanceToStep2_WhenAuthenticatedViaQuery()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        var uri = "http://localhost/onboarding?provider=Google&id=google-123&email=test@example.com&name=Test+User";
        nav.NavigateTo(uri);

        // Act
        var cut = Render<OnboardingWizard>();

        // Assert
        // Should now show Shop Details step
        Assert.Contains("ShopName", cut.Markup);
    }
}