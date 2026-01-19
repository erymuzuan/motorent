# Testing Patterns

**Analysis Date:** 2026-01-19

## Test Framework

**Runner:**
- xUnit 2.9.2
- Config: `tests/MotoRent.Domain.Tests/MotoRent.Domain.Tests.csproj`

**Assertion Library:**
- xUnit built-in assertions (`Assert.Equal`, `Assert.True`, etc.)

**Coverage Tool:**
- coverlet.collector 6.0.2

**Run Commands:**
```bash
dotnet test                                    # Run all tests
dotnet test --filter "FullyQualifiedName~Tests"  # Run filtered tests
dotnet test --collect:"XPlat Code Coverage"   # Run with coverage
```

## Test File Organization

**Location:**
- Separate test project: `tests/MotoRent.Domain.Tests/`
- Mirrors source structure

**Naming:**
- Test files: `{Entity}Tests.cs` (e.g., `MaintenanceAlertTests.cs`)
- Test classes: `{Entity}Tests`
- Test methods: `{Method}_Should{ExpectedBehavior}` or descriptive names

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
        Assert.Equal(MaintenanceStatus.Overdue, alert.Status);
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
- Arrange-Act-Assert (AAA) pattern
- One assertion per concept (multiple related assertions OK)
- Descriptive test names

## Mocking

**Framework:** Not currently used

**Current State:**
- Tests focus on domain entity behavior (pure unit tests)
- No service layer tests with mocking detected
- No integration tests detected

**Recommended Pattern (if adding mocks):**
```csharp
// Would use Moq or NSubstitute for service testing
// Currently not implemented in codebase
```

**What to Mock:**
- External services (database, APIs)
- `IRepository<T>` implementations
- `RentalDataContext` for service tests

**What NOT to Mock:**
- Entity classes
- Value objects
- Pure domain logic

## Fixtures and Factories

**Test Data:**
```csharp
// Currently inline in tests
var alert = new MaintenanceAlert
{
    MaintenanceAlertId = 1,
    VehicleId = 10,
    ServiceTypeId = 5,
    Status = MaintenanceStatus.Overdue
};
```

**Location:**
- No dedicated fixture/factory files
- Test data created inline in each test

**Recommended Pattern (if needed):**
```csharp
// Create test builders or factories in:
// tests/MotoRent.Domain.Tests/Builders/
public static class VehicleBuilder
{
    public static Vehicle CreateDefault() => new Vehicle
    {
        VehicleId = 1,
        Brand = "Honda",
        Model = "Click",
        Status = VehicleStatus.Available
    };
}
```

## Coverage

**Requirements:** None enforced

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Results in: tests/MotoRent.Domain.Tests/TestResults/*/coverage.cobertura.xml
```

**Current State:**
- Coverage tool configured but no enforced thresholds
- Limited test coverage (only `MaintenanceAlertTests.cs` exists)

## Test Types

**Unit Tests:**
- Focus: Domain entity behavior
- Location: `tests/MotoRent.Domain.Tests/`
- Examples: Entity property tests, `GetId()`/`SetId()` contract tests

**Integration Tests:**
- Not currently implemented
- Would test: Service + Repository + Database

**E2E Tests:**
- Not implemented
- No Playwright or Selenium configuration detected

## Common Patterns

**Entity Contract Testing:**
```csharp
[Fact]
public void GetId_ShouldReturnEntityId()
{
    var entity = new Entity { EntityId = 42 };
    Assert.Equal(42, entity.GetId());
}

[Fact]
public void SetId_ShouldSetEntityId()
{
    var entity = new Entity();
    entity.SetId(99);
    Assert.Equal(99, entity.GetId());
}
```

**Property Initialization Testing:**
```csharp
[Fact]
public void Entity_ShouldSetProperties()
{
    var entity = new Entity
    {
        Property1 = value1,
        Property2 = value2
    };

    Assert.Equal(value1, entity.Property1);
    Assert.Equal(value2, entity.Property2);
}
```

**Default Value Testing:**
```csharp
[Fact]
public void Entity_ShouldHaveDefaultValues()
{
    var entity = new Entity();

    Assert.False(entity.IsRead);
    Assert.Equal(0, entity.EntityId);
}
```

## Test Project Configuration

**Project File:** `tests/MotoRent.Domain.Tests/MotoRent.Domain.Tests.csproj`
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
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MotoRent.Domain\MotoRent.Domain.csproj" />
  </ItemGroup>
</Project>
```

## Testing Gaps

**Not Covered:**
- Service layer tests (`VehicleService`, `RentalService`, etc.)
- Repository layer tests
- Blazor component tests
- Integration tests with database
- API endpoint tests
- Authentication/authorization tests

**Recommended Additions:**
1. Add service tests with mocked repositories
2. Add bUnit tests for Blazor components
3. Add integration tests for critical workflows (rental check-in/check-out)

## Adding New Tests

**For Domain Entities:**
1. Create `{Entity}Tests.cs` in `tests/MotoRent.Domain.Tests/`
2. Test `GetId()`, `SetId()` contract
3. Test property initialization
4. Test computed properties and helper methods

**For Services (recommended pattern):**
```csharp
public class VehicleServiceTests
{
    private readonly Mock<RentalDataContext> m_contextMock;
    private readonly VehicleService m_service;

    public VehicleServiceTests()
    {
        m_contextMock = new Mock<RentalDataContext>();
        m_service = new VehicleService(m_contextMock.Object, null);
    }

    [Fact]
    public async Task GetVehicleByIdAsync_WithValidId_ReturnsVehicle()
    {
        // Arrange
        var expected = new Vehicle { VehicleId = 1 };
        m_contextMock.Setup(c => c.LoadOneAsync<Vehicle>(It.IsAny<Expression<Func<Vehicle, bool>>>()))
            .ReturnsAsync(expected);

        // Act
        var result = await m_service.GetVehicleByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VehicleId);
    }
}
```

---

*Testing analysis: 2026-01-19*
