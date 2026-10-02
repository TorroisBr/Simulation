# P12-B Direct NPC Owner Invalidation Implementation Candidate

**Status:** validated implementation candidate; canonical promotion is pending.

## Boundary and source

This bounded candidate connects direct writes to already-censused per-NPC MoneyAccountRuntime and InventoryRuntime owners to the partial P12 mutation epoch. It retains the approved direct-owner scope and does not implement an outer money-transfer operation.

- P12 canonical base: 2f7c7422812de40aa1223e8310dcbd9f5d8ca474
- Exact code candidate: c49f957e45c3059231e9ec66e4010a7c3a389988
- Exact tested tree: 627af2fbd7f93e0025106ee5ba87e72bae6c4ed2
- Candidate branch: codex/phase12/P12BDirectNpcOwnerInvalidationCandidate
- Exact-tip independent review: PASS; see PHASE12_P12B_DIRECT_NPC_OWNER_INVALIDATION_REVIEW.md.

The candidate binds the exact rostered NPC owner instances; checks owner-thread, local revision, and shared protocol baseline before each bound commit; advances the partial epoch after successful direct debit, credit, add, or removal; groups Inventory aliases so one logical commit emits one notification; and reconciles owner bindings when the NPC roster changes. Calls inside already instrumented NPC-trade/Open-market scopes avoid duplicate leaf notifications. A committed owner mutation remains committed if epoch advancement saturates; the protocol faults closed and the established domain result is preserved.

## Validation

All results below were run against the exact tested tree above. Focused suites and required regression gates passed:

| Suite | Result |
|---|---:|
| NpcOwnerCommitInvalidationTests | 13/13 |
| Economy transaction | 45/45 |
| MoneyAccount owner/census | 27/27 |
| NPC census | 10/10 |
| Inventory census | 7/7 |
| Expedition | 32/32 |
| SimulationRuntimeAdmissionTests | 25/25 |
| ALL EditMode | 2149/2149 |
| Official Smoke | 5/5 |
| git diff --check | PASS |

Retained Unity result XML files and SHA-256:

| Result | Artifact | SHA-256 |
|---|---|---|
| Focused NPC owner invalidation | Library/ValidationResults/P12DirectNpcOwnerInvalidation/EditMode-20261002-013323-0c96bc4049284e9abfccc93cffddd516.xml | 1B22A4A3898DF4B07907CD1590FD8AEA4F46A96A4CE1A780D6D67372F041E3C1 |
| Runtime admission | Library/ValidationResults/P12DirectNpcOwnerInvalidation/EditMode-20261002-013752-1d548125718e4db991def8280d80f640.xml | 868CFAD25E94B45D9FE232498301E60AA286D8635FA8C655D8878D29CEF3242D |
| ALL EditMode | Library/ValidationResults/P12DirectNpcOwnerInvalidation/EditMode-20261002-013819-ad583f8e69854cddbb234ddef274d2a5.xml | 46D79CF29BDD7F2020F9F2E738612397CA9FFE53962BA42E35C0215E8B0EE828 |
| Official Smoke | Library/ValidationResults/P12DirectNpcOwnerInvalidation/EditMode-20261002-013853-977bcde9fb57419fbd080996a5cd182c.xml | D9C70B08567E624FE2D3878B615E4650B9B9AFEBA7406099E85D7627B6BBC838 |

## Limits

This is only direct per-NPC account and Inventory owner-write invalidation. It does not provide the separate named TryTransferMoney multi-owner operation, cover all EconomyTransactionService families or other D/E/F writers, establish complete selected-profile owner coverage or shared-epoch coverage, prove global owner-thread/quiescence, grant capture eligibility, or provide export/hydration.

P12-B remains incomplete. P12-A remains WAIT_DEPENDENCY. P13 remains blocked. Phase 12 remains open.
