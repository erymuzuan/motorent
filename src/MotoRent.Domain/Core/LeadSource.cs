namespace MotoRent.Domain.Core;

/// <summary>
/// Defines the sources from which leads are captured.
/// </summary>
public enum LeadSource
{
    /// <summary>
    /// Lead submitted via the contact form.
    /// </summary>
    ContactForm = 0,

    /// <summary>
    /// Lead submitted from the pricing page.
    /// </summary>
    PricingPage = 1,

    /// <summary>
    /// Lead from manual entry by sales team.
    /// </summary>
    Manual = 2,

    /// <summary>
    /// Lead from referral program.
    /// </summary>
    Referral = 3,

    /// <summary>
    /// Lead from social media channels.
    /// </summary>
    SocialMedia = 4,

    /// <summary>
    /// Lead from trade shows or events.
    /// </summary>
    Event = 5
}
