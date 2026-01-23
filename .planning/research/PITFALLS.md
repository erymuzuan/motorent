# Domain Pitfalls: Cashier Till with Multi-Currency Support

**Domain:** Cashier Till / POS Cash Management for Vehicle Rental
**Context:** Thailand tourism (Phuket, Krabi), accepting THB/USD/EUR/CNY, staff turnover, offline PWA
**Researched:** 2026-01-19
**Confidence:** HIGH (team has forex expertise + common patterns well-documented)

---

## Critical Pitfalls

Mistakes that cause rewrites, financial loss, or major operational failures.

### Pitfall 1: Single-Currency Till Tracking for Multi-Currency Cash

**What goes wrong:** The current `TillSession` entity tracks `TotalCashIn`, `TotalCashOut`, `ExpectedCash`, and `ActualCash` as single `decimal` values in THB. When staff receive USD 100 + EUR 50 + THB 1,500 in cash during a shift, they cannot accurately reconcile because:
- Exchange rates used at transaction time may differ from rates at reconciliation
- Physical cash in drawer is in multiple currencies, but expected is single-currency
- Staff cannot prove they have "correct" USD count vs THB count

**Why it happens:** Initial design treats multi-currency as a payment-level concern (which `ReceiptPayment` handles correctly) but not as a till-management concern. Developers assume conversion to THB at transaction time is sufficient.

**Consequences:**
- Reconciliation variances that are not real shortages (exchange rate drift)
- Impossible to track which currency is short/over
- Audit trail breaks down ("I had the right amount of USD!")
- Potential for gaming the system by manipulating which currency to count

**Prevention:**
- Add per-currency tracking to `TillSession`:
  - `CurrencyBalances: Dictionary<string, TillCurrencyBalance>` or separate `TillCurrencyBalance` collection
  - Track opening float, cash in, cash out, actual count PER currency
- Reconciliation compares per-currency, THEN converts to THB for variance reporting
- Store exchange rate snapshot at session close for historical accuracy

**Detection (Warning Signs):**
- Till variance explanations frequently mention "but the USD was correct"
- Staff reluctant to accept foreign currency payments
- Increasing variance amounts correlating with foreign tourist season

**Phase to Address:** Phase 1 - Core domain model must support this from the start

---

### Pitfall 2: Exchange Rate at Transaction vs. Reconciliation Mismatch

**What goes wrong:** Customer pays USD 50 at 10:00 AM (rate: 35.20). Session closes at 6:00 PM (rate: 35.45). System uses different rate for:
- Recording the payment (transaction rate)
- Calculating expected cash (may use current rate or transaction rate)
- Converting actual cash count to THB (current rate)

Result: 50 USD shows as 1,760 THB expected but converts to 1,772.50 THB actual = apparent 12.50 THB overage.

**Why it happens:**
- No clear policy on which rate applies when
- Multiple rate sources (manual entry, auto-fetch, cached)
- Developers assume "just convert everything to THB" solves the problem

**Consequences:**
- False variances (over/short) that are purely exchange rate artifacts
- Inability to determine if actual shortage occurred
- Disputes between staff and management
- Gaming: staff could time foreign currency acceptance based on rate movements

**Prevention:**
- **Lock rate at transaction time:** Store `ExchangeRate` on each `TillTransaction` involving foreign currency
- **Track physical currency units:** Expected USD = 50, Actual USD = 50 = balanced, regardless of THB conversion
- **Report variances per-currency:** USD variance, EUR variance, then aggregate THB equivalent
- **Snapshot rates at session close:** Store rates used for reconciliation calculations

**Detection (Warning Signs):**
- Variance amounts cluster around typical exchange rate spread values (10-30 THB)
- Variances increase on volatile forex days
- Staff complaints about "the rate changed on me"

**Phase to Address:** Phase 1 (core model) and Phase 2 (reconciliation flows)

---

### Pitfall 3: Shared Till Access in Chaotic Environment

**What goes wrong:** In tourist shop environments:
- Multiple staff share one physical cash drawer
- Staff forget to log out, colleague uses their session
- Handover happens without proper count
- "Just put it in the drawer, I'll record later"

Result: Accountability breaks down. When variance found, 3 people worked that drawer.

**Why it happens:**
- Pressure to serve customers quickly (tourist rush)
- Staff turnover means training gaps
- Physical drawer sharing is convenient
- System allows session sharing by not enforcing controls

**Consequences:**
- Cannot identify who caused shortage
- Honest staff blamed for others' mistakes/theft
- Management cannot take corrective action
- Culture of "drawer problems are nobody's problem"

**Prevention:**
- **One session per staff per shift:** Already enforced in `CanOpenSessionAsync()` - keep this
- **Require session handover:** If staff A hands drawer to staff B mid-shift, force:
  - A performs mid-shift reconciliation (counts drawer)
  - A's session closes with that count
  - B opens new session with that count as opening float
- **UI enforcement:** Make it HARD to transact without active session
- **Quick handover workflow:** If count takes too long, staff skip it. Design for 2-minute count.

**Detection (Warning Signs):**
- Same session open for 10+ hours
- Transactions recorded by user different from session owner
- High variance correlating with multi-staff shifts

**Phase to Address:** Phase 2 (session lifecycle and handover flows)

---

### Pitfall 4: Offline Transactions Without Conflict Resolution

**What goes wrong:** Shop has 2 devices. Both go offline. Both record transactions. Both come online:
- Device A recorded USD 50 payment
- Device B recorded the same customer's THB payment (customer paid at both counters?)
- Or: Device A drop of 5,000 THB, Device B sees balance and does another drop

With cash, duplicates cause real money problems - either drawer is short or bank deposit doesn't match.

**Why it happens:**
- Offline-first PWA design doesn't plan for multi-device scenarios
- Cash transactions seem "safe" because no authorization needed
- Developers assume single-device usage

**Consequences:**
- Double-counted revenue
- Missing cash (drop recorded twice, only one physical drop)
- Reconciliation nightmare
- Customer disputes (charged twice)

**Prevention:**
- **Optimistic UI with pending status:** Offline transactions show as "pending sync"
- **Unique transaction IDs (WebId):** Already in `Entity` base class - ensure used for deduplication
- **Sync conflict detection:** On sync, check for:
  - Same RentalId + similar amount + similar time = potential duplicate
  - Multiple drops in quick succession = needs verification
- **Require confirmation for large cash movements:** Drops > 10,000 THB require manager code (which needs online validation)
- **Device affinity for sessions:** One session = one device. Second device cannot transact on that session.

**Detection (Warning Signs):**
- Duplicate transactions appearing after sync
- Revenue totals don't match receipts
- "I only did one drop" complaints

**Phase to Address:** Phase 3 (offline architecture and sync)

---

### Pitfall 5: Denomination Tracking Blindness

**What goes wrong:** Till shows "5,000 THB expected" but doesn't know if that's:
- 5 x 1,000 THB notes
- 10 x 500 THB notes
- 50 x 100 THB notes

When customer needs 500 THB change and drawer only has 1,000s, staff gives wrong change or refuses sale. System cannot help because it doesn't track denominations.

For multi-currency this is worse:
- USD 100 bills vs USD 20 bills have different usefulness
- EUR/CNY denomination standards differ
- Change-making across currencies is complex

**Why it happens:**
- Denomination tracking seen as "nice to have"
- Adds complexity to every transaction
- Staff resist entering denomination counts

**Consequences:**
- Cannot provide optimal change
- Staff make incorrect change (loss)
- Cannot determine correct float composition
- Bank deposit verification harder

**Prevention:**
- **Opening float by denomination:** At session start, count each denomination
- **Optional denomination tracking on close:** At minimum, require denomination count at reconciliation
- **Denomination alerts:** Warn when low on small denominations
- **Suggested float composition:** Based on typical transaction patterns

**But be pragmatic:**
- Don't require denomination on every transaction (too slow)
- Make it quick (button grid, not number entry)
- Focus on open/close, not mid-shift

**Detection (Warning Signs):**
- Staff frequently going to safe for change
- Customer complaints about "no change"
- Large denomination accumulation in drawer

**Phase to Address:** Phase 4 (reconciliation enhancement - not critical path)

---

## Moderate Pitfalls

Mistakes that cause delays, technical debt, or user friction.

### Pitfall 6: Receipt-Till Linkage Without Transaction Mapping

**What goes wrong:** `Receipt` has `Payments[]` with multi-currency support. `TillSession` has aggregate totals. No entity explicitly links "this receipt payment affected the till this way."

When investigating a variance:
- "Show me all transactions that added to TillCashIn" - need to query Receipts
- "What receipt is this TillTransaction from?" - only have loose references

**Prevention:**
- `TillTransaction` already has `PaymentId`, `DepositId`, `RentalId` - ensure these are always populated
- Consider adding `ReceiptId` to `TillTransaction` for direct lookup
- Build variance investigation UI that can drill from session -> transaction -> receipt

**Phase to Address:** Phase 2 (ensure all payment flows create proper TillTransaction links)

---

### Pitfall 7: Manager Override Without Audit Trail

**What goes wrong:** Manager can:
- Verify sessions with variance without explanation
- Adjust totals "to make it balance"
- Approve suspicious transactions

If no audit trail, abuse goes undetected.

**Prevention:**
- Current design has `VerifiedByUserName`, `VerifiedAt`, `VerificationNotes` - good
- Add: `VarianceAcknowledgementReason` (required if variance > threshold)
- Add: Adjustment transactions (cannot edit amounts, must create offsetting entry)
- Report: Manager override frequency and variance acceptance patterns

**Phase to Address:** Phase 2 (reconciliation) and Phase 5 (reporting)

---

### Pitfall 8: Exchange Rate Source Unreliability

**What goes wrong:** System fetches rates from API. API is down, returns stale rate, or has different rate than tourist shop's posted rates.

Result: Customer sees "35.00" on shop board, system uses "35.20" from API, dispute ensues.

**Prevention:**
- **Primary: Manual rate entry:** Shop sets today's rates each morning
- **Secondary: API fetch with manual override:** Can auto-fetch but staff reviews/adjusts
- **Store rate source:** "Manual", "API", "Adjusted"
- **Rate validity period:** Rates expire after X hours, require refresh
- **Allow per-transaction rate override:** For customer disputes, manager can approve different rate

**Phase to Address:** Phase 1 (rate infrastructure) and Phase 2 (UI for rate management)

---

### Pitfall 9: Insufficient Offline Data Retention

**What goes wrong:** PWA offline storage fills up or gets cleared:
- Browser storage quota exceeded
- User clears browser data
- Device switch without sync

Unsynced transactions lost permanently.

**Prevention:**
- **Mandatory sync before closing session:** Cannot close session while offline transactions pending
- **Visual indicators:** Show number of unsynced transactions prominently
- **Storage monitoring:** Warn when approaching quota
- **Critical transaction prioritization:** Payments sync first, metadata later
- **Export fallback:** If sync fails repeatedly, export to file for manual recovery

**Phase to Address:** Phase 3 (offline architecture)

---

### Pitfall 10: Mid-Session Rate Changes

**What goes wrong:** Session opens at 9 AM with USD rate 35.00. At 2 PM, shop changes rate to 34.50. Transactions before 2 PM used old rate, transactions after use new rate. At close:
- Expected cash calculation uses... which rate?
- Historical transactions show... which rate?

**Prevention:**
- **Rate at transaction is immutable:** Each `TillTransaction` stores its rate
- **Session expected calculation uses transaction rates:** Sum of (amount * rate at transaction time)
- **Rate change creates audit entry:** "Rate changed from X to Y at time T"
- **Report shows rate used for each transaction**

**Phase to Address:** Phase 1 (ensure rate stored on transaction) and Phase 2 (reconciliation logic)

---

## Minor Pitfalls

Annoyances that are fixable but waste time if not anticipated.

### Pitfall 11: Receipt Reprint Without Control

**What goes wrong:** Staff reprint receipts freely. Customer claims didn't receive receipt, gets reprint, uses both for expense claims or disputes.

**Prevention:**
- `ReprintCount` already exists on `Receipt` - good
- Add: Require reason for reprint
- Add: "REPRINT" watermark on reprinted receipts
- Limit: Max reprints without manager override

**Phase to Address:** Phase 4 (receipt management polish)

---

### Pitfall 12: Time Zone Confusion

**What goes wrong:** Tourist from China makes payment at 23:30 Thailand time. Their receipt shows their timezone (00:30 next day). Reconciliation date mismatches.

Shop operates across midnight; which day does 00:30 transaction belong to?

**Prevention:**
- **All times in Thailand timezone (UTC+7):** `DateTimeOffset` with explicit timezone
- **Day boundary at shop close, not midnight:** If shop closes at 2 AM, that's still "yesterday's" business day
- **Session defines the date:** Transaction belongs to session's opening date, not transaction timestamp

**Phase to Address:** Phase 1 (ensure `DateTimeOffset` used consistently, define day boundary rules)

---

### Pitfall 13: Currency Rounding Accumulation

**What goes wrong:**
- USD payment of $14.99 at rate 35.123 = 526.49277 THB, rounded to 526.49
- 100 such transactions = 0.00277 * 100 = 0.277 THB lost to rounding
- Over a month with high volume, rounding errors accumulate

Thailand also has no coins below 25 satang (0.25 THB), so cash rounding applies.

**Prevention:**
- **Standard rounding rules:** Define and document (e.g., banker's rounding)
- **Track rounding as separate line:** Small "rounding adjustment" entry keeps books clean
- **Accept rounding variance:** Configure acceptable rounding tolerance (e.g., 5 THB per day)

**Phase to Address:** Phase 2 (calculation precision) and Phase 4 (reconciliation tolerance configuration)

---

### Pitfall 14: Void/Refund Without Till Impact

**What goes wrong:** Receipt voided, but TillTransaction already recorded. Cash balance now wrong.

Or: Refund issued but not recorded as cash out. Drawer shows over.

**Prevention:**
- **Void creates reversing transaction:** Voiding receipt X creates TillTransaction "Void of Receipt X" as cash out
- **Refund workflow enforces till recording:** Cannot complete refund without TillTransaction
- **Cross-reference validation:** Reconciliation report flags receipts without matching till entries

**Phase to Address:** Phase 2 (transaction lifecycle, ensure void/refund flows update till)

---

## Thailand/Tourism-Specific Pitfalls

### Pitfall 15: Counterfeit Currency Detection Not Tracked

**What goes wrong:** Staff accepts counterfeit note (common with USD/EUR in tourist areas). At reconciliation, authentic cash is short by that amount. No record that counterfeit was received.

**Prevention:**
- **Counterfeit transaction type:** Record suspected counterfeits immediately (removed from drawer)
- **Counterfeit flag on foreign currency transactions:** Checkbox for "verified with UV/pen"
- **Training record:** Track which staff completed counterfeit detection training

**Phase to Address:** Phase 4 (operational polish, not critical for MVP)

---

### Pitfall 16: Tourist Season Float Management

**What goes wrong:** During high season, need more USD/EUR float. During low season, excess foreign currency sits idle.

**Prevention:**
- **Float recommendation engine:** Based on historical currency mix by season
- **Cross-shop currency transfer:** If Shop A has excess USD, transfer to Shop B
- **Bank exchange planning:** Schedule currency exchanges based on forecast

**Phase to Address:** Phase 5 (reporting and analytics, post-MVP)

---

## Phase-Specific Warnings Summary

| Phase | Topic | Likely Pitfall | Mitigation |
|-------|-------|----------------|------------|
| Phase 1 | Core Domain Model | Single-currency till (#1) | Design per-currency tracking from start |
| Phase 1 | Exchange Rates | Rate storage (#2) | Store rate on every foreign currency transaction |
| Phase 1 | Time Handling | Timezone confusion (#12) | Use `DateTimeOffset`, define day boundary |
| Phase 2 | Session Lifecycle | Shared access (#3) | Enforce handover workflow |
| Phase 2 | Reconciliation | Rate mismatch (#2) | Per-currency reconciliation, snapshot rates |
| Phase 2 | Void/Refund | Till not updated (#14) | Reversing transactions |
| Phase 3 | Offline Sync | Duplicate transactions (#4) | WebId deduplication, conflict detection |
| Phase 3 | Data Retention | Lost transactions (#9) | Mandatory sync before close |
| Phase 4 | Denomination | Change problems (#5) | Optional denomination tracking on close |
| Phase 4 | Receipts | Reprint abuse (#11) | Watermark, reason required |
| Phase 5 | Reporting | Variance patterns (#7) | Manager override audit reports |

---

## Sources

### Cash Handling & POS Best Practices
- [APG Cash Handling Best Practices](https://apgsolutions.com/cash-handling-best-practices/)
- [Star Micronics: How to Balance Cash Drawers](https://starmicronics.com/blog/how-to-balance-cash-drawers-quickly-and-accurately/)
- [Appriss Retail: Cash Register Discrepancies](https://apprissretail.com/blog/getting-to-the-bottom-of-register-discrepancies/)
- [Solink: Top 19 Cash Handling Procedures](https://solink.com/resources/cash-handling-procedures/)
- [Ramp: Cash Handling Policy Template](https://ramp.com/blog/cash-handling-policy-template)

### Multi-Currency and Forex
- [Cloudbeds: Multi-Currency FAQ](https://myfrontdesk.cloudbeds.com/hc/en-us/articles/360058997873-Multi-Currency-FAQ)
- [Cloudbeds: Currency Rounding](https://myfrontdesk.cloudbeds.com/hc/en-us/articles/6662923736859-Currency-Rounding-Everything-you-need-to-know)
- [HedgeStar: 5 Best Practices to Avoid Foreign Currency Transaction Errors](https://hedgestar.com/single-post/2024/12/19/5-best-practices-to-avoid-errors-in-foreign-currency-transactions/)
- [Controllers Council: Multi-Currency Financial Management Challenges](https://controllerscouncil.org/navigating-the-challenges-of-multi-currency-financial-management/)

### Thailand POS Context
- [ConnectPOS: POS Systems in Thailand](https://www.connectpos.com/top-5-pos-systems-in-thailand/)
- [ConnectPOS: Essential Features for Thai Retail](https://www.connectpos.com/essential-features-of-pos-for-retail-chains-in-thailand/)

### Offline POS & PWA Sync
- [Tillpoint: POS System Offline Transactions](https://www.tillpoint.com/pos-system-offline-transactions/)
- [ConnectPOS: How Offline POS Works](https://www.connectpos.com/how-does-offline-pos-work-a-simple-explanation/)
- [MDN: Offline and Background Operation](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Offline_and_background_operation)
- [Bits and Pieces: Design Patterns for Offline First Web Apps](https://blog.bitsrc.io/design-patterns-for-offline-first-web-apps-5891a4b06f3a)

### Cash Float & Denomination
- [RetailDogma: Cash Float in Retail](https://www.retaildogma.com/cash-float/)
- [KORONA POS: How to Count the Till](https://koronapos.com/blog/count-the-till-cash-handling/)
- [PayComplete: What is a Cash Float](https://paycomplete.com/what-is-a-cash-float-in-retail/)

### Staff Accountability
- [OneHub POS: POS Fraud Prevention](https://www.onehubpos.com/blog/pos-fraud-prevention-how-to-stop-employee-theft-before-profits-disappear)
- [Xenia: Mastering Cash Handling Procedures](https://www.xenia.team/articles/cash-handling-procedures-retail)
- [ZZap: Cash Handling Policies and Procedures](https://www.zzap.com/cash-handling-policies-and-procedures-with-a-policy-example/)
