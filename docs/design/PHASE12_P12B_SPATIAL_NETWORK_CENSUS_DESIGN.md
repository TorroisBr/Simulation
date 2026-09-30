# P12-B Legacy SpatialNetwork Census Design

**Status:** Bounded technical design submitted for independent review. No
implementation has started.

**Canonical design base:** `codex/phase12/canonical` at
`676196bcd807603deb9d01bd2855342a7d47a01e`.

**Relevant cumulative composition candidate:** code tip
`44fc3ab94c9666f656149f346fb2cc553d3cb689`, tree
`b0be75370d32679d0745ed15d29ce359dada0bb6`. Its change to
`SimulationBootstrapComposition.cs` is a shared hotspot. Any implementation
must use the then-current canonical base after that candidate is promoted or
explicitly re-integrated; it must not edit the composition concurrently.

## Bounded purpose

Add passive P12-B census evidence for the one legacy `SpatialNetworkRuntime`
installed in the accepted `UnityBootstrap-Daily-v1` profile. This is owner
inventory evidence for the existing runtime-ID Locations and Routes already
included in the P12-D factual-root scope. It does not add geography, route
semantics, P8 authority, save/export/hydration, or capture eligibility.

## Owner and sections

Publish two fixed schema-v1 owner sections through the normal bootstrap
composition, bound to the exact installed `SimulationBootstrapComposition.SpatialNetwork`
instance:

| Section ID | Cardinality | Revision |
|---|---|---|
| `p12d.legacy-spatial-network.locations` | Number of registered legacy runtime-ID Locations | Shared `SpatialNetworkRuntime` revision |
| `p12d.legacy-spatial-network.routes` | Number of registered legacy runtime-ID Routes | Shared `SpatialNetworkRuntime` revision |

Both witnesses use the same installed owner identity and revision because a
successful registration changes both the network and its `RuntimeIdentityRegistry`
identity index. Keep these sections distinct from registry typed-index
witnesses and from P8-A geography, P8-B passage/crossing, P8-C anchors and
positions, and P8-D route Knowledge/plans.

The selected authored profile is composed and populated: the required
day-zero evidence is two Locations, two Routes, and revision 4, reflecting
the four successful registrations from a new network's revision 0. This is a
positive-cardinality assertion, not an exact-zero case. Later supported
registration changes the counts and shared revision; do not treat authored
asset counts as a maximum.

## Mutation and exposure boundary

`RegisterLocation` and `RegisterRoute` remain the only network mutation
entrypoints. On each successful registration, increment the local revision
exactly once after both the registry identity and network indexes are
installed. Reject revision saturation before calling the registry so an
ordinary `false` result cannot leave registry-only identity state. Existing
registry identity conflicts and its own saturation already reject before
registry mutation. All ordinary `false` paths preserve network cardinality
and revision. No removal path exists.

The intended ordinary-operation commit boundary spans the full network
registration call, including its `RuntimeIdentityRegistry` write. A later
P12-B operation scope must notify its shared mutation epoch once after this
whole commit. This design adds no epoch wiring and makes no claim about
exception rollback for allocation failures.

Prevent supported callers from mutating the network behind its census:
`Locations` currently exposes the backing `HashSet` as `IEnumerable`, while
`Routes` and `GetOutgoingRoutes` expose backing `List` instances through
read-only interfaces. Replace these with non-mutable views or detached
snapshots while retaining existing public signatures, iteration behavior,
route adjacency, and immutable Location/Route objects. No caller may receive
a castable reference that can change installed membership without going
through `Register*`.

The owner and provider reads remain unsynchronized. The witnesses do not
establish a concurrent atomic snapshot, owner-thread affinity, or runtime
quiescence.

## Implementation ownership and validation

The bounded implementation owns:

- `Assets/_Project/Scripts/SpatialRuntime.cs` for the local revision and
  non-mutable collection views;
- a fixed `SpatialNetworkCensusProvider` under `Assets/_Project/Scripts/`;
- provider publication through `SimulationBootstrapComposition.cs`, only
  after the current composition candidate's hotspot is released;
- focused EditMode census tests and selected-profile assertions in
  `SimulationBootstrapCompositionTests.cs`.

Review existing read consumers in `AdventureAutonomy.cs`,
`AdventureExpeditionAutonomySystem.cs`, `TravelSystem.cs`,
`TesteSimulacao.cs`, `WorldObserverReadModel.cs`, and
`Diagnostics/WorldStateSnapshot.cs` before changing collection-view
behavior. The observer and diagnostic snapshot consumers copy and sort these
collections; retain that behavior and include their focused regressions.

Focused tests should establish exact installed-owner identity, stable section
IDs/schema, selected-profile 2/2/revision-4 values, one revision per
successful write, unchanged count/revision and registry state on rejection,
local-revision saturation preflight, protection from collection-view
mutation, preserved outgoing-route adjacency, and unchanged observer and
diagnostic snapshot output. Then run the affected spatial/bootstrap and
observer/diagnostics regression suites, ALL EditMode, complete official
Smoke, and `git diff --check` under the current promotion gate.

## Exclusions and readiness

Exclude shared-epoch wiring, coordinator capture tokens, owner-thread or
quiescence enforcement, edits to `SimulationRuntime`/`SimulationTime`/P18
lease behavior, P8 stores or topology, exports/hydration, and new
geography/route content or semantics. This design fits the already accepted
P12-B passive-census and P12-D existing-factual-owner scopes; independent
technical review is required. If review passes, this bounded implementation
may proceed after the composition hotspot is free. Canonical promotion still
requires separate explicit human approval.
