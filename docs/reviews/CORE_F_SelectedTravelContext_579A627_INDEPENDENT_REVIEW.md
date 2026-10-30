# Independent exact-tip review — CORE-F selected travel context

**Verdict: NEEDS_CHANGES**

## Identity and scope

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate tip: `3d1d5f660467c98c4622d03a9659685c59530eea`
- Implementation commit: `579a627ea399f93a79aaec07f61e1399d73bdaa6`
- Implementation tree: `c3a2e5d3cc7643af084c47a0df161d62c12b2d7a`
- Exact base: `cc7c1d721edcef543d96afbc7319f1a1f9d46efa`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review checkout: isolated clean worktree at the candidate tip; no candidate files were edited.
- Reviewed implementation diff: six production/test files. Candidate evidence adds `VALIDATION.md`, the normalized EditMode manifest and result-only ZIP.

This is a bounded context-binding change, not completion of CORE-F. It makes Core create the admitted travel execution context and check its exact session/owner-graph binding while retaining Unity projection, P12 travel-party protocol and result publication ports. The legacy non-admitted path remains available. No P12 closure or full application/host readiness is claimed.

## Required finding

`IDailyV1SelectedTravelDayHostPorts` exposes `AdvanceSoloTravel`, and the Core context forwards it as `AdvanceSoloTravel`. Unity supplies the value from `travelSystem != null`. `DailyV1SelectedDailyExecutionStage.ShouldAdvanceSelectedSoloTravel` uses the host value when there is no sealed runtime-protocol operation inventory; `HasAdmittedSelectedDailyExecution` does not itself require that inventory. Thus this is an execution-availability input, not only a projection, P12, or result-notification port. The validation scope statement and interface documentation that say the host supplies “only” those three port classes are incomplete.

The candidate preserves the earlier behavior, and this review does not request a semantic redesign. Correct the scope description and close the admitted-path authority gap before treating the context as fully Core-selected: admitted execution must derive solo-travel availability from a sealed Core operation inventory (or fail closed when that required inventory is absent); retain the Unity availability signal only for the explicitly non-admitted compatibility path. Add a focused regression for an admitted state without a sealed inventory if that state can be constructed through supported internal setup. The final `DailyV1Application` still must not let Unity choose authoritative action availability.

This is the sole actionable finding. No unresolved product or architecture decision is required to state the limitation; the approved Core design already assigns execution behavior and composition to Core.

## Review findings that passed

- The new Core context factory rejects null dependencies, unadmitted state, unbound execution roots, and any graph whose reference identity differs from `SelectedRuntimeOwnerGraph`.
- Calls that execute within the party owner operation, publish results, or observe results recheck the admitted state and exact graph binding. The host cannot substitute another graph through this context.
- Admitted execution obtains host ports and constructs the Core-bound context; the existing non-admitted/legacy context-construction and operation path is retained.
- Travel ordering remains TravelParty advancement followed by solo travel. Core executes the selected owner transitions and result/event writes; Unity ports remain for projection, the existing P12 party protocol and notifications. The P12 wrapper/operation semantics are not broadened here.
- Added tests cover exact graph binding/rejection and the selected context path. No existing assertions were removed or weakened in the six-file implementation diff.
- The full diff contains no `ProjectSettings` changes. The candidate does not include the unrelated user-owned ProjectSettings modifications or active worktree-only files.

## Validation and evidence applicability

The review did not rerun tests: the candidate records an unchanged executable tree across the official runs, and the exact changed production/test file identities match the validation-runner source identities.

- Official CoreValidation run `c8deb67b5a8543a39808865df9d690a6`: cold 958/958 and warm 958/958. Input digest `f25700930cab3dfaa1a56c3efc422541752905912f1fe4c3af7dac504c41afc9`; native manifest SHA-256 `bad9fba45a55eed1d25e88d36116bed58335174f27a7bcfb96aab7bbfa8be2bd`. Original TRX SHA-256 values: `bcfe723cd84942a76e034c63ea166533a8b0df98403e5154df490336f9cb356f` and `8d6aef17c17ed7630bc7f8b9f2dfc6a93ec47f9ba0339547d986958abdff5f0d`. No public normalized .NET TRX package is claimed because the native manifest does not satisfy the approved public source-context schema.
- Official Unity EditMode run `1d9bc42fe8cd43628acbad71a81f2170`: 3283/3283, zero failures, skips, or inconclusive cases. All 1,974 inputs were stable. Original XML SHA-256 `28873f34faea8829c9cf62994cd40d2f9fef35c4c8d739f1253a75a46281c4d4`; protected manifest SHA-256 `05c6a518f47a876bd83765a1a3ef29f7d397252d16daafaad285e9840edc8843`.
- The official normalized EditMode package verifier reported `VALID` against the protected original manifest: one run, one archive entry. Public manifest SHA-256 `0be5f5982dab74fe9cc1bbae06ff3e9e895dac4b54c83629a49ad550972fefd7`; result-only ZIP SHA-256 `cf1c4537d420f19f44ed3d06ad2feafb291ba44b07910ec4c4ab8085660a3c9a`. Inspection found no workstation-local absolute path in the public manifest or validation record. Original reports remain private.
- Official Smoke filter run `bc46a1ec7c514e359dbbfb1c1d20635e`: 5/5, with the five identities also present in full EditMode. The earlier `TestCategory=Smoke` attempt selected zero and is correctly excluded. No standalone normalized Smoke package is claimed.
- The code-review checkout uses Git's Windows line-ending conversion. Of 26 raw source-manifest hash differences on unchanged files, normalized text matched in all 26 and no content differences were found. The six changed implementation/test source hashes match the runner records. The reviewed candidate states that the official runs bracketed the implementation commit and no executable source changed between them.
- Candidate reports `git diff --check` PASS; the review checkout is clean.

## Disposition

Do not treat this candidate as a `VALIDATED_CANDIDATE` or promote it while the stated host-availability authority and scope-description gap remains. No test rerun is needed solely for review-record bookkeeping. Re-review any corrected implementation at its new exact tip; validation must cover the changed executable tree.
