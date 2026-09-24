# Phase 8-B/C Shared Segment and Identity Contract

**Status:** bounded technical design for independent review; no implementation authorization or capability promotion.
**Design base:** approved P8-A technical design at `ec4e796ddc955b72d7623e7647924aa3f76e3474`.
**Authority:** `SIMULATION_ARCHITECTURE.md` §69, the Phase 8 Brief, and the P8-A technical design. This document chooses implementation seams only where those authorities already determine the meaning.

## 1. Purpose and readiness

P8-B owns factual passage/traversal facts. P8-C owns the City/Site-to-spatial-anchor bridge and factual position for Person-backed civil travelers. Their parallel implementation needs a shared meaning for a regional segment, a stable way to identify the traversal choice over it, and a position representation that remains meaningful while a Person is between anchors.

This contract makes those meanings explicit without treating adjacency as traversal, merging World Truth with actor Knowledge, or making `NpcRuntime` the persistent authority. It targets the P8-A geographic contract: stable `HexId`, semantic axial coordinate, finite registered geography, `LocationId` with one `AnchorHexId`, and geometric-neighbor queries. P8-B and P8-C may design to this contract now. Their implementation still depends on the relevant P8-A capability being implemented and promoted as required by the Brief and Roadmap.

## 2. Decisions inherited from architecture

The following are invariants, not options for P8-B/C:

- `HexId`, `LocationId`, `CrossingId`, and any persistent `ConnectionId` are semantic identities. IDs are distinct from names, coordinates, content-definition IDs, and runtime identities.
- A finite geometry query returns only registered Hexes. `Hex adjacency != traversability`.
- Terrain is contextual input, not an absolute travel duration. Barriers may span multiple regional boundaries. A Crossing is a concrete way to cross or overcome a Barrier; it is not automatically a Location.
- City and Site remain domain-owned and refer to neutral `LocationId` anchors. A common Hex or anchor does not imply containment, entry, access, or a traversable link.
- Facts about passage condition and position belong to World Truth. Actor beliefs, route assumptions, and route selection belong to Knowledge/decision state. Execution revalidates current factual passage truth and cannot reveal an unobserved cause by itself.
- A moving Person is identified by persistent `PersonId`. Runtime/NPC object identity, loaded representation, rendering coordinates, and legacy `SpatialLocationRuntime.RuntimeId` are not persistent spatial identity.
- A traveler retains factual position in transit through dormancy or unloading. A trip using the new model has one spatial/execution authority; the transitional direct-route/`TravelDays` model cannot also own that trip.
- State and selection that affect outcomes have stable semantic ordering and must be reconstructible in principle. No save format is introduced here.

## 3. Segment and traversal identity

### 3.1 Regional geometric boundary

Use a conceptual `HexBoundaryKey` for the interface between two adjacent Hexes. It is a **derived key**, not another registered world object and not proof of passage:

```text
HexBoundaryKey = unordered pair of distinct registered HexIds
canonical endpoint order = ordinal comparison of HexId.Value
```

The pair is valid only when the P8-A finite geography reports the endpoints as geometric neighbors. It is independent of coordinate order and travel direction; axial coordinates establish adjacency, while the stable endpoint identities establish the key. Direction for a particular move is carried separately as ordered `FromHexId` and `ToHexId`. Reversing direction therefore selects the same geometric boundary, but may produce a different directional traversal result if future content explicitly defines one.

The key names the shared regional boundary only. It does not identify a road, Bridge, Crossing, Barrier, traversability state, or route. Missing key lookup means “no such registered adjacent boundary,” not “blocked passage.”

### 3.2 Traversal option over a boundary

A traversal option is identified by the stable ID of the persistent fact that supplies it:

- an explicit persistent traversal connection uses its `ConnectionId`;
- a traversal through a concrete crossing uses its `CrossingId`, together with the stable boundary key it serves;
- wilderness traversal derived from current terrain/physical conditions uses the boundary key plus a stable, versioned traversal-rule/content identity. It does not create a persisted Connection merely because a query found it possible.

The P8-B API represents these as a typed discriminated reference (`TraversalOptionRef`) rather than a concatenated string. Its equality includes the kind and every identity component. The option is scoped to one boundary and validated against that boundary. If multiple options exist over the same boundary, they remain distinct and are returned in deterministic order by kind then ordinal stable identity. This permits a road and a crossing, or multiple crossings, to coexist without treating the endpoint pair as a unique route.

Persistent Connections and Crossings receive their own stable semantic identities when continuity, condition, or history matters, as §69 requires. A boundary key is never substituted for either identity. Content-definition identity/version and current mutable condition are separately represented; changing a Bridge’s condition does not change its ID or the Hex boundary.

### 3.3 Endpoints and anchors

Regional passage segments have two distinct Hex endpoints. The authoritative passage query accepts ordered endpoint `HexId`s and resolves the boundary through P8-A. It does not accept coordinate pairs or rendering positions as identity.

Position and trip origin/destination use typed physical references: `Hex`, `Location`, `SubLocation`, or `Crossing`. The existing D0 `SpatialReference` already handles the first three; the spatial foundation must add Crossing as a typed reference using stable `CrossingId` and validate resolution through the same world-bound spatial authority. A reference must resolve before it can be used as a factual position or trip endpoint.

A `Location` resolves to its one P8-A `AnchorHexId`; a `SubLocation` resolves through the explicit LocalTopology binding and then its owner’s `Location`; a `Crossing` resolves to its registered physical anchor/boundary facts. Resolution reports its typed reference and regional Hex context. It does not imply a local entrance or a traversable path between that reference and the resolved Hex.

The shared contract does not add a general local-routing graph. Moving between a City/Site anchor and a Hex, between two POIs in one Hex, or into a SubLocation requires an explicit local topology/entry transition if the travel slice needs it. Same-Hex membership alone never supplies that transition. P8-C maps legacy City/Site identity to the already-authoritative neutral anchor; it does not manufacture access links.

## 4. Passage state and query authority

P8-B owns factual passage structures and mutable passage/crossing condition in the existing world-bound `SpatialAuthorityStore` (or a narrowly composed child cloned and guarded by that store). There is one authority, not a second grid or network truth source. The store owns registered stable records and their current factual conditions; derived neighbor indexes and contextual query results are not separately authoritative.

Conceptual seams (names are illustrative, semantics are binding):

```text
TryGetGeometricBoundary(fromHexId, toHexId) -> boundary key or non-adjacent failure
GetTraversalOptions(boundaryKey) -> stable, sorted option references
TryEvaluatePassage(boundaryKey, optionRef, movementProfile, worldContext)
    -> current factual availability + contextual evaluation/provenance
TryChangePassageCondition(validated mutation) -> atomic world-truth mutation
```

The evaluation is a pure read of current world truth plus explicit context. It can report factual availability and contextual cost inputs/results needed by P8-B’s approved scope; it must not mutate Knowledge, choose a route, consume authoritative randomness, or persist an actor-specific cost on the Hex. Profile/rules and conditions that affect the answer are explicit inputs or stable world facts, never silently read from Unity assets. The result distinguishes “no geometric boundary,” “no current traversal option,” and “option currently unavailable.”

Passage mutation is validated against registered endpoints and option identity before changing state. Failure is atomic. A mutation changes the authoritative store revision exactly according to the established store mutation convention and exposes enough stable state to diagnostics/reconstruction. Road condition and crossing condition are facts of runtime world state, not silent content edits. A changed passage does not rewrite prior Knowledge or a chosen plan.

P8-B must not store adjacency as a passage, make `Hex.Blocked`, assume one Barrier per boundary, treat a road/Bridge as the source of adjacency, or equate physical traversal with actor capability or legal/social permission. P8-B has no authority to move a Person.

## 5. Factual position, transit, and trip state

### 5.1 Position representation

P8-C introduces a world-owned factual position authority keyed by `PersonId`, separate from `PersonRuntime`, `NpcRuntime`, and `TravelParty`. Its value is one of:

```text
At(SpatialReference)
InTransit(TraversalProgress)
```

`TraversalProgress` records the stable traversal-option reference, the canonical boundary key, ordered direction (`FromHexId` → `ToHexId`), and a deterministic progress value sufficient to resume at the simulation’s supported temporal granularity. It also identifies the last fully reached spatial reference/Hex. It contains no Unity transform, RuntimeId, or continuous rendering coordinate. It must resolve to a registered factual option/boundary at the point of mutation and preserve the single current position; neither endpoint is treated as already reached until the authoritative arrival transition commits.

The contract intentionally does not select a numeric progress unit, speed formula, interpolation, or sub-day update schedule: §69 requires deterministic progress and allows the daily time base to continue, while travel calculation formulas remain open. The implementation must pick a deterministic representation compatible with the promoted P8-A scale/configuration and P8-D plan contract, and tests must prove exact continuation at every supported boundary.

Crossing a boundary is an atomic position transition from `At(origin reference)` through `InTransit(...)` to `At(destination reference)`. The physical position store is the only source for “where is this Person now?” Position changes must validate the Person identity and spatial references before mutation and must leave state/revision unchanged on rejection. Position is not inferred from residence, a City’s legacy `Location` object, loaded NPC state, or the accepted destination.

### 5.2 Intent and progress ownership

Factual position/progress and the accepted trip plan are different facts. The Person position authority owns current position and the active segment progress. P8-D’s trip/plan authority owns selected route, destination, assumptions, and decision identity. The plan may point to the active segment; it cannot copy and become a second authority for current position/progress. Knowledge stores observations, not factual position or live passage condition.

P8-E’s departure, segment advance, interruption, replanning, and arrival operations must be one world-authorized transaction boundary. If an operation updates position and trip commitment together, validation occurs before either becomes visible, or the operation has a proven rollback path. On interruption, valid progress already made remains; the authority does not silently select another option or invent the hidden cause. A new route requires a new decision. The existing direct-route `TravelSystem` remains a compatibility path only for explicitly legacy trips and cannot participate in these same Person trips.

### 5.3 City/Site bridge

P8-C owns the explicit mapping from each migrated City/Site runtime entity to one stable `LocationId` and validates it against the spatial authority. The City/Site continues to own its domain identity and behavior; its `LocationId` is an anchor reference, not a second physical position. Mapping must be unique and deterministic, and migration must not infer position for an unrelated Person or alter settlement residence. The Person position store, not City/Site and not the legacy `NpcRuntime.CurrentLocation`, reports traveler position.

## 6. Determinism and reconstruction

Canonical projections and query results use semantic ordering:

- Hex boundary endpoints: ordinal `HexId.Value` order;
- traversal options: kind, then ordinal stable identity components;
- persistent barriers, crossings, connections, position records, and failures: their stable IDs / `PersonId.Value` ordinal order;
- direction and progress: explicit values, never collection or discovery order.

Equivalent authoritative state and explicit query inputs produce equivalent passage evaluations and candidate ordering. No query or route preview mutates authority or consumes authoritative RNG. Registration order, Unity scene/asset order, hash iteration, `RuntimeIdAllocator`, and renderer coordinates do not affect identity or results.

Reconstructible causal state for these contracts includes finite Hex membership and stable IDs/coordinates, Location anchors, stable Barrier/Crossing/Connection IDs and endpoint relations, content-rule revision identities used for derived traversal, current mutable passage conditions, each Person’s typed position and transit progress, and the independent selected trip/Knowledge/configuration inputs when those exist. Diagnostics must expose these facts in stable form; diagnostics are not thereby a save/replay implementation.

## 7. File ownership and parallel-work boundary

This is a source-ownership contract for later isolated implementation branches, not a claim that the features are already present.

| Track | Owns | Must not own |
|---|---|---|
| P8-B Factual Passages | New typed passage/option/boundary values; Crossing/Barrier/Connection records and factual conditions; passage queries/mutations; cloning, guards, invariants and diagnostics for passage facts; focused passage tests. Prefer a new `SpatialPassageAuthority.cs` or similarly isolated source so `SpatialAuthority.cs` changes stay narrow. | Person position, City/Site migration, `NpcRuntime`, Knowledge, route selection, daily travel progression, or `SimulationRuntime.AdvanceDay`. It must not create a second spatial authority. |
| P8-C Legacy Anchors and Civil Presence | City/Site ↔ `LocationId` bridge; `PersonId`-keyed position authority and typed `At`/`InTransit` values; position cloning, guards, invariants/diagnostics and focused presence tests. Own a new source such as `PersonSpatialPositionStore.cs`; alter `CityRuntime` / `ExplorableSiteRuntime` only for explicit anchor binding. | Passage truth/condition/query implementation, route choice, `PersonRuntime` duplicate spatial collections, `NpcRuntime`-only position, or daily loop changes. It must not make same-Hex an access link. |
| Shared spatial reference/Crossing seam | A single integration owner adds `CrossingId` and the typed Crossing variant/resolver extension to `SpatialAuthority.cs`; if required as part of P8-B, P8-B owns this shared edit and publishes its exact API before C integration. | Parallel duplicate edits to `SpatialReference`, `SpatialAuthorityStore`, or diagnostics core. |
| Runtime/diagnostics integration | One named integrator owns `SimulationRuntime` composition, `WorldStateSnapshot`/canonical writer/diff/formatter/invariants composition points, and cross-store transaction wiring after feature branches are ready. | Either worker independently editing these shared hotspots while the other is working. |

Both workers may edit separate new domain files and their separately owned tests in isolated worktrees. P8-B is integrated first because P8-C transit progress references the typed traversal-option/boundary identity. P8-C is then rebased/cherry-picked onto the integrated B result and adapted only to the published contract API. The integrator resolves shared `SimulationRuntime`, diagnostics, and any narrowly necessary `SpatialAuthority` overlap explicitly. Do not allow two writers in one checkout.

## 8. Integration and validation sequence

1. Independently review and accept this contract against its stated base, §69, the Phase 8 Brief, and P8-A design. Any architectural mismatch returns for clarification before implementation.
2. Implement and promote the relevant P8-A geography capability. P8-B/C can use the approved contract for design/test discovery meanwhile, but must not rely on unpromoted runtime behavior.
3. Start P8-B and P8-C in isolated feature branches from the same promoted P8-A baseline. Assign the shared Crossing/reference type edit once. P8-C codes against the published B type signatures if needed; integration order is B, then C.
4. Review both branches independently for mutation authority, atomic failure, freshness, deterministic output, cloning/diagnostic completeness, and scope. Integrate B, then C, then the shared runtime/diagnostic integration patch.
5. Run focused suites for geometric-boundary validation; multiple option identity/order; Barrier/Crossing/Connection relation and mutable condition; query purity; atomic mutation/revision; clone and reconstruction projection; City/Site anchor uniqueness/resolution; PersonId identity through materialization/dormancy/death; `At`/`InTransit` transitions and rejected stale references; no inferred access from same Hex; and deterministic results across insertion orders.
6. Run affected P7 spatial/Battle/ArmedForce regressions and existing legacy Travel suites to prove compatibility boundaries. Run relevant Phase 8 EditMode suites and required regression/promotion gates from the current Brief/State before canonical promotion. `git diff --check` is required. No daily-loop or long-run gate is implied by this contract; reassess if implementation adds autonomous progression or alters long-horizon behavior.

## 9. Unresolved decisions and non-goals

There is no unresolved semantic/product choice that blocks the shared B/C contract. The following are deliberately open in §69 or outside these tracks, and remain non-blocking for the contract:

- exact physical-scale value/unit for the authored finite sample;
- the progress numeric unit and movement-rate formula, to be selected by travel implementation under §69’s deterministic-progress requirement;
- final pathfinding algorithm, route score, and universal speed/cost formula;
- identity assignment strategy for procedural generation (the P8 slice is manually authored with explicit stable IDs);
- broad LocalTopology routing/entry semantics, save/replay format, content catalog, world generation/expansion, groups, encounters, and military movement.

The following are explicit non-goals: implementation or promotion; architecture/Brief/Roadmap/State edits; new checkpoint IDs; procedural geometry; persisting every geometric edge; universal traversal profiles; legal/social/military permission; route planning or Knowledge; automatic replanning; NpcRuntime materialization policy; a second travel/combat system; daily `AdvanceDay` work; renderer integration; save/load/replay; or migration of every legacy scenario.
