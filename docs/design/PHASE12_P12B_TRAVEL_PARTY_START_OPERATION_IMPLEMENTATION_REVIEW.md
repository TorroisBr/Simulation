# P12-B TravelParty start operation — implementation review

**Result:** VALIDATED_CANDIDATE — PASS; no remaining actionable implementation findings.
**Canonical base reviewed:** `29162cd0cf63e31f9542e612023a7256ac36ca4c`.
**Exact code-bearing tip:** `2bc6d3264c76347eed21b70dcfcde98533f7aa66`.
**Exact code tree:** `9043a8da8718a364f602eee56aaf48f83f06ad51`.
**Implementation branch:** `codex/phase12/P12BTravelPartyStartOperationImplementation`.
**Reviewed design:** `6709f00c190813858b6e20246e92fbaaab0864dc`, with independent PASS recorded in [PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_DESIGN_REVIEW.md](PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_DESIGN_REVIEW.md).

## Findings

The implementation matches the bounded selected-profile `runtime.travel-party.start` contract. It binds the exact TravelParty allocator witness, preflights the required owners and revision headroom before allocating the TravelParty ID, batches committed owner notifications into one epoch, and rejects direct unwrapped P12-bound starts before domain writes. Existing transaction, event, compensation, refund, and discovery ordering is preserved.

The first review identified missing coverage for a notifier fault after a successful domain write. The revised test proves the start remains committed, the failed notification does not advance the epoch, the protocol faults closed, and the runtime context and active operation count are cleared. The corresponding `ExitOperation` adjustment releases the count only for the bound owner thread and matching scope; off-thread disposal still faults and leaves the operation counted active. This addresses the prior finding without broadening the protocol behavior. No new source defect was found.

The review was read-only and independent of implementation. It did not edit candidate files or rerun Unity; it checked the exact code diff, the final exact-tree artifacts, hashes, and the recorded diff-check result.

## Exact-tree validation checked

All hashes were verified against the artifacts archived at [P12BTravelPartyStart-final-validation.zip](../validation/P12BTravelPartyStart-notify-fix/P12BTravelPartyStart-final-validation.zip), SHA-256 `F3EF32E29C32B335684F130D60DFD3DC1B1352122B2342008A56EEE39CDF6A65`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| P12TravelPartyStartOperationTests | 16/16 PASS | `47073E18879B0BA8813171607920AC5562A223B4E6118018A619FC87CA4AAE44` | `094DF1E6E37E27AD081907EDB8471D7DB9ED931C2C24699DEF0A28C4B6549575` |
| ContinuationCensusProtocolTests | 22/22 PASS | `F0D57E829A51573B7B4DEBB468B31B9D8062C20212750CFC67F4407AB000F22B` | `00D6C1EE72D9A463EC17CE00BE93344ADB0984D426C668EA9C286D6127207EE2` |
| NpcOwnerCommitInvalidationTests | 13/13 PASS | `97DB966D87A12729C820F86274D9621BC91C4CDC44F3979B82A68CB30DAA0913` | `835F208B1B61B31CBDF36CD3304089045C3992845360E8425347861CB8FA0552` |
| ALL EditMode | 2383/2383 PASS | `F3D51CB609193E5C86A5E350C6BA3BAE625EA3117574554CE60F507F815D62AE` | `D9953066EBEB9ADAB6EBB3344D931370FC103BDCBAF1B282C7D017D6B4E0B2B7` |
| Official Smoke | 5/5 PASS | `39DADDD986208CADD120EB383BF1115A8585E7EB1A9110A16C77E674767EBFDA` | `D37461042A987534E075D50B01EFE0917ECA8B3D5F6943FE7E39117AC4348D54` |
| `git diff --check` | PASS | — | — |

The archive contains exactly the final five XML/log pairs listed above. A prior pre-fix focused run of 15/16 is retained as a diagnostic only and is not validation evidence for this candidate. `git diff --check origin/codex/phase12/canonical..2bc6d3264c76347eed21b70dcfcde98533f7aa66` is clean.

## Scope and limitations

This review covers only the normal selected-profile TravelParty group-start operation and exact code/tree identified above. It does not cover Expedition start/return, all TravelParty APIs, arbitrary standalone writers, complete P12-B owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase 12 closure.

P12-B remains INCOMPLETE. P12-A remains WAIT_DEPENDENCY. P13 remains BLOCKED. This review is not canonical promotion.
