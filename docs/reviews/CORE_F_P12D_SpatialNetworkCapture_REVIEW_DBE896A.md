# CORE-F P12-D SpatialNetwork Owner Capture — Independent Exact-tip Review

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact candidate commit: dbe896a51937bea6c3e464033997c840dbd5cfc6
- Candidate tree: 1274b8f028f4e5ed6217868d7dba4362975e67d7
- Immediate/base candidate commit: 24b629c6fcdd6918d1dd1ca402a665b17e363559
- Base tree: e0f1222014ede8c6e7594ef614c1170c976e7c2e
- Spatial implementation/test commit: 075a8bb41df98b5612ac614dfd11c3875d07cbf0, tree 1c330d60c6644693df74ff35b490b213534cbdb9
- Final dbe896a change after the implementation commit: validation-prose-only edit to SpatialNetworkOwnerCapture_20261029/VALIDATION.md.
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Repository origin: https://github.com/TorroisBr/Simulation
- Direct remote-ref check at review matched the candidate and canonical SHAs above.
- The exact candidate was pinned in an isolated review worktree. Candidate and canonical refs were not modified.

## Diff and payload

The full base-to-tip delta is 13 paths: seven source/test files and six scoped validation files. The reviewed SpatialNetwork implementation adds one Core snapshot operation and binds its captured value to the existing P12-D save operation. The earlier P12-D Person/Genealogy portion is unchanged from the independently reviewed base candidate 24b629c6; that prior review is recorded at db227fe65c1ebaecb679db9cd7ef607dcc6ffee1. The entire cumulative diff was inspected.

No ProjectSettings changes, .meta files, private raw evidence, unrelated or untracked files are included in the commit range. No Unity GUID changed. The tested worktree had protected/untracked items, but the exact committed diff excludes them. git diff --check passes.

The SpatialNetwork implementation commit changes only:
- Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- Packages/com.simulation.core/Runtime/Geography/SpatialNetworkOwner.cs
- Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs
- Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/SpatialNetworkOwnerCapture_20261029/VALIDATION.md
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/SpatialNetworkOwnerCapture_20261029/coref-p12d-spatial-editmode-all-20261029.manifest.json
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/SpatialNetworkOwnerCapture_20261029/coref-p12d-spatial-editmode-all-20261029.zip

The later dbe896a commit modifies only the SpatialNetwork validation record. The seven implementation/test checkout-byte SHA-256 values were recomputed and match the validation table; their clean-filtered Git blob IDs match the exact candidate tree under core.autocrlf=true.

## Implementation review

No actionable implementation defect was found.

- The snapshot is produced by the existing authoritative Core SpatialNetworkOwner retained by the active session’s selected owner graph. The code requires the execution boundary to retain that same graph; no second spatial authority or Unity dependency is introduced.
- The snapshot copies the owner’s local revision, ordinal-sorted location IDs, and route IDs, origin/destination IDs and travel days from the authoritative route list. It returns detached immutable values. The existing Unity projection’s snapshot path reads the same selected owner. The P12 codec already sorts location IDs before encoding and preserves route-list order, so output ordering and schema/section format do not change.
- During the active ContinuationSave lease, capture revalidates the source witness before and after the Core snapshot. It requires exactly one required Locations and Routes token section of the expected schema, with matching cardinality and local revision, and a shared owner-instance identity. The source object is bound once to the exact operation and token/witness.
- At P12 image staging, the app-backed path requires the Core spatial source and verifies that its exact SpatialNetworkOwner is both the projection’s SelectedOwner and the owner graph’s SpatialNetwork. The package also validates both census sections against the exact projection provider identity. It consumes the already captured Core snapshot; it does not recapture the spatial owner in the app-backed path.
- Any failed city, demographic or spatial capture prevents publication of the save operation and disposes its ContinuationSave lease. The stale-witness regression verifies rejection while the lease is active and that explicit disposal balances the operation count. The new capture path is read-only; the existing P12 section encoding and restore order are unchanged.
- The no-operation legacy path remains unchanged: without a Core save operation, the Core-bound source is absent and P12DDailyV1OwnerPackage falls back to SpatialNetworkRuntime.CaptureOwnerSnapshot as before.
- Tests assert exact Core owner binding, projection binding, the shared Location/Route owner identity, snapshot cardinalities/revision, captured image section cardinalities/revision, and deterministic restored continuation. Existing full owner-projection comparison includes SpatialNetwork state. Test changes add assertions and current census setup; they do not delete or relax assertions.
- The Unity source only projects the selected owner, while the source snapshot is now taken from Core. There are no .meta changes or public API additions.

Nonblocking test note: the new targeted stale-witness test changes the captured section vector between lease admission and an explicit recapture. The initial and post-snapshot revalidation branches are both present in production; a future focused test could isolate a first spatial-source failure and assert no operation publication. Existing continuation-save admission tests cover failure cleanup for the shared operation path, so this is not a blocking finding.

## Exact-source validation evidence

The scoped record is docs/validation/CORE_F_CoreOwnedRuntimeApplication/SpatialNetworkOwnerCapture_20261029/VALIDATION.md. The validated implementation/test inputs are exactly:

| Input | SHA-256 |
| --- | --- |
| Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs | 2e0a260fad8858af75fbb9103c5d1a3b7ceb36218275a43ae96ddf82fb3bcb42 |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | fd43fcce79c28cc7a2acbe8c8d3ee7468d052e42d9d63c7f6f7a8fe2b0f50b32 |
| Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs | 3935d6df09fb4e1a99ee16f5db04c75b50b8193c200aa861801a104f08c49e31 |
| Packages/com.simulation.core/Runtime/Geography/SpatialNetworkOwner.cs | 2a9a8971575e8802eaeb29b9d4acf929f5c91d0aeb43a9158354bd97fbdc60ab |
| Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs | fe58a2b279e27fc7a313cda2ec8a0aba811c6607a459d114215b6d1a4dc2ebcf |
| Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs | 86e57ebf5625568559fab264cd823cb544e6b60c33f249b502b2155542ef1b8a |
| tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs | 2d865cbbbec645b16ebfd40c59b6c0dfa78051d896d2814dccd6d7fc8de02ef1 |

### CoreValidation

- Protected run ID: b78c4c14c74d4266a02d0bfae612a121
- Official CoreValidation cold and warm results: 935/935 passed each; failed/timeout/inconclusive counters were zero.
- Tested source commit/tree: 24b629c6fcdd6918d1dd1ca402a665b17e363559 / e0f1222014ede8c6e7594ef614c1170c976e7c2e, with a stable Core input digest of 788b79a79692460aeeaa1cbdba225147f6650b36d6afb7b264918cbfbf832f62.
- Protected native runner-manifest SHA-256: e99c1108190f1230494a3a41d6b99719e9ce93cb41081a051d50dc11ff7efb11.
- Independently inspected the private native manifest, both original TRX hashes and counts, the input inventory/digest, and the source hashes. The Core source inputs include the exact seven source/test checkout hashes above.
- The DotNetTrx adapter rejected the native simulation-core-validation/v1 manifest as an unsupported context schema; no synthetic context was invented. The complete native run evidence remains protected and accessible for this review. A normalized, permanently retained .NET result bundle remains a separate promotion-evidence requirement under the current evidence policy; this review does not claim that package exists.

### Unity

- Unity 6000.3.9f1; all final Unity runs have stable pre/post input digest 835eaf112d4a12b1996e5d04f966da86be610142f681dca7b59f005ece38323a.
- Focused P12 restore: 1/1 passed, run 8d613c1560bf40b7b699678f79dd201d; private manifest SHA-256 bc1df725fad1d1e3e2f6b595f3cb683d1eee027c6fe6f5926af742bd46386bce.
- Full EditMode: 3,279/3,279 passed, zero failed/skipped; run f3e2e881b1ff48509f04b05c57a953c9; private manifest SHA-256 c3512ab367c1128b980545c4730f15b654e349eb25efdafd0855faecd5e5cf7b; original XML SHA-256 cd8d52e2c0d8ead101ff91e17b166f317f2491948bbf9fee265a34c5eed67589.
- Official filtered Smoke: 5/5 passed, zero failed/skipped; run 64258787fbfa4a1294e7b5f07f390b6c; private manifest SHA-256 0fcca46265dc7abb6b11887abdf2ce9990be4930fc7c37d783f1af879e7b932c; original XML SHA-256 cebfd934634e2fa409d211dc2b8d9daaddb268d9b33e852dfb4b9e75fa2f67fa.
- The filtered Smoke result-only package is unavailable because the evidence packager rejects its stale parent-suite aggregate count. The full EditMode report and verified archive contain all five same Smoke identities with Passed outcomes, consistent with the accepted precedent recorded in CORE_STATE.
- Unity focused/full/Smoke protected run manifests and raw XML were inspected from protected storage. Their exact XML hashes match the private manifests and the validation record.
- Official package verification was rerun read-only against the protected original full EditMode run manifest: VALID, one run, one XML entry, archive SHA-256 b5381427ae69dc146210027387a571393ffe1107e6947eb5b28b4770b074dce5. Public package-manifest SHA-256 is 9daa162329194ef1ec673c3aef1261c7886b2cfbffac650eae7ede7deb4f40ad. The archive contains exactly 3,279 passed cases, zero failed/skipped, and the five Smoke cases. The public package and validation record contain no workstation-local absolute paths; private raw reports/manifests remain outside Git and unchanged.

No tests were rerun during this read-only review.

## Verdict boundary

This is a validated bounded P12-D SpatialNetwork capture-source candidate. It does not close P12-D or CORE-F, promote Core canonical, establish a complete .NET host, or satisfy all durable .NET evidence packaging requirements for a later canonical promotion. Resolve that separate evidence-packaging obligation before promotion.
