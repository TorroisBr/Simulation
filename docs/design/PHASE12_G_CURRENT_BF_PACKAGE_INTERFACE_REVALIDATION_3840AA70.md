# P12-G current B–F package interface revalidation — 2026-10-11

**Result:** current source/interface reconciliation PASS for the B–F package and restore-coordinator seams. This is documentation-only evidence. It demonstrates no missing B–F package implementation, changes no executable behavior, and does not make P12-G implementation-complete or ready to close.

## Exact baseline and authorities

- At audit time, `origin` was `https://github.com/TorroisBr/Simulation`; the fetched `origin/codex/phase12/canonical` ref and this candidate's exact baseline were `3840aa70fdd88ad344e23327182d1825b644fdbd`. That SHA is the candidate's sole parent. The separately named local `codex/phase12/canonical` branch was not used as the baseline.
- `SIMULATION_ARCHITECTURE.md`: blob `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`.
- `EXECUTION_MODEL.md`: blob `9d007aa93e602a2f8242000d3c60864614b049f3`.
- `ROADMAP.md`: blob `f9bb445880948b5e493fbf7f5682c38a33d18589`.
- `PHASE12_BRIEF.md`: blob `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`.
- `PHASE12_STATE.md`: blob `ace860779c19d050c23b1d3b58aaa2faf23eae64`.
- `PHASE12_G_TECHNICAL_DESIGN.md`: blob `4d2c38298ec3e4ca44244f6efdd795659b463db3`, especially §§6–7.
- Alignment records checked: `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

The accepted Phase 12 profile remains same-build, current-host `UnityBootstrap-Daily-v1` at a successful completed daily boundary. The Brief excludes P18 timeline state and P20 shared-activity state from this profile. The two alignment records therefore do not change the B–F interface contract in this audit; no intraday or multi-participant serialization is claimed.

The previous [`dfeb798` C–F package-interface revalidation](PHASE12_G_CURRENT_TIP_CF_PACKAGE_INTERFACE_REVALIDATION_DFEB798.md) is stale as current evidence: its canonical base and cited package blobs predate the current C/D/E/F source, the integrated P12-G coordinator, and current tests. It remains historical evidence for its exact older baseline.

## Current executable interfaces

| Boundary | Current source/blob | Verified contract and handoff |
|---|---|---|
| B completed-boundary attempt | [`P12CContinuationRootStager.cs`](../../Assets/_Project/Scripts/P12CContinuationRootStager.cs), `3c7761bbf1ff2d46e67beaf8d12e987bef40c141` | `DailyCaptureStagingAttempt.TryBegin` requires a valid selected-profile completed token, the exact `OwnerSections` object owned by that token, the source `WorldId` identity, a positive completed-core sequence, and a currently valid source token. `IsCurrentFor` rechecks runtime, token, vector reference identity, and token validity. This is a staging fence; it is not target admission or proof of runtime-wide quiescence. |
| C identity, provenance, spatial and deterministic roots | [`P12CContinuationRootStager.cs`](../../Assets/_Project/Scripts/P12CContinuationRootStager.cs), same blob | `TryStageForRestore` builds detached WorldId, allocator, record-sequence, P8-A geography/spatial authority, P9-B genesis provenance, and deterministic-random roots against the shared attempt. It does not publish an active runtime. |
| D factual roots and detached NPC facts | [`P12DDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs), `27c5c00bc4a432112d38e58f74800c3d2c5ad64c` | `TryCaptureAndStage` rechecks source token/vector/attempt and stages factual City/NPC roots and related P8-D state against staged C roots and authored definitions. D consumes F-captured TravelParty IDs to build NPC links and returns detached `NpcFRows` for F to own and stage; D does not own E stores. |
| E selected configured domain owners | [`P12EDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs), `09f303fa5379393a66953f4b268c62457ee28369` | `TryCaptureAndStage` rechecks the same source boundary and attempt, captures selected E authorities, and stages them against C/D roots and the supplied time/status/logger context. It returns private E owners plus unresolved PoliticalKnowledge bindings for final whole-candidate validation. |
| F Knowledge, directives, ActorChoice and commitments | [`P12FDailyV1OwnerPackage.cs`](../../Assets/_Project/Scripts/P12FDailyV1OwnerPackage.cs), `1aee3a0c0fbdf746b3090363d9c72cc6c4e297d0` | `TryCapture` validates the selected source composition/vector and captures F owners; `TryStage` requires the same attempt and C/D/E context, then merges D-exported detached NPC facts once. ActorChoice capture validates its P11 owner witness and rejects temporal or nonterminal unsupported states. F stages data; it does not dispatch, replan, or publish. |
| Integrated P12-G restore coordinator | [`P12GDailyV1RestoreCoordinator.cs`](../../Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs), `5bb5f9d63c089ca2bb726ba27e21d4bcbc41ee65` | The coordinator validates the source completed token/vector, begins one attempt, captures F before C root staging, then stages C → D → E → retained F under that same attempt. It rechecks the source after staging, builds a private candidate, obtains a fresh target census, validates the vector and target sentinels/bindings, binds and recaptures the mutation guard, matches the post-bind vector/epoch, and admits the target with the source day/core sequence. |
| Target vector rules | [`P12GDailyV1OwnerVector.cs`](../../Assets/_Project/Scripts/P12GDailyV1OwnerVector.cs), `1878248ac324a7f2fdd621b5b74b6c26c5b9a60b` | `TryValidateRestoredCandidate` requires exact section count/set, unique IDs, matching schema/role/cardinality/revision, fresh target owner identities, zero cardinality for explicitly empty/excluded rows, and preserved owner-alias relationships. `TryMatchCurrentTargetSnapshot` additionally requires unchanged section identity/schema/role/cardinality/revision/owner identity and mutation epoch after guard binding. |
| Serialized active-session publication | [`TesteSimulacao.cs`](../../Assets/_Project/Scripts/TesteSimulacao.cs), `256d81e14f93b458270912be593f5f47caa1831b` | `TryRestoreDailyContinuationCore` holds `activeSessionGate` while checking idle owner-thread state, staging, and publication. Successful publication has one `Interlocked.Exchange` of the active session. No fallible post-swap stage is defined. |

The integrated coordinator's source-to-target sequence is therefore not just the older package-level C–F sequence: F is captured first to validate source temporal state before candidate roots exist; its detached capture is staged after C, D, and E so the aggregate owner graph is complete. The source token is checked again after private staging and after final target graph validation.

## Current tests and their limits

- [`P12CPrivateRootCompositionTests.cs`](../../Assets/_Project/Tests/EditMode/Editor/P12CPrivateRootCompositionTests.cs), blob `366364d01362616da07285c51652d1706a456e5c`, includes `P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt` (line 170). It checks F capture before C, then C/D/E/F use of the same attempt and F aggregation of D's detached NPC facts. This is a local package-order fixture, not a full G target census or publication test.
- [`P12EDailyV1OwnerPackageTests.cs`](../../Assets/_Project/Tests/EditMode/Editor/P12EDailyV1OwnerPackageTests.cs), blob `96a950dcc08d4e921e77ef14b228982c06a68fc5`, exercises package failure/mismatched-boundary cases; package code rechecks exact source token/vector and the same attempt before and after staging.
- [`P12GDailyV1OwnerVectorTests.cs`](../../Assets/_Project/Tests/EditMode/Editor/P12GDailyV1OwnerVectorTests.cs), blob `3cfca28ff472ae151b3c38fcf5c846c818720543`, covers section-set/count, duplicate identity, contract/cardinality/revision/identity drift, empty/excluded disposition, aliases, and post-validation vector/epoch equality.
- [`SimulationRuntimeAdmissionTests.cs`](../../Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs), blob `833f5292b28f818260e503c12ca1ba17eaaf6d69`, exercises the integrated coordinator: unadmitted target census owner-thread/quiescence gates, corrupted owner/root rejection, target owner/cardinality mutation after census, private-stage failure cutpoints, source preservation and valid retry. Named fixtures establish only the owner values, corruption shapes, and restore cuts they actually construct.
- The package capture-order test and the integrated restore tests are distinct proof layers. A passing package-order fixture does not itself establish all-section target graph validity, and the target-vector implementation does not itself establish a complete compatibility/rejection matrix.

This is a source/test revalidation only. No Unity tests were rerun because no executable file changed. No new test result is claimed by this record.

## Reconciliation with the P12-G contract

The current code implements the B–F interface seams and the integrated same-attempt target-vector/mutation-epoch path described above. This audit finds no missing B–F package implementation or stale package-order contract. The old `dfeb798` interface-verification obligation can be considered revalidated at `3840aa70` after independent review of this record.

The following §6 evidence remains open and is not closed by interface inspection or by code presence:

1. **Target census and staged graph evidence:** broader same-attempt target-owner cardinality/identity/sentinel coverage and the complete admitted owner graph across all supported selected-profile combinations.
2. **Compatibility and graph rejection:** remaining supported profile/compatibility cases and cross-owner/reference rejection branches not already named in promoted records.
3. **Failure atomicity:** reachable failure paths not already covered by retained stage-fault and graph-rejection cases. Detached states that normal source capture rejects remain robustness evidence only, not source-reachable histories.
4. **Causal no-replay:** beyond the separately named ActorChoice/directive/commitment fixtures, prove no restore-time effects and no duplicate causal effects for the supported included state.
5. **Continuation parity:** exact included-owner projections, relationships, commitments, sequences/allocators, random-dependent outcomes and revisions over multiple subsequent boundaries beyond the existing fixtures.

The selected 299-section inventory, covered dynamic-transition witnesses, and supported writer/operation/epoch family audit remain promoted within their recorded bounds. This report does not claim exhaustive direct-call coverage, universal shared-epoch coverage, runtime-wide quiescence, capture eligibility, P12-A export/staged hydration, P13 readiness, or Phase 12 closure. P12-B through P12-F remain delivered within scope; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.
