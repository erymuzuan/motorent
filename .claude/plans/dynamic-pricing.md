# Dynamic Pricing for Tourism Seasonality

## Overview

Rule-based dynamic pricing system for Thailand's tourism seasonality. Allows shop owners to configure seasonal rates, event-based multipliers, and demand-driven adjustments.

## Regional Variation (Key Design Principle)

**Thailand has no single "high season"** - each region has different tourism patterns based on:
- Primary tourist demographics (European, Russian, Chinese, Malaysian, domestic)
- Local weather patterns (Andaman vs Gulf coast monsoons differ)
- Regional events and festivals

### Example Regional Differences

| Region | Primary Tourists | Peak Period | Notes |
|--------|-----------------|-------------|-------|
| **Phuket/Krabi** | European, Russian | Nov-Mar | European winter escape |
| **Hat Yai/Songkhla** | Malaysian | Year-round weekends | Border proximity, no monsoon effect |
| **Koh Samui** | Mixed | Jan-Apr, Jul-Sep | Gulf coast has opposite monsoon |
| **Chiang Mai** | Chinese, Western | Nov-Feb | Cool season + festivals |
| **Pattaya** | Russian, Chinese | Nov-Feb, Oct Golden Week | Multiple demographics |

### Regional Event Examples

| Event | Dates | Relevant Regions |
|-------|-------|------------------|
| Chinese New Year | Jan/Feb (varies) | Phuket, Bangkok, Chiang Mai |
| Songkran | Apr 13-15 | Nationwide |
| Full Moon Party | Monthly | Koh Phangan only |
| Vegetarian Festival | Sep/Oct | Phuket primarily |
| Malaysian School Holidays | Mar, Jun, Nov-Dec | Hat Yai, Songkhla, Betong |
| Russian New Year | Jan 1-14 | Phuket, Pattaya |

**Design implication**: No auto-seeded defaults. Each shop defines their own seasons based on their location and customer base.

## Feature Toggle via ISettingConfig

Dynamic pricing is **opt-in per shop** using the existing `ISettingConfig` system.

### Setting Keys

```csharp
public static class SettingKeys
{
    // Dynamic Pricing
    public const string DynamicPricingEnabled = "DynamicPricing.Enabled";           // bool, default false
    public const string DynamicPricingShowOnInvoice = "DynamicPricing.ShowOnInvoice"; // bool, show multiplier on invoice
    public const string DynamicPricingAppliedPreset = "DynamicPricing.AppliedPreset"; // string, which preset was applied
}
```

### Usage in DynamicPricingService

```csharp
public class DynamicPricingService
{
    private readonly ISettingConfig m_settings;
    private readonly RentalDataContext m_context;

    public DynamicPricingService(ISettingConfig settings, RentalDataContext context)
    {
        m_settings = settings;
        m_context = context;
    }

    public async Task<PricingCalculation> CalculateAdjustedRateAsync(
        int shopId,
        decimal baseRate,
        DateOnly rentalDate,
        VehicleType vehicleType,
        int? vehicleId = null)
    {
        // Check if dynamic pricing is enabled for this shop
        var isEnabled = await m_settings.GetBoolAsync(SettingKeys.DynamicPricingEnabled);

        if (!isEnabled)
        {
            // Return unchanged rate
            return new PricingCalculation
            {
                BaseRate = baseRate,
                AdjustedRate = baseRate,
                Multiplier = 1.0m,
                AppliedRuleName = null,
                AppliedRuleType = null
            };
        }

        // Load and apply pricing rules...
        var rules = await LoadActiveRulesAsync(shopId, rentalDate, vehicleType, vehicleId);
        // ... rest of calculation
    }

    public async Task<bool> IsEnabledAsync()
        => await m_settings.GetBoolAsync(SettingKeys.DynamicPricingEnabled);

    public async Task SetEnabledAsync(bool enabled)
        => await m_settings.SetValueAsync(SettingKeys.DynamicPricingEnabled, enabled);
}
```

### UI: Enable Dynamic Pricing Toggle

In Organization Settings page (`/settings/organization`):

```razor
<div class="card mb-3">
    <div class="card-header">
        <h3 class="card-title">
            <i class="ti ti-chart-line me-2"></i>
            @Localizer["DynamicPricing"]
        </h3>
    </div>
    <div class="card-body">
        <label class="form-check form-switch">
            <input class="form-check-input" type="checkbox"
                   @bind="m_dynamicPricingEnabled"
                   @bind:after="OnDynamicPricingToggled" />
            <span class="form-check-label">@Localizer["EnableDynamicPricing"]</span>
        </label>

        @if (m_dynamicPricingEnabled)
        {
            <div class="alert alert-info mt-3">
                <div class="d-flex">
                    <div>
                        <i class="ti ti-info-circle me-2"></i>
                    </div>
                    <div>
                        @Localizer["DynamicPricingEnabledInfo"]
                    </div>
                </div>
            </div>

            <a href="/settings/pricing-rules" class="btn btn-outline-primary mt-2">
                <i class="ti ti-settings me-1"></i>
                @Localizer["ManagePricingRules"]
            </a>
        }
    </div>
</div>
```

### First-Time Setup Flow

When enabling dynamic pricing for the first time:

```
1. User toggles "Enable Dynamic Pricing" ON
2. System checks if any pricing rules exist
3. If no rules exist, show "Apply Regional Preset" dialog
4. User selects preset (or starts blank)
5. Rules created, dynamic pricing active
```

---

## Current Pricing (No Changes Needed)

The existing `RentalPricingService` calculates:
- Vehicle.DailyRate x days
- Insurance.DailyRate x days
- Accessory.DailyRate x days
- Location fees (pickup/dropoff/out-of-hours)

**Integration point**: Modify `RentalPricingService.CalculatePricing()` to apply multiplier before calculation.

## New Entities

### 1. PricingRule

Defines a pricing adjustment rule with date range and multiplier.

```csharp
public class PricingRule : Entity
{
    public int PricingRuleId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; }              // "High Season 2025"
    public string? Description { get; set; }
    public PricingRuleType RuleType { get; set; } // Season, Event, Custom

    // Date range (required)
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Yearly recurrence (optional)
    public bool IsRecurring { get; set; }         // Repeats annually
    public int? RecurringMonth { get; set; }      // For yearly events (1-12)
    public int? RecurringDay { get; set; }        // Day of month
    public int? RecurringDuration { get; set; }   // Days duration

    // Pricing adjustment
    public decimal Multiplier { get; set; }       // 1.5 = +50%, 0.8 = -20%
    public decimal? MinRate { get; set; }         // Floor price (optional)
    public decimal? MaxRate { get; set; }         // Ceiling price (optional)

    // Applicability
    public List<VehicleType> ApplicableVehicleTypes { get; set; } = [];
    public List<int> ApplicableVehicleIds { get; set; } = [];  // Empty = all

    // Priority (higher wins on overlap)
    public int Priority { get; set; }             // Events > Seasons

    public bool IsActive { get; set; }

    // Denormalized
    public string ShopName { get; set; }
}
```

### 2. PricingRuleType Enum

```csharp
public enum PricingRuleType
{
    Season,      // Seasonal pricing (Low/High)
    Event,       // Special events (Songkran, CNY)
    DayOfWeek,   // Weekend premium (Hat Yai Malaysian weekenders)
    Custom       // Shop-specific promotions
}
```

### 3. Day-of-Week Support (For Weekend Premium)

Add to `PricingRule` entity:

```csharp
// Day-of-week applicability (for DayOfWeek rule type)
public List<DayOfWeek> ApplicableDays { get; set; } = [];  // e.g., [Friday, Saturday, Sunday]
```

This enables Hat Yai shops to set weekend premiums for Malaysian tourists who visit year-round on weekends, regardless of season.

### 4. PricingCalculation (Value Object)

Result of applying pricing rules:

```csharp
public record PricingCalculation
{
    public decimal BaseRate { get; init; }
    public decimal AdjustedRate { get; init; }
    public decimal Multiplier { get; init; }
    public string? AppliedRuleName { get; init; }
    public PricingRuleType? AppliedRuleType { get; init; }
    public bool HasAdjustment => Multiplier != 1.0m;
}
```

## Database Schema

### PricingRule Table

```sql
CREATE TABLE [<schema>].[PricingRule]
(
    [PricingRuleId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [RuleType] AS CAST(JSON_VALUE([Json], '$.RuleType') AS NVARCHAR(20)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [EndDate] AS CAST(JSON_VALUE([Json], '$.EndDate') AS DATE),
    [Multiplier] AS CAST(JSON_VALUE([Json], '$.Multiplier') AS DECIMAL(5,2)),
    [Priority] AS CAST(JSON_VALUE([Json], '$.Priority') AS INT),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [IsRecurring] AS CAST(JSON_VALUE([Json], '$.IsRecurring') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE INDEX IDX_<schema>_PricingRule_Date
    ON [<schema>].[PricingRule]([ShopId], [StartDate], [EndDate], [IsActive]);
```

## Files to Create

### Domain Layer

| File | Purpose |
|------|---------|
| `src/MotoRent.Domain/Entities/PricingRule.cs` | Pricing rule entity |
| `src/MotoRent.Domain/Entities/PricingRuleType.cs` | Rule type enum |
| `src/MotoRent.Domain/Models/PricingCalculation.cs` | Calculation result |

### Database

| File | Purpose |
|------|---------|
| `database/tables/MotoRent.PricingRule.sql` | PricingRule table |

### Services

| File | Purpose |
|------|---------|
| `src/MotoRent.Services/DynamicPricingService.cs` | Rule matching + calculation |
| `src/MotoRent.Services/RegionalPresetService.cs` | Regional template presets |

### UI Pages

| File | Purpose |
|------|---------|
| `src/MotoRent.Client/Pages/Settings/PricingRules.razor` | List/manage rules |
| `src/MotoRent.Client/Pages/Settings/PricingRules.razor.cs` | Code-behind |
| `src/MotoRent.Client/Pages/Settings/PricingRuleDialog.razor` | Create/edit rule |
| `src/MotoRent.Client/Pages/Settings/PricingRuleDialog.razor.cs` | Dialog code-behind |
| `src/MotoRent.Client/Pages/Settings/PricingCalendar.razor` | Visual calendar view |
| `src/MotoRent.Client/Pages/Settings/ApplyPresetDialog.razor` | Regional preset selector |

### Components

| File | Purpose |
|------|---------|
| `src/MotoRent.Client/Components/Shared/PricingBadge.razor` | Shows multiplier badge |
| `src/MotoRent.Client/Components/Shared/SeasonIndicator.razor` | Current season indicator |

## Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add `JsonDerivedType` for `PricingRule` |
| `src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs` | Register `IRepository<PricingRule>` |
| `src/MotoRent.Domain/Settings/SettingKeys.cs` | Add dynamic pricing setting keys |
| `src/MotoRent.Services/RentalPricingService.cs` | Inject `DynamicPricingService`, apply multiplier when enabled |
| `src/MotoRent.Server/Program.cs` | Register `DynamicPricingService`, `RegionalPresetService` |
| `src/MotoRent.Client/Pages/Settings/OrganizationSettings.razor` | Add dynamic pricing toggle section |
| `src/MotoRent.Client/Pages/Rentals/ConfigureRentalStep.razor` | Show applied rule info when enabled |
| `src/MotoRent.Client/Shared/NavMenu.razor` | Add "Pricing Rules" under Settings (visible when enabled) |

## Service Implementation

### DynamicPricingService

```csharp
public class DynamicPricingService
{
    public async Task<PricingCalculation> CalculateAdjustedRateAsync(
        int shopId,
        decimal baseRate,
        DateOnly rentalDate,
        VehicleType vehicleType,
        int? vehicleId = null)
    {
        // 1. Load active rules for shop
        // 2. Filter by date range (handle recurring)
        // 3. Filter by vehicle type/id
        // 4. Select highest priority rule
        // 5. Apply multiplier with min/max bounds
        // 6. Return calculation result
    }

    public async Task<List<PricingRule>> GetActiveRulesForDateRangeAsync(
        int shopId, DateOnly start, DateOnly end);

    public async Task<Dictionary<DateOnly, decimal>> GetMultiplierCalendarAsync(
        int shopId, int year, int month, VehicleType vehicleType);
}
```

### RentalPricingService Integration

Modify `CalculatePricing()`:

```csharp
public async Task<RentalPricing> CalculatePricingAsync(
    Vehicle vehicle,
    RentalDurationType durationType,
    DateTimeOffset startDate,
    DateTimeOffset endDate,
    /* ... existing params ... */)
{
    var baseRate = vehicle.DailyRate;

    // NEW: Apply dynamic pricing
    var pricingCalc = await m_dynamicPricingService.CalculateAdjustedRateAsync(
        vehicle.ShopId,
        baseRate,
        DateOnly.FromDateTime(startDate.DateTime),
        vehicle.VehicleType,
        vehicle.VehicleId);

    var effectiveRate = pricingCalc.AdjustedRate;

    // Continue with existing calculation using effectiveRate
    var vehicleTotal = effectiveRate * days;
    // ...

    return new RentalPricing
    {
        // ... existing properties ...
        AppliedPricingRule = pricingCalc.AppliedRuleName,
        PricingMultiplier = pricingCalc.Multiplier
    };
}
```

## UI Design

### Pricing Rules List (`/settings/pricing-rules`)

```
+--------------------------------------------------+
| Pricing Rules                     [+ Add Rule]   |
+--------------------------------------------------+
| Filters: [All Types v] [Active Only] [This Year] |
+--------------------------------------------------+
| Rule Name          | Type    | Dates      | x    |
|--------------------|---------|------------|------|
| High Season 2025   | Season  | Dec-Feb    | 1.5x |
| Songkran 2025      | Event   | Apr 13-15  | 2.0x |
| Low Season Promo   | Custom  | Jun-Sep    | 0.7x |
+--------------------------------------------------+
```

### Pricing Rule Dialog

```
+------------------------------------------+
| Edit Pricing Rule                        |
+------------------------------------------+
| Name: [High Season 2025                ] |
| Type: (o) Season ( ) Event ( ) Custom    |
|                                          |
| Date Range:                              |
| From: [Dec 01, 2025] To: [Feb 28, 2026]  |
| [ ] Repeat yearly                        |
|                                          |
| Pricing:                                 |
| Multiplier: [1.50] (= +50%)             |
| Min Rate: [        ] Max Rate: [       ] |
|                                          |
| Applies To:                              |
| [x] All Vehicles                         |
| [ ] Specific Types: [ ] Motorbike [x] Car|
|                                          |
| Priority: [10] (higher wins on overlap)  |
|                                          |
|              [Cancel] [Save]             |
+------------------------------------------+
```

### Calendar View (`/settings/pricing-calendar`)

Visual month calendar showing multipliers per day with color coding:
- Green: Low season (< 1.0x)
- Yellow: Normal (1.0x)
- Orange: Shoulder (1.2-1.4x)
- Red: High/Event (> 1.5x)

## Regional Template Presets (Built-In)

Shop owners can apply a regional preset as a starting point, then customize. **No auto-seeding** - shops start with zero rules until they choose a preset.

### Available Presets

```csharp
public enum RegionalPreset
{
    None,               // Start blank (default)
    AndamanCoast,       // Phuket, Krabi, Phang Nga, Trang, Satun
    GulfCoast,          // Koh Samui, Koh Phangan, Koh Tao, Chumphon
    SouthernBorder,     // Hat Yai, Songkhla, Yala, Betong
    Northern,           // Chiang Mai, Chiang Rai, Pai, Mae Hong Son
    Eastern,            // Pattaya, Rayong, Koh Samet, Koh Chang
    Central,            // Bangkok, Ayutthaya
    Western,            // Hua Hin, Kanchanaburi, Cha-am
    Isaan               // Udon Thani, Khon Kaen, Korat
}
```

---

## PRESET 1: Andaman Coast (Phuket, Krabi, Phang Nga)

**Target demographics**: Russian, European (UK, German, Scandinavian, French, Italian), Australian, Chinese, Indian (growing), Middle Eastern (growing)
**Monsoon**: Southwest (May-October = rainy)
**Key insight**: Multiple overlapping tourist waves from different regions

### Understanding Phuket's Multi-Demographic Tourism

Phuket receives tourists from many countries, each with their own holiday calendars:

| Demographic | Peak Period | Key Events | Share |
|-------------|-------------|------------|-------|
| **Russian** | Nov-Apr | Orthodox NY (Jan 1-14), May holidays | 25% |
| **European** | Nov-Mar | Christmas, Easter, Half-terms | 30% |
| **Australian** | Dec-Jan | Southern summer holidays | 10% |
| **Chinese** | CNY, Golden Weeks | Lunar calendar events | 15% |
| **Indian** | Oct-Feb | Diwali, Wedding season | 10% |
| **Middle Eastern** | Jul-Aug, Eid | Summer, Islamic holidays | 5% |
| **Domestic Thai** | Songkran, Long weekends | Thai calendar | 5% |

### Seasons

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Ultra Peak | Dec 20 - Jan 10 | 2.2x | 12 | Christmas/NY convergence |
| High Season | Nov 1 - Mar 31 | 1.6x | 10 | European/Russian winter |
| Shoulder (Apr) | Apr 1 - Apr 30 | 1.3x | 8 | Songkran month |
| Shoulder (Oct-Nov) | Oct 15 - Oct 31 | 1.1x | 8 | Monsoon ending |
| Low Season | May 1 - Oct 14 | 0.65x | 6 | Southwest monsoon |
| Deep Low | Jun 1 - Aug 31 | 0.55x | 4 | Wettest months |

---

### RUSSIAN TOURISTS (Priority 25-28)

Russia is Phuket's #1 source market. Key periods:

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Russian New Year** | Jan 1 - Jan 14 | **2.8x** | **28** | Orthodox Christmas Jan 7 |
| Russian Winter Break | Jan 15 - Jan 31 | 2.0x | 22 | School holidays continue |
| Russian Feb Holidays | Feb 23 (+ bridge) | 1.6x | 18 | Defender's Day |
| Russian Women's Day | Mar 8 (+ bridge) | 1.6x | 18 | International Women's Day |
| **Russian May Holidays** | May 1 - May 12 | **1.8x** | **20** | Labour + Victory Day cluster |
| Russian Summer | Jul 1 - Aug 31 | 1.3x | 14 | Families, despite monsoon |
| Russia Day | Jun 12 (+ bridge) | 1.4x | 15 | National holiday |

---

### EUROPEAN TOURISTS

#### UK (Priority 18-25)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Christmas/New Year** | Dec 20 - Jan 5 | **2.5x** | **25** | Peak UK travel |
| UK February Half-Term | Feb 15 - Feb 23 | 1.6x | 18 | 1 week school break |
| UK Easter Holidays | Varies (2 weeks) | 1.7x | 20 | Major family travel |
| UK May Half-Term | May 24 - Jun 1 | 1.4x | 16 | Even in monsoon |
| UK Summer | Jul 20 - Sep 5 | 1.3x | 14 | Despite monsoon |
| UK October Half-Term | Oct 25 - Nov 2 | 1.5x | 17 | Monsoon ending |

#### German (Priority 16-20)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| German Christmas | Dec 20 - Jan 6 | 2.3x | 24 | Long break tradition |
| German Easter | Varies (2 weeks) | 1.6x | 18 | School holidays |
| German Autumn | Oct 1 - Oct 20 | 1.3x | 15 | Bavarian Herbstferien |
| German Winter | Feb 1 - Mar 15 | 1.4x | 16 | Ski alternative |

#### Scandinavian (Priority 16-20)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Nordic Christmas | Dec 20 - Jan 7 | 2.2x | 23 | Escaping dark winter |
| Nordic Winter | Jan 15 - Mar 15 | 1.5x | 17 | Peak winter escape |
| Nordic Easter | Varies | 1.5x | 17 | Påsk holidays |
| Nordic Autumn | Oct 15 - Nov 5 | 1.3x | 15 | Autumn break |

#### French (Priority 15-18)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| French Christmas | Dec 20 - Jan 5 | 2.0x | 22 | Vacances de Noël |
| French February | Feb 8 - Mar 8 | 1.5x | 17 | Zone A/B/C rotations |
| French Toussaint | Oct 18 - Nov 3 | 1.4x | 16 | All Saints break |
| **French August** | Aug 1 - Aug 20 | 1.5x | 17 | Grandes vacances |

#### Italian (Priority 14-16)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Italian Christmas | Dec 23 - Jan 6 | 1.8x | 20 | Natale + Epifania |
| **Ferragosto** | Aug 10 - Aug 20 | 1.5x | 17 | Peak Italian holiday |
| Italian Easter | Varies | 1.4x | 16 | Pasqua + Pasquetta |

---

### AUSTRALIAN TOURISTS (Priority 18-22)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Aussie Summer** | Dec 15 - Jan 31 | **2.0x** | **22** | Peak family travel |
| Aussie Easter | Varies (2 weeks) | 1.5x | 17 | School holidays |
| Aussie July | Jul 1 - Jul 15 | 1.3x | 14 | Winter school break |
| Aussie September | Sep 20 - Oct 10 | 1.3x | 15 | Spring break |

---

### CHINESE TOURISTS (Priority 20-26)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Chinese New Year** | Varies (7+ days) | **2.2x** | **26** | Biggest Chinese holiday |
| CNY Eve Rush | 2 days before CNY | 1.8x | 22 | Last minute arrivals |
| Qingming Festival | Apr 4-6 | 1.4x | 16 | Tomb Sweeping Day |
| Labour Day Golden Week | May 1-5 | 1.6x | 18 | 5-day holiday |
| Dragon Boat Festival | Varies (Jun) | 1.3x | 14 | Duanwu Festival |
| **National Day Golden Week** | Oct 1-7 | **1.8x** | **20** | 7-day holiday |
| Mid-Autumn Festival | Varies (Sep/Oct) | 1.4x | 16 | Mooncake Festival |

---

### INDIAN TOURISTS (Priority 16-20) - Growing Market

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Diwali** | Oct/Nov (5 days) | **1.8x** | **20** | Festival of Lights |
| Indian Wedding Season | Nov 15 - Feb 15 | 1.4x | 16 | Auspicious dates |
| Holi | Mar (varies) | 1.3x | 14 | Festival of Colors |
| Indian Summer | May 15 - Jun 30 | 1.2x | 12 | School holidays |
| Ganesh Chaturthi | Aug/Sep | 1.2x | 12 | 10-day festival |

---

### MIDDLE EASTERN TOURISTS (Priority 16-20) - Growing Market

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Eid al-Fitr** | Varies (lunar) | **1.8x** | **20** | End of Ramadan |
| Eid al-Adha | Varies (lunar) | 1.6x | 18 | Feast of Sacrifice |
| **Gulf Summer** | Jul 1 - Aug 31 | **1.5x** | **17** | Escaping 50°C heat |
| Saudi National Day | Sep 23 (+ bridge) | 1.3x | 14 | Saudi holiday |
| UAE National Day | Dec 2-3 | 1.4x | 15 | Emirates holiday |

---

### PHUKET LOCAL EVENTS (Priority 18-25)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Vegetarian Festival** | Sep/Oct (9 days) | **1.6x** | **20** | Phuket's signature event |
| Old Phuket Town Festival | Feb (varies) | 1.3x | 14 | Cultural celebration |
| Phuket Bike Week | Apr (varies) | 1.4x | 15 | Motorcycle rally |
| Laguna Phuket Triathlon | Nov (varies) | 1.4x | 16 | International event |
| King's Cup Regatta | Dec (first week) | 1.5x | 17 | Sailing event |
| Phuket Pride | Apr (varies) | 1.3x | 14 | LGBTQ+ festival |

---

### THAI HOLIDAYS (Priority 14-18)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Songkran** | Apr 13-17 | **1.8x** | **20** | Extended Thai NY |
| Loy Krathong | Nov (full moon) | 1.3x | 14 | Lantern festival |
| King's Birthday | Jul 28 | 1.2x | 12 | National holiday |
| Queen's Birthday | Jun 3 | 1.2x | 12 | National holiday |
| Constitution Day | Dec 10 | 1.3x | 14 | Long weekend possible |
| Makha Bucha | Feb (full moon) | 1.2x | 12 | Buddhist holiday |
| Visakha Bucha | May (full moon) | 1.2x | 12 | Buddhist holiday |
| Asanha Bucha | Jul (full moon) | 1.2x | 12 | Buddhist holiday |
| Chulalongkorn Day | Oct 23 | 1.2x | 12 | Memorial day |

---

```csharp
public static class AndamanCoastPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // ════════════════════════════════════════════════════════════
        // SEASONS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Ultra Peak Season", RuleType = Season,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 10),
                Multiplier = 2.2m, IsRecurring = true, Priority = 12,
                Description = "Christmas/NY + Russian NY convergence" },

        new() { Name = "High Season", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 3, 31),
                Multiplier = 1.6m, IsRecurring = true, Priority = 10,
                Description = "European/Russian winter escape" },

        new() { Name = "Shoulder Season (Apr)", RuleType = Season,
                StartDate = new(2025, 4, 1), EndDate = new(2025, 4, 30),
                Multiplier = 1.3m, IsRecurring = true, Priority = 8,
                Description = "Songkran month - still busy" },

        new() { Name = "Shoulder Season (Oct)", RuleType = Season,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 10, 31),
                Multiplier = 1.1m, IsRecurring = true, Priority = 8,
                Description = "Monsoon ending, tourists returning" },

        new() { Name = "Low Season", RuleType = Season,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 10, 14),
                Multiplier = 0.65m, IsRecurring = true, Priority = 6,
                Description = "Southwest monsoon" },

        new() { Name = "Deep Low Season", RuleType = Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 8, 31),
                Multiplier = 0.55m, IsRecurring = true, Priority = 4,
                Description = "Wettest months - heavy discounts" },

        // ════════════════════════════════════════════════════════════
        // RUSSIAN TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Russian New Year", RuleType = Event,
                StartDate = new(2026, 1, 1), EndDate = new(2026, 1, 14),
                Multiplier = 2.8m, IsRecurring = true, Priority = 28,
                Description = "Orthodox Christmas (Jan 7) - PEAK Russian period" },

        new() { Name = "Russian Winter Break", RuleType = Event,
                StartDate = new(2026, 1, 15), EndDate = new(2026, 1, 31),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "School holidays continue" },

        new() { Name = "Russian Defender's Day", RuleType = Event,
                StartDate = new(2025, 2, 22), EndDate = new(2025, 2, 24),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Feb 23 holiday + bridge days" },

        new() { Name = "Russian Women's Day", RuleType = Event,
                StartDate = new(2025, 3, 7), EndDate = new(2025, 3, 10),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Mar 8 International Women's Day" },

        new() { Name = "Russian May Holidays", RuleType = Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 12),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Labour Day (May 1) + Victory Day (May 9)" },

        new() { Name = "Russia Day", RuleType = Event,
                StartDate = new(2025, 6, 11), EndDate = new(2025, 6, 14),
                Multiplier = 1.4m, IsRecurring = true, Priority = 15,
                Description = "Jun 12 National Day + bridge" },

        new() { Name = "Russian Summer", RuleType = Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 8, 31),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Family holidays despite monsoon" },

        // ════════════════════════════════════════════════════════════
        // UK TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "UK Christmas/New Year", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.5m, IsRecurring = true, Priority = 25,
                Description = "Peak British travel period" },

        new() { Name = "UK February Half-Term", RuleType = Event,
                StartDate = new(2025, 2, 15), EndDate = new(2025, 2, 23),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "1 week school break" },

        new() { Name = "UK Easter Holidays", RuleType = Event,
                StartDate = new(2025, 4, 5), EndDate = new(2025, 4, 21), // 2025 dates
                Multiplier = 1.7m, IsRecurring = false, Priority = 20,
                Description = "2 week school break - dates vary" },

        new() { Name = "UK May Half-Term", RuleType = Event,
                StartDate = new(2025, 5, 24), EndDate = new(2025, 6, 1),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Even visits during monsoon start" },

        new() { Name = "UK Summer Holidays", RuleType = Event,
                StartDate = new(2025, 7, 20), EndDate = new(2025, 9, 5),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Family travel despite monsoon" },

        new() { Name = "UK October Half-Term", RuleType = Event,
                StartDate = new(2025, 10, 25), EndDate = new(2025, 11, 2),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Monsoon ending - popular week" },

        // ════════════════════════════════════════════════════════════
        // GERMAN TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "German Christmas", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 6),
                Multiplier = 2.3m, IsRecurring = true, Priority = 24,
                Description = "Long Christmas/NY tradition" },

        new() { Name = "German Winter Escape", RuleType = Event,
                StartDate = new(2025, 2, 1), EndDate = new(2025, 3, 15),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Ski alternative seekers" },

        new() { Name = "German Easter", RuleType = Event,
                StartDate = new(2025, 4, 10), EndDate = new(2025, 4, 27), // 2025 dates
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "School holidays - dates vary" },

        new() { Name = "German Autumn Break", RuleType = Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 20),
                Multiplier = 1.3m, IsRecurring = true, Priority = 15,
                Description = "Herbstferien varies by state" },

        // ════════════════════════════════════════════════════════════
        // SCANDINAVIAN TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Nordic Christmas", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 7),
                Multiplier = 2.2m, IsRecurring = true, Priority = 23,
                Description = "Escaping dark Nordic winter" },

        new() { Name = "Nordic Winter Escape", RuleType = Event,
                StartDate = new(2025, 1, 15), EndDate = new(2025, 3, 15),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Peak sun-seeking period" },

        new() { Name = "Nordic Easter", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 21), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "Påsk holidays - dates vary" },

        new() { Name = "Nordic Autumn Break", RuleType = Event,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 11, 5),
                Multiplier = 1.3m, IsRecurring = true, Priority = 15,
                Description = "Høstferie/Syysloma" },

        // ════════════════════════════════════════════════════════════
        // FRENCH TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "French Christmas", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "Vacances de Noël" },

        new() { Name = "French February Break", RuleType = Event,
                StartDate = new(2025, 2, 8), EndDate = new(2025, 3, 8),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Zone A/B/C rotation - month long waves" },

        new() { Name = "French Toussaint", RuleType = Event,
                StartDate = new(2025, 10, 18), EndDate = new(2025, 11, 3),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "All Saints school break" },

        new() { Name = "French August", RuleType = Event,
                StartDate = new(2025, 8, 1), EndDate = new(2025, 8, 20),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Grandes vacances - monsoon doesn't stop them" },

        // ════════════════════════════════════════════════════════════
        // ITALIAN TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Italian Christmas", RuleType = Event,
                StartDate = new(2025, 12, 23), EndDate = new(2026, 1, 6),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Natale to Epifania" },

        new() { Name = "Ferragosto", RuleType = Event,
                StartDate = new(2025, 8, 10), EndDate = new(2025, 8, 20),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Peak Italian summer holiday" },

        new() { Name = "Italian Easter", RuleType = Event,
                StartDate = new(2025, 4, 17), EndDate = new(2025, 4, 22), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Pasqua + Pasquetta" },

        // ════════════════════════════════════════════════════════════
        // AUSTRALIAN TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Australian Summer", RuleType = Event,
                StartDate = new(2025, 12, 15), EndDate = new(2026, 1, 31),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "Peak Aussie family travel" },

        new() { Name = "Australian Easter", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 27), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "2 week school break" },

        new() { Name = "Australian July Break", RuleType = Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 7, 15),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Winter school holidays" },

        new() { Name = "Australian September Break", RuleType = Event,
                StartDate = new(2025, 9, 20), EndDate = new(2025, 10, 10),
                Multiplier = 1.3m, IsRecurring = true, Priority = 15,
                Description = "Spring school holidays" },

        // ════════════════════════════════════════════════════════════
        // CHINESE TOURISTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4), // 2025 dates
                Multiplier = 2.2m, IsRecurring = false, Priority = 26,
                Description = "Spring Festival - dates vary (lunar)" },

        new() { Name = "CNY Eve Rush", RuleType = Event,
                StartDate = new(2025, 1, 26), EndDate = new(2025, 1, 27), // 2025 dates
                Multiplier = 1.8m, IsRecurring = false, Priority = 22,
                Description = "Last minute arrivals" },

        new() { Name = "Chinese Qingming", RuleType = Event,
                StartDate = new(2025, 4, 4), EndDate = new(2025, 4, 6),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Tomb Sweeping Day" },

        new() { Name = "Chinese Labour Day", RuleType = Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 5),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "5-day Golden Week" },

        new() { Name = "Dragon Boat Festival", RuleType = Event,
                StartDate = new(2025, 5, 31), EndDate = new(2025, 6, 2), // 2025 dates
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Duanwu Festival - dates vary (lunar)" },

        new() { Name = "Chinese National Day", RuleType = Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "7-day Golden Week" },

        new() { Name = "Mid-Autumn Festival", RuleType = Event,
                StartDate = new(2025, 10, 6), EndDate = new(2025, 10, 8), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Mooncake Festival - dates vary (lunar)" },

        // ════════════════════════════════════════════════════════════
        // INDIAN TOURISTS (Growing Market)
        // ════════════════════════════════════════════════════════════

        new() { Name = "Diwali", RuleType = Event,
                StartDate = new(2025, 10, 20), EndDate = new(2025, 10, 25), // 2025 dates
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Festival of Lights - dates vary" },

        new() { Name = "Indian Wedding Season", RuleType = Event,
                StartDate = new(2025, 11, 15), EndDate = new(2026, 2, 15),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Auspicious wedding dates" },

        new() { Name = "Holi", RuleType = Event,
                StartDate = new(2025, 3, 13), EndDate = new(2025, 3, 15), // 2025 dates
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Festival of Colors - dates vary" },

        new() { Name = "Indian Summer Break", RuleType = Event,
                StartDate = new(2025, 5, 15), EndDate = new(2025, 6, 30),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "School summer holidays" },

        // ════════════════════════════════════════════════════════════
        // MIDDLE EASTERN TOURISTS (Growing Market)
        // ════════════════════════════════════════════════════════════

        new() { Name = "Eid al-Fitr", RuleType = Event,
                StartDate = new(2025, 3, 30), EndDate = new(2025, 4, 5), // 2025 dates
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "End of Ramadan - dates shift yearly" },

        new() { Name = "Eid al-Adha", RuleType = Event,
                StartDate = new(2025, 6, 6), EndDate = new(2025, 6, 12), // 2025 dates
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Feast of Sacrifice - dates shift yearly" },

        new() { Name = "Gulf Summer Escape", RuleType = Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 8, 31),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Escaping 50°C Gulf heat" },

        new() { Name = "Saudi National Day", RuleType = Event,
                StartDate = new(2025, 9, 23), EndDate = new(2025, 9, 25),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Saudi holiday + bridge" },

        new() { Name = "UAE National Day", RuleType = Event,
                StartDate = new(2025, 12, 2), EndDate = new(2025, 12, 4),
                Multiplier = 1.4m, IsRecurring = true, Priority = 15,
                Description = "Emirates holiday" },

        // ════════════════════════════════════════════════════════════
        // PHUKET LOCAL EVENTS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Phuket Vegetarian Festival", RuleType = Event,
                StartDate = new(2025, 9, 21), EndDate = new(2025, 9, 29), // 2025 dates
                Multiplier = 1.6m, IsRecurring = false, Priority = 20,
                Description = "9-day festival - dates vary (lunar)" },

        new() { Name = "Old Phuket Town Festival", RuleType = Event,
                StartDate = new(2025, 2, 7), EndDate = new(2025, 2, 9),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Cultural celebration" },

        new() { Name = "Phuket Bike Week", RuleType = Event,
                StartDate = new(2025, 4, 10), EndDate = new(2025, 4, 12),
                Multiplier = 1.4m, IsRecurring = true, Priority = 15,
                Description = "Motorcycle rally" },

        new() { Name = "Laguna Phuket Triathlon", RuleType = Event,
                StartDate = new(2025, 11, 23), EndDate = new(2025, 11, 24),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "International triathlon" },

        new() { Name = "King's Cup Regatta", RuleType = Event,
                StartDate = new(2025, 12, 1), EndDate = new(2025, 12, 6),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "International sailing event" },

        new() { Name = "Phuket Pride", RuleType = Event,
                StartDate = new(2025, 4, 25), EndDate = new(2025, 4, 27),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "LGBTQ+ festival" },

        // ════════════════════════════════════════════════════════════
        // THAI NATIONAL HOLIDAYS
        // ════════════════════════════════════════════════════════════

        new() { Name = "Songkran Festival", RuleType = Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 17),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Extended Thai New Year" },

        new() { Name = "Loy Krathong", RuleType = Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 6), // 2025 dates
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Lantern festival - dates vary (lunar)" },

        new() { Name = "King's Birthday", RuleType = Event,
                StartDate = new(2025, 7, 28), EndDate = new(2025, 7, 28),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "National holiday" },

        new() { Name = "Queen's Birthday", RuleType = Event,
                StartDate = new(2025, 6, 3), EndDate = new(2025, 6, 3),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "National holiday" },

        new() { Name = "Constitution Day", RuleType = Event,
                StartDate = new(2025, 12, 10), EndDate = new(2025, 12, 10),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Possible long weekend" },

        new() { Name = "Makha Bucha", RuleType = Event,
                StartDate = new(2025, 2, 12), EndDate = new(2025, 2, 12), // 2025 dates
                Multiplier = 1.2m, IsRecurring = false, Priority = 12,
                Description = "Buddhist holiday - dates vary (lunar)" },

        new() { Name = "Visakha Bucha", RuleType = Event,
                StartDate = new(2025, 5, 11), EndDate = new(2025, 5, 12), // 2025 dates
                Multiplier = 1.2m, IsRecurring = false, Priority = 12,
                Description = "Buddha's birthday - dates vary (lunar)" },

        new() { Name = "Asanha Bucha", RuleType = Event,
                StartDate = new(2025, 7, 10), EndDate = new(2025, 7, 10), // 2025 dates
                Multiplier = 1.2m, IsRecurring = false, Priority = 12,
                Description = "Buddhist Lent - dates vary (lunar)" },

        new() { Name = "Chulalongkorn Day", RuleType = Event,
                StartDate = new(2025, 10, 23), EndDate = new(2025, 10, 23),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "Memorial day" }
    };
}
```

### Priority Hierarchy for Phuket

```
28  Russian New Year (2.8x) - THE peak for Phuket
26  Chinese New Year (2.2x)
25  UK Christmas/New Year (2.5x)
24  German Christmas (2.3x)
23  Nordic Christmas (2.2x)
22  Russian Winter, Aussie Summer, French Christmas, CNY Eve (2.0x)
20  Easter (UK/Aussie), Songkran, Veg Festival, Diwali, Eid al-Fitr (1.7-1.8x)
18  Half-terms, May holidays, Eid al-Adha, German Easter (1.6x)
17  French Feb/Aug, Nordic Winter, Gulf Summer, King's Cup (1.5x)
16  European breaks, Indian Wedding, Chinese Labour Day (1.4x)
14-15 Minor events, summer visitors despite monsoon (1.3x)
12  Seasons (Ultra Peak 2.2x, High 1.6x)
10  High Season base (1.6x)
8   Shoulder seasons (1.1-1.3x)
6   Low Season (0.65x)
4   Deep Low Season (0.55x)
```

---

## PRESET 2: Gulf Coast (Koh Samui, Koh Phangan, Koh Tao)

**Target demographics**: Mixed international, backpackers, party tourists
**Monsoon**: Northeast (Oct-Dec = rainy) - OPPOSITE to Andaman!

### Seasons

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Peak Season | Jan 1 - Apr 15 | 1.6x | 10 | Best weather |
| Summer Peak | Jul 1 - Sep 15 | 1.4x | 10 | European summer holidays |
| Shoulder | Apr 16 - Jun 30 | 1.1x | 8 | Transition |
| Monsoon Low | Oct 15 - Dec 15 | 0.6x | 8 | Gulf monsoon |
| Recovery | Dec 16 - Dec 31 | 1.0x | 8 | Post-monsoon |

### Party Events (Priority 25) - Koh Phangan Specialty

| Rule | Type | Multiplier | Priority | Notes |
|------|------|------------|----------|-------|
| Full Moon Party | Monthly (lunar) | 2.0x | 25 | THE party |
| Half Moon Party | 2x monthly | 1.5x | 20 | Smaller party |
| Black Moon Party | Monthly | 1.3x | 18 | Chill party |

### Major Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Christmas/New Year | Dec 20 - Jan 5 | 2.0x | 22 | Still busy despite weather |
| Songkran | Apr 13 - 15 | 1.8x | 20 | Famous beach parties |
| Chinese New Year | Varies | 1.6x | 18 | Chinese tourists |

```csharp
public static class GulfCoastPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === SEASONS (Opposite monsoon pattern!) ===
        new() { Name = "Peak Season", RuleType = Season,
                StartDate = new(2025, 1, 1), EndDate = new(2025, 4, 15),
                Multiplier = 1.6m, IsRecurring = true, Priority = 10,
                Description = "Best weather - dry and sunny" },

        new() { Name = "European Summer Peak", RuleType = Season,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 9, 15),
                Multiplier = 1.4m, IsRecurring = true, Priority = 10,
                Description = "European school holidays" },

        new() { Name = "Shoulder Season", RuleType = Season,
                StartDate = new(2025, 4, 16), EndDate = new(2025, 6, 30),
                Multiplier = 1.1m, IsRecurring = true, Priority = 8,
                Description = "Transition period - still good weather" },

        new() { Name = "Monsoon Low Season", RuleType = Season,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 12, 15),
                Multiplier = 0.6m, IsRecurring = true, Priority = 8,
                Description = "Gulf monsoon - heavy rain, ferries may cancel" },

        new() { Name = "Post-Monsoon Recovery", RuleType = Season,
                StartDate = new(2025, 12, 16), EndDate = new(2025, 12, 31),
                Multiplier = 1.0m, IsRecurring = true, Priority = 8,
                Description = "Weather improving, holiday demand" },

        // === PARTY EVENTS (Koh Phangan) ===
        // Note: Full Moon dates must be updated yearly - these are 2025 examples
        new() { Name = "Full Moon Party (Jan)", RuleType = Event,
                StartDate = new(2025, 1, 13), EndDate = new(2025, 1, 14),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25,
                Description = "Monthly full moon party - update dates yearly" },

        new() { Name = "Full Moon Party (Feb)", RuleType = Event,
                StartDate = new(2025, 2, 12), EndDate = new(2025, 2, 13),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25 },

        new() { Name = "Full Moon Party (Mar)", RuleType = Event,
                StartDate = new(2025, 3, 14), EndDate = new(2025, 3, 15),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25 },

        // ... (repeat for each month - shop must update yearly)

        new() { Name = "NYE Full Moon Combo", RuleType = Event,
                StartDate = new(2025, 12, 29), EndDate = new(2026, 1, 2),
                Multiplier = 2.5m, IsRecurring = true, Priority = 28,
                Description = "New Year + Full Moon alignment - MASSIVE" },

        // === STANDARD EVENTS ===
        new() { Name = "Christmas & New Year", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "Busy despite monsoon tail-end" },

        new() { Name = "Songkran Beach Party", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Extended Songkran celebrations" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.6m, IsRecurring = false, Priority = 18 },

        new() { Name = "Ten Stars Samui Festival", RuleType = Event,
                StartDate = new(2025, 9, 1), EndDate = new(2025, 9, 10),
                Multiplier = 1.3m, IsRecurring = true, Priority = 15,
                Description = "Local food and culture festival" }
    };
}
```

---

## PRESET 3: Southern Border (Hat Yai, Songkhla, Betong)

**Target demographics**: Malaysian (80%+), Singaporean
**Monsoon**: Minimal impact - Malaysians come regardless of weather
**Pattern**: Long weekend-driven, school holiday spikes, religious festival peaks

### Understanding Malaysian Travel Patterns

Malaysians create long weekends by:
1. **Bridge days** - When holiday falls on Tue/Thu, they take Mon/Fri off
2. **Stacking** - Combining public holidays with annual leave
3. **School holidays** - Family trips during term breaks
4. **Religious festivals** - Extended celebrations (Eid can be 1-2 weeks)

### Base Weekend Pattern

| Rule | Type | Multiplier | Priority | Notes |
|------|------|------------|----------|-------|
| Regular Weekend | DayOfWeek (Sat-Sun) | 1.15x | 8 | Normal weekends |
| Friday Premium | DayOfWeek (Fri) | 1.2x | 10 | Weekend starters |
| Thursday Premium | DayOfWeek (Thu) | 1.1x | 6 | Long weekend starters |

### Malaysian School Holidays (CRITICAL - Priority 20)

| Rule | Dates (2025 Approximate) | Multiplier | Priority | Notes |
|------|--------------------------|------------|----------|-------|
| Term 1 Mid-Break | Mar 14-23 | 1.5x | 20 | 10-day break |
| Term 2 Break | May 24 - Jun 8 | 1.7x | 22 | 2+ weeks - MAJOR |
| Term 3 Mid-Break | Aug 16-24 | 1.4x | 18 | 9-day break |
| Year-End Break | Nov 22 - Jan 1 | 1.9x | 24 | 6 weeks - BIGGEST |

### Major Religious Festivals (Priority 25-30)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| **Hari Raya Aidilfitri** | Varies (lunar) | **3.0x** | **30** | BIGGEST - Eid, 1-2 week exodus |
| **Chinese New Year** | Varies (Jan/Feb) | **2.5x** | **28** | 5-7 days, massive crowds |
| Hari Raya Haji | Varies (lunar) | 2.0x | 24 | Eid al-Adha, 4-5 day break |
| Deepavali | Oct/Nov | 1.8x | 20 | Hindu festival + school break overlap |
| Thaipusam | Jan/Feb (lunar) | 1.6x | 18 | Hindu pilgrimage festival |

### Malaysian Public Holiday Long Weekends (Priority 18)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| New Year Long Weekend | Dec 31 - Jan 2 | 1.8x | 20 | Combined with year-end school |
| Federal Territory Day | Feb 1 (+ bridge) | 1.4x | 16 | KL/Putrajaya/Labuan |
| Nuzul Al-Quran | Varies (lunar) | 1.5x | 17 | Quran revelation day |
| Awal Muharram | Varies (lunar) | 1.4x | 16 | Islamic New Year |
| Maulidur Rasul | Varies (lunar) | 1.4x | 16 | Prophet's birthday |
| Labour Day | May 1 (+ bridge) | 1.5x | 17 | May Day long weekend |
| Wesak Day | May (lunar) | 1.5x | 17 | Buddha's birthday |
| Agong Birthday | 1st Mon Jun | 1.5x | 17 | King's birthday |
| Merdeka Day | Aug 31 (+ bridge) | 1.6x | 18 | Independence Day |
| Malaysia Day | Sep 16 (+ bridge) | 1.6x | 18 | Malaysia formation |
| Christmas | Dec 25-26 | 1.6x | 18 | + year-end school overlap |

### State-Specific Holidays (Northern Malaysian States)

Visitors to Hat Yai primarily come from Kedah, Penang, Perak, Perlis:

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Penang Heritage Day | Jul 7 | 1.3x | 14 | Penang state holiday |
| Thaipusam (Penang) | Jan/Feb | 1.5x | 17 | Big in Penang |
| Sultan Kedah Birthday | Jun (3rd Sun) | 1.3x | 14 | Kedah state holiday |
| Sultan Perak Birthday | Nov (1st Fri) | 1.3x | 14 | Perak state holiday |

### Singaporean Spillover

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| SG Chinese New Year | Jan/Feb | 1.8x | 20 | Overlaps with MY CNY |
| SG National Day | Aug 9 (+ bridge) | 1.5x | 17 | Singapore independence |
| SG Deepavali | Oct/Nov | 1.4x | 16 | Overlaps with MY |
| SG June Holidays | Jun 1-30 | 1.4x | 16 | School mid-year |
| SG Year-End | Nov 15 - Dec 31 | 1.5x | 18 | School + Christmas |

```csharp
public static class SouthernBorderPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === BASE WEEKEND PATTERN ===
        new() { Name = "Weekend (Sat-Sun)", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Saturday, DayOfWeek.Sunday],
                Multiplier = 1.15m, IsRecurring = true, Priority = 8,
                Description = "Regular weekend traffic" },

        new() { Name = "Friday Premium", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Friday],
                Multiplier = 1.2m, IsRecurring = true, Priority = 10,
                Description = "Weekend starters arrive Friday" },

        new() { Name = "Thursday Premium", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Thursday],
                Multiplier = 1.1m, IsRecurring = true, Priority = 6,
                Description = "Long weekend early starters" },

        // === MALAYSIAN SCHOOL HOLIDAYS ===
        new() { Name = "MY School Break (Mar)", RuleType = Event,
                StartDate = new(2025, 3, 14), EndDate = new(2025, 3, 23),
                Multiplier = 1.5m, IsRecurring = true, Priority = 20,
                Description = "Term 1 mid-break - 10 days" },

        new() { Name = "MY School Break (Jun)", RuleType = Event,
                StartDate = new(2025, 5, 24), EndDate = new(2025, 6, 8),
                Multiplier = 1.7m, IsRecurring = true, Priority = 22,
                Description = "Term 2 break - MAJOR family travel period" },

        new() { Name = "MY School Break (Aug)", RuleType = Event,
                StartDate = new(2025, 8, 16), EndDate = new(2025, 8, 24),
                Multiplier = 1.4m, IsRecurring = true, Priority = 18,
                Description = "Term 3 mid-break" },

        new() { Name = "MY Year-End Holiday", RuleType = Event,
                StartDate = new(2025, 11, 22), EndDate = new(2026, 1, 1),
                Multiplier = 1.9m, IsRecurring = true, Priority = 24,
                Description = "6 weeks - BIGGEST travel period for Hat Yai" },

        // === MAJOR RELIGIOUS FESTIVALS ===
        new() { Name = "Hari Raya Aidilfitri", RuleType = Event,
                StartDate = new(2025, 3, 28), EndDate = new(2025, 4, 7), // 2025 dates
                Multiplier = 3.0m, IsRecurring = false, Priority = 30,
                Description = "EID - THE BIGGEST EVENT. Full week+ exodus to Hat Yai. Dates shift ~11 days/year" },

        new() { Name = "Hari Raya Aidilfitri Eve", RuleType = Event,
                StartDate = new(2025, 3, 26), EndDate = new(2025, 3, 27), // 2025 dates
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "Pre-Raya rush - last minute shopping" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 3),
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "CNY week - massive Malaysian Chinese crowds" },

        new() { Name = "Chinese New Year Eve", RuleType = Event,
                StartDate = new(2025, 1, 26), EndDate = new(2025, 1, 27),
                Multiplier = 2.0m, IsRecurring = false, Priority = 24,
                Description = "Pre-CNY shopping rush" },

        new() { Name = "Hari Raya Haji", RuleType = Event,
                StartDate = new(2025, 6, 6), EndDate = new(2025, 6, 10), // 2025 dates
                Multiplier = 2.0m, IsRecurring = false, Priority = 24,
                Description = "Eid al-Adha - 4-5 day break" },

        new() { Name = "Deepavali", RuleType = Event,
                StartDate = new(2025, 10, 18), EndDate = new(2025, 10, 22), // 2025 dates
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Diwali - Hindu festival of lights" },

        new() { Name = "Thaipusam", RuleType = Event,
                StartDate = new(2025, 2, 10), EndDate = new(2025, 2, 12), // 2025 dates
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Hindu pilgrimage festival - big in Penang" },

        // === PUBLIC HOLIDAY LONG WEEKENDS ===
        new() { Name = "New Year Long Weekend", RuleType = Event,
                StartDate = new(2024, 12, 31), EndDate = new(2025, 1, 2),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "NYE + New Year holiday" },

        new() { Name = "Federal Territory Day", RuleType = Event,
                StartDate = new(2025, 2, 1), EndDate = new(2025, 2, 2),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "KL/Putrajaya residents escape" },

        new() { Name = "Nuzul Al-Quran", RuleType = Event,
                StartDate = new(2025, 3, 17), EndDate = new(2025, 3, 18), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "Quran revelation - Islamic holiday" },

        new() { Name = "Awal Muharram", RuleType = Event,
                StartDate = new(2025, 6, 26), EndDate = new(2025, 6, 28), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Islamic New Year" },

        new() { Name = "Maulidur Rasul", RuleType = Event,
                StartDate = new(2025, 9, 4), EndDate = new(2025, 9, 6), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Prophet Muhammad's birthday" },

        new() { Name = "Labour Day Weekend", RuleType = Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 4),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "May Day long weekend" },

        new() { Name = "Wesak Day", RuleType = Event,
                StartDate = new(2025, 5, 12), EndDate = new(2025, 5, 13), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "Buddha's birthday" },

        new() { Name = "Agong Birthday", RuleType = Event,
                StartDate = new(2025, 6, 7), EndDate = new(2025, 6, 9),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "King's birthday - first Monday of June" },

        new() { Name = "Merdeka Day Weekend", RuleType = Event,
                StartDate = new(2025, 8, 30), EndDate = new(2025, 9, 1),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Malaysian Independence Day" },

        new() { Name = "Malaysia Day Weekend", RuleType = Event,
                StartDate = new(2025, 9, 13), EndDate = new(2025, 9, 16),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Malaysia formation anniversary" },

        new() { Name = "Christmas Weekend", RuleType = Event,
                StartDate = new(2025, 12, 24), EndDate = new(2025, 12, 26),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Christmas + year-end school overlap" },

        // === NORTHERN MALAYSIAN STATE HOLIDAYS ===
        new() { Name = "Penang Heritage Day", RuleType = Event,
                StartDate = new(2025, 7, 7), EndDate = new(2025, 7, 7),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Penang state holiday" },

        new() { Name = "Sultan Kedah Birthday", RuleType = Event,
                StartDate = new(2025, 6, 15), EndDate = new(2025, 6, 16), // 3rd Sunday
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Kedah state holiday" },

        new() { Name = "Sultan Perak Birthday", RuleType = Event,
                StartDate = new(2025, 11, 7), EndDate = new(2025, 11, 8), // 1st Friday
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Perak state holiday" },

        // === SINGAPOREAN SPILLOVER ===
        new() { Name = "Singapore National Day", RuleType = Event,
                StartDate = new(2025, 8, 9), EndDate = new(2025, 8, 11),
                Multiplier = 1.5m, IsRecurring = true, Priority = 17,
                Description = "Singaporean independence long weekend" },

        new() { Name = "SG June Holidays", RuleType = Event,
                StartDate = new(2025, 5, 31), EndDate = new(2025, 6, 29),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Singapore school mid-year break" },

        new() { Name = "SG Year-End", RuleType = Event,
                StartDate = new(2025, 11, 15), EndDate = new(2025, 12, 31),
                Multiplier = 1.5m, IsRecurring = true, Priority = 18,
                Description = "Singapore school + Christmas season" },

        // === THAI HOLIDAYS (Secondary) ===
        new() { Name = "Songkran", RuleType = Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 15),
                Multiplier = 1.5m, IsRecurring = true, Priority = 16,
                Description = "Thai New Year - some Malaysian visitors too" }
    };
}
```

### Priority Hierarchy for Hat Yai

```
30  Hari Raya Aidilfitri (THE peak - 3.0x)
28  Hari Raya Eve, Chinese New Year (2.5x)
24  Year-End School, Hari Raya Haji, CNY Eve (2.0x)
22  June School Break (1.7x)
20  Deepavali, March School, NY Weekend (1.8x)
18  Merdeka, Malaysia Day, Aug School, SG Year-End (1.6x)
17  Labour Day, Wesak, Agong, Nuzul Al-Quran, SG National (1.5x)
16  State holidays, SG June, other Islamic holidays (1.3-1.4x)
14  Minor state holidays (1.3x)
10  Friday Premium (1.2x)
8   Saturday-Sunday Base (1.15x)
6   Thursday Premium (1.1x)
```

---

## PRESET 4: Northern Thailand (Chiang Mai, Chiang Rai, Pai)

**Target demographics**: Chinese, Japanese, Korean, Western digital nomads, retirees
**Climate**: Cool season (Nov-Feb), Hot season (Mar-May), Rainy (Jun-Oct)
**Special**: Burning season haze (Feb-Apr) can deter some tourists

### Seasons

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Cool Season Peak | Nov 15 - Feb 15 | 1.6x | 10 | Best weather, festivals |
| Hot/Burning Season | Feb 16 - Apr 30 | 0.9x | 8 | Haze issues |
| Green Season | Jun 1 - Oct 31 | 0.75x | 8 | Rainy but lush |
| Recovery | Nov 1 - Nov 14 | 1.2x | 8 | Post-monsoon |

### Major Events (Priority 20+)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Yi Peng + Loy Krathong | Nov (lunar) | 2.5x | 28 | MASSIVE - lantern festival |
| Songkran | Apr 13-15 | 2.2x | 25 | Famous CM celebrations |
| Chinese New Year | Varies | 2.0x | 22 | Huge Chinese-Thai population |
| Christmas/New Year | Dec 20 - Jan 5 | 1.8x | 20 | Western tourists |

### Asian Tourist Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Chinese Golden Week | Oct 1-7 | 1.8x | 20 | Mainland Chinese |
| Japanese Golden Week | Apr 29 - May 5 | 1.6x | 18 | Japanese tourists |
| Korean Chuseok | Sep/Oct (lunar) | 1.5x | 16 | Korean thanksgiving |
| Korean Seollal | Jan/Feb (lunar) | 1.5x | 16 | Korean new year |

### Local Festivals

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Flower Festival | First weekend Feb | 1.4x | 15 | Chiang Mai specialty |
| Bo Sang Umbrella Fest | 3rd weekend Jan | 1.3x | 14 | San Kamphaeng |
| CM Design Week | Dec (varies) | 1.2x | 12 | Creative scene |

```csharp
public static class NorthernPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === SEASONS ===
        new() { Name = "Cool Season Peak", RuleType = Season,
                StartDate = new(2025, 11, 15), EndDate = new(2026, 2, 15),
                Multiplier = 1.6m, IsRecurring = true, Priority = 10,
                Description = "Best weather, festival season" },

        new() { Name = "Hot/Burning Season", RuleType = Season,
                StartDate = new(2025, 2, 16), EndDate = new(2025, 4, 30),
                Multiplier = 0.9m, IsRecurring = true, Priority = 8,
                Description = "Haze from crop burning - some tourists avoid" },

        new() { Name = "Green/Rainy Season", RuleType = Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.75m, IsRecurring = true, Priority = 8,
                Description = "Rainy but lush scenery - budget travelers" },

        new() { Name = "Post-Monsoon", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2025, 11, 14),
                Multiplier = 1.2m, IsRecurring = true, Priority = 8,
                Description = "Weather improving, pre-festival" },

        // === MAJOR EVENTS ===
        new() { Name = "Yi Peng Lantern Festival", RuleType = Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 7), // 2025 dates
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "ICONIC - floating lanterns. Dates vary (lunar)" },

        new() { Name = "Songkran (Chiang Mai)", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 2.2m, IsRecurring = true, Priority = 25,
                Description = "Famous for best Songkran in Thailand" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.0m, IsRecurring = false, Priority = 22,
                Description = "Large Chinese-Thai population + mainland tourists" },

        new() { Name = "Christmas & New Year", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Western tourist peak" },

        // === ASIAN TOURIST EVENTS ===
        new() { Name = "Chinese Golden Week", RuleType = Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Mainland Chinese national holiday" },

        new() { Name = "Japanese Golden Week", RuleType = Event,
                StartDate = new(2025, 4, 29), EndDate = new(2025, 5, 6),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Japanese holiday cluster" },

        new() { Name = "Korean Chuseok", RuleType = Event,
                StartDate = new(2025, 10, 5), EndDate = new(2025, 10, 8), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 16,
                Description = "Korean thanksgiving - dates vary (lunar)" },

        new() { Name = "Korean Seollal", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 1, 30), // 2025 dates
                Multiplier = 1.5m, IsRecurring = false, Priority = 16,
                Description = "Korean New Year - dates vary (lunar)" },

        // === LOCAL FESTIVALS ===
        new() { Name = "Chiang Mai Flower Festival", RuleType = Event,
                StartDate = new(2025, 2, 7), EndDate = new(2025, 2, 9),
                Multiplier = 1.4m, IsRecurring = true, Priority = 15,
                Description = "First weekend of February tradition" },

        new() { Name = "Bo Sang Umbrella Festival", RuleType = Event,
                StartDate = new(2025, 1, 17), EndDate = new(2025, 1, 19),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Third weekend of January" },

        new() { Name = "Chiang Mai Design Week", RuleType = Event,
                StartDate = new(2025, 12, 6), EndDate = new(2025, 12, 14),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "Creative and design community event" },

        new() { Name = "Loy Krathong", RuleType = Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 6),
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Usually same time as Yi Peng" }
    };
}
```

---

## PRESET 5: Eastern Seaboard (Pattaya, Rayong, Koh Samet, Koh Chang)

**Target demographics**: Russian, Chinese, Indian, Middle Eastern, Bangkok weekenders
**Pattern**: Year-round with Russian winter peak, Bangkok weekend traffic

### Seasons

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| High Season | Nov 1 - Mar 31 | 1.5x | 10 | Russian/European winter |
| Shoulder | Apr 1 - May 31 | 1.1x | 8 | Transition |
| Green Season | Jun 1 - Oct 31 | 0.8x | 8 | Rainy period |

### Weekend Pattern (Bangkok proximity)

| Rule | Type | Multiplier | Priority | Notes |
|------|------|------------|----------|-------|
| Weekend Premium | DayOfWeek (Fri-Sun) | 1.2x | 12 | Bangkok day-trippers |

### Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Russian New Year | Jan 1-14 | 2.3x | 25 | HUGE in Pattaya |
| Christmas/New Year | Dec 20 - Jan 5 | 2.0x | 22 | Peak period |
| Chinese New Year | Varies | 1.8x | 20 | Growing Chinese tourism |
| Songkran | Apr 13-15 | 1.8x | 20 | Famous Pattaya parties |
| Indian Diwali | Oct/Nov | 1.4x | 16 | Growing Indian tourism |
| Eid al-Fitr | Varies | 1.4x | 16 | Middle Eastern visitors |

```csharp
public static class EasternPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === SEASONS ===
        new() { Name = "High Season", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 3, 31),
                Multiplier = 1.5m, IsRecurring = true, Priority = 10,
                Description = "Russian/European winter escape" },

        new() { Name = "Shoulder Season", RuleType = Season,
                StartDate = new(2025, 4, 1), EndDate = new(2025, 5, 31),
                Multiplier = 1.1m, IsRecurring = true, Priority = 8,
                Description = "Transition period" },

        new() { Name = "Green Season", RuleType = Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.8m, IsRecurring = true, Priority = 8,
                Description = "Rainy season discounts" },

        // === WEEKEND PATTERN ===
        new() { Name = "Bangkok Weekend Premium", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "Bangkok residents escape to beach" },

        // === MAJOR EVENTS ===
        new() { Name = "Russian New Year", RuleType = Event,
                StartDate = new(2026, 1, 1), EndDate = new(2026, 1, 14),
                Multiplier = 2.3m, IsRecurring = true, Priority = 25,
                Description = "Pattaya's biggest demographic" },

        new() { Name = "Christmas & New Year", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "Peak international period" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Growing Chinese tourist segment" },

        new() { Name = "Songkran Pattaya", RuleType = Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 19),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Extended Pattaya Songkran - famous parties" },

        new() { Name = "Chinese Golden Week", RuleType = Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Mainland Chinese national holiday" },

        // === GROWING DEMOGRAPHICS ===
        new() { Name = "Indian Diwali", RuleType = Event,
                StartDate = new(2025, 10, 20), EndDate = new(2025, 10, 24), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Growing Indian tourism - dates vary" },

        new() { Name = "Eid al-Fitr", RuleType = Event,
                StartDate = new(2025, 3, 30), EndDate = new(2025, 4, 3), // 2025 dates
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Middle Eastern visitors - dates vary (lunar)" },

        new() { Name = "Eid al-Adha", RuleType = Event,
                StartDate = new(2025, 6, 6), EndDate = new(2025, 6, 10), // 2025 dates
                Multiplier = 1.3m, IsRecurring = false, Priority = 15,
                Description = "Middle Eastern visitors - dates vary (lunar)" },

        // === LOCAL EVENTS ===
        new() { Name = "Pattaya Music Festival", RuleType = Event,
                StartDate = new(2025, 3, 14), EndDate = new(2025, 3, 16),
                Multiplier = 1.4m, IsRecurring = true, Priority = 15,
                Description = "Annual music festival" },

        new() { Name = "Pattaya Fireworks Festival", RuleType = Event,
                StartDate = new(2025, 11, 28), EndDate = new(2025, 11, 30),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "International fireworks competition" }
    };
}
```

---

## PRESET 6: Central/Bangkok

**Target demographics**: Business travelers, Chinese tour groups, diverse international
**Pattern**: Business week + weekend leisure traffic

### Seasons (Less pronounced)

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Cool Season | Nov 1 - Feb 28 | 1.3x | 8 | Best weather |
| Hot Season | Mar 1 - May 31 | 1.0x | 6 | Very hot |
| Rainy Season | Jun 1 - Oct 31 | 0.9x | 6 | Monsoon |

### Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Chinese New Year | Varies | 2.0x | 22 | Yaowarat celebrations |
| Songkran | Apr 13-15 | 1.8x | 20 | Silom/Khao San famous |
| NYE Countdown | Dec 30 - Jan 2 | 2.2x | 25 | CentralWorld countdown |
| Loy Krathong | Nov (lunar) | 1.4x | 16 | Chao Phraya celebrations |

```csharp
public static class CentralPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === SEASONS ===
        new() { Name = "Cool Season", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.3m, IsRecurring = true, Priority = 8,
                Description = "Best weather for sightseeing" },

        new() { Name = "Hot Season", RuleType = Season,
                StartDate = new(2025, 3, 1), EndDate = new(2025, 5, 31),
                Multiplier = 1.0m, IsRecurring = true, Priority = 6,
                Description = "Very hot - mall tourism" },

        new() { Name = "Rainy Season", RuleType = Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.9m, IsRecurring = true, Priority = 6,
                Description = "Monsoon - afternoon showers" },

        // === MAJOR EVENTS ===
        new() { Name = "NYE Countdown", RuleType = Event,
                StartDate = new(2025, 12, 30), EndDate = new(2026, 1, 2),
                Multiplier = 2.2m, IsRecurring = true, Priority = 25,
                Description = "CentralWorld NYE - massive event" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.0m, IsRecurring = false, Priority = 22,
                Description = "Yaowarat (Chinatown) celebrations" },

        new() { Name = "Songkran (Bangkok)", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Famous Silom and Khao San Road parties" },

        new() { Name = "Christmas Period", RuleType = Event,
                StartDate = new(2025, 12, 20), EndDate = new(2025, 12, 28),
                Multiplier = 1.5m, IsRecurring = true, Priority = 18,
                Description = "Western tourist shopping season" },

        new() { Name = "Chinese Golden Week", RuleType = Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Mainland Chinese tour groups" },

        new() { Name = "Loy Krathong", RuleType = Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 6),
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Chao Phraya riverside celebrations" },

        // === BUSINESS EVENTS ===
        new() { Name = "Bangkok Design Week", RuleType = Event,
                StartDate = new(2025, 2, 1), EndDate = new(2025, 2, 9),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "Creative industry event" },

        // === THAI HOLIDAYS ===
        new() { Name = "King's Birthday", RuleType = Event,
                StartDate = new(2025, 7, 28), EndDate = new(2025, 7, 28),
                Multiplier = 1.1m, IsRecurring = true, Priority = 10,
                Description = "National holiday" },

        new() { Name = "Queen's Birthday", RuleType = Event,
                StartDate = new(2025, 6, 3), EndDate = new(2025, 6, 3),
                Multiplier = 1.1m, IsRecurring = true, Priority = 10,
                Description = "National holiday" }
    };
}
```

---

## PRESET 7: Western Thailand (Hua Hin, Kanchanaburi, Cha-am)

**Target demographics**: Bangkok residents, European retirees, domestic tourists
**Pattern**: Heavy weekend traffic from Bangkok

### Weekend-Dominant Pattern

| Rule | Type | Multiplier | Priority | Notes |
|------|------|------------|----------|-------|
| Weekend Premium | DayOfWeek (Fri-Sun) | 1.35x | 12 | Bangkok escape |
| Long Weekend | DayOfWeek (Thu-Sun) | 1.45x | 14 | Extended weekends |

### Seasons

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Cool Season | Nov 1 - Feb 28 | 1.3x | 8 | Best beach weather |
| Rainy Season | Jun 1 - Oct 31 | 0.85x | 6 | Quieter period |

### Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| River Kwai Week | Late Nov - Early Dec | 1.5x | 18 | Kanchanaburi specialty |
| Hua Hin Jazz Festival | June | 1.4x | 16 | Annual music event |
| Royal Events | Various | 1.3x | 15 | Royal family connections |

```csharp
public static class WesternPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === WEEKEND-DOMINANT PATTERN ===
        new() { Name = "Weekend Premium", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                Multiplier = 1.35m, IsRecurring = true, Priority = 12,
                Description = "Bangkok residents weekend escape" },

        new() { Name = "Thursday Start", RuleType = DayOfWeek,
                ApplicableDays = [DayOfWeek.Thursday],
                Multiplier = 1.2m, IsRecurring = true, Priority = 10,
                Description = "Long weekend starters" },

        // === SEASONS ===
        new() { Name = "Cool Season", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.3m, IsRecurring = true, Priority = 8,
                Description = "Best beach weather" },

        new() { Name = "Rainy Season", RuleType = Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.85m, IsRecurring = true, Priority = 6,
                Description = "Quieter monsoon period" },

        // === LOCAL EVENTS ===
        new() { Name = "River Kwai Bridge Week", RuleType = Event,
                StartDate = new(2025, 11, 28), EndDate = new(2025, 12, 7),
                Multiplier = 1.5m, IsRecurring = true, Priority = 18,
                Description = "Kanchanaburi light & sound show" },

        new() { Name = "Hua Hin Jazz Festival", RuleType = Event,
                StartDate = new(2025, 6, 13), EndDate = new(2025, 6, 15),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Annual jazz by the beach" },

        // === THAI HOLIDAYS (Weekend effect compounds) ===
        new() { Name = "Songkran", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 1.6m, IsRecurring = true, Priority = 18,
                Description = "Bangkok exodus to beaches" },

        new() { Name = "King's Birthday Weekend", RuleType = Event,
                StartDate = new(2025, 7, 26), EndDate = new(2025, 7, 28),
                Multiplier = 1.4m, IsRecurring = true, Priority = 16,
                Description = "Long weekend + royal significance in Hua Hin" },

        new() { Name = "New Year", RuleType = Event,
                StartDate = new(2025, 12, 28), EndDate = new(2026, 1, 3),
                Multiplier = 1.8m, IsRecurring = true, Priority = 20,
                Description = "Bangkok families beach escape" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.5m, IsRecurring = false, Priority = 18,
                Description = "Thai-Chinese families" }
    };
}
```

---

## PRESET 8: Isaan/Northeast (Udon Thani, Khon Kaen, Korat)

**Target demographics**: Domestic Thai, expats visiting family, Laotian visitors
**Pattern**: Thai holiday spikes, family reunion periods

### Minimal Seasonality - Event-Driven

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Songkran | Apr 12-17 | 2.5x | 28 | MASSIVE family reunions |
| New Year | Dec 28 - Jan 3 | 2.0x | 22 | Family gatherings |
| Cool Season | Nov-Feb | 1.1x | 6 | Slightly better weather |

### Isaan-Specific Events

| Rule | Dates | Multiplier | Priority | Notes |
|------|-------|------------|----------|-------|
| Phi Ta Khon Festival | Jun/Jul | 1.6x | 18 | Dan Sai, Loei |
| Candle Festival | Jul (Buddhist Lent) | 1.5x | 16 | Ubon Ratchathani |
| Rocket Festival | May | 1.4x | 15 | Yasothon, various |
| Khao Phansa | Jul (full moon) | 1.3x | 14 | Buddhist Lent start |

```csharp
public static class IsaanPreset
{
    public static List<PricingRule> GetRules(int shopId) => new()
    {
        // === MINIMAL SEASONS ===
        new() { Name = "Cool Season", RuleType = Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.1m, IsRecurring = true, Priority = 6,
                Description = "Slightly better weather for travel" },

        // === MAJOR FAMILY EVENTS ===
        new() { Name = "Songkran (Isaan)", RuleType = Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 17),
                Multiplier = 2.5m, IsRecurring = true, Priority = 28,
                Description = "MASSIVE - Bangkok workers return home" },

        new() { Name = "New Year Period", RuleType = Event,
                StartDate = new(2025, 12, 28), EndDate = new(2026, 1, 3),
                Multiplier = 2.0m, IsRecurring = true, Priority = 22,
                Description = "Family reunion period" },

        new() { Name = "Chinese New Year", RuleType = Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Thai-Chinese families" },

        // === ISAAN-SPECIFIC FESTIVALS ===
        new() { Name = "Phi Ta Khon Festival", RuleType = Event,
                StartDate = new(2025, 6, 27), EndDate = new(2025, 6, 29), // Varies yearly
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Ghost Festival in Dan Sai, Loei - dates vary" },

        new() { Name = "Candle Festival (Ubon)", RuleType = Event,
                StartDate = new(2025, 7, 10), EndDate = new(2025, 7, 13), // Asanha Bucha week
                Multiplier = 1.5m, IsRecurring = false, Priority = 16,
                Description = "Ubon Ratchathani Buddhist Lent candle parade" },

        new() { Name = "Bun Bang Fai (Rocket Festival)", RuleType = Event,
                StartDate = new(2025, 5, 9), EndDate = new(2025, 5, 11), // Varies
                Multiplier = 1.4m, IsRecurring = false, Priority = 15,
                Description = "Yasothon rocket festival - dates vary" },

        new() { Name = "Khao Phansa", RuleType = Event,
                StartDate = new(2025, 7, 11), EndDate = new(2025, 7, 13), // 2025 dates
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Buddhist Lent start - merit making" },

        // === THAI HOLIDAYS ===
        new() { Name = "King's Birthday Weekend", RuleType = Event,
                StartDate = new(2025, 7, 26), EndDate = new(2025, 7, 28),
                Multiplier = 1.3m, IsRecurring = true, Priority = 14,
                Description = "Long weekend travel" },

        new() { Name = "Queen's Birthday Weekend", RuleType = Event,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 6, 3),
                Multiplier = 1.2m, IsRecurring = true, Priority = 12,
                Description = "Long weekend" },

        new() { Name = "Makha Bucha", RuleType = Event,
                StartDate = new(2025, 2, 12), EndDate = new(2025, 2, 12), // 2025 dates
                Multiplier = 1.2m, IsRecurring = false, Priority = 12,
                Description = "Buddhist holiday - dates vary (lunar)" }
    };
}
```

---

## UI: Apply Preset Dialog (Updated)

```
+--------------------------------------------------+
| Apply Regional Preset                             |
+--------------------------------------------------+
| Select your region to get started with           |
| recommended pricing rules:                        |
|                                                  |
| ( ) Start blank - create rules from scratch      |
|                                                  |
| BEACH DESTINATIONS                               |
| ( ) Andaman Coast - Phuket, Krabi, Phang Nga    |
|     European & Russian tourists, Nov-Mar peak    |
| ( ) Gulf Coast - Koh Samui, Koh Phangan         |
|     Party scene, opposite monsoon pattern        |
| ( ) Eastern Seaboard - Pattaya, Rayong          |
|     Russian peak, Bangkok weekenders             |
|                                                  |
| BORDER & REGIONAL                                |
| ( ) Southern Border - Hat Yai, Songkhla         |
|     Malaysian weekenders, school holidays        |
| ( ) Northern - Chiang Mai, Chiang Rai           |
|     Cool season, lantern festivals               |
| ( ) Western - Hua Hin, Kanchanaburi             |
|     Bangkok weekend escapes                      |
| ( ) Isaan - Udon Thani, Khon Kaen               |
|     Domestic travel, Songkran exodus             |
|                                                  |
| METROPOLITAN                                     |
| ( ) Central - Bangkok area                       |
|     Business + leisure, Chinese tourists         |
|                                                  |
| ℹ️ Presets include seasons and major events.     |
|    Customize rules after applying.               |
|                                                  |
|                    [Cancel] [Apply Preset]       |
+--------------------------------------------------+
```

## Implementation Phases

### Phase 1: Core (MVP)
1. Add setting keys to `SettingKeys.cs` (DynamicPricing.Enabled, ShowOnInvoice, AppliedPreset)
2. Create `PricingRule` entity and `PricingRuleType` enum
3. Create database table with JSON column and computed columns
4. Implement `DynamicPricingService` with feature toggle check
5. Add feature toggle switch to Organization Settings page
6. Integrate with `RentalPricingService` (apply multiplier when enabled)
7. Build Pricing Rules CRUD UI

### Phase 2: Regional Presets
1. Implement `RegionalPresetService` with all 8 regional templates
2. Build Apply Preset dialog with region selector
3. First-time setup flow (prompt preset when enabling with no rules)
4. Add day-of-week rule support
5. Add recurring rule support (yearly events)

### Phase 3: Enhanced UI
1. Build calendar visualization (`PricingCalendar.razor`)
2. Add vehicle type/ID filtering
3. Show pricing info in check-in wizard
4. Show multiplier badge on rental confirmation
5. Optional: Show multiplier on invoice (controlled by ShowOnInvoice setting)

### Phase 4: Advanced (Future)
1. Utilization-based rules (fleet % booked)
2. Lead-time rules (early booking discounts)
3. Bulk discount tiers (longer = cheaper)
4. Competitor rate integration
5. A/B testing framework

## Rental Entity Update

Add fields to capture applied pricing:

```csharp
// In Rental.cs
public string? AppliedPricingRule { get; set; }
public decimal PricingMultiplier { get; set; } = 1.0m;
public decimal BaseRate { get; set; }  // Original rate before multiplier
```

## Localization Keys

```
// Feature Toggle
EnableDynamicPricing = "Enable Dynamic Pricing"
DynamicPricingEnabledInfo = "Dynamic pricing adjusts rates based on seasons and events. Configure rules to set multipliers."
DynamicPricingDisabled = "Dynamic pricing is disabled. Base rates will be used."
ManagePricingRules = "Manage Pricing Rules"
ShowMultiplierOnInvoice = "Show pricing adjustment on invoice"

// Pricing Rules
PricingRules = "Pricing Rules"
AddPricingRule = "Add Pricing Rule"
EditPricingRule = "Edit Pricing Rule"
DeletePricingRule = "Delete Pricing Rule"
NoPricingRules = "No pricing rules configured. Apply a regional preset or create rules manually."
Season = "Season"
Event = "Event"
DayOfWeek = "Day of Week"
Custom = "Custom"
Multiplier = "Multiplier"
MultiplierDisplay = "{0}x ({1}%)"  // e.g., "1.5x (+50%)"
DateRange = "Date Range"
RepeatYearly = "Repeat yearly"
AppliesTo = "Applies To"
AllVehicles = "All Vehicles"
SpecificTypes = "Specific Types"
Priority = "Priority"
PriorityHelp = "Higher priority rules override lower ones when dates overlap"
HighSeason = "High Season"
LowSeason = "Low Season"
PricingCalendar = "Pricing Calendar"
WeekendPremium = "Weekend Premium"
IsActive = "Active"
Description = "Description"

// Regional Presets
ApplyRegionalPreset = "Apply Regional Preset"
StartBlank = "Start blank - I'll create my own"
AndamanCoast = "Andaman Coast (Phuket, Krabi)"
GulfCoast = "Gulf Coast (Koh Samui, Koh Phangan)"
SouthernBorder = "Southern Border (Hat Yai, Songkhla)"
Northern = "Northern (Chiang Mai)"
Eastern = "Eastern (Pattaya)"
Central = "Central (Bangkok)"
Western = "Western (Hua Hin, Kanchanaburi)"
Isaan = "Isaan (Udon Thani, Khon Kaen)"
PresetAppliedSuccess = "Regional preset applied with {0} rules. You can now customize them."
PresetDescription = "Select a preset matching your location to get started with recommended pricing rules."
BeachDestinations = "Beach Destinations"
BorderRegional = "Border & Regional"
Metropolitan = "Metropolitan"
RulesCount = "{0} rules"

// Lunar Calendar Warnings
LunarEventWarning = "This event has lunar dates that change yearly. Update before each year."
UpdateLunarDates = "Update Lunar Dates"
LunarEventsNeedUpdate = "{0} events have lunar dates that may need updating for {1}."
MarkAsUpdated = "Mark as Updated"

// Pricing Display (Check-in wizard, invoices)
DynamicPricingApplied = "Seasonal pricing applied"
BaseRate = "Base rate"
AdjustedRate = "Adjusted rate"
PricingRule = "Pricing rule"
NoAdjustment = "No adjustment (base rate)"
```

## Testing Scenarios

1. **No active rules**: Returns base rate unchanged
2. **Single season rule**: Applies multiplier correctly
3. **Overlapping rules**: Higher priority wins
4. **Recurring events**: Matches across years
5. **Vehicle type filter**: Only applies to matching types
6. **Min/Max bounds**: Respects floor/ceiling prices
7. **Multi-day rental**: Uses start date for rule matching
8. **Day-of-week rule**: Weekend premium applies only on Fri/Sat/Sun
9. **Day-of-week + Season overlap**: Higher priority wins (weekend during low season)
10. **Regional preset applied**: Creates expected rules for region
11. **Empty preset (blank start)**: No rules created

## Notes

### Design Principles
- **No system-wide defaults** - each shop defines their own seasons/events
- Regional presets are optional starting points, fully customizable
- Multiplier applies to vehicle rate only (not insurance/accessories)
- Rate captured in Rental entity for historical accuracy
- Priority system allows events to override seasons

### Regional Flexibility
- Hat Yai shops can ignore "high season" entirely and focus on Malaysian holidays
- Koh Samui shops can set opposite monsoon pattern from Phuket
- Pattaya can layer Russian + Chinese + Indian demographics
- Isaan focuses on domestic Songkran exodus rather than international tourists

### Lunar Calendar Events
Many Thai events follow lunar calendars and dates shift yearly:
- **Chinese New Year** - Lunar calendar (Jan/Feb)
- **Hari Raya Aidilfitri/Haji** - Islamic lunar calendar (shifts ~11 days/year)
- **Yi Peng / Loy Krathong** - Thai lunar calendar (November full moon)
- **Vegetarian Festival** - Chinese lunar calendar (9th month)
- **Full Moon Party** - Monthly lunar cycle
- **Buddhist holidays** - Thai lunar calendar

**Implementation**: Events with `IsRecurring = false` must be manually updated each year. UI should warn shop owners to update lunar dates before each year begins.

### Rule Count by Preset
| Preset | Rules | Seasons | Events | Day-of-Week | Lunar Events |
|--------|-------|---------|--------|-------------|--------------|
| **Andaman Coast (Phuket)** | **58** | **6** | **52** | **0** | **15** |
| Gulf Coast | 14 | 5 | 9 | 0 | 5 |
| **Southern Border (Hat Yai)** | **32** | **0** | **29** | **3** | **12** |
| Northern | 14 | 4 | 10 | 0 | 5 |
| Eastern | 14 | 3 | 10 | 1 | 4 |
| Central | 11 | 3 | 8 | 0 | 3 |
| Western | 9 | 2 | 5 | 2 | 2 |
| Isaan | 10 | 1 | 9 | 0 | 4 |

**Andaman Coast (Phuket)** has the most rules because it serves multiple demographics:
- Russian tourists (Orthodox calendar + Russian holidays)
- European tourists (UK, German, French, Italian, Scandinavian school calendars)
- Australian tourists (Southern hemisphere summer)
- Chinese tourists (Lunar calendar + Golden Weeks)
- Indian tourists (Hindu festivals + wedding season)
- Middle Eastern tourists (Islamic calendar + Gulf summer)
- Phuket-specific events (Vegetarian Festival, Bike Week, Regatta)

**Southern Border (Hat Yai)** follows with 32 rules because Malaysian visitors follow:
- Multiple religious calendars (Islamic, Chinese, Hindu, Buddhist)
- Malaysian school calendar (4 breaks/year)
- Malaysian + Singaporean public holidays
- Northern Malaysian state-specific holidays
- Base weekend patterns (no seasonal variation)
