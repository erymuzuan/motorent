# Plan: Redesign AssetEdit Page

## Goal
Transform the bland `AssetEdit.razor` page into a visually rich, two-column layout matching the design quality of `AssetDetails.razor`. Add a comment panel and a link to the details page.

## Files to Modify
1. `src/MotoRent.Client/Pages/Finance/AssetEdit.razor` - Main page layout redesign
2. **NEW** `src/MotoRent.Client/Pages/Finance/AssetEdit.razor.css` - Scoped CSS (reusing AssetDetails patterns)
3. `src/MotoRent.Client/Resources/Pages/Finance/AssetEdit.resx` - New localization keys
4. `src/MotoRent.Client/Resources/Pages/Finance/AssetEdit.th.resx` - Thai translations

## Design

### Layout (Edit Mode - existing asset)
```
[Page Header with breadcrumb, title, actions dropdown, "View Details" link button]
[Container]
  Row:
    Col-lg-8:  Form card (AssetDialog) with styled card header + save/cancel footer
    Col-lg-4:  Sidebar
      - Asset Summary card (KPI mini-cards: acquisition cost, book value, depreciation, status)
      - Comment Panel card (using existing CommentPanel component)
```

### Layout (New Mode)
```
[Page Header with breadcrumb, title]
[Container]
  Full-width form card (no sidebar since no asset data or comments yet)
```

### Specific Changes

**Page Header Enhancement:**
- Add "View Details" button (link to `/finance/assets/{AssetId}/details`) when editing existing asset, alongside existing Actions dropdown
- Use `mr-header-icon-finance` gradient styling from AssetDetails

**Form Card (col-lg-8 when editing, col-12 when new):**
- Styled card with `border-radius: 16px` and `mr-edit-card` class
- Card header with icon + title
- AssetDialog embedded in card body
- Card footer with Cancel + Save buttons (existing)

**Sidebar (col-lg-4, edit mode only):**

1. **Asset Summary Mini Card** - compact KPI showing:
   - Status badge
   - Acquisition Cost
   - Current Book Value
   - Accumulated Depreciation
   - Useful Life Remaining

2. **Comment Panel Card** - using existing `<CommentPanel>` component:
   - `EntityId="m_assetId"`
   - `EntityType="Asset"`
   - Wrapped in styled card with header

**CSS File** - New scoped CSS file borrowing patterns from AssetDetails.razor.css:
- `.mr-edit-card` - main form card styling (rounded corners, shadow)
- `.mr-edit-sidebar-card` - sidebar card styling
- `.mr-edit-summary-row` - key-value rows in summary
- `.mr-edit-summary-value` - formatted values
- Dark mode support
- Fade-in animations

## Localization Keys to Add
- `ViewDetails` / "View Details" / "ดูรายละเอียด"
- `AssetSummary` / "Asset Summary" / "สรุปสินทรัพย์"
- `Comments` / "Comments" / "ความคิดเห็น"
- `AcquisitionCost` / "Acquisition Cost" / "ราคาที่ซื้อ"
- `BookValue` / "Book Value" / "มูลค่าตามบัญชี"
- `Depreciation` / "Depreciation" / "ค่าเสื่อมราคา"
- `RemainingLife` / "Remaining Life" / "อายุการใช้งานที่เหลือ"
- `MonthsUnit` / "months" / "เดือน"
- `Status` / "Status" / "สถานะ"
- `CurrencyTHB` / "THB" / "บาท"

## Verification
1. `dotnet build` to verify no compile errors
2. Navigate to `/finance/assets/new` - should show full-width form (no sidebar)
3. Navigate to `/finance/assets/{id}` - should show two-column layout with sidebar
4. Click "View Details" button - should navigate to details page
5. Comment panel should load and allow adding comments
6. Actions dropdown (Dispose/Write Off) should still work
