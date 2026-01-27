# Plan: Fix Learn Page Navigation Header

## Problem
The Learn page (`/learn`) doesn't show the same navigation header as Pricing and Features pages. When clicking "Learn", the page displays without the public site navigation bar.

## Root Cause
The Learn page is **missing the layout directive**:
- `Pricing.razor` has `@layout Layout.PublicLayout` (line 4) - shows correct header
- `Learn.razor` has **no layout directive** - falls back to default app layout

## Solution
Add the `@layout Layout.PublicLayout` directive and `@attribute [AllowAnonymous]` to Learn.razor.

## Files to Modify

### `src/MotoRent.Client/Pages/Learn.razor`

**Current (lines 1-6):**
```razor
@page "/learn/{DocName?}"
@using MotoRent.Client.Services
@using MotoRent.Client.Components
@using MotoRent.Client.Controls
@using System.Net.Http.Json
@inherits LocalizedComponentBase<Learn>
```

**Change to:**
```razor
@page "/learn/{DocName?}"
@using Microsoft.AspNetCore.Authorization
@using MotoRent.Client.Services
@using MotoRent.Client.Components
@using MotoRent.Client.Controls
@using System.Net.Http.Json
@attribute [AllowAnonymous]
@layout Layout.PublicLayout
@inherits LocalizedComponentBase<Learn>
```

## Verification
1. Build the project: `dotnet build`
2. Run the app and navigate to `/learn`
3. Confirm the navigation header shows with "Features", "Pricing", "Learn" links
4. Confirm "Learn" link is highlighted/active when on the Learn page
5. Test navigation between Pricing and Learn pages - both should have consistent header
