# MotoRent Migration Plan: MudBlazor to Tabler CSS

## Overview

**Objective**: Replace MudBlazor with Tabler CSS in the MotoRent project, adopting patterns from the rx-erp project.

**Scope**:
- Full migration (not incremental)
- 61 Razor files with 4,143 MudBlazor component occurrences
- MainLayout first, then other role-based layouts
- Copy and adapt skills from rx-erp

---

## Phase 1: Copy and Adapt Skills Documentation

### Files to Copy from rx-erp

| Source | Destination | Changes |
|--------|-------------|---------|
| `E:\project\work\rx-erp\.claude\skills\dialog-pattern\SKILL.md` | `.claude\skills\dialog-pattern\SKILL.md` | Update base class names: `LocalizedStationModalBase` -> `LocalizedDialogBase` |
| `E:\project\work\rx-erp\.claude\skills\blazor-development\SKILL.md` | `.claude\skills\blazor-development\SKILL.md` | Update: `LocalizedStationComponentBase` -> `LocalizedComponentBase`, `StationPageTitle` -> `MotoRentPageTitle` |
| `E:\project\work\rx-erp\.claude\skills\code-standards\SKILL.md` | `.claude\skills\code-standards\SKILL.md` | Keep as-is (same conventions apply) |

### Naming Convention Mapping

| rx-erp | MotoRent |
|--------|----------|
| `LocalizedStationComponentBase<T>` | `LocalizedComponentBase<T>` |
| `LocalizedStationModalBase<TEntity, TLocalizer>` | `LocalizedDialogBase<TEntity, TLocalizer>` |
| `StationComponentBase` | `MotoRentComponentBase` |
| `StationDataContext` | `RentalDataContext` |
| `StationPageTitle` | `MotoRentPageTitle` |

---

## Phase 2: Infrastructure Setup

### 2.1 Remove ALL MudBlazor References (Do First - Easier to Fix Compile Errors)

**Step 1: Remove NuGet Packages**

`src/MotoRent.Server/MotoRent.Server.csproj`:
```xml
<!-- REMOVE THIS LINE -->
<PackageReference Include="MudBlazor" Version="7.15.0" />
```

`src/MotoRent.Client/MotoRent.Client.csproj`:
```xml
<!-- REMOVE THIS LINE -->
<PackageReference Include="MudBlazor" Version="7.15.0" />
```

**Step 2: Remove from _Imports.razor**

`src/MotoRent.Server/_Imports.razor` and `src/MotoRent.Client/_Imports.razor`:
```razor
@* REMOVE THESE LINES *@
@using MudBlazor
@using MudBlazor.Services
```

**Step 3: Remove Service Registration**

`src/MotoRent.Server/Program.cs`:
```csharp
// REMOVE THIS LINE
builder.Services.AddMudServices();
```

**Step 4: Remove CSS/JS References**

`src/MotoRent.Server/Components/App.razor`:
```html
<!-- REMOVE THESE LINES -->
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

**Step 5: Remove Provider Components from Layouts**

Remove from all layouts:
```razor
@* REMOVE THESE COMPONENTS *@
<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />
<MudPopoverProvider />
```

**Step 6: Delete Theme File**

Delete: `src/MotoRent.Client/MotoRentTheme.cs`

After removing all MudBlazor references, the project will have many compilation errors. This is expected and makes it easier to identify all components that need to be converted.

### 2.2 Add Tabler CSS Dependencies

**Files to create** in `src/MotoRent.Server/wwwroot/`:
```
lib/
├── tabler-1.4.0/
│   ├── css/tabler.min.css
│   ├── css/tabler-flags.min.css
│   └── js/tabler-theme.min.js
└── tabler-icons-2.22.0/
    └── tabler-icons.min.css
css/
├── site.css          (light theme customizations)
└── site-dark.css     (dark theme overrides)
```

### 2.3 Update App.razor

**File**: `src/MotoRent.Server/Components/App.razor`

Add:
```html
<link rel="stylesheet" href="lib/tabler-1.4.0/css/tabler.min.css"/>
<link rel="stylesheet" href="lib/tabler-1.4.0/css/tabler-flags.min.css"/>
<link rel="stylesheet" href="lib/tabler-icons-2.22.0/tabler-icons.min.css"/>
<link href="css/site.css" rel="stylesheet"/>
<script src="lib/tabler-1.4.0/js/tabler-theme.min.js"></script>
```

### 2.4 Theme Switching Implementation

**Pattern**: Cookie-based with body class (`theme-dark`/`theme-light`)

**CSS Toggle (site.css)**:
```css
body.theme-light .change-theme-light { display: none; }
a.change-theme-dark { display: none; }
body.theme-light a.change-theme-dark { display: block; }
```

**ThemeToggle Component** (`src/MotoRent.Client/Components/Shared/ThemeToggle.razor`):
```razor
<a href="?theme=dark" class="nav-link px-0 change-theme-dark">
    <i class="ti ti-moon"></i>
</a>
<a href="?theme=light" class="nav-link px-0 change-theme-light">
    <i class="ti ti-sun"></i>
</a>
```

---

## Phase 3: Core Services Migration

### 3.1 New Service Files to Create

| File | Purpose |
|------|---------|
| `Services/IModalService.cs` | Modal service interface |
| `Services/ModalService.cs` | Modal service implementation |
| `Services/ModalResult.cs` | Modal result class |
| `Services/ModalOptions.cs` | Modal options (size, position, etc.) |
| `Services/DialogService.cs` | High-level dialog service with fluent API |
| `Services/DialogServiceExtensions.cs` | Fluent API extension methods |
| `Services/ToastService.cs` | Toast notification service |
| `Services/ToastMessage.cs` | Toast message model |

### 3.2 Service Registration

**File**: `src/MotoRent.Server/Program.cs`

Add:
```csharp
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();
```

---

## Phase 4: Base Classes Migration

### 4.1 MotoRentComponentBase.cs

**Changes**:
- Remove `[Inject] IDialogService` (MudBlazor)
- Remove `[Inject] ISnackbar` (MudBlazor)
- Add `[Inject] DialogService`
- Add `[Inject] ToastService`
- Update `ShowSuccess()`, `ShowError()`, etc. to use ToastService

### 4.2 LocalizedComponentBase.cs

**Changes**: Minimal - update namespace references only

### 4.3 MotoRentModalBase.cs (renamed from MotoRentDialogBase.cs)

**Changes**:
- Remove `[CascadingParameter] MudDialogInstance`
- Add `[Inject] IModalService ModalService`
- Update `Cancel()` to use `ModalService.Close(ModalResult.Cancel())`
- Update `OkClick()` to use `ModalService.Close(ModalResult.Ok(item))`
- Add `FormId` property for form binding

### 4.4 LocalizedDialogBase.cs

**Changes**: Update base class reference to `MotoRentModalBase<TEntity>`

---

## Phase 5: Container Components

### 5.1 ModalContainer.razor

**Location**: `src/MotoRent.Client/Controls/ModalContainer.razor`

Renders Bootstrap/Tabler modal HTML with dynamic content from ModalService.

### 5.2 ToastContainer.razor

**Location**: `src/MotoRent.Client/Controls/ToastContainer.razor`

Renders toast notifications using Tabler's toast pattern.

### 5.3 Dialog Components

| Component | Purpose |
|-----------|---------|
| `Controls/Dialogs/WindowDialog.razor` | Confirmation dialog with buttons |
| `Controls/Dialogs/MessageBoxDialog.razor` | Information message box |
| `Controls/Dialogs/PromptDialog.razor` | Text input prompt |

---

## Phase 6: Layout Conversion

### 6.1 MainLayout.razor

**Before (MudBlazor)**:
```razor
<MudThemeProvider />
<MudLayout>
    <MudAppBar>...</MudAppBar>
    <MudDrawer>...</MudDrawer>
    <MudMainContent>...</MudMainContent>
</MudLayout>
```

**After (Tabler)**:
```razor
<ModalContainer />
<ToastContainer />

<header class="navbar navbar-expand-md navbar-light d-print-none">
    <div class="container-xl">
        <!-- Logo, nav, theme toggle, user profile -->
    </div>
</header>

<div class="navbar-expand-md">
    <div class="collapse navbar-collapse" id="navbar-menu">
        <div class="navbar navbar-light">
            <div class="container-xl">
                <MainMenu />
            </div>
        </div>
    </div>
</div>

<div class="page-wrapper">
    <div class="page-body">
        <div class="container-xl">
            @Body
        </div>
    </div>
</div>
```

### 6.2 StaffLayout.razor (Kiosk Mode)

- Convert MudAppBar to minimal Tabler navbar
- Convert bottom navigation to custom `staff-bottom-nav` CSS
- Keep touch-friendly targets (48px minimum)

### 6.3 TouristLayout.razor

- Convert to Tabler navbar with language selector
- Add Tabler footer

### 6.4 ManagerLayout.razor

- Convert to Tabler vertical sidebar layout
- Use `navbar-vertical` pattern

---

## Phase 7: Component Mapping Reference

### Layout Components
| MudBlazor | Tabler |
|-----------|--------|
| `MudStack Row="true"` | `<div class="d-flex">` |
| `MudGrid` | `<div class="row">` |
| `MudItem xs="12" md="6"` | `<div class="col-12 col-md-6">` |
| `MudPaper` | `<div class="card">` |
| `MudSpacer` | `<div class="ms-auto">` |
| `MudDivider` | `<hr>` |

### Typography
| MudBlazor | Tabler |
|-----------|--------|
| `MudText Typo="Typo.h4"` | `<h4>` |
| `MudText Color="Primary"` | `class="text-primary"` |

### Buttons
| MudBlazor | Tabler |
|-----------|--------|
| `MudButton Variant="Filled"` | `<button class="btn btn-primary">` |
| `MudButton Variant="Outlined"` | `<button class="btn btn-outline-primary">` |
| `MudIconButton` | `<button class="btn btn-icon">` |

### Form Components
| MudBlazor | Tabler |
|-----------|--------|
| `MudTextField` | `<input class="form-control">` |
| `MudSelect` | `<select class="form-select">` |
| `MudCheckBox` | `<input type="checkbox" class="form-check-input">` |
| `MudDatePicker` | `<input type="date" class="form-control">` |

### Data Display
| MudBlazor | Tabler |
|-----------|--------|
| `MudDataGrid` | `<table class="table table-vcenter card-table">` |
| `MudChip` | `<span class="badge">` |
| `MudAlert` | `<div class="alert">` |
| `MudProgress` | `<div class="progress">` |

### Icons
| Material | Tabler |
|----------|--------|
| `Icons.Material.Filled.Add` | `ti ti-plus` |
| `Icons.Material.Filled.Edit` | `ti ti-edit` |
| `Icons.Material.Filled.Delete` | `ti ti-trash` |
| `Icons.Material.Filled.Search` | `ti ti-search` |
| `Icons.Material.Filled.Home` | `ti ti-home` |
| `Icons.Material.Filled.Menu` | `ti ti-menu-2` |

---

## Phase 8: Shared Components to Create

| Component | Location | Purpose |
|-----------|----------|---------|
| `ThemeToggle.razor` | `Components/Shared/` | Dark/light mode toggle |
| `MotoRentPageTitle.razor` | `Components/Shared/` | Page title component |
| `TablerHeader.razor` | `Components/Shared/` | Page header with title and actions |
| `LoadingSkeleton.razor` | `Components/Shared/` | Loading placeholder |
| `StatusBadge.razor` | `Components/Shared/` | Colored status indicator |

---

## Phase 9: Page Migration Order

### Priority 1: Core Pages
1. `MainLayout.razor`
2. `NavMenu.razor`
3. `Motorbikes.razor` (reference for data grid pattern)
4. `MotorbikeDialog.razor` (reference for dialog pattern)

### Priority 2: Role Layouts
5. `StaffLayout.razor`
6. `ManagerLayout.razor`
7. `TouristLayout.razor`

### Priority 3: All Remaining Pages
8. Renters list and dialog
9. Staff pages
10. Tourist pages
11. Manager pages
12. Rentals workflow

---

## Phase 10: CSS Customization

### site.css (MotoRent brand colors)
```css
:root {
    --tblr-primary: #00897B;
    --tblr-primary-darken: #00695C;
    --tblr-primary-lighten: #4DB6AC;
    --tblr-secondary: #FF7043;
}
```

### site-dark.css (Dark theme overrides)
```css
a.change-theme-dark { display: none; }
body.theme-light a.change-theme-dark { display: block; }
a.change-theme-light { display: block; }
body.theme-light a.change-theme-light { display: none; }
```

---

## Verification

### Build Verification
```bash
cd E:\project\work\motorent.tabler
dotnet build
```

### Runtime Verification
```bash
dotnet watch --project src/MotoRent.Server
```

**Note**: Use `dotnet watch` for live updates. Only restart when there are compilation changes (new files, dependency changes, etc.).

### Manual Testing Checklist
- [ ] Theme toggle (dark/light) works
- [ ] Main navigation renders correctly
- [ ] Motorbike list page loads with data
- [ ] Create motorbike dialog opens and saves
- [ ] Toast notifications appear
- [ ] Staff layout renders with bottom nav
- [ ] Manager layout renders with sidebar
- [ ] All forms validate correctly

---

## Critical Files Summary

### To Create
- `src/MotoRent.Client/Services/ModalService.cs`
- `src/MotoRent.Client/Services/DialogService.cs`
- `src/MotoRent.Client/Services/ToastService.cs`
- `src/MotoRent.Client/Controls/ModalContainer.razor`
- `src/MotoRent.Client/Controls/ToastContainer.razor`
- `src/MotoRent.Client/Components/Shared/ThemeToggle.razor`
- `src/MotoRent.Client/Components/Shared/TablerHeader.razor`
- `src/MotoRent.Client/Components/Shared/LoadingSkeleton.razor`
- `src/MotoRent.Server/wwwroot/css/site.css`
- `src/MotoRent.Server/wwwroot/css/site-dark.css`

### To Modify
- `src/MotoRent.Server/MotoRent.Server.csproj` (remove MudBlazor)
- `src/MotoRent.Client/MotoRent.Client.csproj` (remove MudBlazor)
- `src/MotoRent.Server/Program.cs` (service registration)
- `src/MotoRent.Server/Components/App.razor` (CSS/JS references)
- `src/MotoRent.Client/Controls/MotoRentComponentBase.cs`
- `src/MotoRent.Client/Controls/MotoRentDialogBase.cs` -> `MotoRentModalBase.cs`
- `src/MotoRent.Client/Layout/MainLayout.razor`
- `src/MotoRent.Client/Layout/NavMenu.razor`
- All 61 Razor page files (component replacements)

### Skills to Copy
- `.claude/skills/dialog-pattern/SKILL.md`
- `.claude/skills/blazor-development/SKILL.md`
- `.claude/skills/code-standards/SKILL.md`
