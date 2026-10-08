# Independent Re-review — P12-C P8-A Geography Snapshot Design

**Status: PASS.** The corrected design resolves D1 and is ready for implementation as the bounded P12-C P8-A owner sub-slice, subject to the separately accepted implementation and validation gates. This is a design review, not code validation or promotion.

## Review baseline

- Candidate branch: `codex/phase12/P12CSpatialGeographySnapshotDesign`
- Exact corrected candidate tip: `3aff3ffb4136555276020527b843e40e2b3c2ceb`
- Initial reviewed candidate tip: `f60150aee051bcc6aff16c200cc184ac3018baa3`
- Candidate base / current P12 canonical: `82125b8e20ca997226ede0069bc875472cf90430`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Accepted scope: `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, P12-C
- Reviewed artifact: `docs/design/PHASE12_P12C_P8A_GEOGRAPHY_SNAPSHOT_DESIGN.md`
- Prior review: `docs/design/PHASE12_P12C_P8A_GEOGRAPHY_SNAPSHOT_DESIGN_REVIEW.md`, status NEEDS_CHANGES at exact tip `f60150aee051bcc6aff16c200cc184ac3018baa3`, review commit `5ab88403028b2e36083ac7bf4d6262f85c35b0ec`.
- This record is on separate review branch `codex/phase12/P12CSpatialGeographySnapshotDesignReviewR2`, based on corrected candidate tip `3aff3ffb4136555276020527b843e40e2b3c2ceb`.

The corrected candidate descends from the stated P12 canonical base; its merge base is exactly `82125b8e20ca997226ede0069bc875472cf90430`. The correction from the initially reviewed tip changes only the technical-design document. The candidate diff against its base adds only that design document. The active P20 checkout has unrelated pre-existing local changes; this review used Git objects and a separate review worktree and did not modify those changes or candidate files. No Unity tests were run because the candidate is documentation-only.

## Re-review of D1 — resolved

The corrected DTO contract now names three separate version/identity domains:

1. `P12AdmissionProfile` is the existing `SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1`, supplied and validated by the P12 coordinator.
2. `P9ProfileContractIdentity` and `P9ProfileSchemaVersion` are separately taken from the selected P9-B manifest: `SimulationGenesisPipeline.GeographyProfileContractIdentity` (`unity-authored-bootstrap/authored-geography-v1`) and schema `2`.
3. The P8 owner snapshot schema version is `1`.

The design states that the P12 coordinator validates the admission profile, the P8 snapshot records both profile domains, and later aggregate composition compares the saved P9 identity/schema with the separately staged P9 manifest. The hydration contract validates the P12 enum, P9 identity/schema, and P8 snapshot schema independently. Required focused evidence now explicitly rejects a wrong P12 admission enum, P9 identity, P9 profile schema, or P8 snapshot schema independently. This resolves the previous ambiguity without equating P12 runtime admission with P9 genesis identity or inventing a new profile semantic.

The source contract corroborates these names and values: `SimulationRuntimeAdmissionProfile` defines `UnityBootstrapDailyV1`; `SimulationGenesisPipeline.GeographyProfileContractIdentity` is the stated literal; and `SimulationGenesisManifest` exposes `SelectedP9ContractIdentity` and `SelectedP9SchemaVersion`, with geography schema `2`.

## Verified design points

- **Selected profile facts:** the canonical `Simulation-DailyV1.asset` and `SimulationBootstrapCompositionTests` identify `hex/sample-origin` at `(0,0)`, `terrain/sample-plains@sample-world-v1`, `location/sample-origin` anchored to that Hex, `axial-hex-v1` / `q-then-r`, and `world-scale/Simulation-DailyV1/v1` from `profile/Simulation-DailyV1` version `1`, with `1 km` per neighbor step. The design carries exact selected values rather than generalizing them into a universal P8 schema.
- **Owner and revision:** `SpatialAuthorityStore` remains the authority. Its existing reads expose the design’s facts and excluded child collections. `TryComposeGeography` requires an empty owner, validates all staged inputs before mutation, clones records/context on commit, and advances revision once; a fresh staged owner therefore has revision `1`. Requiring current source revision `1` is explicit and fail-closed, and hydration compares the reconstructed revision and every exported fact instead of normalizing revision.
- **Excluded facts:** rejecting crossings, passage options/barriers/states, and local-topology bindings prevents this P8-A contract from dropping composed P8-B/P10 facts. P8-C legacy anchors and legacy `SpatialNetworkRuntime` identities remain separate authorities. The design does not claim complete P8 or profile-wide owner coverage.
- **Staging and publication:** capture returns only a complete immutable copy and does not mutate/freeze the source. Hydration constructs a fresh private store with the existing atomic `TryComposeGeography`; any validation, invariant, revision, or semantic mismatch returns no staged owner. No public/runtime authority is exposed, and final publication remains with P12-G after whole-graph validation.
- **Validation coverage:** focused tests cover immutable-copy behavior, exact round-trip, source preservation, each profile/schema mismatch independently, malformed or duplicated facts, cardinality/revision/invariant failures, excluded nonempty child facts, and absence of partial results. Existing spatial geography and Daily-v1 bootstrap regressions remain specified. This is a suitable design-level test plan for this owner slice.
- **Scope and dependencies:** the slice is within accepted P12-C coverage for exact selected P8-A facts and depends on promoted P12-B. P9-B manifest/provenance remains separate P12-C work whose restored values are cross-checked later. P12-C remains incomplete; P12-D/E/F/G, P12-A, P13, and Phase 12 readiness are not advanced by this design.

No remaining design ambiguity or scope blocker was found. Implementation review and all exact-tree validation gates remain outstanding; this PASS authorizes none of them by itself.
