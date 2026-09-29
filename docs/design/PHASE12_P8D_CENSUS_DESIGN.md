# P12-B P8-D route owner witness design

**Status:** Submitted for independent technical review.

**Canonical base:** `codex/phase12/canonical` at
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2`.

**Dependency:** The non-admitting `ContinuationCensusProtocol` and passive
P8-B/P8-C witness adapters are canonical. The selected profile publishes the
P8-D route Knowledge and plan owners, but current coverage is only direct
day-zero reads. This slice adds passive evidence for those two existing owners.

## Owner evidence contracts

The selected `UnityBootstrap-Daily-v1` profile composes these runtime owners
after cloning:

| Section | Role/schema | Owner identity | Cardinality | Revision stamp |
|---|---|---|---|---|
| `p8d.spatial-route-observations` | `ExplicitlyEmpty`, v1 | Installed `SpatialRouteKnowledgeStore` | `ObservationCount` | `SpatialRouteKnowledgeStore.Revision` |
| `p8d.person-route-plan-history` | `ExplicitlyEmpty`, v1 | Installed `PersonRoutePlanStore` | `PlanCount` (all retained history rows) | `PersonRoutePlanStore.Revision` |

These section IDs are the proposed P12-B adapter identities; they are not
existing P8 constants. `SpatialRouteKnowledgeStore.ObservationCount` counts
every retained observation across actors. `PersonRoutePlanStore.PlanCount`
counts every retained plan revision. `Plans.Count` is only the number of actors'
latest plans and is not an acceptable history cardinality. `History.Count` is
an equivalent read-only cross-check for the plan owner.

Both owners are exactly empty in the selected day-zero profile: Knowledge has
`ObservationCount=0` / `Revision=0`; route plans have `PlanCount=0`, empty
`History`, and `Revision=0`. Owner identity must be the installed runtime
store, not the source store passed into runtime construction.

The revision stamps are local owner stamps, not the P12 shared mutation epoch.
Each nonempty accepted new-observation batch advances Knowledge cardinality by
the number of new rows and global revision once. An identical replay is a
successful no-op; invalid input or conflicting same-provenance evidence fails
without changing its witness. A prepared P8-E Knowledge install can be a
no-op for duplicate evidence. Each successful plan acceptance appends one
retained history row and advances plan revision. Rejection leaves it unchanged.
P8-E status-only plan installs advance plan revision without changing
`PlanCount`; travel transactions may install Knowledge and plan roots together.
These local revisions do not prove a shared commit notification.

## Implementation boundary

- Add two passive `IOwnerSectionCensusProvider` adapters in a new P12 P8-D
  census source file. Each captures its installed owner reference and returns
  the proposed section ID, schema version 1, owner identity, the exact owner
  cardinality, and local revision. Do not add new owner APIs or alter P8-D/P8-E
  behavior.
- Extend `SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
  to assert both section/schema identities, installed runtime owner identity,
  stable identity across reads, zero cardinality and revision, plus the plan
  history empty cross-check.
- Add focused provider assertions in `SpatialRoutePlanningTests`: new
  Knowledge rows advance count/revision, identical observation replay leaves
  both unchanged, conflicting or invalid input leaves them unchanged, accepted
  plans grow retained history and revision, and stale plan acceptance does not
  change the witness. Extend the existing P8-E civil-travel lifecycle test to
  show its status-only Active/Completed transitions advance plan revision while
  the number of retained rows stays fixed; a later accepted replan grows the
  history by one.
- Preserve the owner references installed in the runtime clone. Do not edit
  `SimulationRuntime`, P8-E transaction ordering, route decision semantics, or
  P8-D Knowledge/plan mutation paths.

Do not register these providers into the incomplete census protocol, connect
them to the shared mutation epoch, add owner-thread/quiescence enforcement,
claim capture eligibility, or introduce P12-F serialization/export and
hydration. They remain unsynchronized passive reads and do not complete P12-B
or make P12-A ready.

## Source evidence reviewed

- `SpatialRouteKnowledgeStore.TryRecordObservations` stages and validates a
  batch before installation, advances global and actor revision once when new
  rows commit, treats exact duplicate replay as a successful no-op, and rejects
  conflicting evidence without partial installation.
- `PersonRoutePlanStore.TryAcceptPlan` appends a retained history row and
  advances store revision on success. `TryPrepareStatusChange` and
  `InstallPrepared` replace only the latest status row and advance store
  revision without changing history cardinality.
- P8-E travel commits validate all participating prepared roots before their
  swaps. They do not notify the P12 shared mutation epoch.
- The selected-profile bootstrap test already directly observes the owners at
  zero/revision zero; `SpatialRoutePlanningTests` already covers replay-related
  Knowledge semantics, plan replacement and stale rejection, runtime cloning,
  and P8-E departure/progress/arrival transitions.
