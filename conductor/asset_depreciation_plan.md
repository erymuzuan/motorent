# Plan: Update Asset Depreciation Guide

## Objective
Update the "Asset Depreciation Guide" (`user.guides.ms/07-asset-depreciation-guide.md` and `user.guides/07-asset-depreciation-guide.md`) to include a "WHY-First" motivation section. Generate a 3D clay-motion illustration using `banana-pro-2` to visually explain the concept. Capture accurate screenshots by impersonating a user from KK Car Rentals using `playwright-cli`.

## Key Files
- `user.guides.ms/07-asset-depreciation-guide.md`
- `user.guides/07-asset-depreciation-guide.md`

## Implementation Steps

### Phase 1: Content Update (WHY-First)
1. **Motivation (WHY)**: Explain that vehicles (like Perodua Bezza or Yamaha NMAX) are depreciating assets. If owners only look at their bank balance, they might mistake gross revenue for profit and fail to save capital for fleet replacement. Tracking depreciation reveals the *true* ROI and Net Profit of each vehicle.
2. **Workflow (HOW)**: Detail the Asset Dashboard, Depreciation Methods (Straight Line, Day Out of Door), and Profitability reports.
3. Update both the English and Bahasa Melayu versions of the guide.

### Phase 2: Generate Illustration
1. Use `banana-pro-2` to generate a 3D clay-motion illustration showing a Malaysian car rental owner analyzing a declining value chart of a car, realizing the true cost of their fleet.

### Phase 3: Capture Screenshots
1. Use `playwright-cli` to navigate to the local application (`https://localhost:7104`).
2. Impersonate `ahmad@kkcar.my` (OrgAdmin) at KK Car Rentals.
3. Navigate to **Finance > Asset Dashboard** (`/finance/asset-dashboard`), **Finance > Assets** (`/finance/assets`), and **Asset Details** (`/finance/assets/1/details` or similar).
4. Capture screenshots and save them to `user.guides/images/07-asset-dashboard.png` and `user.guides/images/07-asset-details.png`.

### Phase 4: Finalize
1. Embed the illustration and screenshots into the updated Markdown guides.
2. Copy the new images to `src/MotoRent.Server/wwwroot/images/`.
3. Update the `manifest.json` files if necessary.
