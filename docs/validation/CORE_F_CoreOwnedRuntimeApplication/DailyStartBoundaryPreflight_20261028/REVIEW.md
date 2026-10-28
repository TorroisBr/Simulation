# Independent exact-tip review — Daily-start boundary preflight

## Reviewed identity

- Candidate commit: `af36036d655a8f84658b1fef77bf455ba200c0f9`
- Candidate tree: `9b06a85d106d39b89a3f351df88338eaf47d3912`
- Review base: `3a8aa6c8178b1d9c41dc3d918958593b5e7f1237`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Verdict: **VALIDATED_CANDIDATE**

## Review result

The independent exact-tip review found no actionable findings. For a fully selected admitted runtime, Core selects compatibility using its exact ordered roster and obtains the required day-start context before logger, demographic, or gameplay callbacks. A missing or null context rejects before daily work. The production context constructor checks the exact roster, owner graph, definitions, and Crime/Justice bindings without mutating state or starting an admitted operation.

The mixed-roster path skips this selected-context preflight and retains the established one-time Unity whole-day fallback. The non-admitted path retains its prior behavior. The enabled-natural-mortality regression configures the fake demographic writer to throw, then proves the missing context is rejected first and no demographic result is published. Existing roster and fallback assertions were preserved.

## Evidence applicability

The reviewed production and test SHA-256 values match the exact candidate and the validation manifests:

- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs`: `1cc44468588629f8eccac1f0f565c994f9c697977953a847c1b89b5babe5a7b3`
- `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs`: `0b3be4262647199ea4e04ab4ae3511ad926ef69ecfe310a0da326205c189658b`

The reviewed evidence records CoreValidation cold and warm 908/908 PASS, full Unity EditMode 3276/3276 PASS, official filtered Smoke 5/5 PASS, and `git diff --check` PASS. Private manifests and normalized Unity evidence package hashes match [VALIDATION.md](VALIDATION.md). The reviewer did not rerun tests; the review verified exact-source and evidence applicability. The Unity run does not compile the separate .NET test project. No Library-independent .NET claim is made.

## Scope boundary

This is a validated implementation checkpoint on the candidate branch, not Core canonical promotion or CORE-F completion. The shared `DailyV1Application.TryStart`, removal of Unity action/P12 adapters, detached host continuation, and separate-process T/T+1/T+2 remain outstanding.