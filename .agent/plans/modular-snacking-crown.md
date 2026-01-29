# Template Designer Import Feature — Execution Plan

## Goal
Add "Import from Document" to the Template Designer. User uploads a PDF/image, Gemini Flash extracts blocks, and the designer canvas is populated.

## Task 1: Add `ExtractTemplateLayoutAsync` to `DocumentTemplateAiService.cs`
**File**: `src/MotoRent.Services/DocumentTemplateAiService.cs`

- Add method: `public async Task<DocumentLayout?> ExtractTemplateLayoutAsync(byte[] fileBytes, string mimeType, DocumentType documentType, CancellationToken cancellationToken = default)`
- Convert fileBytes to base64
- Build Gemini multimodal request (inlineData + text prompt) following existing `GetSuggestedClausesAsync` pattern
- System instruction describes all 8 block types, their JSON schemas, and available `{{Placeholder}}` tokens grouped by document type
- Use `responseMimeType = "application/json"` for structured output
- Parse response, strip markdown code fences, deserialize to `DocumentLayout`
- Return null on failure with error logging

## Task 2: Add Import UI to `DocumentTemplateDesigner.razor`
**File**: `src/MotoRent.Client/Pages/Settings/DocumentTemplateDesigner.razor`

- Add "Import from Document" button in header bar (before AI Suggester button)
- Add hidden `<InputFile>` accepting `.pdf,.jpg,.jpeg,.png,.webp`
- Add JS interop to trigger the hidden file input click (in `designer-splitter.js`)
- Add `HandleImportFileAsync` method:
  - Validate file size (max 10MB)
  - Read file to byte array
  - Call `ExtractTemplateLayoutAsync`
  - Merge returned sections into `m_layout.Sections`
  - Show success/error toast
- Add fields: `m_importFileInput`, `m_importing`

## Task 3: Localization
**Files**: `Resources/Pages/Settings/DocumentTemplateDesigner.resx` + `.th.resx`

| Key | English | Thai |
|-----|---------|------|
| `ImportFromDocument` | Import from Document | นำเข้าจากเอกสาร |
| `ImportProcessing` | Analyzing document... | กำลังวิเคราะห์เอกสาร... |
| `ImportSuccess` | Template blocks imported successfully | นำเข้าบล็อกแม่แบบสำเร็จ |
| `ImportFailed` | Failed to extract template from document | ไม่สามารถดึงแม่แบบจากเอกสารได้ |

## Execution Order
1. Task 1 (service method) — no dependencies
2. Task 2 (UI) — depends on Task 1
3. Task 3 (localization) — can be done alongside Task 2

## Verification
1. `dotnet build` — no errors
2. Navigate to `/settings/templates/designer`
3. Click "Import from Document" → file picker opens
4. Upload a rental agreement PDF/image
5. Verify blocks appear on canvas with correct types and `{{Placeholder}}` tokens
6. Edit imported blocks in property editor
7. Save template — persists correctly
8. Existing features (manual add, AI suggester, preview) still work
