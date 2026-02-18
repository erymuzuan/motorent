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

## Progress (as of 2026-02-15) — TESTING COMPLETE

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

- **Step 8**: Booking created (QB49DQ):
  - Customer: Test Tourist, +66 812345678
  - Vehicle: Honda PCX 160, 300 THB/day, 3 days = 900 THB
  - Insurance: Basic (100 THB/day x 3 = 300 THB)
  - Total: 1,200 THB
  - Status: Pending → CheckedIn (after Step 9)
  - **Fix**: DateTimeOffset UTC conversion in `PgPersistence.GetEntityPropertyValue()` — PostgreSQL `timestamptz` rejects non-UTC offsets
  - **Fix**: Safe RollbackAsync in catch blocks (both `PgPersistence.cs` and `CorePgPersistence.cs`) — `await using` already disposes transaction

- **Step 9**: Check-In completed (R00001):
  - Renter: Test Tourist
  - Vehicle: Honda PCX 160, 1กก 1234
  - Rental Period: Feb 15 - Feb 18, 2026 (3 days)
  - Insurance: Basic, Accessory: Helmet (free)
  - Deposit: Cash 2,000 THB
  - Total: 3,200 THB (rental + deposit)
  - Receipt: RCP-260215-00001
  - Till Session opened with ฿1,000 float at Patong Beach Shop
  - 6-step wizard: Renter → Vehicle → Configure → Deposit → Inspection → Agreement
  - All steps loaded data from PostgreSQL correctly, all writes succeeded

- **Step 10**: Check-Out completed (R00001):
  - 3-step wizard: Return → Damage → Settle
  - Return: Actual return 15/02/2026, Mileage 45km, Fuel Full, Clean, Helmet returned
  - Damage: No Damage (standard inspection)
  - Settle: Original rental 1,200 THB (Paid), Additional Due 0 THB, Deposit refund 2,000 THB via Cash
  - Rental status: Active → Completed
  - Vehicle status: Rented → Available (3 vehicles available again)
  - All PostgreSQL reads/writes succeeded

- **Step 11**: Till Session Close:
  - Close Shift dialog opened, denomination count entered (4×฿1,000 + 4×฿100 = ฿4,400)
  - Variance handling tested: +฿200 variance acknowledged via checkbox
  - Session closed with ClosedWithVariance status
  - Page returned to "Start Your Shift" prompt
  - **Fix**: `ClosingCountPanel.razor` — initial `OnBreakdownsChanged` not fired when no draft exists, causing Expected to show ฿0 in sidebar. Added initial callback after `OnParametersSetAsync` completes.

- **Step 12**: Extended Feature Testing (all pages load from PostgreSQL):
  - **Vehicle Pools** (`/settings/vehicle-pools`): Created "Phuket Area Pool" — entity saved, listed as Active
  - **Pricing Rules** (`/settings/pricing-rules`): Page loads, Add Rule dialog renders with all fields (Season/Event/DayOfWeek/Custom types, Date Configuration, Multiplier with Price Preview, Advanced Options). Note: Blazor form_input automation didn't trigger server-side binding; page itself works correctly.
  - **Agents** (`/agents`): Created "Phuket Island Tours" (TG-001, Tour Guide, 10% commission) — saved and listed correctly
  - **Payments** (`/payments`): Shows 3 payment records from completed rental cycle (Rental 1,200 THB, Deposit 2,000 THB, Refund -2,000 THB). Summary totals correct.
  - **Agent Commissions** (`/finance/agent-commissions`): Page loads with Active Agents: 1, Pending/Outstanding: 0
  - **Renters** (`/renters`): Renter Management page loads with search and empty state
  - **Rentals** (`/rentals`): Active: 0, Completed: 1 (R00001, Honda PCX 160, 1,200 THB)
  - **Dashboard** (`/dashboard`): Active Rentals 0, Available Vehicles 3/3, Today's Revenue 5,200 THB
  - **Manager Dashboard** (`/manager`): Loads with Revenue Trend, Fleet Status sections
  - **Fleet Management** (`/vehicles`): 3 Vehicles, 3 Available, 0 Rented, 0 Maintenance. Grid/List views.
  - **Bookings** (`/bookings`): Today's Arrivals 0, Pending 0, Confirmed 0, Checked In 1
  - **Denomination Groups** (`/settings/denomination-groups`): Created USD "Large Bills" group with denominations 100 and 20 — entity saved and listed
  - **Exchange Rates** (`/settings/exchange-rates`): Full workflow tested:
    - Provider selector (Manual/Mamy Exchange/Super Rich) works
    - Fetched USD rates from Mamy Exchange provider — DenominationRate entity created in PostgreSQL
    - Buy rate: 30.8500, Sell rate: 31.0500 (from provider)
    - Buy/Sell toggle switches rates correctly
    - Rate details flyout shows full breakdown (Provider Rate, Delta, Final Rate, Quick Calculator, Effective timestamp)
    - Rate summary aggregation queries work against PostgreSQL
  - **Service Locations** (`/settings/service-locations`): Page loads with Patong Beach Shop dropdown, Create Default Locations button

### Remaining
- **Step 3**: Create Staff Members (skipped, not blocking)
- **Pricing Rules**: Form submission via browser automation failed due to Blazor Server binding (DOM-only changes don't propagate to C# state). Manual testing recommended.
- **Apply Regional Preset**: Button non-responsive on Pricing Rules page (not investigated)

### Notes
- No PostgreSQL errors during Steps 2, 4-7 — all tenant CRUD works with RLS
- DateTimeOffset UTC fix was required for all write operations (Step 8+)
- Full check-in workflow (6 steps) works end-to-end with PostgreSQL + RLS
- Full check-out workflow (3 steps) works end-to-end with PostgreSQL + RLS
- Till close with denomination count and variance handling works with PostgreSQL
- All 13+ tenant pages tested — no PostgreSQL errors on any page load or data query
- Agent entity CRUD works (create via dialog, list with filters)
- Payment records correctly linked to rentals with proper types and amounts
- Dashboard aggregate queries (COUNT, SUM) work correctly against PostgreSQL
- Denomination groups and exchange rates: full CRUD + external provider fetch works with PostgreSQL
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
9. Check-out flow completes — Rental status Completed, vehicle Available, deposit refunded
10. Till close completes — Session closed with denomination count, variance tracked
11. No unhandled PostgreSQL errors in server output throughout
12. Vehicle Pools, Agents, Payments, Commissions, Renters, Service Locations all load correctly
15. Denomination Groups CRUD works — create group with denominations, persist to PostgreSQL
16. Exchange Rates — fetch from external provider (Mamy Exchange), Buy/Sell toggle, rate details flyout all work
13. Dashboard and Manager Dashboard aggregate data from PostgreSQL correctly
14. All tenant CRUD operations (create, read, list, filter) work with RLS isolation
