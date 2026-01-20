# State: MotoRent Cashier Till

**Milestone:** Cashier Till & End of Day Reconciliation
**Started:** 2026-01-19

---

## Project Reference

**Core Value:** Business visibility and cash control - owners can see if their assets are profitable, where cash is leaking, and whether staff are handling money correctly.

**Current Focus:** Phase 5 in progress. Service layer complete with ManagerPinService and TillService void/refund operations. Ready for void dialog UI implementation.

**Key Constraints:**
- Tech stack: Blazor Server + WASM, .NET 10, SQL Server
- Multi-tenancy: Schema-per-tenant isolation
- Localization: English/Thai/Malay
- Mobile-first: PWA on tablets at desk

---

## Current Position

**Phase:** 5 of 9 (Refunds & Corrections)
**Plan:** 2 of 5 complete
**Status:** In progress

```
Milestone Progress: [######....] 68%
Phase 5 Progress:   [####......] 40%
```

**Last Activity:** 2026-01-20 - Completed 05-02-PLAN.md (Manager PIN Service)

**Next Action:** Execute 05-03-PLAN.md (Void Dialog & Workflow)

---

## Phase 5 Progress - In Progress

### Plan 05-01: Domain Entity Extensions - COMPLETE

| Task | Status | Commit |
|------|--------|--------|
| Task 1: Extend TillTransaction with void metadata | Done | f0d55cf |
| Task 2: Add refund and void transaction types | Done | 4585a41 |
| Task 3: Extend User with manager PIN fields | Done | 2269c5b |

**Key Deliverables:**
- TillTransaction: IsVoided, VoidedAt, VoidedByUserName, VoidReason, VoidApprovedByUserName, OriginalTransactionId, RelatedTransactionId
- TillTransactionType: OverpaymentRefund, VoidReversal
- User: ManagerPinHash, ManagerPinSalt, CanApproveVoids

### Plan 05-02: Manager PIN Service - COMPLETE

| Task | Status | Commit |
|------|--------|--------|
| Task 1: Create ManagerPinService | Done | 322f675 |
| Task 2: Extend TillService with void/refund operations | Done | c74323d |
| Task 3: Register ManagerPinService in DI | Done | d2a26cf |

**Key Deliverables:**
- ManagerPinService: PBKDF2 hashing, 3-attempt lockout, SetPinAsync, VerifyPin, IsLockedOut
- TillService: VoidTransactionAsync with compensating entries, RecordOverpaymentRefundAsync
- Self-approval prevention, bidirectional void linking

---

## Phase 4 Progress - COMPLETE

### Plan 04-01: Payment Terminal Layout - COMPLETE

| Task | Status | Commit |
|------|--------|--------|
| Task 1: Create PaymentTerminalPanel with two-column layout | Done | a0141d9 |
| Task 2: Add localization resources | Done | 8dfb476 |

**Key Deliverables:**
- `PaymentTerminalPanel.razor` - Two-column layout component (415 lines)
- Payment method tabs (Cash, Card, PromptPay, AliPay)
- Cash currency tabs (THB, USD, GBP disabled, EUR, CNY)
- Payment entry list with remove capability
- Summary section (Total Received, Change, Remaining)

### Plan 04-02: THB Keypad & Payment Input - COMPLETE

| Task | Status | Commit |
|------|--------|--------|
| Task 1: Create ThbKeypadPanel component | Done | 15a7075 |
| Task 2: Integrate THB keypad and DenominationEntryPanel | Done | 6bc8064 |
| Task 3: Add non-cash payment panels (Card, PromptPay, AliPay) | Done | b073784 |

**Key Deliverables:**
- `ThbKeypadPanel.razor` - Numeric keypad with quick amounts (144 lines)
- THB cash input via keypad with 100/500/1000/Remaining buttons
- Foreign currency input via DenominationEntryPanel with exchange rate
- Credit Card, PromptPay, AliPay panels with reference fields

### Plan 04-03: Complete Payment Flow - COMPLETE

| Task | Status | Commit |
|------|--------|--------|
| Task 1: Add Step 3 (Payment Terminal) to TillTransactionDialog | Done | b880d97 |
| Task 2: Implement Complete Payment flow with till recording | Done | 72657d6 |
| Task 3: Wire payment completion in TillTransactionDialog | Done | 96954d5 |

**Key Deliverables:**
- Three-step dialog flow (Search -> Items -> Payment)
- PaymentTerminalPanel integration with session context
- Till recording for all payment methods via TillService
- TransactionSearchResult extended with Payments and Change

---

## Performance Metrics

| Metric | Value | Notes |
|--------|-------|-------|
| Plans completed | 14 | Phase 1: 4; Phase 2: 3; Phase 3: 2; Phase 4: 3; Phase 5: 2 |
| Requirements done | 28/40 | +VOID-01 (manager PIN), +VOID-02 (void workflow) |
| Phases done | 4/9 | Phase 5 in progress |
| Blockers hit | 0 | - |

---

## Accumulated Context

### Key Decisions

| Decision | Rationale | Date |
|----------|-----------|------|
| Base currency THB | Thailand market, rental rates always in THB | 2026-01-19 |
| Change always in THB | Simplifies till reconciliation, matches tourist expectations | 2026-01-19 |
| Per-currency till tracking | Matches forex expertise, enables accurate reconciliation | 2026-01-19 |
| Scoped cart (1 booking/rental per receipt) | Not general POS, focused on rental workflow | 2026-01-20 |
| Single "New Transaction" entry point | Staff search for booking/rental rather than manual item entry | 2026-01-20 |
| Auto-detect transaction type | System determines booking deposit, check-in, or check-out from entity status | 2026-01-20 |
| Fullscreen item confirmation | Dedicated focus, reduces errors, shows all details before payment | 2026-01-20 |
| THB keypad, foreign denomination counting | Speed for THB, accuracy for foreign currency | 2026-01-20 |
| Split payments across methods/currencies | Tourists often mix cash, card, and currencies | 2026-01-20 |
| Manager PIN for void approval | Quick authorization without full login swap | 2026-01-20 |
| Voids preserved for audit trail | Accountability and reconciliation | 2026-01-20 |
| PaymentEntry as internal class | (04-01) Separate from ReceiptPayment for working state during collection | 2026-01-20 |
| GBP tab visible but disabled | (04-01) Per CONTEXT.md, GBP support deferred but shown for completeness | 2026-01-20 |
| Component composition for THB keypad | (04-02) ThbKeypadPanel as separate component for reusability | 2026-01-20 |
| Pre-fill remaining on method switch | (04-02) Faster workflow, staff often want exact remaining | 2026-01-20 |
| Required reference for AliPay only | (04-02) Card/PromptPay verifiable via terminal/app | 2026-01-20 |
| Step tracking via m_currentStep | (04-03) Simple state machine for 3-step dialog flow | 2026-01-20 |
| Record payments before callback | (04-03) Ensure till is updated before dialog closes | 2026-01-20 |
| Void soft-delete pattern | (05-01) IsVoided flag preserves transactions for audit trail | 2026-01-20 |
| Bidirectional void linking | (05-01) OriginalTransactionId <-> RelatedTransactionId for navigation | 2026-01-20 |
| Separate PIN from password | (05-01) OAuth users can still have manager approval PIN | 2026-01-20 |
| PBKDF2 static method | (05-02) .NET 10 deprecates constructor; use static Pbkdf2() | 2026-01-20 |
| In-memory lockout tracking | (05-02) Acceptable for MVP; can migrate to Redis/SQL later | 2026-01-20 |
| Bidirectional linking after save | (05-02) Compensating entry needs ID before linking | 2026-01-20 |

### Architecture Notes

**Phase 5 Components (In Progress):**
- TillTransaction extended with void metadata (7 fields)
- TillTransactionType with OverpaymentRefund, VoidReversal
- User with ManagerPinHash, ManagerPinSalt, CanApproveVoids
- ManagerPinService (202 lines) - PBKDF2 hashing, lockout logic
- TillService.VoidTransactionAsync - Compensating entry pattern
- TillService.RecordOverpaymentRefundAsync - THB cash refunds

**Phase 4 Components:**
- `PaymentTerminalPanel.razor` (928 lines) - Two-column payment terminal with all input modes
- `ThbKeypadPanel.razor` (144 lines) - Touch-friendly numeric keypad for THB entry
- `PaymentEntry` internal class - Working model for payment entries during collection
- Integration with TillService for payment recording
- Integration with ExchangeRateService for foreign currency conversion

**Phase 3 Components:**
- `TillTransactionDialog.razor` (1198 lines) - 3-step fullscreen dialog
- `TransactionLineItem.cs` - Working model for editable line items
- `TransactionSearchResult.cs` - Extended DTO with LineItems, Payments, Change

**Phase 2 Components:**
- `DenominationEntryPanel` - Reusable denomination counting panel
- `CurrencyBalancePanel` - Per-currency balance display
- `TillSession` extended with CurrencyBalances
- `TillService` extended with multi-currency methods

**Phase 1 Components:**
- `ExchangeRate` entity with BuyRate, Source, EffectiveDate
- `ExchangeRateService` - Rate management and conversion
- `ExchangeRatePanel` - Staff-facing rate display

### Deferred Ideas (Future TODO)

- Receipt designer (A4 layout customization)
- Walk-in sales mode (general POS without booking/rental)
- Configurable currencies per organization
- GBP and JPY currency support
- Card refund to original card (all refunds are THB cash for now)
- Threshold-based manager approval for large refunds

### TODOs

- [ ] Confirm with user: Is offline/PWA support critical for MVP?
- [ ] Confirm with user: What is the variance tolerance for auto-acceptance?
- [ ] Confirm with user: Should exchange rates expire automatically?

### Blockers

None currently.

---

## Session Continuity

**Last Session:** 2026-01-20 - Completed 05-02-PLAN.md (Manager PIN Service)

**Context for Next Session:**
- Phase 5 Plans 1-2 complete: domain + service layer done
- ManagerPinService ready for void approval dialogs
- TillService.VoidTransactionAsync ready for UI integration
- Ready for Plan 05-03: Void Dialog & Workflow

**Files to Review:**
- `.planning/phases/05-refunds-corrections/05-02-SUMMARY.md` - Just completed
- `.planning/phases/05-refunds-corrections/05-03-PLAN.md` - Next plan
- `src/MotoRent.Services/ManagerPinService.cs` - PIN service
- `src/MotoRent.Services/TillService.cs` - Void/refund operations

---

*Last updated: 2026-01-20*
