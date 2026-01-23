namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for exchange rate provider codes.
/// Identifies the source of exchange rates for tracking and display.
/// </summary>
public static class RateProviderCodes
{
    /// <summary>
    /// Rate was entered manually by staff
    /// </summary>
    public const string Manual = "Manual";

    /// <summary>
    /// Rate from Mamy Exchange API (mamyexchange.com)
    /// </summary>
    public const string MamyExchange = "MamyExchange";

    /// <summary>
    /// Rate from Super Rich Thailand API (superrichthailand.com)
    /// </summary>
    public const string SuperRich = "SuperRich";

    /// <summary>
    /// All available provider codes
    /// </summary>
    public static readonly string[] All = [Manual, MamyExchange, SuperRich];

    /// <summary>
    /// Gets the human-readable display name for a provider code
    /// </summary>
    public static string GetDisplayName(string code) => code switch
    {
        MamyExchange => "Mamy Exchange",
        SuperRich => "Super Rich",
        _ => "Manual"
    };

    /// <summary>
    /// Gets the Tabler icon class for a provider
    /// </summary>
    public static string GetIconClass(string code) => code switch
    {
        MamyExchange => "ti ti-currency-baht",
        SuperRich => "ti ti-crown",
        _ => "ti ti-edit"
    };
}
