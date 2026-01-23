# Coding Conventions

**Analysis Date:** 2026-01-23

## Naming Patterns

**Files:**
- Entities: PascalCase singular noun (e.g., `Rental.cs`, `Vehicle.cs`)
- Services: PascalCase with `Service` suffix (e.g., `RentalService.cs`, `VehicleService.cs`)
- Razor pages: PascalCase (e.g., `CheckIn.razor`, `AccessoryDialog.razor`)
- Razor code-behind: Same name with `.razor.cs` extension
- Resource files: Same name as Razor file with culture suffix (e.g., `Home.resx`, `Home.th.resx`)

**Functions:**
- PascalCase for all methods
- Async methods: Always use `Async` suffix (e.g., `LoadRentalAsync`, `SaveAsync`)
- Event handlers: Prefixed with `On` (e.g., `OnIncludedChanged`)

**Variables:**
- Private instance fields: `m_` prefix (e.g., `m_loading`, `m_activeRentals`, `m_context`)
- Static fields: `s_` prefix (e.g., `s_defaultOptions`, `s_jsonOptions`)
- Constants: `UPPERCASE_WITH_UNDERSCORES` or PascalCase
- Local variables: camelCase
- Parameters: camelCase

**Types:**
- Classes: PascalCase
- Interfaces: `I` prefix (e.g., `IRepository<T>`, `IRequestContext`, `IModalService`)
- Entities: PascalCase, inherit from `Entity` base class
- DTOs/Records: PascalCase (e.g., `CheckInRequest`, `CheckOutResult`)

## Code Style

**Formatting:**
- Nullable reference types: Enabled
- Implicit usings: Enabled
- File-scoped namespaces: Used (single line `namespace X;`)

**Linting:**
- No formal .editorconfig detected
- Code standards documented in `.claude/skills/code-standards/SKILL.md`

**Key Rules:**
- Always use `this` keyword when referencing instance members
- Use `base` keyword when referencing base class members
- Prefer modern C# features (pattern matching, records, collection expressions)

## Import Organization

**Order:**
1. System namespaces
2. Microsoft namespaces
3. Third-party packages
4. Project namespaces (e.g., `MotoRent.Domain.Entities`, `MotoRent.Services`)

**Path Aliases:**
- None detected - uses standard namespace imports

## Error Handling

**Patterns:**

**Try-Catch with Logging:**
```csharp
try
{
    await this.ProcessAsync();
}
catch (Exception ex)
{
    this.Logger.LogError(ex, "Failed to process {EntityId}", entity.Id);
    this.ShowError("An error occurred");
}
```

**Result Pattern (SubmitOperation):**
```csharp
// Services return SubmitOperation for CRUD results
public static SubmitOperation CreateSuccess(int inserted = 0, int updated = 0, int deleted = 0);
public static SubmitOperation CreateFailure(string message, Exception? exception = null);

// Check result in calling code
var result = await this.Service.CreateAsync(entity, this.UserName);
if (result.Success)
{
    this.ShowSuccess("Saved");
    this.Close();
}
else
{
    this.ShowError(result.Message ?? "Save failed");
}
```

**Workflow Results (CheckInResult, CheckOutResult):**
```csharp
// Static factory methods for success/failure
public static CheckInResult CreateSuccess(int rentalId);
public static CheckInResult CreateFailure(string message);
```

**Loading State Pattern:**
```csharp
private bool m_loading;

protected override async Task OnInitializedAsync()
{
    this.m_loading = true;
    try
    {
        await this.LoadDataAsync();
    }
    catch (Exception ex)
    {
        this.Logger.LogError(ex, "Error loading data");
        this.ShowError("Failed to load data");
    }
    finally
    {
        this.m_loading = false;
    }
}
```

## Logging

**Framework:** Microsoft.Extensions.Logging

**Patterns:**
```csharp
// Inject logger in components
[Inject] protected ILogger<MotoRentComponentBase> Logger { get; set; } = null!;

// Structured logging with templates
this.Logger.LogInformation("Processing rental {RentalId}", rentalId);
this.Logger.LogError(ex, "Failed to extract document data from Gemini API");
this.Logger.LogWarning("Gemini API key not configured, returning empty extraction");
```

## Comments

**When to Comment:**
- XML docs for public APIs and service methods
- Brief explanations for non-obvious logic
- TODO comments for incomplete features

**JSDoc/TSDoc:**
- XML documentation comments used for C# public APIs

```csharp
/// <summary>
/// Creates a new query for the specified entity type.
/// Preferred pattern over using Query properties directly.
/// </summary>
public Query<T> CreateQuery<T>() where T : Entity, new()
```

## Function Design

**Size:**
- Single responsibility principle
- Extract complex logic to private helper methods
- Use partial classes instead of #region/#endregion for organizing large classes

**Parameters:**
- Use named parameters for clarity when calling methods with many parameters
- Use default parameter values where appropriate
- Prefer object parameters for methods with many inputs (e.g., `CheckInRequest`, `CheckOutRequest`)

**Return Values:**
- Use `Task<T>` for async operations
- Use nullable return types when entity may not exist (e.g., `Task<Rental?>`)
- Use result objects for operations that can fail (e.g., `SubmitOperation`, `CheckInResult`)

## Module Design

**Exports:**
- Services registered via DI in `ServicesExtensions.cs`
- Repositories registered as singletons

**Barrel Files:**
- Not used - explicit imports preferred

## Service Injection Pattern

```csharp
// Primary constructor injection (modern C# pattern)
public class RentalService(
    RentalDataContext context,
    VehiclePoolService? poolService = null,
    BookingService? bookingService = null)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService? PoolService { get; } = poolService;
}
```

## Entity Pattern

```csharp
public class Accessory : Entity
{
    public int AccessoryId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public int QuantityAvailable { get; set; }
    public bool IsIncluded { get; set; }

    public override int GetId() => this.AccessoryId;
    public override void SetId(int value) => this.AccessoryId = value;
}
```

## Blazor Component Patterns

**Base Classes:**
- `MotoRentComponentBase`: Core services (DataContext, Logger, ToastService, DialogService)
- `LocalizedComponentBase<T>`: Adds type-safe localization
- `MotoRentDialogBase<TEntity>`: Dialog handling
- `LocalizedDialogBase<TEntity, TLocalizer>`: Localized dialogs

**Component Declaration:**
```razor
@page "/rentals/checkin"
@inherits LocalizedComponentBase<CheckIn>
@inject RentalService RentalService
```

**Private Fields in @code:**
```csharp
@code {
    private bool m_loading = true;
    private int m_activeRentals;
    private List<Rental> m_recentRentals = [];
}
```

## Localization

**Pattern:**
- Use `@Localizer["Key"]` in Razor files
- Resource files in `Resources/` mirroring page structure
- Cultures: `.resx` (default), `.en.resx`, `.th.resx`, `.ms.resx`

**Shared Localizer:**
- `CommonResources` for commonly used text
- Access via `@CommonLocalizer["Key"]`

## JSON Serialization

**Framework:** System.Text.Json with custom configuration

**Configuration (`JsonSerializerService.cs`):**
```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = pretty,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
};
```

**Polymorphic Serialization:**
```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Rental), nameof(Rental))]
[JsonDerivedType(typeof(Vehicle), nameof(Vehicle))]
public abstract class Entity { }
```

## Repository Pattern

**Usage:**
```csharp
// Load data
var context = new RentalDataContext();
var query = context.CreateQuery<Rental>()
    .Where(r => r.Status == "Active")
    .OrderByDescending(r => r.RentalId);
var result = await context.LoadAsync(query, page: 1, size: 20, includeTotalRows: true);

// Save data
using var session = context.OpenSession("username");
session.Attach(rental);
await session.SubmitChanges("Create");
```

## The `this` Keyword Rule (CRITICAL)

**Always use `this` when referencing instance members:**

```csharp
// WRONG - missing this keyword
m_loading = true;
DataContext.LoadAsync(query);
ShowSuccess("Saved");

// CORRECT - always use this
this.m_loading = true;
this.DataContext.LoadAsync(query);
this.ShowSuccess("Saved");
```

This applies to:
- Private fields (`this.m_field`)
- Properties (`this.PropertyName`)
- Methods (`this.MethodName()`)
- Injected services (`this.DataContext`, `this.DialogService`)

---

*Convention analysis: 2026-01-23*
