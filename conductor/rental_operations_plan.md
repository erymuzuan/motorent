# Plan: Create Complete Rental Operations Guide

## Objective
Create a comprehensive, "WHY-First" guide for core Rental Operations (Booking, Check-In/eKYC, Check-Out, Damage/Fuel tracking) and place it in `user.guides/16-rental-operations.md` and `user.guides.ms/16-rental-operations.md`. Generate a 3D clay-motion illustration using `banana-pro-2` to visually explain the concept. Capture accurate screenshots by impersonating a user from KK Car Rentals using `playwright-cli`.

## Key Files
- `user.guides/16-rental-operations.md`
- `user.guides.ms/16-rental-operations.md`

## Implementation Steps

### Phase 1: Content Update (WHY-First)
1. **Motivation (WHY)**: Explain that the rental desk is the bottleneck of the business. Manual data entry, paper agreements, and fuzzy "he said, she said" damage disputes lead to long queues, angry tourists, and lost money on repairs. JaleOS digitizes this with eKYC (OCR), digital signatures, and strict photographic damage tracking, turning a 15-minute headache into a 3-minute professional experience.
2. **Stories**:
   - *The Check-in Queue*: A scenario during a busy Friday afternoon in KK. Instead of manually typing passport details, staff use the OCR feature to scan documents instantly, add an insurance up-sell, and collect the deposit digitally.
   - *The Fuel/Damage Dispute*: A customer returns a car claiming a dent was "already there." The staff pulls up the Check-in photos on the tablet, proving the dent is new, and accurately charges the repair to the deposit.
3. **Workflow (HOW)**: Detail the Booking (deposits), Check-In Wizard (eKYC, agreements, accessories), Check-Out (billing), and Damage/Cleanliness/Fuel tracking.
4. Draft both English and Bahasa Melayu (MS) versions.

### Phase 2: Generate Illustration
1. Use `banana-pro-2` to generate a 3D clay-motion style illustration showing a smooth rental transaction. A friendly Malay staff member handing a car key to a tourist across a clean desk, with a digital tablet showing a signed agreement and a green checkmark. Style: 3D clay motion, minimal flat design with Tropical Teal (#00897B) and white palette.

### Phase 3: Capture Screenshots
1. Use `playwright-cli` to navigate to the local application (`https://localhost:7104`).
2. Impersonate `rizal@kkcar.my` (Staff) at KK Car Rentals.
3. Navigate to **Rentals > Check-In** and capture screenshots of:
   - The Check-in Wizard (eKYC/Renter step) -> `16-checkin-ekyc.png`
   - Add-ons/Accessories step -> `16-checkin-addons.png`
4. Navigate to an active rental and capture the **Check-Out** dialog (showing Damage/Fuel tracking) -> `16-checkout-billing.png`.

### Phase 4: Finalize
1. Embed the illustration and screenshots into the new Markdown guides.
2. Copy the new images to `src/MotoRent.Server/wwwroot/images/`.
3. Update the `manifest.json` files by running the PowerShell script.
