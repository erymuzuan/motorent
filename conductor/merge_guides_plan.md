# Plan: Merge Guides and Create Illustrations

## Objective
Merge the content of `USER_GUIDES` into `user.guides`, ensuring completeness for each role. Generate correct screenshots using `playwright-cli` by impersonating users from KK Car Rentals. Create 3D clay motion style illustrations using `banana-pro-2` for each role's tasks and job workflow. Update `Learn.razor` and related assets accordingly.

## Key Files & Context
- **Source Guides**: `USER_GUIDES/MANAGER_GUIDE.md`, `MECHANIC_GUIDE.md`, `STAFF_GUIDE.md`, `TOURIST_GUIDE.md`
- **Target Guides**: `user.guides/01-orgadmin-quickstart.md`, `02-staff-quickstart.md`, `03-mechanic-quickstart.md`, `04-shopmanager-quickstart.md`, `05-tourist-guide.md`
- **Frontend Page**: `src/MotoRent.Client/Pages/Learn.razor`
- **Manifest Script**: `scripts/generate-docs-manifest.ps1`

## Implementation Steps

### Phase 1: Merge and Update Guides Content
1. **OrgAdmin / ShopManager**: Merge `MANAGER_GUIDE.md` into `01-orgadmin-quickstart.md` and `04-shopmanager-quickstart.md`. Ensure role-specific tasks are properly divided.
2. **Staff**: Merge `STAFF_GUIDE.md` into `02-staff-quickstart.md`.
3. **Mechanic**: Merge `MECHANIC_GUIDE.md` into `03-mechanic-quickstart.md`.
4. **Tourist**: Merge `TOURIST_GUIDE.md` into `05-tourist-guide.md`.

### Phase 2: Generate Screenshots
1. Use `playwright-cli` to navigate to the MotoRent application.
2. Log in and use the SuperAdmin Impersonation feature (`/super-admin/impersonate`) to impersonate users from **KK Car Rentals**.
3. Take updated screenshots for the Dashboard, Fleet, Check-In, Accidents, etc., and save them to `user.guides/images/`.

### Phase 3: Create 3D Clay Motion Illustrations
1. Use `banana-pro-2` to generate 3D clay motion style illustrations for each role's tasks:
    - OrgAdmin/Manager: Dashboard and metrics
    - Staff: Check-in process
    - Mechanic: Fleet maintenance
    - Tourist: Booking a vehicle
2. Download and move these images to `user.guides/images/`.

### Phase 4: Finalize and Verify
1. Update `Learn.razor` and related components to ensure the updated documentation structure is correctly displayed.
2. Re-run `scripts/generate-docs-manifest.ps1` to update the manifests for `user.guides` and `user.guides.ms`.

## Verification
- Verify all guides load properly in the app with the new screenshots and illustrations.
