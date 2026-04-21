# MotoRent PWA + WASM InteractiveAuto Implementation Plan

## Status: PENDING REVIEW

## Context

MotoRent is a vehicle rental system (Blazor Server, .NET 10) that already has basic PWA infrastructure (manifest, service worker, offline page, push notification stubs). However, it runs exclusively in `InteractiveServer` mode, meaning every user interaction requires a persistent SignalR connection. For tourist-facing pages accessed on mobile devices in Thailand (potentially with unreliable connectivity), this is suboptimal.

**Goal**: Add `InteractiveAuto` render mode for Tourist pages (SSR first load, WASM on subsequent visits) and enhance the service worker for better caching, WASM asset management, and push notification integration.

**Approach**: Selective InteractiveAuto on Tourist pages only. Staff/Admin/Manager pages remain `InteractiveServer` (they use 50+ server-side services with direct DB access that cannot run in WASM). Tourist pages use a limited set of services (5-6) that we'll expose via API endpoints.

---

## Codebase Analysis (Pre-Implementation Notes)

### Current Tourist Pages & Their Service Dependencies

| Page | Inherits | Injects |
|------|----------|---------|
| `Browse.razor` | `TouristComponentBase` | `VehicleService`, `ShopService`, `FleetModelImageService`, `IBinaryStore`, `IJSRuntime` |
| `Landing.razor` | `TouristComponentBase` | `MotorbikeService`, `ShopService` |
| `VehicleDetails.razor` | `TouristComponentBase` | `MotorbikeService`, `VehicleImageService`, `IBinaryStore` |
| `MyBooking.razor` | `TouristComponentBase` | `BookingService` |
| `RentalHistory.razor` | `TouristComponentBase` | `RentalService`, `BookingService`, `IJSRuntime` |
| `ReservationDialog.razor` | `MotoRentComponentBase` | `BookingService`, `InsuranceService` |
| `MotorbikeDetailsDialog.razor` | `MotoRentComponentBase` | (none extra) |
| `QRCodeDialog.razor` | `MotoRentComponentBase` | (none extra) |

### Key Base Class: `MotoRentComponentBase`
Injects: `ILogger`, `RentalDataContext`, `DialogService`, `ToastService`, `IRequestContext`, `NavigationManager`, `IStringLocalizer<CommonResources>`, `IHashids`, `ISettingConfig`

**Critical**: `RentalDataContext` cannot run in WASM. Plan uses stub that throws if actually queried.

### Key Base Class: `TouristComponentBase` extends `MotoRentComponentBase`
Adds: `TenantContext` cascading parameter, URL helpers, currency formatting.

### Project Reference Issue
`MotoRent.Client.csproj` references `MotoRent.Services` directly. WASM will bundle these assemblies but they won't be callable (no DB connection). The plan's interface-based approach handles this - Tourist pages inject interfaces, WASM provides HttpClient implementations.

---

## Phase 1: Tourist Service Interfaces + API Controllers

### Task 1.1: Create Tourist Service Interfaces in MotoRent.Domain

**Create** `src/MotoRent.Domain/Services/ITouristVehicleService.cs`:
```csharp
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Domain.Services;

public interface ITouristVehicleService
{
    Task<List<VehicleGroup>> GetAvailableVehicleGroupsAsync(int shopId);
    Task<VehicleGroup?> GetVehicleGroupDetailsAsync(int fleetModelId, int shopId);
}
```

**Create** `src/MotoRent.Domain/Services/ITouristShopService.cs`:
```csharp
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Services;

public interface ITouristShopService
{
    Task<List<Shop>> GetShopsAsync();
    Task<Shop?> GetShopAsync(int shopId);
}
```

**Create** `src/MotoRent.Domain/Services/ITouristBookingService.cs`:
```csharp
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Domain.Services;

public interface ITouristBookingService
{
    Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, string customerEmail);
    Task<Booking?> GetBookingByRefAsync(string reference);
    Task<List<Booking>> GetBookingsForTouristAsync(string? email, string? phone);
    Task<OperationResult> CancelBookingAsync(int bookingId, string reason, string cancelledBy);
    Task<List<Rental>> GetRentalHistoryAsync(int shopId, string? email, string? phone);
}
```

**Create** `src/MotoRent.Domain/Services/ITouristImageService.cs`:
```csharp
namespace MotoRent.Domain.Services;

public interface ITouristImageService
{
    Task<string?> GetPrimaryImageUrlAsync(int fleetModelId);
    Task<Dictionary<int, string>> GetPrimaryImageUrlsAsync(IEnumerable<int> fleetModelIds);
    string GetImageUrl(string storeId);
}
```

**Create** `src/MotoRent.Domain/Services/ITouristInsuranceService.cs`:
```csharp
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Services;

public interface ITouristInsuranceService
{
    Task<List<Insurance>> GetActiveInsurancesAsync();
}
```

**Create** `src/MotoRent.Domain/Services/ITouristMotorbikeService.cs`:
```csharp
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Services;

public interface ITouristMotorbikeService
{
    Task<IEnumerable<Motorbike>> GetAvailableMotorbikesAsync();
    Task<Motorbike?> GetMotorbikeByIdAsync(int motorbikeId);
}
```

### Task 1.2: Implement Interfaces on Existing Services

**Modify** `src/MotoRent.Services/VehicleService.cs` - add `: ITouristVehicleService`
- Map `GetAvailableVehicleGroupsAsync` to existing `GetAvailableVehicleGroupsForTouristAsync`

**Modify** `src/MotoRent.Services/ShopService.cs` - add `: ITouristShopService`
- Map `GetShopsAsync` to existing `GetActiveShopsAsync`

**Modify** `src/MotoRent.Services/BookingService.cs` - add `: ITouristBookingService`
- Already has `CreateBookingAsync`, `GetBookingByRefAsync`, `GetBookingsForTouristAsync`, `CancelBookingAsync`
- Add `GetRentalHistoryAsync` delegation to `RentalService`

**Modify** `src/MotoRent.Services/InsuranceService.cs` - add `: ITouristInsuranceService`

**Create** `src/MotoRent.Services/Tourist/TouristImageService.cs` - wraps `FleetModelImageService` + `IBinaryStore`

**Create** `src/MotoRent.Services/Tourist/TouristMotorbikeService.cs` - wraps `MotorbikeService`

### Task 1.3: Create API Controllers

**Create** `src/MotoRent.Server/Controllers/Api/TouristVehiclesController.cs`:
- `GET /api/tourist/{accountNo}/vehicles?shopId={id}` - list vehicle groups
- `GET /api/tourist/{accountNo}/vehicles/{fleetModelId}?shopId={id}` - vehicle details
- `GET /api/tourist/{accountNo}/vehicle-images` - batch image URLs (POST with body of fleetModelIds)

**Create** `src/MotoRent.Server/Controllers/Api/TouristShopsController.cs`:
- `GET /api/tourist/{accountNo}/shops` - list shops
- `GET /api/tourist/{accountNo}/shops/{id}` - shop details

**Create** `src/MotoRent.Server/Controllers/Api/TouristBookingsController.cs`:
- `POST /api/tourist/{accountNo}/bookings` - create booking
- `GET /api/tourist/{accountNo}/bookings/{ref}` - booking by ref
- `GET /api/tourist/{accountNo}/bookings?email={email}&phone={phone}` - bookings for tourist
- `DELETE /api/tourist/{accountNo}/bookings/{id}` - cancel booking
- `GET /api/tourist/{accountNo}/rentals?email={email}&phone={phone}` - rental history

**Create** `src/MotoRent.Server/Controllers/Api/TouristInsuranceController.cs`:
- `GET /api/tourist/{accountNo}/insurance` - active insurances

**Create** `src/MotoRent.Server/Controllers/Api/TouristMotorbikesController.cs`:
- `GET /api/tourist/{accountNo}/motorbikes` - available motorbikes
- `GET /api/tourist/{accountNo}/motorbikes/{id}` - motorbike details

All endpoints: `[AllowAnonymous]`, tenant resolved from `{accountNo}` URL segment using `ITenantResolverService`.

### Task 1.4: Create HttpClient-Based Implementations for WASM

**Create** `src/MotoRent.Client/Services/Http/HttpTouristVehicleService.cs`
**Create** `src/MotoRent.Client/Services/Http/HttpTouristShopService.cs`
**Create** `src/MotoRent.Client/Services/Http/HttpTouristBookingService.cs`
**Create** `src/MotoRent.Client/Services/Http/HttpTouristImageService.cs`
**Create** `src/MotoRent.Client/Services/Http/HttpTouristInsuranceService.cs`
**Create** `src/MotoRent.Client/Services/Http/HttpTouristMotorbikeService.cs`

Each calls the corresponding API endpoint via `HttpClient`. The `accountNo` is extracted from the current URL.

---

## Phase 2: Render Mode Configuration

### Task 2.1: Enable InteractiveWebAssembly in Server Program.cs

**Modify** `src/MotoRent.Server/Program.cs` (line ~346):
```csharp
// Change from:
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()

// To:
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
```

And endpoint mapping (line ~405):
```csharp
// Change from:
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);

// To:
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MotoRent.Client._Imports).Assembly);
```

Also register Tourist service interfaces for server-side resolution:
```csharp
builder.Services.AddScoped<ITouristVehicleService>(sp => sp.GetRequiredService<VehicleService>());
builder.Services.AddScoped<ITouristShopService>(sp => sp.GetRequiredService<ShopService>());
builder.Services.AddScoped<ITouristBookingService>(sp => sp.GetRequiredService<BookingService>());
builder.Services.AddScoped<ITouristInsuranceService>(sp => sp.GetRequiredService<InsuranceService>());
builder.Services.AddScoped<ITouristMotorbikeService, TouristMotorbikeService>();
builder.Services.AddScoped<ITouristImageService, TouristImageService>();
```

### Task 2.2: App.razor - No Change to Global Default

Keep `@rendermode="InteractiveServer"` as global default on `<Routes>` and `<HeadOutlet>`.
Tourist pages will override per-page with `@rendermode InteractiveAuto`.

### Task 2.3: Add InteractiveAuto to Tourist Pages

**Modify** these files - add `@rendermode InteractiveAuto` directive after `@attribute [AllowAnonymous]`:
- `src/MotoRent.Client/Pages/Tourist/Browse.razor`
- `src/MotoRent.Client/Pages/Tourist/Landing.razor`
- `src/MotoRent.Client/Pages/Tourist/VehicleDetails.razor`
- `src/MotoRent.Client/Pages/Tourist/MyBooking.razor`
- `src/MotoRent.Client/Pages/Tourist/RentalHistory.razor`

**Change** service injections from concrete to interface:

Browse.razor:
```razor
@* Before: *@
@inject VehicleService VehicleService
@inject ShopService ShopService
@inject FleetModelImageService FleetModelImageService
@inject IBinaryStore BinaryStore

@* After: *@
@inject ITouristVehicleService VehicleService
@inject ITouristShopService ShopService
@inject ITouristImageService ImageService
```

Landing.razor:
```razor
@* Before: *@
@inject MotorbikeService MotorbikeService
@inject ShopService ShopService

@* After: *@
@inject ITouristMotorbikeService MotorbikeService
@inject ITouristShopService ShopService
```

VehicleDetails.razor:
```razor
@* Before: *@
@inject MotorbikeService MotorbikeService
@inject VehicleImageService VehicleImageService
@inject IBinaryStore BinaryStore

@* After: *@
@inject ITouristMotorbikeService MotorbikeService
@inject ITouristImageService ImageService
```

MyBooking.razor:
```razor
@* Before: *@
@inject BookingService BookingService

@* After: *@
@inject ITouristBookingService BookingService
```

RentalHistory.razor:
```razor
@* Before: *@
@inject RentalService RentalService
@inject BookingService BookingService

@* After: *@
@inject ITouristBookingService BookingService
```

ReservationDialog.razor:
```razor
@* Before: *@
@inject BookingService BookingService
@inject InsuranceService InsuranceService

@* After: *@
@inject ITouristBookingService BookingService
@inject ITouristInsuranceService InsuranceService
```

**Update code-behind** in each page to use the new interface method signatures.

### Task 2.4: Configure WASM Client Program.cs

**Modify** `src/MotoRent.Client/Program.cs`:
```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MotoRent.Client.Services;
using MotoRent.Client.Services.Http;
using MotoRent.Domain.Services;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Settings;
using HashidsNet;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Tourist service implementations (HttpClient-based)
builder.Services.AddScoped<ITouristVehicleService, HttpTouristVehicleService>();
builder.Services.AddScoped<ITouristShopService, HttpTouristShopService>();
builder.Services.AddScoped<ITouristBookingService, HttpTouristBookingService>();
builder.Services.AddScoped<ITouristImageService, HttpTouristImageService>();
builder.Services.AddScoped<ITouristInsuranceService, HttpTouristInsuranceService>();
builder.Services.AddScoped<ITouristMotorbikeService, HttpTouristMotorbikeService>();

// UI services (pure client-side, already in MotoRent.Client)
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

// Stubs for base class dependencies
builder.Services.AddScoped<IRequestContext, WasmRequestContext>();
builder.Services.AddScoped<ISettingConfig, WasmSettingConfig>();
builder.Services.AddSingleton<IHashids>(_ => new Hashids(
    builder.Configuration["HashId:Salt"] ?? "motorent", 8));

// Stub RentalDataContext - throws if actually queried
builder.Services.AddScoped<RentalDataContext>(_ =>
    throw new PlatformNotSupportedException("DataContext not available in WASM"));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

await builder.Build().RunAsync();
```

### Task 2.5: Create WASM-Compatible Stubs

**Create** `src/MotoRent.Client/Services/WasmRequestContext.cs`:
```csharp
using MotoRent.Domain.Core;

namespace MotoRent.Client.Services;

/// <summary>
/// WASM-compatible IRequestContext implementation.
/// Returns defaults suitable for tourist context.
/// </summary>
public class WasmRequestContext : IRequestContext
{
    // Implement all IRequestContext methods with tourist defaults
    // TimeZone: Asia/Bangkok (+7)
    // UserName: "anonymous"
    // Date formatting using CultureInfo
}
```

**Create** `src/MotoRent.Client/Services/WasmSettingConfig.cs`:
```csharp
using MotoRent.Domain.Settings;

namespace MotoRent.Client.Services;

/// <summary>
/// WASM-compatible ISettingConfig implementation.
/// Returns default settings for tourist context.
/// </summary>
public class WasmSettingConfig : ISettingConfig
{
    // Implement all ISettingConfig methods returning defaults
}
```

### Task 2.6: Update _Imports.razor

**Modify** `src/MotoRent.Client/_Imports.razor` - add:
```razor
@using MotoRent.Domain.Services
```

---

## Phase 3: Service Worker Enhancement

### Task 3.1: Rewrite Service Worker with Multi-Cache Strategy

**Modify** `src/MotoRent.Server/wwwroot/service-worker.js`:

- Version bump: `motorent-static-v2`, plus new caches: `motorent-wasm-v1`, `motorent-api-v1`, `motorent-images-v1`
- WASM assets (`_framework/*.wasm`, `*.dll`, `dotnet.*`): Cache-first (fingerprinted, large)
- CSS, JS, fonts: Stale-while-revalidate
- `/api/tourist/*`: Network-first with 5min cache fallback
- Vehicle images (S3 URLs): Cache-first, 50MB limit
- Navigation requests: Network-first, offline fallback
- `/_blazor`, SignalR: Never cache
- `blazor.boot.json`: Network-first (triggers WASM cache invalidation on change)
- Cache size management (evict old entries from image cache when over limit)

### Task 3.2: Add Update Notification to pwa.js

**Modify** `src/MotoRent.Server/wwwroot/js/pwa.js`:
- Add `onUpdateAvailable` callback
- Listen for `controllerchange` on service worker
- Add `skipWaiting()` message channel for user-initiated updates

**Create** `src/MotoRent.Client/Components/Shared/PwaUpdateBanner.razor`:
- Toast/banner when new SW version detected
- "Update available - Refresh to get the latest version" with refresh button
- Uses JS interop to `MotoRentPwa.checkForUpdate()`

### Task 3.3: Blazor WASM Boot Manifest Awareness

The service worker handles `_framework/blazor.boot.json` specially:
- Network-first for `blazor.boot.json`
- When it changes, invalidate WASM cache and re-cache new assets

---

## Phase 4: Push Notifications

### Task 4.1: VAPID Configuration

**Modify** `src/MotoRent.Domain/Core/MotoConfig.cs` - add:
```csharp
// Push Notification VAPID Configuration
public static string? VapidPublicKey => GetEnvironmentVariable("VapidPublicKey");
public static string? VapidPrivateKey => GetEnvironmentVariable("VapidPrivateKey");
public static string VapidSubject => GetEnvironmentVariable("VapidSubject") ?? "mailto:admin@motorent.com";
```

### Task 4.2: Push Subscription Entity + SQL

**Create** `src/MotoRent.Domain/Entities/PushSubscription.cs`:
```csharp
public class PushSubscription : Entity
{
    public int PushSubscriptionId { get; set; }
    public string Endpoint { get; set; } = "";
    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";
    public string? UserId { get; set; }
    public string? DeviceInfo { get; set; }
}
```

**Create** `output/database.motorent/tables/MotoRent.PushSubscription.sql` (PostgreSQL with RLS)

Register repository in PostgreSQL `ServiceCollectionExtensions.cs`.
Add IQueryable property to `RentalDataContext.repositories.cs`.

### Task 4.3: Server-Side Push Service

**Create** `src/MotoRent.Services/PushNotificationService.cs`:
- Uses WebPush NuGet package
- `SubscribeAsync`, `UnsubscribeAsync`, `SendNotificationAsync`, `BroadcastAsync`

**Add** `WebPush` NuGet to `MotoRent.Services.csproj`.

### Task 4.4: Push API Controller

**Create** `src/MotoRent.Server/Controllers/Api/PushController.cs`:
- `POST /api/push/subscribe` - authenticated
- `DELETE /api/push/unsubscribe`
- `GET /api/push/vapid-key` - return public key

### Task 4.5: Blazor JS Interop

**Create** `src/MotoRent.Client/Interops/PushNotificationJsInterop.cs`

Register in both Server and Client `Program.cs`.

### Task 4.6: Notification Permission UI

**Create** `src/MotoRent.Client/Components/Shared/NotificationPermissionBanner.razor`:
- Shown in Staff/Manager layouts after login
- Checks if push supported and permission not granted
- On accept: request permission -> subscribe -> send to server

---

## Execution Order

| Batch | Tasks | Dependency |
|-------|-------|------------|
| 1 | 1.1, 1.2, 4.1, 4.2 | None - interfaces + entity creation |
| 2 | 1.3, 1.4, 4.3, 4.4 | Batch 1 (interfaces exist) |
| 3 | 2.1-2.6 | Batch 2 (API + Http services exist) |
| 4 | 3.1-3.3, 4.5, 4.6 | Batch 3 (WASM configured) |

---

## Verification

### Build Verification
```bash
dotnet build E:\project\work\motorent\MotoRent.sln
```

### Runtime Verification
1. Start app: `dotnet watch --project src/MotoRent.Server`
2. Navigate to Tourist page (`/tourist/{accountNo}/browse`)
3. Verify SSR on first load (DevTools Network - no WASM download initially)
4. Check `_framework/*.wasm` assets start downloading in background
5. Reload - verify WASM takes over (no SignalR `/_blazor` connection)
6. Test offline: disconnect network, verify SW serves cached content
7. Test SW update: modify version, verify update banner
8. Test push: enable notifications, verify subscription stored

### Regression Check
1. Staff/Manager/Admin pages still work as `InteractiveServer`
2. No broken service injections on non-Tourist pages
3. Build succeeds without WASM trimming errors
4. Existing Tourist page functionality unchanged
