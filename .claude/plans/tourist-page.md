# Multi-Tenant Tourist-Facing Pages Implementation Plan

## Overview

Design a public-facing, multi-tenant tourist portal where each rental shop can have branded pages accessible via:
- Standard URL: `https://www.motorent.co.th/tourist/{accountNo}/browse`
- Custom domains: `https://adam.co.th/browse` (rewrites to `/tourist/AdamMotoGolok/browse`)

## Implementation Status

### Completed (Sprint 1 & 2)
- [x] `TenantContext.cs` + `TenantBranding` models
- [x] Extended `Organization` entity with `CustomDomain` and `Branding`
- [x] Database migration `008-tenant-branding.sql`
- [x] `TenantResolverService` with caching
- [x] `TenantDomainMiddleware` (subdomain + custom domain)
- [x] `TouristRequestContext` for URL-based tenant resolution
- [x] Updated `Program.cs` with service registration

### Pending
- [ ] `TouristComponentBase` base class
- [ ] Rewrite `TouristLayout.razor` with cascading
- [ ] Layout templates (Modern, Classic, Minimal)
- [ ] Update tourist page routes (`Browse.razor`, etc.)
- [ ] Branding settings UI

## Architecture

### Key Design Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| How to inject AccountNo for anonymous users? | `TouristRequestContext` checks URL before claims | Keeps `MotoRentRequestContext` unchanged |
| Where to store tenant context during navigation? | CascadingValue from `TouristLayout` | Blazor-native, survives SPA navigation |
| How to handle custom domains? | Server middleware + circuit state | Middleware rewrites initial request |
| Theme storage? | Embedded `TenantBranding` in Organization | Simpler, fewer database calls |
| Layout templates? | Enum-based: Classic, Modern, Minimal | Preset layouts with CSS customization |

### URL Routing Strategy

1. **Server middleware** handles custom domain → URL path rewriting
2. **Blazor routing** uses `@page "/tourist/{AccountNo}/browse"` pattern
3. **CascadingValue** maintains tenant context during SPA navigation
4. Links use relative paths to work on both URL patterns

## Files Created

| File | Purpose |
|------|---------|
| `src/MotoRent.Domain/Tourist/TenantContext.cs` | Tenant context model with branding |
| `src/MotoRent.Services/Tourist/TenantResolverService.cs` | Tenant resolution with caching |
| `src/MotoRent.Server/Middleware/TenantDomainMiddleware.cs` | Custom domain/subdomain handling |
| `src/MotoRent.Server/Services/TouristRequestContext.cs` | URL-based IRequestContext |
| `database/008-tenant-branding.sql` | Schema migration for CustomDomain |

## Files Modified

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Core/Organization.cs` | Added `CustomDomain`, `TenantBranding` |
| `src/MotoRent.Server/Program.cs` | Service registration, middleware pipeline |

## Confirmed Design Decisions

1. **Clean URLs** - Custom domains use internal URL rewrite; users see `adam.co.th/browse`
2. **Subdomain support** - Support both `{tenant}.motorent.co.th` and full custom domains
3. **Branding UI** - Tenants configure basics, SuperAdmin handles advanced/CSS
4. **Full customization** - Colors, layout template, hero images, footer text, custom CSS

## Next Steps

### Sprint 3: Blazor Components
1. Create `TouristComponentBase` base class
2. Rewrite `TouristLayout.razor` with tenant context cascading
3. Create `ModernTouristTemplate.razor`
4. Update `Browse.razor` routes to `/tourist/{AccountNo}/browse`

### Sprint 4: Additional Templates
1. `ClassicTouristTemplate.razor`
2. `MinimalTouristTemplate.razor`
3. CSS variable system for theming

### Sprint 5: Settings UI
1. Tenant branding settings page
2. SuperAdmin tenant branding page
3. Color picker + live preview
