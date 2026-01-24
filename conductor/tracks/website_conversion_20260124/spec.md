# Specification: MotoRent SaaS Signup & Web App Conversion

## Overview
This track involves converting the existing static marketing website (`website/**`) into a functional web application that allows motorbike rental operators in Thailand to sign up for the MotoRent SaaS platform. The goal is to provide a seamless onboarding experience tailored to the local market (Phuket, Samui, Bangkok, etc.).

## Functional Requirements

### 1. User Authentication
- Implement social signup and login using **Google** and **LINE**.
- Store user profiles and link them to their newly created organizations (shops).

### 2. Onboarding Workflow
- **Step 1: Account Creation:** Social login via Google or LINE.
- **Step 2: Shop Details:**
    - Shop Name (Text)
    - Location (Free-text with `datalist` of Thai tourist spots: Phuket, Samui, Krabi, Pattaya, Chiang Mai, Bangkok, etc.)
    - Phone Number (Tel)
    - Preferred Language (Toggle: Thai/English)
- **Step 3: Initial Inventory:**
    - Quick input for initial fleet size and common motorbike types.
- **Step 4: Plan Selection:**
    - Choice of Free, Pro, or Ultra tiers.
    - Automatic application of a **30-day Pro trial** upon completion of signup.

### 3. Subscription & Payment Integration
- Implement a billing dashboard within the app.
- **Payment Channels:**
    - **PromptPay:** Dynamic QR code generation for direct transfers.
    - **Local Gateways:** Integration with Omise or GB Prime Pay for local card processing.
    - **Stripe:** International card processing.
- Handle subscription lifecycle (active, trialing, past_due, canceled).

### 4. UI/UX Conversion
- Migrate the aesthetic of the static website (`style.css`, `variables.css`) into the Blazor WebAssembly frontend.
- Ensure the signup process is mobile-friendly, as many operators may use smartphones.

## Non-Functional Requirements
- **Localization:** Full support for Thai and English.
- **Security:** Secure handling of OAuth tokens and payment webhooks.
- **Performance:** Fast initial load for the landing page and signup flow.

## Acceptance Criteria
- [ ] Users can sign up and log in via Google and LINE.
- [ ] A new organization and shop are correctly created in the database upon signup.
- [ ] The onboarding wizard collects all required shop and fleet information.
- [ ] Users are automatically enrolled in a 30-day Pro trial.
- [ ] The billing dashboard correctly displays the current plan and supports payment via PromptPay, local gateways, and Stripe.
- [ ] The UI matches the branding and style of the original static website.

## Out of Scope
- Advanced shop management features (these are covered by existing/other tracks).
- Domain mapping for Ultra customers (to be handled in a separate track).
