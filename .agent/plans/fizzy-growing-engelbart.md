# Phase 3: Blazor Onboarding UI & Localization

## Overview
Build a 4-step onboarding wizard for new SaaS signups, following the existing CheckIn wizard pattern.

## User Requirements
- **OAuth Providers:** Google + LINE (both required)
- **Post-signup redirect:** Quick Start Guide page
- **Plan Pricing:** Free (฿0), Pro (฿999/mo), Ultra (฿1,499/mo)

## Task Breakdown & Dependencies

```
Task 1: OnboardingWizard.razor (Container)
    │
    ├──► Task 2: Step 1 - AuthStep.razor (Google + LINE OAuth)
    │       └── Depends on: Task 1
    │
    ├──► Task 3: Step 2 - ShopDetailsStep.razor
    │       └── Depends on: Task 1
    │
    ├──► Task 4: Step 3 - FleetSetupStep.razor
    │       └── Depends on: Task 1
    │
    └──► Task 5: Step 4 - PlanSelectionStep.razor
            └── Depends on: Task 1

Task 6: Quick Start Guide Page
    └── Depends on: Task 5 (post-signup redirect target)

Task 7: Localization (.resx files)
    └── Depends on: Tasks 1-6 (all UI complete)

Task 8: Integration & Testing
    └── Depends on: Task 7
```

---

## Task 1: OnboardingWizard Container Component

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/OnboardingWizard.razor`
- `src/MotoRent.Client/Pages/Onboarding/OnboardingWizard.razor.cs`

**Implementation:**
```csharp
@page "/signup"
@page "/onboarding"
@inherits LocalizedComponentBase<OnboardingWizard>

// Step indicator (1-4)
// @switch (m_activeStep) for step content
// Navigation buttons (Previous/Next/Complete)
// OnboardingRequest state object passed to child steps
```

**Pattern reference:** `/src/MotoRent.Client/Pages/Rentals/CheckIn.razor`

**State to track:**
- `m_activeStep` (int 0-3)
- `m_onboardingRequest` (OnboardingRequest DTO from Phase 2)
- `m_isAuthenticated` (bool - controls step 1 completion)

---

## Task 2: Step 1 - AuthStep (Google + LINE OAuth)

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/Steps/AuthStep.razor`
- `src/MotoRent.Client/Pages/Onboarding/Steps/AuthStep.razor.cs`

**Files to modify:**
- `src/MotoRent.Server/Program.cs` - Add LINE OAuth configuration

**Implementation:**
- Google OAuth button (existing - configured in Program.cs)
- LINE OAuth button (LINE Login v2.1 API)
- Branded social buttons with provider logos
- On success: populate `OnboardingRequest.Provider`, `ProviderId`, `UserName`, `Email`, `FullName`

**LINE OAuth Setup:**
```csharp
// In Program.cs - add LINE authentication
// Secrets from environment variables (never in config files)
authBuilder.AddOAuth("Line", options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("MOTO_LineClientId")
        ?? throw new InvalidOperationException("MOTO_LineClientId not set");
    options.ClientSecret = Environment.GetEnvironmentVariable("MOTO_LineClientSecret")
        ?? throw new InvalidOperationException("MOTO_LineClientSecret not set");
    options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
    options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
    options.UserInformationEndpoint = "https://api.line.me/v2/profile";
    options.CallbackPath = "/signin-line";
    options.Scope.Add("profile");
    options.Scope.Add("openid");
    options.Scope.Add("email");
});
```

**Environment Variables Required:**
| Variable | Description |
|----------|-------------|
| `MOTO_LineClientId` | LINE Login Channel ID |
| `MOTO_LineClientSecret` | LINE Login Channel Secret |

**Depends on:** Task 1 (container)

**Pattern reference:**
- `/src/MotoRent.Client/Controls/ProviderPicker.razor` (provider icons)
- Existing OAuth flow in `/src/MotoRent.Server/Program.cs`

---

## Task 3: Step 2 - ShopDetailsStep

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/Steps/ShopDetailsStep.razor`
- `src/MotoRent.Client/Pages/Onboarding/Steps/ShopDetailsStep.razor.cs`

**Implementation:**
- Shop name input (required)
- Location datalist (Thai tourist areas - Phuket, Krabi, Koh Samui, etc.)
- Phone number input
- Preferred language selector (Thai/English)
- Form validation

**Fields populated:** `ShopName`, `Location`, `Phone`, `PreferredLanguage`

**Depends on:** Task 1 (container)

**Pattern reference:** `/src/MotoRent.Client/Pages/ShopForm.razor` (location datalist lines 89-120)

---

## Task 4: Step 3 - FleetSetupStep

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/Steps/FleetSetupStep.razor`
- `src/MotoRent.Client/Pages/Onboarding/Steps/FleetSetupStep.razor.cs`

**Implementation:**
- Vehicle type selector (Motorbike/Car/JetSki/Boat/Van)
- Quantity input per type
- Brand/Model suggestions (optional)
- Add/remove fleet items
- Summary display

**Fields populated:** `Fleet` (List<InitialFleetDto>)

**Depends on:** Task 1 (container)

**Pattern reference:** `/src/MotoRent.Client/Pages/Vehicles/VehicleForm.razor` (brand datalist)

---

## Task 5: Step 4 - PlanSelectionStep

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/Steps/PlanSelectionStep.razor`
- `src/MotoRent.Client/Pages/Onboarding/Steps/PlanSelectionStep.razor.cs`

**Implementation:**
- 3 pricing cards with Tabler card styling
- Feature comparison matrix
- "30-day Pro trial" highlight badge
- Plan selection (card click or radio)
- Terms acceptance checkbox
- Submit button → calls `/api/onboarding/submit`
- On success: redirect to `/quick-start`

**Pricing (THB/month):**
| Plan | Price | Features |
|------|-------|----------|
| Free | ฿0 | 1 shop, 10 vehicles, basic reports |
| Pro | ฿999 | 3 shops, 50 vehicles, full reports, OCR |
| Ultra | ฿1,499 | Unlimited shops/vehicles, white-label, API access |

**Fields populated:** `Plan` (SubscriptionPlan enum)

**Depends on:** Task 1 (container)

**API endpoint:** `POST /api/onboarding/submit` (exists from Phase 2)

---

## Task 6: Quick Start Guide Page

**Files to create:**
- `src/MotoRent.Client/Pages/Onboarding/QuickStart.razor`
- `src/MotoRent.Client/Pages/Onboarding/QuickStart.razor.cs`

**Implementation:**
- Welcome message with shop name
- 4-5 quick action cards:
  1. "Add your first vehicle" → `/vehicles/create`
  2. "Configure shop hours" → `/shops/{id}/edit`
  3. "Start a rental" → `/rentals/check-in`
  4. "View dashboard" → `/dashboard`
- Progress indicator (completed steps)
- "Skip tutorial" option

**Route:** `/quick-start`

**Depends on:** Task 5 (redirect target after signup)

---

## Task 7: Localization Files

**Files to create (4 files per component × 6 components = 24 files):**

```
Resources/Pages/Onboarding/
├── OnboardingWizard.resx / .en.resx / .th.resx / .ms.resx
├── QuickStart.resx / .en.resx / .th.resx / .ms.resx
└── Steps/
    ├── AuthStep.resx / .en.resx / .th.resx / .ms.resx
    ├── ShopDetailsStep.resx / .en.resx / .th.resx / .ms.resx
    ├── FleetSetupStep.resx / .en.resx / .th.resx / .ms.resx
    └── PlanSelectionStep.resx / .en.resx / .th.resx / .ms.resx
```

**Key translations needed:**
| Key | English | Thai |
|-----|---------|------|
| WelcomeTitle | Welcome to MotoRent | ยินดีต้อนรับสู่ MotoRent |
| ShopDetails | Shop Details | รายละเอียดร้าน |
| FleetSetup | Initial Fleet | ยานพาหนะเริ่มต้น |
| SelectPlan | Choose Your Plan | เลือกแผนของคุณ |
| ContinueWithGoogle | Continue with Google | ดำเนินการต่อด้วย Google |
| ContinueWithLine | Continue with LINE | ดำเนินการต่อด้วย LINE |
| FreeTrial | 30-Day Free Pro Trial | ทดลองใช้ Pro ฟรี 30 วัน |
| QuickStartTitle | Get Started | เริ่มต้นใช้งาน |
| AddFirstVehicle | Add your first vehicle | เพิ่มยานพาหนะแรก |
| ConfigureShop | Configure shop hours | ตั้งค่าเวลาทำการ |

**Depends on:** Tasks 1-6

---

## Task 8: Integration & Testing

**Subtasks:**
1. Wire up navigation from marketing site "Sign Up" buttons to `/signup`
2. Handle OAuth callback redirect back to wizard
3. Test full flow: Auth → Shop → Fleet → Plan → Quick Start
4. Mobile responsiveness verification
5. Error handling for API failures

**Depends on:** Task 7

---

## Critical Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Server/Program.cs` | Add LINE OAuth configuration (env vars) |
| `src/MotoRent.Client/_Imports.razor` | Add `@using MotoRent.Client.Pages.Onboarding` |

## Environment Variables (All Secrets)

| Variable | Description | Required |
|----------|-------------|----------|
| `MOTO_LineClientId` | LINE Login Channel ID | Yes |
| `MOTO_LineClientSecret` | LINE Login Channel Secret | Yes |
| `MOTO_GoogleClientId` | Google OAuth Client ID | Existing |
| `MOTO_GoogleClientSecret` | Google OAuth Client Secret | Existing |

## API Endpoints Used (from Phase 2)

- `GET /api/onboarding/check-availability?accountNo=xxx`
- `POST /api/onboarding/submit` (body: OnboardingRequest)

## Verification Plan

1. **Build:** `dotnet build` - ensure no compilation errors
2. **Unit tests:** Validate wizard step transitions
3. **Manual testing:**
   - Complete signup with Google OAuth
   - Complete signup with LINE OAuth
   - Verify organization created in database
   - Verify user redirected to Quick Start guide
   - Test language toggle (EN ↔ TH)
4. **Mobile:** Test on iPhone Safari and Android Chrome

---

## Recommended Implementation Order

1. **Task 1** - OnboardingWizard container (scaffold)
2. **Tasks 2-5** - All steps (can parallelize 3-5 after 2)
3. **Task 6** - Quick Start Guide page
4. **Task 7** - Localization after UI stabilized
5. **Task 8** - Integration testing
