# Core-owned selected census source — validation

Validation date: 2026-11-05

## Candidate identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Code commit: `bab7f816cedcdbb4ba7e61990ad8d864a3477663`
- Code tree: `b6a464a450fd7a5816c012f36649cbbdfebb9d20`
- Parent: `40a687780f2cb1648f44524ccdc1e9390bc231c1`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This change moves selected census-source selection and reconstruction into Simulation.Core. The Unity host supplies the complete quiescent owner census; Core validates the exact owner graph, selects the P12-F source set, and validates both normal admission and restored-vector capture. Unity retains host quiescence and the exact Justice projection identity ports. The full selected census and mixed-roster fallback remain supported.

Changed implementation and test paths:

- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`

This is an incomplete CORE-F candidate. It does not establish a complete Unity-independent runtime, the fixed .NET host, or T/T+1/T+2 continuation, and it is not canonical promotion.

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| Focused Core session-gate tests | PASS, 77/77 | Incremental `dotnet test` |
| Official CoreValidation cold | PASS, 1,017/1,017 | Run `009978abec6843c4a6d8de62295c093a` |
| Official CoreValidation warm | PASS, 1,017/1,017 | Same run; original cold and warm TRX files are preserved |
| Focused Unity admission test | PASS, 1/1 | Official runner; original report and run manifest remain in protected evidence |
| Full Unity EditMode | PASS, 3,299/3,299 | Run `80cb8e311b7d4c169883391e989a0608`; stable input snapshot |
| Official Smoke | PASS, 5/5 | Run `12b364c9500f48c9834f2d2e932fdeef`; all five case identities also pass in the full EditMode report |
| `git diff --check` | PASS | Code commit against its parent |

The full EditMode evidence package is `UnityFull/coref-core-owned-census-source-bab7f816-full.zip` with manifest `UnityFull/coref-core-owned-census-source-bab7f816-full.manifest.json`. Archive SHA-256: `208d85958a2bdffdbd3e09ff37bcb1598819036c9da2d6fb17c94103c147e0a4`. Manifest SHA-256: `6c9c05e0b97382b5a1ae64cff8469c6803b48a2c87cc1be485f000328554559b`. The official verifier accepted it (`runs=1`, `entries=1`) with the same archive hash.

CoreValidation's original reports and manifest are preserved in protected local evidence under run ID `009978abec6843c4a6d8de62295c093a`. The public .NET TRX packager was not used: the native CoreValidation manifest uses schema `simulation-core-validation/v1`, while that packager requires `dotnet-trx-source-context-v1`. No source-context record was fabricated and no normalized public .NET package is claimed. The CoreValidation manifest also reports `libraryAbsent=false`; these results do not claim a Library-free run.

The filtered Smoke XML was not edited. Its standalone package was rejected by the official packager because the filtered NUnit tree's declared testcase count does not match its descendants. The full EditMode package contains the same five passing Smoke case identities; original Smoke evidence remains protected locally.

Validation completed before the code commit, and the committed implementation/test blobs match the validated input hashes. The only subsequent addition is this documentation and the official full EditMode evidence package. User-owned `ProjectSettings` changes and all unrelated, untracked, and protected files were excluded.
