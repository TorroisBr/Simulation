# Independent exact-tip review — P12-E unified Core owner staging

## Reviewed identity

- Repository: TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Review base: `7e7e8018f6bb2448411d83344af9398c267a3bb0`
- Source/code commit: `88920f0e2c4af7c6084a96e73947abf6c982eca3`
- Source tree: `5be4aa5513f8c9f368491acc05979ce1c480d02a`
- Evidence tip: `576b79d044980e29c00cc47eedb9de4ffddb3fe1` (tree `b4fe0d1e16167f35a5db4fee198cceb145b9a41f`)
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Verdict: **VALIDATED_CANDIDATE**

## Review result

No actionable defects were found in the bounded P12-E unified Core owner-staging change. The ordered private builder stages Institution/Office, Property/Estate, Faction, Political Claim, Political Support, Political Decision, Justice Records, then Military. The military snapshot receives the exact staged P8 `SpatialAuthorityStore` and `stagedLocalTopologyComposed=false`. The builder returns its private stage only after all eight stages succeed; the Core application admission check remains before owner-value staging.

Capture and image-restore callers now consume the ArmedForce, manpower, and position stores returned by that Core stage. Their downstream conflict/war/battle staging receives those same ArmedForce and P8 spatial references. The previous separate post-Core military staging calls were removed. The diff also leaves whole-graph publication to the existing restore coordinator.

The tests cover dependency order, exact Person/Institution/ArmedForce/P8 owner references, zero restored revisions, rejection of an invalid military schema with a null stage, and preservation of the application restore-authority gate. The exact base-to-evidence diff contains eight paths across source, tests, and validation evidence; it contains no ProjectSettings changes or unrelated paths.

## Validation evidence inspected

- CoreValidation run `43622a46fb494236818d02ec47fb2b18`: cold and warm each 923/923 passed with zero failures. The private manifest SHA-256 is `f91cf9c34a419e626c5fc58f51ab243b3d54b386cbb63678bbc40697dcfc313f`; its committed source/tree are `88920f0e` / `5be4aa55`, input digest `78d581dbea929e877585db2d642382966de91e74b9a0d92f4ed963b425b7b7cb`, and worktree-change list is empty. Both original TRX hashes match the validation record: cold `2beb985839431b4ba5c51a5d7583731e2f163e190b7ce4fc9f76816884f36be2`, warm `4820a68336c52b84a83fa57e88da34b865b505a0a4786e5e9360694a40346141`. The validation workspace had no Unity `Library`.
- Standalone Simulation Lab run `4c91af367eb247e695f45b1094568dc1`: locked restores/build succeeded, cold and warm runs exited successfully, and outputs matched.
- Unity evidence: P12-E focused 111/111, Smoke 5/5, full EditMode 3279/3279 with zero failed, skipped, or inconclusive. The full report is the normalized XML archive with SHA-256 `397b362ed1c3a2105a6e0e06428439bdd0167014047eaff33cce4c46ca81c8b3`; its public manifest SHA-256 is `c7ddc570356079bdd653bc931b9ac5ca4be80a28afa23655b21e0d51d25b7fa8`. I verified the remote manifest/archive at the evidence tip and parsed the full report. Unity input digest: `c51b94bc7d17db206284b9638c06a8038c921f0b5bbadecbfa9b8f1f3e407455`.
- `git diff --check` passes for base `7e7e8018` through source commit `88920f0e`.

The review inspected the committed evidence; it did not rerun the CoreValidation or Unity gates. Original .NET TRXs remain in protected local evidence; no public .NET TRX package is claimed.

## Scope boundary

This review accepts only the exact P12-E unified Core owner-staging candidate above. It does not claim complete CORE-F, P12-E closure, Phase closure, or canonical promotion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
