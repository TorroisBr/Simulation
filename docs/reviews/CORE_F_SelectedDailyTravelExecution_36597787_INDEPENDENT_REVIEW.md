# CORE-F selected daily/travel execution — exact-tip review

**Verdict:** `VALIDATED_CANDIDATE`
**Reviewed candidate:** `36597787d1d737e951e55dbedb15380065af1ea0`
**Candidate base:** `24e76512cd3c2b238afef35b909d282514e6b403`
**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
**Review date:** 2026-10-23

## Scope reviewed

The candidate is a direct child of its stated base. The remote candidate and Core canonical refs matched the recorded identities at review. The exact candidate delta contains only:

- `Assets/_Project/Scripts/SimulationRuntime.DailyV1TravelStage.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs`
- `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs`
- `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs`

The review checked ownership and mutation authority, admission, deterministic ordering and RNG behavior, identity binding, stale-state and failure behavior, replay/reconstruction sensitivity, test integrity, and the full candidate diff against the exact base.

## Findings

The changes bind day, directive, demographic-owner, actor-roster, and travel-party inputs to the admitted Core state/owner graph. Party membership is used for lookup rather than iteration, preserving party-before-solo order without introducing order-sensitive behavior. The new tests exercise state-owned roster/directive behavior and graph-owned travel inputs; no existing assertions were weakened.

No actionable defects were found. A direct state-backed assertion with a deliberately conflicting host calendar could further emphasize the Core calendar binding, but this is a non-blocking coverage opportunity; the implementation reads the admitted Core calendar and existing tests remain intact.

## Validation applicability

The source inputs were checked against the successful run manifests without rerunning unchanged validation:

- CoreValidation run `c5e089560b8d48e9b1b846ded037b038`: cold and warm `666/666`. The five Core/test files in that manifest match their candidate SHA-256 identities.
- Focused Unity Core runtime run `02305c41896a493bb7f0e5f2402ad61d`: `16/16`.
- P12 regression run `8b8fdf74a97945f2b78ecc20878f3b2e`: `556/556`.
- Full Unity EditMode run `5fb9a509becc481288e1f706fbdb0278`: `3254/3254`; the Unity manifest is stable and its two changed Unity runtime inputs match the candidate hashes.
- Official EditMode Smoke run `1fce345a3bbb4122a0111de55a947e3a`: `5/5`.
- LongRun: `7/7`, included in the full EditMode run.
- `git diff --check`: pass.

The validation manifests and original evidence remain in the approved private evidence locations; this record does not copy or alter them.

## Integration limits

This verdict validates only the bounded seven-file candidate. It does not claim completion of shared `DailyV1Application`, active-session ownership, detached P12 restoration, the fixed .NET host, separate-process T/T+1/T+2 continuation, or full CORE-F acceptance. It does not promote or authorize promotion of Core canonical.
