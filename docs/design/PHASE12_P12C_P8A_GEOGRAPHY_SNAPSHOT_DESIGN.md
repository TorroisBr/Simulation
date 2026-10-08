# P12-C P8-A Geography Snapshot and Staged Reconstruction Design

**Status:** Technical design proposal; ready for independent review before implementation.

**Checkpoint:** P12-C — Identity, genesis provenance, and deterministic roots. This is a bounded P12-C sub-slice under the accepted P12-C scope, not a new checkpoint or a P12-A integration.

**Evidence baseline:** P12 canonical `82125b8e20ca997226ede0069bc875472cf90430`; architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; P8 canonical `470667d37863384edadb3d93ef64d8004aff46a3`; P9 canonical `82396ae7ffaf407fda278928da456b06dc5394d4`. The selected profile is `UnityBootstrap-Daily-v1` backed by `Assets/_Project/Data/Simulations/Simulation-DailyV1.asset`. The P12 State and accepted capability decomposition are authoritative for checkpoint scope.

## Purpose and boundary

Add exact immutable export and private staged reconstruction for the selected profile's P8-A factual geography owned by `SpatialAuthorityStore`. The owner slice contains the profile's authored Hex, its terrain identity and authored revision, the anchored Location, coordinate convention/order, and authored world-scale context. It preserves exact typed IDs, values, owner revision, and relationship cardinality.

This design follows the P12-C dependency on promoted P12-B. The owner helper consumes a coherent source selected by the P12 coordinator; it does not issue or validate the ephemeral P12-B completed-boundary token. The enclosing coordinator remains responsible for healthy admission, the exact selected profile, owner-thread/quiescence, current census coverage, and the successful completed boundary before capture.

The design owns only P8-A geography facts currently held by `SpatialAuthorityStore`. P8-A section witnesses are inventory evidence and are not export/hydration. The selected-profile proving values currently come from the authored asset: one `hex/sample-origin` at axial `(0,0)`, terrain `terrain/sample-plains` revision `sample-world-v1`; one `location/sample-origin` anchored to that Hex; convention `axial-hex-v1`, canonical order `q-then-r`; scale `world-scale/Simulation-DailyV1/v1`, source `profile/Simulation-DailyV1`, source version `1`, distance `1 km` per neighbor step. These are current profile inputs, not a universal Hex or Location schema; the snapshot carries the actual exact values selected by its admitted profile revision.

## Owner and data contract

`SpatialAuthorityStore` remains the only authority. Add an internal P12-C snapshot/helper in a dedicated source file, provisionally `SpatialAuthorityContinuationSnapshot.cs`, without adding a second store or a public extension API.

Schema version 1 carries:

- the selected profile contract identity and owner snapshot schema version;
- source owner revision;
- coordinate convention version and canonical order;
- the complete ordered Hex list: stable `HexId`, axial `q/r`, `TerrainDefinitionId`, and authored terrain revision token;
- the complete ordered Location list: stable `LocationId` and its `AnchorHexId`;
- scale context: resolved convention identity, source identity/version, exact decimal distance per neighbor step, and unit.

DTO construction copies all lists and stores primitive/value data. No source collection or mutable owner object is retained. Records are ordered by the authority's existing stable ordering. Nulls, blank identities, invalid numeric values, and unrecognized schema/profile identity are rejected. Typed IDs are reconstructed from their exact recorded values; they are never reallocated or inferred.

For the current `UnityBootstrap-Daily-v1` section, capture requires exactly one geographic Hex, exactly one Location, a present valid scale context, and source owner revision `1`. This admits the single authored geography composition. A future supported P8 mutation that changes this owner revision requires a new reviewed schema/contract before a later revision may be accepted; the hydrator must not silently normalize the revision or claim to preserve it when the current owner API cannot set it.

## Exact capture rules

Capture validates `SpatialAuthorityStore.ValidateInvariants()` and reads the same owner instance selected by normal bootstrap composition. It rejects unless all of these hold:

- `Revision == 1`, `HexCount == 1`, `LocationCount == 1`, and `HasGeography` is true;
- each list read agrees with its corresponding count and contains no null, duplicate, or malformed record;
- the Location's exact anchor resolves to the sole recorded Hex;
- scale context and coordinate convention/order are present and valid;
- `Crossings` is empty and `LocalTopologyBindingCount == 0`;
- passage options, barriers, option states, and barrier states are empty;
- the owner invariant report is valid.

Rejecting the extra collections is deliberate: this profile admits the current P8-A root only. It does not silently omit newly composed P8-B/C/D/E or P10-owned truth. If current profile admission later intentionally includes such facts, that owner coverage requires its own accepted owner contract and cannot be treated as empty by this schema.

The capture helper returns either a complete immutable snapshot or a typed failure. It never returns a partial DTO and never mutates or freezes the source owner.

## Private staged reconstruction

The hydrator validates snapshot schema and profile identity, exact cardinality, owner revision, typed IDs, coordinate/terrain/anchor relationships, decimal scale, and deterministic ordering before exposing a result. It then creates a new private `SpatialAuthorityStore`, constructs one `SpatialGeographyDefinition` from copied values, and calls the existing atomic `TryComposeGeography` on that empty owner.

The staged owner must pass `ValidateInvariants()`, have revision equal to the snapshot's recorded revision, and compare equal across every exported fact. The equality comparison includes IDs, `q/r`, terrain identity/revision, Location anchor, convention/order, full scale context, cardinality, and all required-empty collections. Any exception, validation failure, revision mismatch, or semantic mismatch discards the private staged owner and returns failure with no owner result. No runtime or public authority reference is published by this helper; the future P12-G/coordinator performs one publication only after whole-graph validation.

The stage path does not rerun `SimulationGenesisPipeline`, resolve authored assets, generate IDs, republish the live runtime, or reconstruct provenance from present-day definitions. Genesis output and P9-B manifest/lineage are separate P12-C owner work and remain historical inputs to a later envelope.

## Validation and implementation files

Implementation should add the snapshot/helper in a new owner-specific script file and focused EditMode tests in a new test file, with required Unity `.meta` files. Avoid edits to `SimulationRuntime`, bootstrap composition, `TesteSimulacao`, and the shared geography composer unless review proves an owner-level API gap that cannot be handled through current reads and `TryComposeGeography`. Reuse the existing P12-B owner admission/census boundary; do not add new census registration or mutation-epoch wiring in this slice.

Required focused evidence:

1. Capture the selected Daily-v1 profile and assert the exact authored facts listed above, cardinality, owner identity supplied by the caller, and source revision.
2. Mutate/replace source lists or references after capture where a test fixture permits it and prove the snapshot remains an independent immutable copy.
3. Reconstruct into a fresh private store and compare every fact, owner revision, and invariant; prove the source owner is unchanged.
4. Reject wrong/unsupported schema and profile identity, wrong cardinality, missing/duplicate/blank IDs, malformed coordinates/terrain/anchor, invalid scale, wrong owner revision, invalid owner invariants, and every excluded nonempty crossing, passage, barrier, or local-topology binding fact.
5. For every rejection, assert no partial snapshot or staged owner escapes and the source owner remains unchanged.
6. Retain existing `SpatialGeographyTests` and selected Daily-v1 bootstrap geography regressions.

Then run the focused P12-C geography and bootstrap/spatial suites, ALL EditMode, official Smoke, SimulationRuntime LongRun, and `git diff --check` on the exact candidate tree. Record Unity version, exact code/tree SHAs, raw Git-blob SHA-256 source/test hashes, XML/log hashes, and commands/results. Obtain independent exact-tip implementation review against the same exact code tree before integration or promotion. Documentation-only review-tip changes must not be represented as code-tree changes.

## Dependencies and claims

P12-B is COMPLETE/PROMOTED only within its accepted bounded profile-admission and completed-boundary lifecycle contract. The already promoted identity/sequence snapshot is one partial P12-C slice. This design makes only the P8-A geography sub-slice implementation-ready after independent design review.

P12-C remains incomplete until its other accepted roots, including P9-B genesis provenance and continuation-relevant deterministic-random roots, have separately reviewed owner contracts and implementations. P12-D/E remain blocked on P12-C; P12-F waits on C/D/E; P12-G waits on B-F plus the live-profile inventory. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.

This owner design does not claim profile-wide census coverage, complete owner export/hydration, global graph validation, capture eligibility, save/load, P12-A readiness, P13 readiness, or Phase closure. It does not include City/Market/population/NPC truth, routes/passages, LocalTopology/Ruins, P10 facts, P8-B/C/D/E facts, legacy `SpatialNetworkRuntime` identities, Knowledge, activities, or gameplay state. Legacy runtime IDs remain distinct from P8 `HexId` and `LocationId`.
