# CORE-F Genealogy Census and Restore Validation

Status: validated candidate checkpoint; not Core canonical promotion or CORE-F completion.

## Bounded change

- Core recapture now verifies and rebinds the required Genealogy census row to the exact `GenealogyStore` already bound to the selected runtime owner graph. Missing, duplicate, foreign-owner, and stale-revision rows are rejected; an exact empty owner remains a required zero-cardinality witness.
- Rebuilt selected-runtime membership graphs retain the same Genealogy owner instance.
- The P12 detached restore compatibility composer binds the staged Genealogy owner to the restored Core graph before constructing its Unity projection.

No assertions or P12 admission checks were weakened.

## Source and validation inputs

- Base commit/tree: `5cb9d0d9c51da9e7eb8843ae31368feac1e03e53` / `6520f01044ea4983f9928c41c497b50f70eacbf7`.
- The CoreValidation run recorded working-tree input digest `46310f39736935339208b8bf1753a00fe0e83e1c63f20454e01f7fb6fff31506`; its Core source and test files match the candidate changes below.
- The final Unity full-suite and Smoke runs both recorded stable input digest `0062a8fcfe71cd20ed157cd24b4638ab1992effba0d8a6ea4625b137f21644f5`. Their complete input inventory is retained in the public package manifest.
- Changed source/test byte identities in the final Unity input inventory:
  - `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs`: `ca032ca9da5159e51057bc3c497d9b5cf89174cea19d1c82c37d3f8c72f33e6d`.
  - `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`: `cd90806667cb4c7e0ce4931c580ab9ad45a83ce75b44f2b4a3899ad26c52e8c8`.
  - `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs`: `82ef0e04c22bd49fb4244d2619bc6480ae24761e6d1fc7abdd6fdbe3f054ef55`.
  - `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`: `8f09e7a87c349596f8ecc10a6888314966e594a6a25cd0a4b6bcf0aa093a6351`.
  - `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs`: `b67a37e701fc34004f02a284816cb9fcea712f4cd1d3723adcbfc80b916ac53f`.
- The Unity inventory included local changes to `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, and `ProjectSettings/ShaderGraphSettings.asset`. These are preserved outside the candidate commit and were not edited, staged, or reverted as part of this checkpoint.
- CoreValidation ran before the Unity restore-composer edit, which is Unity-only. The Core source and test bytes did not change afterward.

## Results

| Gate | Run | Result | Input identity |
|---|---|---:|---|
| CoreValidation cold | `2b7952a7f8f840cc81406aa79edd485d` | 965/965, zero forbidden outcomes | Stable Core inventory `46310f39736935339208b8bf1753a00fe0e83e1c63f20454e01f7fb6fff31506` |
| CoreValidation warm | `2b7952a7f8f840cc81406aa79edd485d` | 965/965, zero forbidden outcomes | Same run and inventory as cold |
| P12 restored-session exchange regression | `c8bfd0c2a0704ad09243c41f5551c50a` | 1/1 PASS | Stable Unity input inventory `a9bac87e750d6d7f25a79a8027fb09ec3b99e93597abcd4be50c623bb43b63ed` |
| P12 dynamic NPC roster restore regression | `01e2524b074342028aa7dcab80205f7e` | 1/1 PASS | Same stable Unity input inventory as the focused exchange regression |
| Full Unity EditMode | `b13b4a6aef534d31af0277871fd24064` | 3,283/3,283 PASS | Stable final Unity inventory `0062a8fcfe71cd20ed157cd24b4638ab1992effba0d8a6ea4625b137f21644f5` |
| Official Unity Smoke (`EditMode`, filter `Smoke`) | `62419af29dda484ab085797f8ad102e3` | 5/5 PASS | Same stable final Unity inventory as full EditMode |
| `git diff --check` | final candidate working diff | PASS | No whitespace errors |

The full EditMode XML and the separate official Smoke XML were independently checked to use the same final input inventory. All five Smoke identities also appear as Passed cases in the full EditMode report.

The validated result-only package is [`coref-genealogy-20261030-editmode.zip`](coref-genealogy-20261030-editmode.zip), SHA-256 `87c4097294339cc9bb750127de07bf784f54c0d79692e637cfa1a17e8029b2b2`. Its manifest SHA-256 is `52120f17142868292b9981e32273a4c10b0b5bba9f946a7e1e3cb0f0b1ff1214`. The approved packager verified the archive against the protected original full-suite run. It contains all 3,283 cases, including all five Smoke cases.

The separate filtered Smoke XML remains in protected evidence: the packager rejects that filtered report because its parent-suite aggregate `testcasecount` does not match the selected descendants. No XML was changed. The complete full-suite report is packaged instead; the separate 5/5 run remains independently inspectable by its run ID.

The CoreValidation original manifest and cold/warm TRX files remain in protected evidence. Manifest SHA-256: `647158fa60fef0e9456b08c37bce1324cd3e94fe3816f5f05767615167538e20`.

The CoreValidation worktree contained a Unity `Library`; its manifest records `worktreeLibraryAbsent=false`. The 965/965 result validates the Core suites but is not evidence that this checkout was Unity-cache-free or an independently packaged detached-host run.

## Diagnostic history and disposition

Before the fix, the full EditMode run had two failures in the restore cases named above. The dynamic membership graph dropped its exact Genealogy root; the restore compatibility path passed `null` to the Core composer even though P12 had staged a Genealogy owner. The final implementation retains and binds that exact owner, and both original regression assertions pass.

Temporary diagnostic-only runs and a mistaken PlayMode selection that executed zero tests are excluded from gate evidence. They remain preserved with their original manifests; no zero-test result is counted as Smoke. The accepted Smoke gate is the five-case EditMode run recorded above.

## Protected evidence

The official runners retain original XML, TRX, logs, and full private manifests in owner-only local evidence directories, identified by the run IDs above. The Unity result-only package omits logs and output-bearing XML fields under the approved packager policy. No private workstation path, raw log, credential, or unrelated user file is included in this validation record or package.
