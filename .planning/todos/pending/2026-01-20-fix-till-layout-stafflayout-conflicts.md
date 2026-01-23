---
created: 2026-01-20T12:30
title: Fix Till layout conflicts with StaffLayout
area: ui
files:
  - src/MotoRent.Client/Pages/Staff/Till.razor:68-94
  - src/MotoRent.Client/Layout/StaffLayout.razor:37-44
  - src/MotoRent.Client/Components/Till/ExchangeRatePanel.razor.css:1-20
  - src/MotoRent.Server/wwwroot/css/site.css:2098-2112
---

## Problem

Till page has layout conflicts when rendered inside StaffLayout:

1. **Session bar hidden under header**: The `mr-till-session-bar` with `position: sticky; top: 0` was being cut off at the top because StaffLayout's `<main>` has `py-4` padding and the content is wrapped in `container-lg`.

2. **Exchange rate FAB hidden under bottom nav**: The floating action button had `bottom: 24px` but StaffLayout has a bottom navigation bar (~70px height), causing the FAB to be partially or fully hidden.

3. **Content cut off at bottom**: Till content near the bottom was hidden behind the bottom nav and FAB.

Root causes:
- StaffLayout uses flex layout with header, scrollable main (py-4), and bottom nav
- Till page content renders inside `container-lg` which constrains width
- Sticky positioning interacts with container padding/margins
- FAB position didn't account for bottom nav

## Solution

Applied fixes (may need cleanup/refinement):

1. Session bar: Added `margin-top: -1.5rem` to negate py-4 padding, plus full-width breakout using `margin-left/right: calc(-50vw + 50%)` and matching padding

2. FAB: Increased `bottom` to `90px` (80px mobile) to clear bottom nav

3. Till content: Added inline `padding-bottom: 100px` to ensure scrollable content isn't cut off

Consider for future:
- Create utility CSS class for "break out of container" pattern
- Add CSS custom property for bottom nav height (`--staff-bottom-nav-height`)
- Consider removing py-4 from StaffLayout main and letting pages control their own padding
