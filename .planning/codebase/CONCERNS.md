# Codebase Concerns

**Analysis Date:** 2026-01-19

## Tech Debt

**Deprecated Motorbike Entity Still in Use:**
- Issue: Legacy `Motorbike` entity exists alongside new `Vehicle` entity with backward compatibility shims
- Files:
  - `src/MotoRent.Domain/Entities/Entity.cs` (line 16: JsonDerivedType still registered)
  - `src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs` (line 24: Repository still registered)
  - `src/MotoRent.Server/Program.cs` (line 51: MotorbikeService still registered)
  - `src/MotoRent.Services/RentalService.cs` (lines 178-182, 1054-1058, 1192-1196: Obsolete methods)
  - `src/MotoRent.Domain/Entities/Rental.cs` (lines 50, 87, 182, 226: Obsolete properties)
- Impact: Code duplication, confusion for developers, extra maintenance burden
- Fix approach: Complete migration to Vehicle entity, remove all `[Obsolete]` marked code, update remaining references

**Staff Pages Using Hardcoded Mock Data:**
- Issue: Staff pages (Return.razor, ActiveRentals.razor, Index.razor) use hardcoded test data instead of real service calls
- Files:
  - `src/MotoRent.Client/Pages/Staff/Return.razor` (lines 337-361, 371-398, 422-435)
  - `src/MotoRent.Client/Pages/Staff/ActiveRentals.razor` (lines 204-236, 290-296)
  - `src/MotoRent.Client/Pages/Staff/Index.razor` (line 323)
- Impact: Staff workflow pages non-functional, requires full implementation
- Fix approach: Inject RentalService, VehicleService; implement actual data loading; remove placeholder data

**Incomplete TODO Implementations (30+ items):**
- Issue: Multiple service and UI methods have TODO comments indicating incomplete functionality
- Files:
  - `src/MotoRent.Services/BookingService.cs` (line 700): Cancellation policy not implemented
  - `src/MotoRent.Worker/Subscribers/RentalExpirySubscriber.cs` (lines 40, 61-62): Notifications not implemented
  - `src/MotoRent.Worker/Subscribers/RentalCheckOutSubscriber.cs` (lines 29, 36): Vehicle status update, thank-you notification
  - `src/MotoRent.Client/Pages/Vehicles/Vehicles.razor` (line 823): Pool selection dialog
  - `src/MotoRent.Client/Pages/Rentals/RentalDetails.razor` (lines 1139, 1146): Payment and damage dialogs
  - `src/MotoRent.Client/Pages/DocumentViewerDialog.razor` (lines 307-322): Zoom/rotation functionality
  - `src/MotoRent.Client/Pages/Bookings/BookingDetails.razor` (lines 379, 422, 429, 457, 464): Multiple dialogs
  - `src/MotoRent.Client/Controls/DocumentUpload.razor` (line 337): Reprocess endpoint
  - `src/MotoRent.Client/Layout/ManagerLayout.razor` (line 284): Data refresh by period
  - `src/MotoRent.Client/Layout/Templates/ModernTouristTemplate.razor` (line 133): Language switching
- Impact: Features incomplete, potential confusion for users encountering stubbed functionality
- Fix approach: Prioritize TODOs by user impact; implement cancellation policy and notifications first

## Known Bugs

**Async Void Event Handler:**
- Symptoms: Potential unhandled exceptions in navigation callback
- Files: `src/MotoRent.Client/Layout/TouristLayout.razor` (line 120)
- Trigger: Navigation events in tourist layout
- Workaround: None - exceptions may be swallowed silently
- Fix: Change to `async Task` and use proper event subscription pattern

## Security Considerations

**Default JWT Secret in Code:**
- Risk: Production deployment may use default secret if not properly configured
- Files:
  - `src/MotoRent.Domain/Core/MotoConfig.cs` (line 32): `"motorent-default-jwt-secret-key-change-in-production"`
  - `src/MotoRent.Server/appsettings.json` (line 29): `"change-this-in-production"`
- Current mitigation: Intended to be overridden by environment variable
- Recommendations:
  - Add startup validation that fails if default secret is detected in non-development
  - Use Azure Key Vault or similar secrets manager

**API Keys Passed in Query Strings:**
- Risk: API keys may appear in server logs, browser history, referer headers
- Files:
  - `src/MotoRent.Services/DocumentOcrService.cs` (line 49): `?key={apiKey}`
  - `src/MotoRent.Services/VehicleRecognitionService.cs` (line 76): `?key={apiKey}`
- Current mitigation: HTTPS encryption
- Recommendations: Use request headers for API keys where supported

**SHA256 Password Hashing (Adequate but not Ideal):**
- Risk: SHA256 is fast, making brute-force attacks easier
- Files: `src/MotoRent.Services/Core/SqlDirectoryService.cs` (lines 284-304)
- Current mitigation: Salt is used
- Recommendations: Consider bcrypt or Argon2 for password hashing

## Performance Bottlenecks

**Large Page Sizes Loading All Records:**
- Problem: Multiple services load up to 10,000 records without pagination
- Files:
  - `src/MotoRent.Services/RentalService.cs` (lines 86, 102): `size: 10000`
  - `src/MotoRent.Services/PaymentService.cs` (lines 26, 33): `size: 10000`
  - `src/MotoRent.Services/DepositService.cs` (lines 22, 64, 71, 82, 90, 152, 173, 179): `size: 10000`
  - `src/MotoRent.Services/DamageReportService.cs` (lines 28, 35, 123, 130, 155, 162, 169, 276, 283, 301, 308): `size: 10000`
  - `src/MotoRent.Services/ReceiptService.cs` (line 577): `size: 10000`
  - `src/MotoRent.Services/TillService.cs` (line 641): `size: 10000`
  - `src/MotoRent.Services/AssetService.cs` (line 371): `size: 10000`
  - `src/MotoRent.Services/AssetLoanService.cs` (lines 312, 336, 385): `size: 10000`
  - `src/MotoRent.Services/AssetExpenseService.cs` (line 255): `size: 10000`
- Cause: Loading all records for in-memory aggregation/filtering
- Improvement path:
  - Use SQL aggregation queries instead of loading all records
  - Add date range parameters to narrow result sets
  - Implement server-side filtering/paging

**CheckOutDialog Loads Multiple Services Sequentially:**
- Problem: OnInitializedAsync loads 8+ services sequentially
- Files: `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` (lines 1333-1409)
- Cause: Sequential await calls for each data source
- Improvement path: Use `Task.WhenAll()` for parallel loading

## Fragile Areas

**Repository Auto-Create Pattern:**
- Files: `src/MotoRent.Domain/DataContext/Repository.cs` (lines 59-64, 126-131, etc.)
- Why fragile: Catches SQL exceptions and attempts to auto-create missing tables/columns; silent failures possible
- Safe modification: Avoid relying on auto-creation; run migrations explicitly
- Test coverage: Single test file exists (`tests/MotoRent.Domain.Tests/MaintenanceAlertTests.cs`) - 57 lines

**Multi-Tenant Schema Resolution:**
- Files:
  - `src/MotoRent.Domain/DataContext/Repository.cs` (lines 26-39)
  - `src/MotoRent.Server/Services/MotoRentRequestContext.cs`
- Why fragile: Schema determined at runtime from IRequestContext; wrong context = wrong tenant data
- Safe modification: Always verify IRequestContext is correctly scoped; test with multiple tenants
- Test coverage: No dedicated multi-tenant tests found

**Complex Rental Workflow (CheckIn/CheckOut):**
- Files:
  - `src/MotoRent.Services/RentalService.cs` (lines 188-394: CheckInAsync, 396-641: CheckOutAsync)
  - `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` (1668 lines total)
- Why fragile: Many entities updated in single transaction (Rental, Vehicle, Deposit, Payment, OwnerPayment, DamageReport, etc.)
- Safe modification: Add comprehensive integration tests; use transactions; validate state before operations
- Test coverage: No rental workflow tests found

## Scaling Limits

**In-Memory Status Counting:**
- Current capacity: Works for shops with <10,000 rentals
- Limit: Memory consumption grows linearly with rental count
- Files: `src/MotoRent.Services/RentalService.cs` (lines 82-91: GetStatusCountsAsync)
- Scaling path: Use SQL GROUP BY for counting

**Document/Image Storage:**
- Current capacity: S3 with pre-signed URLs
- Limit: No cleanup policy for orphaned documents
- Files: `src/MotoRent.Services/Storage/S3BinaryStore.cs`
- Scaling path: Implement lifecycle policies on S3 bucket

## Dependencies at Risk

**RabbitMQ Optional but Message Loss Possible:**
- Risk: If RabbitMQ unavailable, messages are lost (no local queue fallback)
- Files: `src/MotoRent.Messaging/RabbitMqMessageBroker.cs`
- Impact: Background processing fails silently
- Migration plan: Add persistent outbox pattern or use Azure Service Bus with dead-letter queue

## Missing Critical Features

**Export Functionality Not Implemented:**
- Problem: Multiple "Export" buttons reference unimplemented methods
- Files:
  - `src/MotoRent.Client/Pages/Finance/OwnerPayments.razor` (line 355)
  - `src/MotoRent.Client/Pages/Finance/DepreciationReport.razor` (line 421)
- Blocks: Users cannot export financial reports for accounting

**Notification System Incomplete:**
- Problem: NotificationService exists but notification triggers not wired
- Files:
  - `src/MotoRent.Services/NotificationService.cs` (528 lines)
  - `src/MotoRent.Worker/Subscribers/RentalExpirySubscriber.cs` (TODO at line 40)
- Blocks: Rental expiry warnings, overdue notifications not sent

## Test Coverage Gaps

**Minimal Unit Tests:**
- What's not tested: Almost entire codebase
- Files: Only `tests/MotoRent.Domain.Tests/MaintenanceAlertTests.cs` (57 lines, 3 tests)
- Risk: Regressions go unnoticed; refactoring is dangerous
- Priority: High - critical paths (CheckIn, CheckOut, Payment) need tests first

**No Integration Tests:**
- What's not tested: Repository operations, multi-tenant isolation, workflow end-to-end
- Files: No integration test project found
- Risk: Database schema changes may break production
- Priority: High - add tests for Repository and RentalService

**No E2E Tests:**
- What's not tested: Full user workflows
- Files: `qa.tests/` directory exists with test plans but no automated tests
- Risk: UI regressions, workflow breaks
- Priority: Medium - add Playwright tests for critical paths (check-in, check-out, till operations)

---

*Concerns audit: 2026-01-19*
