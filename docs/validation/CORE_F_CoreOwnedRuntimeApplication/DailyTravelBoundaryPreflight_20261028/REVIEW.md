# Independent exact-tip review — admitted travel/P12 boundary preflight

## Reviewed identity

- Candidate commit: `4480557b6e2bd622f209875e83d35c33fbe73890`
- Candidate tree: `b1ed44e1ee29aecf86c6578bddf215124c0896eb`
- Review base: `f2be368f809c2c5a3e1d7fad933af686d6951ad1`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Verdict: **VALIDATED_CANDIDATE**

## Review result

The independent reviewer found no actionable findings. For fully selected admitted execution, the travel-context preflight is selected by Core's exact runtime roster. The production factory checks the exact graph and party-store binding and maps each selected Core NPC owner to its exact Unity adapter; it checks fault state and allocates a context without advancing travel, opening a P12 scope, mutating owner data, or logging.

The same context is reused at the existing post-actor travel point. Travel advancement, projection publication, observation, and P12 mutation remain at their established location. Mixed-roster execution skips early acquisition and preserves `BeginSimulationDay` before the later travel-context acquisition. Non-admitted execution keeps its previous acquisition point.

The new failure regression proves that missing travel/P12 context is rejected before logger and enabled demographic-writer callbacks and publishes no daily result. The mixed-roster test keeps its existing compatibility assertions and adds only an ordering assertion. No action result or established execution order was rewritten.

## Evidence applicability

The exact candidate source hashes match the official runner manifests:

- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs`: `2afa32b99c0d03a7ec36b2a7b2073c951141c4e82fd9eea8466430f75a4e08bf`
- `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs`: `c68d0646dc6bac7aec3cd59e190c248fb13a77fbc5bcdab15ede9665e063108e`

CoreValidation passed cold and warm at 909/909. Full Unity EditMode passed 3276/3276, including all 7 `SimulationRuntimeLongRunTests`; filtered Smoke passed 5/5. The reviewer verified the private manifest identities and the sanitized Unity package hashes against [VALIDATION.md](VALIDATION.md), confirmed stable source snapshots, and found no absolute local paths in the public package. The reviewer did not rerun tests. No Library-independent .NET claim is made.

## Scope boundary

This is a validated candidate checkpoint, not Core canonical promotion or CORE-F completion. `DailyV1Application.TryStart`, removal of Unity action/P12 adapters, detached .NET host, and separate-process T/T+1/T+2 continuation remain outstanding.