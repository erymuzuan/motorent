# Codebase Concerns

**Analysis Date:** 2026-01-23

## Tech Debt

**Incomplete Notification System:**
- Issue: Notification TODOs throughout worker subscribers indicate notifications are logged but not sent
- Files:
  - `src/MotoRent.Worker/Subscribers/RentalExpirySubscriber.cs` (lines 40, 61-62)
  - `src/MotoRent.Worker/Subscribers/RentalCheckOutSubscriber.cs` (lines 29, 36)
- Impact: Customers and shop managers do not receive automated notifications for rental expiry, overdue rentals, or checkout confirmations
- Fix approach: Implement actual notification calls using `NotificationService` which exists but is not wired into subscribers

**Unimplemented UI Feature Stubs:**
- Issue: Multiple UI components have TODO comments for core functionality
- Files:
  - `src/MotoRent.Client/Pages/Bookings/BookingDetails.razor` (lines 379, 422, 429, 457, 464)
  - `src/MotoRent.Client/Pages/Staff/ActiveRentals.razor` (lines 210, 281, 295)
  - `src/MotoRent.Client/Pages/Staff/Return.razor` (lines 339, 355, 380, 433)
  - `src/MotoRent.Client/Pages/Rentals/RentalDetails.razor` (lines 1139, 1146)
  - `src/MotoRent.Client/Pages/DocumentViewerDialog.razor` (lines 307, 312, 317, 322)
  - `src/MotoRent.Client/Controls/DocumentUpload.razor` (line 337)
  - `src/MotoRent.Client/Pages/Vehicles/Vehicles.razor` (line 823)
  - `src/MotoRent.Client/Pages/Finance/DepreciationReport.razor` (line 421)
  - `src/MotoRent.Client/Pages/Finance/OwnerPayments.razor` (line 355)
- Impact: Missing dialogs for status changes, payments, damage reports, document zoom/rotation, CSV export, and pool selection
- Fix approach: Implement each dialog/feature systematically, prioritizing payment and status change dialogs

**Cancellation Policy Not Implemented:**
- Issue: TODO in BookingService for cancellation policy
- Files: `src/MotoRent.Services/BookingService.cs` (line 700)
- Impact: No automated refund calculation based on cancellation timing
- Fix approach: Implement cancellation policy configuration in tenant settings and apply during cancellation

**Legacy Motorbike Entity Parallel Path:**
- Issue: `RentalService` has dual code paths for `Vehicle` and `Motorbike` entities
- Files: `src/MotoRent.Services/RentalService.cs` (lines 359-391, 404-412)
- Impact: Code duplication, maintenance burden, potential inconsistency
- Fix approach: Complete migration from `Motorbike` to `Vehicle` entity, then remove legacy paths

## Known Bugs

**No known bugs documented in code.**
- The codebase uses proper error handling patterns
- TODO comments are for incomplete features, not bugs

## Security Considerations

**Default JWT Secret in Code:**
- Risk: Default JWT secret is hardcoded as fallback
- Files: `src/MotoRent.Domain/Core/MotoConfig.cs` (line 32)
- Current mitigation: Production should set `MOTO_JwtSecret` environment variable
- Recommendations: Add startup validation that fails if default secret is used in production; log warning if using default

**RabbitMQ Default Credentials:**
- Risk: Default "guest/guest" credentials if environment variables not set
- Files: `src/MotoRent.Messaging/RabbitMqConfigurationManager.cs` (lines 10-11)
- Current mitigation: None - will use guest credentials in production if not configured
- Recommendations: Fail fast if credentials not explicitly configured in production environment

**Error Details Exposed in API Responses:**
- Risk: Exception messages returned to client in some error responses
- Files: `src/MotoRent.Server/Controllers/StoresController.cs` (line 90 - `details = ex.Message`)
- Current mitigation: Only in storage controller
- Recommendations: Remove exception details from production responses; log internally only

**File Upload Without Validation:**
- Risk: No content-type validation beyond extension check
- Files: `src/MotoRent.Server/Controllers/StoresController.cs` (lines 30-91)
- Current mitigation: 10MB size limit
- Recommendations: Add magic byte validation for file types; restrict to expected content types (images, PDFs)

## Performance Bottlenecks

**Unbounded Query Results:**
- Problem: Many service methods fetch up to 10,000 records without pagination
- Files:
  - `src/MotoRent.Services/RentalService.cs` (lines 86, 102)
  - `src/MotoRent.Services/DepositService.cs` (multiple occurrences)
  - `src/MotoRent.Services/DamageReportService.cs` (multiple occurrences)
  - `src/MotoRent.Services/AssetLoanService.cs` (lines 312, 336, 385)
  - `src/MotoRent.Services/AssetService.cs` (line 371)
  - `src/MotoRent.Services/AgentService.cs` (lines 284, 338)
  - `src/MotoRent.Services/AgentCommissionService.cs` (lines 342, 369)
  - `src/MotoRent.Services/PaymentService.cs` (lines 26, 33)
- Cause: `page: 1, size: 10000` pattern loads all data into memory
- Improvement path: Add proper server-side pagination or use SQL aggregation; add date range filters

**In-Memory Status Counting:**
- Problem: `GetStatusCountsAsync` loads all rentals to count by status
- Files: `src/MotoRent.Services/RentalService.cs` (lines 82-91)
- Cause: Loads up to 10,000 rentals then groups in memory
- Improvement path: Use SQL `GROUP BY` query via repository aggregate methods

## Fragile Areas

**Multi-Tenant Schema Resolution:**
- Files: `src/MotoRent.Domain/DataContext/Repository.cs` (lines 26-39)
- Why fragile: Schema name comes from `IRequestContext` which relies on claims; missing/invalid context silently falls back to "MotoRent" default schema
- Safe modification: Always verify `IRequestContext` is properly injected before database operations
- Test coverage: No tests for multi-tenant schema switching

**Expression Tree Parsing in Repository:**
- Files: `src/MotoRent.Domain/DataContext/Repository.cs` (lines 866-911)
- Why fragile: Custom LINQ expression parsing for WHERE clauses; only handles specific patterns
- Safe modification: Test any new expression patterns thoroughly; fallback generates empty condition
- Test coverage: Limited - only `MaintenanceAlertTests` exists

**Auto-Table Creation on Error:**
- Files: `src/MotoRent.Domain/DataContext/ExceptionExtensions.cs` (line 40)
- Why fragile: Attempts to create missing SQL objects from exceptions; relies on exception parsing
- Safe modification: Ensure database scripts are up-to-date; don't rely on auto-creation in production
- Test coverage: None

## Scaling Limits

**Single SQL Server Instance:**
- Current capacity: All tenants share single database with schema-based isolation
- Limit: Database connection pool exhaustion at high concurrent user count
- Scaling path: Implement read replicas; consider tenant sharding for large deployments

**RabbitMQ Single Consumer:**
- Current capacity: One worker instance per subscriber queue
- Limit: Message processing throughput limited by single consumer
- Scaling path: Add prefetch count tuning; implement competing consumers with instance naming

## Dependencies at Risk

**Gemini API Model Version:**
- Risk: Hardcoded model name "gemini-3-flash-preview" may become deprecated
- Files: `src/MotoRent.Domain/Core/MotoConfig.cs` (line 44)
- Impact: OCR functionality would break if model is retired
- Migration plan: Make model configurable; monitor Google API announcements

**Polly Retry Policy:**
- Risk: Used extensively but dependency is implicitly assumed
- Files: `src/MotoRent.Core.Repository/CoreSqlJsonRepository.cs` (multiple occurrences)
- Impact: Retry logic for SQL network errors
- Migration plan: Ensure Polly package is pinned; consider moving to built-in resilience in .NET 8+

## Missing Critical Features

**Push Notifications:**
- Problem: Infrastructure ready but not implemented
- Blocks: Real-time alerts for rental expiry, overdue returns, booking confirmations

**Rate Limiting:**
- Problem: No rate limiting on API endpoints
- Blocks: Protection against abuse, especially on public tourist endpoints

**Audit Trail:**
- Problem: Only basic CreatedBy/ChangedBy tracking; no detailed change history for entities
- Blocks: Compliance requirements, dispute resolution

## Test Coverage Gaps

**Services Layer:**
- What's not tested: All service classes (`RentalService`, `BookingService`, `VehicleService`, etc.)
- Files: `src/MotoRent.Services/*.cs`
- Risk: Core business logic changes could introduce regressions undetected
- Priority: High

**Repository Operations:**
- What's not tested: `Repository<T>` CRUD operations, expression parsing, aggregate methods
- Files: `src/MotoRent.Domain/DataContext/Repository.cs`
- Risk: Query generation bugs, tenant isolation failures
- Priority: High

**Worker Subscribers:**
- What's not tested: All RabbitMQ message subscribers
- Files: `src/MotoRent.Worker/Subscribers/*.cs`
- Risk: Message processing failures not caught before deployment
- Priority: Medium

**Multi-Tenant Isolation:**
- What's not tested: Schema-based tenant isolation
- Files: `src/MotoRent.Domain/DataContext/Repository.cs`, `src/MotoRent.Services/Core/SqlSubscriptionService.cs`
- Risk: Data leakage between tenants
- Priority: Critical

**Only Test File:**
- Current coverage: `tests/MotoRent.Domain.Tests/MaintenanceAlertTests.cs` (57 lines, 3 tests)
- Tests only: Basic `MaintenanceAlert` entity property setters and ID methods
- Not tested: Any actual business logic, services, or data access

---

*Concerns audit: 2026-01-23*
