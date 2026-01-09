# Code Standards

C# coding conventions and patterns for MotoRent.

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Class | PascalCase | `RentalService` |
| Interface | IPascalCase | `IRentalService` |
| Method | PascalCase | `GetActiveRentals()` |
| Property | PascalCase | `RentalId` |
| Parameter | camelCase | `rentalId` |
| Local variable | camelCase | `activeRentals` |
| Private field | m_camelCase | `m_rentals` |
| Constant | PascalCase | `MaxDailyRate` |
| Static readonly | s_camelCase | `s_defaultOptions` |

## File Organization

```csharp
// 1. Usings (sorted, no unnecessary)
using System.Text.Json;
using MotoRent.Domain.Entities;

// 2. Namespace
namespace MotoRent.Services;

// 3. Type declaration
public class RentalService
{
    // 4. Constants
    private const int MaxRentalDays = 30;

    // 5. Static fields
    private static readonly JsonSerializerOptions s_options = new();

    // 6. Instance fields (m_ prefix)
    private readonly RentalDataContext m_context;
    private List<Rental> m_cachedRentals = [];

    // 7. Constructor
    public RentalService(RentalDataContext context)
    {
        m_context = context;
    }

    // 8. Properties
    public int ShopId { get; set; }

    // 9. Public methods
    public async Task<Rental?> GetRentalAsync(int id)
    {
        return await m_context.LoadOneAsync<Rental>(r => r.RentalId == id);
    }

    // 10. Private methods
    private void ValidateRental(Rental rental)
    {
        // ...
    }
}
```

## Expression-Bodied Members

```csharp
// Properties
public int RentalId { get; set; }
public string FullName => $"{FirstName} {LastName}";

// Methods (single expression)
public override int GetId() => this.RentalId;
public override void SetId(int value) => this.RentalId = value;

// Methods (multiple statements - use block body)
public async Task SaveAsync()
{
    using var session = m_context.OpenSession();
    session.Attach(this);
    await session.SubmitChanges("Save");
}
```

## Null Handling

```csharp
// Nullable reference types (enabled in project)
public string? OptionalField { get; set; }
public string RequiredField { get; set; } = string.Empty;

// Null checks
if (rental is null)
    return;

// Pattern matching
if (rental is { Status: "Active" })
    ProcessActive(rental);

// Null coalescing
var name = rental?.RenterName ?? "Unknown";

// Null-forgiving (only when certain)
var item = list.FirstOrDefault()!;
```

## Collections

```csharp
// Use collection expressions
private List<Rental> m_rentals = [];
private Dictionary<int, Rental> m_cache = [];

// LINQ patterns
var activeRentals = m_rentals
    .Where(r => r.Status == "Active")
    .OrderByDescending(r => r.StartDate)
    .ToList();

// Prefer foreach for side effects
foreach (var rental in rentals)
{
    rental.Status = "Completed";
}
```

## Async/Await

```csharp
// Always use async suffix
public async Task<Rental?> LoadRentalAsync(int id)

// Always await or return
public async Task ProcessAsync()
{
    await DoWorkAsync();
}

// Fire and forget (rare, use with caution)
_ = Task.Run(async () => await BackgroundWorkAsync());

// Parallel operations
await Task.WhenAll(
    LoadRentalsAsync(),
    LoadMotorbikesAsync(),
    LoadRentersAsync()
);
```

## Entity Patterns

```csharp
public class Rental : Entity
{
    // Primary key
    public int RentalId { get; set; }

    // Foreign keys
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int MotorbikeId { get; set; }

    // Required strings
    public string Status { get; set; } = "Reserved";

    // Optional strings
    public string? Notes { get; set; }

    // Dates
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? ActualEndDate { get; set; }

    // Money
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }

    // Entity base implementation
    public override int GetId() => this.RentalId;
    public override void SetId(int value) => this.RentalId = value;
}
```

## Blazor Components

```razor
@* Component file naming: PascalCase.razor *@
@* Code-behind: PascalCase.razor.cs *@

@page "/rentals"
@inject RentalDataContext DataContext
@inject ISnackbar Snackbar

<PageTitle>Rentals</PageTitle>

@* Component markup *@

@code {
    // Fields with m_ prefix
    private List<Rental> m_rentals = [];
    private bool m_loading;

    // Lifecycle methods
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // Event handlers
    private async Task OnSaveClicked()
    {
        // ...
    }

    // Private methods
    private async Task LoadDataAsync()
    {
        m_loading = true;
        try
        {
            var result = await DataContext.LoadAsync(DataContext.Rentals);
            m_rentals = result.ItemCollection.ToList();
        }
        finally
        {
            m_loading = false;
        }
    }
}
```

## Error Handling

```csharp
// Use try-catch for expected errors
try
{
    await ProcessRentalAsync(rental);
}
catch (ValidationException ex)
{
    Snackbar.Add(ex.Message, Severity.Warning);
}
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to process rental {RentalId}", rental.RentalId);
    Snackbar.Add("An error occurred", Severity.Error);
}

// Throw for programming errors
if (rental is null)
    throw new ArgumentNullException(nameof(rental));
```

## Comments

```csharp
// Single-line for brief explanations
// Calculate total including insurance

/// <summary>
/// XML doc for public APIs
/// </summary>
/// <param name="rentalId">The rental identifier</param>
/// <returns>The rental or null if not found</returns>
public async Task<Rental?> GetRentalAsync(int rentalId)

// Avoid obvious comments
// BAD: Increment counter
// GOOD: (no comment needed for i++)
```

## Source
- Based on: `D:\project\work\rx-erp` patterns
- Microsoft C# Coding Conventions

```