# Coding Conventions

**Analysis Date:** 2026-01-19

## Naming Patterns

**Files:**
- Entity classes: PascalCase (`Vehicle.cs`, `Rental.cs`, `MaintenanceAlert.cs`)
- Partial entity files: `{Entity}.{Aspect}.cs` (e.g., `Vehicle.search.cs`, `Rental.Calculated.cs`)
- Services: `{Entity}Service.cs` (e.g., `VehicleService.cs`, `RentalService.cs`)
- Razor pages: PascalCase (`VehicleDialog.razor`, `BookingList.razor`)
- Razor code-behind: `{Component}.razor.cs` (if needed, most use `@code` blocks)
- Resource files: `{Component}.{culture}.resx` (e.g., `Home.th.resx`, `Home.en.resx`)

**Private Fields:**
- Use `m_` prefix for instance members: `m_context`, `m_loading`, `m_owners`
- Example from `PersistenceSession.cs`: `private RentalDataContext? m_context;`

**Private Static Fields:**
- Use `s_` prefix (documented convention, though less common in codebase)

**Constants:**
- Use SCREAMING_SNAKE_CASE for role constants: `SUPER_ADMIN`, `OrgAdmin`
- PascalCase for other constants within classes

**Functions/Methods:**
- PascalCase: `GetVehicleByIdAsync`, `LoadDashboardDataAsync`
- Async methods: Always suffix with `Async`
- Query methods: Prefix with `Get`, `Load`, or `Create`

**Variables:**
- camelCase for local variables: `vehicleStats`, `rentalStats`
- Private backing fields in Razor: `m_` prefix

**Types/Classes:**
- PascalCase: `Vehicle`, `RentalDataContext`, `SubmitOperation`
- Interfaces: `I` prefix: `IRepository<T>`, `IRequestContext`, `IModalService`
- Generic type parameters: Single uppercase letter: `T`, `TEntity`, `TResult`

## Code Style

**Formatting:**
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- File-scoped namespaces used throughout
- Nullable reference types enabled

**Braces:**
- Allman style (braces on new line) for classes and methods
- Single-line expression-bodied members for simple getters/methods

**Expression-Bodied Members:**
```csharp
// Preferred for simple members
public override int GetId() => this.VehicleId;
public override void SetId(int value) => this.VehicleId = value;
protected string UserName => this.RequestContext.GetUserName() ?? "system";
```

**Primary Constructors:**
```csharp
// Used for service classes (.NET 10)
public class VehicleService(RentalDataContext context, VehiclePoolService poolService)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService PoolService { get; } = poolService;
}
```

**Collection Expressions:**
```csharp
// Use [] syntax for empty collections
public List<UserAccount> AccountCollection { get; } = [];
private List<VehicleOwner> m_owners = [];
```

## Import Organization

**Order:**
1. System namespaces (implicit with `ImplicitUsings`)
2. Microsoft namespaces (`Microsoft.AspNetCore.Components`, etc.)
3. Third-party packages
4. Project namespaces (`MotoRent.Domain.*`, `MotoRent.Services.*`)

**Path Aliases:**
- None used; direct project references

## Error Handling

**Service Layer Pattern:**
```csharp
public async Task<SubmitOperation> CreateVehicleAsync(Vehicle vehicle, string username)
{
    using var session = this.Context.OpenSession(username);
    session.Attach(vehicle);
    return await session.SubmitChanges("Create");
}
```

**SubmitOperation Result Type:**
```csharp
// Success case
return SubmitOperation.CreateSuccess(inserted, updated, deleted);

// Failure case
return SubmitOperation.CreateFailure($"Cannot delete vehicle with {activeRentals} active rental(s).");
```

**Blazor Component Error Handling:**
```csharp
try
{
    m_loading = true;
    // operation
}
catch (Exception ex)
{
    Logger.LogError(ex, "Error loading dashboard data");
    ShowError("Failed to load dashboard data");
}
finally
{
    m_loading = false;
}
```

**Dialog Save Pattern:**
```csharp
private async Task SaveAsync()
{
    this.Saving = true;
    try
    {
        var result = this.IsNew
            ? await this.VehicleService.CreateVehicleAsync(this.Entity, this.UserName)
            : await this.VehicleService.UpdateVehicleAsync(this.Entity, this.UserName);

        if (result.Success)
        {
            this.Close();
        }
        else
        {
            this.ShowError(Localizer["SaveFailed", result.Message ?? ""]);
        }
    }
    catch (Exception ex)
    {
        this.ShowError(Localizer["Error", ex.Message]);
    }
    finally
    {
        this.Saving = false;
    }
}
```

## Logging

**Framework:** Microsoft.Extensions.Logging

**Patterns:**
```csharp
// Inject logger in component base
[Inject] protected ILogger<MotoRentComponentBase> Logger { get; set; } = null!;

// Log errors with exception
Logger.LogError(ex, "Error loading dashboard data");
```

## Comments

**When to Comment:**
- XML documentation on public entities and service methods
- Inline comments for complex business logic

**XML Documentation:**
```csharp
/// <summary>
/// Represents a user in the system. Users can belong to multiple organizations
/// via their AccountCollection.
/// </summary>
public class User : Entity
{
    /// <summary>
    /// Gets the roles for a specific account.
    /// </summary>
    public IEnumerable<string> GetRoles(string accountNo) { ... }
}
```

**Region Usage:**
```csharp
#region Query Methods
// ... methods
#endregion

#region CRUD Operations
// ... methods
#endregion
```

## Function Design

**Size:**
- Methods typically 10-30 lines
- Complex operations split into helper methods

**Parameters:**
- Use optional parameters with defaults for filtering:
```csharp
public async Task<LoadOperation<Vehicle>> GetVehiclesAsync(
    int shopId,
    VehicleType? vehicleType = null,
    string? status = null,
    string? searchTerm = null,
    bool includePooled = false,
    int page = 1,
    int pageSize = 20)
```

**Return Values:**
- Use `Task<T>` for async operations
- Return `SubmitOperation` for CRUD operations
- Return `LoadOperation<T>` for paginated queries
- Return nullable for single-entity lookups: `Task<T?>`

## Module Design

**Exports:**
- One primary class per file
- Related helper classes/DTOs in same file for services

**Entity Pattern:**
```csharp
public partial class Vehicle : Entity
{
    public int VehicleId { get; set; }
    // ... properties

    public override int GetId() => this.VehicleId;
    public override void SetId(int value) => this.VehicleId = value;
}
```

**Service Pattern:**
```csharp
public class VehicleService(RentalDataContext context, VehiclePoolService poolService)
{
    private RentalDataContext Context { get; } = context;

    public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId)
    {
        return await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);
    }
}
```

## Blazor Component Conventions

**Base Classes:**
- Pages/Components: Inherit from `LocalizedComponentBase<T>` or `MotoRentComponentBase`
- Dialogs: Inherit from `LocalizedDialogBase<TEntity, TComponent>` or `MotoRentDialogBase<TEntity>`

**Component Structure:**
```razor
@page "/path"
@using MotoRent.Domain.Entities
@attribute [Authorize(Policy = "RequireTenantStaff")]
@inherits LocalizedComponentBase<ComponentName>
@inject ServiceType ServiceName

<MotoRentPageTitle>@Localizer["Title"]</MotoRentPageTitle>
<TablerHeader Title="@Localizer["Title"]" PreTitle="@Localizer["Subtitle"]" />

@* Content *@

@code {
    private bool m_loading = true;
    private List<Entity> m_items = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }
}
```

**Localization:**
- Use `@Localizer["Key"]` for all user-facing text
- Resource files at: `Resources/{Path}/{Component}.{culture}.resx`
- Cultures: default `.resx`, `.en.resx`, `.th.resx`, `.ms.resx`

**Form Patterns:**
```razor
<form id="@FormId" @onsubmit="SaveAsync" @onsubmit:preventDefault>
    @* Form content *@
</form>

<div class="modal-footer">
    <button type="button" class="btn btn-ghost-secondary" @onclick="Cancel">
        @Localizer["Cancel"]
    </button>
    <button type="submit" form="@FormId" class="btn btn-primary" disabled="@Saving">
        @if (Saving)
        {
            <span class="spinner-border spinner-border-sm me-2" role="status"></span>
        }
        @SaveButtonText
    </button>
</div>
```

## JSON Serialization

**Polymorphism:**
```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Vehicle), nameof(Vehicle))]
[JsonDerivedType(typeof(Rental), nameof(Rental))]
// ... all entity types
public abstract class Entity { ... }
```

**Conditional Serialization:**
```csharp
[JsonIgnore]
public bool IsThirdPartyOwned => this is { VehicleOwnerId: > 0 };

[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string? VehiclePoolName { get; set; }
```

---

*Convention analysis: 2026-01-19*
