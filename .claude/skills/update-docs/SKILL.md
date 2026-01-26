---
name: update-docs
description: Update MotoRent documentation - translate to Thai, update meta.json, and optionally generate styled HTML using frontend-design skill.
---

# Update Documentation Skill

This skill manages the MotoRent documentation system. It handles:
1. Translating English documentation to Thai using Gemini API
2. Tracking document versions and translation status via meta.json
3. Generating beautifully styled HTML documentation using the frontend-design skill

## When to activate

Trigger on requests like:
- "update docs", "update documentation"
- "translate guides to Thai"
- "generate HTML docs"
- "check translation status"
- "sync documentation"

## Documentation Structure

```
src/MotoRent.Server/wwwroot/
├── user.guides/           # English source documentation
│   ├── manifest.json      # Document metadata (title, order)
│   ├── meta.json          # Version tracking and translation status
│   ├── 01-orgadmin-quickstart.md
│   ├── ... (12 guides)
│   └── images/
├── user.guides.th/        # Thai translations
│   ├── manifest.json      # Thai titles
│   ├── ... (translated .md files)
│   └── images/ → symlink to user.guides/images
├── docs/en/               # Generated HTML (English)
└── docs/th/               # Generated HTML (Thai)
```

## Workflow Options

### 1. Check Translation Status

```bash
# Quick status check via API (requires auth)
curl -X GET "https://localhost:5001/api/help/translations/status"

# Or read meta.json directly
cat src/MotoRent.Server/wwwroot/user.guides/meta.json
```

Look for documents where:
- `translations.th.status` is "pending"
- `translations.th.sourceHash` doesn't match `contentHash` (outdated)

### 2. Translate Documents to Thai

**Option A: Via API (automated)**
```bash
# Translate single document
curl -X POST "https://localhost:5001/api/help/translations/translate?fileName=01-orgadmin-quickstart.md"

# Translate all pending
curl -X POST "https://localhost:5001/api/help/translations/translate-all"
```

**Option B: Manual translation with Claude**
When API is unavailable or for higher quality translations:

1. Read the English source file
2. Apply translation rules (see below)
3. Write to `user.guides.th/` with same filename
4. Update meta.json with new sourceHash

### 3. Generate Styled HTML (frontend-design integration)

For beautiful, production-ready HTML documentation:

1. **Read markdown content** from user.guides/ or user.guides.th/
2. **Invoke frontend-design skill** with these requirements:
   - Use Tabler CSS framework classes
   - Professional typography
   - Syntax-highlighted code blocks
   - Beautiful tables with alternating rows
   - Callout boxes for tips/warnings/notes
   - Responsive images with captions
   - Navigation breadcrumbs
   - Table of contents sidebar
   - Print-friendly layout
3. **Write HTML files** to docs/en/ or docs/th/

Example prompt for frontend-design:
```
Convert this markdown documentation to beautifully styled HTML:
- Use Tabler CSS classes for styling
- Add syntax highlighting to code blocks
- Create callout boxes for blockquotes (tips, warnings)
- Add a floating table of contents
- Include print stylesheet
- Make images responsive with shadow
- Add "copy code" buttons to code blocks

[MARKDOWN CONTENT HERE]
```

## Translation Rules

### Keep in English
- Brand names: MotoRent
- Technical terms: API, URL, JSON, HTML, CSS, ID, GPS, PDF
- File paths and extensions
- Code snippets and examples
- Image paths
- Internal .md links

### Translate to Thai
- Menu names and UI labels
- Instructions and descriptions
- Headers and section titles

### Thai Transliterations
| English | Thai |
|---------|------|
| Check-In | เช็คอิน |
| Check-Out | เช็คเอาท์ |
| Dashboard | แดชบอร์ด |
| Login | ล็อกอิน |
| Logout | ล็อกเอาท์ |
| Settings | การตั้งค่า |
| Profile | โปรไฟล์ |
| Staff | พนักงาน |
| Manager | ผู้จัดการ |

## Updating meta.json

When translating a document:

```json
{
  "documents": {
    "01-orgadmin-quickstart.md": {
      "contentHash": "3972c2c73bb2a251",
      "lastModified": "2026-01-27",
      "translations": {
        "th": {
          "sourceHash": "3972c2c73bb2a251",
          "status": "current"
        }
      }
    }
  }
}
```

Status values:
- `pending` - Not yet translated
- `current` - Translation matches source
- `outdated` - Source has changed since translation

## Code Change Detection

When code changes might affect documentation:

1. Run `git diff --name-only HEAD~10` to find changed files
2. Map code changes to documentation:
   - `Pages/Rentals/*.razor` → 01-orgadmin, 02-staff guides
   - `Pages/Fleet/*.razor` → 03-mechanic, 04-shopmanager guides
   - `Controllers/*.cs` → API-related guides
   - `Services/*.cs` → feature guides

3. Flag affected docs for review:
```bash
# Example detection
git diff --name-only HEAD~5 | grep -E "(Rental|Fleet|Vehicle)" && echo "Review: 01, 02, 03, 04 guides"
```

## Example Sessions

### Translate all pending docs
```
User: update docs --translate

Claude: Checking translation status...
Found 3 documents needing translation:
- 07-asset-depreciation-guide.md (pending)
- 08-cashier-till-guide.md (pending)
- 09-template-designer-guide.md (pending)

Translating via Gemini API...
✓ 07-asset-depreciation-guide.md translated
✓ 08-cashier-till-guide.md translated
✓ 09-template-designer-guide.md translated

Updated meta.json with new hashes.
```

### Generate HTML with frontend-design
```
User: update docs --html

Claude: Generating styled HTML documentation...

Using frontend-design skill to create beautiful layouts:
- Reading 01-orgadmin-quickstart.md
- Applying Tabler styling
- Adding table of contents
- Writing docs/en/01-orgadmin-quickstart.html

✓ Generated 12 English HTML files
✓ Generated 12 Thai HTML files

All HTML documentation updated!
```

### Full sync after code changes
```
User: update docs

Claude: Running full documentation sync...

1. Checking for code changes affecting docs...
   Found changes in: RentalService.cs, CheckInWizard.razor
   Potentially affected: 01-orgadmin, 02-staff guides

2. Checking translation status...
   All translations current.

3. Verifying meta.json hashes...
   ✓ All hashes up to date

Documentation is in sync!
```

## Constraints

- Always preserve markdown formatting in translations
- Never translate code blocks, file paths, or technical identifiers
- Update meta.json after every translation
- When using frontend-design, ensure Tabler CSS is available
- Test generated HTML in browser before committing
- Keep image paths relative and consistent

## Files to Know

| File | Purpose |
|------|---------|
| `user.guides/manifest.json` | English doc titles and order |
| `user.guides.th/manifest.json` | Thai doc titles |
| `user.guides/meta.json` | Version tracking |
| `DocumentationTranslationService.cs` | Gemini translation API |
| `HelpController.cs` | Translation API endpoints |
| `Learn.razor` | Documentation viewer page |
| `MarkdownService.cs` | Markdown rendering with fallback |
