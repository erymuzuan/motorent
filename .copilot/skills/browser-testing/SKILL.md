---
name: browser-testing
description: >
  End-to-end browser testing for MotoRent using playwright-cli. Use this skill whenever the user asks to
  test a feature, verify a page, run an E2E test, QA a page, validate a workflow, check UI/UX
  quality, audit localization, or do browser-based verification of any kind — even if they just say
  "test the rental page" or "check if the fleet list works" or "QA the check-in flow". Covers all
  MotoRent pages and roles (OrgAdmin, ShopManager, Staff, Mechanic, SuperAdmin). Evaluates flow
  correctness, usability, localization, and UI/UX.
---

# Browser Testing Skill

End-to-end browser testing for MotoRent that evaluates **flow correctness**, **usability**, **localization quality**, and **UI/UX best practices** — not just "does it work."

## Mental Model

```
You (commands) → playwright-cli → Browser → Snapshot (back to you)
                                      ↕
                               Refs: e1, e2, e3...
```

Every interaction follows: **snapshot → identify ref → act → snapshot again**. Never guess refs. Never act on stale snapshots.

## When to Use

- "test the rental page", "verify the fleet list", "QA the check-in flow"
- "check if the motorbike list works after my changes"
- "run E2E test for the renter registration"
- "audit the UI on the dashboard"
- "check localization on the shop management page"
- After implementing a feature that touches UI
- Before a release, to verify key flows

## Workflow Overview

```
1. Research    → Understand what to test (code, plans, docs)
2. Plan        → Present a test plan for approval
3. Environment → Start servers via /watch skill
4. Seed        → Prepare test data
5. Execute     → Run tests via playwright-cli
6. Evaluate    → Grade flow, UI/UX, localization, accessibility
7. Report      → Summarize findings with evidence
```

---

## Phase 1: Research

Before testing anything, understand the feature surface. This phase is what separates useful testing from blind clicking.

### 1.1 Identify What to Test

Determine the **role** and **page scope** from the user's request:

| Page Area | Route | Key Features |
|-----------|-------|-------------|
| **Dashboard** | `/` | Overview stats, recent rentals, alerts |
| **Rentals** | `/rentals` | Rental list, filters, status tracking |
| **Check-In** | `/check-in/{id}` | Renter selection, motorbike assignment, deposit, photos |
| **Check-Out** | `/check-out/{id}` | Damage documentation, charges, refund |
| **Vehicles** | `/vehicles` | Fleet inventory, status badges, maintenance history |
| **Renters** | `/renters` | Tourist/customer records, passport/license OCR |
| **Shops** | `/shops` | Shop locations within organization |
| **Maintenance** | `/maintenance` | Mechanic dashboard, repair tracking |
| **Reports** | `/reports` | Rental reports, revenue, fleet utilization |
| **Settings** | `/settings` | Organization settings, payment config |
| **Super Admin** | `/super-admin/*` | Organizations, Users, Impersonate, Logs, Invites |

### 1.2 Gather Context

Search for existing knowledge before writing a plan:

1. **E2E plans**: Check `.agent/plans/*e2e*` or `.agent/plans/*test*` for existing test plans
2. **Code**: Read the relevant Razor page and its `.razor.cs` to understand current behavior
3. **Domain model**: Read relevant entities in `src/MotoRent.Domain/`
4. **Recent changes**: `git log --oneline -20` to see what changed recently
5. **Known bugs**: Check `.agent/plans/` for gap analyses or bug reports

```bash
# Find existing E2E plans
ls .agent/plans/*e2e* 2>/dev/null; ls .agent/plans/*test* 2>/dev/null

# Find pages related to a feature
find src -name "*.razor" -path "*Rental*" 2>/dev/null

# Check for seed/demo endpoints
grep -rn "demo\|seed" src/MotoRent.Server/Controllers/ 2>/dev/null
```

### 1.3 Read Existing Plans

If an E2E plan exists for this area, read it. Reuse existing plans rather than inventing new ones.

---

## Phase 2: Present a Test Plan

Before executing, present the plan to the user for approval. Structure it as:

```markdown
## Test Plan: [Feature/Page] — [Tenant/Shop]

### Scope
- Role: [OrgAdmin / ShopManager / Staff / Mechanic / SuperAdmin]
- Pages under test: [list]
- Test user: [email]
- Evaluation dimensions: Flow ✓ | UI/UX ✓ | Localization ✓ | Accessibility ✓

### Environment
- [ ] Blazor Server (https://localhost:{port})

### Test Data
- Seed method: [API endpoint / manual setup / existing data]
- Organization: [name], AccountNo: [account]
- Vehicles: [count] motorbikes
- Renters: [count] tourists

### Test Scenarios
1. [Scenario name] — [what it validates]
2. [Scenario name] — [what it validates]
...

### Evaluation Criteria
- Flow: [specific checkpoints]
- UI/UX: [what to audit — hierarchy, targets, feedback, grouping]
- Localization: [cultures to check — en, th, ms]
- Accessibility: [contrast, focus, keyboard nav]
```

Wait for the user's go-ahead before proceeding to execution.

---

## Phase 3: Environment Setup

Use the `/watch` skill to start the server.

### 3.1 Start Server

```bash
dotnet watch --project src/MotoRent.Server/MotoRent.Server.csproj
```

Wait for it to report `Now listening on...` before proceeding.

### 3.2 Authentication

Use the impersonate endpoint — no OAuth needed:

```
https://localhost:{port}/account/impersonate?user={userName}&account={accountNo}&hash={MD5(userName:accountNo)}
```

---

## Phase 4: Seed Test Data

### 4.1 Use Existing Seeds

Check if seed/demo endpoints exist:

```bash
# Check for demo or seed controllers
grep -rn "seed\|demo" src/MotoRent.Server/Controllers/ 2>/dev/null
```

### 4.2 Manual Setup via UI

If no seed exists, set up data through the UI using playwright-cli:
- Vehicles → `/vehicles`
- Renters → `/renters`
- Shops → `/shops`
- Settings → `/settings`

### 4.3 Use test-data Skill

If structured test data is needed, use the `/test-data` skill to insert data via SqlCmd.

---

## Phase 5: Execute Tests

### 5.1 The Core Loop

Every test interaction follows this pattern:

```bash
playwright-cli snapshot                    # 1. See current state
# Read the snapshot, identify target ref   # 2. Find what to interact with
playwright-cli click e15                   # 3. Act
playwright-cli snapshot                    # 4. Verify result
```

**Critical rules:**
- Always snapshot before acting — refs change after navigation, dialog open, AJAX load
- Snapshot after every critical action — don't chain 5 clicks blindly
- Use `--filename=` for artifacts you'll reference in the report
- Check `console` after page loads to catch JS errors early

### 5.2 Common Test Actions

```bash
# Navigate and verify page load
playwright-cli goto https://localhost:{port}/vehicles
playwright-cli snapshot

# Fill a form
playwright-cli fill e5 "Honda PCX 160"
playwright-cli fill e7 "350.00"
playwright-cli click e12   # Save button

# Search/filter
playwright-cli fill e3 "available"
playwright-cli press Enter
playwright-cli snapshot

# Dialog interaction
playwright-cli click e8    # Opens dialog
playwright-cli snapshot    # MUST snapshot again — dialog has new refs
playwright-cli fill e22 "value"
playwright-cli click e25   # Confirm

# Check for errors
playwright-cli console error
```

---

## Phase 6: Evaluate

This is what makes this skill different from basic flow verification. Every test evaluates four dimensions.

### 6.1 Flow Correctness

The baseline — does the feature work as intended?

- [ ] Page loads without errors (no 500, no console exceptions)
- [ ] Data displays correctly (names, prices, statuses match expected data)
- [ ] Interactive elements respond (buttons, links, forms submit)
- [ ] State transitions work (e.g., rental Pending → Active → Completed)
- [ ] Navigation flows complete end-to-end
- [ ] Error states handled (empty lists, invalid input, network failures)

### 6.2 UI/UX Audit

Apply the frontend-design skill's UX verification checklist. For each page visited:

**Visual Hierarchy** (2-second test)
- Can a user identify the primary action within 2 seconds?
- Is the most important information (total, status, next step) visually dominant?

**Gestalt Grouping**
- Are related controls visually clustered?
- Are unrelated sections clearly separated?
- Is whitespace used as a structural element?

**Interaction Quality**
- Interactive elements at least 44x44px touch targets?
- Do buttons have hover/active/loading states?
- Is feedback immediate after every action? (press states, spinners, success indicators)

**Cognitive Load**
- Are there more than 7 ungrouped options visible? (Hick's Law violation)
- Is information chunked into digestible groups? (Miller's Law)
- Are advanced features behind progressive disclosure?

**Familiarity**
- Would a rental shop staff recognize the layout without training?
- Do icons have labels? (Recognition over recall)
- Are patterns consistent across similar pages?

**Rental-Specific UX**
- Is the motorbike status always visible (Available, Rented, Maintenance)?
- Can staff complete a check-in without excessive scrolling?
- Is the rental price/deposit prominently displayed?
- Are damage photos easy to capture and review?
- Is the renter's passport/license information clearly displayed?

### 6.3 Localization Audit

MotoRent supports three cultures: **en** (default), **th**, **ms**.

For each page under test:

```bash
# Switch to Thai
playwright-cli goto "https://localhost:{port}/<page>?culture=th"
playwright-cli snapshot --filename=page-th.yaml

# Switch to Malay
playwright-cli goto "https://localhost:{port}/<page>?culture=ms"
playwright-cli snapshot --filename=page-ms.yaml
```

Check for:
- [ ] All visible text is translated (no English leaking through in th/ms)
- [ ] No raw enum values displayed (should be `@Localizer[EnumValue.ToEmptyString().Humanize()]`)
- [ ] Formatted numbers use correct locale
- [ ] Currency displays correctly (THB ฿)
- [ ] Date formats match locale
- [ ] No truncated text from longer translations
- [ ] Text alignment is correct

### 6.4 Accessibility Check

Quick accessibility audit using playwright-cli:

```bash
# Check color contrast
playwright-cli eval "getComputedStyle(document.querySelector('.btn-primary')).color"
playwright-cli eval "getComputedStyle(document.querySelector('.btn-primary')).backgroundColor"

# Check focus visibility
playwright-cli press Tab
playwright-cli snapshot   # Verify focus ring visible

# Check aria labels
playwright-cli eval "document.querySelectorAll('[role]').length"
playwright-cli eval "document.querySelectorAll('img:not([alt])').length"
```

- [ ] Contrast ratios sufficient (4.5:1 text, 3:1 UI elements)
- [ ] Focus states visible when tabbing
- [ ] Images have alt text
- [ ] Form inputs have labels
- [ ] Error messages associated with their inputs

---

## Phase 7: Report

Summarize findings in a structured report. Save screenshots as evidence.

```markdown
## Browser Test Report: [Feature/Page]

**Date**: [date]
**Page**: [route] | **Role**: [role] | **User**: [email]

### Flow Correctness: [PASS/FAIL]
- [x] Page loads ✓
- [x] Data correct ✓
- [ ] Bug: [description] — screenshot: [filename]

### UI/UX: [score/10]
| Principle | Status | Notes |
|-----------|--------|-------|
| Visual Hierarchy | ✓/✗ | [observation] |
| Touch Targets | ✓/✗ | [observation] |
| Feedback | ✓/✗ | [observation] |
| Cognitive Load | ✓/✗ | [observation] |

### Localization: [PASS/PARTIAL/FAIL]
| Culture | Status | Missing Keys |
|---------|--------|-------------|
| en | ✓ | — |
| th | ✗ | [list] |
| ms | ✗ | [list] |

### Accessibility: [PASS/PARTIAL/FAIL]
- Contrast: [status]
- Focus: [status]
- Labels: [status]

### Screenshots
- [filename]: [description]
```

---

## Playwright-CLI Best Practices

### Do

| Practice | Why |
|----------|-----|
| Snapshot before every action | Refs change after navigation, dialog open, AJAX load |
| Use `--filename=` for evidence | Named snapshots are findable; timestamped ones get lost |
| Check `console error` after page load | Catches JS errors that don't show in the UI |
| Close sessions when done | Browsers consume resources; stale sessions cause confusion |

### Don't

| Mistake | What Happens | Fix |
|---------|-------------|-----|
| Acting on stale refs | Wrong element clicked, test corrupted | Take a fresh snapshot |
| Chaining 5+ actions without snapshot | Can't tell where things went wrong | Snapshot after critical actions |
| Using `screenshot` as primary tool | Images aren't machine-readable | Use `snapshot` — it returns structured data |
| Not closing browsers | Memory leak, port conflicts | `playwright-cli close` or `playwright-cli close-all` |

### Pro Tips

1. **Snapshot filenames for reports**: `playwright-cli snapshot --filename=after-checkout.yaml`
2. **Tab management**: `tab-new`, `tab-select`, `tab-list` for multi-page workflows
3. **Cookie manipulation**: `cookie-set`/`cookie-get` for testing auth edge cases
4. **Route mocking**: `playwright-cli route "https://api.example.com/**" --body='{"error": true}'` to test error states
5. **Tracing**: `playwright-cli tracing-start` before a complex flow, `playwright-cli tracing-stop` after — produces a trace file for debugging
6. **Console filtering**: `playwright-cli console warning` or `playwright-cli console error` to filter noise
7. **DOM inspection**: `playwright-cli eval "document.querySelector('.total')?.textContent"` for precise value checks

---

## Quick Reference: URLs

### MotoRent Server (https://localhost:{port})

| Page | Route |
|------|-------|
| Dashboard | `/` |
| Rentals | `/rentals` |
| Vehicles | `/vehicles` |
| Renters | `/renters` |
| Shops | `/shops` |
| Maintenance | `/maintenance` |
| Reports | `/reports` |
| Settings | `/settings` |
| Super Admin - Organizations | `/super-admin/organizations` |
| Super Admin - Users | `/super-admin/users` |
| Super Admin - Impersonate | `/super-admin/impersonate` |
| Super Admin - Invites | `/super-admin/invites` |
| Super Admin - Logs | `/super-admin/logs` |
| Super Admin - Settings | `/super-admin/settings` |

### Auth
```
/account/impersonate?user={userName}&account={accountNo}&hash={MD5(userName:accountNo)}
```

### User Roles for Testing
| Role | What to Test |
|------|-------------|
| **OrgAdmin** | Full tenant access — all pages, settings, reports |
| **ShopManager** | Shop-level management — vehicles, rentals, staff |
| **Staff** | Rental desk — check-in, check-out, renter management |
| **Mechanic** | Maintenance — vehicle repairs, damage reports |
| **SuperAdmin** | Platform admin — organizations, users, impersonation |
