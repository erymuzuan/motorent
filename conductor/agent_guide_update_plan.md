# Plan: Update Agent Management Guide

## Objective
Update the "Agent management guide" (`user.guides.ms/10-agent-management-guide.md` and `user.guides/10-agent-management-guide.md`) to include a "WHY-First" motivation section. Generate a 3D clay-motion illustration using `banana-pro-2` to visually explain the concept. Capture accurate screenshots by impersonating a user from KK Car Rentals using `playwright-cli` (`/browser-testing`).

## Key Files
- `user.guides.ms/10-agent-management-guide.md`
- `user.guides/10-agent-management-guide.md`
- `src/MotoRent.Client/Pages/Learn.razor` (to ensure it links correctly)

## Implementation Steps

### Phase 1: Content Update (WHY-First)
1. **Motivation (WHY)**: Explain how car rental businesses in Malaysia rely heavily on local partners (hotels, tour guides, street agents). Without a proper system, tracking who brought which customer and how much commission is owed leads to disputes and lost margins.
2. **Workflow (HOW)**: Detail the Commission workflow (Pending -> Eligible -> Approved -> Paid) and how it integrates with the Till system.
3. Update both the English and Bahasa Melayu versions of the guide.

### Phase 2: Generate Illustration
1. Use `banana-pro-2` to generate a 3D clay-motion illustration showing a car rental owner shaking hands with a hotel concierge (agent), with a booking confirmation and MYR commission visible.

### Phase 3: Capture Screenshots
1. Use `playwright-cli` to navigate to the local application.
2. Impersonate `ahmad@kkcar.my` (OrgAdmin) at KK Car Rentals.
3. Navigate to the **Agents** and **Agent Commissions** pages.
4. Capture screenshots and save them to `user.guides/images/10-agents-list.png` and `user.guides/images/10-agent-commissions.png`.

### Phase 4: Finalize
1. Embed the illustration and screenshots into the updated Markdown guides.
2. Ensure everything renders correctly.
