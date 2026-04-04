---
name: guide-writer
description: Create or rewrite WHY-first user guides for MotoRent features. Use this skill whenever the user asks to write documentation, create a user guide, improve docs, add a feature guide, document a feature, or says things like "write guide for X", "document the Y feature", "update the Z guide". Also use when the user mentions user.guides, guide illustrations, or translating guides. Covers the full pipeline - codebase exploration, business-case writing, illustration generation, screenshot capture, translation, and docs infrastructure updates.
---

# Guide Writer — WHY-First User Guide Pipeline

Turn any MotoRent feature into a business-focused user guide that explains WHY before HOW.

## When This Skill Runs

The user specifies a **feature name** (e.g., "Check-In", "Fleet Management", "Damage Documentation", "Agent Management") and optionally a **tenant** for screenshots. The skill runs the full pipeline, skipping steps where output already exists.

## The WHY-First Philosophy

Traditional docs say "click here, then here." Business owners don't care about buttons — they care about outcomes. Every guide this skill produces follows a narrative structure:

1. **The Problem** — What pain point does this feature solve?
2. **Visual Overview** — How the whole flow works at a glance
3. **Real Scenarios** — Day-in-the-life stories showing the feature in action
4. **The Numbers** — ROI, cost-benefit, "what if" calculations
5. **Quick Setup** — Condensed HOW-TO (the traditional docs content)
6. **Day-to-Day Operations** — Reference for daily use
7. **Optimization Tips** — How to get more value from the feature
8. **Troubleshooting** — Quick fixes

## Pipeline Steps

### Step 1: Explore the Feature

Launch Explore agents to understand the feature deeply:

**Codebase exploration** — Find all related files:
- Domain entities in `src/MotoRent.Domain/`
- Pages in `src/MotoRent.Server/Components/Pages/`
- Client components in `src/MotoRent.Client/`
- Services in `src/MotoRent.Services/`
- Database schema in `database/`
- Feature flags and settings

**Existing guide exploration** — Check what already exists:
- English guide: `user.guides/<nn>-<slug>.md`
- Thai: `user.guides.th/<nn>-<slug>.md` (if directory exists)
- Malay: `user.guides.ms/<nn>-<slug>.md` (if directory exists)
- Guide images: `user.guides/images/`
- Guide index: `user.guides/README.md`

**Business context** — Understand the WHY:
- What problem does this feature solve for a motorbike rental shop owner in Phuket/Krabi?
- What happens without this feature? (pain points)
- Who benefits? (owner, staff, tourists, mechanics, agents)
- What's the ROI story?

### Step 2: Plan the Guide Structure

Based on exploration, design the narrative:

1. Choose 1-2 **real-world scenarios** as story anchors (e.g., "Khun Somchai who runs a rental shop near Patong Beach")
2. Identify **5-7 illustration scenes** that tell the visual story
3. List **screenshots** needed from the live app
4. Draft the **section outline** with estimated content per section
5. Plan the **ROI calculation** with realistic Thai business numbers (THB currency)

Present the plan to the user for approval before proceeding.

### Step 3: Generate Illustrations

Use `/banana-pro-2` (Gemini image generation) to create flat illustrations.

**Style consistency** — Every illustration uses:
- Flat design, clean modern SaaS aesthetic
- Tropical Teal (#00897B) primary color with warm Thai accents (orange, gold)
- White background, no text labels
- Subtle shadows, professional documentation quality
- Thai tourist-area context (motorbike shops, beach settings, tourist characters)

**Process:**
1. Open Gemini with saved auth: `playwright-cli -s=gemini open "https://gemini.google.com"` then `playwright-cli -s=gemini state-load .claude/skills/banana-pro-2/gemini-auth.json`
2. For each illustration: new chat, fill prompt, submit, wait 35s, find download button, click download, copy to target
3. Save to: `user.guides/images/`
4. Naming: `<nn>-<feature>-<scene-description>.png` (e.g., `04-checkin-hero.png`)

**If auth fails** (Gemini asks to sign in), tell the user to run:
```
! playwright-cli -s=gemini open --headed "https://gemini.google.com"
```
Then sign in manually, and save state: `playwright-cli -s=gemini state-save .claude/skills/banana-pro-2/gemini-auth.json`

### Step 4: Capture Screenshots

Use `/browser-testing` or `playwright-cli` to capture live app screenshots.

1. Check which browser sessions are open: `playwright-cli list`
2. Navigate to the relevant pages
3. If demo data is needed, seed it or create items via the UI
4. Take screenshots: `playwright-cli screenshot --filename=<name>.png`
5. Save to: `user.guides/images/`

**If a page errors or the app isn't running**, use HTML comments as placeholders:
```markdown
<!-- Screenshot: filename.png — description of what it shows -->
```
These can be filled in later without breaking the guide.

### Step 5: Write the English Guide

Create or replace `user.guides/<nn>-<slug>.md` with the WHY-first narrative.

**Structure template:**

```markdown
# [Feature Name]: [Value Proposition Tagline]

[Hook paragraph — the problem, framed as a question to the owner]

[Solution paragraph — what this feature gives them]

![Hero illustration](images/<feature>-hero.png)

## How It Works in 30 Seconds
[Visual flow + 5-step numbered list]

---

## Story: [Character Name] the [Role]
[Day-in-the-life walkthrough with illustrations and screenshots at each stage]

## Story: [Second Scenario]
[Shorter parallel narrative showing different use case]

---

## The Numbers: Why [Feature] Pays for Itself
[ROI table, "what if" scenarios, cost-benefit in THB]

## Quick Setup
[3-step condensed HOW-TO with field reference tables]

## Day-to-Day Operations
[Condensed operational reference]

## Optimize Your [Feature]
[Actionable growth/improvement tips]

## Troubleshooting
[Problem → Solution table]

## Related Guides
[Cross-links to related guides]
```

**Image references:** Use `images/<filename>.png` format (relative paths).
**Tone:** Conversational, "you" addressing the business owner. Real Thai names, THB currency.

### Step 6: Translate

Launch parallel background agents to translate:

- **Thai** → `user.guides.th/<nn>-<slug>.md`
  - Natural Thai prose, technical terms (QR, GPS, MotoRent) stay English
  - Thai names (e.g., คุณสมชาย), currency บาท
  - Image paths stay identical

- **Malay** → `user.guides.ms/<nn>-<slug>.md`
  - Natural Bahasa Melayu, technical terms stay English
  - Keep Thai scenario context (it's set in Thailand)
  - Image paths stay identical

Reference existing translated guides for style consistency. Also reference the `update-docs` skill for translation workflow and meta.json tracking.

### Step 7: Update Docs Infrastructure

1. **Update README.md** at `user.guides/README.md`:
   - Add or update the entry for the new guide
   - Maintain consistent numbering and description format

2. **Update meta.json** (if it exists) via the `update-docs` skill workflow

## File Locations Reference

| Asset | Path |
|-------|------|
| English guides | `user.guides/<nn>-<slug>.md` |
| Thai guides | `user.guides.th/<nn>-<slug>.md` |
| Malay guides | `user.guides.ms/<nn>-<slug>.md` |
| Illustrations & screenshots | `user.guides/images/` |
| Guide index | `user.guides/README.md` |
| Audit log | `user.guides/AUDIT.md` |

## Existing Guides

| # | Slug | Target Role |
|---|------|------------|
| 01 | orgadmin-quickstart | Org Admin |
| 02 | staff-quickstart | Staff |
| 03 | mechanic-quickstart | Mechanic |
| 04 | shopmanager-quickstart | Shop Manager |
| 05 | tourist-guide | Tourist/Renter |
| 06 | superadmin-guide | Super Admin |
| 07 | asset-depreciation-guide | Org Admin / Finance |
| 08 | cashier-till-guide | Staff / Cashier |
| 09 | template-designer-guide | Org Admin |
| 10 | agent-management-guide | Org Admin |
| 11 | service-locations-guide | Org Admin / Shop Manager |
| 12 | saas-onboarding-guide | Super Admin / New Tenants |

## Skip Logic

Before each step, check if the output already exists:
- **Illustrations**: Check `user.guides/images/<feature>-*.png` — skip generation if files exist
- **Screenshots**: Check for screenshot files — skip if they exist
- **Guide text**: If the markdown file exists and was recently modified, ask the user whether to replace or update
- **Translations**: If translated files exist and are newer than the English source, skip

## Example Invocations

- `"Write a guide for the Check-In flow"` → Full pipeline for `02-staff-quickstart` or new guide
- `"Update the Fleet Management guide with WHY-first approach"` → Rewrite existing guide
- `"Document the Agent Management feature for demo-shop tenant"` → Full pipeline with screenshots from demo-shop
- `"Revisit the Tourist guide — add a damage scenario"` → Update existing guide, skip illustrations
