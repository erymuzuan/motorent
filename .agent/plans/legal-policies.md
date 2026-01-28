# Plan: Legal Pages & Footer Cleanup

## Summary
1. Remove SUPPORT section from footer
2. Add "Refund Policy" link to LEGAL section in footer
3. Create 3 new public pages: Terms of Service, Privacy Policy, Refund Policy
4. All content in English + Thai localization via .resx files
5. Content reflects SaaS subscription model for vehicle rental management in Thailand

---

## 1. Modify Footer (`PublicLayout.razor` lines 157-173)

**Remove** the entire SUPPORT section (lines 166-173):
```razor
@* Contact *@
<div class="col-lg-2 col-md-6 col-6">
    <h6 class="footer-heading">@Localizer["Support"]</h6>
    ...
</div>
```

**Add** Refund link to LEGAL section:
```razor
<li><a href="/refund">@Localizer["Refund"]</a></li>
```

**Update resource files:**
- `Resources/Layout/PublicLayout.resx` — add `Refund` = "Refund Policy"
- `Resources/Layout/PublicLayout.th.resx` — add `Refund` = "นโยบายการคืนเงิน"

---

## 2. Create 3 Legal Pages

Each page follows the `Pricing.razor` pattern:
```razor
@page "/terms"
@using Microsoft.AspNetCore.Authorization
@attribute [AllowAnonymous]
@layout Layout.PublicLayout
@inherits LocalizedComponentBase<TermsOfService>
```

No `@code` block needed — these are static content pages.

### Page structure (shared across all 3):
- Teal gradient hero section with page title + "Last Updated" date
- Centered content area (`col-lg-8`) with section headings and paragraphs
- All text via `@Localizer["Key"]` — section titles and paragraphs as separate keys
- Scoped `<style>` block in each `.razor` file (matching project pattern)

### Files to create:

| Page | Route | Razor File |
|------|-------|-----------|
| Terms of Service | `/terms` | `Pages/TermsOfService.razor` |
| Privacy Policy | `/privacy` | `Pages/PrivacyPolicy.razor` |
| Refund Policy | `/refund` | `Pages/RefundPolicy.razor` |

### Resource files (4 per page = 12 total):

For each page: `.resx` (default), `.en.resx`, `.th.resx`, `.ms.resx`
Location: `Resources/Pages/<PageName>.<culture>.resx`

---

## 3. Content Outline

### Terms of Service
1. Acceptance of Terms
2. Description of Service — SaaS platform for motorbike/vehicle rental management
3. Account Registration & Security
4. Subscription Plans & Billing — recurring billing, VAT (7%), auto-renewal
5. User Responsibilities & Acceptable Use
6. Intellectual Property
7. Data Protection — cross-reference to Privacy Policy
8. Limitation of Liability — balanced per Thai Unfair Contract Terms Act
9. Termination
10. Governing Law — Thai law, Thai courts
11. Contact Information

### Privacy Policy (PDPA-compliant)
1. Information We Collect — account data, rental data, document images (OCR)
2. Legal Basis for Processing — contract, consent, legitimate interest
3. How We Use Your Information
4. Data Sharing & Third Parties — Google/Microsoft OAuth, Google Gemini OCR, cloud hosting
5. Cross-Border Data Transfers
6. Data Retention
7. Your Rights Under PDPA — access, rectification, erasure, portability, objection, withdrawal of consent, right to complain to PDPC
8. Cookies & Tracking
9. Data Security
10. Changes to This Policy
11. Contact / Data Protection Officer

### Refund Policy (must match Pricing page: "14-day money-back guarantee on all paid plans")
1. 14-Day Money-Back Guarantee
2. How to Request a Refund
3. Refund Processing — same payment method, timeline
4. Post-Guarantee Period — pro-rata for annual plans, no refund for monthly after current period
5. Free Trial — no charge, no refund applicable
6. Contact Information

---

## 4. CSS Styling

Shared scoped CSS in each `.razor` file (duplicated per project convention):
- `.legal-page` — light background
- `.legal-hero` — teal gradient matching site theme
- `.legal-content` — centered, readable typography (`col-lg-8`)
- Section `h2` with teal bottom border
- Body text in muted color with 1.8 line-height

---

## 5. Files Summary

### Modified (3 files):
- `src/MotoRent.Client/Layout/PublicLayout.razor`
- `src/MotoRent.Client/Resources/Layout/PublicLayout.resx`
- `src/MotoRent.Client/Resources/Layout/PublicLayout.th.resx`

### Created (15 files):
- `src/MotoRent.Client/Pages/TermsOfService.razor`
- `src/MotoRent.Client/Pages/PrivacyPolicy.razor`
- `src/MotoRent.Client/Pages/RefundPolicy.razor`
- `src/MotoRent.Client/Resources/Pages/TermsOfService.resx`
- `src/MotoRent.Client/Resources/Pages/TermsOfService.en.resx`
- `src/MotoRent.Client/Resources/Pages/TermsOfService.th.resx`
- `src/MotoRent.Client/Resources/Pages/TermsOfService.ms.resx`
- `src/MotoRent.Client/Resources/Pages/PrivacyPolicy.resx`
- `src/MotoRent.Client/Resources/Pages/PrivacyPolicy.en.resx`
- `src/MotoRent.Client/Resources/Pages/PrivacyPolicy.th.resx`
- `src/MotoRent.Client/Resources/Pages/PrivacyPolicy.ms.resx`
- `src/MotoRent.Client/Resources/Pages/RefundPolicy.resx`
- `src/MotoRent.Client/Resources/Pages/RefundPolicy.en.resx`
- `src/MotoRent.Client/Resources/Pages/RefundPolicy.th.resx`
- `src/MotoRent.Client/Resources/Pages/RefundPolicy.ms.resx`

---

## 6. Verification

1. `dotnet build` — ensure no compilation errors
2. Navigate to `/terms`, `/privacy`, `/refund` — pages render correctly
3. Switch language to Thai — all content displays in Thai
4. Check footer — SUPPORT section removed, Refund Policy link present
5. Verify footer links navigate to correct pages

---

## Notes
- Policy content is based on Thai PDPA, Consumer Protection Act, and E-Transactions Act requirements
- Refund Policy aligns with existing Pricing FAQ: "14-day money-back guarantee on all paid plans"
- `.ms.resx` (Malay) files created with English content as placeholder
- These policies should eventually be reviewed by a Thai-qualified attorney
