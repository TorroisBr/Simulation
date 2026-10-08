# P12-D City/NPC receipt-owner zero-witness design — exact-content review

**Verdict: NEEDS_CHANGES.** This is a design review only. The candidate was not edited, and no code or tests were run.

## Exact scope

- Candidate branch: `codex/phase12/P12DCityNpcReceiptZeroWitnessDesign`
- Candidate tip: `22d1befe971f74f6551b307f704abef1975bd861`
- Candidate tree: `a82d5cede48dc8c41ba1bfe13b7d25a0d84d93ba`
- Exact P12 canonical base and candidate parent: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Sole candidate change: `docs/design/PHASE12_D_CITY_NPC_RECEIPT_OWNER_ZERO_WITNESS_DESIGN.md`
- Candidate document SHA-256: `3D2005E687F4740AD7F98FF54DF2740A456AF32604471852B6BC21BA757A1A54`
- Crosswalk candidate/review read: `3b1c8508b85995d468ccd955210e4fa8c89fab24` / `3027816b6f95901cba36020e6c8990263d5e722e`

The candidate is directly based on the current remote canonical tip. The State header confirms P12-B and P12-C are complete within bounded contracts, P12-D/P12-E remain in progress, P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains open.

## Findings

### [P1] Define reconciliation of receipt rows when the supported NPC roster changes

The design creates the two per-NPC provider families from `npcRuntimeSnapshot` before inventory sealing (§4), then says a stale roster invalidates admission/token assessment (§4). It does not state how the expected/registered receipt sections and provider lists are reconciled on later NPC membership changes.

This matters because `SimulationRuntime.cs:1374` assigns `npcRuntimeSnapshot` as a read-only view over the live mutable roster. Supported `TryRegisterNpc`/`TryUnregisterNpc` membership operations run through the existing census boundary (`SimulationRuntime.cs:5587–5644`). That boundary calls `ContinuationCensusProtocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations`; the protocol rebuilds per-NPC family candidates and updates the expected/registered sections and provider arrays (`ContinuationCensusProtocol.cs:1315, 1383–1399, 1504–1557, 1859–1874`).

Unless the receipt families participate in that same reconciliation, a valid roster addition/removal leaves the exact receipt-section set stale. Subsequent census/token assessment will reject the roster rather than witness two exact-empty owners for each currently installed NPC. This conflicts with the design’s claim that row count derives from the actual accepted roster and with the existing supported membership lifecycle.

Specify the receipt families’ add/remove/replacement behavior in the existing roster reconciliation, preserve the existing membership mutation-epoch semantics, and include dynamic-roster assertions (for example 10→11→10). A populated, null, aliased, or replaced child owner must still fail closed; this does not require a new receipt-write callback or P12-B operation contract.

### [P2] Correct the planned EditMode test path

Section 6 names `Assets/_Project/Tests/Editor/P12DCityNpcReceiptOwnerCensusTests.cs`. The current tree uses `Assets/_Project/Tests/EditMode/Editor/` for EditMode tests; for example, `P12DCityRootOwnerSnapshotTests.cs` is there, and `Assets/_Project/Tests` contains the `EditMode` directory. The proposed path omits `EditMode`, so tests created at the specified location would not belong to the established EditMode assembly. Change the plan to `Assets/_Project/Tests/EditMode/Editor/P12DCityNpcReceiptOwnerCensusTests.cs`.

## Verified design boundaries

- Both receipt owners are serialized fields nested in `NpcRuntime`, each with a serialized private revision and receipt list. Their public revision is read-only; the `ReceiptList` getter lazily allocates when the backing list is null. The existing internal `NpcRuntime` getters also lazily create missing owners, so the proposed raw reads and non-lazy `Existing*` accessors are necessary to preserve malformed/missing-state evidence.
- A new successful commit appends a receipt and advances the respective owner revision; duplicate/idempotent handling does not append a second receipt. Requiring both raw count and revision to equal zero therefore catches empty-list/nonzero-revision state as well as populated state.
- The exact nested owner object can serve as `OwnerInstanceIdentity`; one required schema-v1 section per NPC and stable `RuntimeId` keys match the neighboring owner-family pattern. The proposed census exposes no receipt values or mutable list and keeps these ledgers out of D/F DTOs, staging payloads, and replay.
- The negative-profile witness remains compatible with the accepted D/E split: D captures the single NPC projection, E does not duplicate NPC fields, and the excluded P18 state is rejected unless proven exact-empty. The design adds no P12-B operation, mutation scope, receipt writer, notification callback, or epoch reservation, and it preserves the Daily-v1 boundary and P10/LocalTopology exclusion.
- No other findings in the reviewed document scope.

No implementation readiness or phase/checkpoint completion follows from this review. The design should be revised and independently reviewed at a new exact content hash before this witness slice is handed to implementation.
