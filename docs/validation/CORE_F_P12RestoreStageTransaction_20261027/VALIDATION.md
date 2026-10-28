# CORE-F P12 restore-stage transaction validation

Date: 2026-10-27 (America/Sao_Paulo)

## Scope and source identity

Candidate base: `c2109dd9eb7efbcae4fcebbfde5562a35e9e791b`.

This evidence covers the ordered Core-owned P12 C→D→E→F restore-stage transaction and Core validation of restored TravelParty bindings. It does not claim full detached .NET restoration, completion of P12, or completion of CORE-F.

The seven implementation/test inputs were:

- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs`
- `Packages/com.simulation.core/Runtime/Persistence/P12CSelectedCoreRootStager.cs`
- `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs`
- `tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj`
- `tests/Simulation.Core.Tests/DailyV1P12RestoreStageTransactionTests.cs`

The source files were unchanged after validation. Unity's before/after validation-input digest was `42d66b2b89a8d2c21931a6bd08f7ce66a5cb3b181569f5f1a7f0ab5f124a7680` in all three runs.

## Validation results

| Gate | Run ID | Result |
| --- | --- | --- |
| CoreValidation cold | `c5b430d9b1ae4554be8ab74524eaa6c6` | 906/906 passed |
| CoreValidation warm | `c5b430d9b1ae4554be8ab74524eaa6c6` | 906/906 passed |
| Focused P12 restore EditMode | `813d1f0fe8e2460bb36c6c0fcf03067b` | 15/15 passed |
| Full Unity EditMode | `9fefc02e4dd645b3b59c804ee49b054d` | 3274/3274 passed |
| Official Smoke | `86fca371a1214464a54bc3123030e5e2` | 5/5 passed |
| `git diff --check` | final source and record | passed |

The 15 focused cases and 5 Smoke cases were matched by `(test id, full name)` to passing cases in the full 3274-case EditMode report.

## Durable result package

The normalized, result-only full EditMode package was produced and verified by the repository Unity evidence tool:

- Manifest: `coref-p12-restore-stage-editmode-20261027.manifest.json`
- Manifest SHA-256: `25ed78c7385a5de764446184d000ab1b70ef9cb286a7f1cf1498881e5e4cce11`
- Archive: `coref-p12-restore-stage-editmode-20261027.zip`
- Archive SHA-256: `a341560554a9813d428a99dce97be963c0f7c269166be6c42f90ec7bddf53767`
- Archive size: 173279 bytes
- Verification: `VALID`, one run and one result entry

The Unity runner rejected packaging the focused and Smoke filtered XML because their parent suite nodes retain the unfiltered suite `testcasecount`. The originals and their raw logs remain unchanged in protected local evidence. Their test identities and passing outcomes are present in the packaged full EditMode report. No XML was edited to make it package.

CoreValidation's original reports and logs remain in its protected local evidence store under the run ID above; no raw .NET reports or logs are published by this record.

## File preservation and publication

Unrelated ProjectSettings edits and untracked Unity `.meta` files remain unstaged. The official Unity input inventory records relative file names and SHA-256 values for all required validation roots, including those files; the published archive contains only normalized NUnit result XML and does not copy their file contents. Private raw reports/logs, local build outputs, and earlier protected evidence are not staged.
