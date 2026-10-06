# P12-B TravelParty Start Operation Candidate

**Status:** exact-tip independently reviewed and validated; awaiting autonomous canonical promotion preflight. No canonical promotion has occurred.
**Implementation branch:** `codex/phase12/P12BTravelPartyStartOperationImplementation`
**Canonical base:** `29162cd0cf63e31f9542e612023a7256ac36ca4c`
**Code-bearing commit:** `2bc6d3264c76347eed21b70dcfcde98533f7aa66`
**Code-bearing tree:** `9043a8da8718a364f602eee56aaf48f83f06ad51`
**Reviewed design:** `codex/phase12/P12BTravelPartyStartOperationDesign` at `6709f00c190813858b6e20246e92fbaaab0864dc`, tree `3675044588dba2feefd7a0bebf4f8ed01ef26184`.
**Design review:** PASS; durable record is [PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_DESIGN_REVIEW.md](PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_DESIGN_REVIEW.md).
**Implementation review:** PASS on the exact code commit/tree; durable record is [PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_IMPLEMENTATION_REVIEW.md](PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_IMPLEMENTATION_REVIEW.md).

## Bounded implementation

The selected-profile runtime path opens the named operation `runtime.travel-party.start` after TravelParty preparation and before allocating a TravelParty ID. It registers the exact TravelParty allocator counter witness, checks prepared owners and local revision headroom, batches committed owner-section notifications into one shared mutation epoch, and closes the operation on every normal owner-thread exit.

The P12-bound runtime rejects a direct unwrapped TravelParty start before ID allocation or TravelParty, NPC travel, account, Knowledge, Event, or record-sequence writes. The unbound runtime keeps its existing behavior. Existing transaction order, charge/refund behavior, event semantics, compensation order, and Knowledge discovery timing remain unchanged.

Focused coverage includes exact allocator identity/schema/cardinality/revision, successful one-epoch start, direct-entry rejection, unbound behavior, later debit-commit rejection and refund, Event-store rejection compensation, post-allocation exception cleanup, wrong-thread and same-ID replacement failures, owner-capacity saturation, and post-commit notification failure.

## Review finding and correction

The first exact-tip review of the earlier candidate found one missing regression: a post-write notification fault needed to prove that the TravelParty start remains committed while the protocol faults closed and the operation scope exits. The added test initially exposed that disposal under a fault latch left the active operation count at one.

The candidate now releases the scope count when disposal is on the bound owner thread and the scope owner matches, even when the protocol has already faulted. Wrong-thread disposal still faults the protocol and leaves the active count in place. The regression verifies committed travel state is retained, the shared epoch does not advance on the failed notification, runtime context is cleared, and the active count returns to zero. The reviewer independently confirmed the narrow cleanup behavior and found no further source defect.

## Exact-tree validation evidence

All results below correspond to code-bearing commit `2bc6d3264c76347eed21b70dcfcde98533f7aa66`, tree `9043a8da8718a364f602eee56aaf48f83f06ad51`. The final focused rerun is the passing artifact; the earlier 15/16 pre-fix run is retained only as a diagnostic.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| P12TravelPartyStartOperationTests | 16/16 PASS | `47073E18879B0BA8813171607920AC5562A223B4E6118018A619FC87CA4AAE44` | `094DF1E6E37E27AD081907EDB8471D7DB9ED931C2C24699DEF0A28C4B6549575` |
| ContinuationCensusProtocolTests | 22/22 PASS | `F0D57E829A51573B7B4DEBB468B31B9D8062C20212750CFC67F4407AB000F22B` | `00D6C1EE72D9A463EC17CE00BE93344ADB0984D426C668EA9C286D6127207EE2` |
| NpcOwnerCommitInvalidationTests | 13/13 PASS | `97DB966D87A12729C820F86274D9621BC91C4CDC44F3979B82A68CB30DAA0913` | `835F208B1B61B31CBDF36CD3304089045C3992845360E8425347861CB8FA0552` |
| ALL EditMode | 2383/2383 PASS | `F3D51CB609193E5C86A5E350C6BA3BAE625EA3117574554CE60F507F815D62AE` | `D9953066EBEB9ADAB6EBB3344D931370FC103BDCBAF1B282C7D017D6B4E0B2B7` |
| Official Smoke | 5/5 PASS | `39DADDD986208CADD120EB383BF1115A8585E7EB1A9110A16C77E674767EBFDA` | `D37461042A987534E075D50B01EFE0917ECA8B3D5F6943FE7E39117AC4348D54` |
| `git diff --check` | PASS | — | — |

The compact exact-tree artifacts are archived at [P12BTravelPartyStart-final-validation.zip](../validation/P12BTravelPartyStart-notify-fix/P12BTravelPartyStart-final-validation.zip), SHA-256 `F3EF32E29C32B335684F130D60DFD3DC1B1352122B2342008A56EEE39CDF6A65`. It contains the five final XML/log pairs above. The pre-fix focused diagnostic is retained separately in that directory and is not counted as passing evidence. The earlier broader implementation archive remains [P12BTravelPartyStart-validation.zip](../validation/P12BTravelPartyStart/P12BTravelPartyStart-validation.zip) as historical evidence.

The independent reviewer verified the exact code diff, cleanup behavior, all five exact-tree result pairs and hashes, and `git diff --check`. See the implementation review record for the independent assessment.

## Limits

This is only the selected-profile normal group-start operation. Direct Expedition start/return, all TravelParty APIs, arbitrary standalone writers, P12-B complete owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, and Phase 12 closure remain outside this candidate.

P12-B remains INCOMPLETE. P12-A remains WAIT_DEPENDENCY. P13 remains BLOCKED.
