---
name: dialog-pattern
description: MudBlazor dialog patterns using localized base classes and fluent API extensions.
---
# Dialog Pattern (MudBlazor)

Dialog patterns using MudBlazor with localized base classes.

## Dialog Base Classes

| Base Class | Usage |
|------------|-------|
| `LocalizedDialogBase<TEntity, TLocalizer>` | **Default** - Dialogs with localization |
| `MotoRentDialogBase<TEntity>` | Dialogs without localization (rare) |

### Base Class Features

`MotoRentDialogBase<TEntity>` provides:
- `MudDialog` - Cascading dialog instance
- `Entity` - The entity being edited
- `IsNew` - Whether creating new or editing existing
- `Form` - MudForm reference for validation
- `FormValid` - Form validation state
- `Saving` - Save operation in progress
- `Cancel()` - Cancel the dialog
- `Close()` / `Close(result)` - Close with result
- `ValidateFormAsync()` - Validate and return result
- `SaveButtonText` - "Add" or "Save" based on IsNew

`LocalizedDialogBase<TEntity, TLocalizer>` adds:
- `Localizer` - Component-specific localization
- `GetLocalizedText(key, defaultText)` - Get localized string

## Pattern Guidelines

| Rule | Description |
|------|-------------|
| **Dialog Pattern** | Use for **SIMPLE** create/update operations |
| **Form Editor Pattern** | Use for **COMPLEX** create/update with multiple sections |
| **Clone Objects** | Always clone before passing to dialog (preserve original on cancel) |
| **Persistence in Parent** | Save in parent component, not in dialog |
| **No Business Logic** | Keep business logic in services, not in dialogs or parents |
| **Operation Name** | Use descriptive operation names for SubmitChanges |

## Localized Dialog Template

```razor
@* MotorbikeDialog.razor *@
@inherits LocalizedDialogBase<Motorbike, MotorbikeDialog>
@using MotoRent.Domain.Entities
@using MotoRent.Services
@inject MotorbikeService MotorbikeService

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.TwoWheeler" Class="mr-2" />
            @(IsNew ? Localizer["AddTitle"] : Localizer["EditTitle"])
        </MudText>
    </TitleContent>

    <DialogContent>
        <MudForm @ref="Form" @bind-IsValid="FormValid">
            <MudGrid>
                <MudItem xs="12" sm="6">
                    <MudTextField @bind-Value="Entity.LicensePlate"
                                  Label="@Localizer["LicensePlate"]"
                                  Required="true"
                                  RequiredError="@Localizer["LicensePlateRequired"]"
                                  Variant="Variant.Outlined" />
                </MudItem>

                <MudItem xs="12" sm="6">
                    <MudSelect @bind-Value="Entity.Brand"
                               Label="@Localizer["Brand"]"
                               Required="true"
                               Variant="Variant.Outlined">
                        <MudSelectItem Value="@("Honda")">Honda</MudSelectItem>
                        <MudSelectItem Value="@("Yamaha")">Yamaha</MudSelectItem>
                        <MudSelectItem Value="@("Suzuki")">Suzuki</MudSelectItem>
                    </MudSelect>
                </MudItem>

                <MudItem xs="12" sm="6">
                    <MudTextField @bind-Value="Entity.Model"
                                  Label="@Localizer["Model"]"
                                  Required="true"
                                  Variant="Variant.Outlined" />
                </MudItem>

                <MudItem xs="12" sm="6">
                    <MudNumericField @bind-Value="Entity.DailyRate"
                                     Label="@Localizer["DailyRate"]"
                                     Format="N0" Min="0"
                                     Variant="Variant.Outlined" />
                </MudItem>
            </MudGrid>
        </MudForm>
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">@CommonLocalizer["Cancel"]</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   OnClick="SaveAsync" Disabled="@(!FormValid || Saving)">
            @if (Saving)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
            }
            @(IsNew ? CommonLocalizer["Add"] : CommonLocalizer["Save"])
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    private async Task SaveAsync()
    {
        if (!await ValidateFormAsync()) return;

        Saving = true;
        try
        {
            var result = IsNew
                ? await MotorbikeService.CreateMotorbikeAsync(Entity, UserName)
                : await MotorbikeService.UpdateMotorbikeAsync(Entity, UserName);

            if (result.Success)
            {
                Close();
            }
            else
            {
                ShowError(result.Message ?? Localizer["SaveFailed"]);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            Saving = false;
        }
    }
}
```

## Calling Dialog from Parent (Fluent API)

Use the fluent `DialogServiceExtensions` for cleaner dialog invocation:

```csharp
@using MotoRent.Client.Controls
@inherits LocalizedComponentBase<MotorbikeList>

// Add new entity
private async Task OpenAddDialog()
{
    var motorbike = new Motorbike { ShopId = ShopId };

    var confirmed = await DialogService
        .CreateDialog<MotorbikeDialog>(Localizer["AddDialogTitle"])
        .WithParameter(x => x.Entity, motorbike)
        .WithParameter(x => x.IsNew, true)
        .ShowAndConfirmAsync();

    if (confirmed)
    {
        await LoadDataAsync();
        ShowSuccess(Localizer["AddSuccess"]);
    }
}

// Edit existing entity
private async Task OpenEditDialog(Motorbike motorbike)
{
    var confirmed = await DialogService
        .CreateDialog<MotorbikeDialog>(Localizer["EditDialogTitle"])
        .WithParameter(x => x.Entity, motorbike.Clone())
        .WithParameter(x => x.IsNew, false)
        .ShowAndConfirmAsync();

    if (confirmed)
    {
        await LoadDataAsync();
        ShowSuccess(Localizer["UpdateSuccess"]);
    }
}
```

## Fluent API Reference

### Creating Dialogs

```csharp
// Standard dialog (Medium, FullWidth, CloseButton by default)
DialogService.CreateDialog<MyDialog>("Title")

// Shorthand for sizes
DialogService.CreateSmallDialog<MyDialog>("Title")
DialogService.CreateLargeDialog<MyDialog>("Title")
DialogService.CreateFullscreenDialog<MyDialog>("Title")
```

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithParameter(x => x.Prop, value)` | Set component parameter |
| `.Small()` / `.Medium()` / `.Large()` | Set dialog size |
| `.WithMaxWidth(MaxWidth.ExtraLarge)` | Custom max width |
| `.WithFullWidth(bool)` | Stretch to max width |
| `.WithCloseButton(bool)` | Show/hide close button |
| `.WithCloseOnEscapeKey(bool)` | ESC key behavior |
| `.DisableBackdropClick()` | Prevent backdrop close |
| `.WithNoHeader(bool)` | Hide dialog header |
| `.WithPosition(DialogPosition)` | Dialog position |
| `.Centered()` / `.TopCenter()` | Position shortcuts |
| `.Fullscreen(bool)` | Fullscreen mode |

### Showing Dialogs

```csharp
// Get raw DialogResult
var result = await dialog.ShowAsync();

// Get boolean (true if not canceled)
var confirmed = await dialog.ShowAndConfirmAsync();

// Get typed data from result
var data = await dialog.ShowAndGetDataAsync<MyData>();

// Non-blocking (for manual handling)
var dialogRef = await dialog.ShowNonBlockingAsync();
```

## Traditional Pattern (Alternative)

```csharp
private async Task OpenEditDialog(Motorbike? motorbike = null)
{
    var isNew = motorbike is null;
    var entity = isNew
        ? new Motorbike { ShopId = ShopId }
        : motorbike.Clone();

    var parameters = new DialogParameters<MotorbikeDialog>
    {
        { x => x.Entity, entity },
        { x => x.IsNew, isNew }
    };

    var options = new DialogOptions
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = true
    };

    var dialog = await DialogService.ShowAsync<MotorbikeDialog>(
        isNew ? Localizer["AddDialogTitle"] : Localizer["EditDialogTitle"],
        parameters,
        options);

    var result = await dialog.Result;

    if (result is { Canceled: false })
    {
        await LoadDataAsync();
        ShowSuccess(isNew ? Localizer["AddSuccess"] : Localizer["UpdateSuccess"]);
    }
}
```

## Delete Confirmation Pattern

Use the built-in `ConfirmDeleteAsync` from base class:

```csharp
private async Task DeleteMotorbike(Motorbike motorbike)
{
    // Uses MudMessageBox with localized strings
    if (!await ConfirmDeleteAsync(motorbike.LicensePlate))
        return;

    var result = await MotorbikeService.DeleteMotorbikeAsync(motorbike, UserName);
    if (result.Success)
    {
        await LoadDataAsync();
        ShowSuccess(Localizer["DeleteSuccess"]);
    }
    else
    {
        ShowError(result.Message ?? Localizer["DeleteFailed"]);
    }
}
```

## Custom Confirmation Dialog

For custom confirmation messages:

```csharp
private async Task ExtendRental(Rental rental)
{
    var confirmed = await ConfirmAsync(
        title: Localizer["ExtendRentalTitle"],
        message: Localizer["ExtendRentalMessage", rental.RenterName, rental.DaysToExtend],
        yesText: Localizer["ExtendButton"],
        noText: CommonLocalizer["Cancel"]
    );

    if (!confirmed) return;

    // Proceed with extension...
}
```

## Dialog Options Reference

```csharp
var options = new DialogOptions
{
    MaxWidth = MaxWidth.Small,      // ExtraSmall, Small, Medium, Large, ExtraLarge
    FullWidth = true,               // Stretch to MaxWidth
    CloseButton = true,             // Show X button
    CloseOnEscapeKey = true,        // ESC to close
    DisableBackdropClick = false,   // Click outside to close
    NoHeader = false,               // Hide header
    Position = DialogPosition.Center // TopLeft, TopCenter, etc.
};
```

## Resource File Structure

For each dialog, create resource files:

```
Resources/
└── Pages/
    └── MotorbikeDialog.resx           # English (default)
    └── MotorbikeDialog.th.resx        # Thai
```

Example keys:
- `AddTitle` - "Add Motorbike"
- `EditTitle` - "Edit Motorbike"
- `LicensePlate` - "License Plate"
- `LicensePlateRequired` - "License plate is required"
- `SaveFailed` - "Failed to save motorbike"

## Source
- Base classes: `src/MotoRent.Client/Controls/MotoRentDialogBase.cs`
- Fluent API: `src/MotoRent.Client/Controls/DialogFluent.cs`
- Extensions: `src/MotoRent.Client/Controls/DialogServiceExtensions.cs`
- MudBlazor Dialogs: https://mudblazor.com/components/dialog
