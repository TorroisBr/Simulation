# Independent Review — P12-C P9-B Genesis Manifest Snapshot Design

**Status: PASS (design review only).** The bounded P9-B manifest snapshot and direct staged reconstruction design satisfies the accepted P12-C owner-slice contract and is ready to proceed to its separately authorized implementation phase. This is not implementation validation or promotion.

## Review baseline

- Candidate branch: `codex/phase12/P12CP9GenesisProvenanceDesign`
- Exact candidate tip: `15d2d02f221c38a45ac7ae29b4466439f30e27bc`
- Candidate base / current P12 canonical: `82125b8e20ca997226ede0069bc875472cf90430`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- P9 canonical cited by the design: `82396ae7ffaf407fda278928da456b06dc5394d4`
- Accepted scope: `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, P12-C
- Reviewed artifact: `docs/design/PHASE12_P12C_P9B_GENESIS_MANIFEST_SNAPSHOT_DESIGN.md`
- Review branch: `codex/phase12/P12CP9GenesisProvenanceDesignReview`, based directly on the exact candidate tip.

The candidate descends directly from the stated P12 canonical base; the merge base is exactly `82125b8e20ca997226ede0069bc875472cf90430`. Its diff adds only the technical-design document. The candidate worktree is clean. No Unity tests were run; the artifact is documentation-only and defines implementation-time validation gates.

## Findings

No blocking design findings.

## Verified design points

- **Direct manifest preservation:** the snapshot field inventory covers every public `SimulationGenesisManifest` property: contract and schema identities, both fingerprints, effective configuration, canonical provenance, authored definition IDs, output owners, dependency records, calendar fields, seed/source, stage order, and first boundary. It separately copies every nested effective-configuration value into detached scalar values and preserves exact list order. Staging reconstructs those values directly through a narrow internal factory, without the config-derived manifest constructor, asset access, `CreateFingerprint`, stage resolution, or stage execution.
- **Calendar and effective configuration:** the design includes months/year, weeks/month, days/week, and exact ordered month lengths, plus all nine current effective-configuration components. Its finite-value, enum, range, and calendar validation constraints match the existing scalar value types and avoid normalization or re-resolution.
- **P12 versus P9 profile identity:** P12-B owns validation of the outer `UnityBootstrap-Daily-v1` admission profile. The P9 manifest independently binds `ContractIdentity` and `SelectedP9ContractIdentity` to `unity-authored-bootstrap/authored-geography-v1`, with manifest and selected-P9 schema versions both `2`. The snapshot schema is explicitly separate. The Daily-v1 P9-B-only contract excludes P10-A/P10-B topology identity and output facts.
- **Fingerprint verification:** source code confirms P9-B `CreateFingerprint` returns SHA-256 over the ordered canonical records using a four-byte length-prefixed UTF-8 framing. Existing selected Daily-v1 composition tests assert `Fingerprint == SelectedP9ProfileFingerprint`. The design requires a pure helper over the retained record sequence and checks that its value matches both stored fingerprints, so historical provenance is checked without re-running genesis or consulting current assets.
- **Staging and failure behavior:** the snapshot copies all source lists and scalar values; the restore factory builds fresh immutable value objects/read-only lists and returns either one complete manifest or failure. It does not create `SimulationBootstrapComposition` or publish runtime owners. The future P12 aggregate coordinator retains this manifest privately until whole-graph validation and the single publication boundary.
- **Rejection and validation plan:** required cases cover unsupported schema/profile, malformed or missing fingerprints/provenance, altered/missing/duplicated/reordered lineage, invalid effective configuration and calendar values, duplicate authored IDs, dependency/boundary incoherence, and no partial staged object. Positive cases compare every scalar/list field and nested value; tests also require no asset resolution, `CreateFingerprint`, or genesis-stage invocation. The specified focused, regression, full EditMode, Smoke, LongRun, and exact-tree diff-check gates remain implementation work.
- **Accepted exclusions:** this slice owns only the P9-B manifest value. P8-A geography, identity/sequence and RNG roots remain separate P12-C owners; generated outputs remain with their authorities. P12-C remains incomplete, and no P12-A/P13/Phase 12 readiness or closure is claimed.

The exact review evidence is limited to the design and current source/test contracts; implementation review and all Unity validation gates remain outstanding. No architecture or product decision is introduced by this PASS.
