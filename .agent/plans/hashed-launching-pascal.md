# Plan: Test Tenant Operations with PostgreSQL

## Context
Super admin dashboard testing is complete (commit `b04904e`). Core entity persistence (Organization, User) and all super admin pages work with PostgreSQL. Now we need to test **tenant-level operations** by impersonating the org admin of the newly created "PhuketBeach" tenant and exercising the full rental workflow: staff, fleet, vehicles, insurance, accessories, bookings, and check-in.

This tests `PgJsonRepository<T>` (tenant entities with RLS) and `PgPersistence` (tenant batch writes) — the counterpart to the Core repositories already validated.

## Prerequisites
- Server running on port 7105 with env vars (background task `b5068ed` may still be running)
- Authenticated as super admin (`erymuzuan@gmail.com`)
- Organization "PhuketBeach" with admin user `admin@phuketbeach.test` already exists
- 65 tenant tables already provisioned in database

## Approach

### Step 1: Start Server & Impersonate
- Ensure `dotnet watch` is running on port 7105
- Navigate to `/super-admin/impersonate`
- Find user `admin@phuketbeach.test` / account `PhuketBeach`
- Click impersonate — lands on `/dashboard`
- Verify tenant dashboard loads (RLS `app.current_tenant` = PhuketBeach)

### Step 2: Create a Shop (prerequisite for all other entities)
- Route: `/shops/create`
- Required: Name, Province, Location, Address, Phone
- Example: "Patong Beach Shop", Phuket, Patong

### Step 3: Create 2 Staff Members
- Route: `/settings/users`
- Create staff via dialog/form
- Staff 1: "Somchai", role Staff
- Staff 2: "Nattaya", role Mechanic
- These are User entities in Core schema with UserAccount linked to PhuketBeach

### Step 4: Create Fleet Models
- Route: `/fleet-models/new`
- **Motorbike models** (2-3):
  - Honda PCX 160, DailyRate ~300 THB
  - Yamaha NMAX 155, DailyRate ~250 THB
- **Car models** (2):
  - Toyota Yaris, DailyRate ~1200 THB
  - Honda City, DailyRate ~1500 THB
- Set VehicleType, Brand, Model, Year, DailyRate, DepositAmount

### Step 5: Create Vehicles
- Routes: `/vehicles/motorbike/new`, `/vehicles/car/new`
- Link each to a FleetModel and HomeShop
- Create 3-4 motorbikes (different plates) and 2 cars
- Each needs: LicensePlate, FleetModelId, HomeShopId
- Status defaults to "Available"

### Step 6: Create Insurance Packages
- Route: `/insurance` (dialog-based create)
- Create 2-3 packages:
  - Basic: 100 THB/day, 50K coverage
  - Premium: 250 THB/day, 200K coverage

### Step 7: Create Accessories
- Route: `/accessories` (dialog-based create)
- Create 3-4 accessories:
  - Helmet (included free), Phone Holder, Rain Gear, Child Seat
- Set ShopId, DailyRate, QuantityAvailable

### Step 8: Create Bookings
- Route: `/bookings/create`
- Create 2-3 bookings with different vehicles
- Fill: CustomerName, CustomerPhone, dates, preferred shop, vehicle selection
- Status should be Pending/Confirmed

### Step 9: Check In Bookings
- Route: `/rentals/checkin/{BookingRef}`
- Check in 1-2 of the bookings
- Multi-step flow: SelectRenter → SelectVehicle → PreInspection → ConfigureRental → CollectDeposit → AgreementSignature
- Verify Rental entity created with correct Status

### Fix Issues As Found
- Watch server output for PostgreSQL errors (missing columns, type mismatches, RLS issues)
- Common patterns to fix:
  - Missing computed columns in tenant table SQL (like LogEntry Status issue)
  - JSONB parameter type issues (already fixed in Core, tenant repos should be OK)
  - Query provider generating wrong SQL

## Key Files (if fixes needed)
- `src/MotoRent.PostgreSqlRepository/PgJsonRepository.cs` — Tenant entity CRUD (already uses NpgsqlDbType.Jsonb)
- `src/MotoRent.PostgreSqlRepository/PgPersistence.cs` — Tenant batch writes
- `src/MotoRent.PostgreSqlRepository/DbConnectionInterceptor.cs` — RLS tenant setting
- `database/tables/MotoRent.*.sql` — Tenant table schemas (computed columns)

## Progress (as of 2026-02-15)

### Completed
- **Step 1**: Impersonation as `admin@phuketbeach.test` — tenant dashboard loads with RLS
- **Step 2**: Shop created — "Patong Beach Shop", Phuket
- **Step 4**: Fleet Models created (4 total):
  - Honda PCX 160 (Motorbike, 300 THB/day)
  - Yamaha NMAX 155 (Motorbike, 250 THB/day)
  - Toyota Yaris (Car, 1200 THB/day)
  - Honda City (Car, 1500 THB/day)
- **Step 5**: Vehicles created (3 total):
  - Honda PCX 160 — plate 1กก 1234
  - Yamaha NMAX 155 — plate 2ขข 5678
  - Toyota Yaris — plate กต 9012
- **Step 6**: Insurance created (2 packages):
  - Basic: 100 THB/day, 50K coverage, 5K deductible
  - Premium: 250 THB/day, 200K coverage, 0 deductible
- **Step 7**: Accessories created (2 items):
  - Helmet (Free, qty 10, included with rental)
  - Phone Holder (50 THB/day, qty 5)

### Fixes Committed (commit `1535c77`, pushed to `origin/postgres`)
- **PgQueryFormatter.cs**: Fixed FROM clause quoting — subqueries no longer wrapped in double-quotes
- **PgJsonRepository.cs**: LoadAsync/GetReaderAsync use `SELECT *` + outer wrapping instead of inline replacement
- **PgJsonRepository.aggregate.cs**: All 13 aggregate methods use wrapping pattern for subquery compatibility

### Remaining
- **Step 3**: Create Staff Members (skipped, not blocking)
- **Step 8**: Create Bookings (browser disconnected before starting)
- **Step 9**: Check-In Bookings (multi-step rental workflow)

### Notes
- No PostgreSQL errors during Steps 2, 4-7 — all tenant CRUD works with RLS
- Browser extension (Claude in Chrome) disconnected; needs reconnection to continue
- Server runs on port 7105 with `dotnet watch`

## Verification
1. Impersonation works — tenant dashboard loads with RLS isolation
2. Shop created and visible in list
3. Staff users created with correct roles
4. Fleet models created for both motorbikes and cars
5. Vehicles created and linked to fleet models, show "Available" status
6. Insurance packages and accessories visible in lists
7. Bookings created with correct status and customer info
8. Check-in flow completes — Rental entity created, vehicle status changes to "Rented"
9. No unhandled PostgreSQL errors in server output throughout
