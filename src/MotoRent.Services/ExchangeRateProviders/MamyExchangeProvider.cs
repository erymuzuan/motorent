using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.ExchangeRateProviders;

/// <summary>
/// Exchange rate provider for Mamy Exchange (mamyexchange.com).
/// Fetches rates via their WordPress REST API with denomination-specific pricing.
/// </summary>
public class MamyExchangeProvider : IExchangeRateProvider
{
    private const string API_URL = "https://mamyexchange.com/wp-json/currencyxrates/v1/rates";
    private const string CACHE_KEY = "MamyExchange_Rates";
    private static readonly TimeSpan s_cacheTtl = TimeSpan.FromMinutes(15);

    private readonly HttpClient m_httpClient;
    private readonly IMemoryCache m_cache;
    private readonly ILogger<MamyExchangeProvider> m_logger;
    private DateTimeOffset? m_lastUpdated;

    public string Name => "Mamy Exchange";
    public string Code => RateProviderCodes.MamyExchange;
    public string IconClass => "ti ti-currency-baht";
    public DateTimeOffset? LastUpdatedOn => m_lastUpdated;

    public MamyExchangeProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<MamyExchangeProvider> logger)
    {
        m_httpClient = httpClient;
        m_cache = cache;
        m_logger = logger;

        // Configure default headers
        m_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        m_httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<RawExchangeRate[]> GetRatesAsync(CancellationToken ct = default)
    {
        // Check cache first
        if (m_cache.TryGetValue(CACHE_KEY, out RawExchangeRate[]? cached) && cached != null)
        {
            m_logger.LogDebug("Returning cached Mamy Exchange rates");
            return cached;
        }

        try
        {
            m_logger.LogInformation("Fetching rates from Mamy Exchange API");

            var response = await m_httpClient.GetAsync(API_URL, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var rates = ParseRates(json);

            m_lastUpdated = DateTimeOffset.Now;

            // Cache the results
            m_cache.Set(CACHE_KEY, rates, s_cacheTtl);

            m_logger.LogInformation("Fetched {Count} rates from Mamy Exchange", rates.Length);
            return rates;
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to fetch rates from Mamy Exchange");
            return [];
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, API_URL);
            var response = await m_httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private RawExchangeRate[] ParseRates(string json)
    {
        var rates = new List<RawExchangeRate>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Expected format: array of rate objects
            // Each rate has: currency, buying, selling, and may have denomination info
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var rate = ParseRateElement(element);
                    if (rate != null)
                        rates.Add(rate);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Some APIs wrap in an object with "data" or "rates" property
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in data.EnumerateArray())
                    {
                        var rate = ParseRateElement(element);
                        if (rate != null)
                            rates.Add(rate);
                    }
                }
                else if (root.TryGetProperty("rates", out var ratesArray) && ratesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in ratesArray.EnumerateArray())
                    {
                        var rate = ParseRateElement(element);
                        if (rate != null)
                            rates.Add(rate);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            m_logger.LogError(ex, "Failed to parse Mamy Exchange JSON response");
        }

        return rates.ToArray();
    }

    private RawExchangeRate? ParseRateElement(JsonElement element)
    {
        try
        {
            // Try different property name conventions
            var currency = GetStringProperty(element, "currency", "Currency", "code", "Code");
            if (string.IsNullOrEmpty(currency))
                return null;

            var buying = GetDecimalProperty(element, "buying", "Buying", "buy", "Buy", "buyRate", "BuyRate");
            var selling = GetDecimalProperty(element, "selling", "Selling", "sell", "Sell", "sellRate", "SellRate");

            if (buying == 0 && selling == 0)
                return null;

            var rate = new RawExchangeRate
            {
                Provider = Code,
                Currency = currency.ToUpperInvariant(),
                Buying = buying,
                Selling = selling,
                UpdatedOn = DateTimeOffset.Now
            };

            // Try to parse denomination info if present
            if (element.TryGetProperty("denominations", out var denominations) && denominations.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in denominations.EnumerateArray())
                {
                    if (d.TryGetDecimal(out var denom))
                        rate.Denominations.Add(denom);
                }
            }

            // Try to parse group hint
            rate.GroupHint = GetStringProperty(element, "group", "Group", "groupName", "GroupName", "type", "Type");

            // Try to parse update time
            var updateStr = GetStringProperty(element, "updated", "Updated", "updatedAt", "UpdatedAt", "timestamp", "Timestamp");
            if (!string.IsNullOrEmpty(updateStr) && DateTimeOffset.TryParse(updateStr, out var updated))
            {
                rate.UpdatedOn = updated;
            }

            return rate;
        }
        catch (Exception ex)
        {
            m_logger.LogWarning(ex, "Failed to parse rate element from Mamy Exchange");
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }
        return null;
    }

    private static decimal GetDecimalProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
                    return d;
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out d))
                    return d;
            }
        }
        return 0;
    }
}
