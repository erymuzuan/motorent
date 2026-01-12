using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class DocumentOcrService(
    RentalDataContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<DocumentOcrService> logger)
{
    private RentalDataContext Context { get; } = context;
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<DocumentOcrService> Logger { get; } = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<ExtractedDocumentData> ExtractDocumentDataAsync(
        string imagePath,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured, returning empty extraction");
            return new ExtractedDocumentData { DocumentType = documentType };
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var base64Image = Convert.ToBase64String(imageBytes);
        var mimeType = GetMimeType(imagePath);

        var prompt = GetExtractionPrompt(documentType);
        var request = CreateGeminiRequest(base64Image, mimeType, prompt);

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
            var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "{}";

            this.Logger.LogInformation("Gemini OCR response received for {DocumentType}", documentType);

            return this.ParseExtractedData(rawJson, documentType);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to extract document data from Gemini API");
            return new ExtractedDocumentData { DocumentType = documentType, Error = ex.Message };
        }
    }

    public async Task<Document> SaveDocumentAsync(
        int renterId,
        string documentType,
        string imagePath,
        ExtractedDocumentData extractedData,
        string username)
    {
        var document = new Document
        {
            RenterId = renterId,
            DocumentType = documentType,
            ImagePath = imagePath,
            OcrRawJson = JsonSerializer.Serialize(extractedData, s_jsonOptions),
            ExtractedData = JsonSerializer.Serialize(extractedData, s_jsonOptions),
            UploadedOn = DateTimeOffset.UtcNow,
            IsVerified = false
        };

        using var session = this.Context.OpenSession(username);
        session.Attach(document);
        await session.SubmitChanges("DocumentUpload");

        return document;
    }

    public async Task<SubmitOperation> VerifyDocumentAsync(int documentId, bool isVerified, string username)
    {
        var document = await this.Context.LoadOneAsync<Document>(d => d.DocumentId == documentId);
        if (document == null)
        {
            return SubmitOperation.CreateFailure("Document not found");
        }

        document.IsVerified = isVerified;

        using var session = this.Context.OpenSession(username);
        session.Attach(document);
        return await session.SubmitChanges("DocumentVerification");
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId)
    {
        return await this.Context.LoadOneAsync<Document>(d => d.DocumentId == documentId);
    }

    public async Task<List<Document>> GetDocumentsByRenterAsync(int renterId)
    {
        var query = this.Context.CreateQuery<Document>()
            .Where(d => d.RenterId == renterId)
            .OrderByDescending(d => d.UploadedOn);

        var result = await this.Context.LoadAsync(query, page: 1, size: 50, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> DeleteDocumentAsync(Document document, string username)
    {
        // Delete the physical file
        if (!string.IsNullOrEmpty(document.ImagePath) && File.Exists(document.ImagePath))
        {
            try
            {
                File.Delete(document.ImagePath);
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "Failed to delete document file: {ImagePath}", document.ImagePath);
            }
        }

        using var session = this.Context.OpenSession(username);
        session.Delete(document);
        return await session.SubmitChanges("DocumentDelete");
    }

    private static string GetExtractionPrompt(string documentType)
    {
        var basePrompt = """
            Analyze this document image and extract the following information.
            Return ONLY a valid JSON object with the extracted data.
            If a field cannot be determined, use null.
            For dates, use ISO 8601 format (YYYY-MM-DD).
            """;

        return documentType switch
        {
            "Passport" => basePrompt + """

                Extract these fields for a Passport:
                {
                    "documentNumber": "passport number",
                    "fullName": "full name as shown",
                    "givenName": "first/given name",
                    "surname": "last/family name",
                    "nationality": "nationality/citizenship",
                    "dateOfBirth": "YYYY-MM-DD",
                    "gender": "M or F",
                    "placeOfBirth": "place of birth",
                    "dateOfIssue": "YYYY-MM-DD",
                    "dateOfExpiry": "YYYY-MM-DD",
                    "issuingAuthority": "issuing authority/country",
                    "mrz": "machine readable zone if visible"
                }
                """,

            "NationalId" => basePrompt + """

                Extract these fields for a National ID Card:
                {
                    "documentNumber": "ID number",
                    "fullName": "full name as shown",
                    "givenName": "first/given name",
                    "surname": "last/family name",
                    "nationality": "nationality if shown",
                    "dateOfBirth": "YYYY-MM-DD",
                    "gender": "M or F",
                    "address": "address if shown",
                    "dateOfIssue": "YYYY-MM-DD",
                    "dateOfExpiry": "YYYY-MM-DD"
                }
                """,

            "DrivingLicense" => basePrompt + """

                Extract these fields for a Driving License:
                {
                    "documentNumber": "license number",
                    "fullName": "full name as shown",
                    "givenName": "first/given name",
                    "surname": "last/family name",
                    "dateOfBirth": "YYYY-MM-DD",
                    "address": "address if shown",
                    "dateOfIssue": "YYYY-MM-DD",
                    "dateOfExpiry": "YYYY-MM-DD",
                    "issuingCountry": "country that issued the license",
                    "vehicleClasses": ["array of vehicle classes/categories"],
                    "restrictions": "any restrictions noted"
                }
                """,

            _ => basePrompt + """

                Extract any relevant identification information:
                {
                    "documentNumber": "any ID number found",
                    "fullName": "full name",
                    "dateOfBirth": "YYYY-MM-DD",
                    "dateOfExpiry": "YYYY-MM-DD"
                }
                """
        };
    }

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

    private ExtractedDocumentData ParseExtractedData(string rawJson, string documentType)
    {
        try
        {
            // Clean up the JSON response (remove markdown code blocks if present)
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
            rawJson = rawJson.Trim();

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            var data = new ExtractedDocumentData
            {
                DocumentType = documentType,
                RawJson = rawJson,
                DocumentNumber = GetStringProperty(root, "documentNumber"),
                FullName = GetStringProperty(root, "fullName"),
                GivenName = GetStringProperty(root, "givenName"),
                Surname = GetStringProperty(root, "surname"),
                Nationality = GetStringProperty(root, "nationality"),
                DateOfBirth = GetDateProperty(root, "dateOfBirth"),
                Gender = GetStringProperty(root, "gender"),
                PlaceOfBirth = GetStringProperty(root, "placeOfBirth"),
                Address = GetStringProperty(root, "address"),
                DateOfIssue = GetDateProperty(root, "dateOfIssue"),
                DateOfExpiry = GetDateProperty(root, "dateOfExpiry"),
                IssuingAuthority = GetStringProperty(root, "issuingAuthority"),
                IssuingCountry = GetStringProperty(root, "issuingCountry"),
                Mrz = GetStringProperty(root, "mrz"),
                Restrictions = GetStringProperty(root, "restrictions")
            };

            // Parse vehicle classes for driving licenses
            if (root.TryGetProperty("vehicleClasses", out var vehicleClasses) && vehicleClasses.ValueKind == JsonValueKind.Array)
            {
                data.VehicleClasses = vehicleClasses.EnumerateArray()
                    .Select(v => v.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            return data;
        }
        catch (JsonException ex)
        {
            this.Logger.LogWarning(ex, "Failed to parse Gemini response JSON");
            return new ExtractedDocumentData
            {
                DocumentType = documentType,
                RawJson = rawJson,
                Error = "Failed to parse OCR response"
            };
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static DateTimeOffset? GetDateProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var dateStr = prop.GetString();
            if (DateTimeOffset.TryParse(dateStr, out var date))
            {
                return date;
            }
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
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}

// DTOs for extracted document data
public class ExtractedDocumentData
{
    public string DocumentType { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public string? Error { get; set; }

    // Common fields
    public string? DocumentNumber { get; set; }
    public string? FullName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Nationality { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Address { get; set; }
    public DateTimeOffset? DateOfIssue { get; set; }
    public DateTimeOffset? DateOfExpiry { get; set; }

    // Passport specific
    public string? IssuingAuthority { get; set; }
    public string? Mrz { get; set; }

    // Driving License specific
    public string? IssuingCountry { get; set; }
    public List<string>? VehicleClasses { get; set; }
    public string? Restrictions { get; set; }
}

// Gemini API response models
internal class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

internal class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

internal class GeminiPart
{
    public string? Text { get; set; }
}

// DTO for document upload result (used by Blazor components)
public class DocumentUploadResult
{
    public int? DocumentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExtractedDocumentData? ExtractedData { get; set; }
}
