# Documentation Management System Plan

## Overview
Create a comprehensive documentation system with:
1. Thai translation workflow via Claude skill
2. Enhanced Learn page with language switcher
3. Claude `/update-docs` slash command for automated documentation maintenance
4. **AI-powered HTML generation** using frontend-design skill for beautiful documentation

### Key Insight
The `/update-docs` skill will leverage Claude's `frontend-design` plugin to generate beautifully styled HTML from markdown - not mechanical conversion, but AI-crafted layouts with professional design.

---

## Task Breakdown with Dependencies

### Phase 1: Foundation (No Dependencies)

| ID | Task | Files |
|----|------|-------|
| 1.1 | Create `user.guides.th/` folder structure | `wwwroot/user.guides.th/` |
| 1.2 | Create Thai manifest.json with translated titles | `user.guides.th/manifest.json` |
| 1.3 | Create `meta.json` for version tracking | `user.guides/meta.json` |

### Phase 2: Translation Service (Depends: 1.3)

| ID | Task | Files |
|----|------|-------|
| 2.1 | Create `DocumentationTranslationService.cs` | `MotoRent.Services/` |
| 2.2 | Add translation endpoint to HelpController | `Controllers/HelpController.cs` |
| 2.3 | Translate first 3 guides as proof of concept | `user.guides.th/*.md` |

### Phase 3: Learn Page Enhancement (Depends: 1.1, 1.2)

| ID | Task | Files |
|----|------|-------|
| 3.1 | Create `DocLanguageSwitcher.razor` component | `Components/` |
| 3.2 | Update Learn.razor with language parameter | `Pages/Learn.razor` |
| 3.3 | Enhance Learn.razor.css with beautiful styling | `Pages/Learn.razor.css` |
| 3.4 | Update MarkdownService for language-aware loading | `Services/MarkdownService.cs` |
| 3.5 | Add Learn page localization resources | `Resources/Pages/Learn.*.resx` |

### Phase 4: Claude Slash Command (Depends: 2.1)

| ID | Task | Files |
|----|------|-------|
| 4.1 | Create `/update-docs` skill file | `.claude/skills/update-docs/SKILL.md` |
| 4.2 | Document frontend-design integration | Skill file |
| 4.3 | Create `docs/` output structure | `wwwroot/docs/en/`, `wwwroot/docs/th/` |

### Phase 5: Complete Translations & HTML (Depends: 4.1)

| ID | Task | Files |
|----|------|-------|
| 5.1 | Translate remaining 9 guides to Thai | `user.guides.th/*.md` |
| 5.2 | Verify all translations render correctly | Manual testing |

---

## Key Implementation Details

### 1. Thai Folder Structure
```
wwwroot/user.guides.th/
├── manifest.json           # Thai titles
├── 01-orgadmin-quickstart.md
├── 02-staff-quickstart.md
├── ... (12 files)
└── images/                 # Symlink or copy from English
```

### 2. Version Tracking (`meta.json`)
```json
{
  "documents": {
    "01-orgadmin-quickstart.md": {
      "contentHash": "sha256...",
      "translations": {
        "th": { "sourceHash": "sha256...", "status": "current" }
      }
    }
  }
}
```

### 3. Learn Page Route Change
```
@page "/learn/{Lang?}/{DocName?}"
```
- `/learn` → English, first doc
- `/learn/th` → Thai, first doc
- `/learn/en/02-staff-quickstart.md` → English, specific doc

### 4. Language Switcher UI
- Two buttons: EN | TH
- Active language highlighted
- Preserves current document when switching

### 5. CSS Enhancement (mr-* patterns)
- Professional typography for markdown
- Styled code blocks with syntax highlighting support
- Beautiful tables with Tabler styling
- Blockquote callouts for tips/warnings
- Image shadows and rounded corners

### 6. `/update-docs` Skill Workflow
```
/update-docs [--translate] [--html] [--guide <name>]

1. Scan git diff for code changes affecting documentation
2. Identify affected documentation areas
3. Propose updates to English markdown guides
4. Generate Thai translations for changed files
5. Update meta.json with new hashes
6. (--html) Use frontend-design skill to generate beautiful HTML:
   - Read markdown content
   - Apply professional design patterns
   - Generate styled HTML files to docs/en/ and docs/th/
```

### 7. Frontend-Design HTML Generation
The skill will invoke frontend-design to create:
- Professional typography with Tabler CSS
- Syntax-highlighted code blocks
- Beautiful tables with alternating rows
- Callout boxes for tips/warnings
- Responsive image galleries
- Navigation breadcrumbs and TOC
- Print-friendly layouts

---

## Critical Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Client/Pages/Learn.razor` | Add Lang parameter, language-aware loading |
| `src/MotoRent.Client/Pages/Learn.razor.css` | Enhanced markdown styling |
| `src/MotoRent.Client/Services/MarkdownService.cs` | Language folder support |
| `src/MotoRent.Services/DocumentationTranslationService.cs` | NEW - Gemini translation |
| `src/MotoRent.Server/Controllers/HelpController.cs` | Translation API endpoint |
| `.claude/skills/update-docs/SKILL.md` | NEW - Slash command skill |

---

## Verification Plan

### After Phase 1
- [ ] `user.guides.th/` folder exists
- [ ] `manifest.json` has Thai titles
- [ ] `meta.json` created with document hashes

### After Phase 3
- [ ] `/learn` shows English docs
- [ ] `/learn/th` shows Thai docs (or fallback message)
- [ ] Language switcher toggles between EN/TH
- [ ] CSS styling looks professional
- [ ] Previous/Next navigation works in both languages

### After Phase 4
- [ ] `/update-docs` skill activates correctly
- [ ] Skill can explore codebase for changes
- [ ] Skill can generate Thai translations

### Final Verification
- [ ] All 12 guides translated to Thai
- [ ] DocumentationChat works in both languages
- [ ] `dotnet watch` and browse to `/learn/th`
- [ ] Run `/update-docs --html` to generate styled HTML
- [ ] Verify `docs/en/` and `docs/th/` contain beautiful HTML files
- [ ] Learn page can optionally serve from `docs/` folder

---

## Translation Rules for Thai

| Keep in English | Translate |
|-----------------|-----------|
| MotoRent (brand) | Menu names |
| API, URL, JSON, etc. | Instructions |
| Image paths | Descriptions |
| Code blocks | UI labels |
| Internal links (.md) | Headers |

Technical transliterations:
- Check-In → เช็คอิน
- Check-Out → เช็คเอาท์
- Dashboard → แดชบอร์ด

---

## Estimated Effort

| Phase | Scope |
|-------|-------|
| Phase 1 | Folder setup, meta.json |
| Phase 2 | Translation service |
| Phase 3 | Learn page UI changes |
| Phase 4 | Skill file creation |
| Phase 5 | Bulk translation |
