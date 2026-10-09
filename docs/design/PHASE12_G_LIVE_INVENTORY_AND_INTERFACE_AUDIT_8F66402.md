# P12-G current Daily-v1 inventory and B–F interface audit

**Audit result:** source-level reconciliation complete for the accepted `UnityBootstrap-Daily-v1` proving profile; outstanding exact-empty target checks and G implementation obligations are listed below.
**P12 canonical source:** `02009f9063dd252bd4b177fd6aef1e74dcd947f5` (tree `e42ef56abd6780a565c68f5b8887d397518eafe8`; Assets tree `a9a7c1015a5fa3cacfdb6219b18f2f593c863174`)
**Candidate branch snapshot:** `8f66402aaf3ccd587af02188fadebe7bd3a06bae` (Assets tree `9e915e339bbf398747b17b2c308754756ec63538`; its only Assets delta from canonical is the separately reviewed capture-order test at code commit `22de553fb48507c71041a9dfb401c74ac83735d3`).
**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Design authority:** [`PHASE12_G_TECHNICAL_DESIGN.md`](PHASE12_G_TECHNICAL_DESIGN.md), especially §§3–7.

## Effective profile and section coverage

`SampleScene.unity` selects `Simulation-DailyV1.asset` with `UnityBootstrapDailyV1`. The selected asset composes 2 Cities and 10 NPCs at day zero, with 0 Persons, one authored P8-A Hex/Location/scale context, no P10 Ruin, and no P14 material-flow sources. The two-City runtime roster is read-only after composition; the accepted profile's city cardinality is established at bootstrap. P10 and P14 are rejected before identity allocation when selected for this Daily profile.

The accepted vector has 61 fixed sections (48 Required, 13 ExplicitlyEmpty) and these dynamic families:

`61 + 22N + (N-M) + P + 4C`,

where `N` is live NPC count, `M` is NPCs bound to Persons, `P` is Person count, and `C` is City count. At the selected day-zero fixture, `N=10`, `M=0`, `P=0`, `C=2`, so the vector has 299 sections. All dynamic rows are Required, including exact-zero cardinalities. The current vector defines no Conditional or Excluded role.

The promoted inventory and source tests together cover the accepted roster: `SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne` asserts the 299-section schema/roles and exact selected spatial/profile cardinalities; the City roster is immutable after bootstrap. `SelectedDailyV1NpcMembershipCommitsIdentityAndKeepsItAfterUnregister` adds, unregisters, rejects aliases, and re-registers an NPC while checking the affected family providers and calling `TryAssessOwnerSectionInventory`. `SelectedDailyV1PersonMaterializationReconcilesDynamicOwnerCardinality` registers a Person, materializes its bound NPC, and reassesses the complete census. The sealed census protocol checks exact section/provider coverage, owner-thread/quiescent state, every witness identity/cardinality/revision, and dynamic per-NPC family membership when it captures a copied vector.

This reconciles the live profile and its supported dynamic roster changes; it is not restored-composition evidence and does not claim that a census witness itself is an export or hydrator.

## B–F package interfaces at canonical

| Boundary | Current canonical entrypoint and verified contract | G handoff/order |
|---|---|---|
| B | `SimulationRuntime.TryReadDailyCaptureEvidence` and `TryPublishCompletedDailyCaptureToken` call the sealed census protocol. The token binds runtime/config/profile/WorldId/day/successful core sequence/epoch and exact owner vector; validation is source-runtime-bound. B has no serialized domain rows. | G must admit a fresh token for the reconstructed runtime at the preserved day/sequence; the source token cannot transfer. |
| C | `P12CContinuationRootStager.TryStage` composes WorldId, all allocator counters, record sequence, P9-B manifest/provenance, P8-A geography, and deterministic-random root. It validates the Daily profile, P8/P9 identity/schema, spatial output cardinality/anchors, and random seed vs manifest. | Stage after the reviewed F source capture and before D; private root only. P8-B passage/crossing zero state is checked in its spatial snapshot. |
| D | `P12DDailyV1OwnerPackage.TryCaptureAndStage` validates exact token/vector/attempt and source-owner identities, captures/stages Person/Genealogy/legacy SpatialNetwork/site, each City, and each NPC. It binds City/NPC/Person/Location identities and exports detached D-owned NPC factual rows plus detached F projections. Failures return a typed failure and require discarding the private C/D attempt. | D receives private C roots; create each City/NPC once. D's detached NPC F projection is merged by F once. |
| E | `P12EDailyV1OwnerPackage.TryCaptureAndStage` composes the promoted required E authority snapshots (institutions/offices, property/estate, faction, claims/support/decisions, military/conflict/war/battle, Justice, Crime/Social). Owner snapshots validate token/vector, schema, role, identity, cardinality/revision and stage typed relations against D/C owners. PoliticalDecision exposes unresolved Knowledge bindings for graph validation. | Stage after D; preserve unresolved bindings for whole-graph checks. Justice's P18 receipt sentinel is checked here. |
| F | `P12FDailyV1OwnerCapture.TryCapture` captures the five fixed F owners against the exact source runtime/composition/token/vector; `P12FDailyV1OwnerCapture.TryStage` binds them to staged D/E and validates the same attempt. F retains Knowledge, directives, ActorChoice, TravelParty and Expedition plus the detached NPC F merge. | Call source capture before any staged domain object is allocated; later stage retained capture on the same C/D/E attempt. The order-only test at `22de553` verifies this fixture. |

These are typed in-memory snapshot/stage boundaries, not a common serialized archive codec. Exact validation artifacts for promoted C–F packages remain in their existing validation records, including the aggregate C/D/E/F test at `P12FDailyV1OwnerPackage/VALIDATION.md`.

## Owners without a B–F payload receiver

The source inventory distinguishes rows from witnesses; G must not drop or infer emptiness from absent payload:

* Four empty RuntimeIdentityRegistry categories and the empty ExplorableSite owner are explicitly created/validated by D. P8-B passage/crossing zero state is checked by C.
* P8-C city-site bindings/person positions and P8-D route observations/person route-plan history have ExplicitlyEmpty census witnesses but no B–F payload snapshot. The reconstructed profile must create the corresponding fresh owners and verify each target witness is bound to the new owner with cardinality/revision zero.
* The global NpcDecision occurrence-receipt and economy keyed-sale caches are ExplicitlyEmpty with no B–F payload receiver. The new target owners must verify exact zero. The two per-NPC D receipt families are source exact-zero witnesses and require corresponding target zero checks.
* The Justice P18 receipt sentinel is a Required cardinality-one/revision-zero witness and is consumed by E. The Crime P18 receipt sentinel has the same Required sentinel contract but no package consumer was found outside its provider/runtime registration; G must explicitly bind and verify it rather than infer coverage from Justice or from the Crime/Social snapshot.
* `ActorChoiceTemporalCensusProvider` is exposed but not registered in the 299-section vector. It is not inferred empty from omission: pre-allocation F source capture checks the P11 owner identity/revision and `TemporalInputCount == 0`. The order-only test at `22de553` proves this call precedes C root staging in the fixture.
* `NpcDecisionStore` and `DomainEventStore` are `OmittedNonCausalReadModel`; `HistoryStore` is an event subset and `NpcChronicle` is derived. Their rows are not silently classified as empty or as required continuation payload.
* P10 `LocalTopology`, P14 sources, and P15/P16/P17/P18 intraday/P19/P20 payload are not composed by this profile. P10 Ruin and P14 City profiles remain separate.

## Remaining P12-G implementation work

The audit resolves the B–F interface and profile inventory questions for starting the accepted G checkpoint. It does not deliver G. G still needs the already reviewed restored-boundary admission API, fresh target exact-empty/sentinel verification above, ordered C→D→E→F staged composition, whole-graph/reference and rejection checks, failure atomicity, one `TesteSimulacao` active-session holder/swap, and continuation parity. The technical design §6 remains the implementation validation contract. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.
