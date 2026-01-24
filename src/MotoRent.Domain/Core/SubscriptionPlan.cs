namespace MotoRent.Domain.Core;

/// <summary>
/// Defines the subscription tiers for the MotoRent SaaS platform.
/// </summary>
public enum SubscriptionPlan
{
    /// <summary>
    /// Free tier for small shops or trial.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Professional tier with more features.
    /// </summary>
    Pro = 1,

    /// <summary>
    /// Ultra tier with full feature set and white-labeling.
    /// </summary>
    Ultra = 2
}
