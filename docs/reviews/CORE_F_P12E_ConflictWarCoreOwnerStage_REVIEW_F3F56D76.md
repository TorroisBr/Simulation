# P12-E Conflict/War Core owner-stage — exact-tip independent review

**Review status: `NEEDS_CHANGES` (evidence closure only).**

**Code verdict: `PASS`.** The reviewed implementation has no actionable code findings. The checkpoint is not recorded as `VALIDATED_CANDIDATE` because the required official Smoke result is verified in protected originals but has no compliant durable result package; the current packager rejects the filtered XML's suite accounting. This is an evidence-publication gap, not a test failure or code defect.

## Exact identities

- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Candidate validation/documentation tip: `965bafd5980e1e963b4bf3d830d20f57a34eeab6`.
- Exact reviewed code tip: `f3f56d76829adc5467d24781fc73520765a3f3d3`.
- Reviewed code tree: `b9038d65389f19fc65e61a7a63a1e229cc5bd794`.
- Review base: `576b79d044980e29c00cc47eedb9de4ffddb3fe1`.
- Code changes in scope: `e1abf28124b6705b3e34ff656d350832e0ebbdd9` (Core source migration/stager) and `f3f56d76829adc5467d24781fc73520765a3f3d3` (Unity metadata correction). The later `965bafd...` commit adds only validation documentation, the full EditMode package manifest, and its ZIP; it does not change code.
- The review base is a descendant of current Core canonical; it is also an ancestor of the reviewed code tip.

## Code review

The exact `576b79d...` to `f3f56d...` diff contains 21 files (1,605 insertions and 1,453 deletions). `git diff --check` passes.

Conflict and War contracts/stores were split from the Unity-owned combined Conflict/War/Battle files into the engine-independent Core package. The Battle-specific contracts and store remain in Unity. The four existing Conflict/War census-provider and owner-snapshot sources and their `.meta` files are 100% identical renames. All four moved `.meta` GUIDs match their prior Assets locations; a repository-wide scan found no duplicate GUID. The new Core `.meta` identifiers are unique, use the expected folder/MonoImporter format, and the validation snapshot records their CRLF Windows checkout bytes. `Simulation.Core.csproj` explicitly includes the moved providers/snapshots and new Conflict/War sources. Unity's `Simulation.Core` assembly is auto-referenced and has `noEngineReferences`; CoreValidation and the exact-tree Unity run compile the split.

The restore chain preserves its existing dependency order and adds `Conflict` immediately after `Military`, then `War` immediately after `Conflict`, before Unity stages `Battle`. Conflict receives the exact staged `ArmedForceStore`; War receives that same ArmedForce instance and the exact staged Conflict instance. The War snapshot independently enforces that its Conflict store is composed over the same ArmedForce store. The completed stage exposes no owner graph until all ten stages succeed; failure leaves the output stage null. Subsequent Battle staging uses those same Conflict/War/ArmedForce instances. Existing stage observer names and ordering are preserved.

The new Core regression asserts order, exact cross-owner references, and restored zero revisions, and rejects invalid Conflict or War snapshots without returning a partial stage. Existing Conflict/War snapshot and P12-G tests cover payload validation, typed links, ordering, lifecycle, local revision restoration, parent identity, and unsupported P17-A rejection. The owner-stage change does not change capture token/vector checks or publish a session; later P12-E/P12-G admission and publication remain responsible for those boundaries.

No code findings. This is a bounded source-ownership/staging increment only; it does not complete P12-E, P12-A, P12-G, or CORE-F, and it does not authorize canonical promotion.

## Evidence independently checked

- **CoreValidation:** protected run `fed2956898e84fe9a242199fe7d8daf9`; manifest SHA-256 `d004c0924f505c427e6d908d778aafb1efa876beb18a33c45f46284fbc084bea`. The manifest reports exact commit/tree `f3f56d...` / `b9038d...`, stable inputs, no worktree changes, and 924/924 cold plus 924/924 warm, with no failed, skipped, inconclusive, or unexecuted tests. Original TRX/logs remain in protected evidence; this review does not publish them.
- **ALL EditMode:** the committed sanitized package manifest and ZIP were independently read from candidate tip `965bafd...`. Archive byte count and SHA-256 match the manifest (`173,792` bytes; `80f11d3fe2fb648c903b6f4eb072b74db96ed1218fd9db50de1c67bb226975f3`). It contains one normalized result XML with 3,279/3,279 passed and zero failed; the source snapshot is exact `f3f56d...` / `b9038d...`. The package and manifest scan found no C:/Users, workstation username, or E: repository paths.
- **Official Smoke:** protected run `6c192b3c24584dd4beb4df27eca5c645`; manifest SHA-256 `cd0f303d3e4bf9d85953c212e81ee00b3771a598014a9aee8b63493c2c43e993`. The manifest records exact HEAD/tree `f3f56d...` / `b9038d...`, stable identical input snapshots, and 5 passed / 0 failed / 0 skipped / 0 inconclusive. The original XML hash matches `d3679538b9ad68c95c5b9eacb8d698c2ba21f971276cf454daf80c520674a1ba`; it remains private and unchanged. Independent XML parsing confirms root total 5 and five selected testcase nodes, while the nested `Simulation` suite declares `testcasecount=3279`. The official evidence packager rejects this mismatch, so there is no compliant durable Smoke result package. No XML was edited, normalized by hand, or reconstructed.

## Evidence disposition

The official Smoke execution **passed 5/5**, but the result-packaging gate remains open because the filtered XML's retained `Simulation` suite count does not match the five selected cases. The original report is protected and unchanged. Resolve this through the approved validation-evidence workflow and publish a safe, durable result artifact tied to the same exact code tree; do not alter the original report or infer a packaged result from its manifest. Until then, code review remains PASS, but this candidate's complete evidence closure remains `NEEDS_CHANGES` and must not be marked `VALIDATED_CANDIDATE` or promoted on the strength of this review.
