# GitHub Copilot Instructions for MotoRent

## Project Context
MotoRent is a motorbike rental system for Thailand tourist areas (Phuket, Krabi, etc.). Built with Blazor Server + WASM PWA using MudBlazor UI components and custom repository pattern with JSON storage.

## Code Style & Conventions

### Naming Conventions
- **Private fields**: Use `m_` prefix (e.g., `m_context`, `m_loading`)
- **Static fields**: Use `s_` prefix (e.g., `s_defaultOptions`)
- **Constants**: Use `c_` prefix or PascalCase
- **Async methods**: Always use `Async` suffix

### File Organization Pattern
```csharp
// 1. Usings (sorted, no unnecessary)
// 2. Namespace (file-scoped when possible)
// 3. Type declaration
// 4. Constants
// 5. Static fields (s_ prefix)
// 6. Instance fields (m_ prefix)
// 7. Constructor
// 8. Properties
// 9. Public methods
// 10. Private methods
```

### Entity Pattern
All entities inherit from `Entity` base class:
- Use `GetId()` / `SetId()` abstract methods for primary key
- Include `WebId` property for client-side tracking
- Audit fields are JsonIgnored: `CreatedBy`, `ChangedBy`, `CreatedTimestamp`, `ChangedTimestamp`

## Repository Pattern

### Loading Data
```csharp
var context = new RentalDataContext();
var item = await context.LoadOneAsync<Rental>(r => r.RentalId == id);

var query = context.Rentals.Where(r => r.ShopId == shopId);
var result = await context.LoadAsync(query, page: 1, size: 20);
```

### Saving Data
```csharp
using var session = context.OpenSession();
session.Attach(entity);
await session.SubmitChanges("OperationName");  // Operation publishes events
```

### Deleting Data
```csharp
using var session = context.OpenSession();
session.Delete(entity);
await session.SubmitChanges("Delete");
```

## Blazor Component Patterns

### Component Structure
- Use `@inject` for dependency injection
- Private fields use `m_` prefix in `@code` blocks
- Always clone entities before editing: `var cloned = entity.Clone();`
- Use MudBlazor components (MudDataGrid, MudDialog, MudForm, etc.)

### Dialog Pattern
- **Clone objects** before passing to dialogs (preserve original on cancel)
- **Persistence in parent** component, not in dialog
- Use `DialogBase<TItem>` base class for dialogs
- Use descriptive operation names in `SubmitChanges()`

```csharp
private async Task EditItem(Item? item)
{
    var isNew = item is null;
    item = isNew ? new Item() : item.Clone();
    
    var dialog = await DialogService.ShowAsync<ItemDialog>("Edit", 
        new DialogParameters<ItemDialog> { { x => x.Item, item } });
    
    var result = await dialog.Result;
    if (result is { Canceled: false, Data: Item updated })
    {
        using var session = DataContext.OpenSession();
        session.Attach(updated);
        await session.SubmitChanges(isNew ? "Add" : "Edit");
    }
}
```

## JSON Serialization
- Use `System.Text.Json` (not Newtonsoft.Json)
- Entity polymorphism via `[JsonPolymorphic]` and `[JsonDerivedType]` attributes
- Use `entity.ToJsonString()` and `json.DeserializeFromJson<T>()`
- Clone via JSON: `var clone = entity.Clone();`

## Database Design
- Tables use JSON columns with computed columns for indexing
- Computed columns extract frequently queried fields
- Pattern: `[ColumnName] AS CAST(JSON_VALUE([Json], '$.Property') AS TYPE)`

## Entity Status Values

### Motorbike.Status
- `Available` - Ready for rental
- `Rented` - Currently rented out
- `Maintenance` - Under repair/service

### Rental.Status
- `Reserved` - Booking confirmed
- `Active` - Currently rented
- `Completed` - Returned successfully
- `Cancelled` - Booking cancelled

### Deposit.Status
- `Held` - Deposit collected
- `Refunded` - Returned to customer
- `Forfeited` - Kept due to damage

## Extension Methods
Available extensions to use:
- **String**: `IsNullOrEmpty()`, `HasValue()`, `Truncate()`, `FormatThaiPhone()`
- **Collection**: `AddOrReplace()`, `ClearAndAddRange()`, `IsEmpty()`, `HasItems()`
- **DateTime**: `ThailandNow()`, `ToThaiDateString()`, `CalculateRentalDays()`
- **Decimal**: `ToThb()`, `RoundMoney()`, `IsPositive()`
- **Entity**: `IsNew()`, `Exists()`

## Rental Workflow

### Check-In
1. Select/Register Renter (with OCR support)
2. Select Motorbike (available only)
3. Choose Add-ons (insurance, accessories)
4. Collect Deposit (cash/card)
5. Capture Before Photos
6. Sign Agreement
7. Confirm & Receipt

### Check-Out
1. Find Active Rental
2. Record Return (mileage, time)
3. Capture After Photos
4. Damage Assessment
5. Calculate Final Charges
6. Process Payment
7. Refund Deposit
8. Complete Rental

## MudBlazor Theme
- Primary color: Tropical Teal (`#00897B`)
- Use Material Design icons from `Icons.Material.Filled`
- Prefer `MudDataGrid<T>` for tables with pagination
- Use `ISnackbar` for notifications

## Localization
- Support English (default) and Thai
- Use `IStringLocalizer<T>` injection
- Resource files: `Resources/Pages/{Page}.resx` and `Resources/Pages/{Page}.th.resx`
- Never hardcode app name in strings

## Messaging & Events
- `SubmitChanges(operation)` publishes messages to RabbitMQ
- Routing key format: `{Entity}.{Crud}.{Operation}`
- Example: `Rental.Changed.CheckIn`
- Use subscribers for async processing

## OCR Integration
- Use `DocumentOcrService` for passport/license scanning
- Gemini Flash API for OCR
- Always allow manual correction of extracted data
- Save raw OCR JSON for debugging

## Best Practices
- Keep business logic in services, not in components
- Use short-lived persistence sessions
- Validate motorbike availability before rental
- Calculate totals including insurance and accessories
- Document damage with before/after photos
- Use descriptive operation names for event routing

## Project Structure
```
src/
├── MotoRent.Server/        # Blazor Server host
├── MotoRent.Client/        # WASM client (PWA)
├── MotoRent.Domain/        # Entities & DataContext
└── MotoRent.Services/      # Business services
```

## Connection String
Default: `Server=localhost;Database=MotoRent;Trusted_Connection=True;TrustServerCertificate=True;`

Configure in `appsettings.json` under `ConnectionStrings:MotoRent`
