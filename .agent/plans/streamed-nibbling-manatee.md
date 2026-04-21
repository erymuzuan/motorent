# Fix All Compiler Warnings

## Context

Building with `dotnet build "-p:EnforceCodeStyleInBuild=true;AnalysisLevel=latest"` reveals **79 compiler warnings** across the solution. These are currently invisible in the default build but indicate real code quality issues: null safety gaps, obsolete API usage, member shadowing bugs, and dead code. This plan fixes all 79 warnings organized by type, using parallel teams.

---

## Warning Summary

| Category | CS Code(s) | Count | Team |
|----------|------------|-------|------|
| Member Hiding | CS0108, CS0114 | 23 | `team-member-hiding` |
| Obsolete API | CS0618 | 10 | `team-obsolete-api` |
| Nullable References | CS8601, CS8602, CS8604, CS8625 | 33 | `team-nullable-refs` |
| Dead Code | CS0414, CS0649 | 7 | `team-dead-code` |
| Misc Cleanup | CS0105, CS4014, BL0007 | 6 | `team-misc-cleanup` |

---

## Team 1: `team-member-hiding` (CS0108 + CS0114) — 23 warnings

These are members in derived classes that shadow base class members from `MotoRentComponentBase` or `MotoRentDialogBase`.

### CS0108 — Hides inherited member (13 warnings)

**Fix strategy: Remove the duplicate `@inject` or local declaration, use the inherited member instead.**

| # | File | Member | Inherited From | Fix |
|---|------|--------|---------------|-----|
| 1 | `Components/Till/PaymentTerminalPanel.razor:6` | `Logger` | `MotoRentComponentBase.Logger` | Remove `@inject ILogger Logger` |
| 2 | `Components/Till/PaymentTerminalPanel.razor:487` | `UserName` | `MotoRentComponentBase.UserName` | Remove local `UserName` property, use `this.UserName` from base |
| 3 | `Components/Shared/InspectionSelector.razor:75` | `AccountNo` | `MotoRentComponentBase.AccountNo` | Remove local `AccountNo`, use inherited |
| 4 | `Pages/DamageReportDialog.razor:206` | `Saving` | `MotoRentDialogBase.Saving` | Remove local `private bool Saving`, use inherited `this.Saving` |
| 5 | `Pages/Bookings/RecordBookingPaymentDialog.razor:158` | `FormatCurrency()` | `MotoRentComponentBase.FormatCurrency()` | Remove local method, use inherited `this.FormatCurrency()` |
| 6 | `Pages/Staff/ActiveRentals.razor:6` | `NavigationManager` | `MotoRentComponentBase.NavigationManager` | Remove `@inject NavigationManager NavigationManager` |
| 7 | `Pages/Staff/TillRecordPayoutDialog.razor:146` | `Saving` | `MotoRentDialogBase.Saving` | Remove local field, use inherited |
| 8 | `Pages/Staff/TillCloseSessionDialog.razor:238` | `Saving` | `MotoRentDialogBase.Saving` | Remove local field, use inherited |
| 9 | `Pages/Staff/TillTransactionDialog.razor:646` | `UserName` | `MotoRentComponentBase.UserName` | Remove local property, use inherited |
| 10 | `Pages/Vehicles/VehicleInspectionPage.razor:10` | `NavigationManager` | `MotoRentComponentBase.NavigationManager` | Remove `@inject` |
| 11 | `Pages/Staff/Return.razor:6` | `NavigationManager` | `MotoRentComponentBase.NavigationManager` | Remove `@inject` |
| 12 | `Pages/Rentals/CheckInSteps/AgreementSignatureStep.razor:6` | `NavigationManager` | `MotoRentComponentBase.NavigationManager` | Remove `@inject` |
| 13 | `Pages/SuperAdmin/Impersonate.razor:12` | `NavigationManager` | `MotoRentComponentBase.NavigationManager` | Remove `@inject` |

### CS0114 — Hides inherited method without `override` (10 warnings)

**Fix strategy: Change `private void Cancel()` to `protected override void Cancel()` to properly override the base `MotoRentDialogBase.Cancel()` method.**

| # | File | Method | Fix |
|---|------|--------|-----|
| 1 | `Pages/InvoiceDialog.razor:318` | `Cancel()` | Add `override` keyword |
| 2 | `Pages/Staff/TillCashDropDialog.razor:280` | `Cancel()` | Add `override` keyword |
| 3 | `Pages/Staff/TillRecordPayoutDialog.razor:172` | `Cancel()` | Add `override` keyword |
| 4 | `Pages/Staff/TillOpenSessionDialog.razor:197` | `Cancel()` | Add `override` keyword |
| 5 | `Pages/Staff/TillCloseSessionDialog.razor:497` | `Cancel()` | Add `override` keyword |
| 6 | `Pages/Staff/TillTransactionDialog.razor:1469` | `Cancel()` | Add `override` keyword |
| 7 | `Pages/Staff/TillReceiveDepositDialog.razor:220` | `Cancel()` | Add `override` keyword |
| 8 | `Pages/Staff/TillHistoryDialog.razor:99` | `Cancel()` | Add `override` keyword |
| 9 | `Pages/Staff/TillBookingDepositDialog.razor:589` | `Cancel()` | Add `override` keyword |
| 10 | `Pages/Staff/TillReceivePaymentDialog.razor:389` | `Cancel()` | Add `override` keyword |

**Important**: Read each `Cancel()` to verify the local implementation matches or extends the base behavior (`this.ModalService.Close(ModalResult.Cancel())`). If the local method does something different (e.g., extra cleanup), use `override` and include the extra logic before calling `base.Cancel()`.

---

## Team 2: `team-obsolete-api` (CS0618) — 10 warnings

Replace deprecated property names with their designated replacements. The obsolete properties are backward-compatibility shims in the entity classes.

| # | File:Line | Original | Replacement |
|---|-----------|----------|-------------|
| 1 | `Services/DepositService.cs:25` | `rental.ShopId` | `rental.RentedFromShopId` |
| 2 | `Services/DepositService.cs:68` | `rental.ShopId` | `rental.RentedFromShopId` |
| 3 | `Services/DepositService.cs:87` | `rental.ShopId` | `rental.RentedFromShopId` |
| 4 | `Services/DepositService.cs:155` | `rental.ShopId` | `rental.RentedFromShopId` |
| 5 | `Services/InvoiceService.cs:34` | `rental.MotorbikeId` | `rental.VehicleId` |
| 6 | `Services/InvoiceService.cs:166` | `invoice.DailyRate` | `invoice.RentalRate` |
| 7 | `Client/Pages/InvoiceDialog.razor:69` | `m_invoice.MotorbikeBrand` | `m_invoice.VehicleBrand` |
| 8 | `Client/Pages/InvoiceDialog.razor:69` | `m_invoice.MotorbikeModel` | `m_invoice.VehicleModel` |
| 9 | `Client/Pages/InvoiceDialog.razor:70` | `m_invoice.MotorbikeLicensePlate` | `m_invoice.VehicleLicensePlate` |
| 10 | `Client/Pages/Tourist/RentalHistory.razor:202` | `rental.DailyRate` | `rental.RentalRate` |

---

## Team 3: `team-nullable-refs` (CS8601/CS8602/CS8604/CS8625) — 33 warnings

Fix null-safety warnings with appropriate null checks, null-coalescing, or null-forgiving operators.

### Fix strategies by sub-type:

**CS8604 — Possible null argument (23 warnings)**

Two recurring patterns:

*Pattern A: `AccountNo` passed to service methods (7 occurrences)*
```
Original: await OrganizationService.GetOrganizationByAccountNoAsync(AccountNo)
Fix:      await OrganizationService.GetOrganizationByAccountNoAsync(AccountNo!)
  (or)    if (AccountNo is null) return; // guard above
```

Files: `PrintBookingConfirmation:270`, `ReceiptPrintDialog:117`, `PrintAgreement:507`, `BrandingSettings:304,377`, `DocumentTemplateDesigner:748`, `OrganizationSettings:160`, `DocumentTemplatesController:158`

*Pattern B: `Localizer[key, args]` with potentially null args (8 occurrences)*
```
Original: Localizer["Key", someNullableValue]
Fix:      Localizer["Key", someNullableValue ?? ""]
```

Files: `BookingDetails:636`, `EodSessionDetailDialog:90`, `Staff/Index:183`, `AccidentDetails:378,395`, `AddMaintenanceRecordDialog:170`, `AssetDialog:376`, `OwnerPayments:317`

*Pattern C: Nullable string passed to non-nullable parameter in switch methods (7 occurrences)*
```
Original: GetPaymentTypeBadgeClass(payment.Type)
Fix:      GetPaymentTypeBadgeClass(payment.Type ?? "")
  (or)    Change method signature: string GetPaymentTypeBadgeClass(string? type)
```

Files: `InvoiceDialog:227`, `RentalDetails:729,739,785,800`

*Other CS8604 (1)*: `RabbitMqMessageBroker:236` — add null check on `m_channel` before use, `TsqlQueryFormatter:328` — add `!` or null coalescing on `.ToString()`

**CS8625 — Cannot convert null literal (7 warnings)**

```
Original: return new ProjectionExpression(source, projector, null);
Fix:      return new ProjectionExpression(source, projector, null!);
```

Files: `QueryBinder.cs:132,158,178,208,287` (Domain), `Vehicles.razor:67`

**CS8601 — Possible null reference assignment (2 warnings)**
```
Original: this.Orderings = orderings as ReadOnlyCollection<OrderExpression>;
Fix:      this.Orderings = orderings as ReadOnlyCollection<OrderExpression>
              ?? new List<OrderExpression>(orderings).AsReadOnly();
```

Files: `OrderByRewriter.cs:111` (Domain + Core.Repository — same file, two projects)

**CS8602 — Dereference of possibly null reference (2 warnings)**
```
Fix: Add null-conditional operator (?.) or null check
```

Files: `SqlJsonRepository.cs:311`, `AccidentDetails.razor:263`

---

## Team 4: `team-dead-code` (CS0414 + CS0649) — 7 warnings

### CS0649 — Field never assigned (3 warnings)

| # | File:Line | Field | Fix |
|---|-----------|-------|-----|
| 1 | `Pages/RentalList.razor:236` | `m_dueTodayCount` | Remove field (not used in UI or logic) |
| 2 | `Pages/RentalList.razor:237` | `m_overdueCount` | Remove field (not used in UI or logic) |
| 3 | `Pages/Finance/AssetEdit.razor:102` | `m_saving` | Remove field or connect to save logic |

### CS0414 — Field assigned but never used (4 warnings)

| # | File:Line | Field | Fix |
|---|-----------|-------|-----|
| 1 | `Pages/Rentals/CheckIn.razor:194` | `m_loadingBooking` | Remove field and assignments |
| 2 | `Pages/Home.razor:193` | `m_loading` | Remove field, or bind to `LoadingSkeleton` |
| 3 | `Pages/Rentals/CheckInSteps/SelectRenterStep.razor:161` | `m_loading` | Remove field, or bind to `LoadingSkeleton` |
| 4 | `Pages/Tourist/Landing.razor:355` | `m_loading` | Remove field, or bind to `LoadingSkeleton` |

**Important**: Before removing, read each file to check if the field is referenced in the razor markup (template section). If it's used in UI binding like `Loading="@m_loading"`, keep it and suppress the warning or convert to a property.

---

## Team 5: `team-misc-cleanup` (CS0105 + CS4014 + BL0007) — 6 warnings

### CS0105 — Duplicate using directive (1 warning)

| File | Fix |
|------|-----|
| `Components/Templates/DocumentCanvas.razor:56` | Remove the duplicate `@using MotoRent.Client.Services` |

### CS4014 — Call is not awaited (4 warnings)

| # | File:Line | Fix |
|---|-----------|-----|
| 1 | `Pages/Settings/ShopUsers.razor:143` | Add `await` to async call |
| 2 | `Pages/Settings/ShopUsers.razor:150` | Add `await` to async call |
| 3 | `Pages/Settings/ShopUsers.razor:158` | Add `await` to async call |
| 4 | `Pages/Settings/ShopUsers.razor:167` | Add `await` to async call |

### BL0007 — Component parameter should be auto property (1 warning)

| File | Fix |
|------|-----|
| `Controls/MotoRentDialogBase.cs:113` | Convert `Item` parameter to auto property |

---

## Execution Plan

1. Create a team with 5 agents running in parallel
2. Each agent handles one warning category
3. After all agents complete, run `dotnet build "-p:EnforceCodeStyleInBuild=true;AnalysisLevel=latest"` to verify 0 warnings
4. Commit all changes

## Verification

```bash
# Must produce 0 warnings
dotnet build "-p:EnforceCodeStyleInBuild=true;AnalysisLevel=latest"

# Must produce 0 errors
dotnet build
```

## Key files

All paths are relative to `src/MotoRent.Client/` unless noted:

- **Base classes**: `Controls/MotoRentComponentBase.cs`, `Controls/MotoRentDialogBase.cs`
- **Services** (`src/MotoRent.Services/`): `DepositService.cs`, `InvoiceService.cs`
- **Domain** (`src/MotoRent.Domain/`): `QueryProviders/OrderByRewriter.cs`, `QueryProviders/QueryBinder.cs`
- **Repository** (`src/MotoRent.SqlServerRepository/`): `SqlJsonRepository.cs`, `TsqlQueryFormatter.cs`
- **Core.Repository** (`src/MotoRent.Core.Repository/`): `QueryProviders/OrderByRewriter.cs`
- **Messaging** (`src/MotoRent.Messaging/`): `RabbitMqMessageBroker.cs`
- **Server** (`src/MotoRent.Server/`): `Controllers/DocumentTemplatesController.cs`
