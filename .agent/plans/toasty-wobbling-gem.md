# Plan: Centralized Error Logging with Localhost Rethrow

## Objective
Add a helper method in `MotoRentComponentBase` that logs errors and rethrows in localhost, so all components can use it without modifying each page individually.

## Approach
Add a new `LogError` helper method in the base class that:
1. Logs the error using `Logger.LogError`
2. Checks if running in localhost
3. Rethrows the exception in localhost to show full stack trace via `ErrorPage.razor`

## File to Modify

### `src/MotoRent.Client/Controls/MotoRentComponentBase.cs`

Add new method in the `#region UI Helpers` section (after `ShowInfo`):

```csharp
/// <summary>
/// Logs an error and rethrows in localhost for debugging.
/// In production, only logs the error without rethrowing.
/// </summary>
protected void LogError(Exception ex, string message, params object[] args)
{
    this.Logger.LogError(ex, message, args);

    // Rethrow in localhost to show full stack trace in ErrorPage
    if (this.NavigationManager.Uri.StartsWith("https://localhost") ||
        this.NavigationManager.Uri.StartsWith("http://localhost"))
    {
        throw;
    }
}
```

## Usage Example

Components can now use:
```csharp
catch (Exception ex)
{
    ShowError(Localizer["ErrorLoading", ex.Message]);
    LogError(ex, "Failed to load rental {RentalId}", m_rentalId);
}
```

Instead of:
```csharp
catch (Exception ex)
{
    ShowError(Localizer["ErrorLoading", ex.Message]);
    Logger.LogError(ex, "Failed to load rental {RentalId}", m_rentalId);
}
```

## Benefits
- **Centralized**: One place to control localhost rethrow behavior
- **Backward compatible**: Existing `Logger.LogError` calls continue to work
- **Opt-in**: Pages can choose to use `LogError` when they want localhost debugging
- **Consistent**: Follows existing helper method pattern (`ShowError`, `ShowWarning`, etc.)

## Code Standards Applied
- Uses `throw;` to preserve original stack trace
- Follows existing localhost detection pattern from `ErrorPage.razor`
- Uses `this.` prefix for instance members
- XML documentation comment included

## Verification
1. Build: `dotnet build src/MotoRent.Client/MotoRent.Client.csproj`
2. Run in localhost: `dotnet watch --project src/MotoRent.Server`
3. Update one page (e.g., `RentalDetails.razor`) to use `LogError` instead of `Logger.LogError`
4. Trigger an error condition and verify ErrorPage shows full stack trace
