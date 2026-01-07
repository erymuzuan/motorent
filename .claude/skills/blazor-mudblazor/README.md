# Blazor MudBlazor UI

MudBlazor component patterns and theming for MotoRent.

## Setup

### Package Reference

```xml
<PackageReference Include="MudBlazor" Version="7.15.0" />
```

### Program.cs

```csharp
using MudBlazor.Services;

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
});
```

### App.razor (Head)

```html
<link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
```

### App.razor (Body)

```html
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### _Imports.razor

```razor
@using MudBlazor
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
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#4DB6AC",        // Teal 300
            Secondary = "#FFAB91",
            AppbarBackground = "#1E1E1E",
            Background = "#121212",
            Surface = "#1E1E1E"
        }
    };
}
```

### MainLayout.razor

```razor
@inherits LayoutComponentBase

<MudThemeProvider Theme="MotoRentTheme.Theme" @bind-IsDarkMode="@m_isDarkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit"
                       OnClick="@(_ => m_drawerOpen = !m_drawerOpen)" />
        <MudIcon Icon="@Icons.Material.Filled.TwoWheeler" Class="mr-2" />
        <MudText Typo="Typo.h6">MotoRent</MudText>
        <MudSpacer />
        <MudIconButton Icon="@(m_isDarkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode)"
                       Color="Color.Inherit" OnClick="@(_ => m_isDarkMode = !m_isDarkMode)" />
    </MudAppBar>

    <MudDrawer @bind-Open="m_drawerOpen" ClipMode="DrawerClipMode.Always" Elevation="2">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-4 pt-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool m_drawerOpen = true;
    private bool m_isDarkMode;
}
```

## Common Components

### Data Table

```razor
<MudTable Items="@m_motorbikes" Hover="true" Dense="true" Loading="@m_loading">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Motorbikes</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="m_searchString" Placeholder="Search..."
                      Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Search"
                      Immediate="true" DebounceInterval="300" />
        <MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add"
                   OnClick="AddNew" Class="ml-4">Add</MudButton>
    </ToolBarContent>
    <HeaderContent>
        <MudTh>License Plate</MudTh>
        <MudTh>Brand</MudTh>
        <MudTh>Model</MudTh>
        <MudTh>Status</MudTh>
        <MudTh>Daily Rate</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.LicensePlate</MudTd>
        <MudTd>@context.Brand</MudTd>
        <MudTd>@context.Model</MudTd>
        <MudTd>
            <MudChip T="string" Size="Size.Small"
                     Color="@(context.Status == "Available" ? Color.Success :
                              context.Status == "Rented" ? Color.Warning : Color.Default)">
                @context.Status
            </MudChip>
        </MudTd>
        <MudTd>@context.DailyRate.ToString("N0") THB</MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                           OnClick="@(() => Edit(context))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                           OnClick="@(() => Delete(context))" />
        </MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 25, 50 }" />
    </PagerContent>
</MudTable>
```

### Form with Validation

```razor
<MudForm @ref="m_form" @bind-IsValid="m_isValid">
    <MudGrid>
        <MudItem xs="12" sm="6">
            <MudTextField @bind-Value="m_item.LicensePlate"
                          Label="License Plate"
                          Required="true"
                          RequiredError="License plate is required" />
        </MudItem>
        <MudItem xs="12" sm="6">
            <MudSelect @bind-Value="m_item.Brand" Label="Brand" Required="true">
                <MudSelectItem Value="@("Honda")">Honda</MudSelectItem>
                <MudSelectItem Value="@("Yamaha")">Yamaha</MudSelectItem>
                <MudSelectItem Value="@("Suzuki")">Suzuki</MudSelectItem>
            </MudSelect>
        </MudItem>
        <MudItem xs="12" sm="6">
            <MudNumericField @bind-Value="m_item.DailyRate" Label="Daily Rate (THB)"
                             Format="N0" Min="0" />
        </MudItem>
        <MudItem xs="12" sm="6">
            <MudDatePicker @bind-Date="m_item.LastServiceDate" Label="Last Service Date" />
        </MudItem>
    </MudGrid>
</MudForm>
```

### Dashboard Cards

```razor
<MudGrid>
    <MudItem xs="12" sm="6" md="3">
        <MudPaper Elevation="2" Class="pa-4">
            <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
                <div>
                    <MudText Typo="Typo.subtitle2">Active Rentals</MudText>
                    <MudText Typo="Typo.h4">@m_activeRentals</MudText>
                </div>
                <MudIcon Icon="@Icons.Material.Filled.Receipt" Color="Color.Primary" Size="Size.Large" />
            </MudStack>
        </MudPaper>
    </MudItem>
</MudGrid>
```

### Navigation Menu

```razor
<MudNavMenu>
    <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.Dashboard">
        Dashboard
    </MudNavLink>

    <MudNavGroup Title="Rentals" Icon="@Icons.Material.Filled.Receipt" Expanded="true">
        <MudNavLink Href="/rentals" Icon="@Icons.Material.Filled.List">Active Rentals</MudNavLink>
        <MudNavLink Href="/rentals/checkin" Icon="@Icons.Material.Filled.Login">Check-In</MudNavLink>
        <MudNavLink Href="/rentals/checkout" Icon="@Icons.Material.Filled.Logout">Check-Out</MudNavLink>
    </MudNavGroup>

    <MudNavGroup Title="Inventory" Icon="@Icons.Material.Filled.TwoWheeler">
        <MudNavLink Href="/motorbikes" Icon="@Icons.Material.Filled.DirectionsBike">Motorbikes</MudNavLink>
    </MudNavGroup>
</MudNavMenu>
```

### Snackbar Notifications

```csharp
@inject ISnackbar Snackbar

// Success
Snackbar.Add("Rental saved successfully", Severity.Success);

// Error
Snackbar.Add("Failed to save rental", Severity.Error);

// Warning
Snackbar.Add("This bike needs maintenance", Severity.Warning);

// Info
Snackbar.Add("New booking received", Severity.Info);
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
