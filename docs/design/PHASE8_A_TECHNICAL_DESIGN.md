# P8-A — Factual Geography Technical Design

**Design base:** `1f4651e99db2c357dd3be3c6b9284d104379f706` (`codex/phase7/canonical` baseline)
**Candidate branch:** `codex/phase8/P8ATechnicalDesign`
**Scope:** P8-A technical design only. This document changes no domain architecture, Phase Brief, Phase State, or executable code.
**Readiness:** no unresolved architecture or product choice is identified. Implementation readiness remains subject to independent technical review of this design and normal candidate readiness checks. The sample world's exact scale value/unit remain authored content and are not fixed here.

## 1. Authority and scope

This design is subordinate to `docs/SIMULATION_ARCHITECTURE.md` §69, `docs/phases/PHASE8_BRIEF.md`, and the scheduling and reconstruction rules in `docs/EXECUTION_MODEL.md`. The current `docs/PHASE7_STATE.md` records the promoted spatial reference foundation; the current checkout has no `docs/PHASE8_STATE.md`, consistent with Phase 8 having no delivered checkpoint record. No ADR is cited by the Phase 8 Brief, and no separate ADR file is present in the docs tree.

P8-A establishes a finite, manually authored set of factual regional Hexes, stable Hex identity and semantic coordinates, regional scale, terrain assignment, and neutral Location anchors. It does not implement passages, crossings, traversal, routes, pathfinding, travel, spatial Knowledge, world generation, runtime expansion, rendering, or persistence. This is a bounded design, not authorization to implement or promote.

The coordinate representation below is a technical realization of §69's existing requirement for semantic coordinates sufficient to derive hex adjacency. It does not change the accepted spatial semantics: identity remains distinct from coordinate, only existing Hexes participate, and geometric adjacency remains distinct from traversal.

## 2. Canonical code baseline

At the base SHA, `SpatialAuthorityStore` is the existing world-bound authority for stable Hex and Location identity and the LocalTopology-to-Location bridge:

- `Assets/_Project/Scripts/SpatialAuthority.cs` defines `HexId`, `LocationId`, `HexRecord`, `LocationRecord`, `SpatialReference`, and `SpatialAuthorityStore`. IDs are typed, ordinal string identities independent of coordinates. `HexRecord` currently contains only an ID. A `LocationRecord` contains one `AnchorHexId`; registration requires that Hex to exist. The store owns registration, lookup, resolution, deterministic ordering, revision, cloning, mutation-guard binding, and invariant validation.
- The current Hex model does **not** hold coordinates or terrain, derive neighbors, define a finite grid, or carry regional scale. `SpatialReferenceKind` currently supports Hex, Location, and SubLocation, but not Crossing.
- `SimulationRuntime` clones and composes the injected spatial authority (`CloneSpatialAuthorityStore`) without adding spatial daily processing. The source store and runtime store are separate authorities after composition.
- `WorldStateSnapshot`, `WorldStateCanonicalWriter`, `WorldStateDiff`, `WorldStateInvariantValidator`, and `WorldStateFormatter` currently expose Hex IDs and Location-to-Hex anchors. They do not project coordinates, terrain assignment, or scale. Added authoritative fields therefore need clone, snapshot, canonical, diff, formatter, and invariant coverage together.
- P7-D0 deliberately did not migrate `CityRuntime` or `ExplorableSiteRuntime` into `LocationRecord`. Those runtime objects still carry legacy `SpatialLocationRuntime` references. P7-D0 binds existing LocalTopology/SubLocation to a `LocationId` only through an explicit `SpatialLocalTopologyBinding`.
- `SpatialNetworkRuntime` and `TravelSystem` are transitional, fixed-`TravelDays` systems. Travel is direct-route and `NpcRuntime`-owned today. They must not become P8-A geography authority or be changed as part of this checkpoint.

Relevant existing tests are `SpatialAuthorityTests.cs`, `BattleSpatialBindingTests.cs`, `ArmedForceSpatialPositionTests.cs`, and the world-state diagnostics suites. They establish stable identity, anchors, clone isolation, deterministic projection, reference resolution, and compatibility relied on by Phase 7.

## 3. P8-A acceptance mapping

| Approved P8-A outcome | Technical boundary and evidence |
|---|---|
| A finite manually authored geography exists as factual world state before the first simulated boundary, independent of observer, scene, or rendering state. | Compose an explicit finite spatial definition into the runtime's spatial authority before the initial simulation boundary. The finite set is the set of registered geographic Hex records; no query or coordinate neighborhood implicitly creates a cell. Validate through a small authored fixture/composition test. No procedural generator or runtime expansion. |
| Hex has stable semantic identity distinct from semantic coordinate and from rendering coordinates. | Keep `HexId` as instance identity. Add an immutable axial integer coordinate pair `(q, r)` to each geographic Hex. Equality is equality of both integers; canonical order is lexicographic `(q, then r)`. Reject duplicate IDs and duplicate coordinates within one geographic world. Never derive IDs or coordinates from asset load order, RuntimeIds, discovery order, or rendering transforms. |
| Existing semantic coordinates suffice to derive hexagonal geometric adjacency, while only existing cells participate. | Derive the six candidate coordinates using deltas `{(1,0), (1,-1), (0,-1), (-1,0), (-1,1), (0,1)}`. A candidate is a neighbor only if a geographic Hex is registered at that exact pair. Return existing neighbors in lexicographic coordinate order. Do not store derived adjacency as passage truth or instantiate missing cells. Add no distance, pathfinding, or rendering projection. |
| Regional scale and terrain are factual context, not universal travel cost. | The effective authoritative world context has exactly one resolved, immutable, world-local physical-scale convention for a P8-A geography. Its authored content/world-input provenance and resolved identity are retained for reconstruction. The convention maps one geometric neighbor step in the uniform axial grid to an authored physical-distance value and unit; it does not define travel time or cost. Applied terrain is a stable reference to content-defined terrain; do not introduce a closed universal terrain enum or `Hex.TravelTime`. The exact scale value and unit for the sample world remain deferred to authored content. |
| Locations are stable neutral anchors; each P8-A Location anchors to exactly one existing Hex. | Preserve `LocationId` and `LocationRecord.AnchorHexId` semantics. Validate the single registered anchor. Do not make Location a base class or migrate City/Site ownership in P8-A. |
| Geography is deterministic and reconstructible. | Extend authority clone, ordered projection, diagnostics, and invariants for every new factual field. Registration order must not change neighbor queries or canonical snapshots. The manual slice must give explicit stable IDs, coordinate pairs, terrain references, anchors, and a resolved scale input with provenance. |

P8-A provides geometry only. `HEX ADJACENCY != TRAVERSABILITY`; P8-B owns passage truth. Sharing a Hex does not create containment or connectivity. P8-A does not reveal its geography through Knowledge.

## 4. Ownership and proposed interfaces

The proposals below are implementation ownership and API seams, not additional architectural commitments.

### Spatial truth owner and coordinates

Keep one world-bound authority: the existing `SpatialAuthorityStore`, or a narrowly composed geography component that remains owned and cloned by it. Do not create a parallel grid authority beside it. Extend the existing Hex record with immutable semantic coordinate and applied terrain reference; retain `HexId` as its identity. Keep Location-to-Hex anchors in the existing store.

Use an immutable axial coordinate value `HexCoordinate(q, r)` with two integer components. Pair equality compares `q` and `r`; canonical ordering compares `q` first, then `r`. The six geometric neighbor deltas are exactly:

```text
(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)
```

Neighbor lookup adds each delta, consults the finite coordinate index, and returns only registered geographic Hexes. Neighbor results are sorted lexicographically by coordinate, independent of delta iteration or registration order. Coordinate arithmetic must not wrap on integer overflow; unrepresentable candidates are absent. The coordinate pair is semantic and does not prescribe a rendering projection. No distance metric, pathfinding algorithm, or travel calculation is added.

The minimal conceptual surface is:

```text
P8-A geographic Hex record: HexId + HexCoordinate(q,r) + applied TerrainDefinitionId
P8-A geography context: registered geographic Hexes, anchored Locations, exactly one world-local scale record; identity-only P7 contexts remain scale-free
SpatialAuthorityStore: query existing geometric neighbors for a HexId
```

This surface must not expose an implicit `GetOrCreateNeighbor` operation. It must not return roads, crossings, movement capability, legal permission, route cost, or actor belief.

### World-local scale and global configuration

A P8-A geography has one resolved physical-scale convention in the effective authoritative world context. It defines the physical distance represented by one adjacent axial-grid step under this world’s uniform regional scale. Keep that single immutable record with the world-bound spatial authority (or a value owned and cloned by it), so `SimulationRuntime` exposes one composed authority rather than a second scale source. The record retains:

- a stable identity for the resolved world-local convention;
- the stable source identity and version/revision that supplied it (content definition or world-authoring input, including a stable override identity when applicable);
- the resolved distance-per-neighbor-step numeric value and unit once authored for the world.

The world definition/content input feeds resolution once during world composition. Runtime consumers read the resolved record; they do not silently reread assets or recompute it during `AdvanceDay`. The exact value and unit are deferred from this design and must be authored explicitly for the finite world before its first simulated boundary.

`EffectiveSimulationConfiguration` remains the single authority for global simulation policies and calculation parameters, including `EffectiveTravelConfiguration`. It is not a snapshot of world content and must not duplicate the world-local physical scale. A future travel calculation may consume both the world-local resolved scale and global travel parameters as separate inputs; P8-A does not combine them into travel duration/cost or define a formula. Reconstruction carries the resolved world-scale record and the effective global configuration independently through their respective authorities; the spatial scale record does not duplicate global configuration fields.

### Composition and compatibility

Use existing `SimulationRuntime` spatial-store composition and cloning as the runtime boundary. The authored slice must be composed before day zero / the first simulated boundary; it must not depend on a loaded Unity scene or discovered cells. Do not add work to `AdvanceDay`.

Preserve Phase 7 identity-only spatial uses that do not claim to be a geographic grid. When a P8-A geographic context is present, all geographic Hexes in that context require coordinates and the world-local scale record. A legacy spatial reference store with no P8-A geography must not be mistaken for a finite grid or be forced to invent coordinates/scale solely to resolve Battle references.

P8-A owns geographic facts and Location anchors only. P8-C owns the City/Site legacy bridge and Person-backed presence. P8-B owns barriers, crossings, passage state and contextual traversal queries. Neither downstream checkpoint may turn geometric neighbors into passage truth or introduce a second spatial authority for the same trip.

### Likely file ownership

| Files | P8-A ownership |
|---|---|
| `Assets/_Project/Scripts/SpatialAuthority.cs` and, if a clean type boundary requires it, one new spatial-geography source file | Axial coordinate value, coordinate index, coordinate-bearing geographic Hex value, terrain reference, world-local scale record/provenance, finite neighbor query, validation and cloning. |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | Only the `CloneSpatialAuthorityStore` / composition path needed to preserve geographic fields and scale provenance and bind the authoritative store. No daily-loop changes. |
| `Assets/_Project/Scripts/Diagnostics/WorldStateSnapshot.cs`, `WorldStateCanonicalWriter.cs`, `WorldStateDiff.cs`, `WorldStateFormatter.cs`, `WorldStateInvariantValidator.cs` | Snapshot, deterministic serialization/formatting, value diffs, and coordinate/terrain/scale provenance invariants for added factual geography. |
| `Assets/_Project/Tests/EditMode/Editor/SpatialAuthorityTests.cs` and focused world-state diagnostics tests | P8-A assertions for finite membership, coordinate identity/separation, exact six-neighbor behavior, terrain/scale provenance, anchors, cloning, order independence, and diagnostics. |
| `Assets/_Project/Scripts/SpatialRuntime.cs`, `TravelSystem.cs`, `NpcRuntime.cs`, `SimulationRuntime.AdvanceDay`, `CityRuntime.cs`, `ExplorableSiteRuntime.cs`, LocalTopology ownership | No P8-A ownership. Existing consumers are regression boundaries; City/Site migration belongs to P8-C. |

`SpatialAuthority.cs`, `SimulationRuntime.cs`, and diagnostics are shared architectural hotspots. P8-A should be integrated before B/C implementation work that consumes the new contract. B and C should then work in isolated branches with explicit ownership of their overlapping spatial identity/segment boundary.

## 5. Migration and integration order

1. Add the immutable axial coordinate value and deterministic finite-neighbor query using the convention in §4. Keep coordinates semantic and separate from IDs and rendering.
2. Define the finite authored geography input and its one world-local resolved scale record. Retain source identity/version and resolved identity; author an explicit value/unit for the sample world before runtime composition. Validate stable IDs, coordinate uniqueness, terrain content references, anchors, and scale provenance.
3. Add immutable geographic facts to the existing spatial authority boundary. Preserve existing identity-only Phase 7 uses; P8-A geographic contexts require all geographic Hex coordinates and one scale record. Update `Clone()` and runtime composition atomically with the new fields.
4. Extend snapshot, canonical writer, diff, formatter, and invariant validation so no new authoritative value or reconstruction identity is invisible to world-state diagnostics. Preserve stable semantic ordering.
5. Add focused P8-A tests for registration/rejection atomicity, six-neighbor results over existing cells only, order independence, clone fidelity, terrain/scale provenance, anchor validity, diagnostic fidelity, and pre-boundary authored composition. Run affected Phase 7 spatial/diagnostic regressions during implementation.
6. Independently review the full P8-A diff against the recorded base, including architecture boundaries and reconstruction sensitivity. Integrate and validate P8-A before treating its behavior as a promoted dependency for B/C. Only after stable P8-A contracts/capabilities should B and C enter parallel isolated implementation; P8-D waits for the required B/C contracts/capabilities, and P8-E waits for promoted B/C/D plus integration validation.

Do not use this order to create checkpoint IDs beyond the IDs already approved in `PHASE8_BRIEF.md`.

## 6. Validation plan for implementation

No tests were run for this documentation-only artifact. After independent technical review and implementation authorization, validate at least:

- focused `SpatialAuthorityTests` for typed IDs/coordinates, pair equality and lexicographic ordering, coordinate uniqueness, the exact six candidate deltas, lookup of only registered neighbors, deterministic results, anchor checks, mutation guard/revision behavior, and clone equivalence;
- focused snapshot/canonical/diff/formatter/invariant tests proving coordinates, terrain, world-local scale value/unit, and scale provenance appear as factual state and differ when changed;
- composition before the first simulated boundary and proof that runtime composition does not depend on rendering or discovery;
- Phase 7 spatial consumers: `BattleSpatialBindingTests`, `ArmedForceSpatialPositionTests`, and the relevant Battle execution/diagnostic tests, including legacy identity-only stores without geography context;
- all relevant EditMode domain suites, ALL EditMode, and the complete official Smoke suite before promotion, plus `git diff --check`;
- no long-run gate is indicated by P8-A alone because it does not change the daily loop or long-horizon causal behavior. Reassess if implementation expands scope.

Green tests do not approve promotion. The coordinate convention and scale provenance are specified here, but the authored sample-world value/unit must still be present and validated in the implementation.

## 7. Reconstruction-sensitive truth

A future reconstruction of the same initial geography must recover, at minimum:

- the finite membership of the world’s geographic Hex set;
- each stable `HexId` and its axial integer pair `(q, r)`;
- the coordinate convention version and canonical pair ordering used to derive adjacency;
- each applied terrain reference and the corresponding compatible content definition/version;
- each stable `LocationId` and its single `AnchorHexId`;
- the single resolved world-local physical-scale convention, its stable resolved identity, source identity/version or world-input revision, exact authored value, and unit;
- the effective global configuration as an independently resolved input under its own authority, without copying global travel calculation parameters into spatial world content;
- deterministic composition/order rules and any external authored inputs that affect these facts.

This declaration is for architecture/design review; P8-A does not implement save, replay, or fork. Terrain/scale content definitions and world-authoring inputs are sources; their resolved application to this world is factual context. No Unity transform, scene ordering, RuntimeId allocator sequence, renderer, or discovered-cell cache may be required to reproduce authoritative geography.

## 8. Remaining implementation-readiness requirements

The previous review identified the coordinate representation as unspecified. This design now resolves that technical choice within §69's open representation detail: immutable integer axial pairs `(q, r)`, pair equality, lexicographic ordering, and the exact six-neighbor deltas in §4. This preserves, rather than changes, accepted semantics. No architecture or Brief amendment is required for that choice.

Before a P8-A implementation wave is marked `READY_FOR_IMPLEMENTATION`:

1. an independent technical reviewer must accept this artifact against its recorded canonical base and current §69/Brief, including the axial adjacency law and world-scale/configuration boundary; and
2. the reviewed implementation contract requires the finite world author to provide one explicit scale value/unit and stable provenance before the geography is composed at the first simulated boundary. The implementation selects the exact value/unit as world-local content; it is not fixed by this design review or a universal constant.

The usual branch/base verification, isolated implementation, targeted and required regression validation, independent code review, integration checks, and promotion approval still apply. The artifact itself does not authorize implementation, create a Phase State record, or promote a capability.
