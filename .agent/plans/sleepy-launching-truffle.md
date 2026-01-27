# Plan: Improve DocumentOcrService Gemini Request

## Objective
Improve the Gemini request crafting in `DocumentOcrService.cs` based on patterns from rx-erp's `GeminiService.cs` to obtain better OCR results with Gemini Flash 3 preview.

## Key Improvements from rx-erp GeminiService

The rx-erp implementation uses several advanced Gemini API features that the current DocumentOcrService lacks:

| Feature | Current | rx-erp Pattern |
|---------|---------|----------------|
| Model | `MotoConfig.GeminiModel` (unknown) | `gemini-3-flash-preview` |
| System instruction | Embedded in prompt | Separate `system_instruction` field |
| Response schema | None (free-form JSON) | Typed `response_schema` with property definitions |
| Config naming | camelCase (`generationConfig`) | snake_case (`generation_config`) |
| Temperature | 0.1 | Not specified (default) |

## Implementation Changes

### File: `src/MotoRent.Services/DocumentOcrService.cs`

#### 1. Update model constant
```csharp
private const string GEMINI_3_FLASH_PREVIEW = "gemini-3-flash-preview";
```

#### 2. Restructure `CreateGeminiRequest` method
- Add `system_instruction` for base extraction rules
- Add `response_schema` for typed JSON output
- Use snake_case for Gemini API fields

#### 3. Update prompts to separate system vs user instructions
- **System instruction**: Expert document analyzer role, date formats, null handling rules
- **User prompt**: Document-type specific extraction request

### Detailed Request Structure

```csharp
var requestBody = new
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
        response_schema = GetResponseSchema(documentType)
    }
};
```

### Response Schema per Document Type

Define typed schemas for each document type:

**Passport Schema:**
```csharp
new
{
    type = "OBJECT",
    properties = new
    {
        documentNumber = new { type = "STRING", description = "Passport number" },
        fullName = new { type = "STRING", description = "Full name as shown" },
        givenName = new { type = "STRING", description = "First/given name" },
        surname = new { type = "STRING", description = "Last/family name" },
        nationality = new { type = "STRING", description = "Nationality/citizenship" },
        dateOfBirth = new { type = "STRING", description = "Date of birth in YYYY-MM-DD" },
        gender = new { type = "STRING", description = "M or F" },
        placeOfBirth = new { type = "STRING", description = "Place of birth" },
        dateOfIssue = new { type = "STRING", description = "Issue date in YYYY-MM-DD" },
        dateOfExpiry = new { type = "STRING", description = "Expiry date in YYYY-MM-DD" },
        issuingAuthority = new { type = "STRING", description = "Issuing authority/country" },
        mrz = new { type = "STRING", description = "Machine readable zone if visible" }
    }
}
```

Similar schemas for `NationalId` and `DrivingLicense`.

### System Instruction Template

```text
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
```

## Files to Modify

1. `src/MotoRent.Services/DocumentOcrService.cs`
   - Add model constant
   - Refactor `CreateGeminiRequest` to use `system_instruction` and `response_schema`
   - Add `GetResponseSchema` method for typed schemas
   - Update `GetExtractionPrompt` to return only user-facing prompt
   - Add `GetSystemInstruction` method

## Verification

1. Build the solution: `dotnet build`
2. Test OCR with sample documents (passport, national ID, driving license)
3. Verify JSON response matches expected schema
4. Check that null fields are properly handled

## Risk Assessment

- **Low risk**: Changes are isolated to the OCR service
- **Backward compatible**: `ExtractedDocumentData` class remains unchanged
- **Fallback**: If new API format fails, can revert to current approach
