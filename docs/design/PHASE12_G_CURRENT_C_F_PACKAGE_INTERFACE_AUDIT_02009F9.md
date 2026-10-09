# P12-G current C–F package interface audit

**Baseline:** P12 canonical `02009f9063dd252bd4b177fd6aef1e74dcd947f5`; Assets tree `a9a7c1015a5fa3cacfdb6219b18f2f593c863174`; architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.

**Status:** source-level interface and dependency-order audit. It records existing package seams for the P12-G entry audit. It is not a complete live-graph inventory, integrated reconstruction, implementation review, readiness change, or authorization. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

## Current package seams

| Package | Current entry and input contract | Private result and relevant dependency |
|---|---|---|
| P12-C roots | `P12CContinuationRootStager.TryStage` requires the current shared `DailyCaptureStagingAttempt` and detached WorldId, allocator, record-sequence, P8-A geography, P9-B manifest, and deterministic-random snapshots. It reconciles P8-A to P9-B identity/schema and selected Daily-v1 profile before returning. | A private `P12CStagedContinuationRoot` containing WorldId, allocator, record sequence, SpatialAuthorityStore, genesis manifest, and deterministic random root. It is not an active runtime. |
| P12-D owners | `P12DDailyV1OwnerPackage.TryCaptureAndStage` requires the exact source runtime/token/owner vector, same current staging attempt, source identity/spatial/site owners, staged identity registry and WorldId, authored definitions, and TravelParty IDs. It validates token and owner evidence before capture and staging. | A private D package with RuntimeIdentityRegistry, legacy SpatialNetwork, Cities/NPCs, Persons, Genealogy, empty ExplorableSiteStore, and detached NPC F rows. D consumes C's staged identity/provenance context and uses the captured TravelParty IDs to restore reciprocal NPC links. |
| P12-E owners | `P12EDailyV1OwnerPackage.TryCaptureAndStage` requires the source runtime, exact completed token/vector, and a context carrying the same staging attempt plus staged C/D roots, staged time, statuses, event recorder, and logger. It captures each current E owner and stages against D Persons and C SpatialAuthority. | A private E package with its supported domain owners and unresolved typed PoliticalKnowledge bindings. Dependency resolution is deliberately left for P12-G/F. |
| P12-F owners | `P12FDailyV1OwnerCapture.TryCapture` validates the exact source runtime/composition/token/vector and captures PoliticalKnowledge, directives, ActorChoice, TravelParty, and Expedition. `TryStage` later requires the same token/vector/attempt and the staged C/D/E objects. | A private F package with its owners, detached NPC F rows, and unresolved E bindings. ActorChoice capture rejects unsupported temporal inputs under the Daily-v1 contract; this is an owner-package check, not proof of G's full capture-order integration. |

## Required composition order

The API dependencies imply this bounded order for a future G orchestrator:

1. Begin one `DailyCaptureStagingAttempt` for the source runtime, exact completed-boundary token, and exact owner-section vector for the private staging calls.
2. Capture F from the source runtime/composition using the exact token and owner vector. `TryCapture` validates those directly; it does not receive the staging-attempt object.
3. Stage C's continuation roots privately under the shared attempt.
4. Capture and stage D against the private roots and F's captured TravelParty IDs, which D needs to restore reciprocal NPC TravelParty links.
5. Capture and stage E against the same attempt, C roots, and D package.
6. Stage F against the same attempt and the staged C/D/E packages; resolve only the typed bindings admitted by the current contracts.
7. Build and validate the entire candidate runtime, establish fresh admission bound to that reconstructed runtime while preserving the source logical boundary, then publish it through one active-session root.

The P12-F regression `P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt` fixes the key temporal order: F capture, including the ActorChoice exact-zero check, precedes C root staging. Later D/E/F staging must retain the shared current attempt.

The sequence is supported by current method signatures, guards, and the cited F regression. There is no current P12-G orchestrator that performs the complete sequence. Step 7 remains a G/B admission and publication obligation; the source capture token is identity-bound to the original runtime and cannot be reused as the reconstructed runtime's admission token.

## Bootstrap composition-root object disposition

The `SimulationBootstrapComposition` constructor is the explicit normal-bootstrap handoff. The following maps each constructor argument to the current census/package boundary or records why it has no independent serialized owner row. This closes the top-level handoff list only; `SimulationRuntime`'s nested graph and every dynamic provider row still require the live-inventory proof above.

| Constructor object | Current disposition |
|---|---|
| `WorldId`, `SimulationGenesisManifest` | P12-C continuation roots; preserve the exact world identity and P9-B manifest/provenance. |
| `SimulationTime`, `CalendarDefinition` | Boundary time and authored calendar/configuration dependencies. Preserve the token's exact day and compatible calendar identity; neither is a separate row in the 299-section vector. |
| `SpatialNetworkRuntime` | P12-D legacy spatial-network Location and Route sections; it shares identity authority with `RuntimeIdentityRegistry`. |
| `DomainEventStore`, `HistoryStore` | Omitted noncausal read model and derived event subset respectively; they do not replace authoritative owner payload. |
| `ScheduledDirectiveStore` | P12-F `p12f.scheduled-directives`; the constructor checks it is the exact owner exposed by the runtime. |
| `NpcDecisionStore` | Omitted decision read model; the causal record sequence and the P12-D NPC facts have separate owners. |
| `NpcDecisionRecorder` | Global `p12f.npc-decision-occurrence-receipts` exact-empty census owner. The recorder's receipt identity token is distinct from the recorder object itself. |
| `SimulationRecordSequence` | P12-C `p12c.simulation-record-sequence`; the constructor validates the exact runtime owner witness. |
| `EconomyTransactionService` | Global `p12e.economy-keyed-sale-receipts` exact-empty census owner; its domain writes affect separately inventoried City/NPC owners. |
| `NpcChronicleService`, `NpcChronicleFormatter` | Chronicle is a derived query/read model; formatter is a presentation helper. Neither is an independent authoritative section. |
| `TravelPartyStore`, `TravelPartySystem` | P12-F `p12f.travel-parties`. The system shares the exact store and adds no separate owner row. |
| `SimulationRuntime` | Root for the registered fixed and dynamic census providers and owner-operation protocol; not an additional serialized section. |
| `RuntimeIdentityRegistry` | Four Required identity subsections and four ExplicitlyEmpty subsections; same instance must be shared with the runtime and legacy spatial network. |
| `RuntimeIdAllocator` | P12-C full typed counter snapshot; the live vector has event, decision, and travel-party counter sections while the remaining allocator kinds are preserved by the root snapshot. |
| `ExplorableSiteStore` | P12-D `p12d.explorable-sites` ExplicitlyEmpty row for this Daily-v1 profile; it is constructed and checked, not inferred absent. |
| `ExpeditionStore`, `ExpeditionSystem` | P12-F `p12f.expeditions` Required row. The system shares the installed store. Current Daily-v1 has no supported successful Expedition start because its site owner is exact-empty and its autonomy producer is not composed. Retain the owner; any future profile admission requires a fresh owner and producer audit. |

The constructor additionally creates the passive provider surfaces for Person/binding, City/NPC presence, per-NPC exact-zero receipts, City Market, ActorChoice, identity/allocator, E authorities, Genealogy, site, population, legacy spatial, and Expedition. It checks exact shared identity for ScheduledDirective, record sequence, allocator counters, RuntimeIdentityRegistry/SpatialNetwork/Site, and the two-per-NPC receipt provider inventory. Those exposed providers are not themselves extra domain owners.

## Remaining gates

This source audit narrows, but does not discharge, the P12-G design entry gates:

* Validate all 299 expected sections against every owner/provider actually composed by normal Daily-v1 bootstrap, including dynamic membership/cardinality transitions, conditional owners, exact-zero targets, and omitted noncausal read models. The current registry/census is not a generic exhaustive graph walk.
* Complete exact-tip checks for every B–F package's schemas, owner identity, revision/cardinality, bindings, and failure behavior, including the full integrated C/F-capture/D/E/F-stage path. Existing local package tests do not establish whole-graph parity or publication.
* Independently review and implement the fresh restored-boundary admission seam. Preserve the completed day/core sequence without advancing the candidate runtime.
* Move all post-publication `TesteSimulacao` consumers to one active-session holder; verify a single swap and that failures before it leave the old graph authoritative.
* Perform whole-graph admission/rejection, failure-atomicity, no-replay, and continuation-parity validation on the supported Daily-v1 profile.

No gameplay or profile scope is added. P12-B through P12-F remain promoted only within their recorded limits; P12-A and P13 statuses are unchanged.
