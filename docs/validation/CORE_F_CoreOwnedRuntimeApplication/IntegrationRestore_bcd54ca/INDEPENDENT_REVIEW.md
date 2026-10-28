# Independent exact-tree review: CORE-F integrated initial restore

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable findings remain for the reviewed integration tree. This is not CORE canonical promotion, complete CORE-F acceptance, or a claim that the public `DailyV1Application.TryStart` and detached host are complete.

## Exact identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed integration merge: `bcd54ca87c7741252ebc26978889a2a5464ae9c0`
- Exact source tree: `ed717d5a74c1ca75b1c8dccc1703d1a3d1b24051`
- Merge parents:
  - P8-D City/Location cardinality fix: `7292e2f4b3ce5d0a46af926932c5b332ec39cd8f`
  - Core application restore work: `a61b980d3e768ba3e47b152f54bddb6e31dd5e7e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate branch's docs-only successor is `45875a1d721418606769f64ad85db3593984fc0f); comparison confirms it adds only the validation record, normalized Unity package, and manifest. Its public manifest names the exact reviewed commit and tree.

## Review performed

The merge is a two-parent integration. Comparing the merge with its Core-application parent `a61b980d` shows exactly two changed files: the P12-D relation validator and its unit tests. No other source or test file differs from that parent. Thus the previously reviewed application/restore source is retained byte-for-byte at the Git-tree level; there is no additional conflict-resolution drift in the Core startup or P12-C/D/E/F integration path.

The P8 parent contributes the bounded P12-D validation increment:

- City relation rows must have the same cardinality as the City-to-Location map.
- Duplicate City IDs are rejected.
- Distinct top-level Cities cannot bind to the same Location.
- Tests reject shared-Location bindings, duplicate City rows, and a missing City row for a map entry.

This preserves the approved P8 invariant of one top-level owner per Location, while allowing Locations with no City owner. For the accepted P12-D graph this rejects malformed/incomplete identity relations; it does not add profile owners or broaden P12-D scope.

The merged source retains the previously reviewed invalid-initial-host correction: on an otherwise unused application, invalid initial host input keeps the `InvalidOwnerValue` result, marks the one-shot load attempt and blocks later genesis. A completed `NoSave` result remains the only outcome permitting genesis. The corresponding same-application regression is present.

The existing Core-owned staged restore interfaces for P12-C, P12-D, P12-E, and P12-F remain unchanged from the reviewed application parent. They retain their bootstrap/exact-session replacement authority checks and stage detached roots/owners through Core before the host's compatibility projection and publication boundary. Prior independent records in the parent history cover restored-source admission, restored owner-vector identity, and restored-session commit authority. This review verifies their exact source retention in the merge and does not claim new completion beyond those bounded reviewed contracts.

No test assertions were removed or weakened in the reviewed merge delta.

## Validation evidence

The integrated validation record reports:

- Official CoreValidation: 901/901 cold and 901/901 warm; run `d2d2dbe638ca43909df10c6c9c1b6924`.
- Lab build and cold/warm scenario: PASS; Library absent.
- Official full Unity EditMode: 3274/3274; stable input digest `4b27b7b16a32d83848862528758b9813927a2971b0b548664ea0f87de867e3e7`.
- Official Smoke: 5/5, same stable input digest.
- `git diff --check`: PASS.
- The normalized Unity package was reported as verified; archive SHA-256 `4a2752b219d336f1b9ae8d665e34fb11858a5101009fd5ae85c68b56100b7b57`.

I independently inspected the public validation record and package manifest. The manifest's source snapshot identifies commit `bcd54ca87c7741252ebc26978889a2a5464ae9c0` and tree `ed717d5a74c1ca75b1c8dccc1703d1a3d1b24051), and records the reported EditMode run. The protected CoreValidation manifest and raw reports, and the binary Unity archive itself, were not accessible through my review tools; therefore I did not independently rehash those artifacts or rerun validation. Their results are recorded as supplied evidence, not independently reverified here.

## Scope limit

The integrated tree advances initial restore admission and ordered P12-C/D/E/F restoration under the shared Core application. Unity bootstrap/restore host responsibilities remain. This does not deliver a complete public `TryStart`, detached .NET host, separate-process continuation, or full CORE-F acceptance. Core canonical remains unchanged.
