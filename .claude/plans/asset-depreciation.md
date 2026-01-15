# Asset Depreciation Feature - Implementation Plan

## Overview

Financial modeling feature for capital investment tracking in the motorbike rental system. Tracks vehicle depreciation, operational expenses, loan financing, and ROI calculations.

## User Requirements (Confirmed)

1. **Separate Asset Entity** - Links to Vehicle, independent tracking
2. **Hybrid depreciation tracking** - Calculate on demand + manual overrides
3. **Full expense tracking** - Maintenance, Insurance, Financing, Accidents, Registration, Fuel
4. **Full loan tracking** - Principal, interest, monthly payments, amortization

## Depreciation Methods

| Method | Formula | Use Case |
|--------|---------|----------|
| Day Out of Door | `AcquisitionCost * DayOutOfDoorPercent` | Immediate depreciation on first rental |
| Straight Line | `(Cost - Residual) / UsefulLifeMonths` | Equal monthly amounts |
| Declining Balance | `BookValue * (AnnualRate/12)` | Percentage of book value |
| Custom | User-defined monthly schedule | Special situations |
| Hybrid variants | Day Out + Straight Line/Declining | Combined methods |

---

## Phase 1: Entity Classes

### 1.1 Asset Entity
**File:** `src/MotoRent.Domain/Entities/Asset.cs`

```csharp
public class Asset : Entity
{
    public int AssetId { get; set; }

    // Vehicle reference
    public int VehicleId { get; set; }
    public string? VehicleName { get; set; }  // Denormalized
    public string? LicensePlate { get; set; } // Denormalized

    // Acquisition
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public DateTimeOffset? FirstRentalDate { get; set; }
    public string? AcquisitionRef { get; set; }
    public string? VendorName { get; set; }
    public bool IsPreExisting { get; set; }
    public decimal? InitialBookValue { get; set; }
    public DateTimeOffset? SystemEntryDate { get; set; }

    // Depreciation settings
    public DepreciationMethod DepreciationMethod { get; set; }
    public int UsefulLifeMonths { get; set; } = 60;
    public decimal ResidualValue { get; set; }
    public decimal? DayOutOfDoorPercent { get; set; }
    public decimal? DecliningBalanceRate { get; set; }
    public List<CustomDepreciationEntry>? CustomSchedule { get; set; }

    // Current values
    public decimal CurrentBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTimeOffset? LastDepreciationDate { get; set; }

    // Status
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public DateTimeOffset? DisposalDate { get; set; }
    public decimal? DisposalAmount { get; set; }
    public decimal? DisposalGainLoss { get; set; }

    // Loan reference
    public int? AssetLoanId { get; set; }
}
```

### 1.2 DepreciationEntry Entity
**File:** `src/MotoRent.Domain/Entities/DepreciationEntry.cs`

For manual overrides and period-end records.

### 1.3 AssetExpense Entity
**File:** `src/MotoRent.Domain/Entities/AssetExpense.cs`

Categorized expense tracking (Maintenance, Insurance, Financing, Accident, Registration, Consumables).

### 1.4 AssetLoan + AssetLoanPayment Entities
**Files:** `src/MotoRent.Domain/Entities/AssetLoan.cs`, `AssetLoanPayment.cs`

Full loan amortization with interest calculations.

### 1.5 Enumerations
**File:** `src/MotoRent.Domain/Entities/AssetEnums.cs`

- `DepreciationMethod`: DayOutOfDoor, StraightLine, DecliningBalance, Custom, HybridDayOutThenStraightLine
- `AssetStatus`: Active, Disposed, WriteOff
- `AssetExpenseCategory`: Maintenance, Insurance, Financing, Accident, Registration, Consumables, Fuel, Other
- `LoanStatus`: Active, PaidOff, Defaulted
- `LoanPaymentStatus`: Pending, Paid, Late, Missed

---

## Phase 2: SQL Tables

### 2.1 Asset Table
**File:** `database/tables/MotoRent.Asset.sql`

```sql
CREATE TABLE [<schema>].[Asset]
(
    [AssetId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [AcquisitionDate] AS CAST(JSON_VALUE([Json], '$.AcquisitionDate') AS DATE),
    [AcquisitionCost] AS CAST(JSON_VALUE([Json], '$.AcquisitionCost') AS DECIMAL(12,2)),
    [DepreciationMethod] AS CAST(JSON_VALUE([Json], '$.DepreciationMethod') AS NVARCHAR(30)),
    [CurrentBookValue] AS CAST(JSON_VALUE([Json], '$.CurrentBookValue') AS DECIMAL(12,2)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
CREATE UNIQUE INDEX IX_Asset_VehicleId ON [<schema>].[Asset]([VehicleId])
CREATE INDEX IX_Asset_Status ON [<schema>].[Asset]([Status])
```

### 2.2 Additional Tables
- `MotoRent.DepreciationEntry.sql`
- `MotoRent.AssetExpense.sql`
- `MotoRent.AssetLoan.sql`
- `MotoRent.AssetLoanPayment.sql`

---

## Phase 3: Services

### 3.1 DepreciationCalculator
**File:** `src/MotoRent.Services/DepreciationCalculator.cs`

Core calculation logic for all depreciation methods.

```csharp
public class DepreciationCalculator
{
    public DepreciationCalculation Calculate(Asset asset, DateTimeOffset periodStart, DateTimeOffset periodEnd);
    public List<DepreciationProjection> ProjectDepreciation(Asset asset, int monthsAhead);
}
```

### 3.2 AssetService
**File:** `src/MotoRent.Services/AssetService.cs`

- CRUD operations
- `RecordDepreciationAsync()` - Calculate and record with optional override
- `RecordFirstRentalAsync()` - Trigger Day Out of Door if applicable
- `DisposeAssetAsync()` / `WriteOffAssetAsync()`
- `AddRevenueFromRentalAsync()` - Called from rental checkout
- `GetSummaryAsync()` / `GetVehicleProfitabilityAsync()`

### 3.3 AssetExpenseService
**File:** `src/MotoRent.Services/AssetExpenseService.cs`

- Expense CRUD with automatic asset total updates
- `CreateFromMaintenanceAsync()` - Integration with maintenance module

### 3.4 AssetLoanService
**File:** `src/MotoRent.Services/AssetLoanService.cs`

- Loan CRUD
- `CalculateMonthlyPayment()` - Standard amortization formula
- `GenerateAmortizationSchedule()` - Full payment schedule
- `RecordPaymentAsync()` - Record payment and create interest expense

---

## Phase 4: Asset Dashboard (Financial Command Center)

**File:** `src/MotoRent.Client/Pages/Finance/AssetDashboard.razor`
**Route:** `/finance/asset-dashboard`

### 4.0.1 Dashboard Overview

A visually stunning financial dashboard that provides at-a-glance insight into fleet asset health, ROI performance, and financial obligations.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  HEADER: Asset Financial Overview                              [+ Add Asset]│
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │
│  │ TOTAL    │ │ BOOK     │ │ ACCUM    │ │ NET      │ │ FLEET    │          │
│  │ INVESTED │ │ VALUE    │ │ DEPREC   │ │ PROFIT   │ │ ROI %    │          │
│  │ ฿2.5M    │ │ ฿1.8M    │ │ ฿700K    │ │ ฿450K    │ │ 18.2%    │          │
│  │ ▲12 vehs │ │ 72% orig │ │ 28% used │ │ ▲8.5%    │ │ ▲2.1%    │          │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘          │
│                                                                             │
│  ┌─────────────────────────────────┐ ┌─────────────────────────────────┐   │
│  │  DEPRECIATION TREND (12 mo)    │ │  EXPENSE BREAKDOWN              │   │
│  │  ████████████████              │ │  ████ Maintenance  45%          │   │
│  │  ███████████████               │ │  ███  Financing    28%          │   │
│  │  ██████████████                │ │  ██   Insurance    15%          │   │
│  │  Line chart showing monthly    │ │  █    Accidents     8%          │   │
│  │  depreciation & book value     │ │  ▪    Other         4%          │   │
│  └─────────────────────────────────┘ └─────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────┐ ┌─────────────────────────────────┐   │
│  │  🏆 TOP PERFORMERS              │ │  ⚠️ ATTENTION NEEDED             │   │
│  │  1. Honda PCX 160   ROI: 32%   │ │  • 3 assets need depreciation   │   │
│  │  2. Yamaha NMAX     ROI: 28%   │ │  • 2 loan payments overdue      │   │
│  │  3. Honda Click     ROI: 25%   │ │  • 1 asset below threshold      │   │
│  └─────────────────────────────────┘ └─────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  ⚡ QUICK ACTIONS                                                    │   │
│  │  [Run Monthly Depreciation] [Record Expense] [View All Assets]      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.0.2 Dashboard Razor Component

```razor
@page "/finance/asset-dashboard"
@rendermode InteractiveServer
@inherits LocalizedComponentBase<AssetDashboard>
@using Microsoft.AspNetCore.Authorization
@using MotoRent.Domain.Core
@attribute [Authorize(Policy = "RequireTenantManager")]
@inject AssetService AssetService
@inject AssetExpenseService ExpenseService
@inject AssetLoanService LoanService

<PageTitle>@Localizer["PageTitle"]</PageTitle>

<!-- Page Header with Gradient Icon -->
<div class="mr-page-header">
    <div class="container-xl">
        <nav class="mr-breadcrumb">
            <span class="mr-breadcrumb-item"><a href="/">@CommonLocalizer["Home"]</a></span>
            <i class="ti ti-chevron-right mr-breadcrumb-separator"></i>
            <span class="mr-breadcrumb-item"><a href="/finance">@Localizer["Finance"]</a></span>
            <i class="ti ti-chevron-right mr-breadcrumb-separator"></i>
            <span class="mr-breadcrumb-item active">@Localizer["AssetDashboard"]</span>
        </nav>
        <div class="d-flex justify-content-between align-items-center pb-3">
            <div class="d-flex align-items-center gap-3">
                <div class="mr-header-icon mr-header-icon-finance">
                    <i class="ti ti-chart-pie"></i>
                </div>
                <div>
                    <h1 class="mr-page-title">@Localizer["HeaderTitle"]</h1>
                    <p class="mr-page-subtitle mb-0">@Localizer["HeaderSubtitle"]</p>
                </div>
            </div>
            <div class="btn-list">
                <a href="/finance/assets/new" class="mr-btn-primary-action">
                    <i class="ti ti-plus"></i>
                    @Localizer["AddAsset"]
                </a>
            </div>
        </div>
    </div>
</div>

<div class="container-xl py-4">
    <!-- ═══════════════════════════════════════════════════════════════
         SECTION 1: Financial KPI Summary Cards (5 cards)
         ═══════════════════════════════════════════════════════════════ -->
    <div class="mr-finance-kpi-grid mr-animate-in">
        <!-- Total Invested -->
        <div class="mr-kpi-card mr-kpi-invested">
            <div class="mr-kpi-icon">
                <i class="ti ti-building-bank"></i>
            </div>
            <div class="mr-kpi-content">
                <span class="mr-kpi-label">@Localizer["TotalInvested"]</span>
                <span class="mr-kpi-value">@m_summary.TotalAcquisitionCost.ToString("N0")</span>
                <span class="mr-kpi-subtext">
                    <i class="ti ti-motorbike"></i> @m_summary.ActiveAssets @Localizer["Vehicles"]
                </span>
            </div>
            <div class="mr-kpi-trend neutral">
                <span class="mr-kpi-trend-icon"><i class="ti ti-wallet"></i></span>
            </div>
        </div>

        <!-- Current Book Value -->
        <div class="mr-kpi-card mr-kpi-bookvalue">
            <div class="mr-kpi-icon">
                <i class="ti ti-receipt"></i>
            </div>
            <div class="mr-kpi-content">
                <span class="mr-kpi-label">@Localizer["CurrentBookValue"]</span>
                <span class="mr-kpi-value">@m_summary.TotalCurrentBookValue.ToString("N0")</span>
                <span class="mr-kpi-subtext">
                    @((m_summary.TotalCurrentBookValue / Math.Max(1, m_summary.TotalAcquisitionCost) * 100).ToString("N0"))% @Localizer["OfOriginal"]
                </span>
            </div>
            <div class="mr-kpi-gauge">
                <svg viewBox="0 0 36 36" class="mr-gauge-svg">
                    <path class="mr-gauge-bg" d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"/>
                    <path class="mr-gauge-fill" stroke-dasharray="@m_bookValuePercent, 100"
                          d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"/>
                </svg>
            </div>
        </div>

        <!-- Accumulated Depreciation -->
        <div class="mr-kpi-card mr-kpi-depreciation">
            <div class="mr-kpi-icon">
                <i class="ti ti-trending-down"></i>
            </div>
            <div class="mr-kpi-content">
                <span class="mr-kpi-label">@Localizer["AccumulatedDepreciation"]</span>
                <span class="mr-kpi-value mr-kpi-warning">@m_summary.TotalAccumulatedDepreciation.ToString("N0")</span>
                <span class="mr-kpi-subtext">
                    @((m_summary.TotalAccumulatedDepreciation / Math.Max(1, m_summary.TotalAcquisitionCost) * 100).ToString("N0"))% @Localizer["Depreciated"]
                </span>
            </div>
            <div class="mr-kpi-progress">
                <div class="mr-kpi-progress-bar" style="width: @m_depreciationPercent%"></div>
            </div>
        </div>

        <!-- Net Profit/Loss -->
        <div class="mr-kpi-card @(m_summary.NetProfitLoss >= 0 ? "mr-kpi-profit" : "mr-kpi-loss")">
            <div class="mr-kpi-icon">
                <i class="ti ti-@(m_summary.NetProfitLoss >= 0 ? "trending-up" : "trending-down")"></i>
            </div>
            <div class="mr-kpi-content">
                <span class="mr-kpi-label">@Localizer["NetProfitLoss"]</span>
                <span class="mr-kpi-value @(m_summary.NetProfitLoss >= 0 ? "mr-kpi-success" : "mr-kpi-danger")">
                    @(m_summary.NetProfitLoss >= 0 ? "+" : "")@m_summary.NetProfitLoss.ToString("N0")
                </span>
                <span class="mr-kpi-subtext">
                    @Localizer["Revenue"]: @m_summary.TotalRevenue.ToString("N0")
                </span>
            </div>
            <div class="mr-kpi-trend @(m_summary.NetProfitLoss >= 0 ? "up" : "down")">
                <i class="ti ti-arrow-@(m_summary.NetProfitLoss >= 0 ? "up" : "down")-right"></i>
            </div>
        </div>

        <!-- Fleet ROI -->
        <div class="mr-kpi-card mr-kpi-roi">
            <div class="mr-kpi-icon">
                <i class="ti ti-percentage"></i>
            </div>
            <div class="mr-kpi-content">
                <span class="mr-kpi-label">@Localizer["FleetROI"]</span>
                <span class="mr-kpi-value mr-kpi-highlight">@m_fleetROI.ToString("N1")%</span>
                <span class="mr-kpi-subtext">
                    <i class="ti ti-chart-line"></i> @Localizer["AnnualizedReturn"]
                </span>
            </div>
            <div class="mr-kpi-sparkline">
                @* Mini sparkline chart placeholder *@
                <svg viewBox="0 0 100 30" class="mr-sparkline">
                    <polyline points="@m_roiSparklinePoints" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>
            </div>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════
         SECTION 2: Charts Row (Depreciation Trend + Expense Breakdown)
         ═══════════════════════════════════════════════════════════════ -->
    <div class="row row-deck row-cards mt-4">
        <!-- Depreciation Trend Chart -->
        <div class="col-lg-7 mr-animate-in mr-animate-delay-1">
            <div class="mr-chart-card">
                <div class="mr-chart-header">
                    <div class="mr-chart-title">
                        <i class="ti ti-chart-area"></i>
                        <h3>@Localizer["DepreciationTrend"]</h3>
                    </div>
                    <div class="mr-chart-legend">
                        <span class="mr-legend-item mr-legend-bookvalue">
                            <span class="mr-legend-dot"></span> @Localizer["BookValue"]
                        </span>
                        <span class="mr-legend-item mr-legend-depreciation">
                            <span class="mr-legend-dot"></span> @Localizer["MonthlyDeprec"]
                        </span>
                    </div>
                </div>
                <div class="mr-chart-body">
                    <div class="mr-chart-container" id="depreciation-trend-chart">
                        @* Chart.js or ApexCharts renders here *@
                        <canvas id="depreciationChart"></canvas>
                    </div>
                </div>
                <div class="mr-chart-footer">
                    <span class="mr-chart-period">@Localizer["Last12Months"]</span>
                    <a href="/finance/reports/depreciation" class="mr-chart-link">
                        @Localizer["ViewFullReport"] <i class="ti ti-arrow-right"></i>
                    </a>
                </div>
            </div>
        </div>

        <!-- Expense Breakdown Donut -->
        <div class="col-lg-5 mr-animate-in mr-animate-delay-1">
            <div class="mr-chart-card">
                <div class="mr-chart-header">
                    <div class="mr-chart-title">
                        <i class="ti ti-chart-donut"></i>
                        <h3>@Localizer["ExpenseBreakdown"]</h3>
                    </div>
                </div>
                <div class="mr-chart-body">
                    <div class="mr-donut-chart-container">
                        <div class="mr-donut-chart" id="expense-donut-chart">
                            <canvas id="expenseDonutChart"></canvas>
                        </div>
                        <div class="mr-donut-legend">
                            @foreach (var expense in m_expensesByCategory.Take(5))
                            {
                                <div class="mr-donut-legend-item">
                                    <span class="mr-donut-color" style="background: @GetCategoryColor(expense.Key)"></span>
                                    <span class="mr-donut-label">@Localizer[expense.Key.ToString()]</span>
                                    <span class="mr-donut-value">@expense.Value.ToString("N0")</span>
                                    <span class="mr-donut-percent">@((expense.Value / Math.Max(1, m_totalExpenses) * 100).ToString("N0"))%</span>
                                </div>
                            }
                        </div>
                    </div>
                </div>
                <div class="mr-chart-footer">
                    <span class="mr-expense-total">@Localizer["Total"]: ฿@m_totalExpenses.ToString("N0")</span>
                </div>
            </div>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════
         SECTION 3: Performance Tables (Top Performers + Attention Needed)
         ═══════════════════════════════════════════════════════════════ -->
    <div class="row row-deck row-cards mt-4">
        <!-- Top Performers -->
        <div class="col-lg-6 mr-animate-in mr-animate-delay-2">
            <div class="mr-performance-card mr-top-performers">
                <div class="mr-performance-header">
                    <div class="mr-performance-icon success">
                        <i class="ti ti-trophy"></i>
                    </div>
                    <h3>@Localizer["TopPerformers"]</h3>
                    <span class="mr-performance-badge">@Localizer["HighestROI"]</span>
                </div>
                <div class="mr-performance-body">
                    @if (m_topPerformers.Any())
                    {
                        <div class="mr-performer-list">
                            @{ var rank = 1; }
                            @foreach (var asset in m_topPerformers)
                            {
                                <a href="/finance/assets/@asset.AssetId" class="mr-performer-item">
                                    <div class="mr-performer-rank @GetRankClass(rank)">@rank</div>
                                    <div class="mr-performer-info">
                                        <span class="mr-performer-name">@asset.VehicleName</span>
                                        <span class="mr-performer-plate">@asset.LicensePlate</span>
                                    </div>
                                    <div class="mr-performer-stats">
                                        <span class="mr-performer-roi success">
                                            <i class="ti ti-trending-up"></i>
                                            @asset.ROIPercent.ToString("N1")%
                                        </span>
                                        <span class="mr-performer-revenue">
                                            ฿@asset.TotalRevenue.ToString("N0")
                                        </span>
                                    </div>
                                </a>
                                rank++;
                            }
                        </div>
                    }
                    else
                    {
                        <div class="mr-empty-state">
                            <i class="ti ti-chart-bar-off"></i>
                            <span>@Localizer["NoPerformanceData"]</span>
                        </div>
                    }
                </div>
                <div class="mr-performance-footer">
                    <a href="/finance/reports/profitability" class="mr-btn-action view">
                        @Localizer["ViewAllPerformance"] <i class="ti ti-arrow-right"></i>
                    </a>
                </div>
            </div>
        </div>

        <!-- Attention Needed / Alerts -->
        <div class="col-lg-6 mr-animate-in mr-animate-delay-2">
            <div class="mr-performance-card mr-attention-needed">
                <div class="mr-performance-header">
                    <div class="mr-performance-icon warning">
                        <i class="ti ti-alert-triangle"></i>
                    </div>
                    <h3>@Localizer["AttentionNeeded"]</h3>
                    @if (m_totalAlerts > 0)
                    {
                        <span class="mr-alert-badge">@m_totalAlerts</span>
                    }
                </div>
                <div class="mr-performance-body">
                    <div class="mr-alert-list">
                        @if (m_assetsNeedingDepreciation > 0)
                        {
                            <a href="/finance/depreciation" class="mr-alert-item depreciation">
                                <div class="mr-alert-icon">
                                    <i class="ti ti-calculator"></i>
                                </div>
                                <div class="mr-alert-content">
                                    <span class="mr-alert-title">@Localizer["DepreciationDue"]</span>
                                    <span class="mr-alert-desc">
                                        @m_assetsNeedingDepreciation @Localizer["AssetsNeedMonthlyDepreciation"]
                                    </span>
                                </div>
                                <div class="mr-alert-action">
                                    <i class="ti ti-chevron-right"></i>
                                </div>
                            </a>
                        }

                        @if (m_overduePayments > 0)
                        {
                            <a href="/finance/loans?filter=overdue" class="mr-alert-item payment">
                                <div class="mr-alert-icon">
                                    <i class="ti ti-credit-card-off"></i>
                                </div>
                                <div class="mr-alert-content">
                                    <span class="mr-alert-title">@Localizer["OverduePayments"]</span>
                                    <span class="mr-alert-desc">
                                        @m_overduePayments @Localizer["LoanPaymentsOverdue"]
                                    </span>
                                </div>
                                <div class="mr-alert-action">
                                    <i class="ti ti-chevron-right"></i>
                                </div>
                            </a>
                        }

                        @if (m_upcomingPayments > 0)
                        {
                            <a href="/finance/loans?filter=upcoming" class="mr-alert-item upcoming">
                                <div class="mr-alert-icon">
                                    <i class="ti ti-calendar-due"></i>
                                </div>
                                <div class="mr-alert-content">
                                    <span class="mr-alert-title">@Localizer["UpcomingPayments"]</span>
                                    <span class="mr-alert-desc">
                                        @m_upcomingPayments @Localizer["PaymentsDueThisWeek"]
                                    </span>
                                </div>
                                <div class="mr-alert-action">
                                    <i class="ti ti-chevron-right"></i>
                                </div>
                            </a>
                        }

                        @if (m_underperformingAssets > 0)
                        {
                            <a href="/finance/reports/profitability?filter=negative" class="mr-alert-item underperform">
                                <div class="mr-alert-icon">
                                    <i class="ti ti-trending-down"></i>
                                </div>
                                <div class="mr-alert-content">
                                    <span class="mr-alert-title">@Localizer["Underperforming"]</span>
                                    <span class="mr-alert-desc">
                                        @m_underperformingAssets @Localizer["AssetsWithNegativeROI"]
                                    </span>
                                </div>
                                <div class="mr-alert-action">
                                    <i class="ti ti-chevron-right"></i>
                                </div>
                            </a>
                        }

                        @if (m_totalAlerts == 0)
                        {
                            <div class="mr-all-clear">
                                <i class="ti ti-circle-check"></i>
                                <span>@Localizer["AllClear"]</span>
                                <p>@Localizer["NoImmediateActionsRequired"]</p>
                            </div>
                        }
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════
         SECTION 4: Quick Actions Bar
         ═══════════════════════════════════════════════════════════════ -->
    <div class="mr-quick-actions mr-animate-in mr-animate-delay-3">
        <div class="mr-quick-actions-header">
            <i class="ti ti-bolt"></i>
            <h3>@Localizer["QuickActions"]</h3>
        </div>
        <div class="mr-quick-actions-body">
            <a href="/finance/depreciation/run" class="mr-btn-primary-action">
                <i class="ti ti-calculator"></i>
                @Localizer["RunMonthlyDepreciation"]
            </a>
            <a href="/finance/expenses/new" class="mr-btn-action success">
                <i class="ti ti-receipt-2"></i>
                @Localizer["RecordExpense"]
            </a>
            <a href="/finance/assets" class="mr-btn-action view">
                <i class="ti ti-list"></i>
                @Localizer["ViewAllAssets"]
            </a>
            <a href="/finance/loans" class="mr-btn-action info">
                <i class="ti ti-credit-card"></i>
                @Localizer["ManageLoans"]
            </a>
            <a href="/finance/reports/profitability" class="mr-btn-action warning">
                <i class="ti ti-report-analytics"></i>
                @Localizer["ProfitabilityReport"]
            </a>
        </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════
         SECTION 5: Recent Activity Timeline
         ═══════════════════════════════════════════════════════════════ -->
    <div class="mr-activity-section mr-animate-in mr-animate-delay-4">
        <div class="mr-section-header">
            <i class="ti ti-history"></i>
            <h3>@Localizer["RecentActivity"]</h3>
        </div>
        <div class="mr-activity-timeline">
            @foreach (var activity in m_recentActivities.Take(5))
            {
                <div class="mr-activity-item @activity.Type.ToLower()">
                    <div class="mr-activity-icon">
                        <i class="ti ti-@GetActivityIcon(activity.Type)"></i>
                    </div>
                    <div class="mr-activity-content">
                        <span class="mr-activity-title">@activity.Title</span>
                        <span class="mr-activity-desc">@activity.Description</span>
                        <span class="mr-activity-meta">
                            <i class="ti ti-clock"></i> @activity.Timestamp.Humanize()
                        </span>
                    </div>
                    @if (activity.Amount.HasValue)
                    {
                        <div class="mr-activity-amount @(activity.Amount > 0 ? "positive" : "negative")">
                            @(activity.Amount > 0 ? "+" : "")฿@activity.Amount.Value.ToString("N0")
                        </div>
                    }
                </div>
            }
        </div>
    </div>
</div>

@code {
    private AssetSummary m_summary = new();
    private List<Asset> m_topPerformers = [];
    private Dictionary<AssetExpenseCategory, decimal> m_expensesByCategory = new();
    private List<ActivityItem> m_recentActivities = [];

    private decimal m_totalExpenses;
    private decimal m_fleetROI;
    private decimal m_bookValuePercent;
    private decimal m_depreciationPercent;
    private string m_roiSparklinePoints = "0,25 20,22 40,20 60,18 80,15 100,12";

    private int m_assetsNeedingDepreciation;
    private int m_overduePayments;
    private int m_upcomingPayments;
    private int m_underperformingAssets;
    private int m_totalAlerts => m_assetsNeedingDepreciation + m_overduePayments + m_upcomingPayments + m_underperformingAssets;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        // Load summary
        m_summary = await AssetService.GetSummaryAsync();

        // Calculate percentages
        m_bookValuePercent = m_summary.TotalAcquisitionCost > 0
            ? (m_summary.TotalCurrentBookValue / m_summary.TotalAcquisitionCost * 100)
            : 0;
        m_depreciationPercent = m_summary.TotalAcquisitionCost > 0
            ? (m_summary.TotalAccumulatedDepreciation / m_summary.TotalAcquisitionCost * 100)
            : 0;
        m_fleetROI = m_summary.TotalAcquisitionCost > 0
            ? (m_summary.NetProfitLoss / m_summary.TotalAcquisitionCost * 100)
            : 0;

        // Load top performers
        var allAssets = await AssetService.GetAssetsAsync(AssetStatus.Active);
        m_topPerformers = allAssets.ItemCollection
            .OrderByDescending(a => a.ROIPercent)
            .Take(5)
            .ToList();

        // Load expense breakdown
        m_expensesByCategory = await ExpenseService.GetExpensesByCategoryAsync();
        m_totalExpenses = m_expensesByCategory.Values.Sum();

        // Load alerts
        await LoadAlertsAsync();

        // Load recent activity
        m_recentActivities = await AssetService.GetRecentActivityAsync(10);
    }

    private async Task LoadAlertsAsync()
    {
        m_assetsNeedingDepreciation = await AssetService.GetAssetsNeedingDepreciationCountAsync();
        m_overduePayments = await LoanService.GetOverduePaymentsCountAsync();
        m_upcomingPayments = await LoanService.GetUpcomingPaymentsCountAsync(7);
        m_underperformingAssets = await AssetService.GetUnderperformingAssetsCountAsync();
    }

    private string GetCategoryColor(AssetExpenseCategory category) => category switch
    {
        AssetExpenseCategory.Maintenance => "#22c55e",
        AssetExpenseCategory.Financing => "#3b82f6",
        AssetExpenseCategory.Insurance => "#8b5cf6",
        AssetExpenseCategory.Accident => "#ef4444",
        AssetExpenseCategory.Registration => "#f59e0b",
        AssetExpenseCategory.Consumables => "#06b6d4",
        _ => "#6b7280"
    };

    private string GetRankClass(int rank) => rank switch
    {
        1 => "gold",
        2 => "silver",
        3 => "bronze",
        _ => ""
    };

    private string GetActivityIcon(string type) => type switch
    {
        "Depreciation" => "trending-down",
        "Expense" => "receipt",
        "Payment" => "credit-card",
        "Revenue" => "cash",
        "Disposal" => "trash",
        _ => "activity"
    };

    public class ActivityItem
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTimeOffset Timestamp { get; set; }
        public decimal? Amount { get; set; }
    }
}
```

### 4.0.3 Dashboard CSS Styles

**File:** `src/MotoRent.Client/Pages/Finance/AssetDashboard.razor.css`

```css
/* ═══════════════════════════════════════════════════════════════════════
   Asset Dashboard - Financial Command Center Styles
   ═══════════════════════════════════════════════════════════════════════ */

/* Header Icon - Finance Theme */
.mr-header-icon-finance {
    background: linear-gradient(135deg, #059669 0%, #047857 50%, #065f46 100%);
    box-shadow: 0 4px 14px rgba(5, 150, 105, 0.4);
}

/* ═══════════════════════════════════════════════════════════════════════
   KPI Cards Grid - 5 Column Financial Summary
   ═══════════════════════════════════════════════════════════════════════ */
.mr-finance-kpi-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 1.25rem;
    margin-bottom: 1.5rem;
}

@media (max-width: 1399.98px) {
    .mr-finance-kpi-grid {
        grid-template-columns: repeat(3, 1fr);
    }
}

@media (max-width: 991.98px) {
    .mr-finance-kpi-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

@media (max-width: 575.98px) {
    .mr-finance-kpi-grid {
        grid-template-columns: 1fr;
    }
}

/* KPI Card Base */
.mr-kpi-card {
    background: var(--mr-bg-card);
    border: 1px solid var(--mr-border-light);
    border-radius: 16px;
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    position: relative;
    overflow: hidden;
    box-shadow: var(--mr-card-shadow);
    transition: all 0.3s ease;
}

.mr-kpi-card:hover {
    transform: translateY(-4px);
    box-shadow: var(--mr-card-shadow-hover);
}

/* KPI Card Accent Borders */
.mr-kpi-invested { border-top: 3px solid #3b82f6; }
.mr-kpi-bookvalue { border-top: 3px solid #8b5cf6; }
.mr-kpi-depreciation { border-top: 3px solid #f59e0b; }
.mr-kpi-profit { border-top: 3px solid #22c55e; }
.mr-kpi-loss { border-top: 3px solid #ef4444; }
.mr-kpi-roi { border-top: 3px solid #06b6d4; }

/* KPI Icon */
.mr-kpi-icon {
    width: 42px;
    height: 42px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
}

.mr-kpi-invested .mr-kpi-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.mr-kpi-bookvalue .mr-kpi-icon { background: rgba(139, 92, 246, 0.1); color: #8b5cf6; }
.mr-kpi-depreciation .mr-kpi-icon { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
.mr-kpi-profit .mr-kpi-icon { background: rgba(34, 197, 94, 0.1); color: #22c55e; }
.mr-kpi-loss .mr-kpi-icon { background: rgba(239, 68, 68, 0.1); color: #ef4444; }
.mr-kpi-roi .mr-kpi-icon { background: rgba(6, 182, 212, 0.1); color: #06b6d4; }

/* KPI Content */
.mr-kpi-content {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.mr-kpi-label {
    font-size: 0.75rem;
    font-weight: 500;
    color: var(--mr-text-muted);
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.mr-kpi-value {
    font-size: 1.75rem;
    font-weight: 700;
    color: var(--mr-text-primary);
    line-height: 1.2;
}

.mr-kpi-value.mr-kpi-success { color: #22c55e; }
.mr-kpi-value.mr-kpi-danger { color: #ef4444; }
.mr-kpi-value.mr-kpi-warning { color: #f59e0b; }
.mr-kpi-value.mr-kpi-highlight { color: #06b6d4; }

.mr-kpi-subtext {
    font-size: 0.8125rem;
    color: var(--mr-text-muted);
    display: flex;
    align-items: center;
    gap: 0.375rem;
}

.mr-kpi-subtext i {
    font-size: 0.875rem;
}

/* KPI Gauge (circular) */
.mr-kpi-gauge {
    position: absolute;
    top: 1rem;
    right: 1rem;
    width: 48px;
    height: 48px;
}

.mr-gauge-svg {
    transform: rotate(-90deg);
}

.mr-gauge-bg {
    fill: none;
    stroke: var(--mr-border-light);
    stroke-width: 3;
}

.mr-gauge-fill {
    fill: none;
    stroke: #8b5cf6;
    stroke-width: 3;
    stroke-linecap: round;
    transition: stroke-dasharray 0.6s ease;
}

/* KPI Progress Bar */
.mr-kpi-progress {
    height: 6px;
    background: var(--mr-border-light);
    border-radius: 3px;
    overflow: hidden;
    margin-top: 0.5rem;
}

.mr-kpi-progress-bar {
    height: 100%;
    background: linear-gradient(90deg, #f59e0b 0%, #d97706 100%);
    border-radius: 3px;
    transition: width 0.6s ease;
}

/* KPI Trend Indicator */
.mr-kpi-trend {
    position: absolute;
    top: 1rem;
    right: 1rem;
    width: 32px;
    height: 32px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1rem;
}

.mr-kpi-trend.up {
    background: rgba(34, 197, 94, 0.1);
    color: #22c55e;
}

.mr-kpi-trend.down {
    background: rgba(239, 68, 68, 0.1);
    color: #ef4444;
}

.mr-kpi-trend.neutral {
    background: rgba(107, 114, 128, 0.1);
    color: #6b7280;
}

/* KPI Sparkline */
.mr-kpi-sparkline {
    position: absolute;
    bottom: 1rem;
    right: 1rem;
    width: 80px;
    height: 24px;
    opacity: 0.6;
}

.mr-sparkline {
    width: 100%;
    height: 100%;
    color: #06b6d4;
}

/* ═══════════════════════════════════════════════════════════════════════
   Chart Cards
   ═══════════════════════════════════════════════════════════════════════ */
.mr-chart-card {
    background: var(--mr-bg-card);
    border: 1px solid var(--mr-border-light);
    border-radius: 16px;
    box-shadow: var(--mr-card-shadow);
    overflow: hidden;
    height: 100%;
    display: flex;
    flex-direction: column;
}

.mr-chart-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid var(--mr-border-light);
    background: var(--mr-bg-muted);
}

.mr-chart-title {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.mr-chart-title i {
    font-size: 1.25rem;
    color: var(--mr-accent-primary);
}

.mr-chart-title h3 {
    font-size: 1rem;
    font-weight: 600;
    margin: 0;
    color: var(--mr-text-primary);
}

.mr-chart-legend {
    display: flex;
    gap: 1rem;
}

.mr-legend-item {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.75rem;
    color: var(--mr-text-muted);
}

.mr-legend-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
}

.mr-legend-bookvalue .mr-legend-dot { background: #8b5cf6; }
.mr-legend-depreciation .mr-legend-dot { background: #f59e0b; }

.mr-chart-body {
    flex: 1;
    padding: 1.5rem;
    min-height: 280px;
}

.mr-chart-container {
    height: 100%;
    width: 100%;
}

.mr-chart-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--mr-border-light);
    background: var(--mr-bg-muted);
}

.mr-chart-period {
    font-size: 0.8125rem;
    color: var(--mr-text-muted);
}

.mr-chart-link {
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--mr-accent-primary);
    text-decoration: none;
    display: flex;
    align-items: center;
    gap: 0.375rem;
    transition: color 0.2s;
}

.mr-chart-link:hover {
    color: var(--mr-accent-dark);
}

.mr-chart-link i {
    transition: transform 0.2s;
}

.mr-chart-link:hover i {
    transform: translateX(3px);
}

/* Donut Chart Container */
.mr-donut-chart-container {
    display: flex;
    gap: 1.5rem;
    align-items: center;
    height: 100%;
}

.mr-donut-chart {
    flex: 0 0 160px;
    height: 160px;
}

.mr-donut-legend {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.mr-donut-legend-item {
    display: grid;
    grid-template-columns: 12px 1fr auto auto;
    gap: 0.75rem;
    align-items: center;
    font-size: 0.875rem;
}

.mr-donut-color {
    width: 12px;
    height: 12px;
    border-radius: 3px;
}

.mr-donut-label {
    color: var(--mr-text-primary);
}

.mr-donut-value {
    font-weight: 600;
    color: var(--mr-text-primary);
}

.mr-donut-percent {
    color: var(--mr-text-muted);
    font-size: 0.75rem;
}

.mr-expense-total {
    font-weight: 600;
    color: var(--mr-text-primary);
}

/* ═══════════════════════════════════════════════════════════════════════
   Performance Cards (Top Performers & Attention Needed)
   ═══════════════════════════════════════════════════════════════════════ */
.mr-performance-card {
    background: var(--mr-bg-card);
    border: 1px solid var(--mr-border-light);
    border-radius: 16px;
    box-shadow: var(--mr-card-shadow);
    overflow: hidden;
    height: 100%;
    display: flex;
    flex-direction: column;
}

.mr-performance-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid var(--mr-border-light);
    background: var(--mr-bg-muted);
}

.mr-performance-icon {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
}

.mr-performance-icon.success {
    background: rgba(34, 197, 94, 0.1);
    color: #22c55e;
}

.mr-performance-icon.warning {
    background: rgba(245, 158, 11, 0.1);
    color: #f59e0b;
}

.mr-performance-header h3 {
    flex: 1;
    font-size: 1rem;
    font-weight: 600;
    margin: 0;
    color: var(--mr-text-primary);
}

.mr-performance-badge {
    font-size: 0.6875rem;
    font-weight: 500;
    padding: 0.25rem 0.625rem;
    border-radius: 999px;
    background: rgba(34, 197, 94, 0.1);
    color: #22c55e;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.mr-alert-badge {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    background: #ef4444;
    color: white;
    font-size: 0.75rem;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
}

.mr-performance-body {
    flex: 1;
    padding: 1rem 0;
}

/* Performer List */
.mr-performer-list {
    display: flex;
    flex-direction: column;
}

.mr-performer-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.875rem 1.5rem;
    text-decoration: none;
    color: inherit;
    transition: background 0.2s;
    border-bottom: 1px solid var(--mr-border-light);
}

.mr-performer-item:last-child {
    border-bottom: none;
}

.mr-performer-item:hover {
    background: var(--mr-bg-muted);
}

.mr-performer-rank {
    width: 32px;
    height: 32px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.875rem;
    background: var(--mr-bg-muted);
    color: var(--mr-text-muted);
}

.mr-performer-rank.gold {
    background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
    color: white;
    box-shadow: 0 2px 8px rgba(251, 191, 36, 0.4);
}

.mr-performer-rank.silver {
    background: linear-gradient(135deg, #94a3b8 0%, #64748b 100%);
    color: white;
}

.mr-performer-rank.bronze {
    background: linear-gradient(135deg, #d97706 0%, #b45309 100%);
    color: white;
}

.mr-performer-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.mr-performer-name {
    font-weight: 600;
    color: var(--mr-text-primary);
    font-size: 0.9375rem;
}

.mr-performer-plate {
    font-size: 0.75rem;
    color: var(--mr-text-muted);
}

.mr-performer-stats {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.125rem;
}

.mr-performer-roi {
    font-weight: 600;
    font-size: 0.9375rem;
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.mr-performer-roi.success { color: #22c55e; }
.mr-performer-roi.danger { color: #ef4444; }

.mr-performer-revenue {
    font-size: 0.75rem;
    color: var(--mr-text-muted);
}

/* Alert List */
.mr-alert-list {
    display: flex;
    flex-direction: column;
}

.mr-alert-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem 1.5rem;
    text-decoration: none;
    color: inherit;
    transition: all 0.2s;
    border-left: 3px solid transparent;
}

.mr-alert-item:hover {
    background: var(--mr-bg-muted);
}

.mr-alert-item.depreciation { border-left-color: #f59e0b; }
.mr-alert-item.payment { border-left-color: #ef4444; }
.mr-alert-item.upcoming { border-left-color: #3b82f6; }
.mr-alert-item.underperform { border-left-color: #8b5cf6; }

.mr-alert-icon {
    width: 36px;
    height: 36px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.125rem;
}

.mr-alert-item.depreciation .mr-alert-icon { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
.mr-alert-item.payment .mr-alert-icon { background: rgba(239, 68, 68, 0.1); color: #ef4444; }
.mr-alert-item.upcoming .mr-alert-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.mr-alert-item.underperform .mr-alert-icon { background: rgba(139, 92, 246, 0.1); color: #8b5cf6; }

.mr-alert-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
}

.mr-alert-title {
    font-weight: 600;
    color: var(--mr-text-primary);
    font-size: 0.9375rem;
}

.mr-alert-desc {
    font-size: 0.8125rem;
    color: var(--mr-text-muted);
}

.mr-alert-action {
    color: var(--mr-text-muted);
    transition: color 0.2s, transform 0.2s;
}

.mr-alert-item:hover .mr-alert-action {
    color: var(--mr-accent-primary);
    transform: translateX(3px);
}

/* All Clear State */
.mr-all-clear {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    text-align: center;
}

.mr-all-clear i {
    font-size: 3rem;
    color: #22c55e;
    margin-bottom: 1rem;
}

.mr-all-clear span {
    font-size: 1.125rem;
    font-weight: 600;
    color: #22c55e;
}

.mr-all-clear p {
    font-size: 0.875rem;
    color: var(--mr-text-muted);
    margin: 0.5rem 0 0 0;
}

.mr-performance-footer {
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--mr-border-light);
    background: var(--mr-bg-muted);
    text-align: center;
}

/* Empty State */
.mr-empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    color: var(--mr-text-muted);
}

.mr-empty-state i {
    font-size: 2.5rem;
    margin-bottom: 0.75rem;
    opacity: 0.5;
}

/* ═══════════════════════════════════════════════════════════════════════
   Activity Timeline
   ═══════════════════════════════════════════════════════════════════════ */
.mr-activity-section {
    background: var(--mr-bg-card);
    border: 1px solid var(--mr-border-light);
    border-radius: 16px;
    box-shadow: var(--mr-card-shadow);
    margin-top: 1.5rem;
    overflow: hidden;
}

.mr-section-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid var(--mr-border-light);
    background: var(--mr-bg-muted);
}

.mr-section-header i {
    font-size: 1.25rem;
    color: var(--mr-accent-primary);
}

.mr-section-header h3 {
    font-size: 1rem;
    font-weight: 600;
    margin: 0;
    color: var(--mr-text-primary);
}

.mr-activity-timeline {
    padding: 0.5rem 0;
}

.mr-activity-item {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 1.5rem;
    border-bottom: 1px solid var(--mr-border-light);
    transition: background 0.2s;
}

.mr-activity-item:last-child {
    border-bottom: none;
}

.mr-activity-item:hover {
    background: var(--mr-bg-muted);
}

.mr-activity-icon {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1rem;
    flex-shrink: 0;
}

.mr-activity-item.depreciation .mr-activity-icon { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
.mr-activity-item.expense .mr-activity-icon { background: rgba(239, 68, 68, 0.1); color: #ef4444; }
.mr-activity-item.payment .mr-activity-icon { background: rgba(59, 130, 246, 0.1); color: #3b82f6; }
.mr-activity-item.revenue .mr-activity-icon { background: rgba(34, 197, 94, 0.1); color: #22c55e; }
.mr-activity-item.disposal .mr-activity-icon { background: rgba(107, 114, 128, 0.1); color: #6b7280; }

.mr-activity-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.mr-activity-title {
    font-weight: 600;
    color: var(--mr-text-primary);
    font-size: 0.9375rem;
}

.mr-activity-desc {
    font-size: 0.8125rem;
    color: var(--mr-text-muted);
}

.mr-activity-meta {
    font-size: 0.75rem;
    color: var(--mr-text-muted);
    display: flex;
    align-items: center;
    gap: 0.375rem;
    margin-top: 0.25rem;
}

.mr-activity-amount {
    font-weight: 600;
    font-size: 0.9375rem;
    align-self: center;
}

.mr-activity-amount.positive { color: #22c55e; }
.mr-activity-amount.negative { color: #ef4444; }

/* ═══════════════════════════════════════════════════════════════════════
   Animations
   ═══════════════════════════════════════════════════════════════════════ */
.mr-animate-in {
    animation: fadeSlideIn 0.5s ease forwards;
    opacity: 0;
}

.mr-animate-delay-1 { animation-delay: 0.1s; }
.mr-animate-delay-2 { animation-delay: 0.2s; }
.mr-animate-delay-3 { animation-delay: 0.3s; }
.mr-animate-delay-4 { animation-delay: 0.4s; }

@keyframes fadeSlideIn {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* ═══════════════════════════════════════════════════════════════════════
   Dark Mode Support
   ═══════════════════════════════════════════════════════════════════════ */
[data-bs-theme="dark"] .mr-kpi-card,
[data-bs-theme="dark"] .mr-chart-card,
[data-bs-theme="dark"] .mr-performance-card,
[data-bs-theme="dark"] .mr-activity-section {
    background: var(--mr-bg-card);
    border-color: var(--mr-border-default);
}

[data-bs-theme="dark"] .mr-chart-header,
[data-bs-theme="dark"] .mr-chart-footer,
[data-bs-theme="dark"] .mr-performance-header,
[data-bs-theme="dark"] .mr-section-header,
[data-bs-theme="dark"] .mr-performance-footer {
    background: rgba(0, 0, 0, 0.2);
}

[data-bs-theme="dark"] .mr-performer-item:hover,
[data-bs-theme="dark"] .mr-alert-item:hover,
[data-bs-theme="dark"] .mr-activity-item:hover {
    background: rgba(255, 255, 255, 0.03);
}
```

### 4.0.4 Dashboard Service Methods

Add to `AssetService.cs`:

```csharp
public async Task<int> GetAssetsNeedingDepreciationCountAsync()
{
    var currentMonth = DateTimeOffset.Now.ToString("yyyy-MM");
    var query = Context.CreateQuery<Asset>()
        .Where(a => a.Status == AssetStatus.Active)
        .Where(a => a.LastDepreciationDate == null ||
                    a.LastDepreciationDate < DateTimeOffset.Now.AddDays(-25));
    return await Context.GetCountAsync(query);
}

public async Task<int> GetUnderperformingAssetsCountAsync()
{
    var query = Context.CreateQuery<Asset>()
        .Where(a => a.Status == AssetStatus.Active);
    var assets = await Context.LoadAsync(query, 1, 1000, false);
    return assets.ItemCollection.Count(a => a.ROIPercent < 0);
}

public async Task<List<ActivityItem>> GetRecentActivityAsync(int count)
{
    // Combine recent depreciation entries, expenses, and payments
    // Return unified activity list ordered by timestamp
}
```

Add to `AssetExpenseService.cs`:

```csharp
public async Task<Dictionary<AssetExpenseCategory, decimal>> GetExpensesByCategoryAsync()
{
    var expenses = await Context.LoadAsync(
        Context.CreateQuery<AssetExpense>(), 1, 10000, false);

    return expenses.ItemCollection
        .GroupBy(e => e.Category)
        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
}
```

Add to `AssetLoanService.cs`:

```csharp
public async Task<int> GetOverduePaymentsCountAsync()
{
    var query = Context.CreateQuery<AssetLoanPayment>()
        .Where(p => p.Status == LoanPaymentStatus.Pending)
        .Where(p => p.DueDate < DateTimeOffset.Now);
    return await Context.GetCountAsync(query);
}

public async Task<int> GetUpcomingPaymentsCountAsync(int daysAhead)
{
    var endDate = DateTimeOffset.Now.AddDays(daysAhead);
    var query = Context.CreateQuery<AssetLoanPayment>()
        .Where(p => p.Status == LoanPaymentStatus.Pending)
        .Where(p => p.DueDate >= DateTimeOffset.Now)
        .Where(p => p.DueDate <= endDate);
    return await Context.GetCountAsync(query);
}
```

---

## Phase 5: UI Pages

### 5.1 Asset Management
| Route | Purpose |
|-------|---------|
| `/finance/assets` | Asset list with summary cards |
| `/finance/assets/{id}` | Asset detail with tabs (Overview, Depreciation, Expenses, Projections, Loan) |
| `AssetDialog.razor` | Create/edit asset |

### 4.2 Depreciation
| Route | Purpose |
|-------|---------|
| `/finance/depreciation` | Batch depreciation processing |
| `RecordDepreciationDialog.razor` | Record with optional override |

### 4.3 Expenses
| Route | Purpose |
|-------|---------|
| `/finance/expenses` | Expense list by category |
| `AssetExpenseDialog.razor` | Create/edit expense |

### 4.4 Loans
| Route | Purpose |
|-------|---------|
| `/finance/loans` | Loan list |
| `/finance/loans/{id}` | Loan detail with amortization schedule |
| `AssetLoanDialog.razor` | Create loan |
| `LoanPaymentDialog.razor` | Record payment |

### 4.5 Reports
| Route | Purpose |
|-------|---------|
| `/finance/reports/profitability` | Vehicle ROI report |
| `/finance/reports/depreciation` | Depreciation schedule report |

---

## Phase 5: Integration Points

### 5.1 Entity.cs
Add JSON polymorphism attributes:
```csharp
// Asset tracking entities
[JsonDerivedType(typeof(Asset), nameof(Asset))]
[JsonDerivedType(typeof(DepreciationEntry), nameof(DepreciationEntry))]
[JsonDerivedType(typeof(AssetExpense), nameof(AssetExpense))]
[JsonDerivedType(typeof(AssetLoan), nameof(AssetLoan))]
[JsonDerivedType(typeof(AssetLoanPayment), nameof(AssetLoanPayment))]
```

### 5.2 ServiceCollectionExtensions.cs
Register repositories:
```csharp
// Asset tracking entities
services.AddSingleton<IRepository<Asset>, Repository<Asset>>();
services.AddSingleton<IRepository<DepreciationEntry>, Repository<DepreciationEntry>>();
services.AddSingleton<IRepository<AssetExpense>, Repository<AssetExpense>>();
services.AddSingleton<IRepository<AssetLoan>, Repository<AssetLoan>>();
services.AddSingleton<IRepository<AssetLoanPayment>, Repository<AssetLoanPayment>>();
```

### 5.3 Rental Checkout Integration
In `RentalService.CompleteCheckoutAsync()`:
- Call `AssetService.AddRevenueFromRentalAsync()`
- Trigger first rental depreciation if applicable

### 5.4 Maintenance Integration
In `MaintenanceService.RecordServiceAsync()`:
- Call `AssetExpenseService.CreateFromMaintenanceAsync()`

### 5.5 Accident Integration
When accident costs are finalized:
- Create `AssetExpense` with `Category = Accident`

---

## Critical Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add 5 JsonDerivedType attributes |
| `src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs` | Register 5 repositories |
| `src/MotoRent.Server/Program.cs` | Register services |

## New Files to Create

### Entities (5 files)
- `src/MotoRent.Domain/Entities/Asset.cs`
- `src/MotoRent.Domain/Entities/DepreciationEntry.cs`
- `src/MotoRent.Domain/Entities/AssetExpense.cs`
- `src/MotoRent.Domain/Entities/AssetLoan.cs`
- `src/MotoRent.Domain/Entities/AssetLoanPayment.cs`
- `src/MotoRent.Domain/Entities/AssetEnums.cs`

### SQL Tables (5 files)
- `database/tables/MotoRent.Asset.sql`
- `database/tables/MotoRent.DepreciationEntry.sql`
- `database/tables/MotoRent.AssetExpense.sql`
- `database/tables/MotoRent.AssetLoan.sql`
- `database/tables/MotoRent.AssetLoanPayment.sql`

### Services (4 files)
- `src/MotoRent.Services/DepreciationCalculator.cs`
- `src/MotoRent.Services/AssetService.cs`
- `src/MotoRent.Services/AssetExpenseService.cs`
- `src/MotoRent.Services/AssetLoanService.cs`

### UI Pages (~10 files)
- `src/MotoRent.Client/Pages/Finance/Assets.razor`
- `src/MotoRent.Client/Pages/Finance/AssetDetail.razor`
- `src/MotoRent.Client/Pages/Finance/AssetDialog.razor`
- `src/MotoRent.Client/Pages/Finance/Depreciation.razor`
- `src/MotoRent.Client/Pages/Finance/AssetExpenses.razor`
- `src/MotoRent.Client/Pages/Finance/AssetExpenseDialog.razor`
- `src/MotoRent.Client/Pages/Finance/Loans.razor`
- `src/MotoRent.Client/Pages/Finance/LoanDetail.razor`
- `src/MotoRent.Client/Pages/Finance/Reports/Profitability.razor`

---

## Verification Plan

### 1. Build & Run
```bash
cd E:\project\work\motorent.aseet-depreciation
dotnet build
dotnet run --project src/MotoRent.Server
```

### 2. Database
- Run SQL scripts against local database
- Verify tables created with computed columns

### 3. Functional Testing
- [ ] Create asset for existing vehicle
- [ ] Create asset with loan financing
- [ ] Record depreciation (all methods)
- [ ] Record manual depreciation override
- [ ] Add expenses (various categories)
- [ ] Record loan payments
- [ ] View depreciation projections
- [ ] View profitability report
- [ ] Complete rental and verify revenue tracking

### 4. Edge Cases
- [ ] Pre-existing vehicle with initial book value
- [ ] First rental triggering Day Out of Door
- [ ] Depreciation below residual value (should stop)
- [ ] Loan payoff
- [ ] Asset disposal with gain/loss calculation

---

## Implementation Order

1. **Entities & Enums** - Define data structures
2. **SQL Tables** - Create database schema
3. **Entity Registration** - Update Entity.cs and ServiceCollectionExtensions.cs
4. **DepreciationCalculator** - Core calculation logic
5. **AssetService** - Main CRUD and operations
6. **AssetExpenseService** - Expense tracking
7. **AssetLoanService** - Loan management
8. **UI: Asset list & detail** - Core pages
9. **UI: Dialogs** - Create/edit forms
10. **UI: Reports** - Profitability and depreciation reports
11. **Integration** - Rental checkout, maintenance hooks
