# Plan: Comprehensive Documentation Update for MotoRent (JaleOs Malaysia)

## Objective
Update the MotoRent documentation to include all newly identified features, focusing on a "WHY-First" approach for the Malaysian market. This includes creating three new high-impact guides in English and Bahasa Melayu, updating the `Learn.razor` page, and ensuring all guides are discoverable via an automatically generated manifest.

## Key Files & Context
- **Guides Path**: `user.guides/` (English), `user.guides.ms/` (Bahasa Melayu)
- **New Guides**:
  - `13-dynamic-pricing.md`
  - `14-asset-financing.md`
  - `15-accidents-and-fines.md`
- **Frontend Page**: `src/MotoRent.Client/Pages/Learn.razor`
- **Manifest Script**: `scripts/generate-docs-manifest.ps1`
- **Market Context**: Malaysian rental market (MYR currency, Langkawi/Penang/KK locations, SST, Hire Purchase loans).

## Implementation Steps

### Phase 1: Infrastructure & Discoverability
1. **Create Malay Directory**: Ensure `user.guides.ms/` exists for localized documentation.
2. **Generate Manifests**: Run the `generate-docs-manifest.ps1` script for both English and Malay directories to create `manifest.json`.
3. **Update `Learn.razor`**: Ensure the language switcher supports 'ms' and the sidebar correctly loads the Malay manifest.

### Phase 2: Content Creation (WHY-First approach)
1. **Dynamic Pricing Guide (`13-dynamic-pricing.md`)**:
   - **WHY**: Explain how static prices lose revenue during school holidays and festive seasons (Hari Raya, CNY) and how automated rules increase profit.
   - **HOW**: Instructions for setting up seasonal rules, duration-based discounts, and demand-based adjustments in MYR.
2. **Asset Financing Guide (`14-asset-financing.md`)**:
   - **WHY**: Explain how tracking Hire Purchase (HP) interest and principal payments is critical for calculating true ROI on each bike.
   - **HOW**: Instructions for entering loan details, tracking amortization, and viewing financing reports.
3. **Accident & Fine Recovery Guide (`15-accidents-and-fines.md`)**:
   - **WHY**: Explain how unrecovered JPJ/PDRM summons and accident damages are "hidden leaks" in profit.
   - **HOW**: Walkthrough of the Accident Reporting module and the Traffic Summons billing process.

### Phase 3: Visuals & Localization
1. **Generate Illustrations**: Use `banana-pro-2` to create professional flat-design illustrations (Hero images) with a Malaysian aesthetic.
2. **Bahasa Melayu Translation**: Translate the three new guides into Malay and place them in `user.guides.ms/`.
3. **Update Manifests**: Re-run the manifest generation script to include the newly created guides.

## Verification & Testing
1. **Visual Inspection**: Open the `/learn` page and verify the new guides appear in the sidebar.
2. **Content Audit**: Verify MYR currency and Malaysian context are used consistently.
3. **Language Switcher**: Test switching between EN and MS.
