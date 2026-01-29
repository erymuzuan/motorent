# AssetDetails Page Redesign Plan

## Overview
Redesign `/finance/assets/{AssetId}/details` to be a comprehensive vehicle asset dashboard with vehicle widget, expanded KPIs, income vs cost analysis, and loan amortization table.

## Task Breakdown

### Task 1: Vehicle Hero Widget
Add a vehicle identity card at the top showing:
- Vehicle image from `Vehicle.ImagePath` (fallback to vehicle-type icon if null)
- Brand, model, year, color, license plate, vehicle type badge
- Daily rate, deposit amount
- Asset status badge (Active/Disposed/WriteOff)
- Acquisition date, vendor name

**Data**: Inject `VehicleService`, load via `Asset.VehicleId`.

### Task 2: Expanded KPI Stats Grid (3 rows × 4 cards = 12 KPIs)

**Row 1 — Asset Value & Depreciation**:
| KPI | Source | Icon |
|-----|--------|------|
| Acquisition Cost | `Asset.AcquisitionCost` | ti-building-bank |
| Current Book Value | `Asset.CurrentBookValue` | ti-receipt |
| Accumulated Depreciation | `Asset.AccumulatedDepreciation` | ti-trending-down |
| Monthly Depreciation | `Asset.MonthlyDepreciationStraightLine` | ti-calendar-minus |

**Row 2 — Income & Profitability**:
| KPI | Source | Icon |
|-----|--------|------|
| Total Rental Income | `Asset.TotalRevenue` | ti-cash |
| Total Expenses | `Asset.TotalExpenses` | ti-receipt-2 |
| Net Profit/Loss | `Asset.NetProfitLoss` | ti-scale |
| ROI % | `Asset.ROIPercent` | ti-percentage |

**Row 3 — Operational**:
| KPI | Source | Icon |
|-----|--------|------|
| Revenue per Month | `TotalRevenue / monthsActive` | ti-report-money |
| Income-to-Maintenance Ratio | `TotalRevenue / maintenanceCost` | ti-chart-pie |
| Remaining Useful Life | `Asset.RemainingUsefulLifeMonths` | ti-hourglass |
| Fleet Age | months since acquisition | ti-clock |

### Task 3: Income vs CapEx/OpEx Comparison Card
Simple two-number split with horizontal bar visualization:
- **CapEx**: Acquisition cost (+ loan interest if financed)
- **OpEx**: `Asset.TotalExpenses` (all operational costs)
- **Income**: `Asset.TotalRevenue`
- **Net Position**: Income - CapEx - OpEx
- SVG horizontal stacked bar for visual comparison

### Task 4: Loan Amortization Table (Paginated)
Shown only when `Asset.AssetLoanId.HasValue`. Paginated at 12 rows/page.

**Loan Summary Header**: Lender, account no, principal, rate, term, monthly payment, status badge.

**Table Columns**:
| Column | Source |
|--------|--------|
| Payment # | `PaymentNumber` |
| Due Date | `DueDate` |
| Paid Date | `PaidDate` |
| Principal | `PrincipalAmount` |
| Interest | `InterestAmount` |
| VAT on Interest | `InterestAmount * 0.07` (Thai 7% VAT) |
| Total | `TotalAmount + VAT` |
| Balance | `BalanceAfter` |
| Status | Badge: Paid/Pending/Late/Missed |

**Footer totals**: Sum of principal paid, interest paid, total VAT paid.

### Task 5: Localization Resources
Update `.resx` and `.th.resx` with all new label keys.

### Task 6: Data Loading & Code Block
- Inject: `VehicleService`, `AssetExpenseService`, `AssetLoanService`
- Load in `LoadDataAsync()`: vehicle, expenses (for maintenance total), loan + payments
- Add pagination state for loan table (`m_loanPage`, `m_loanPageSize = 12`)
- Add computed helpers for new KPIs

## Page Layout

```
┌─────────────────────────────────────────────────┐
│ Breadcrumb + Page Header + Edit button          │
├────────────┬────────────────────────────────────┤
│ Vehicle    │  KPI Row 1: Value & Depreciation   │
│ Image +    ├────────────────────────────────────┤
│ Info Card  │  KPI Row 2: Income & Profitability │
│ (col-lg-4) ├────────────────────────────────────┤
│            │  KPI Row 3: Operational            │
│            │  (col-lg-8)                        │
├────────────┴────────────────────────────────────┤
│ ┌─────────────────────┐ ┌────────────────────┐  │
│ │ Depreciation Chart  │ │ Income vs CapEx/   │  │
│ │ (col-lg-8, existing)│ │ OpEx (col-lg-4)    │  │
│ └─────────────────────┘ └────────────────────┘  │
├─────────────────────────────────────────────────┤
│ Depreciation Settings + History (existing)      │
├─────────────────────────────────────────────────┤
│ Loan Amortization (if financed)                 │
│ - Summary KPIs + Paginated payment table        │
└─────────────────────────────────────────────────┘
```

## Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Client/Pages/Finance/AssetDetails.razor` | All UI + code block changes |
| `src/MotoRent.Client/Pages/Finance/AssetDetails.razor.css` | New styles for vehicle card, KPI rows, loan table |
| `src/MotoRent.Client/Resources/Pages/Finance/AssetDetails.resx` | ~30 new English labels |
| `src/MotoRent.Client/Resources/Pages/Finance/AssetDetails.th.resx` | ~30 new Thai labels |

## Verification
1. `dotnet build` — no compilation errors
2. Navigate to `/finance/assets/{id}/details` — page loads
3. Vehicle image displays (or fallback icon)
4. All 12 KPI cards render with correct values
5. Income vs CapEx/OpEx bar renders
6. Loan table shows for financed assets with pagination
7. Loan table hidden for non-financed assets
8. Responsive layout works on mobile/tablet
