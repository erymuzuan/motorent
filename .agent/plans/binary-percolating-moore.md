# Plan: Hash Asset Page URL IDs

## Summary
Asset pages expose raw integer IDs in URLs (e.g., `/finance/assets/42/details`). Convert to HashIds-encoded strings using the existing `EncodeId`/`DecodeId` from `MotoRentComponentBase`.

## Pattern Reference
Existing correct pattern (from VehicleDetails.razor):
- Route: `@page "/vehicles/{VehicleId}"` (string parameter)
- Code: `m_vehicleId = DecodeId(VehicleId);` then use `m_vehicleId` for data loading
- Links: `href="/vehicles/@EncodeId(vehicle.VehicleId)"`

## Files to Modify

### 1. `src/MotoRent.Client/Pages/Finance/AssetDetails.razor`
- **Line 1**: `{AssetId:int}` -> `{AssetId}`
- **Line 33**: `href="/finance/assets/@AssetId"` -> `href="/finance/assets/@AssetId"` (already string, but need EncodeId for edit link since AssetEdit will also use string param)
- **Line 335**: `public int AssetId` -> `public string? AssetId`
- Add `private int m_assetId;` field
- In `OnParametersSetAsync`: `m_assetId = DecodeId(AssetId);`
- Replace all `AssetId` usages in data loading with `m_assetId`

### 2. `src/MotoRent.Client/Pages/Finance/AssetEdit.razor`
- **Line 2**: `{AssetId:int}` -> `{AssetId}`
- **Line 96**: `public int AssetId` -> `public string? AssetId`
- **Line 103**: `IsNew` check: `AssetId == 0` -> `string.IsNullOrEmpty(AssetId)`
- Add `private int m_assetId;` field
- In `OnParametersSetAsync`: `m_assetId = DecodeId(AssetId);`
- Replace `AssetId` usages in data loading with `m_assetId`
- After save navigation: use `EncodeId` for redirect URL

### 3. `src/MotoRent.Client/Pages/Finance/AssetDashboard.razor`
- **Line 293**: `@asset.AssetId` -> `@EncodeId(asset.AssetId)`
- **Line 344**: `@asset.AssetId` -> `@EncodeId(asset.AssetId)`
- **Line 448**: `@asset.AssetId/details` -> `@EncodeId(asset.AssetId)/details`
- **Line 451**: `@asset.AssetId` -> `@EncodeId(asset.AssetId)`

### 4. `src/MotoRent.Client/Pages/Finance/DepreciationReport.razor`
- **Line 264**: `@assetData.AssetId/details` -> `@EncodeId(assetData.AssetId)/details`

## Verification
- `dotnet build` - no compile errors
- Navigate to Asset Dashboard, verify links use encoded IDs
- Click through to AssetDetails and AssetEdit, verify pages load correctly
- Verify "Edit Asset" link from AssetDetails works
- Verify DepreciationReport links work
