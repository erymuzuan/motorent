# Research Summary: Multi-Currency Cashier Till

**Project:** MotoRent Cashier Till Enhancement
**Synthesized:** 2026-01-19
**Research Files:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md
**Overall Confidence:** HIGH

---

## Executive Summary

MotoRent requires multi-currency cash management for its vehicle rental POS system serving Thailand's tourist market. The existing codebase has a **well-architected till system** with TillSession, TillTransaction, and Receipt entities, plus a comprehensive TillService. Multi-currency support is **partially implemented** at the receipt level (ReceiptPayment tracks Currency, ExchangeRate, AmountInBaseCurrency), but the critical gap is that **TillSession tracks only aggregate THB amounts**.

The recommended approach is to **extend the existing architecture** rather than rebuild. The core enhancement is adding per-currency balance tracking to TillSession and storing exchange rates on each TillTransaction. No external libraries are needed - the existing .NET 10, Blazor Server, and SQL Server JSON column patterns fully support all requirements. The `SupportedCurrencies` class already defines THB, USD, EUR, GBP, CNY, JPY, AUD, and RUB.

The primary risks are exchange rate mismatches between transaction time and reconciliation time (causing false variances), and shared till access in chaotic tourist environments breaking accountability. Both are addressed by per-currency tracking with immutable transaction-level rates and strict session ownership enforcement.

---

## Key Findings

### From STACK.md: Technology Decisions

| Technology | Decision | Rationale |
|------------|----------|-----------|
| External currency libraries | DO NOT USE | `ReceiptPayment` already implements decimal + currency code pattern |
| External exchange rate APIs | DO NOT USE | Shops set their own buy/sell rates with margin; internet dependency risky |
| Event sourcing/CQRS | DO NOT USE | `TillTransaction` is already append-only; overkill complexity |
| SignalR real-time updates | EXTEND | `CommentHub.cs` pattern exists; apply same for till balance updates |
| `decimal` type | CONTINUE | Native .NET type with 28-29 significant digits; no floating-point errors |

**New entities required:**
- `ExchangeRate` - Organization-scoped with BuyRate/SellRate and effective dates
- `CurrencyBalance` - Embedded class for per-currency tracking in TillSession

### From FEATURES.md: Feature Priorities

**Table Stakes (Must Have):**
| Feature | Status | Priority |
|---------|--------|----------|
| Multi-Currency Float Tracking | GAP | HIGH |
| Exchange Rate Configuration | GAP | HIGH |
| Currency-Specific Cash Count at Close | GAP | HIGH |
| Change Calculation Display (foreign to THB) | GAP | HIGH |
| Multi-Currency Variance Tracking | GAP | HIGH |
| Session Management (open/close/verify) | IMPLEMENTED | - |
| Cash-In/Cash-Out Operations | IMPLEMENTED | - |
| Reconciliation (expected vs actual) | IMPLEMENTED | - |
| Receipt Generation (multi-currency payments) | IMPLEMENTED | - |

**Differentiators (Should Have):**
- Real-time rate display at payment
- Multi-currency receipt with THB equivalent
- Variance alerts for managers
- Till dashboard widget

**Anti-Features (Do NOT Build):**
- Complex split-till sharing (keep one-staff-per-session)
- Automatic rate fetching from external APIs
- Integrated accounting system
- Biometric authentication
- Cash drawer sensors

### From ARCHITECTURE.md: System Design

**Existing Architecture Strengths:**
- Session-based till model with clear lifecycle (Open -> Close -> Verify)
- Denormalized totals on TillSession for fast UI reads
- Cross-reference linking (TillTransaction -> Payment/Deposit/Rental)
- Embedded collections in JSON (ReceiptItem, ReceiptPayment)
- Computed `ExpectedCash` property

**Required Extensions:**

```csharp
// Add to TillSession
public List<CurrencyBalance> CurrencyBalances { get; set; } = [];
public List<CurrencyBalance> OpeningFloatByCurrency { get; set; } = [];
public List<CurrencyBalance> ActualCashByCurrency { get; set; } = [];

// Add to TillTransaction
public string Currency { get; set; } = SupportedCurrencies.THB;
public decimal ExchangeRate { get; set; } = 1.0m;
public decimal AmountInBaseCurrency { get; set; }
```

**Service Layer Additions:**
- `ExchangeRateService` - Rate management (get current buy/sell, set new rates, history)
- Extend `TillService` - Multi-currency transaction recording, per-currency reconciliation

### From PITFALLS.md: Critical Risks

| Pitfall | Severity | Mitigation |
|---------|----------|------------|
| Single-currency till tracking | CRITICAL | Per-currency balances on TillSession from Phase 1 |
| Exchange rate mismatch (transaction vs reconciliation) | CRITICAL | Store rate on every TillTransaction; per-currency reconciliation |
| Shared till access in chaotic environment | CRITICAL | Enforce handover workflow with mid-shift count |
| Offline duplicate transactions | CRITICAL | WebId deduplication; sync before session close |
| Denomination tracking blindness | MODERATE | Optional denomination count at close |
| Receipt-till linkage gaps | MODERATE | Ensure all payments create linked TillTransaction |
| Manager override without audit | MODERATE | Require reason for variance acceptance |

---

## Implications for Roadmap

Based on combined research, the recommended phase structure:

### Phase 1: Multi-Currency Core (Foundation)

**Rationale:** The domain model must support per-currency tracking from the start. Retrofitting is expensive and error-prone.

**Delivers:**
- `ExchangeRate` entity with buy/sell rates
- `CurrencyBalance` embedded class
- Extended `TillSession` with per-currency tracking
- Extended `TillTransaction` with currency/rate fields
- `ExchangeRateService` for rate management
- SQL migration scripts

**Features from FEATURES.md:** Multi-currency float tracking, exchange rate configuration

**Pitfalls to Avoid:**
- #1: Single-currency till tracking (address directly)
- #2: Exchange rate mismatch (store rate on transactions)
- #12: Timezone confusion (use `DateTimeOffset` consistently)

**Research Flag:** Standard patterns - no additional research needed

---

### Phase 2: Multi-Currency Operations

**Rationale:** With the domain model in place, build the operational workflows.

**Delivers:**
- Exchange rate management UI (`/settings/exchange-rates`)
- Multi-currency payment dialog (extend `TillReceivePaymentDialog.razor`)
- Currency drawer display on Till page
- Per-currency reconciliation at close (`TillCloseSessionDialog.razor`)
- Change calculation helper (accept foreign, show THB change)

**Features from FEATURES.md:** Currency-specific cash count, change calculation display, multi-currency variance tracking

**Pitfalls to Avoid:**
- #3: Shared access (enforce handover workflow)
- #10: Mid-session rate changes (rates immutable on transactions)
- #14: Void/refund without till impact (reversing transactions)

**Research Flag:** Standard patterns - UI extensions follow existing MudBlazor patterns

---

### Phase 3: Session Lifecycle Hardening

**Rationale:** Ensure accountability and handle edge cases.

**Delivers:**
- Session handover workflow (count + transfer)
- Manager verification dashboard (`/manager/till-verification`)
- Variance investigation drill-down
- Daily summary report with per-currency breakdown

**Features from FEATURES.md:** Manager override, variance alerts, shift handover report

**Pitfalls to Avoid:**
- #3: Shared access (handover flow)
- #6: Receipt-till linkage (ensure all flows link properly)
- #7: Manager override without audit (require variance reason)

**Research Flag:** May need `/gsd:research-phase` for manager dashboard UX patterns

---

### Phase 4: Offline Resilience (If PWA Required)

**Rationale:** Critical for shops with unreliable internet, but adds complexity.

**Delivers:**
- Offline transaction queuing
- Sync conflict detection
- Visual pending transaction indicators
- Mandatory sync before session close

**Features from FEATURES.md:** (PWA infrastructure)

**Pitfalls to Avoid:**
- #4: Offline duplicates (WebId deduplication)
- #9: Insufficient offline retention (mandatory sync)

**Research Flag:** NEEDS `/gsd:research-phase` - offline sync patterns require careful design

---

### Phase 5: Polish and Reporting (Post-MVP)

**Rationale:** Operational polish once core flows are stable.

**Delivers:**
- Denomination counting UI (optional)
- Currency mix reports
- Staff variance trend analysis
- Receipt reprint controls with watermark
- Counterfeit tracking

**Features from FEATURES.md:** Denomination counting, variance trend analysis, currency mix report

**Pitfalls to Avoid:**
- #5: Denomination blindness (address if needed)
- #11: Reprint abuse (watermark, reason)
- #15: Counterfeit tracking

**Research Flag:** Standard patterns

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Codebase analysis shows all required patterns exist; no external dependencies needed |
| Features | MEDIUM-HIGH | Industry research + codebase gap analysis; Thailand-specific context well-documented |
| Architecture | HIGH | Existing architecture is sound; extensions are additive, not disruptive |
| Pitfalls | HIGH | Team has forex expertise; common POS pitfalls well-documented in sources |

### Gaps to Address During Planning

1. **Day boundary definition:** Does business day end at midnight or shop close? Affects which date transactions belong to.

2. **Float currency composition:** Which currencies should shops stock? Is there a default recommendation?

3. **Rate refresh frequency:** How often should shops update rates? Daily? Per-shift?

4. **Offline scope:** Is PWA offline support required for MVP, or can it be deferred?

5. **Denomination granularity:** Do shops need full denomination tracking, or just total per currency?

---

## Open Questions for User

1. **Is offline/PWA support critical for MVP?** This significantly impacts Phase 4 scope and overall complexity.

2. **What is the variance tolerance?** Should small variances (e.g., 5-10 THB rounding) be auto-accepted?

3. **Do shops need denomination tracking at open/close?** Or is per-currency total sufficient?

4. **Should exchange rates expire automatically?** e.g., require daily refresh vs. rates valid until changed.

5. **Is multi-device till access needed?** Current design assumes one device per session; multi-device adds sync complexity.

---

## Aggregated Sources

### From STACK.md
- Existing codebase: TillSession.cs, TillTransaction.cs, ReceiptPayment.cs, CommentHub.cs
- .NET decimal precision documentation

### From FEATURES.md
- POS System Features: SelectHub, Shopify, Business.com
- Cash Drawer Management: Star Micronics, Lightspeed, Microsoft Dynamics
- Multi-Currency POS: Posytude, ERPLY, ConnectPOS
- Thailand Payments: ConnectPOS, Statista

### From ARCHITECTURE.md
- Existing codebase: TillService.cs, ReceiptService.cs, Till.razor, SQL schemas

### From PITFALLS.md
- Cash Handling: APG, Solink, Ramp
- Multi-Currency: Cloudbeds, HedgeStar, Controllers Council
- Offline POS: Tillpoint, ConnectPOS, MDN PWA docs
- Staff Accountability: OneHub POS, Xenia, ZZap

---

**Synthesis Complete.** Ready for roadmap creation.
