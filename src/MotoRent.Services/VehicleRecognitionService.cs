using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Lookups;

namespace MotoRent.Services;

/// <summary>
/// Service for Gemini-powered vehicle image recognition.
/// Analyzes vehicle photos and matches against the lookup database.
/// </summary>
public class VehicleRecognitionService(
    VehicleLookupService lookupService,
    IHttpClientFactory httpClientFactory,
    ILogger<VehicleRecognitionService> logger)
{
    private VehicleLookupService LookupService { get; } = lookupService;
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<VehicleRecognitionService> Logger { get; } = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Analyzes a vehicle image and extracts identification data.
    /// </summary>
    public async Task<VehicleRecognitionResult> RecognizeVehicleAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);
            var mimeType = GetMimeType(imagePath);

            return await this.RecognizeVehicleFromBase64Async(base64Image, mimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to read vehicle image: {ImagePath}", imagePath);
            return new VehicleRecognitionResult { Error = $"Failed to read image: {ex.Message}" };
        }
    }

    
    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models";
    /// <summary>
    /// Analyzes a vehicle image from base64 data.
    /// </summary>
    public async Task<VehicleRecognitionResult> RecognizeVehicleFromBase64Async(
        string base64Image,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured, returning empty recognition result");
            return new VehicleRecognitionResult { Error = "Gemini API key not configured" };
        }

        var prompt = GetRecognitionPrompt();
        var request = CreateGeminiRequest(base64Image, mimeType, prompt);

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"{BASE_URL}/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(s_jsonOptions, cancellationToken);
            var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "{}";

            this.Logger.LogInformation("Gemini vehicle recognition response received");

            return await this.ParseAndMatchResultAsync(rawJson, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            this.Logger.LogError(ex, "Gemini API request failed");
            return new VehicleRecognitionResult { Error = $"API request failed: {ex.Message}" };
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to recognize vehicle from image");
            return new VehicleRecognitionResult { Error = $"Recognition failed: {ex.Message}" };
        }
    }

    private static string GetRecognitionPrompt() => """
        Analyze this vehicle image and extract identification information.
        Return ONLY a valid JSON object with the extracted data.
        If a field cannot be determined, use null.

        Focus on vehicles commonly found in Thailand:
        - Motorbikes: Honda (Click, PCX, Wave, Scoopy, ADV), Yamaha (NMAX, Aerox, Fino, Grand Filano), Vespa, Suzuki, GPX
        - Cars: Toyota (Yaris, Vios, Camry, Fortuner), Honda (City, Civic, HR-V), Mazda, Nissan
        - Vans: Toyota HiAce, Hyundai H1
        - Boats: Speedboats, longtail boats

        Extract these fields:
        {
            "vehicleType": "Motorbike|Car|Van|Boat|JetSki",
            "make": "manufacturer/brand name (e.g., Honda, Toyota, Yamaha)",
            "model": "model name (e.g., Click 125, PCX 160, Camry)",
            "year": estimated year (integer) or null,
            "color": "primary color (e.g., Red, Blue, White, Black)",
            "licensePlate": "Thai license plate if visible (e.g., กก 1234, 1กก 1234, กท 5555)",
            "engineCC": engine displacement in CC for motorbikes (integer) or null,
            "segment": "SmallSedan|BigSedan|SUV|Pickup|Hatchback|Minivan|Luxury" for cars only or null,
            "seatCount": number of seats for cars/vans (integer) or null,
            "confidence": 0.0 to 1.0 confidence score
        }
        """;

    private static object CreateGeminiRequest(string base64Image, string mimeType, string prompt)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Image
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };
    }

    private async Task<VehicleRecognitionResult> ParseAndMatchResultAsync(
        string rawJson,
        CancellationToken cancellationToken)
    {
        var result = new VehicleRecognitionResult { RawJson = rawJson };

        try
        {
            // Clean up the JSON response (remove markdown code blocks if present)
            rawJson = CleanJsonResponse(rawJson);

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // Parse vehicle type
            var vehicleTypeStr = GetStringProperty(root, "vehicleType");
            if (!string.IsNullOrEmpty(vehicleTypeStr) && Enum.TryParse<VehicleType>(vehicleTypeStr, true, out var vehicleType))
            {
                result.VehicleType = vehicleType;
            }

            // Parse basic fields
            result.RecognizedMake = GetStringProperty(root, "make");
            result.RecognizedModel = GetStringProperty(root, "model");
            result.Color = GetStringProperty(root, "color");
            result.LicensePlate = GetStringProperty(root, "licensePlate");

            // Parse numeric fields
            if (root.TryGetProperty("year", out var yearProp) && yearProp.ValueKind == JsonValueKind.Number)
            {
                result.Year = yearProp.GetInt32();
            }

            if (root.TryGetProperty("engineCC", out var engineCCProp) && engineCCProp.ValueKind == JsonValueKind.Number)
            {
                result.EngineCC = engineCCProp.GetInt32();
            }

            if (root.TryGetProperty("confidence", out var confidenceProp) && confidenceProp.ValueKind == JsonValueKind.Number)
            {
                result.Confidence = confidenceProp.GetDecimal();
            }

            // Parse car segment
            var segmentStr = GetStringProperty(root, "segment");
            if (!string.IsNullOrEmpty(segmentStr) && Enum.TryParse<CarSegment>(segmentStr, true, out var segment))
            {
                result.Segment = segment;
            }

            // Try to match against lookup database
            if (!string.IsNullOrEmpty(result.RecognizedMake) && !string.IsNullOrEmpty(result.RecognizedModel))
            {
                result.MatchedVehicleModel = await this.LookupService.MatchAsync(
                    result.RecognizedMake,
                    result.RecognizedModel);
            }
        }
        catch (JsonException ex)
        {
            this.Logger.LogWarning(ex, "Failed to parse Gemini vehicle recognition response");
            result.Error = "Failed to parse recognition response";
        }

        return result;
    }

    private static string CleanJsonResponse(string rawJson)
    {
        rawJson = rawJson.Trim();

        if (rawJson.StartsWith("```json"))
        {
            rawJson = rawJson[7..];
        }
        if (rawJson.StartsWith("```"))
        {
            rawJson = rawJson[3..];
        }
        if (rawJson.EndsWith("```"))
        {
            rawJson = rawJson[..^3];
        }

        return rawJson.Trim();
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}

// Internal Gemini API response models
internal class GeminiApiResponse
{
    public List<GeminiApiCandidate>? Candidates { get; set; }
}

internal class GeminiApiCandidate
{
    public GeminiApiContent? Content { get; set; }
}

internal class GeminiApiContent
{
    public List<GeminiApiPart>? Parts { get; set; }
}

internal class GeminiApiPart
{
    public string? Text { get; set; }
}
