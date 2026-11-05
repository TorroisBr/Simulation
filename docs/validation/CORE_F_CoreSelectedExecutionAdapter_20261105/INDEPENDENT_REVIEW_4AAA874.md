# Independent Review — CORE-F Core Selected Execution Adapter

## Verdict

VALIDATED_CANDIDATE for the bounded session-bound selected-execution-adapter change and its exact validation package. This record transcribes the independent exact-tip review and its evidence follow-up; it does not promote the candidate or close CORE-F.

## Identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Reviewed implementation commit: 73ec1bde90604b19814e43ae27de80dcd646ff29
- Reviewed docs/evidence successor: 4aaa8743891a3e1f305547b8afcad71a6cce098e
- Implementation review base: 42da4175645f6448b88fc6ea7bfdc3e84ade374d
- Candidate code tree: 88c46e173801fbf9df1e4ef0b0c72f51ef290c20
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Evidence successor changed only the validation record and isolated Unity manifest/archive; implementation and test source remained unchanged.

## Reviewed change

Core retains a selected execution adapter only while it is bound to the same exact selected runtime owner graph. When an admitted dynamic roster transition replaces that graph, Core drops the stale adapter and asks the existing host boundary to create one for the replacement graph. The test verifies one factory call for repeated requests on the same session/graph.

Independent implementation review found no actionable code or test-integrity defects. It confirmed exact graph identity, adapter invalidation after graph replacement, preservation of Core advance/capture/admission/P12 behavior, and the dynamic roster regression passing after the fix.

## Source identity

| File | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs | f87ae6da47144984e638da59d054be14cd8d239693e0c17876f9e0cda6951e61 |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs | 0df45408eef05fd8cab51e6bb4ebc4f532d7f5a7b43e67625143f2014d899f0c |
| tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs | 0e5859f5c829e62209ea5d7b3f921bf9912d2c2c101a0594bca71d3eebaf91e6 |

## Validation

- Official CoreValidation, from the E: candidate worktree, SDK 9.0.315: 1,016/1,016 cold and 1,016/1,016 warm; run 65d194f510974fadbd7180e2ad362eff; input digest af9374c261a0f2a781284dc910280ad2f8ed1a0f519608aee2e1d21ba13e2fb3.
- Isolated Unity 6000.3.9f1 / Test Framework 1.6.0 at exact implementation commit 73ec1bde and tree 88c46e17: full EditMode 3,299/3,299 PASS, stable run 314552bfc799491c9c114cf9bd63bb6f.
- Isolated official Smoke: 5/5 PASS, stable run aaacf0eca78b4a16bf2e99c7a5417326.
- Isolated SimulationRuntimeLongRunTests: 7/7 PASS, stable run 61e5fee532234716b73fe4192e831e7a.
- All 12 filtered Smoke/LongRun case identities and passing outcomes match the full EditMode report.
- Full EditMode package: packager Verify returned VALID (one run, one report); archive SHA-256 860fa0fd8cb149c71251af56b57d7a6cd6db71d55c4f9bdc7f58aad77d4e930b; public manifest SHA-256 951f366728fba643c3a1ef455947a7f8853974fbc7f1c3e26b12d4e7fbfbaa1c.
- git diff --check: PASS.

## Evidence review findings and disposition

The initial implementation review returned NEEDS_CHANGES solely because Unity validation had run in the active implementation worktree and the validation record overstated that all cache/build outputs stayed on E:. The required isolated reruns passed and are packaged. The corrected record discloses that an excluded preliminary CoreValidation invocation was launched from the C: orchestration checkout and may have generated or updated ignored C: Core bin/obj outputs; any such outputs were left in place, not cleaned, and not used as evidence.

The first isolated full Unity run passed but had an unstable input snapshot because Unity initialized ProjectSettings files during cold startup; it is preserved privately and excluded. The stable full rerun followed initialization. The closed packager rejected filtered-only Smoke/LongRun XML due to parent-suite counts for the full assembly; those reports remain private, and the complete full EditMode package includes every selected case.

## Scope limits

This candidate demonstrates session-bound adapter reuse and graph-transition rebinding only. It does not complete DailyV1Application.TryStart, detached .NET hosting, the full CORE-F runtime/restore contract, or Phase/Core closure. No canonical promotion is claimed.