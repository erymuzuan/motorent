namespace MotoRent.Domain.Entities;

/// <summary>
/// Provides denomination definitions for supported currencies.
/// Used for cash counting during till operations (opening float, cash drops, closing).
/// </summary>
public static class CurrencyDenominations
{
    /// <summary>
    /// Thai Baht denominations (notes and coins).
    /// </summary>
    private static readonly decimal[] s_thbDenominations = [1000, 500, 100, 50, 20, 10, 5, 2, 1];

    /// <summary>
    /// US Dollar denominations (notes only - coins rarely used in cash handling).
    /// </summary>
    private static readonly decimal[] s_usdDenominations = [100, 50, 20, 10, 5, 1];

    /// <summary>
    /// Euro denominations (notes only - coins rarely used in cash handling).
    /// </summary>
    private static readonly decimal[] s_eurDenominations = [500, 200, 100, 50, 20, 10, 5];

    /// <summary>
    /// Chinese Yuan denominations (notes only).
    /// </summary>
    private static readonly decimal[] s_cnyDenominations = [100, 50, 20, 10, 5, 1];

    /// <summary>
    /// Gets the denominations for a currency.
    /// Returns denominations in descending order (largest first).
    /// </summary>
    /// <param name="currency">Currency code (THB, USD, EUR, CNY)</param>
    /// <returns>Array of denominations for the currency</returns>
    public static decimal[] GetDenominations(string currency) => currency switch
    {
        SupportedCurrencies.THB => s_thbDenominations,
        SupportedCurrencies.USD => s_usdDenominations,
        SupportedCurrencies.EUR => s_eurDenominations,
        SupportedCurrencies.CNY => s_cnyDenominations,
        _ => []
    };

    /// <summary>
    /// Gets the currency symbol for display.
    /// </summary>
    /// <param name="currency">Currency code (THB, USD, EUR, CNY)</param>
    /// <returns>Currency symbol</returns>
    public static string GetCurrencySymbol(string currency) => currency switch
    {
        SupportedCurrencies.THB => "\u0E3F", // Thai Baht symbol
        SupportedCurrencies.USD => "$",
        SupportedCurrencies.EUR => "\u20AC", // Euro symbol
        SupportedCurrencies.CNY => "\u00A5", // Yuan symbol
        SupportedCurrencies.GBP => "\u00A3", // Pound symbol
        SupportedCurrencies.JPY => "\u00A5", // Yen symbol (same as Yuan)
        SupportedCurrencies.AUD => "A$",
        SupportedCurrencies.RUB => "\u20BD", // Ruble symbol
        _ => currency
    };

    /// <summary>
    /// Gets the list of currencies that support denomination counting.
    /// These are the primary currencies used for cash handling in Thai tourist areas.
    /// </summary>
    public static readonly string[] CurrenciesWithDenominations =
    [
        SupportedCurrencies.THB,
        SupportedCurrencies.USD,
        SupportedCurrencies.EUR,
        SupportedCurrencies.CNY
    ];
}
