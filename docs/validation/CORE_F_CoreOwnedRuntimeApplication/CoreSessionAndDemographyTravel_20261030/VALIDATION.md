# Core-owned session and demography/travel validation

Validation date: 2026-10-30

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Candidate code commit: 8414de899e24a04ff6139cee5570f65bdfb9325f
- Candidate code tree: 0cb45c804e2627769a50b11f743d8c1b8c3b074c
- Candidate parent: 3d1d5f660467c98c4622d03a9659685c59530eea
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- The code commit contains ten implementation and test paths. Their committed Git tree is the validated source identity; the clean .NET run records the same commit and tree. The Unity runner's protected post-run input snapshot matches the Unity-side changed files. Two shared C# files have different raw hashes between Unity and clean .NET checkouts because their working-tree line endings differ; both checkouts resolve to the same Git blob IDs.

Changed paths:

- Assets/_Project/Scripts/SimulationRuntime.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs
- tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs

## Validation results

| Gate | Result | Source/evidence identity |
|---|---|---|
| CoreValidation cold | PASS, 960/960 | Official run e0b38a3fdd214c55a8572d92d36ac568; native report fca352e8-b912-4e65-a2d5-6cf6e824d3b7; clean checkout at the candidate commit, Unity Library absent |
| CoreValidation warm | PASS, 960/960 | Same official run; native report 8407681b-b551-4767-9dff-5cef4868de1f |
| Unity full EditMode | PASS, 3,283/3,283 | Official run 5c5ece02e37544edac9806042bd91b7f; stable input snapshot; original XML SHA-256 98441d661edfbfdb549b7d3b131a77dca8076f0929dfd77d84936f005c005683 |
| Filtered Smoke check | PASS, 5/5 | Official run b94dc63aa9844e0ab160046d28aee867; stable input snapshot; original XML SHA-256 0e01aaf54a060f399592a3ae4c14ffa2168b91c2333fe8a4bcdd0eea50a733c3; all five case identities also occur in the packaged full EditMode result |
| git diff --check | PASS | Candidate code commit against its parent |

The cold/warm CoreValidation source inventory is stable (586e90ceeecfe60688b7b936f48125e34236063d541a6626eaa1add1f989b743). Runtime: .NET SDK 9.0.315, CLR 8.0.28.

## Evidence packages

Packages were produced with the repository's official evidence packagers. They contain privacy-normalized test XML and manifests; original reports, logs, source contexts and private metadata remain in protected local evidence storage.

### .NET TRX cold/warm

- Manifest: DotNetTrx/coref-session-demography-cold-warm-20261030.manifest.json
- Manifest SHA-256: 8ca99239a5a24d30e39d4b2e3e73442e05b5706ec9377a7bec31fe8b27a3399c
- Archive: DotNetTrx/coref-session-demography-cold-warm-20261030.zip
- Archive SHA-256: a6f748acc103efd93d73ecdf834650d9110b9b9edce922607ca4431ebc513057
- Original-to-normalized TRX comparison: PASS for both cold and warm reports; each attempt has coherent 960/960 counters and zero failures.
- Cold original report SHA-256: 4b58bea6fb20208dda2a3be2a42e492dfb27d9706cae9ecfac90f7cf56c33620
- Warm original report SHA-256: 2d9c245053bffff0e0067344c516cc3cfe9422167668060f00a60d8671c44d72

### Unity full EditMode

- Manifest: Unity/coref-session-demography-travel-editmode-20261030.manifest.json
- Manifest SHA-256: 579b1caad3b9f58896860c815b970c041002d6af20ea57c2a477e028c96796e1
- Archive: Unity/coref-session-demography-travel-editmode-20261030.zip
- Archive SHA-256: b89e5262110f43751a893b56dcbc5a5740216be1cfa406e2fa8a3d2e91c0e47a
- Original-to-normalized XML comparison: PASS; the package is bound to the protected run manifest and stable input snapshot.

### Filtered Smoke packaging limitation

The original filtered Smoke report is preserved unchanged. The official packager rejected it because the filtered XML has five result cases while suite-level testcasecount attributes retain full-assembly counts (including 3,283 at the top suite). This is the packager's documented fail-closed count check. No report was edited or repackaged through another route. The passing full EditMode package contains the same five Smoke case identities. The protected Smoke manifest and original XML remain available for authorized review.

## Superseded attempts and scope

- An earlier CoreValidation invocation used the wrong working directory and stopped at locked restore with MSB1009 before compilation or test execution. The approved runner was then invoked from the clean candidate checkout and passed cold and warm.
- A pre-commit cold run found one test-fixture call-log ordering failure. The fake was corrected to record the boundary request before its injected rejection; the assertion and production behavior were preserved. The final exact-commit cold/warm run passed.
- No canonical promotion or CORE-F completion is claimed. This evidence applies only to the candidate commit and its bounded integration slice.
- Existing user ProjectSettings edits, unrelated untracked files, and protected evidence were not staged or included in the candidate.