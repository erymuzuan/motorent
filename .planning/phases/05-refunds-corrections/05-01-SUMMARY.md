---
phase: 05-refunds-corrections
plan: 01
subsystem: domain
tags: [entity, void, refund, pin, audit]

dependency_graph:
  requires: []
  provides:
    - TillTransaction void metadata (IsVoided, VoidedAt, VoidReason, etc.)
    - OverpaymentRefund and VoidReversal transaction types
    - User manager PIN fields (ManagerPinHash, ManagerPinSalt, CanApproveVoids)
  affects:
    - 05-02 (ManagerPinService uses User.ManagerPinHash/Salt)
    - 05-03 (ManagerPinDialog uses CanApproveVoids)
    - 05-04 (VoidTransactionDialog uses TillTransaction void fields)
    - 05-05 (OverpaymentRefundDialog uses OverpaymentRefund type)

tech_stack:
  added: []
  patterns:
    - Soft-delete with audit trail (IsVoided flag preserves transactions)
    - Bidirectional linking (OriginalTransactionId <-> RelatedTransactionId)
    - Computed property for role check (CanApproveVoids)

key_files:
  created: []
  modified:
    - src/MotoRent.Domain/Entities/TillTransaction.cs
    - src/MotoRent.Domain/Entities/TillEnums.cs
    - src/MotoRent.Domain/Core/User.cs

decisions:
  - id: void-soft-delete
    choice: Preserve voided transactions with IsVoided flag
    rationale: Audit trail requirement - voided transactions must be visible in reports
  - id: void-bidirectional-link
    choice: OriginalTransactionId and RelatedTransactionId for two-way navigation
    rationale: Easy traversal from original to reversal and vice versa
  - id: separate-pin-from-password
    choice: ManagerPinHash/Salt separate from HashedPassword/Salt
    rationale: OAuth users (Google/Microsoft) can still have void approval PIN

metrics:
  duration: 5 minutes
  completed: 2026-01-20
---

# Phase 5 Plan 01: Domain Entity Extensions Summary

**One-liner:** Void metadata fields on TillTransaction, OverpaymentRefund/VoidReversal transaction types, manager PIN fields on User.

## What Was Built

Extended three domain entities to support refund and void operations:

### TillTransaction Void Metadata
| Field | Type | Purpose |
|-------|------|---------|
| IsVoided | bool | Flag indicating transaction was voided |
| VoidedAt | DateTimeOffset? | When the void occurred |
| VoidedByUserName | string? | Staff who initiated void |
| VoidReason | string? | Reason for voiding |
| VoidApprovedByUserName | string? | Manager who approved (must differ from initiator) |
| OriginalTransactionId | int? | Links compensating entry to original |
| RelatedTransactionId | int? | Links original to its reversal |

### TillTransactionType Additions
| Value | Description |
|-------|-------------|
| OverpaymentRefund | Refund when customer pays more than owed |
| VoidReversal | Compensating entry reversing voided transaction |

### User Manager PIN Fields
| Field | Type | Purpose |
|-------|------|---------|
| ManagerPinHash | string? | PBKDF2-hashed 4-6 digit PIN |
| ManagerPinSalt | string? | Salt for PIN hashing |
| CanApproveVoids | bool (computed) | True if PIN is set |

## Task Completion

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Extend TillTransaction with void metadata | f0d55cf | TillTransaction.cs |
| 2 | Add refund and void transaction types | 4585a41 | TillEnums.cs |
| 3 | Extend User with manager PIN fields | 2269c5b | User.cs |

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

1. **Void as soft-delete**: Voided transactions preserved with IsVoided=true for audit trail
2. **Bidirectional linking**: Both original and reversal transactions reference each other
3. **Separate PIN from password**: Manager PIN is independent of login credentials (OAuth users can have PINs)

## Verification Results

- [x] `dotnet build src/MotoRent.Domain` - compiles without errors
- [x] TillTransaction has all 7 void-related fields
- [x] TillTransactionType has OverpaymentRefund and VoidReversal
- [x] User has ManagerPinHash, ManagerPinSalt, CanApproveVoids

## Next Phase Readiness

**Dependencies provided for:**
- Plan 05-02: ManagerPinService will use User.ManagerPinHash/ManagerPinSalt
- Plan 05-03: ManagerPinDialog will check User.CanApproveVoids
- Plan 05-04: VoidTransactionDialog will populate TillTransaction void fields
- Plan 05-05: OverpaymentRefundDialog will use OverpaymentRefund transaction type

**No blockers.**
