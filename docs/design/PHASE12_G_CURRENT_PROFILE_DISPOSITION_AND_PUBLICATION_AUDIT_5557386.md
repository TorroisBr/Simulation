# P12-G current-profile disposition and publication-source audit

**Audit baseline:** P12 canonical `02009f9063dd252bd4b177fd6aef1e74dcd947f5`; architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; candidate branch base `5557386627b0e3aa6a8916561059106e14a855c7`.

**Status:** read-only source and contract revalidation. This resolves profile classification questions and inventories the existing publication aliases. It is not the validated complete live-profile inventory, an implementation review, or a readiness change. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

## Military owners: required base state, excluded extensions

Keep the eight P12-E military/Conflict/War/Battle census rows `Required`. P12-E technical design §1 explicitly includes the current core military and conflict truth composed by `UnityBootstrap-Daily-v1`; empty-at-bootstrap owners still need an explicit empty section, and populated supported base facts must be exported and privately staged by the existing P12-E snapshots. The `Required` role does not require positive record cardinality.

This does not admit P16/P17 extension payload or gameplay. P12-E capture rejects LocalTopology, P17 provenance/War extension fields, and P16 profile/supply/receipt extensions. The P16/P17 States and P17 Brief keep their separate proving profiles outside Daily-v1. The source audit found no normal post-publication Daily-v1 writer for these core owners. That finding classifies current profile reachability only; it does not prove every same-process direct mutator or shared-epoch path. Preserve that as a technical inventory/validation obligation where a writer is shown to be part of the supported profile. Do not add gameplay or security-oriented rejection to settle the audit.

## Expedition: current Daily-v1 has no supported producer

The fixed F vector includes the `ExpeditionStore` owner and P12-F captures/stages that owner. However, the selected `SampleScene` → `Simulation-DailyV1.asset` profile has an exact-empty ExplorableSite owner, excludes P10 Ruin/LocalTopology, and does not compose `AdventureExpeditionAutonomySystem`. No normal selected-profile caller of `TesteSimulacao.TryStartExpedition` was found. A successful Expedition requires a profile-supported site; therefore the current supported Daily-v1 flow has no Expedition producer. `SimulationRuntime` may call `ReconcileAfterTravel`, but there are no active profile Expeditions for that path to reconcile.

The direct `TesteSimulacao` facade and raw store APIs are visible code surfaces, not evidence of a configured Daily-v1 input path. Keep the existing census row and require G to validate exact-zero source and target owner state. Do not add a new Daily operation that enables excluded site content. A future profile that supports sites and Expedition starts must refresh the owner/operation matrix and review one outer operation around the Expedition and nested TravelParty commits.

## Active publication root and consumers

`TesteSimulacao.Start` captures the selected profile's Unity owner thread. Initial publication currently assigns `publishedComposition`, closes/assesses bootstrap publication, marks factual reads available, and then sets `worldPublished`. `PublicComposition` gates most public reads. This is the bootstrap startup gate, not an atomic restored-session replacement.

`SimulationBootstrapComposition` already holds the main runtime graph: WorldId/manifest, time/calendar, spatial network, event/history and directive/decision owners, runtime, identity registry, sites, TravelParty and Expedition stores/systems, and related passive census providers. It does not hold every reference used by `TesteSimulacao` after publication. Current methods still read parallel fields:

| Field/reference | Post-publication use |
|---|---|
| `simulationRuntime` | `Simulate` advances the runtime; admission faults also target this field. |
| `runtimeIdentityRegistry` | NPC, City, and site lookup plus NPC display-name resolution. |
| `spatialNetwork` and `cityRuntimeByLocation` | Location/route lookup and display-name resolution. |
| `npcChronicleService` | Chronicle queries. |
| `npcRuntimeList`, `cityRuntimeList` | End-of-day NPC and market reports. |
| `justiceSystem` | Warrant text in NPC reports. |
| `logger` | Public full-log read, reports, warnings, errors, and file output. |
| `simulationConfig`, `lastEconomySnapshotDay` | Report enablement/name/cadence and duplicate snapshot suppression. |
| `publishedComposition`, `worldPublished` | Public availability gate and the composition pointer, currently separate values. |

`Simulate`, reporting, lookup, and chronicle methods use the gate and then access cached private fields rather than capturing one immutable active-session reference for the complete operation. An eventual restore that changes only `publishedComposition` can therefore leave consumers on the prior graph or report state. The bounded design direction is one published session root containing the composition and the presentation/report dependencies and boundary-local report state needed by this component; every public operation should capture that root once. Build and validate the candidate session privately, exchange the root once, and keep the old root authoritative on any earlier failure. This changes publication ownership only; it does not add domain truth, save format, UI semantics, or gameplay.

## Remaining P12-G entry evidence

1. Complete and validate the section-to-live-owner matrix against the constructed graph, including every supported successful writer, exact identity/cardinality/revision source, and publication consumer. Preserve the 299 formula and temporal roster tests; neither proves exhaustiveness by itself.
2. Finish exact target-owner checks for P8-C/D, global receipt caches, per-NPC target receipt witnesses, and the Crime P18 sentinel. Justice's sentinel and F's ActorChoice temporal pre-capture order remain separately covered by their current source contracts.
3. Revalidate every B–F package interface and source/target disposition against exact current canonical tips, then independently review the refreshed G design and its publication-holder seam.
4. Only after those entry gates clear, implement whole-graph validation, fresh restored-boundary admission, failure-atomic staged composition, one active-session publication root, and continuation/rejection parity under the accepted G scope.

This audit preserves promoted P12-B through P12-F and does not promote P12-G, authorize P12-A implementation, or change any checkpoint dependency.
