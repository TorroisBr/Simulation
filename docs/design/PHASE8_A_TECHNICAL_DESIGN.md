# P8-A — Factual Geography Technical Design

**Design base:** `1f4651e99db2c357dd3be3c6b9284d104379f706` (`codex/phase7/canonical` baseline)
**Candidate branch:** `codex/phase8/P8ATechnicalDesign`
**Scope:** P8-A technical design only. This document changes no domain architecture, Phase Brief, Phase State, or executable code.
**Readiness:** technical design candidate; implementation readiness is blocked by the coordinate-representation gate in §8.

## 1. Authority and scope

This design is subordinate to `docs/SIMULATION_ARCHITECTURE.md` §69, `docs/phases/PHASE8_BRIEF.md`, and the scheduling and reconstruction rules in `docs/EXECUTION_MODEL.md`. The current `docs/PHASE7_STATE.md` records the promoted spatial reference foundation; the current checkout has no `docs/PHASE8_STATE.md`, consistent with Phase 8 having no delivered checkpoint record. No ADR is cited by the Phase 8 Brief, and no separate ADR file is present in the docs tree.

P8-A establishes a finite, manually authored set of factual regional Hexes, stable Hex identity and semantic coordinates, regional scale, terrain assignment, and neutral Location anchors. It does not implement passages, crossings, traversal, routes, pathfinding, travel, spatial Knowledge, world generation, runtime expansion, rendering, or persistence. This is a bounded design, not authorization to implement or promote.

## 2. Canonical code baseline

At the base SHA, `SpatialAuthorityStore` is the existing world-bound authority for stable Hex and Location identity and the LocalTopology-to-Location bridge:

- `Assets/_Project/Scripts/SpatialAuthority.cs` defines `HexId`, `LocationId`, `HexRecord`, `LocationRecord`, `SpatialReference`, and `SpatialAuthorityStore`. IDs are typed, ordinal string identities independent of coordinates. `HexRecord` currently contains only an ID. A `LocationRecord` contains one `AnchorHexId`; registration requires that Hex to exist. The store owns registration, lookup, resolution, deterministic ordering, revision, cloning, mutation-guard binding, and invariant validation.
- The current Hex model does **not** hold coordinates or terrain, derive neighbors, define a finite grid, or carry a regional scale. `SpatialReferenceKind` currently supports Hex, Location, and SubLocation, but not Crossing.
- `SimulationRuntime` clones and composes the injected spatial authority (`CloneSpatialAuthorityStore`) without adding spatial daily processing. The source store and runtime store are separate authorities after composition.
- `WorldStateSnapshot`, `WorldStateCanonicalWriter`, `WorldStateDiff`, `WorldStateInvariantValidator`, and `WorldStateFormatter` currently expose Hex IDs and Location-to-Hex anchors. They do not project coordinates, terrain assignment, or scale. Added authoritative fields therefore need clone, snapshot, canonical, diff, formatter, and invariant coverage together.
- P7-D0 deliberately did not migrate `CityRuntime` or `ExplorableSiteRuntime` into `LocationRecord`. Those runtime objects still carry legacy `SpatialLocationRuntime` references. P7-D0 binds existing LocalTopology/SubLocation to a `LocationId` only through an explicit `SpatialLocalTopologyBinding`.
- `SpatialNetworkRuntime` and `TravelSystem` are transitional, fixed-`TravelDays` systems. Travel is direct-route and `NpcRuntime`-owned today. They must not become P8-A geography authority or be changed as part of this checkpoint.

Relevant existing tests are `SpatialAuthorityTests.cs`, `BattleSpatialBindingTests.cs`, `ArmedForceSpatialPositionTests.cs`, and the world-state diagnostics suites. They establish stable identity, anchors, clone isolation, deterministic projection, reference resolution, and compatibility relied on by Phase 7.

## 3. P8-A acceptance mapping

| Approved P8-A outcome | Technical boundary and evidence |
|---|---|
| A finite manually authored geography exists as factual world state before the first simulated boundary, independent of observer, scene, or rendering state. | Compose an explicit finite spatial definition into the runtime's spatial authority before the initial simulation boundary. The finite set is the set of registered Hex records; no query or coordinate neighborhood implicitly creates a cell. Validate through a small authored fixture/composition test. No procedural generator or runtime expansion. |
| Hex has stable semantic identity distinct from semantic coordinate and from rendering coordinates. | Keep `HexId` as instance identity. Add an immutable typed coordinate value to each Hex only after the gate in §8 is resolved. Reject duplicate Hex IDs and duplicate coordinates within one world. Never derive IDs from array order, asset load order, RuntimeIds, or rendering transforms. |
| Existing semantic coordinates suffice to derive hexagonal geometric adjacency, while only existing cells participate. | Expose a pure deterministic neighbor query over the registered finite set, derived from the approved coordinate convention. Return only registered neighbors in stable order. Do not store derived adjacency as authoritative passage or instantiate missing neighbors. This query does not answer traversability. |
| Regional scale and terrain are factual context, not universal travel cost. | Store the authored world’s coherent regional scale convention/value in spatial world context. Store applied terrain as a stable reference to content-defined terrain; do not introduce a closed universal terrain enum or `Hex.TravelTime`. Terrain definitions remain content; terrain applied to a Hex is World Truth. Exact world-specific scale content is authored by the finite slice, not made a global constant here. |
| Locations are stable neutral anchors; each P8-A Location anchors to exactly one existing Hex. | Preserve `LocationId` and `LocationRecord.AnchorHexId` semantics. Validate the single registered anchor. Do not make Location a base class or migrate City/Site ownership in P8-A. |
| Geography is deterministic and reconstructible. | Extend authority clone, ordered projection, diagnostics, and invariants for every new factual field. Registration order must not change neighbor queries or canonical snapshots. The manual slice must give explicit stable IDs, coordinates, terrain references, anchors, and scale. |

P8-A provides geometry only. `HEX ADJACENCY != TRAVERSABILITY`; P8-B owns passage truth. Sharing a Hex does not create containment or connectivity. P8-A does not reveal its geography through Knowledge.

## 4. Ownership and proposed interfaces

The proposals below are implementation ownership and API seams, not additional architectural commitments.

### Spatial truth owner

Keep one world-bound authority: the existing `SpatialAuthorityStore`, or a narrowly composed geography component that remains owned and cloned by it. Do not create a parallel grid authority beside it. Extend the existing Hex record with immutable semantic coordinate and applied terrain reference; retain `HexId` as its identity. Keep Location-to-Hex anchors in the existing store.

The coordinate type and its six-neighbor operation are gated. Once approved, the minimal conceptual surface is:

```text
HexRecord: HexId + approved semantic coordinate + applied TerrainDefinitionId
SpatialAuthorityStore: registered Hexes, anchored Locations, regional scale context
SpatialAuthorityStore: query existing geometric neighbors for a HexId
```

This surface must not expose an implicit `GetOrCreateNeighbor` operation. It must not return roads, crossings, movement capability, legal permission, route cost, or actor belief.

Regional scale belongs to the authored world/content context because §69 says each world using physical travel has one coherent convention. Keep it out of `TravelSystem` and out of a universal Hex constant. The precise value/unit for the P8-A sample is authored data and is not selected by this design. Terrain type/value is content-defined; the per-Hex applied definition reference is factual state.

### Composition and compatibility

Use existing `SimulationRuntime` spatial-store composition and cloning as the runtime boundary. The authored slice must be composed before day zero / the first simulated boundary; it must not depend on a loaded Unity scene or discovered cells. Do not add work to `AdvanceDay`.

P8-A owns geographic facts and Location anchors only. P8-C owns the City/Site legacy bridge and Person-backed presence. P8-B owns barriers, crossings, passage state and contextual traversal queries. Neither downstream checkpoint may turn geometric neighbors into passage truth or introduce a second spatial authority for the same trip.

### Likely file ownership

| Files | P8-A ownership |
|---|---|
| `Assets/_Project/Scripts/SpatialAuthority.cs` and, if a clean type boundary requires it, one new spatial-geography source file | Coordinate-bearing Hex value, terrain reference, scale context, finite neighbor query, validation and cloning. The coordinate representation is excluded until its gate is resolved. |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | Only the `CloneSpatialAuthorityStore` / composition path needed to preserve new fields and bind the authoritative store. No daily-loop changes. |
| `Assets/_Project/Scripts/Diagnostics/WorldStateSnapshot.cs`, `WorldStateCanonicalWriter.cs`, `WorldStateDiff.cs`, `WorldStateFormatter.cs`, `WorldStateInvariantValidator.cs` | Snapshot, deterministic serialization/formatting, value diffs, and cross-reference/coordinate/scale invariants for added factual geography. |
| `Assets/_Project/Tests/EditMode/Editor/SpatialAuthorityTests.cs` and focused world-state diagnostics tests | P8-A assertions for finite membership, coordinate identity/separation and geometry after gate approval, terrain/scale facts, anchors, cloning, order independence, and diagnostics. |
| `Assets/_Project/Scripts/SpatialRuntime.cs`, `TravelSystem.cs`, `NpcRuntime.cs`, `SimulationRuntime.AdvanceDay`, `CityRuntime.cs`, `ExplorableSiteRuntime.cs`, LocalTopology ownership | No P8-A ownership. Existing consumers are regression boundaries; City/Site migration belongs to P8-C. |

`SpatialAuthority.cs`, `SimulationRuntime.cs`, and diagnostics are shared architectural hotspots. P8-A should be integrated before B/C implementation work that consumes the new contract. B and C should then work in isolated branches with explicit ownership of their overlapping spatial identity/segment boundary.

## 5. Migration and integration order

1. Resolve and canonically record the semantic coordinate convention and the exact adjacency law needed by a finite hex set. Re-review this design against the decision; do not code around it with rendering coordinates or opaque coordinates.
2. Define the finite authored geography input and validate stable IDs, coordinate uniqueness, content references, anchors, and a coherent world-specific regional scale before constructing the runtime authority.
3. Add immutable geographic facts and deterministic finite-neighbor query to the existing spatial authority boundary. Preserve existing Hex/Location APIs where source compatibility permits; update `Clone()` and runtime composition atomically with the new fields.
4. Extend snapshot, canonical writer, diff, formatter, and invariant validation so no new authoritative value is invisible to world-state diagnostics. Preserve stable semantic ordering.
5. Add focused P8-A tests for registration/rejection atomicity, order independence, clone fidelity, only-existing-neighbor results, anchor validity, diagnostic fidelity, and pre-boundary authored composition. Run affected Phase 7 spatial/diagnostic regressions during implementation.
6. Independently review the full P8-A diff against the recorded base, including architecture boundaries and reconstruction sensitivity. Integrate and validate P8-A before treating its behavior as a promoted dependency for B/C. Only after stable P8-A contracts/capabilities should B and C enter parallel isolated implementation; P8-D waits for the required B/C contracts/capabilities, and P8-E waits for promoted B/C/D plus integration validation.

Do not use this order to create checkpoint IDs beyond the IDs already approved in `PHASE8_BRIEF.md`.

## 6. Validation plan for implementation

No tests were run for this documentation-only artifact. When implementation is authorized and the coordinate gate is closed, validate at least:

- focused `SpatialAuthorityTests` for typed coordinates/IDs, coordinate uniqueness, geometric neighbors over existing Hexes, deterministic order, anchor checks, mutation guard/revision behavior, and clone equivalence;
- focused snapshot/canonical/diff/formatter/invariant tests proving coordinates, terrain, and world scale appear as factual state and differ when changed;
- composition before the first simulated boundary and proof that runtime composition does not depend on rendering or discovery;
- Phase 7 spatial consumers: `BattleSpatialBindingTests`, `ArmedForceSpatialPositionTests`, and the relevant Battle execution/diagnostic tests;
- all relevant EditMode domain suites, ALL EditMode, and the complete official Smoke suite before promotion, plus `git diff --check`;
- no long-run gate is indicated by P8-A alone because it does not change the daily loop or long-horizon causal behavior. Reassess if implementation expands scope.

Green tests do not settle the semantic coordinate gate or approve promotion.

## 7. Reconstruction-sensitive truth

A future reconstruction of the same initial geography must recover, at minimum:

- the finite membership of the world’s Hex set;
- each stable `HexId` and semantic coordinate under the approved convention, including any convention-level origin/orientation needed to interpret it;
- each applied terrain reference and the corresponding compatible content definition/version;
- each stable `LocationId` and its single `AnchorHexId`;
- the authored world-specific regional scale convention, unit, and value;
- deterministic composition/order rules and any external authored inputs that affect these facts.

This declaration is for architecture/design review; P8-A does not implement save, replay, or fork. Terrain/scale content definitions are inputs, while their resolved application to the authored world is factual context. No Unity transform, scene ordering, RuntimeId allocator sequence, renderer, or discovered-cell cache may be required to reproduce authoritative geography.

## 8. Unresolved gate: semantic coordinate representation

There is a direct planning mismatch that this design cannot settle. Architecture §69 says Hexes require semantic coordinates sufficient to derive geometric adjacency, but explicitly leaves the coordinate representation open. P8-A is approved to deliver stable semantic identity **and coordinates**. The current code has only `HexId`; no coordinate contract exists. Choosing a representation here would make a durable semantic decision that the architecture explicitly deferred.

**Gate:** P8-A is not `READY_FOR_IMPLEMENTATION` until the architecture authority records one of the following in the canonical architecture and the Brief is checked for consistency:

1. an approved semantic coordinate convention and adjacency law sufficient for stable regional hex geometry; or
2. an explicit revision of P8-A’s coordinate acceptance and dependency contract, if semantic coordinates are to remain deferred.

This design recommends neither outcome and names no coordinate scheme. Until the gate is resolved, the public coordinate type, equality/canonical form, adjacency formula, uniqueness rules for coordinates, and any coordinate migration contract remain unspecified. A technical review may approve this document’s ownership and migration plan conditionally, but cannot certify implementation readiness against the unresolved acceptance.

Other values are intentionally scoped: the sample world’s scale is authored per world rather than chosen as a universal constant; City/Site-to-Location bridging remains P8-C; passage truth remains P8-B. These do not resolve or weaken the coordinate gate.
