using MotoRent.Domain.Entities;

namespace MotoRent.Services.ExchangeRateProviders;

/// <summary>
/// Interface for exchange rate providers.
/// Providers fetch rates from external APIs (e.g., MamyExchange, SuperRich).
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Human-readable provider name for display
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Provider code constant (matches RateProviderCodes)
    /// </summary>
    string Code { get; }

    /// <summary>
    /// CSS class for provider icon (Tabler icons)
    /// </summary>
    string IconClass { get; }

    /// <summary>
    /// When rates were last successfully fetched from this provider
    /// </summary>
    DateTimeOffset? LastUpdatedOn { get; }

    /// <summary>
    /// Fetches current exchange rates from the provider API.
    /// Returns raw rates with denomination grouping hints.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Array of raw exchange rates</returns>
    Task<RawExchangeRate[]> GetRatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if the provider API is currently available.
    /// Useful for displaying provider status in UI.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if provider is reachable and responding</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
