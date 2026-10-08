# P12-C P9-B Genesis Manifest Snapshot and Staged Reconstruction Design

**Status:** Technical design proposal for independent review; no implementation is authorized by this document alone.

**Checkpoint:** P12-C — Identity, genesis provenance, and deterministic roots. This is one bounded owner slice under accepted P12-C scope; it does not create a new numbered checkpoint or implement P12-A.

**Evidence baseline:** P12 canonical `82125b8e20ca997226ede0069bc875472cf90430`; architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; P9 canonical `82396ae7ffaf407fda278928da456b06dc5394d4`. Selected input profile: `UnityBootstrap-Daily-v1` using the dedicated `Simulation-DailyV1.asset`, which selects the P9-B authored-geography profile and excludes P10 Ruin/LocalTopology. The accepted P12-C decomposition and current P12 State define the boundary.

## Objective and boundary

Capture and privately reconstruct the exact immutable P9-B `SimulationGenesisManifest` already produced by the successful ordered genesis pipeline. Preserve historical profile/schema identity, both selected-P9 and full-profile fingerprints, authored provenance records, authored definition IDs, owner outputs, dependency graph, stage order, seed source/value, effective configuration, calendar facts, and first simulated boundary.

Hydration restores the recorded manifest value. It never reads current Unity assets, calls `CreateFingerprint`, calls `ExecuteStages`/`ResolveStageOrder`, reruns genesis, regenerates outputs, or allocates identities. A changed asset or newer genesis implementation must not rewrite historical provenance.

This slice owns the immutable manifest value exposed as `SimulationBootstrapComposition.Manifest`. P9-B generated City/Actor/spatial outputs remain owned by their existing authorities and are covered by their separate P12 owner sections. `WorldId`, runtime identity counters, record sequence, P8-A geography, and the deterministic-random root have separate P12-C slices. The P12-B coordinator remains responsible for admitting the exact profile and selecting a healthy completed boundary before capture.

## Current owner evidence

`SimulationGenesisManifest` in `Assets/_Project/Scripts/SimulationGenesisPipeline.cs` exposes the required read-only fields. Its existing internal constructor derives and orders manifest values from mutable Unity config/assets and recomputes stage/output metadata; calling it during hydration would rerun genesis semantics and is therefore prohibited.

`EffectiveSimulationConfiguration` and its nested population, economy, travel, crime, guard-crime, merchant-trade, commercial-knowledge, natural-mortality, and aggregate-demography values are immutable scalar objects. The selected calendar is represented in the manifest by months/year, weeks/month, days/week, and exact custom month lengths. The manifest's ordered lists are already the canonical retained outputs and must be copied without sorting or regeneration.

For the selected `Simulation-DailyV1.asset`, the profile contract is `unity-authored-bootstrap/authored-geography-v1`, selected P9 schema version is `2`, and P10 Ruin/LocalTopology and P10-B generated topology are absent. The manifest contains the P9-A inherited stage identities and the P9-B authored-geography stage `p9.genesis.authored-geography/v1`, plus the current selected stage order/dependency records and first boundary `advance-day:1`. Exact profile fingerprints, authored inputs, and output facts are copied from the live manifest; they are not replaced with these design-time fixture facts.

## Snapshot value contract

Add a detached immutable `P12CP9GenesisManifestSnapshot`, provisionally in `P9GenesisManifestContinuationSnapshot.cs`, with schema identity/version and scalar/list fields corresponding exactly to every public `SimulationGenesisManifest` property:

- `ContractIdentity`, manifest `SchemaVersion`, `Fingerprint`, selected P9 contract identity/schema/fingerprint;
- a detached scalar DTO for every field in `EffectiveSimulationConfiguration`;
- exact ordered `CanonicalProvenanceRecords`, `AuthoredDefinitionIds`, `OutputOwners`, `StageDependencyRecords`, and `StageOrder`;
- `CalendarMonthsPerYear`, `CalendarWeeksPerMonth`, `CalendarDaysPerWeek`, and exact ordered `MonthLengths`;
- `Seed`, `SeedSource`, and `FirstSimulatedBoundary`.

The DTO takes defensive copies and exposes immutable/read-only values. Every source list is copied in its existing order. The snapshot schema version is independent of the P9 profile schema version recorded inside it.

Capture receives the already-validated outer profile identity from the P12-B coordinator and requires `UnityBootstrap-Daily-v1`. Separately, the manifest must have `ContractIdentity` and `SelectedP9ContractIdentity` equal to `SimulationGenesisPipeline.GeographyProfileContractIdentity`, manifest schema `2`, and selected P9 schema `2`. Require the authored-geography stage exactly once, the current P9-A inherited stage identities and P9-B stage/dependency shape, no P10 topology stage/output facts, and first boundary `advance-day:1`. The helper does not mint or validate a P12-B admission/completed-boundary token. It rejects null/malformed effective config, invalid enum values, non-finite floating-point values, invalid calendar shape/month lengths, blank list entries, duplicate authored IDs or stage identities, and incoherent stage dependencies. Capture copies all values and performs no mutation.

This check is for the selected profile contract. Do not require the manifest to equal today's live asset output. Profile/content compatibility and the outer envelope's build/content fingerprint remain separate admission rules. Preserve exact floats/doubles and strings as represented by the immutable owner; do not round, locale-format, sort, or normalize them in this owner DTO.

## Private staged reconstruction

Implement a narrow internal reconstruction constructor/factory on `SimulationGenesisManifest` (or a same-assembly manifest factory) that accepts only a validated snapshot and assigns recorded values directly. It must not call the existing config-derived constructor or any genesis pipeline. Construct fresh effective-configuration value objects, a fresh read-only copy of every manifest list, and a fresh instance with exact recorded scalar values.

The stage factory validates the snapshot schema/profile, all required fields, enum/range/calendar constraints, exact list ordering/uniqueness constraints, P9-B stage and dependency evidence, and manifest internal coherence. It returns either one complete private manifest instance or failure; no partially filled manifest escapes. The staged object must compare equal to the snapshot field-by-field, including nested effective configuration, exact list contents/order, fingerprints, schema values, seed provenance, and calendar. Any failure returns no manifest and has no effect on a live bootstrap/runtime.

Do not create `SimulationBootstrapComposition` here: its constructor binds manifest to live authorities and runtime owners. The future P12 aggregate coordinator retains the staged manifest privately until whole-graph validation and the single publication point. Do not create a new manifest owner, alternate genesis service, or asset re-resolution path.

The P9-B `Fingerprint` is the SHA-256 over the retained canonical provenance records using the pipeline's existing length-prefixed UTF-8 record framing. Add or expose a pure helper that hashes only the supplied recorded records; during capture and staging, require the resulting fingerprint to equal both `Fingerprint` and `SelectedP9ProfileFingerprint` for this P9-B-only profile. This verifies retained record coherence without loading current assets, generating profile facts, or executing genesis. The future P12 envelope still owns encoded snapshot integrity.

## Implementation ownership and validation plan

Implementation is expected to add the snapshot DTO/helper and focused EditMode tests, plus a minimal direct-value restore factory in `SimulationGenesisPipeline.cs`. Avoid changes to genesis stage execution, `TesteSimulacao`, `SimulationBootstrapComposition`, `SimulationRuntime`, or P9 assets. `SimulationGenesisPipeline.cs` is a shared bootstrap hotspot: the restore factory must be a side-effect-free constructor path and must not alter the normal genesis path. The P8-A geography and RNG-root owner implementations can proceed independently; integration of all P12-C owner snapshots remains coordinated.

Focused coverage must include:

1. Capture the selected Daily-v1 live manifest and assert every scalar and list field is preserved exactly.
2. Mutate or replace source list inputs where possible after capture; prove detached snapshots and staged values do not change.
3. Stage a manifest from a snapshot without asset access or genesis invocation; compare every field and nested effective-config value with the original.
4. Preserve all P9-A inherited stages plus exactly the selected P9-B geography stage and dependency records, and prove no P10 stage/output facts are introduced.
5. Reject null/unsupported snapshot schema, wrong profile/contract/schema, missing or blank fingerprints/provenance, altered/missing/duplicate/reordered lineage, invalid effective-config enums/non-finite numeric values, malformed calendar/custom month lengths, duplicate IDs, and incoherent first-boundary or dependency records.
6. For every rejection, assert no staged manifest escapes and the original manifest remains unchanged.
7. Retain P9 genesis provenance/fingerprint/bootstrap tests; prove the restore path does not call asset resolution, `CreateFingerprint`, or stage execution.

After implementation on a fresh branch from then-current P12 canonical, run focused manifest/provenance and bootstrap suites, ALL EditMode, official Smoke, applicable SimulationRuntime LongRun validation, and `git diff --check` on the exact candidate tree. Retain source/test raw Git-blob hashes and validation XML/log hashes. Obtain independent exact-tip implementation review before integration or promotion. The design phase runs no tests.

## Dependencies and limits

P12-B is complete within its accepted contract; the identity/sequence snapshot is already promoted as partial P12-C work. This document scopes only the P9-B manifest owner. P8-A geography and deterministic-random roots remain separate P12-C owners. P12-C remains incomplete until all accepted roots are implemented and integrated. P12-D/E continue to wait on C; P12-F waits on C/D/E; P12-G waits on B-F and the validated live profile inventory. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.

No save format, complete P12 envelope, profile-wide export/hydration, profile-wide census, P12-A readiness, P13 readiness, or Phase closure is claimed. No P10 content, P14 source state, P18 temporal state, P20 activity state, P19 module state, arbitrary asset migration, generated-world expansion, cross-version compatibility, or gameplay semantics are included.
