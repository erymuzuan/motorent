# Implementation Plan: MotoRent SaaS Signup & Web App Conversion

This plan outlines the conversion of the static marketing website into a functional SaaS onboarding flow for MotoRent, including authentication, multi-step onboarding, and payment integration.

## Phase 1: Authentication & Domain Extensions [checkpoint: 1e4b7b3]

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

- [ ] Task: Conductor - User Manual Verification 'Phase 1: Authentication & Domain Extensions' (Protocol in workflow.md)

## Phase 2: Onboarding Wizard Backend & API [checkpoint: ]

### Task 1: Create Organization Setup Service
- [ ] Task: Implement a service to handle the multi-step onboarding logic.
    - [ ] Write unit tests for organization/shop creation with initial fleet data.
    - [ ] Implement `IOnboardingService` to create `Organization`, `Shop`, and initial `Vehicle` records.
    - [ ] Implement logic to automatically grant a 30-day Pro trial upon successful signup.

### Task 2: Onboarding API Endpoints
- [ ] Task: Create REST endpoints for the onboarding wizard.
    - [ ] Write unit tests for the onboarding controller (validation, success/error cases).
    - [ ] Implement POST endpoints for each step of the wizard (Shop Info, Fleet, Plan).

- [ ] Task: Conductor - User Manual Verification 'Phase 2: Onboarding Wizard Backend & API' (Protocol in workflow.md)

## Phase 3: Blazor Onboarding UI & Localization [checkpoint: ]

### Task 1: Create Multi-Step Onboarding Wizard
- [ ] Task: Build the 4-step signup wizard in Blazor.
    - [ ] Implement Step 1: Social Auth buttons (Google/LINE).
    - [ ] Implement Step 2: Shop Details (with `datalist` for Thai locations).
    - [ ] Implement Step 3: Initial Inventory/Fleet setup.
    - [ ] Implement Step 4: Plan Selection (Free/Pro/Ultra).

### Task 2: Thai & English Localization
- [ ] Task: Implement full localization for the onboarding flow.
    - [ ] Add Thai and English resource files for all onboarding labels and messages.
    - [ ] Implement language toggle logic that updates the organization setting.

- [ ] Task: Conductor - User Manual Verification 'Phase 3: Blazor Onboarding UI & Localization' (Protocol in workflow.md)

## Phase 4: Payment & Billing Integration [checkpoint: ]

### Task 1: PromptPay QR Integration
- [ ] Task: Implement dynamic PromptPay QR code generation.
    - [ ] Write unit tests for QR payload generation.
    - [ ] Implement a service to generate PromptPay QR images based on subscription fees.

### Task 2: Local & International Gateway Integration
- [ ] Task: Integrate Omise/GB Prime Pay and Stripe.
    - [ ] Write unit tests for payment webhook handling.
    - [ ] Implement Omise/GB Prime Pay integration for local cards.
    - [ ] Implement Stripe integration for international cards.
    - [ ] Create a "Billing Dashboard" component in the app.

- [ ] Task: Conductor - User Manual Verification 'Phase 4: Payment & Billing Integration' (Protocol in workflow.md)

## Phase 5: Marketing Site Migration & Final Integration [checkpoint: ]

### Task 1: Convert Static Pages to Blazor Components
- [ ] Task: Migrate `website/index.html` and other pages to the Blazor Server/WASM host.
    - [ ] Migrate `style.css` and `variables.css` into the main application styles.
    - [ ] Convert static HTML sections into Blazor components.
    - [ ] Update "Sign Up" and "Get Started" buttons to link to the new onboarding flow.

### Task 2: Final Verification & Cleanup
- [ ] Task: Perform end-to-end verification of the signup-to-app flow.
    - [ ] Verify mobile responsiveness on iPhone/Android browsers.
    - [ ] Ensure all assets (images/logos) are correctly served from the new app structure.

- [ ] Task: Conductor - User Manual Verification 'Phase 5: Marketing Site Migration & Final Integration' (Protocol in workflow.md)
