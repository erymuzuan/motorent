# Localization (Multi-Language Support)

Multi-language support patterns for MotoRent (English/Thai).

## Naming Conventions for Keys

### Use Short, Descriptive Keys
```csharp
// BAD - Long sentences as keys
Localizer["Would you like to confirm this rental for {0}?"]

// GOOD - PascalCase variable-like names
Localizer["RentalConfirmationMessage", renterName]
```

### Context Prefixes
```csharp
// Different contexts for similar actions
Localizer["CheckInSuccessMessage"]      // "Rental checked in successfully"
Localizer["CheckOutSuccessMessage"]     // "Rental checked out successfully"
Localizer["DepositCollectedMessage"]    // "Deposit collected successfully"
```

## Resource File Structure

```
Resources/
├── Pages/
│   ├── Shop/
│   │   ├── Dashboard.resx           # English (default)
│   │   ├── Dashboard.th.resx        # Thai
│   │   ├── CheckIn.resx
│   │   ├── CheckIn.th.resx
│   │   └── ...
│   ├── Tourist/
│   │   ├── Browse.resx
│   │   ├── Browse.th.resx
│   │   └── ...
│   └── Shared/
│       ├── Common.resx              # Shared strings
│       └── Common.th.resx
```

## Supported Cultures

| Culture | File Suffix | Example |
|---------|-------------|---------|
| English (default) | `.resx` | `CheckIn.resx` |
| Thai | `.th.resx` | `CheckIn.th.resx` |
| Chinese (future) | `.zh.resx` | `CheckIn.zh.resx` |

## Thai Language Reference

| English | Thai | Notes |
|---------|------|-------|
| Rental | การเช่า | |
| Motorbike | มอเตอร์ไซค์ | or รถจักรยานยนต์ (formal) |
| Deposit | เงินมัดจำ | |
| Passport | หนังสือเดินทาง | |
| Driving License | ใบขับขี่ | |
| Check-in | เช็คอิน | transliteration common |
| Check-out | เช็คเอาท์ | transliteration common |
| Available | ว่าง | |
| Rented | ถูกเช่า | |
| Damage | ความเสียหาย | |
| Insurance | ประกันภัย | |
| Daily Rate | ราคาต่อวัน | |
| Save | บันทึก | |
| Cancel | ยกเลิก | |
| Delete | ลบ | |
| Edit | แก้ไข | |
| Search | ค้นหา | |

## Service Registration

```csharp
// Program.cs
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "th" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

// In app pipeline
app.UseRequestLocalization();
```

## Blazor Component Usage

```razor
@page "/shop/checkin"
@inject IStringLocalizer<CheckIn> Localizer
@inject IStringLocalizer<Common> CommonLocalizer

<PageTitle>@Localizer["PageTitle"]</PageTitle>

<MudText Typo="Typo.h4">@Localizer["CheckInHeader"]</MudText>

<MudButton Color="Color.Primary" OnClick="Submit">
    @CommonLocalizer["SaveButton"]
</MudButton>

@* With parameters *@
<MudText>@Localizer["RentalConfirmMessage", rental.RenterName, rental.MotorbikeName]</MudText>

@* Validation messages *@
<MudTextField @bind-Value="renter.FullName"
              Label="@Localizer["FullNameLabel"]"
              Required="true"
              RequiredError="@Localizer["FullNameRequired"]" />
```

## Resource File Examples

### Common.resx (English)
```xml
<data name="SaveButton"><value>Save</value></data>
<data name="CancelButton"><value>Cancel</value></data>
<data name="DeleteButton"><value>Delete</value></data>
<data name="EditButton"><value>Edit</value></data>
<data name="SearchPlaceholder"><value>Search...</value></data>
<data name="LoadingMessage"><value>Loading...</value></data>
<data name="NoDataMessage"><value>No data available</value></data>
<data name="ConfirmDeleteMessage"><value>Are you sure you want to delete this item?</value></data>
<data name="SuccessMessage"><value>Operation completed successfully</value></data>
<data name="ErrorMessage"><value>An error occurred. Please try again.</value></data>
```

### Common.th.resx (Thai)
```xml
<data name="SaveButton"><value>บันทึก</value></data>
<data name="CancelButton"><value>ยกเลิก</value></data>
<data name="DeleteButton"><value>ลบ</value></data>
<data name="EditButton"><value>แก้ไข</value></data>
<data name="SearchPlaceholder"><value>ค้นหา...</value></data>
<data name="LoadingMessage"><value>กำลังโหลด...</value></data>
<data name="NoDataMessage"><value>ไม่มีข้อมูล</value></data>
<data name="ConfirmDeleteMessage"><value>คุณแน่ใจหรือไม่ว่าต้องการลบรายการนี้?</value></data>
<data name="SuccessMessage"><value>ดำเนินการสำเร็จ</value></data>
<data name="ErrorMessage"><value>เกิดข้อผิดพลาด กรุณาลองใหม่อีกครั้ง</value></data>
```

## Culture Switching Component

```razor
@inject NavigationManager Navigation

<MudSelect T="string" Value="@currentCulture" ValueChanged="OnCultureChanged"
           Label="Language" Variant="Variant.Outlined" Style="width: 120px;">
    <MudSelectItem Value="@("en")">English</MudSelectItem>
    <MudSelectItem Value="@("th")">ไทย</MudSelectItem>
</MudSelect>

@code {
    private string currentCulture = CultureInfo.CurrentCulture.Name;

    private void OnCultureChanged(string culture)
    {
        var uri = new Uri(Navigation.Uri)
            .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
        var cultureEscaped = Uri.EscapeDataString(culture);
        var uriEscaped = Uri.EscapeDataString(uri);

        Navigation.NavigateTo(
            $"Culture/Set?culture={cultureEscaped}&redirectUri={uriEscaped}",
            forceLoad: true);
    }
}
```

## What to Localize

### DO Localize
- UI labels and headers
- Button text
- Validation messages
- Success/error messages
- Tooltips and help text
- Email/SMS templates

### DO NOT Localize
- Code logic strings: `if (status == "Active")`
- Database values (status enum values)
- API responses (unless user-facing)
- Log messages (keep in English for debugging)
- Configuration keys

## Dynamic Branding

```xml
<!-- Never hardcode app names -->
<!-- BAD -->
<data name="WelcomeMessage">
    <value>Welcome to MotoRent!</value>
</data>

<!-- GOOD - Use placeholder -->
<data name="WelcomeMessage">
    <value>Welcome to {0}!</value>
</data>
```

```csharp
@inject IConfiguration Configuration
@inject IStringLocalizer<CheckIn> Localizer

<MudText>@Localizer["WelcomeMessage", Configuration["App:Name"] ?? "MotoRent"]</MudText>
```

## Source
- From: `D:\project\work\rx-erp` localization patterns
