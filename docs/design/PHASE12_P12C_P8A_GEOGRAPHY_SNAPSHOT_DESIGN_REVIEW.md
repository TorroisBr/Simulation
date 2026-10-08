# Independent Review — P12-C P8-A Geography Snapshot Design

**Status: NEEDS_CHANGES.** The owner boundary and reconstruction approach fit the accepted P12-C sub-slice. Clarify the profile identity carried and validated by the snapshot before implementation.

## Review baseline

- Candidate branch: `codex/phase12/P12CSpatialGeographySnapshotDesign`
- Exact candidate tip: `f60150aee051bcc6aff16c200cc184ac3018baa3`
- Candidate base / current P12 canonical: `82125b8e20ca997226ede0069bc875472cf90430`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Accepted scope: `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, P12-C
- Reviewed artifact: `docs/design/PHASE12_P12C_P8A_GEOGRAPHY_SNAPSHOT_DESIGN.md`
- Review branch: `codex/phase12/P12CSpatialGeographySnapshotDesignReview`, based directly on the exact candidate tip.

The candidate descends from the stated P12 canonical base; the merge base is `82125b8e20ca997226ede0069bc875472cf90430`. Its diff adds only the technical-design document. The active P20 checkout contains unrelated pre-existing local changes; review was performed from the candidate and canonical Git objects and did not touch that checkout. No Unity tests were run; this is a design review.

## Finding

### D1 — Identify the profile identity that the snapshot binds to

The design says the schema carries a “selected profile contract identity” and rejects an unrecognized “schema/profile identity” (design lines 23, 30, 52), but the current system has two distinct identities in this scope:

- P12 admission profile `SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1` (`Assets/_Project/Scripts/SimulationRuntime.cs`, enum and admission context).
- P9 genesis profile `SimulationGenesisPipeline.GeographyProfileContractIdentity`, `unity-authored-bootstrap/authored-geography-v1`, with selected P9 schema version 2 (`Assets/_Project/Scripts/SimulationGenesisPipeline.cs` and `SimulationGenesisManifest`).

The P12-C decomposition requires both the selected P9-B manifest/profile lineage and exact P8-A facts. The design also assigns P9 manifest/lineage to separate P12-C work. It therefore needs to say whether this P8 owner snapshot binds to the P12 Daily-v1 admission profile, to the P9-B identity/schema, or records both and leaves a specified cross-check to the coordinator. As written, capture and hydration have no unambiguous accepted identity value, and the wrong-profile rejection tests cannot be specified precisely.

Please name the exact existing identity source(s) and their validation boundary without adding a new profile semantic. The implementation test plan should cover mismatch for each identity that this snapshot actually carries; if P9 identity is coordinator-owned, state that the coordinator cross-checks the separately restored P9 manifest against the P12 profile.

## Verified design points

- **Profile values:** `Simulation-DailyV1.asset` contains `hex/sample-origin` at `(0,0)`, `terrain/sample-plains@sample-world-v1`, `location/sample-origin` anchored to that Hex, convention `axial-hex-v1` / `q-then-r`, and scale `world-scale/Simulation-DailyV1/v1`, source `profile/Simulation-DailyV1` version `1`, `1 km` per neighbor step. `SimulationBootstrapCompositionTests` asserts these exact values and P9-B schema/stage identity at lines 1479–1506.
- **Owner and revision:** `SpatialAuthorityStore` owns the Hex, Location, scale, convention, crossings, passage authority, and local-topology bindings. Its current read APIs expose the necessary values. `TryComposeGeography` validates and composes atomically into an empty store, cloning owner facts and advancing revision once; the selected profile’s P8-A census requires `1/1/1` at the same owner revision. Requiring revision `1` is consistent with the current one-time P9-B composition and fail-closed rejection of later P8 mutations.
- **Excluded facts:** rejecting crossings, passage records/states, and local-topology bindings prevents this P8-A snapshot from dropping P8-B or P10 facts. P8-C legacy anchors and legacy `SpatialNetworkRuntime` identities are separate owners and correctly remain outside this slice.
- **Staging/publication:** creating a fresh private `SpatialAuthorityStore`, using the existing atomic `TryComposeGeography`, validating every fact and revision, and returning no owner on failure preserves the private-stage boundary. Deferring the only publication to P12-G is consistent with the decomposition and architecture §92A.
- **Validation plan:** the proposed immutable-copy, exact round-trip, malformed/unsupported/rejected-extra, no-partial-result, and regression cases cover the owner slice. Existing geography and selected Daily-v1 composition tests provide factual baselines; they do not themselves provide P12 export/hydration evidence.

The design otherwise remains within P12-C’s accepted P8-A owner scope. It does not complete P12-C, establish profile-wide owner coverage, or imply P12-A readiness. After D1 is resolved, obtain a fresh independent design review before implementation.
