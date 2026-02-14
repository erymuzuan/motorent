# JaleOS Worktree Setup - Make Operator Branding Configurable

## Context
The multi-market infrastructure is implemented on the `jaleos` branch but many values are still hardcoded (branding, emails, domains, currency formatting). The goal is to make ALL operator-specific values configurable via environment variables (`MOTO_` prefix) and `appsettings.json`, then set the JaleOS values in config only.

**JaleOS config values:**
- App name: JaleOS
- Contact: Adam JaleOS, B063A Ken Damansara 2, SS 2/72 Petaling Jaya, Selangor
- Phone: +60129879200 | Email: adam@jaleos.my | Domain: jaleos.my
- Country: MY | Currency: MYR | Timezone: UTC+8 | Languages: en,ms

---

## Part A: Extend Configuration Infrastructure

### A1. Add `DefaultCulture` to `CountryConfig` record
**File:** `src/MotoRent.Domain/Core/MotoConfig.cs`
- Add `DefaultCulture` field to `CountryConfig` record: `string DefaultCulture`
- MY → `"en-MY"`, TH → `"th-TH"`
- Update the `GetCountryConfig` switch to include it

### A2. Add `BaseDomain` to `MotoConfig`
**File:** `src/MotoRent.Domain/Core/MotoConfig.cs`
- New static property: `BaseDomain` from env var `MOTO_BaseDomain`
- Default: `"motorent.co.th"` (preserves existing behavior; operators override per deployment)

### A3. Extend `CompanySettings` with `SupportEmail` and `DocsUrl`
**File:** `src/MotoRent.Domain/Settings/CompanySettings.cs`
- Add `public string? SupportEmail { get; set; }` — falls back to `Email` when null
- Add `public string? DocsUrl { get; set; }`
- Add computed helper: `public string GetSupportEmail() => SupportEmail ?? Email ?? "";`

### A4. Fix currency formatting in base class
**File:** `src/MotoRent.Client/Controls/MotoRentComponentBase.cs`
- Lines 81, 86: Replace hardcoded `"THB"` with `MotoConfig.CountryDefaults.Currency`
```csharp
protected string FormatCurrency(decimal amount) => $"{amount:N0} {MotoConfig.CountryDefaults.Currency}";
protected string FormatCurrencyWithDecimals(decimal amount) => $"{amount:N2} {MotoConfig.CountryDefaults.Currency}";
```

---

## Part B: Replace Hardcoded Branding with Config

### B1. Replace `"SafeNGo"` fallbacks → `MotoConfig.ApplicationName`
All these files use hardcoded `"SafeNGo"` where they should read from config:

| File | Lines | Change |
|------|-------|--------|
| `src/MotoRent.Client/Layout/StaffLayout.razor` | 13-14 | `alt` and text → `@MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Layout/ManagerLayout.razor` | 19-20 | Same |
| `src/MotoRent.Client/Layout/Templates/TenantNotFound.razor` | 16 | `"Go to SafeNGo Home"` → `"Go to @MotoConfig.ApplicationName Home"` |
| `src/MotoRent.Client/Layout/Templates/TenantFooter.razor` | 20, 89 | `?? "SafeNGo"` → `?? MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Layout/Templates/ModernTouristTemplate.razor` | 21 | Same |
| `src/MotoRent.Client/Layout/Templates/MinimalTouristTemplate.razor` | 19, 50 | Same |
| `src/MotoRent.Client/Layout/Templates/ClassicTouristTemplate.razor` | 16 | Same |

### B2. Replace `"MotoRent"` fallbacks → `MotoConfig.ApplicationName`

| File | Line | Change |
|------|------|--------|
| `src/MotoRent.Client/Components/Shared/LogoHeader.razor` | 47 | `DefaultName = "MotoRent"` → `DefaultName = MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Layout/MainLayout.razor` | 211 | `m_orgName = "MotoRent"` → `= MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Pages/Bookings/PrintBookingConfirmation.razor` | 148 | `?? "MotoRent"` → `?? MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Pages/Rentals/PrintAgreement.razor` | 224 | Same |
| `src/MotoRent.Client/Pages/Onboarding/OnboardingWizard.razor` | 18 | `alt="MotoRent"` → `alt="@MotoConfig.ApplicationName"` |
| `src/MotoRent.Client/Pages/RefundPolicy.razor` | 7 | `- MotoRent` → `- @MotoConfig.ApplicationName` |
| `src/MotoRent.Client/Pages/SuperAdmin/TenantBrandingSettings.razor` | 12 | Same |

### B3. Replace `"SafeNGo"` in resx files → `MotoConfig.ApplicationName`

| File | Change |
|------|--------|
| `Resources/Pages/SuperAdmin/VehicleLookups.resx` | Resx value: `"Vehicle Lookups"` (remove ` - SafeNGo` suffix); page uses `@Localizer["PageTitle"] - @MotoConfig.ApplicationName` pattern |
| `Resources/Pages/SuperAdmin/VehicleLookups.th.resx` | Same approach |

---

## Part C: Replace Hardcoded Emails with CompanySettings

### C1. Inject `IOptions<CompanySettings>` into pages that display contact info
Add `@inject IOptions<CompanySettings> CompanyInfo` to each page, access via `CompanyInfo.Value`.

### C2. Contact page
**File:** `src/MotoRent.Client/Pages/Contact.razor`
- Add `@inject IOptions<CompanySettings> CompanyInfo`
- Line 254: `contact@motorent.io` → `@CompanyInfo.Value.GetSupportEmail()`
- Line 283: Change `ti-currency-baht` to `ti-coin` (generic icon, not Thai-specific)

### C3. QuickStart page
**File:** `src/MotoRent.Client/Pages/Onboarding/QuickStart.razor`
- Add `@inject IOptions<CompanySettings> CompanyInfo`
- Line 135: `https://docs.motorent.io` → `@(CompanyInfo.Value.DocsUrl ?? "#")`
- Line 139: `support@motorent.io` → `mailto:@CompanyInfo.Value.GetSupportEmail()`

### C4. Tourist MyBooking page
**File:** `src/MotoRent.Client/Pages/Tourist/MyBooking.razor`
- Add `@inject IOptions<CompanySettings> CompanyInfo`
- Line 318: `support@motorent.com` → `@CompanyInfo.Value.GetSupportEmail()`

### C5. ShopForm placeholder
**File:** `src/MotoRent.Client/Pages/ShopForm.razor`
- Line 87: `shop@motorent.com` → generic placeholder like `shop@example.com`

### C6. Worker support email
**File:** `src/MotoRent.Worker/Subscribers/CommentSupportSubscriber.cs`
- Line 20: Inject `IOptions<CompanySettings>` via constructor
- Replace hardcoded `"support@motorent.com"` with `CompanySettings.GetSupportEmail()`

### C7. RefundPolicy resx — parameterize emails
**File:** `src/MotoRent.Client/Pages/RefundPolicy.razor`
- Add `@inject IOptions<CompanySettings> CompanyInfo`
- Lines 32, 52: Change to `@Localizer["HowToRequestBody", CompanyInfo.Value.GetSupportEmail()]` and `@Localizer["ContactBody", CompanyInfo.Value.GetSupportEmail()]`

**Files:** All 4 RefundPolicy resx files (`.resx`, `.en.resx`, `.th.resx`, `.ms.resx`)
- `HowToRequestBody`: Replace literal `support@motorent.app` with `{0}` placeholder
- `ContactBody`: Replace literal `support@motorent.app` with `{0}` and `Thailand time, GMT+7` — fix in `.ms.resx` to be `Malaysia time, GMT+8` (this is locale-specific, correct for ms translation)

---

## Part D: Configurable Domain in TenantDomainMiddleware

### D1. Read base domain from MotoConfig
**File:** `src/MotoRent.Server/Middleware/TenantDomainMiddleware.cs`
- Replace `c_baseDomain = ".motorent.co.th"` with `$".{MotoConfig.BaseDomain}"`
- Replace `c_baseThDomain = ".motorent.th"` — remove entirely (extra Thai domain not needed in generic code)
- Replace `s_systemDomains` hardcoded set — build dynamically from `MotoConfig.BaseDomain`:
  ```csharp
  private static readonly HashSet<string> s_systemDomains = new(StringComparer.OrdinalIgnoreCase)
  {
      "localhost",
      MotoConfig.BaseDomain,
      $"www.{MotoConfig.BaseDomain}"
  };
  ```
- Update `TryExtractSubdomain` to use single base domain from config

### D2. TenantBrandingSettings — display domain from config
**File:** `src/MotoRent.Client/Pages/SuperAdmin/TenantBrandingSettings.razor`
- Line 86: `motorent.co.th/tourist/...` → `@MotoConfig.BaseDomain/tourist/...`
- Line 107: `.motorent.co.th` → `.@MotoConfig.BaseDomain`

---

## Part E: Configurable Language Defaults

### E1. Entity defaults read from CountryConfig.DefaultCulture

| File | Line | Change |
|------|------|--------|
| `src/MotoRent.Domain/Core/Organization.cs` | 37 | `Language = "th-TH"` → `Language = MotoConfig.CountryDefaults.DefaultCulture` |
| `src/MotoRent.Domain/Tourist/TenantContext.cs` | 42 | Same |
| `src/MotoRent.Domain/Core/IOnboardingService.cs` | 30 | `PreferredLanguage = "th-TH"` → `= MotoConfig.CountryDefaults.DefaultCulture` |

---

## Part F: Dynamic manifest.json

### F1. Serve manifest.json dynamically via minimal API
**File:** `src/MotoRent.Server/Program.cs`
- Add a minimal API endpoint that generates manifest.json from `MotoConfig.ApplicationName`:
```csharp
app.MapGet("/manifest.json", () => Results.Json(new { ... }, contentType: "application/manifest+json"));
```
- Remove or rename static `wwwroot/manifest.json` to `wwwroot/manifest.template.json` (reference only)
- The dynamic endpoint populates `name`, `short_name`, `description` from `MotoConfig.ApplicationName`
- All other manifest fields (icons, screenshots, shortcuts, etc.) remain static values

---

## Part G: Set JaleOS Configuration

### G1. appsettings.json — JaleOS values
**File:** `src/MotoRent.Server/appsettings.json`
```json
"CompanyInfo": {
  "Name": "JaleOS",
  "LegalName": "JaleOS",
  "Email": "adam@jaleos.my",
  "Phone": "+60129879200",
  "Address": "B063A Ken Damansara 2, SS 2/72 Petaling Jaya, Selangor",
  "Website": "https://jaleos.my",
  "SupportEmail": "support@jaleos.my",
  "DocsUrl": "https://docs.jaleos.my"
}
```

### G2. launchSettings.json — JaleOS env vars for dev
**File:** `src/MotoRent.Server/Properties/launchSettings.json`
Add MOTO_ environment variables to the profile:
```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "MOTO_BaseUrl": "https://localhost:7103",
  "MOTO_ApplicationName": "JaleOS",
  "MOTO_Country": "MY",
  "MOTO_Languages": "en,ms",
  "MOTO_BaseDomain": "jaleos.my"
}
```

### G3. service-worker.js — minor cleanup
**File:** `src/MotoRent.Server/wwwroot/service-worker.js`
- Line 2: Change cache name to generic `'app-cache-v1'` (not operator-specific)
- Line 113: Push title reads from push payload data, default fallback is acceptable as generic

---

## Not Changed (intentional)
- **JS object names** (`window.MotoRentPwa`, `window.MotoRentCamera`, `window.MotoRent`): Internal technical names referenced from C# Blazor interop. Renaming requires coordinated cross-assembly changes with no user-visible benefit.
- **Assembly names** (`MotoRent.Client`, `MotoRent.Server.styles.css`): Build artifacts, not user-facing.
- **`appsettings.json` connection string / RabbitMQ names**: These are infrastructure config, already overridable via env vars. Leave as-is for local dev defaults; operators set via MOTO_ env vars in production.
- **pwa.js DotNet.invokeMethodAsync**: Must match actual assembly name.

---

## Verification
1. `dotnet build` — zero errors
2. Grep for remaining hardcoded operator names:
   - `grep -ri "SafeNGo" src/ --include="*.razor" --include="*.cs"` — should be zero
   - `grep -ri "motorent\.io\|motorent\.com\|motorent\.co\.th" src/ --include="*.razor"` — should be zero
   - `grep -ri "motorent" src/ --include="*.razor" --include="*.resx"` — remaining should only be assembly/namespace references
3. Run `dotnet watch --project src/MotoRent.Server` and verify:
   - App title shows "JaleOS" (not MotoRent/SafeNGo)
   - Language switcher shows English + Bahasa Melayu
   - Contact page shows adam@jaleos.my
   - `/manifest.json` returns JaleOS branding
   - Currency formatting shows MYR (not THB)
