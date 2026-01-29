# Upload Template PDF/Image → Gemini Extracts Blocks into Designer

## Goal
Add an "Import from Document" feature to the Template Designer. Users upload a PDF or scanned image of an existing agreement template, Gemini Flash analyzes it, and the designer is automatically populated with extracted blocks (headings, text paragraphs, tables, signature lines, dividers) with placeholders mapped to the system's `{{Token}}` format.

## Architecture

### Flow
1. User clicks "Import" button in designer header
2. File picker opens (accepts PDF, JPG, PNG, WebP)
3. File is read as base64 in the browser (same pattern as `DocumentUpload.razor`)
4. Base64 is sent to a new service method `ExtractTemplateLayoutAsync` on `DocumentTemplateAiService`
5. Gemini Flash receives the image + a structured prompt asking for a JSON `DocumentLayout`
6. The response is deserialized into `DocumentLayout` and merged into the designer canvas
7. User can review, edit, and save as usual

### Why `DocumentTemplateAiService` (not `DocumentOcrService`)
- `DocumentOcrService` is purpose-built for identity documents (passports, IDs) with a fixed schema
- `DocumentTemplateAiService` already handles Gemini calls for template-related AI features
- The new method fits naturally alongside `GetSuggestedClausesAsync`

### Gemini Prompt Strategy
Send the document image with a system instruction that:
- Describes all 8 block types and their JSON schemas
- Lists all available `{{Placeholder}}` tokens grouped by document type
- Asks Gemini to return a valid `DocumentLayout` JSON
- Instructs Gemini to map real data values to the closest placeholder token
- Handles multi-section documents (header, body, footer, terms)

## Files to Modify

| File | Change |
|------|--------|
| `DocumentTemplateAiService.cs` | Add `ExtractTemplateLayoutAsync(byte[] fileBytes, string mimeType, DocumentType type)` |
| `DocumentTemplateDesigner.razor` | Add Import button in header, file input, loading state, merge logic |
| `DocumentTemplateDesigner.resx` | New keys: `ImportFromDocument`, `ImportProcessing`, `ImportSuccess`, `ImportFailed` |
| `DocumentTemplateDesigner.th.resx` | Thai translations for above |

## Task 1: Add `ExtractTemplateLayoutAsync` to `DocumentTemplateAiService.cs`

New method signature:
```csharp
public async Task<DocumentLayout?> ExtractTemplateLayoutAsync(
    byte[] fileBytes,
    string mimeType,
    DocumentType documentType,
    CancellationToken cancellationToken = default)
```

Implementation:
- Convert `fileBytes` to base64
- Build Gemini request with `inlineData` part (base64 + mimeType) + text prompt part
- System instruction describes block types, JSON schema, placeholder tokens
- `generationConfig.responseMimeType = "application/json"` for structured output
- Response schema matches `DocumentLayout` structure
- Parse response, clean up markdown code fences, deserialize to `DocumentLayout`
- Return null on failure (log error)

Prompt template (key parts):
```
You are analyzing a document template image for a motorbike rental business.
Extract the visual structure into a JSON layout with sections and blocks.

Available block types:
- "heading": { "$type": "heading", "Content": "...", "Level": 1-6, "HorizontalAlignment": "Center", "IsBold": true }
- "text": { "$type": "text", "Content": "...", "HorizontalAlignment": "Left", "IsBold": false }
- "table": { "$type": "table", "BindingPath": "...", "Columns": [{"Header": "...", "BindingPath": "..."}] }
- "image": { "$type": "image", "BindingPath": "Organization.Logo", "Width": 200 }
- "divider": { "$type": "divider", "Thickness": 1, "Color": "#000000" }
- "signature": { "$type": "signature", "Label": "..." }
- "two-columns": { "$type": "two-columns", "LeftColumn": [...], "RightColumn": [...] }
- "spacer": { "$type": "spacer", "Height": 20 }

Available placeholders (use {{Token}} format in text content):
[list tokens by document type from PlaceholderPicker groups]

Rules:
- Replace actual names/dates/values with matching {{Placeholder}} tokens
- Group content into logical sections (Header, Terms, Footer, etc.)
- Preserve the document's visual hierarchy (headings, paragraphs, tables)
- For tables, infer column structure and map to data binding paths
- For signature areas, use SignatureBlock with descriptive labels
- For horizontal lines, use DividerBlock
- Return valid JSON matching the DocumentLayout schema
```

### Gemini multimodal request shape (matching existing codebase pattern):
```csharp
var request = new
{
    contents = new[]
    {
        new
        {
            parts = new object[]
            {
                new { inlineData = new { mimeType, data = base64 } },
                new { text = prompt }
            }
        }
    },
    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
    generationConfig = new
    {
        temperature = 0.3,
        maxOutputTokens = 8192,
        responseMimeType = "application/json"
    }
};
```

## Task 2: Add Import UI to `DocumentTemplateDesigner.razor`

### Header button (in btn-list, before AI Suggester):
```razor
<button type="button" class="btn btn-outline-secondary" @onclick="this.TriggerImport"
        disabled="@this.m_importing">
    @if (this.m_importing)
    {
        <span class="spinner-border spinner-border-sm me-1"></span>
    }
    else
    {
        <i class="ti ti-file-upload me-1"></i>
    }
    @this.Localizer["ImportFromDocument"]
</button>
<InputFile @ref="m_importFileInput" OnChange="this.HandleImportFileAsync"
           accept=".pdf,.jpg,.jpeg,.png,.webp" style="display:none" />
```

### Code-behind additions:
```csharp
private InputFile? m_importFileInput;
private bool m_importing;

private async Task TriggerImport()
{
    // Trigger the hidden InputFile via JS interop
    // (Blazor InputFile doesn't have a Click() method - need JS)
}

private async Task HandleImportFileAsync(InputFileChangeEventArgs e)
{
    var file = e.File;
    if (file.Size > 10 * 1024 * 1024) { ShowError("File too large (max 10MB)"); return; }

    m_importing = true;
    try
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var layout = await AiService.ExtractTemplateLayoutAsync(
            ms.ToArray(), file.ContentType, m_template.Type);

        if (layout?.Sections.Any() == true)
        {
            // Merge: append extracted sections to existing layout
            foreach (var section in layout.Sections)
                m_layout.Sections.Add(section);

            ShowSuccess(Localizer["ImportSuccess"]);
        }
        else
        {
            ShowError(Localizer["ImportFailed"]);
        }
    }
    finally
    {
        m_importing = false;
    }
}
```

### JS helper to click the hidden file input:
Add to `designer-splitter.js`:
```js
export function triggerFileInput(inputElement) {
    inputElement?.click();
}
```

## Task 3: Localization

### DocumentTemplateDesigner.resx (English)
| Key | Value |
|-----|-------|
| `ImportFromDocument` | `Import from Document` |
| `ImportProcessing` | `Analyzing document...` |
| `ImportSuccess` | `Template blocks imported successfully` |
| `ImportFailed` | `Failed to extract template from document` |

### DocumentTemplateDesigner.th.resx (Thai)
| Key | Value |
|-----|-------|
| `ImportFromDocument` | `นำเข้าจากเอกสาร` |
| `ImportProcessing` | `กำลังวิเคราะห์เอกสาร...` |
| `ImportSuccess` | `นำเข้าบล็อกแม่แบบสำเร็จ` |
| `ImportFailed` | `ไม่สามารถดึงแม่แบบจากเอกสารได้` |

## Verification
1. `dotnet build` — no compilation errors
2. Navigate to `/settings/templates/designer`
3. Click "Import from Document" → file picker opens
4. Upload a PDF/image of a rental agreement
5. Gemini processes → blocks appear on canvas
6. Verify headings, text blocks, tables, signatures extracted correctly
7. Verify `{{Placeholder}}` tokens are used in text content
8. Edit imported blocks in property editor — all work
9. Save template — persists correctly
10. Existing features (manual block add, AI suggester, preview) still work
