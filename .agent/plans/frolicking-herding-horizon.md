# Plan: Port Skills from rx.pos to motorent.jaleos

## Context

The motorent.jaleos project has 19 existing skills but is missing browser testing, playwright automation, guide writing, and Gemini API development skills that exist in the sibling `rx.pos` project. Porting these skills enables E2E browser testing, automated guide creation with illustrations, and Gemini API integration for the MotoRent project.

## Skills to Port (5 total)

### 1. `playwright-cli` — Browser automation (COPY AS-IS)
- **Source**: `../rx.pos/.claude/skills/playwright-cli/`
- **Target**: `.claude/skills/playwright-cli/`
- **Adaptation**: None needed — fully generic browser automation commands
- **Files**:
  - `SKILL.md` (279 lines)
  - `references/request-mocking.md`
  - `references/running-code.md`
  - `references/session-management.md`
  - `references/storage-state.md`
  - `references/test-generation.md`
  - `references/tracing.md`
  - `references/video-recording.md`

### 2. `browser-testing` — E2E testing skill (ADAPT)
- **Source**: `../rx.pos/.claude/skills/browser-testing/SKILL.md`
- **Target**: `.claude/skills/browser-testing/SKILL.md`
- **Adaptation needed**:
  - Remove all 10 RxPOS terminal modes (Retail, QSR, Coffee, etc.) — replace with MotoRent pages:
    - `/rentals` — Rental list and management
    - `/vehicles` — Fleet/motorbike inventory
    - `/renters` — Tourist/customer records
    - `/shops` — Shop locations
    - `/maintenance` — Mechanic dashboard
    - `/check-in`, `/check-out` — Rental workflow
    - `/super-admin/*` — Super admin pages
  - Replace auth URL: `dev-impersonate` → MotoRent's `/account/impersonate?user={user}&account={accountNo}&hash={hash}`
  - Replace server URLs: `https://localhost:7079` → `https://localhost:{MOTO_BaseUrl port}`
  - Remove WASM terminal references (MotoRent is Blazor Server only, no WASM POS)
  - Replace seed endpoints with MotoRent equivalents (if any exist in DemoController or similar)
  - Update localization cultures: `en-MY, th-TH, ms-MY` → `en, th, ms`
  - Update evaluation criteria to match MotoRent domain (rental workflows, fleet management, tourist onboarding)
  - Keep the 7-phase workflow structure (Research → Plan → Environment → Seed → Execute → Evaluate → Report)
  - Keep the 4 evaluation dimensions (Flow, UI/UX, Localization, Accessibility)

### 3. `guide-writer` — WHY-first documentation pipeline (ADAPT)
- **Source**: `../rx.pos/.claude/skills/guide-writer/SKILL.md`
- **Target**: `.claude/skills/guide-writer/SKILL.md`
- **Adaptation needed**:
  - Replace all RxPOS source paths with MotoRent paths:
    - `source/domain.pos/` → `src/MotoRent.Domain/`
    - `source/Stafy.Web.Pos.Server/Components/Pages/` → `src/MotoRent.Server/Components/Pages/`
    - `source/database/rx-pos/Tables/` → `database/`
  - Replace guide file locations:
    - `user.guides/<category>/<nn>-<slug>.md` → `user.guides/<nn>-<slug>.md` (flat structure, no categories)
    - Thai: `user.guides.th/` (create if needed)
    - Malay: `user.guides.ms/` (create if needed)
  - Replace 12 RxPOS categories with MotoRent guide structure (role-based: orgadmin, staff, mechanic, shopmanager, tourist, superadmin, etc.)
  - Replace image paths: `wwwroot/guides/images/` → `user.guides/images/`
  - Remove manifest.json/GuideBuildTool references — MotoRent uses simpler `user.guides/README.md` index
  - Update business context: Thai restaurant → motorbike rental in tourist areas (Phuket, Krabi)
  - Update currency references: stays THB
  - Keep the WHY-first philosophy and 7-step pipeline
  - Reference existing `update-docs` skill for translation workflow integration

### 4. `banana-pro-2` — Gemini image generation (COPY WITH MINOR EDITS)
- **Source**: `../rx.pos/.claude/skills/banana-pro-2/SKILL.md`
- **Target**: `.claude/skills/banana-pro-2/SKILL.md`
- **Adaptation**:
  - Auth file path: keep at `.claude/skills/banana-pro-2/gemini-auth.json`
  - Add `.claude/skills/banana-pro-2/gemini-auth.json` to `.gitignore` if not already
  - Style: change teal (#0f766e) to MotoRent's Tropical Teal (#00897B) from css-styling skill
  - Update example from "POS upsell" to MotoRent-relevant example (e.g., "fleet overview dashboard")

### 5. `gemini-api-dev` — Gemini API development (COPY AS-IS)
- **Source**: `../motorent/.claude/skills/gemini-api-dev/SKILL.md`
- **Target**: `.claude/skills/gemini-api-dev/SKILL.md`
- **Adaptation**: None — fully generic API reference

## Superpowers Note

Superpowers skills (brainstorming, writing-plans, executing-plans, test-driven-development, etc.) are **installed plugins**, not local skills. They are already available in this project via the superpowers plugin. No porting needed.

## Mirror to .copilot and .gemini

All 5 new skills must also be copied to:
- `.copilot/skills/<skill-name>/` (identical content)
- `.gemini/skills/<skill-name>/` (identical content)

This maintains consistency with the existing pattern where all three directories have the same skills.

## Execution Order

1. **playwright-cli** (copy as-is, 8 files) — dependency for browser-testing and guide-writer
2. **banana-pro-2** (copy + minor edits, 1 file + gitignore) — dependency for guide-writer
3. **gemini-api-dev** (copy as-is, 1 file)
4. **browser-testing** (adapt, 1 file) — uses playwright-cli
5. **guide-writer** (adapt, 1 file) — uses playwright-cli and banana-pro-2
6. **Mirror all 5 skills** to `.copilot/skills/` and `.gemini/skills/`

## Verification

- Confirm all skill files exist in all 3 directories (`.claude`, `.copilot`, `.gemini`)
- Verify no RxPOS-specific references remain in adapted skills (grep for "Stafy", "rx-pos", "RxPOS", "terminal mode", "pos-qsr", etc.)
- Verify MotoRent paths referenced in skills actually exist in the codebase
- Verify `.gitignore` includes `gemini-auth.json`
