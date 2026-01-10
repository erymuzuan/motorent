---
name: blazor-mudblazor
description: MudBlazor component patterns, theming, and base classes for MotoRent UI development.
---
# Blazor MudBlazor UI

MudBlazor component patterns and theming for MotoRent.

## Component Base Classes (IMPORTANT)

All components should inherit from the appropriate base class:

| Base Class | Usage |
|------------|-------|
| `LocalizedComponentBase<T>` | **Default** - Pages and components with localization |
| `MotoRentComponentBase` | Components without localization (rare) |
| `LocalizedDialogBase<TEntity, TLocalizer>` | Dialogs with localization |
| `MotoRentDialogBase<TEntity>` | Dialogs without localization (rare) |

### Base Class Services

`MotoRentComponentBase` provides:
- `DataContext` - RentalDataContext for data operations
- `RequestContext` - User context, timezone, formatting
- `DialogService` - MudBlazor dialogs
- `Snackbar` - Toast notifications
- `NavigationManager` - URL navigation
- `Logger` - Logging
- `CommonLocalizer` - Shared localization strings

`LocalizedComponentBase<T>` adds:
- `Localizer` - Component-specific localization

### Helper Properties & Methods

```csharp
// Request context properties
protected int ShopId;           // Current shop
protected string UserName;      // Current user or "system"
protected string? AccountNo;    // Tenant identifier
protected DateOnly Today;       // Today in user's timezone

// Formatting
protected string FormatDateTime(DateTimeOffset? dto);
protected string FormatDate(DateTimeOffset? dto);
protected string FormatTime(DateTimeOffset? dto);
protected string FormatCurrency(decimal amount);  // "1,500 THB"

// Notifications
protected void ShowSuccess(string message);
protected void ShowError(string message);
protected void ShowWarning(string message);
protected void ShowInfo(string message);

// Confirmation dialogs
protected Task<bool> ConfirmAsync(string title, string message, ...);
protected Task<bool> ConfirmDeleteAsync(string itemName);
```

## Page Template (Localized)

```razor
@page "/motorbikes"
@inherits LocalizedComponentBase<Motorbikes>
@using MotoRent.Domain.Entities
@using MotoRent.Services
@inject MotorbikeService MotorbikeService

<PageTitle>@Localizer["PageTitle"]</PageTitle>

<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
    <MudText Typo="Typo.h4">@Localizer["Header"]</MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add" OnClick="OpenAddDialog">
        @CommonLocalizer["AddButton"]
    </MudButton>
</MudStack>

@if (m_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-4" />
}

<MudPaper Elevation="2">
    <MudDataGrid T="Motorbike" Items="@m_motorbikes" Hover="true" Striped="true"
                 Loading="@m_loading" Dense="true">
        <Columns>
            <PropertyColumn Property="x => x.LicensePlate" Title="@Localizer["LicensePlate"]" />
            <PropertyColumn Property="x => x.Brand" Title="@Localizer["Brand"]" />
            <TemplateColumn Title="@Localizer["DailyRate"]">
                <CellTemplate>
                    <MudText Color="Color.Primary">@FormatCurrency(context.Item.DailyRate)</MudText>
                </CellTemplate>
            </TemplateColumn>
        </Columns>
    </MudDataGrid>
</MudPaper>

@code {
    private List<Motorbike> m_motorbikes = [];
    private bool m_loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (m_loading) return; // Prevent double loading
        m_loading = true;
        try
        {
            var result = await MotorbikeService.GetMotorbikesAsync(ShopId);
            m_motorbikes = result.ItemCollection;
        }
        catch (Exception ex)
        {
            ShowError($"Error loading data: {ex.Message}");
        }
        finally
        {
            m_loading = false;
        }
    }

    private async Task ConfirmDelete(Motorbike motorbike)
    {
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
}
```

## Loading Pattern

```csharp
private bool m_loading;

private async Task LoadDataAsync()
{
    if (m_loading) return; // Prevent double loading
    m_loading = true;
    try
    {
        // Load data...
    }
    catch (Exception ex)
    {
        ShowError($"Error: {ex.Message}");
    }
    finally
    {
        m_loading = false;
    }
}
```

## Theme (Tropical Teal)

```csharp
// MotoRentTheme.cs
public static class MotoRentTheme
{
    public static MudTheme Theme { get; } = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#00897B",        // Teal 600
            PrimaryDarken = "#00695C",  // Teal 800
            PrimaryLighten = "#4DB6AC", // Teal 300
            Secondary = "#FF7043",      // Deep Orange accent
            AppbarBackground = "#00897B",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Info = "#2196F3"
        }
    };
}
```

## Common Components

### Data Grid with Localization

```razor
<MudDataGrid T="Motorbike" Items="@m_motorbikes" Hover="true" Striped="true"
             Loading="@m_loading" Dense="true" Virtualize="true">
    <Columns>
        <PropertyColumn Property="x => x.LicensePlate" Title="@Localizer["LicensePlate"]" />
        <PropertyColumn Property="x => x.Brand" Title="@Localizer["Brand"]" />
        <TemplateColumn Title="@Localizer["Status"]">
            <CellTemplate>
                <MudChip T="string" Size="Size.Small"
                         Color="@GetStatusColor(context.Item.Status)">
                    @Localizer[context.Item.Status ?? "Unknown"]
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="@CommonLocalizer["Actions"]" Sortable="false">
            <CellTemplate>
                <MudStack Row="true" Spacing="1">
                    <MudTooltip Text="@CommonLocalizer["Edit"]">
                        <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                                       Color="Color.Primary" OnClick="@(() => Edit(context.Item))" />
                    </MudTooltip>
                    <MudTooltip Text="@CommonLocalizer["Delete"]">
                        <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small"
                                       Color="Color.Error" OnClick="@(() => Delete(context.Item))" />
                    </MudTooltip>
                </MudStack>
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="Motorbike" PageSizeOptions="new[] { 10, 20, 50, 100 }" />
    </PagerContent>
</MudDataGrid>
```

### Form with Validation

```razor
<MudForm @ref="m_form" @bind-IsValid="m_formValid">
    <MudGrid>
        <MudItem xs="12" sm="6">
            <MudTextField @bind-Value="m_item.LicensePlate"
                          Label="@Localizer["LicensePlate"]"
                          Required="true"
                          RequiredError="@Localizer["LicensePlateRequired"]" />
        </MudItem>
        <MudItem xs="12" sm="6">
            <MudSelect @bind-Value="m_item.Brand" Label="@Localizer["Brand"]" Required="true">
                <MudSelectItem Value="@("Honda")">Honda</MudSelectItem>
                <MudSelectItem Value="@("Yamaha")">Yamaha</MudSelectItem>
            </MudSelect>
        </MudItem>
        <MudItem xs="12" sm="6">
            <MudNumericField @bind-Value="m_item.DailyRate"
                             Label="@Localizer["DailyRate"]"
                             Format="N0" Min="0" />
        </MudItem>
    </MudGrid>
</MudForm>
```

## Status Color Helper

```csharp
private static Color GetStatusColor(string? status) => status switch
{
    "Available" => Color.Success,
    "Rented" => Color.Primary,
    "Maintenance" => Color.Warning,
    _ => Color.Default
};
```

## Component Reference

| Component | Usage |
|-----------|-------|
| MudDataGrid | Complex data tables with sorting/filtering |
| MudTable | Simple data tables |
| MudForm | Form validation |
| MudDialog | Modal dialogs |
| MudStepper | Multi-step wizards |
| MudDatePicker | Date selection |
| MudTimePicker | Time selection |
| MudFileUpload | File/image uploads |
| MudChip | Status badges |
| MudCard | Content cards |
| MudSnackbar | Toast notifications |

## Source
- MudBlazor Docs: https://mudblazor.com
- Base classes: `src/MotoRent.Client/Controls/`
