# CORE-F application current-session advance review

## Verdict

`VALIDATED_CANDIDATE` for the five-file current-session advance follow-up. This record reviews the exact candidate tip and its delta; it is not canonical promotion, CORE-F acceptance, or Phase closure.

## Exact identities

- Repository: `TorroisBr/Simulation` (`https://github.com/TorroisBr/Simulation`).
- Candidate branch: `codex/core/CORE-F-runtime-app-session-boundary-20261025`.
- Candidate: `7f7632db6e1e2c451cc56a1cf34eb4f2659ce555`.
- Candidate tree: `cf7f2ff9799638a9c511c23693af8c0d5467a24b`.
- Exact delta base: `251afabc1cd198bd9e9898e405d983f469b3e0c4` (tree `3d60b4a6c43b6a28c977f1f993ffa6b2ae38b08b`).
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The remote candidate ref matched the full candidate SHA at review time. The new commit is a direct child of the reviewed base; canonical remains separate and unchanged.

## Reviewed files and behavior

The delta contains only:

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`
- `docs/validation/CORE_F_CoreSelectedRuntimeAdvance/VALIDATION.md`

The new `TryAdvanceDay(out failure)` and `TryAdvanceDays(...)` overloads read the application's volatile current-session reference and delegate to the existing exact-session implementation. That implementation verifies reference identity, reserves advancement under the session gate, checks owner-thread and active-operation state, then invokes the Core selected execution boundary. The reservation is released through `IDisposable`. The batch overload initializes `daysAdvanced` to zero on absence or rejection.

The Unity forwarding helper no longer chooses or passes a Core session reference. It passes only the existing runtime projection used to create the established typed failure diagnostic. The tests cover no session published, both current-session overloads entering the existing rejection path without falling back to host-owned day advancement, and preservation of the Unity failure diagnostic. No owner, P12 census, epoch, lease, mutation, deterministic ordering, or persistence semantics are changed by this delta.

I found no actionable defect or weakened assertion in the reviewed delta. The previous exact-tip review of `251afabc` remains applicable to its unchanged parent content; it does not substitute for this exact-tip review.

## Validation and evidence applicability

- Official CoreValidation run `214b262df74e44199e49a033fd94a805`: cold 834/834 and warm 834/834; both exit code 0, zero failed or skipped, stable input digest `0745e8b8b4316adfe84e69f524c2007a3a32347bef487962b77f7ac58a693e05`. The exact current source hashes for `DailyV1Application.cs` and `DailyV1ApplicationSessionGateTests.cs` match the official input snapshot.
- Official Unity focused run `5c2a7fcaa22e4e12b630e2d60d68e4c4`: `SimulationRuntimeOrchestrationTests.ApplicationBoundSimulationLoopPreservesTypedAdvanceFailureDiagnostic`, 1/1 passed, zero failed/skipped, stable input. The exact current source hashes for `TesteSimulacao.cs` and `SimulationRuntimeOrchestrationTests.cs` match the official input snapshot. Original XML SHA-256 `3fc9b2988ed181ae502657a49cf852cf840c0c151960fb822b7cefc442c3fb3f`; log SHA-256 `e8fbf0ce2843c1014e1c68ba8c37930882f9eea757ce67f5176c2a8cf2a3bb25`.
- `git diff --check` passed for the candidate delta.
- The earlier full Unity result 3270/3270 applies to base `251afabc`, not this follow-up. No full EditMode, Smoke, or LongRun rerun is claimed here. Normalized .NET and Smoke packages are absent as stated in the validation record; this remains an overall CORE-F evidence-closure limitation, not a hidden pass.

## Scope and constraints

This is an internal application API follow-up. It does not complete `DailyV1Application.TryStart`, Unity-independent runtime construction, the selected projection/census split, detached persistence/restoration, the fixed .NET executable host, or separate-process T/T+1/T+2 continuation. CORE-F remains incomplete and unpromoted.

The candidate commit manifest contains no `ProjectSettings` files. The existing local user changes to `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` remain outside the candidate.
