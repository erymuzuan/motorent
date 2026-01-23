---
phase: 08-manager-oversight
plan: 02
subsystem: manager-ui
tags: [till, dashboard, blazor, localization, verification]

dependency-graph:
  requires: ["08-01"]
  provides: ["manager-till-dashboard", "active-session-card", "variance-alert-badge"]
  affects: ["09-01"]

tech-stack:
  added: []
  patterns:
    - active-session-card: "Reusable card component for session display"
    - variance-alert-badge: "Bell icon with count badge linking to dashboard"

key-files:
  created:
    - src/MotoRent.Client/Pages/Manager/TillDashboard.razor
    - src/MotoRent.Client/Components/Till/ActiveSessionCard.razor
    - src/MotoRent.Client/Components/Till/VarianceAlertBadge.razor
    - src/MotoRent.Client/Resources/Pages/Manager/TillDashboard.resx
    - src/MotoRent.Client/Resources/Pages/Manager/TillDashboard.en.resx
    - src/MotoRent.Client/Resources/Pages/Manager/TillDashboard.th.resx
    - src/MotoRent.Client/Resources/Pages/Manager/TillDashboard.ms.resx
    - src/MotoRent.Client/Resources/Components/Till/ActiveSessionCard.resx
    - src/MotoRent.Client/Resources/Components/Till/ActiveSessionCard.en.resx
    - src/MotoRent.Client/Resources/Components/Till/ActiveSessionCard.th.resx
    - src/MotoRent.Client/Resources/Components/Till/ActiveSessionCard.ms.resx
  modified: []

decisions:
  - id: active-session-card-composition
    choice: "Separate component for session cards"
    rationale: "Reusable display, cleaner TillDashboard code"
  - id: variance-alert-badge-simple
    choice: "MotoRentComponentBase inheritance (no localization)"
    rationale: "Component has no visible text, just icon and number"
  - id: self-verification-case-insensitive
    choice: "StringComparison.OrdinalIgnoreCase for username comparison"
    rationale: "OAuth providers may return differently-cased emails"
  - id: settings-decimal-api
    choice: "Use GetDecimalAsync with defaultValue parameter"
    rationale: "ISettingConfig provides typed getters, not GetSettingAsync"

metrics:
  duration: 10m
  completed: 2026-01-21
---

# Phase 8 Plan 2: Manager Dashboard UI Summary

**One-liner:** Manager till dashboard showing active session cards and closed sessions table with verify, date filter, and variance alerts.

## What Was Done

### Task 1: ActiveSessionCard Component (84 lines)
- Card component displaying staff name, avatar, and session duration
- Per-currency balance display showing THB and non-zero foreign currencies
- Warning badge indicator when variance exceeds threshold
- View Details button with EventCallback
- Duration formatting: hours+minutes or just minutes
- Localization: English, Thai, Malay (6 keys each)

### Task 2: VarianceAlertBadge Component (39 lines)
- Bell icon (`ti-bell`) with red badge showing count
- Links to `/manager/till-dashboard`
- Loads threshold from organization settings (default 100 THB)
- Shows "9+" when count exceeds 9
- Uses `GetVarianceAlertCountAsync` from TillService

### Task 3: TillDashboard Page (336 lines)
- Route: `/manager/till-dashboard`
- Authorization: `RequireTenantManager` policy
- Two-section layout:
  - **Active Sessions**: Cards grid (4 per row on desktop, 2 on tablet)
  - **Closed Sessions**: Table with staff, time, duration, variance, status
- Date filter dropdown: Today / Last 3 days / Last 7 days
- Verify single session with confirmation dialog
- Verify All pending sessions (excludes self)
- Handover report button opens HandoverReportDialog
- Variance calculated in THB using `GetTotalVarianceInThbAsync`
- Row highlighting for sessions exceeding threshold
- Refresh button for manual data reload
- Localization: English, Thai, Malay (32 keys each)

## Key Technical Details

### Integration Points
- `TillService.GetAllActiveSessionsAsync` - Active session loading
- `TillService.GetRecentClosedSessionsAsync` - Closed session loading with day filter
- `TillService.GetTotalVarianceInThbAsync` - Multi-currency variance conversion
- `TillService.VerifySessionAsync` - Session verification with self-prevention
- `ISettingConfig.GetDecimalAsync` - Threshold from organization settings
- `ShopService.GetShopsAsync` - Current shop resolution

### Authorization
- Page restricted to managers via `[Authorize(Policy = "RequireTenantManager")]`
- Self-verification prevented in UI (excludes own sessions from Verify All)
- Self-verification also prevented in TillService (backend enforcement)

### Status Display
- Closed: Green light badge
- ClosedWithVariance: Warning light badge
- Verified: Green solid badge

### Variance Colors
- Zero: text-success (green)
- Positive (over): text-info (blue)
- Negative (short): text-danger + fw-bold (red, bold)

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

**Prerequisites met for Phase 8 Plan 3:**
- Manager dashboard complete at `/manager/till-dashboard`
- Verify workflow functional
- Handover report accessible via button (dialog exists)

**Dependencies for next plan:**
- If 08-03 adds navigation to dashboard, VarianceAlertBadge can be placed in layout
- Settings page may need variance threshold configuration UI
