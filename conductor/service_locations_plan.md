# Plan: Update Service Locations Guide

## Objective
Update the "Service Locations Guide" (`user.guides/11-service-locations-guide.md` and create `user.guides.ms/11-service-locations-guide.md`) using a "WHY-First" approach. Generate a 3D clay-motion illustration using `banana-pro-2` to visually explain the concept. Capture accurate screenshots by impersonating a user from KK Car Rentals using `playwright-cli`.

## Key Files
- `user.guides/11-service-locations-guide.md`
- `user.guides.ms/11-service-locations-guide.md`
- `src/MotoRent.Client/Pages/Learn.razor` (ensure it links properly via manifest)

## Implementation Steps

### Phase 1: Content Update (WHY-First)
1. **Motivation (WHY)**: Explain that offering flexible pickup and drop-off points (like hotels or airports) is a massive competitive advantage in tourist areas like Sabah or Langkawi. It allows businesses to charge premium "Drop-off Fees" (one-way fees), turning a logistical challenge into a new revenue stream, while also keeping track of exactly where every vehicle is parked.
2. **Stories**:
   - *Story 1*: A tourist picks up a car at the KK City Office but wants to drop it off at Kota Kinabalu International Airport (BKI) at 6 AM. The system automatically adds a RM50 drop-off fee.
   - *Story 2*: A customer's car breaks down and is swapped at a partner hotel. The system tracks the broken car's location at the hotel until the mechanic retrieves it.
3. **Workflow (HOW)**: Detail configuring locations, setting drop-off fees, and selecting locations during Check-In/Check-Out.
4. Draft both English and Bahasa Melayu (MS) versions.

### Phase 2: Generate Illustration
1. Use `banana-pro-2` to generate a 3D clay-motion style illustration showing a tourist returning a rental car at a Malaysian airport terminal (like KLIA or Langkawi Airport) to a friendly rental staff member, with a "Drop-off Fee: RM50" receipt visible.

### Phase 3: Capture Screenshots
1. Use `playwright-cli` to navigate to the local application (`https://localhost:7104`).
2. Impersonate `ahmad@kkcar.my` (OrgAdmin) at KK Car Rentals.
3. Navigate to **Settings > Service Locations** (`/settings/service-locations`).
4. Capture a screenshot and save it to `user.guides/images/11-service-locations.png`.
5. Navigate to a Check-in or Check-out dialog to show location selection and capture a screenshot to `user.guides/images/11-location-selection.png`.

### Phase 4: Finalize
1. Embed the illustration and screenshots into the updated Markdown guides.
2. Copy the new images to `src/MotoRent.Server/wwwroot/images/`.
3. Update the `manifest.json` files by running the PowerShell script.
