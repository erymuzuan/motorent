namespace MotoRent.Domain.Entities;

/// <summary>
/// Types of pricing rules for dynamic pricing
/// </summary>
public enum PricingRuleType
{
    /// <summary>Seasonal pricing (e.g., High Season Dec-Feb)</summary>
    Season,

    /// <summary>Event-based pricing (e.g., Songkran, CNY)</summary>
    Event,

    /// <summary>Day of week pricing (e.g., Weekend premium)</summary>
    DayOfWeek,

    /// <summary>Custom/manual pricing rule</summary>
    Custom
}
