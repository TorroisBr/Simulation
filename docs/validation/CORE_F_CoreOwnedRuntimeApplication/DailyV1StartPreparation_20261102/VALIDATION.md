# CORE-F admitted genesis start-preparation validation

Date: 2026-11-02

## Candidate and scope

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Code commit: 770c70dfc2c3d7522d83dac1dd36c22c1487f61f
- Code tree: 91d91fcb6ed660895543fb35096253abd50859f1
- Parent: 40a232a35ce0d1228beafffca86b59dd01dbc690
- Core canonical remains: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The three changed files add a Core result for the already-admitted NoSave genesis path, route the Unity bootstrap through that result, and assert that the application remains unpublished during preparation. The validation ran on the same three file contents before they were committed; the recorded file SHA-256 values match the committed files and no source changes followed.

This is an internal genesis-preparation integration only. It does not deliver public DailyV1Application.TryStart, Core-owned detached restoration, or an independent .NET host. No canonical promotion is claimed.

## Validation

- Official CoreValidation: cold 995/995 PASS, warm 995/995 PASS.
- Core runner run ID: 746a330784514b98a4e1910d3dbe721a.
- Private Core manifest SHA-256: 845b0668410f48ac9e931097bb3bf296f614da85431f29b5e8493f82cb8858c5.
- Core input digest: 2d2c8cb0acf8f18e752bce0fb1a7a688c76285afd0f061b0860bbaf00d6bcbf1; stable source and generated dependency inputs.
- The Core runner reported a Unity Library in the worktree. This run does not establish Library independence.
- Official Unity ALL EditMode: 3296/3296 PASS, zero failures/skips/inconclusive; input stable.
- Official Smoke selection: 5/5 PASS, zero failures/skips/inconclusive; its complete input snapshot matched the ALL EditMode run.
- Unity Editor: 6000.3.9f1.
- Privacy-normalized Unity result package is in this directory. The official verifier passed against the private run manifest: one run, one result entry. Archive SHA-256: c40605ea393b881dc2ff5770e706f12fd8ded11ccebc23a68bdb1405b7be44dc (174,155 bytes).
- git diff --check: PASS.

The CoreValidation runner retained its original cold/warm TRX reports and manifest in protected E: evidence. No public .NET TRX package is claimed: the runner emitted its native simulation-core-validation/v1 manifest, not the separate source-context schema required by the approved adapter. The adapter correctly rejected an attempt to pass the native manifest directly; no source context was fabricated and no original report was changed.

Unity validation captured the existing uncommitted ProjectSettings values as test inputs. Those user-owned files remain uncommitted and untouched; this record does not claim validation against their clean committed versions.

## Exact changed file identities

- Assets/_Project/Scripts/TesteSimulacao.cs: SHA-256 d18fd2ed4f6a493f9e12ac5c034b262d1d84cd282116d82a884ed38b0d02df17
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs: SHA-256 d07c345084a4f08138ecb0e6fd2e293dd699b65fdd3b225c5fa554ecebb60a1b
- tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs: SHA-256 1d778ddc173dd063e5b7f465bc48ccf422ba2de5f907ac0cd7db0ed303385c6f

Each file's SHA-256 matches its applicable runner input manifest and the committed content. Unity input snapshot digest: fa54a6be0c2a5ee67ed9fd96c5089d6cb61bef8af7f06cd15ffad4907115a8f0.

## Review status and limitations

Independent exact-tip review remains pending. The candidate is not canonical. Full CORE-F acceptance remains open for the Core-owned application lifecycle, detached restoration, independent .NET host execution, and separate-process continuation.