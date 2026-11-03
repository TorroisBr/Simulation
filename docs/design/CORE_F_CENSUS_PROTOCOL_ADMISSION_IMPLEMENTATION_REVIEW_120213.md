# CORE-F Census Protocol Admission Review — 120213DA

## Verdict

**PASS — VALIDATED CANDIDATE SLICE.** Independent exact-tip review found no actionable implementation or test-integrity findings.

## Identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `120213dad68ee7adaa9b7a70f66896ded6ce17bf`
- Candidate tree: `f09c929d2c0765317abaa00223a8a743e66aa759`
- Exact parent/base: `946578287c61a1fec13d86161df2a7a667e8f995`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote preflight: `origin/codex/core/CORE-F-core-owned-runtime-application` points to the candidate; `origin/codex/core/canonical` remains at the canonical SHA above. Candidate is a clean fast-forward descendant of that canonical history.

## Reviewed payload and behavior

The complete base-to-candidate delta contains only:

- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs`

Core now owns the owner-thread check and the equality decision between the selected session's runtime-protocol operation count and the census's registered active-operation count. The Unity adapter exposes only the census count read; its existing census reader still enforces its bound owner thread. An unreadable or negative count, wrong Core admission thread, or count mismatch faults admission before a daily operation scope opens. On a matching count, Core still uses its atomic operation registry with `requireNoActiveRuntimeProtocolOperations`, so a pre-existing active protocol operation is rejected as before.

Coverage includes unreadable, negative, and mismatched census counts; rejected paths assert no operation scope or active count is published and admission is faulted. Existing tests continue to cover active-operation rejection and off-thread scope disposal. The Unity test double was updated for the port shape without changing assertions. The comment now accurately assigns the comparison to Core.

No owner rows, mutation epochs, census collection, capture, persistence, restore, day execution, action semantics, or P12 profile scope changes. No duplicate authority or API exposure was added. `git diff --check` passes for the exact base-to-candidate diff.

## Validation evidence

- Official CoreValidation: PASS, cold 1001/1001 and warm 1001/1001, zero failed/aborted/not-executed/inconclusive cases. Run `562b2e20363a4fc0a39fb73eaa7e0a83`; stable input digest `5b3a2b436dba671cdbdebcef758833c98508e7c48c78c1b0321e93bd378c65c1`. The run started from parent `9465782` with the candidate source/test changes in its working tree. The changed Core implementation and test files' SHA-256 values match the committed candidate; all other CoreValidation inputs were unchanged. Cold TRX SHA-256: `ea06fa10ff8951a2d4efe66a7477954515d4687015ef77918a3cddbc4237bd52`; warm TRX SHA-256: `51b202a54e46dc83c462c5c00928f3e786cea67f2391194c50770e00554176d6`.
- Official Unity focused `SimulationRuntimeAdmissionTests`: 241/241, run `c7e0b63a328341ef999ce879e454413e`, but `inputStable=false`; the before/after manifest identifies only `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` as changing during that run. This focused result is retained as non-stable evidence and is not relied on as a stable gate. Those user-owned ProjectSettings modifications were excluded from the candidate and left untouched by review.
- Official Unity ALL EditMode: PASS 3296/3296, zero failed/skipped/inconclusive, run `a94381b8e2484303b1a495dd1333c8c4`, `inputStable=true`, repository head/tree exactly candidate `120213d`/`f09c929`. XML SHA-256: `28945bd77e619b492c489d542d6bb35d10742b1588f65b7af372764f937d7dfe`; log SHA-256: `53ca082ec9c40c64e6529f25f2b8e6eabffbaffce157753b2104fad11fe83e81`.
- Official Unity Smoke: PASS 5/5, run `9af3578ae5f94a74a21e52a77d6ab425`, `inputStable=true`, same exact candidate head/tree. XML SHA-256: `8da7b76ade0d753a10013a029d1d1873655b2a45863d6d00f512e2bf7dd98da5`; log SHA-256: `5b0eac5a4086a12ff5ed917ba8efeb3178e673b906f63611fb57d6c364484459`.

## Limitations and integration constraints

This transfers only the selected daily-operation census admission comparison. Unity still owns census collection and shared mutation-epoch evidence. The selected runtime/application construction, P12 capture and detached restoration integration, public `DailyV1Application.TryStart`, independent .NET host, and process-detached T/T+1/T+2 continuation remain incomplete. This does not complete CORE-F, widen P12, close a Phase, or authorize canonical promotion.

## Independent review

The reviewer independently checked the exact candidate commit/tree, remote candidate and canonical refs, complete four-file delta against parent `9465782`, owner-thread and active-count behavior, fail-closed/atomic scope entry, changed tests, and exact-source validation evidence. Verdict: **PASS; no actionable findings.**