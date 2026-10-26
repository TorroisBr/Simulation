# CORE-F P12-C Retained Roots — Validation

**Status:** Validated implementation checkpoint; independent exact-tip review is pending. This record does not promote Core canonical or declare CORE-F complete.

## Candidate identity

- Candidate branch: codex/core/CORE-F-shared-daily-v1-app-host
- Published candidate ref: codex/core/CORE-F-runtime-app-session-boundary-20261025
- Implementation commit: 386f43d38b69e87f407dcd9505580d15d15237e3
- Implementation tree: 166499fc54ebc5712d39198bc642f7631869269a
- Base: 01ea01fb29b755bb2e99e3f5d7c6eb11daf5f358
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The ten-file implementation/test change is limited to P12-C retained-root handling across Core genesis and restored sessions, its Unity restore coordinator, and focused regressions. The change keeps restored sessions distinct from genesis compositions while retaining the exact admitted P9 manifest and Core-owned spatial authority needed by a later Core-bound save. It does not claim that restore reruns genesis, or that the detached host/full CORE-F acceptance is complete.

After validation, the candidate commit was created from the same source and test bytes. All seven changed CoreValidation input files and all eight changed Unity source/test inputs were hash-compared with their recorded pre-run inputs and matched. The two pre-existing local ProjectSettings edits were excluded from staging and remained unchanged.

## Validation

- Official CoreValidation: cold 822/822 PASS; warm 822/822 PASS. Protected original run: c132b6140e7448e68825907f736570b4.
- Focused restored-save continuation: PASS. Protected run: 0b4e35460690439d98411e15043d0b3c.
- Focused restore-stage operation invalidation: PASS. Protected run: 9c22f9df105249ce8007e7aa649d3da6.
- Unity ALL EditMode: 3269/3269 PASS, with zero failures, skips, or inconclusive cases. Protected original run: e2f7fa3e121d4d9fb19c6be3efcc164f.
- Official Smoke: 5/5 PASS. Protected original run: e033b0b9379c447ba8bff40dc80423e4.
- Applicable SimulationRuntime LongRun cases are included in the complete ALL EditMode run; no separate LongRun run is claimed.
- git diff --check: PASS for the implementation commit.

The public Unity evidence package contains the complete unfiltered ALL EditMode result report. The official evidence verifier returned VALID for one run and one archive entry. The archive contains no local workstation path or credential patterns.

- Manifest: coref-p12c-restored-roots-386f43d.manifest.json
- Manifest SHA-256: CFF9D0E0C5CFD6C89155F5B571779B397D2E34038786DEA7E735DDD34F97F15C
- Archive: coref-p12c-restored-roots-386f43d.zip
- Archive SHA-256: 5b59173bd81bbe5658691f7216b3b5d11e502358272a2e7b07b9ec2ee55ebdb9
- Packager: Unity validation evidence package schema unity-validation-evidence-package-v1, tool version 1.1.0.

Focused Unity reports, the filtered Smoke report, and original CoreValidation reports remain in protected E: evidence storage and were not added to the repository. No public .NET TRX projection is claimed. The complete Unity package is not represented as a substitute for the protected .NET originals or for later full CORE-F acceptance.

An earlier compile attempt in this change sequence caught an incorrect access through the Unity manifest wrapper; the source was corrected to use the staged Core root before the successful focused and full runs. No test assertions were weakened to obtain these results.

## Files in the implementation commit

1. Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs
2. Assets/_Project/Scripts/SimulationRuntime.cs
3. Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs
4. Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
5. Packages/com.simulation.core/Runtime/Application/DailyV1ApplicationSessionGate.cs
6. Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs
7. Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs
8. Packages/com.simulation.core/Runtime/Persistence/P12CP9GenesisManifestCoreSnapshot.cs
9. tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
10. tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs

## Scope limits

This is a candidate checkpoint only. It does not promote codex/core/canonical, close CORE-F, establish complete owner-source closure, claim a fixed detached host, or prove separate-process T/T+1/T+2 parity. The local ProjectSettings/EditorBuildSettings.asset and ProjectSettings/ShaderGraphSettings.asset edits remain uncommitted and untouched.
