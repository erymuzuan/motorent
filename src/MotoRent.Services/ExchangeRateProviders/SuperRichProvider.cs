using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.ExchangeRateProviders;

/// <summary>
/// Exchange rate provider for Super Rich Thailand (superrichthailand.com).
/// Fetches rates via their public API with 30-minute cache TTL.
/// </summary>
public class SuperRichProvider : IExchangeRateProvider
{
    private const string API_URL = "https://www.superrichthailand.com/web/api/v1/rates";
    private const string CACHE_KEY = "SuperRich_Rates";
    private static readonly TimeSpan s_cacheTtl = TimeSpan.FromMinutes(30);

    private readonly HttpClient m_httpClient;
    private readonly IMemoryCache m_cache;
    private readonly ILogger<SuperRichProvider> m_logger;
    private DateTimeOffset? m_lastUpdated;

    public string Name => "Super Rich";
    public string Code => RateProviderCodes.SuperRich;
    public string IconClass => "ti ti-crown";
    public DateTimeOffset? LastUpdatedOn => m_lastUpdated;

    public SuperRichProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<SuperRichProvider> logger)
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
            m_logger.LogDebug("Returning cached Super Rich rates");
            return cached;
        }

        try
        {
            m_logger.LogInformation("Fetching rates from Super Rich API");

            var response = await m_httpClient.GetAsync(API_URL, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var rates = ParseRates(json);

            m_lastUpdated = DateTimeOffset.Now;

            // Cache the results with longer TTL
            m_cache.Set(CACHE_KEY, rates, s_cacheTtl);

            m_logger.LogInformation("Fetched {Count} rates from Super Rich", rates.Length);
            return rates;
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to fetch rates from Super Rich");
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

            // Super Rich API typically returns: { "data": { "buy": [...], "sell": [...] } }
            // or similar structure with currency arrays
            if (root.TryGetProperty("data", out var data))
            {
                rates.AddRange(ParseDataSection(data));
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
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var rate = ParseRateElement(element);
                    if (rate != null)
                        rates.Add(rate);
                }
            }
        }
        catch (JsonException ex)
        {
            m_logger.LogError(ex, "Failed to parse Super Rich JSON response");
        }

        return rates.ToArray();
    }

    private IEnumerable<RawExchangeRate> ParseDataSection(JsonElement data)
    {
        var rateDict = new Dictionary<string, RawExchangeRate>();

        // Handle buy rates
        if (data.TryGetProperty("buy", out var buyArray) && buyArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in buyArray.EnumerateArray())
            {
                var currency = GetStringProperty(element, "currency_code", "currencyCode", "currency", "code");
                if (string.IsNullOrEmpty(currency))
                    continue;

                currency = currency.ToUpperInvariant();
                var rate = GetDecimalProperty(element, "rate", "buying", "buy");

                if (!rateDict.TryGetValue(currency, out var rawRate))
                {
                    rawRate = new RawExchangeRate
                    {
                        Provider = Code,
                        Currency = currency,
                        UpdatedOn = DateTimeOffset.Now
                    };
                    rateDict[currency] = rawRate;
                }

                rawRate.Buying = rate;
                ParseDenominationInfo(element, rawRate);
            }
        }

        // Handle sell rates
        if (data.TryGetProperty("sell", out var sellArray) && sellArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in sellArray.EnumerateArray())
            {
                var currency = GetStringProperty(element, "currency_code", "currencyCode", "currency", "code");
                if (string.IsNullOrEmpty(currency))
                    continue;

                currency = currency.ToUpperInvariant();
                var rate = GetDecimalProperty(element, "rate", "selling", "sell");

                if (!rateDict.TryGetValue(currency, out var rawRate))
                {
                    rawRate = new RawExchangeRate
                    {
                        Provider = Code,
                        Currency = currency,
                        UpdatedOn = DateTimeOffset.Now
                    };
                    rateDict[currency] = rawRate;
                }

                rawRate.Selling = rate;
                ParseDenominationInfo(element, rawRate);
            }
        }

        // Handle combined format
        if (data.TryGetProperty("currencies", out var currencies) && currencies.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in currencies.EnumerateArray())
            {
                var rate = ParseRateElement(element);
                if (rate != null)
                {
                    if (rateDict.TryGetValue(rate.Currency, out var existing))
                    {
                        // Merge with existing
                        if (rate.Buying > 0) existing.Buying = rate.Buying;
                        if (rate.Selling > 0) existing.Selling = rate.Selling;
                    }
                    else
                    {
                        rateDict[rate.Currency] = rate;
                    }
                }
            }
        }

        return rateDict.Values;
    }

    private RawExchangeRate? ParseRateElement(JsonElement element)
    {
        try
        {
            var currency = GetStringProperty(element, "currency_code", "currencyCode", "currency", "code");
            if (string.IsNullOrEmpty(currency))
                return null;

            var buying = GetDecimalProperty(element, "buying", "buy", "buyRate", "buy_rate");
            var selling = GetDecimalProperty(element, "selling", "sell", "sellRate", "sell_rate");

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

            ParseDenominationInfo(element, rate);

            // Try to parse update time
            var updateStr = GetStringProperty(element, "updated_at", "updatedAt", "updated", "timestamp");
            if (!string.IsNullOrEmpty(updateStr) && DateTimeOffset.TryParse(updateStr, out var updated))
            {
                rate.UpdatedOn = updated;
            }

            return rate;
        }
        catch (Exception ex)
        {
            m_logger.LogWarning(ex, "Failed to parse rate element from Super Rich");
            return null;
        }
    }

    private static void ParseDenominationInfo(JsonElement element, RawExchangeRate rate)
    {
        // Try to parse denomination range (e.g., "1-20" or "50-100")
        var denomRange = GetStringProperty(element, "denomination", "denom", "bill_type", "billType");
        if (!string.IsNullOrEmpty(denomRange))
        {
            rate.GroupHint = denomRange;

            // Try to parse numeric range
            var parts = denomRange.Split('-', ',');
            foreach (var part in parts)
            {
                if (decimal.TryParse(part.Trim(), out var d))
                {
                    rate.Denominations.Add(d);
                }
            }
        }

        // Also try explicit denominations array
        if (element.TryGetProperty("denominations", out var denominations) && denominations.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in denominations.EnumerateArray())
            {
                if (d.TryGetDecimal(out var denom) && !rate.Denominations.Contains(denom))
                    rate.Denominations.Add(denom);
            }
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
