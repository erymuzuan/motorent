# Plan: Fix AssetDashboard Localization

## Problem
The AssetDashboard page (`src/MotoRent.Client/Pages/Finance/AssetDashboard.razor`) has no resource files, so all `@this.Localizer["Key"]` calls display raw key names (e.g., "ExpenseBreakdown", "TotalExpensesLabel", "RevenueLabel") instead of natural, human-readable text.

## Solution
Create resource files with natural language translations:

1. **`Resources/Pages/Finance/AssetDashboard.resx`** - Default (English)
2. **`Resources/Pages/Finance/AssetDashboard.th.resx`** - Thai

## All Localizer Keys and Proposed Values

| Key | English (natural) | Thai |
|-----|-------------------|------|
| AssetDashboard | Asset Dashboard | แดชบอร์ดสินทรัพย์ |
| Finance | Finance | การเงิน |
| AssetFinancialOverview | Asset Financial Overview | ภาพรวมการเงินสินทรัพย์ |
| AssetOverviewDescription | Track your fleet's financial health, depreciation, and ROI | ติดตามสถานะการเงิน ค่าเสื่อมราคา และผลตอบแทนของรถ |
| AddAsset | Add Asset | เพิ่มสินทรัพย์ |
| TotalInvested | Total Invested | เงินลงทุนทั้งหมด |
| ActiveVehiclesCount | {0} active vehicles | รถที่ใช้งาน {0} คัน |
| CurrentBookValue | Current Book Value | มูลค่าตามบัญชี |
| PercentOfOriginal | {0}% of original cost | {0}% ของต้นทุนเดิม |
| AccumulatedDepreciation | Accumulated Depreciation | ค่าเสื่อมราคาสะสม |
| PercentDepreciated | {0}% depreciated | เสื่อมราคาแล้ว {0}% |
| NetProfitLoss | Net Profit / Loss | กำไร/ขาดทุนสุทธิ |
| Revenue | Revenue: {0} | รายได้: {0} |
| FleetROI | Fleet ROI | ผลตอบแทนรถทั้งหมด |
| ReturnOnInvestment | Return on Investment | ผลตอบแทนการลงทุน |
| ExpenseBreakdown | Expense Breakdown | รายละเอียดค่าใช้จ่าย |
| TotalExpensesLabel | Total expenses: {0} | ค่าใช้จ่ายรวม: {0} |
| AttentionNeeded | Attention Needed | ต้องดำเนินการ |
| DepreciationDue | Depreciation Due | ถึงกำหนดคิดค่าเสื่อม |
| AssetsNeedingDepreciationCount | {0} assets need depreciation calculation | สินทรัพย์ {0} รายการ ต้องคำนวณค่าเสื่อม |
| OverduePayments | Overdue Payments | ค่างวดค้างชำระ |
| OverduePaymentsCount | {0} payments are overdue | ค้างชำระ {0} งวด |
| UpcomingPayments | Upcoming Payments | ค่างวดที่จะถึงกำหนด |
| UpcomingPaymentsCount | {0} payments due within 7 days | ถึงกำหนดใน 7 วัน {0} งวด |
| Underperforming | Underperforming | ผลตอบแทนต่ำ |
| UnderperformingAssetsCount | {0} assets with negative ROI | สินทรัพย์ {0} รายการ ขาดทุน |
| AllClear | All Clear | ไม่มีรายการ |
| NoImmediateActions | No immediate actions required | ไม่มีรายการที่ต้องดำเนินการ |
| TopPerformers | Top Performers | รถทำกำไรสูงสุด |
| HighestROI | Highest ROI | ผลตอบแทนสูงสุด |
| RevenueLabel | Revenue: {0} | รายได้: {0} |
| ViewAllPerformance | View All Performance | ดูผลประกอบการทั้งหมด |
| NoPerformanceData | No performance data yet | ยังไม่มีข้อมูลผลประกอบการ |
| UnderperformingAssets | Underperforming Assets | สินทรัพย์ที่ขาดทุน |
| LossLabel | Loss: {0} | ขาดทุน: {0} |
| AllAssetsProfitable | All Assets Profitable | สินทรัพย์ทั้งหมดมีกำไร |
| NoUnderperformingAssets | No underperforming assets | ไม่มีสินทรัพย์ที่ขาดทุน |
| AllAssets | All Assets | สินทรัพย์ทั้งหมด |
| AllCount | All ({0}) | ทั้งหมด ({0}) |
| ActiveCount | Active ({0}) | ใช้งาน ({0}) |
| DisposedCount | Disposed ({0}) | ขายแล้ว ({0}) |
| WrittenOffCount | Written Off ({0}) | ตัดจำหน่าย ({0}) |
| Vehicle | Vehicle | รถ |
| Status | Status | สถานะ |
| AcquisitionCost | Acquisition Cost | ราคาซื้อ |
| BookValue | Book Value | มูลค่าตามบัญชี |
| Depreciation | Depreciation | ค่าเสื่อมราคา |
| ROI | ROI | ผลตอบแทน |
| ViewDetails | View Details | ดูรายละเอียด |
| Edit | Edit | แก้ไข |
| NoAssetsFound | No assets found | ไม่พบสินทรัพย์ |
| QuickActions | Quick Actions | ทางลัด |
| RunMonthlyDepreciation | Run Monthly Depreciation | คำนวณค่าเสื่อมรายเดือน |
| RecordExpense | Record Expense | บันทึกค่าใช้จ่าย |
| DepreciationReport | Depreciation Report | รายงานค่าเสื่อมราคา |
| ManageLoans | Manage Loans | จัดการสินเชื่อ |

## Files to Create
1. `src/MotoRent.Client/Resources/Pages/Finance/AssetDashboard.resx`
2. `src/MotoRent.Client/Resources/Pages/Finance/AssetDashboard.th.resx`

## Verification
- Build with `dotnet build` to ensure resources compile
- Navigate to `/finance/asset-dashboard` and confirm labels show natural English text
- Switch to Thai locale and confirm Thai translations display correctly
