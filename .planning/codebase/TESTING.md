# Testing Patterns

**Analysis Date:** 2026-01-23

## Test Framework

**Runner:**
- xUnit 2.9.2
- Config: `tests/MotoRent.Domain.Tests/MotoRent.Domain.Tests.csproj`

**Assertion Library:**
- xUnit built-in assertions (`Assert.Equal`, `Assert.True`, etc.)

**Coverage:**
- Coverlet.collector 6.0.2 included

**Run Commands:**
```bash
dotnet test                                    # Run all tests
dotnet test --filter "FullyQualifiedName~Tests"  # Run specific tests
dotnet test --collect:"XPlat Code Coverage"    # With coverage
```

## Test File Organization

**Location:**
- Separate `tests/` directory at solution root
- Project naming: `MotoRent.{Layer}.Tests` (e.g., `MotoRent.Domain.Tests`)

**Naming:**
- Test files: `{EntityName}Tests.cs` (e.g., `MaintenanceAlertTests.cs`)
- Test classes: `{EntityName}Tests`
- Test methods: `{Method}_Should{Behavior}` or descriptive names

**Structure:**
```
tests/
└── MotoRent.Domain.Tests/
    ├── MotoRent.Domain.Tests.csproj
    └── MaintenanceAlertTests.cs
```

## Test Structure

**Suite Organization:**
```csharp
using MotoRent.Domain.Entities;
using Xunit;

namespace MotoRent.Domain.Tests;

public class MaintenanceAlertTests
{
    [Fact]
    public void MaintenanceAlert_ShouldSetProperties()
    {
        // Arrange
        var alert = new MaintenanceAlert
        {
            MaintenanceAlertId = 1,
            VehicleId = 10,
            ServiceTypeId = 5,
            Status = MaintenanceStatus.Overdue,
            TriggerMileage = 1000,
            VehicleName = "Honda Click",
            LicensePlate = "ABC-123"
        };

        // Assert
        Assert.Equal(1, alert.MaintenanceAlertId);
        Assert.Equal(10, alert.VehicleId);
        Assert.Equal(5, alert.ServiceTypeId);
        Assert.Equal(MaintenanceStatus.Overdue, alert.Status);
        Assert.Equal(1000, alert.TriggerMileage);
        Assert.Equal("Honda Click", alert.VehicleName);
        Assert.Equal("ABC-123", alert.LicensePlate);
        Assert.False(alert.IsRead);
    }

    [Fact]
    public void GetId_ShouldReturnMaintenanceAlertId()
    {
        // Arrange
        var alert = new MaintenanceAlert { MaintenanceAlertId = 42 };

        // Assert
        Assert.Equal(42, alert.GetId());
    }

    [Fact]
    public void SetId_ShouldSetMaintenanceAlertId()
    {
        // Arrange
        var alert = new MaintenanceAlert();

        // Act
        alert.SetId(99);

        // Assert
        Assert.Equal(99, alert.MaintenanceAlertId);
        Assert.Equal(99, alert.GetId());
    }
}
```

**Patterns:**
- AAA Pattern: Arrange-Act-Assert
- Single assertion focus per test when possible
- Descriptive test method names

## Mocking

**Framework:** Not currently configured

**Current Approach:**
- Direct entity instantiation for unit tests
- No mock framework references in test project

**Recommended Setup (if needed):**
```csharp
// Add to test project: Moq or NSubstitute
<PackageReference Include="Moq" Version="4.20.0" />

// Example mock pattern
var mockContext = new Mock<RentalDataContext>();
mockContext.Setup(c => c.LoadOneAsync<Rental>(It.IsAny<Expression<Func<Rental, bool>>>()))
    .ReturnsAsync(new Rental { RentalId = 1 });
```

**What to Mock:**
- External services (Gemini API, RabbitMQ)
- Database context for service tests
- IHttpClientFactory

**What NOT to Mock:**
- Entity objects (instantiate directly)
- Simple value objects
- LINQ expressions for query tests

## Fixtures and Factories

**Test Data:**
```csharp
// Direct instantiation with object initializers
var alert = new MaintenanceAlert
{
    MaintenanceAlertId = 1,
    VehicleId = 10,
    ServiceTypeId = 5,
    Status = MaintenanceStatus.Overdue
};
```

**Location:**
- Test data created inline in test methods
- No shared fixture files detected

**Recommended Pattern (for expansion):**
```csharp
// Create TestData directory with builders
public class RentalBuilder
{
    private int m_rentalId = 1;
    private string m_status = "Active";

    public RentalBuilder WithId(int id) { m_rentalId = id; return this; }
    public RentalBuilder WithStatus(string status) { m_status = status; return this; }
    public Rental Build() => new() { RentalId = m_rentalId, Status = m_status };
}
```

## Coverage

**Requirements:** Not enforced (no coverage thresholds configured)

**View Coverage:**
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Results in TestResults directory
# Use ReportGenerator for HTML reports
```

## Test Types

**Unit Tests:**
- Entity property tests (currently implemented)
- Entity method tests (GetId/SetId)
- Focus on domain logic

**Integration Tests:**
- Not currently implemented
- Would test: Repository operations, Service workflows

**E2E Tests:**
- Playwright directory exists (`qa.tests/`) but contains documentation, not tests
- No automated E2E test framework detected

## Common Patterns

**Entity Testing:**
```csharp
[Fact]
public void Entity_ShouldSetAndGetId()
{
    // Arrange
    var entity = new Vehicle();

    // Act
    entity.SetId(42);

    // Assert
    Assert.Equal(42, entity.GetId());
    Assert.Equal(42, entity.VehicleId);
}
```

**Property Testing:**
```csharp
[Fact]
public void Entity_ShouldInitializeWithDefaults()
{
    // Arrange & Act
    var rental = new Rental();

    // Assert
    Assert.Equal(0, rental.RentalId);
    Assert.Null(rental.Notes);
    Assert.Equal(string.Empty, rental.Status);
}
```

**Enum Testing:**
```csharp
[Fact]
public void Entity_ShouldAcceptEnumValues()
{
    // Arrange
    var alert = new MaintenanceAlert();

    // Act
    alert.Status = MaintenanceStatus.Overdue;

    // Assert
    Assert.Equal(MaintenanceStatus.Overdue, alert.Status);
}
```

## Test Coverage Gaps

**Untested Areas:**
- Services (`MotoRent.Services/*`) - No service tests
- Repository operations - No integration tests
- Blazor components - No component tests
- API endpoints - No endpoint tests
- Most entities - Only `MaintenanceAlert` has tests

**Files Without Tests:**
- `src/MotoRent.Services/RentalService.cs`
- `src/MotoRent.Services/VehicleService.cs`
- `src/MotoRent.Services/DocumentOcrService.cs`
- `src/MotoRent.Domain/DataContext/RentalDataContext.cs`
- `src/MotoRent.Domain/DataContext/Repository.cs`

**Risk:** Critical business logic (check-in, check-out workflows) has no automated tests

**Priority:**
- High: Service layer tests for RentalService workflows
- High: Repository integration tests
- Medium: Entity tests for all domain objects
- Low: Blazor component tests

## Project Reference

**Test Project Configuration:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MotoRent.Domain\MotoRent.Domain.csproj" />
  </ItemGroup>
</Project>
```

## Recommendations for New Tests

**Adding Entity Tests:**
1. Create file: `tests/MotoRent.Domain.Tests/{EntityName}Tests.cs`
2. Follow existing pattern from `MaintenanceAlertTests.cs`
3. Test: Property initialization, GetId/SetId, default values

**Adding Service Tests:**
1. Create project: `tests/MotoRent.Services.Tests/`
2. Add mock framework (Moq recommended)
3. Test: CRUD operations, workflow methods, error handling

**Adding Integration Tests:**
1. Create project: `tests/MotoRent.Integration.Tests/`
2. Use test database or SQL Server LocalDB
3. Test: Repository operations, data context, multi-entity workflows

---

*Testing analysis: 2026-01-23*
