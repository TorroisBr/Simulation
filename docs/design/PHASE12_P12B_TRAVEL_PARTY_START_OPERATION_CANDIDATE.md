# P12-B TravelParty Start Operation Candidate

**Status:** validated implementation candidate; independent exact-tip code review is pending. No canonical promotion has occurred.
**Implementation branch:** codex/phase12/P12BTravelPartyStartOperationImplementation
**Exact base / current P12 canonical at implementation:** 29162cd0cf63e31f9542e612023a7256ac36ca4c
**Code-bearing commit:** d08e1c21ef335c6ef5a7ebbb7cb3d5a42598b91a
**Code-bearing tree:** a816f186960bb3e4c51df2ee307345cc0d852c79
**Reviewed design:** codex/phase12/P12BTravelPartyStartOperationDesign at 6709f00c190813858b6e20246e92fbaaab0864dc, tree 3675044588dba2feefd7a0bebf4f8ed01ef26184.
**Design review:** PASS; durable record is PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_DESIGN_REVIEW.md.

## Bounded implementation

The selected-profile runtime path now opens the named operation runtime.travel-party.start after TravelParty preparation and before allocating a TravelParty ID. It registers the exact TravelParty allocator counter witness, checks the prepared owners and local revision headroom, batches committed owner-section notifications into one shared mutation epoch, and closes the operation in a finally path.

The P12-bound runtime rejects a direct unwrapped TravelPartySystem start before ID allocation or TravelParty, NPC travel, account, Knowledge, Event, or record-sequence writes. The unbound runtime keeps its existing behavior. Existing transaction order, charge/refund behavior, event semantics, compensation order, and Knowledge discovery timing remain unchanged.

Focused coverage includes exact allocator identity/schema/cardinality/revision, successful one-epoch start, direct-entry rejection, unbound behavior, later debit-commit rejection and refund, Event-store rejection compensation, post-allocation exception cleanup, wrong-thread and same-ID replacement failures, and owner-capacity saturation.

## Validation evidence

The retained archive is docs/validation/P12BTravelPartyStart/P12BTravelPartyStart-validation.zip. It contains 44 XML/log artifacts from the focused implementation regressions and final gates.

Final exact-code-tree evidence for code-bearing commit d08e1c21ef335c6ef5a7ebbb7cb3d5a42598b91a, tree a816f186960bb3e4c51df2ee307345cc0d852c79:

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| P12TravelPartyStartOperationTests | 15/15 PASS | 1E556085A92FF271E5AE0E9A9560F4E7EDCDDC939FD1697830F3BC9AEB5BA4CA | 57FB4B25188CC381A82BD01B0B78927C91E2D9BDB78BE41AF0FC7106B71F1D17 |
| NpcOwnerCommitInvalidationTests | 13/13 PASS | 6E34E86653B4E5D7DCCD6520F10BC0AE3C3D087CBECFDC7CF54B593766B55A07 | 293B13012098278366738F01D3134C02C180FA05E428CA33B395AC605F07AE54 |
| ALL EditMode | 2382/2382 PASS | 83F659E49FD2EC7C31A51CAEE089613541E0DF926372B94B5D859A7A6D6794C4 | 234AAF7D1B1B21073F31DECEC6E47F364C6DC3D2692848679E97049566570618 |
| Official Smoke | 5/5 PASS | 9B9A60F825D7810C382AF454528098610010B841CD89927CE3C323648188F327 | AB7FA595A8C4E51940AEE04B31DC8C36F4D1A20F5F99F8D02E94DAE814C2812A |
| git diff --check | PASS | — | — |

The archive SHA-256 is F10E24C833AA4BF62A8C1AC7C385CC261DE3CC432E205EB32101E8AF14D7A60B.

The implementation regression sweep also passed TravelParty census 10/10, GroupTravel 32/32, MoneyAccount 27/27, NPC MoneyAccount census 10/10, NPC Inventory/City-presence 7/7, SpatialKnowledge census 16/16, runtime admission 37/37, runtime orchestration 12/12, bootstrap composition 21/21, TravelParty advance 10/10, solo travel start 11/11, GeneralizedSpatialTravel 18/18, SpatialPassageAuthority 13/13, and SpatialRoutePlanning 21/21. Those focused runs preceded the final test-only debit-refusal case and a formatting-only indentation adjustment; the final exact-tree ALL EditMode run re-executed all tests after both changes.

## Limits

This is only the selected-profile normal group-start operation. Direct Expedition start/return, all TravelParty APIs, arbitrary standalone writers, P12-B complete owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, and Phase 12 closure remain outside this candidate.

P12-B remains INCOMPLETE. P12-A remains WAIT_DEPENDENCY. P13 remains BLOCKED.
