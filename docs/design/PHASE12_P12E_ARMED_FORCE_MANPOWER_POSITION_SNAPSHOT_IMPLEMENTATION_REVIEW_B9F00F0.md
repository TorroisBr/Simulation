# P12-E ArmedForce / Manpower / Position Snapshot — Exact-Tip Implementation Review

**Result: `VALIDATED_CANDIDATE` — PASS.** The revised implementation and test-only follow-up satisfy the accepted bounded P12-E owner snapshot/private-stage contract. No production correctness defect or remaining required test gap was found in this slice. This review does not promote the candidate.

## Exact refs and evidence

- Candidate branch: `codex/phase12/P12EArmedForceManpowerPositionSnapshotImplementation`
- Exact remote candidate tip: `b9f00f0b14f010931e5e37ca0b512e4ea1f43932` (repository tree `9f0ef4ae3bc9d19e7468310c9b5a91c484f6c451`)
- Reviewed code/test commit: `a6ecf55aa8f3aca6d51a1a3d332881383eac5b4a` (repository tree `fa62a40c9698235f0c6bae1acbe0bc45ba1cf141`; `Assets` tree `09ba6fe9c74c2f19f84b318944ce3e0bcc8119b6`)
- Canonical base, refreshed before review: `codex/phase12/canonical` at `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- Accepted technical design: `3ba6516bf6bcf081445f9c2c7fea192423487851`
- Independent design review PASS: `903bd26fc31f015f1e0d89fdf436ec8d240ab5df`
- Prior implementation review NEEDS_CHANGES: `e4ff2a2610cd72e6a27fd55789172b8beb220567`
- Required validation manifest: `docs/validation/P12EArmedForceManpowerPosition/VALIDATION.md`
- Follow-up evidence archive: `P12E-review-gap-followup-20261008.zip`, SHA-256 `AC3AF3F4F852880725B01E45225FCE95BD0446279BE227ECBB0134058EEF08C3`
- Follow-up test file blob: `3f88e6da4fddbc34fd3de12f760f8d78a79864d5`; independently calculated file SHA-256 `2CC12AD504566FF77C5FF7C1114A8689DDEC0514811000406B4BB9DFEED84748`.

The candidate is a clean fast-forward from the named canonical base. The exact current remote candidate was rechecked after fetching; `b9f00f0` has parent `a6ecf55`. The final commit adds only validation binding. The implementation/test tree and `Assets` subtree match the values above. The test-only follow-up from prior candidate `7395ab58354bc859b33469c5af3f0b17a82e840b` changes only `P12EMilitaryOwnerSnapshotTests.cs`; all production owner/snapshot source is unchanged from the code already reviewed at that tip. The original code review found no source-level defect.

## Scope and source review

The slice is limited to detached export and private staged reconstruction for the existing ArmedForce, contingent manpower, and baseline armed-force position owners. The candidate does not change P12-B mutation/admission/quiescence/token behavior, `SimulationRuntime`, bootstrap composition, census registration, P12-D composition, P12-G publication, P16/P17 implementation, or unrelated user files. P16 and P17 state remains excluded by explicit fail-closed capture tests; no P12-B completion, capture eligibility, P12-A readiness, P13 readiness, export/hydration composition, or Phase 12 closure is claimed.

The exact-token owner-vector binding and owner-provider preconditions remain covered. Capture rejects non-null source providers, composed LocalTopology, selected P16 extensions, P17 provenance, stale vector identity, and missing required sections. Staging validates schema, counts, identities, force hierarchy, contingent/state one-to-one coverage, amount mirrors, source-null policy, cohort tuple/order/custody/availability rules, typed position resolution, and the exact required census stamp before publishing any private owner.

## Prior review findings resolved

The follow-up adds direct evidence for all three findings in `e4ff2a2`:

1. **Complete populated round-trip and detachment.** `PopulatedSnapshot_RoundTripsOwnerFieldsTypedReferencesAndLocalRevisions` checks retained force, contingent, Person-reference, manpower cohort, typed position, and exact revision fields, including nullable values and order. It mutates source values after capture and verifies the detached snapshot does not change; public collections reject writes. The terminated-force case checks the full retained termination record.
2. **Malformed snapshot matrix.** `StageRejectsMalformedHierarchyDuplicateOrMissingOwnerRowsAndInvalidPositions` and `StageRejectsMalformedCohortsSourceCustodyMirrorAndSpatialRows` cover duplicate/missing rows and identities, hierarchy/cycle/lifecycle errors, count mismatch, source/custody/mirror failures, duplicate and unsorted cohort tuples, invalid enums, invalid typed references, zero/negative/overflow amounts, and negative revisions. In particular, test assertions cover negative contingent amount and negative manpower cohort amount, negative ArmedForce/manpower/position owner revisions, and negative per-state revision; the `long.MaxValue + 1` cohort total fails closed.
3. **Failure at each private owner boundary.** `PrivateOwnerFactoriesFailClosedWithoutChangingTheirInputsOrLiveOwners` exercises force, manpower, and spatial factory rejection, requires null candidate outputs, and verifies supplied/live owner values, bindings, revisions, and guards remain unchanged.

The resulting tests preserve the contract's exact-zero and populated behavior while proving failures do not leak partially staged state.

## Validation evidence

The manifest binds the successful rerun to code/test commit `a6ecf55` and test blob `3f88e6d`. The archive's SHA-256 matches the manifest, and all eight successful XML/log pairs' recorded hashes match their archive contents. The Smoke log includes the `-testFilter Smoke` invocation and its XML reports five passed tests. Results:

| Gate | Result |
|---|---:|
| `P12EMilitaryOwnerSnapshotTests` | 10/10 PASS |
| `ArmedForceFoundationTests` | 10/10 PASS |
| `ArmedForceSpatialPositionTests` | 12/12 PASS |
| `ArmedForceStoreCensusTests` | 1/1 PASS |
| `P16AMilitaryMovementTests` | 21/21 PASS |
| `P17ARuntimeTests` | 10/10 PASS |
| ALL EditMode | 2579/2579 PASS |
| Official Smoke | 5/5 PASS |
| `git diff --check` from canonical base through candidate | PASS |

No Unity rerun was needed: the source/code tree did not change after its retained successful validation, and the new validation files and their artifact hashes were verified directly.

## Disposition and limitations

This exact-tip review records `VALIDATED_CANDIDATE` for this bounded P12-E slice only. It does not itself promote canonical code. Preserve the review and validation evidence if the candidate is later recomposed; any changed code tree requires the normal affected-validation and fresh exact-tip review. Broader P12-E owner coverage and all runtime/bootstrap composition remain separate work.
