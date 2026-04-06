using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

public class DocumentOcrService(
    RentalDataContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<DocumentOcrService> logger)
{
    private const string GEMINI_MODEL = "gemini-3-flash-preview";

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
        var model = GEMINI_MODEL;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured, returning empty extraction");
            return new ExtractedDocumentData { DocumentType = documentType };
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var base64Image = Convert.ToBase64String(imageBytes);
        var mimeType = GetMimeType(imagePath);

        var request = CreateGeminiRequest(base64Image, mimeType, documentType);

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", apiKey);
            httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiText = await response.ReadContentAsStringAsync();//().ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(geminiText);
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

    private static string GetSystemInstruction()
    {
        return """
            You are an expert document analyzer AI specializing in identity document OCR.
            Your task is to analyze images of identity documents and extract structured data with high precision.

            **CRITICAL RULES:**
            1. Extract text exactly as it appears on the document
            2. For dates, use ISO 8601 format (YYYY-MM-DD)
            3. If a field cannot be determined or is not visible, return null
            4. For gender, normalize to "M" or "F"
            5. For names, preserve the original casing and spelling
            6. If the document is blurry or partially visible, extract what is readable

            **DOCUMENT QUALITY:**
            - If the image quality is poor, still attempt extraction
            - Note any fields that are uncertain in the response
            """;
    }

    private static string GetExtractionPrompt(string documentType)
    {
        return documentType switch
        {
            "Passport" => "Extract all visible information from this passport document.",
            "NationalId" => "Extract all visible information from this national ID card.",
            "DrivingLicense" => "Extract all visible information from this driving license.",
            _ => "Extract any relevant identification information from this document."
        };
    }

    private static object GetResponseSchema(string documentType)
    {
        return documentType switch
        {
            "Passport" => new
            {
                type = "OBJECT",
                properties = new Dictionary<string, object>
                {
                    ["documentNumber"] = new { type = "STRING", description = "Passport number", nullable = true },
                    ["fullName"] = new { type = "STRING", description = "Full name as shown on passport", nullable = true },
                    ["givenName"] = new { type = "STRING", description = "First/given name", nullable = true },
                    ["surname"] = new { type = "STRING", description = "Last/family name", nullable = true },
                    ["nationality"] = new { type = "STRING", description = "Nationality/citizenship", nullable = true },
                    ["dateOfBirth"] = new { type = "STRING", description = "Date of birth in YYYY-MM-DD format", nullable = true },
                    ["gender"] = new { type = "STRING", description = "Gender: M or F", nullable = true },
                    ["placeOfBirth"] = new { type = "STRING", description = "Place of birth", nullable = true },
                    ["dateOfIssue"] = new { type = "STRING", description = "Issue date in YYYY-MM-DD format", nullable = true },
                    ["dateOfExpiry"] = new { type = "STRING", description = "Expiry date in YYYY-MM-DD format", nullable = true },
                    ["issuingAuthority"] = new { type = "STRING", description = "Issuing authority or country", nullable = true },
                    ["mrz"] = new { type = "STRING", description = "Machine readable zone if visible", nullable = true }
                }
            },
            "NationalId" => new
            {
                type = "OBJECT",
                properties = new Dictionary<string, object>
                {
                    ["documentNumber"] = new { type = "STRING", description = "National ID number", nullable = true },
                    ["fullName"] = new { type = "STRING", description = "Full name as shown", nullable = true },
                    ["givenName"] = new { type = "STRING", description = "First/given name", nullable = true },
                    ["surname"] = new { type = "STRING", description = "Last/family name", nullable = true },
                    ["nationality"] = new { type = "STRING", description = "Nationality if shown", nullable = true },
                    ["dateOfBirth"] = new { type = "STRING", description = "Date of birth in YYYY-MM-DD format", nullable = true },
                    ["gender"] = new { type = "STRING", description = "Gender: M or F", nullable = true },
                    ["address"] = new { type = "STRING", description = "Address if shown", nullable = true },
                    ["dateOfIssue"] = new { type = "STRING", description = "Issue date in YYYY-MM-DD format", nullable = true },
                    ["dateOfExpiry"] = new { type = "STRING", description = "Expiry date in YYYY-MM-DD format", nullable = true }
                }
            },
            "DrivingLicense" => new
            {
                type = "OBJECT",
                properties = new Dictionary<string, object>
                {
                    ["documentNumber"] = new { type = "STRING", description = "License number", nullable = true },
                    ["fullName"] = new { type = "STRING", description = "Full name as shown", nullable = true },
                    ["givenName"] = new { type = "STRING", description = "First/given name", nullable = true },
                    ["surname"] = new { type = "STRING", description = "Last/family name", nullable = true },
                    ["dateOfBirth"] = new { type = "STRING", description = "Date of birth in YYYY-MM-DD format", nullable = true },
                    ["address"] = new { type = "STRING", description = "Address if shown", nullable = true },
                    ["dateOfIssue"] = new { type = "STRING", description = "Issue date in YYYY-MM-DD format", nullable = true },
                    ["dateOfExpiry"] = new { type = "STRING", description = "Expiry date in YYYY-MM-DD format", nullable = true },
                    ["issuingCountry"] = new { type = "STRING", description = "Country that issued the license", nullable = true },
                    ["vehicleClasses"] = new { type = "ARRAY", items = new { type = "STRING" }, description = "Vehicle classes/categories", nullable = true },
                    ["restrictions"] = new { type = "STRING", description = "Any restrictions noted", nullable = true }
                }
            },
            _ => new
            {
                type = "OBJECT",
                properties = new Dictionary<string, object>
                {
                    ["documentNumber"] = new { type = "STRING", description = "Any ID number found", nullable = true },
                    ["fullName"] = new { type = "STRING", description = "Full name", nullable = true },
                    ["dateOfBirth"] = new { type = "STRING", description = "Date of birth in YYYY-MM-DD format", nullable = true },
                    ["dateOfExpiry"] = new { type = "STRING", description = "Expiry date in YYYY-MM-DD format", nullable = true }
                }
            }
        };
    }

    private static object CreateGeminiRequest(string base64Image, string mimeType, string documentType)
    {
        var userPrompt = GetExtractionPrompt(documentType);
        var systemInstruction = GetSystemInstruction();
        var responseSchema = GetResponseSchema(documentType);

        return new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Image
                            }
                        },
                        new { text = userPrompt }
                    }
                }
            },
            system_instruction = new
            {
                parts = new[]
                {
                    new { text = systemInstruction }
                }
            },
            generation_config = new
            {
                response_mime_type = "application/json",
                response_schema = responseSchema
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

// DTO for document upload result (used by Blazor components)
public class DocumentUploadResult
{
    public int? DocumentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExtractedDocumentData? ExtractedData { get; set; }
}
