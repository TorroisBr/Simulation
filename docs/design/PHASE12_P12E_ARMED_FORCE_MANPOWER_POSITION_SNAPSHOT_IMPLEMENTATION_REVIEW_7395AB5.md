# P12-E ArmedForce / Manpower / Position Snapshot — Independent Implementation Review

**Result:** NEEDS_CHANGES — exact-tip source review found required test-coverage gaps. No source-level correctness defect was identified in the reviewed paths.

**Candidate branch:** `codex/phase12/P12EArmedForceManpowerPositionSnapshotImplementation`

**Exact candidate tip:** `7395ab58354bc859b33469c5af3f0b17a82e840b` (whole-repository tree `2ed5e60c0e45de029e4e8b7b221642f3ef71c7e5`)

**Reviewed implementation code commit:** `31c5fc19fda1c8ed0ec51d0e94d80ea5bde27f0d`

**Reviewed code tree:** `fa1ee31fa9c4469c1ee4916c9d5425ee2f891828`; exact `Assets` subtree `96a74b341f5c2ae07f53d7a16682a7d3e183f033`.

**Canonical base:** `codex/phase12/canonical` at `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`.

**Accepted design:** `codex/phase12/P12EArmedForceManpowerPositionSnapshotDesign` at `3ba6516bf6bcf081445f9c2c7fea192423487851`; exact design review PASS at `903bd26fc31f015f1e0d89fdf436ec8d240ab5df`.

## Exact-tip and scope verification

Immediately before review recording, `origin` was confirmed as `https://github.com/TorroisBr/Simulation`; refreshed remote refs showed the candidate tip and P12 canonical base above. The candidate is a clean fast-forward from the base (six commits; merge-base is the base). The final candidate commit `7395ab5` adds validation binding only: the implementation code tree remains the code commit's exact tree, and the validation archive SHA-256 is `73236A9CA2DA8890A8F6C6846B71EC9011B5BE176DFE8794E983A4FB590EDB11`, matching the manifest. The manifest source blob IDs match the implementation tree; all 26 archived XML/log artifacts match their recorded SHA-256 values. `git diff --check` passes for base-to-candidate.

The code diff is limited to the three military owner authorities, the detached P12-E snapshot DTO/stager, its focused tests, and validation evidence. It does not change `SimulationRuntime`, bootstrap composition, census registration, token/epoch/quiescence behavior, P12-D/P12-G, or P12-B semantics. P16/P17 state remains excluded from this selected Daily-v1 slice.

## Review findings

The implementation's reviewed paths appear consistent with the accepted boundary: detached value DTOs, exact five owner-stamp matching, dependency-ordered private construction, exact local-revision restoration, source-provider rejection, typed spatial-reference resolution, and P10 topology rejection. The `TryStage` outputs remain unset until all three private owner candidates validate. The current review result is nevertheless **NEEDS_CHANGES** because the focused suite does not prove several tests explicitly required by design §6.

1. **Required malformed-document rejection matrix is not covered.** `StageRejectsSchemaCardinalityTopologyAndSourceBoundRowsBeforeReturningOwners` (`P12EMilitaryOwnerSnapshotTests`, around line 230) covers schema, one count mismatch, injected topology, a source-bound row, and a topology-bound reference. It does not exercise missing/duplicate rows or IDs, invalid enums/ranges, malformed force hierarchy/lifecycle relations, missing/duplicate manpower-state coverage, invalid cohort totals/order/custody, `Amount`/living-roster mirror mismatch, or unresolved/wrong-kind position references. These cases are expressly required by design §6 (lines 327–330); source validators are present, but their fail-closed behavior is not established by this candidate's snapshot/staging tests.

2. **Populated round-trip and detachment assertions do not prove every retained field.** `PopulatedSnapshot_RoundTripsOwnerFieldsTypedReferencesAndLocalRevisions` (around line 55) asserts representative values and cohort amounts, but does not assert all force fields (including detached state and termination day), the complete manpower cohort tuple/order (injury, custody, custodian, availability, amount), all null/stable-reference cases, and all local/state revisions. Its post-capture mutation changes only the manpower cohort; collection immutability is checked only for the force list and a characteristic list. The terminated-force test verifies lifecycle and position, but not the full terminated record round-trip. Design §6 (lines 301–305) requires full field/null/reference/order/revision preservation and deep detachment after source writes.

3. **Failure-at-owner-boundary invariance is not demonstrated.** The staged-snapshot negative cases fail during document/dependency prevalidation; none demonstrates rejection from each private owner factory boundary after an earlier private candidate has been built, with no returned candidate and no observable change to supplied/live state. This leaves the explicit design §6 requirement (lines 329–332) without direct test evidence. Add focused tests for the force, manpower, and spatial candidate failure boundaries, asserting every output stays null and all live/supplied owner values, bindings, revisions, and guards remain unchanged.

The current suite does verify capture rejection for a non-null `SourceProvider`, injected `LocalTopology`, a P16 profile, P17 provenance, stale token-vector identity, and a missing required token section. These checks are retained and should continue passing.

## Validation evidence inspected

The exact-tree manifest `docs/validation/P12EArmedForceManpowerPosition/VALIDATION.md` records the focused military snapshot suite 7/7, the applicable force/manpower/spatial and Battle/P16/P17 suites, ALL EditMode 2576/2576, official Smoke 5/5, and `git diff --check` PASS. Artifact hashes were checked against the committed archive. I did not rerun Unity; this review found no concrete execution issue requiring a rerun. Existing passing validation does not close the test-coverage findings above.

## Scope limits retained

This review concerns only the bounded ArmedForce/manpower/baseline-position detached snapshot and private staging slice. It does not establish complete P12-E owner coverage, P12-D composition, runtime/bootstrap integration, P12-G publication, P12-A readiness, P13 readiness, or Phase 12 closure. No candidate source or validation file was changed during review.
