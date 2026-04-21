# Malaysian Market Expansion - Brainstorm & Impact Analysis

## Context
MotoRent is a Thailand-focused motorbike rental POS system. We're expanding to support the Malaysian market (Langkawi, Penang, etc.). The goal is environment-variable-driven multi-market support without breaking the existing Thai deployment.

**User requirements:**
1. App name via `MOTO_ApplicationName` (already exists)
2. Company contact info in `appsettings.json`
3. Languages on/off via environment variable
4. Country via environment variable → drives currency, timezone, exchange providers

---

## Findings: What's Thailand-Specific Today

| Area | File(s) | Hardcoded? | Impact |
|------|---------|-----------|--------|
| **Supported cultures** | `Program.cs:335` — `["en", "th"]` | Yes | Language switcher & localization |
| **Language switcher UI** | `MainLayout.razor:38-55` — only EN/TH | Yes | Users can't pick Malay |
| **Default timezone** | `MotoRentRequestContext.cs:16` — `c_thailandTimezone = 7.0` | Yes | All date displays |
| **Default currency** | `Organization.cs` — `Currency = "THB"` | Yes | New org defaults |
| **Currency denominations** | `CurrencyDenominations.cs` — no MYR | Missing | Cash counting won't work for MYR |
| **Exchange rate providers** | `SuperRichProvider.cs`, `MamyExchangeProvider.cs` | Thai-only | No MY providers |
| **PromptPay payment** | `TillEnums.cs:103` — enum value | Thai-only | No DuitNow/TNG equivalent |
| **Fiuu payment gateway** | `FiuuPaymentService.cs` — `Country="TH"`, `Currency="THB"` | Yes | Payments fail for MY |
| **Thai address system** | `ThaiProvinces.cs`, `ThaiAddressSelector.razor`, `thailand-addresses.json` | Thai-only | No MY state/postcode picker |
| **Regional pricing presets** | `RegionalPresetService.cs` — 8 Thai regions, Thai festivals | Thai-only | No Langkawi/Penang presets |
| **License plate province** | `Vehicle.LicensePlateProvince` — expects Thai province | Thai-biased | MY uses state |
| **Legal T&C fields** | `Shop.TermsAndConditionsEn/Th` — no Malay | Missing | No BM terms field |
| **LINE Notify** | notification integration | Thai-popular | Less relevant for MY |
| **App branding** | `MotoRentPageTitle.razor`, `PublicLayout.razor`, `App.razor` — mixed "MotoRent"/"SafeNGo" | Yes | Branding inconsistency |
| **Dashboard label** | `Home.resx` — "THB collected today" | Yes | Wrong currency label for MY |
| **Malay .resx files** | ~51 of ~162 needed | Partial | Incomplete translations |

---

## Malaysian Market Specifics (from Malaysian Business Expert)

### Must-Know for Implementation

| Aspect | Malaysia | Thailand (current) |
|--------|---------|-------------------|
| **Currency** | MYR (RM) | THB (฿) |
| **Timezone** | UTC+8 (MYT) | UTC+7 (ICT) |
| **Language** | Bahasa Melayu (ms) | Thai (th) |
| **QR Payment** | DuitNow | PromptPay |
| **Top eWallet** | Touch 'n Go | - |
| **Online banking** | FPX (via Fiuu) | - |
| **Address hierarchy** | State → Postcode (16 states) | Province → District → Subdistrict (77 provinces) |
| **Phone format** | +60 1X-XXXX XXXX | +66 8X-XXX-XXXX |
| **Postcode** | 5 digits | 5 digits |
| **ID document** | MyKad (YYMMDD-PB-XXXX) | Thai ID (13 digits) |
| **Tax** | SST 6% (if revenue >RM500k) | No VAT on tourism |
| **Messaging app** | WhatsApp dominant | LINE dominant |
| **Deposit method** | Cash RM300-500 or passport | Cash 3000-5000฿ or passport |

### MYR Denominations (for cash counting)
Notes: RM100, RM50, RM20, RM10, RM5, RM1
Coins: 50sen, 20sen, 10sen, 5sen

### Malaysian Tourist Regions (for regional presets)
- **Langkawi** (Kedah) — duty-free island, European/Middle Eastern
- **Penang** (George Town) — heritage, digital nomads, Chinese tourists
- **Melaka** — UNESCO heritage, Singaporean weekenders
- **Johor Bahru** — Singapore border spillover
- **Cameron Highlands** — hill station escapes
- **Perhentian/Tioman** — diving islands

### Malaysian Holidays/Festivals (pricing impact)
- **Hari Raya Aidilfitri** (Eid) — biggest, +200-300% surge
- **Chinese New Year** — massive in Penang/Melaka, +200%
- **Deepavali** — moderate impact
- **School holidays** (Mar, May-Jun, Aug, Nov-Jan) — primary demand driver
- **Merdeka Day** (Aug 31) — domestic travel surge

---

## Thai Expert Risk Assessment

**PRESERVE these Thai-specific features** (do not generalize away):
- PromptPay integration — 60%+ of digital transactions
- Thai address hierarchy (Province→District→Subdistrict) — legally required
- Regional pricing presets with festival logic (Songkran, Loy Krathong) — years of business intelligence
- LINE Notify — 75% penetration in Thailand
- Thai license plate province field — required by police

**Safe to abstract:**
- Currency defaults → country config
- Timezone defaults → country config
- Exchange rate providers → pluggable per country
- Fiuu gateway country/currency → from config
- Language registration → env var driven

---

## Implementation Plan

### Phase 1: Country Configuration Foundation

**1.1 Add country & language env vars to `MotoConfig.cs`**
- File: `src/MotoRent.Domain/Core/MotoConfig.cs`
- Add: `MOTO_Country` (default: `"TH"`)
- Add: `MOTO_Languages` (default: `"en,th"`)
- Add: country helper that returns defaults for currency, timezone based on country code

```
MOTO_Country=TH → currency=THB, timezone=7, languages=en,th
MOTO_Country=MY → currency=MYR, timezone=8, languages=en,ms
```

**1.2 Add `CompanyInfo` section to `appsettings.json`**
- File: `src/MotoRent.Server/appsettings.json`
- New section with name, legal name, email, phone, address, website
- File: `src/MotoRent.Domain/Settings/CompanySettings.cs` (new — IOptions binding class)
- Register in `Program.cs` via `builder.Services.Configure<CompanySettings>(...)`

**1.3 Dynamic language registration in `Program.cs`**
- File: `src/MotoRent.Server/Program.cs:333-339`
- Replace hardcoded `["en", "th"]` with `MotoConfig.Languages`
- Default culture = first in list

**1.4 Dynamic language switcher in `MainLayout.razor`**
- File: `src/MotoRent.Client/Layout/MainLayout.razor:38-55`
- Build language options from `MotoConfig.Languages` instead of hardcoded EN/TH
- Map: `en→"English"`, `th→"ไทย"`, `ms→"Bahasa Melayu"`

### Phase 2: Currency & Timezone

**2.1 Add MYR denominations to `CurrencyDenominations.cs`**
- File: `src/MotoRent.Domain/Entities/CurrencyDenominations.cs`
- Add MYR: `[100, 50, 20, 10, 5, 1]` (notes only for till counting)
- Add MYR symbol: `"RM"`
- Add to `CurrenciesWithDenominations` array
- Also add SGD for JB border shops

**2.2 Update timezone default in `MotoRentRequestContext.cs`**
- File: `src/MotoRent.Server/Services/MotoRentRequestContext.cs:16`
- Replace `c_thailandTimezone = 7.0` with value from `MotoConfig` country helper

**2.3 Update Fiuu payment gateway**
- File: `src/MotoRent.Services/Payments/FiuuPaymentService.cs`
- Replace hardcoded `Country = "TH"` and `Currency = "THB"` with country config values

### Phase 3: Address System

**3.1 Create `MalaysianStates.cs`**
- File: `src/MotoRent.Domain/Lookups/MalaysianStates.cs` (new)
- 13 states + 3 federal territories

**3.2 Create `malaysia-addresses.json`**
- File: `src/MotoRent.Server/wwwroot/data/malaysia-addresses.json` (new)
- State → Postcode → City mapping

**3.3 Abstract address selector**
- Keep existing `ThaiAddressSelector.razor` untouched
- Create `MalaysianAddressSelector.razor` (new)
- Use country config to pick which component renders in forms

### Phase 4: Payment Methods

**4.1 Add Malaysian payment types to `TillEnums.cs`**
- File: `src/MotoRent.Domain/Entities/TillEnums.cs`
- Add: `DuitNow`, `TouchNGo` as new `TillTransactionType` values
- These are non-cash tracked types like existing `PromptPay`

**4.2 Country-aware payment method display**
- In till/payment UI components, filter available payment methods by country
- TH shows: Cash, Card, BankTransfer, PromptPay
- MY shows: Cash, Card, BankTransfer, DuitNow, TouchNGo, FPX

### Phase 5: Exchange Rates & Regional Presets

**5.1 BNM exchange rate provider**
- File: `src/MotoRent.Services/ExchangeRateProviders/BnmExchangeProvider.cs` (new)
- API: `https://api.bnm.gov.my/public/exchange-rate` (Bank Negara Malaysia, official)

**5.2 Malaysian regional presets** (future)
- File: `src/MotoRent.Services/MalaysiaRegionalPresetService.cs` (new)
- Langkawi, Penang regions with Hari Raya, CNY, school holiday pricing

### Phase 6: Branding & Polish

**6.1 Use `CompanySettings` in branding components**
- `MotoRentPageTitle.razor` — replace hardcoded "SafeNGo" with company name
- `PublicLayout.razor` — replace hardcoded "MotoRent"
- `App.razor` — meta tags from company settings
- `Contact.razor` — email from company settings

**6.2 Add `TermsAndConditionsMs` to Shop entity**
- File: `src/MotoRent.Domain/Entities/Shop.cs`
- SQL: `database/Tables/Rental.Shop.sql`

**6.3 Complete Malay .ms.resx files** (ongoing)
- Priority: tourist-facing pages, check-in/out, payment, receipt
- ~110 additional .ms.resx files needed for full parity

---

## Files Changed Summary

| Priority | File | Change |
|----------|------|--------|
| P1 | `src/MotoRent.Domain/Core/MotoConfig.cs` | Add Country, Languages, country helper |
| P1 | `src/MotoRent.Domain/Settings/CompanySettings.cs` | **New** — IOptions class |
| P1 | `src/MotoRent.Server/appsettings.json` | Add CompanyInfo section |
| P1 | `src/MotoRent.Server/Program.cs` | Dynamic culture registration, CompanySettings binding |
| P1 | `src/MotoRent.Client/Layout/MainLayout.razor` | Dynamic language switcher |
| P2 | `src/MotoRent.Domain/Entities/CurrencyDenominations.cs` | Add MYR, SGD denominations |
| P2 | `src/MotoRent.Server/Services/MotoRentRequestContext.cs` | Country-aware timezone default |
| P2 | `src/MotoRent.Services/Payments/FiuuPaymentService.cs` | Country-aware params |
| P3 | `src/MotoRent.Domain/Lookups/MalaysianStates.cs` | **New** — 16 states |
| P3 | `src/MotoRent.Server/wwwroot/data/malaysia-addresses.json` | **New** — address data |
| P3 | `src/MotoRent.Client/Components/Shared/MalaysianAddressSelector.razor` | **New** |
| P4 | `src/MotoRent.Domain/Entities/TillEnums.cs` | Add DuitNow, TouchNGo |
| P5 | `src/MotoRent.Services/ExchangeRateProviders/BnmExchangeProvider.cs` | **New** (future) |
| P6 | `src/MotoRent.Client/Components/Shared/MotoRentPageTitle.razor` | Use CompanySettings |
| P6 | `src/MotoRent.Client/Layout/PublicLayout.razor` | Use CompanySettings |
| P6 | `src/MotoRent.Domain/Entities/Shop.cs` | Add TermsAndConditionsMs |

---

## Environment Variable Configuration Examples

### Thailand (default, no changes needed)
```bash
# No env vars required - all defaults are Thai
MOTO_Country=TH
MOTO_Languages=en,th
MOTO_ApplicationName=SafeNGo
```

### Malaysia
```bash
MOTO_Country=MY
MOTO_Languages=en,ms
MOTO_ApplicationName=SafeNGo Malaysia
```

### Multi-language (both markets)
```bash
MOTO_Country=TH
MOTO_Languages=en,th,ms
```

---

## Verification Plan
1. Set `MOTO_Country=TH` — verify all Thai features work unchanged
2. Set `MOTO_Country=MY` — verify MYR currency, UTC+8 timezone, Malay language option
3. Language switcher shows only configured languages
4. Cash counting shows correct denominations for MYR
5. New org defaults to correct currency/timezone per country
6. Fiuu payments use correct country code
7. `dotnet build` passes with no errors
