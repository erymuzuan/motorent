# Implementation Plan: MotoRent SaaS Signup & Web App Conversion

This plan outlines the conversion of the static marketing website into a functional SaaS onboarding flow for MotoRent, including authentication, multi-step onboarding, and payment integration.

## Phase 1: Authentication & Domain Extensions [checkpoint: a81a7f9]

### Task 1: Update Domain Models for Subscription & Onboarding (COMPLETED) 1456ace
- [x] Task: Extend `Organization` and `User` entities for SaaS signup. 1456ace
    - [x] Write unit tests for trial period calculation and subscription status logic.
    - [x] Add `TrialEndDate`, `SubscriptionPlan` (Free/Pro/Ultra), and `PreferredLanguage` to `Organization`.
    - [x] Add OAuth provider fields to the `User` entity for Google and LINE.

### Task 2: Implement OAuth Authentication Services (COMPLETED) a3c80bc
- [x] Task: Configure and implement Google and LINE authentication. a3c80bc
    - [x] Write unit tests for the OAuth response handling and user mapping.
    - [x] Implement `IAuthenticationService` methods for Google and LINE.
    - [x] Setup API endpoints to handle OAuth callbacks and initiate session creation.

- [x] Task: Conductor - User Manual Verification 'Phase 1: Authentication & Domain Extensions' (Protocol in workflow.md) a81a7f9

## Phase 2: Onboarding Wizard Backend & API [checkpoint: ddb44cd]

### Task 1: Create Organization Setup Service (COMPLETED) c460073
- [x] Task: Implement a service to handle the multi-step onboarding logic. c460073
    - [x] Write unit tests for organization/shop creation with initial fleet data.
    - [x] Implement `IOnboardingService` to create `Organization`, `Shop`, and initial `Vehicle` records.
    - [x] Implement logic to automatically grant a 30-day Pro trial upon successful signup.

### Task 2: Onboarding API Endpoints (COMPLETED) a87c1b8
- [x] Task: Create REST endpoints for the onboarding wizard. a87c1b8
    - [x] Write unit tests for the onboarding controller (validation, success/error cases).
    - [x] Implement POST endpoints for each step of the wizard (Shop Info, Fleet, Plan).

- [x] Task: Conductor - User Manual Verification 'Phase 2: Onboarding Wizard Backend & API' (Protocol in workflow.md) ddb44cd

## Phase 3: Blazor Onboarding UI & Localization [checkpoint: 267dcf4]

### Task 1: Create Multi-Step Onboarding Wizard (COMPLETED) 686f3e4
- [x] Task: Build the 4-step signup wizard in Blazor. 686f3e4
    - [x] Implement Step 1: Social Auth buttons (Google/LINE).
    - [x] Implement Step 2: Shop Details (with `datalist` for Thai locations).
    - [x] Implement Step 3: Initial Inventory/Fleet setup.
    - [x] Implement Step 4: Plan Selection (Free/Pro/Ultra).

### Task 2: Thai & English Localization (COMPLETED)
- [x] Task: Implement full localization for the onboarding flow.

- [x] Task: Conductor - User Manual Verification 'Phase 3: Blazor Onboarding UI & Localization' (Protocol in workflow.md) 267dcf4

## Phase 4: Payment & Billing Integration [checkpoint: 61d8cbd]

### Task 1: Fiuu Payment Gateway Integration
- [x] Task: Integrate Fiuu (formerly Razer Merchant Services) for all payment methods.
    - [x] Write unit tests for Fiuu payment webhook handling (IPN) and signature verification. c59caeb
    - [x] Implement Fiuu service for generating payment requests (Credit Card & PromptPay QR). dca0195
    - [x] Implement Fiuu IPN (Instant Payment Notification) endpoint to handle payment success/failure callbacks. 3478ab2
    - [x] **Requirement:** Handle 0 THB transactions for 'Free' plan (process as 0-value cart item or registration). 50f15a7
    - [x] **Requirement:** Update `Pricing.razor` to redirect to `/signup?plan={planId}`. 50f15a7
    - [x] **Requirement:** Update `OnboardingWizard` to read `plan` query parameter and capture email (via Auth step) before initializing Fiuu payment. 50f15a7
    - [x] Create a "Billing Dashboard" component for managing subscriptions and viewing payment history. 50f15a7

- [x] Task: Conductor - User Manual Verification 'Phase 4: Payment & Billing Integration' (Protocol in workflow.md) 61d8cbd

## Phase 5: Marketing Site Migration & Final Integration [checkpoint: ]

### Task 1: Convert Static Pages to Blazor Components
- [x] Task: Migrate `website/index.html` and other pages to the Blazor Server/WASM host. b827859
    - [x] Migrate `style.css` and `variables.css` into the main application styles.
    - [x] Convert static HTML sections into Blazor components.
    - [x] Update "Sign Up" and "Get Started" buttons to link to the new onboarding flow. b827859

### Task 2: Final Verification & Cleanup
- [x] Task: Perform end-to-end verification of the signup-to-app flow.
    - [x] Verify mobile responsiveness on iPhone/Android browsers.
    - [x] Ensure all assets (images/logos) are correctly served from the new app structure.

- [ ] Task: Conductor - User Manual Verification 'Phase 5: Marketing Site Migration & Final Integration' (Protocol in workflow.md)
