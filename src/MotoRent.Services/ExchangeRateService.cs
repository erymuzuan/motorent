using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Services.ExchangeRateProviders;

namespace MotoRent.Services;

/// <summary>
/// Service for managing exchange rates.
/// Provides CRUD operations for rates plus currency conversion.
/// </summary>
public partial class ExchangeRateService(
    RentalDataContext context,
    IEnumerable<IExchangeRateProvider> providers)
{
    private RentalDataContext Context { get; } = context;
    private IEnumerable<IExchangeRateProvider> m_providers = providers;

    // Rate Retrieval

    /// <summary>
    /// Gets the current active exchange rate for a currency.
    /// Returns the most recently effective rate that hasn't expired.
    /// </summary>
    /// <param name="currency">Currency code (USD, EUR, etc.)</param>
    /// <returns>The current exchange rate or null if no rate is configured</returns>
    public async Task<ExchangeRate?> GetCurrentRateAsync(string currency)
    {
        var now = DateTimeOffset.Now;

        // Query for active rate for this currency
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<ExchangeRate>()
                .Where(r => r.Currency == currency && r.IsActive)
                .OrderByDescending(r => r.EffectiveDate),
            page: 1, size: 1, includeTotalRows: false);

        var rate = result.ItemCollection.FirstOrDefault();

        // Check if rate has expired
        if (rate != null && rate.ExpiresOn.HasValue && rate.ExpiresOn.Value <= now)
            return null;

        // Check if rate is effective yet
        if (rate != null && rate.EffectiveDate > now)
            return null;

        return rate;
    }

    /// <summary>
    /// Gets all current active exchange rates.
    /// Returns one rate per currency that is currently effective.
    /// </summary>
    /// <returns>List of current exchange rates ordered by currency</returns>
    public async Task<List<ExchangeRate>> GetAllCurrentRatesAsync()
    {
        var now = DateTimeOffset.Now;

        // Load all active rates
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<ExchangeRate>()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Currency),
            page: 1, size: 100, includeTotalRows: false);

        // Filter to only currently effective rates (not expired, already effective)
        // Group by currency and take most recent for each
        var currentRates = result.ItemCollection
            .Where(r => r.EffectiveDate <= now)
            .Where(r => !r.ExpiresOn.HasValue || r.ExpiresOn.Value > now)
            .GroupBy(r => r.Currency)
            .Select(g => g.OrderByDescending(r => r.EffectiveDate).First())
            .OrderBy(r => r.Currency)
            .ToList();

        return currentRates;
    }

    /// <summary>
    /// Gets rate history for a currency.
    /// Useful for audit trail and historical analysis.
    /// </summary>
    /// <param name="currency">Currency code</param>
    /// <param name="count">Number of historical rates to retrieve (default 10)</param>
    /// <returns>List of historical rates ordered by effective date descending</returns>
    public async Task<List<ExchangeRate>> GetRateHistoryAsync(string currency, int count = 10)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<ExchangeRate>()
                .Where(r => r.Currency == currency)
                .OrderByDescending(r => r.EffectiveDate),
            page: 1, size: count, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    // Rate Management

    /// <summary>
    /// Sets a new exchange rate for a currency.
    /// Deactivates any existing rate for the same currency.
    /// </summary>
    /// <param name="currency">Currency code (USD, EUR, etc.)</param>
    /// <param name="buyRate">THB amount for 1 unit of foreign currency</param>
    /// <param name="source">Rate source (Manual, API, Adjusted)</param>
    /// <param name="username">User setting the rate</param>
    /// <param name="apiRate">Original API rate if adjusted (optional)</param>
    /// <param name="notes">Notes about this rate (optional)</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> SetRateAsync(
        string currency,
        decimal buyRate,
        string source,
        string username,
        decimal? apiRate = null,
        string? notes = null)
    {
        var now = DateTimeOffset.Now;

        // Get current active rate for this currency (if any)
        var currentRate = await this.GetCurrentRateAsync(currency);

        // Create new rate
        var newRate = new ExchangeRate
        {
            Currency = currency,
            BuyRate = buyRate,
            Source = source,
            EffectiveDate = now,
            IsActive = true,
            ApiRate = apiRate,
            Notes = notes
        };

        using var session = this.Context.OpenSession(username);

        // Deactivate current rate if exists
        if (currentRate != null)
        {
            currentRate.IsActive = false;
            currentRate.ExpiresOn = now;
            session.Attach(currentRate);
        }

        session.Attach(newRate);
        return await session.SubmitChanges("SetExchangeRate");
    }

    // Currency Conversion

    /// <summary>
    /// Converts a foreign currency amount to THB.
    /// Returns conversion details including rate used and source for audit.
    /// </summary>
    /// <param name="currency">Source currency code</param>
    /// <param name="foreignAmount">Amount in foreign currency</param>
    /// <returns>Conversion result with THB amount and rate details, or null if no rate configured</returns>
    public async Task<ExchangeConversionResult?> ConvertToThbAsync(string currency, decimal foreignAmount)
    {
        // If already THB, return as-is
        if (currency == SupportedCurrencies.THB)
        {
            return new ExchangeConversionResult(foreignAmount, 1.0m, "Base", null);
        }

        // Get current rate for currency
        var rate = await this.GetCurrentRateAsync(currency);
        if (rate == null)
            return null;

        // Calculate THB amount
        var thbAmount = foreignAmount * rate.BuyRate;

        return new ExchangeConversionResult(thbAmount, rate.BuyRate, rate.Source, rate.ExchangeRateId);
    }

    // API Integration

    /// <summary>
    /// Fetches exchange rates from the Forex POS API.
    /// Currently a stub - implementation pending API documentation from the forex system team.
    /// </summary>
    /// <param name="username">User requesting the rate fetch</param>
    /// <returns>Result indicating success/failure and number of rates updated</returns>
    // TODO: Implement when Forex POS API endpoint, auth method, and response format are documented
    public Task<FetchRatesResult> FetchRatesFromApiAsync(string username)
    {
        // Stub implementation - API integration not yet configured
        return Task.FromResult(new FetchRatesResult(
            false,
            "Forex POS API integration not yet configured. Contact system administrator.",
            0));
    }
}

/// <summary>
/// Result of a currency conversion operation.
/// Contains the converted amount and audit information about the rate used.
/// </summary>
/// <param name="ThbAmount">Amount in THB (base currency)</param>
/// <param name="RateUsed">Exchange rate that was applied</param>
/// <param name="RateSource">Source of the rate (Manual, API, Adjusted, or Base for THB)</param>
/// <param name="ExchangeRateId">Reference to the ExchangeRate entity used (null for THB)</param>
public record ExchangeConversionResult(
    decimal ThbAmount,
    decimal RateUsed,
    string RateSource,
    int? ExchangeRateId);

/// <summary>
/// Result of an API rate fetch operation.
/// </summary>
/// <param name="Success">Whether the fetch was successful</param>
/// <param name="Message">Status message or error description</param>
/// <param name="RatesUpdated">Number of rates that were updated</param>
public record FetchRatesResult(
    bool Success,
    string Message,
    int RatesUpdated);
