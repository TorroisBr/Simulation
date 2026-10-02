# P12-B Direct NPC Owner Invalidation Independent Implementation Review

**Verdict: PASS**

- Reviewed candidate: codex/phase12/P12BDirectNpcOwnerInvalidationCandidate at c49f957e45c3059231e9ec66e4010a7c3a389988
- Reviewed base: P12 canonical 2f7c7422812de40aa1223e8310dcbd9f5d8ca474
- Exact candidate tree: 627af2fbd7f93e0025106ee5ba87e72bae6c4ed2
- Review type: independent exact-tip implementation review
- Scope: direct committed writes to already-censused per-NPC MoneyAccountRuntime and InventoryRuntime owners.

The review verified the exact owner bindings and baseline checks, roster-driven owner rebinding/unbinding, direct postcommit debit/credit/add/remove notification, and alias grouping so one Inventory owner commit produces one section notification. It checked that writes inside already instrumented NPC-trade and Open-market operations do not duplicate leaf notifications. Failed admission is rejected before the corresponding owner mutation. At epoch saturation the already-committed domain mutation is retained and the continuation protocol faults closed; the candidate does not claim rollback or whole-operation atomicity for every caller.

The changed Expedition retrieval caller now checks the Inventory add result after the place-stack take. The review confirmed the resulting failure is reported without claiming compensation or restoration that the existing contract does not provide.

Validation is recorded in PHASE12_P12B_DIRECT_NPC_OWNER_INVALIDATION_CANDIDATE.md and was run on the identical tree 627af2fbd7f93e0025106ee5ba87e72bae6c4ed2. The code candidate remains unchanged after review.

The review does not extend the candidate to a named money-transfer operation, transfer compensation boundary, all EconomyTransactionService families, complete profile owner coverage, universal shared-epoch coverage, global owner-thread/quiescence, capture eligibility, export, or hydration. P12-B remains incomplete, P12-A remains WAIT_DEPENDENCY, P13 remains blocked, and Phase 12 remains open.
