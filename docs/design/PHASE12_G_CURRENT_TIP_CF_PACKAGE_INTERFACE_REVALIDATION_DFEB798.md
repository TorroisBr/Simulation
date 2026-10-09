# P12-G current-tip C–F package interface revalidation

**Result:** source/interface revalidation PASS for the bounded C–F package seams; no P12-G implementation-readiness change.

**P12 canonical and accepted State baseline:** `dfeb79818d40390cc981ceeaa198eb6318b03535`.

**Architecture baseline:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.

**Reviewed design:** [`PHASE12_G_TECHNICAL_DESIGN.md`](PHASE12_G_TECHNICAL_DESIGN.md), blob `b7b8a983489aa7018af54c9a5361dbfe19f37a90`.

**Previous interface audit:** [`PHASE12_G_CURRENT_C_F_PACKAGE_INTERFACE_AUDIT_02009F9.md`](PHASE12_G_CURRENT_C_F_PACKAGE_INTERFACE_AUDIT_02009F9.md), blob `4e122b1e350aceeeb603da47d8b88a62fae916ed`, authored against P12 canonical `02009f9063dd252bd4b177fd6aef1e74dcd947f5`.

**Current code identities:** `Assets` tree `1b90b4f77586f69c04330b564a32e0a9475808d4`; `Assets/_Project/Scripts` tree `d47c3ef99be5225aefb876527ab64ca73978e462`.

## Refresh result

Comparing P12 canonical `02009f9` with current canonical `dfeb798`, the `Assets/_Project/Scripts` tree is identical. The five production files that define the C–F entry points and the ActorChoice temporal guard are unchanged at these exact blobs:

| Surface | Current source | Blob |
|---|---|---|
| Shared capture/staging attempt and C roots | [`P12CContinuationRootStager.cs`](../../Assets/_Project/Scripts/P12CContinuationRootStager.cs#L11) | `19f61d8abb5d70e30e0a0dc53b6a1ba03f55cf43` |
| D factual-owner capture/stage | [`P12DDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs#L68) | `7db204396b5a7526e44966f7e7d7b292206957f0` |
| E configured-owner capture/stage | [`P12EDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs#L147) | `241caeea44df42a23c6665567d93fb6924994cf3` |
| F detached source capture and private stage | [`P12FDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12FDailyV1OwnerPackage.cs#L61) | `b5c6e64bc2b8fa69dbfef8f8aa1e04eec03ed8c1` |
| ActorChoice temporal/pending-state rejection | [`P12FActorChoiceSnapshot.cs`](../../Assets/_Project/Scripts/P12FActorChoiceSnapshot.cs#L44) | `dbbc29f5e663cab99741421c3a04d553a53a8b6a` |

The `Assets` changes since `02009f9` are test-only. The C/F aggregate test now asserts F source capture before C root staging, then stages C, D, E, and the previously captured F against the same `DailyCaptureStagingAttempt`. The independent Daily-v1 manifest now compares the exact expected IDs and roles with protocol-expected and registered sets, checks repeated owner identity/cardinality/revision stability and ExplicitlyEmpty cardinality, and exercises the selected NPC roster and Person registration/materialization transitions. It also checks the deterministic-random root identity and TravelParty allocator-counter provider identity. These are useful fixture and selected-transition witnesses, not a generic runtime graph walk or a proof of every supported transition.

The retained exact-tree validation in [`P12GCurrentCanonicalInventory/VALIDATION.md`](../validation/P12GCurrentCanonicalInventory/VALIDATION.md) names the current `Assets` tree `1b90b4f77586f69c04330b564a32e0a9475808d4`: the focused bootstrap-composition suite passed 26/26, ALL EditMode 2732/2732, and official Smoke 5/5. This documentation revalidation does not rerun Unity. The exact current `Assets` tree matches the evidence tree.

## Current package contracts

| Package | Revalidated current seam | Current bounded result and remaining limitation |
|---|---|---|
| B staging boundary | `DailyCaptureStagingAttempt.TryBegin` requires the selected Daily-v1 token, exact owner-vector object, source `WorldId` identity, positive completed-core sequence, and a currently valid source token. `IsCurrentFor` rechecks runtime, token, vector identity, and token validity. | One attempt can fence private package staging to the same source completed boundary. It is not a restored-runtime admission token and does not prove the full live graph is quiescent. |
| C roots | `P12CContinuationRootStager.TryStage` consumes the same attempt and detached WorldId, allocator, record-sequence, P8-A geography, P9-B manifest, and deterministic-random snapshots. | Returns private roots for identity, allocator, sequence, SpatialAuthority, manifest, and random provenance. It does not build or publish an active runtime. |
| D owners | `P12DDailyV1OwnerPackage.TryCaptureAndStage` checks the exact source runtime/token/vector and current attempt, then consumes the staged `WorldId`, a separately supplied staged `RuntimeIdentityRegistry`, and authored definitions. It does not receive the complete C root/provenance bundle. Its captured TravelParty IDs restore reciprocal NPC links. | Returns private D factual roots plus `NpcFRows`: detached NPC projection rows carrying Knowledge/travel/plan facts for the later F stage. D exports these rows; F owns and stages their values. D does not supply E stores. |
| E owners | `P12EDailyV1OwnerPackage.TryCaptureAndStage` validates the same source boundary and attempt, captures the currently composed E authorities, then stages them against D Persons, C SpatialAuthority, and the supplied staged time/status/logger context. | Returns private E owners and typed unresolved PoliticalKnowledge bindings. Resolving those bindings and validating them against the complete candidate graph remain G integration work. |
| F owners | `P12FDailyV1OwnerCapture.TryCapture` validates the exact source runtime/composition/token/vector and captures PoliticalKnowledge, directives, ActorChoice, TravelParty, and Expedition. Its later `TryStage` requires the same attempt and staged C/D/E inputs, then merges detached F NPC rows once. | Preserves F source facts without dispatch/replanning. It does not construct the whole candidate or publish it. |
| ActorChoice source boundary | `P12FActorChoiceSnapshot.TryCapture` matches the required P11 owner witness, checks invariants, and rejects temporal inputs, pending/awaiting-terminal inputs, temporal dispositions, and deferred dispositions. | The capture-order test invokes this through F before C creates staged domain roots. A fresh target-side exact-zero temporal check after F staging remains required. |

## Dependency order and evidence boundary

The current API and [`P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt`](../../Assets/_Project/Tests/EditMode/Editor/P12CPrivateRootCompositionTests.cs#L170) support this local package sequence: begin the attempt; capture F from the source; stage C; capture/stage D; capture/stage E; stage the retained F capture against C/D/E using the same attempt. The test demonstrates this successful private aggregate path and identity separation for its fixture. It does not invoke a P12-G coordinator, restored-boundary admission, whole-graph validation, target-owner zero checks, or active publication.

No current P12-G orchestrator is delivered. The current interface revalidation closes only the stale-source/interface portion of the P12-G §7 package gate. Still open are:

- exhaustive live owner/cardinality coverage across every supported transition and conditional owner;
- exact successful-writer, enclosing-operation, owner-thread, quiescence, and epoch mapping, including Crime/Social and any supported direct paths;
- fresh target checks for P8-C/D, global and per-NPC receipt owners, Crime/Justice sentinels, and ActorChoice temporal state;
- restored-boundary admission bound to a fresh healthy candidate runtime while preserving the source day and successful-core sequence;
- one authoritative active-session holder and one publication exchange for all simulation/reporting consumers;
- whole-graph admission/rejection, failure atomicity, no-replay, and continuation-parity implementation and evidence.

This result changes no Phase status, checkpoint identity, owner scope, or architecture. P12-B through P12-F remain promoted within their recorded limits; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. It does not make P12-G implementation-ready, make P12-A ready, or satisfy P13 prerequisites. No Unity test was run for this documentation-only revalidation.
