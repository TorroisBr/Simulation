# Core-owned selected census advance gate — validation

Status: **validated candidate slice; not Core canonical; CORE-F remains incomplete**.

## Identity and scope

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-try-start-transaction
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Exact candidate tip: ce467895f9241b4f6e7b1958b94c390892fb1596
- Exact candidate tree: da90871b22df4ae8c1f967cc431bba40ee5f8c4e
- Review base: 2ed093cf766ba56f7c1a540cdb9bb959a6e14fc2

This slice moves selected runtime census readiness, the shared operation count, and the protocol fault latch into DailyV1ExecutionBoundaryState. The Core advance gate opens the existing DailyAdvance operation while holding the same Core operation lock that validates readiness and fault state. The Unity census protocol reports its sealed binding into Core; it remains responsible for the full owner/provider inventory and live census values.

The final cumulative diff changes only:

- Assets/_Project/Scripts/ContinuationCensusProtocol.cs
- Assets/_Project/Scripts/SimulationRuntime.cs
- Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs

The two pre-existing ProjectSettings modifications remained outside the candidate commits.

The candidate history includes a follow-up correction commit restoring existing census regressions after an intermediate local test edit. The final cumulative diff retains those tests and adds the new readiness/fault regression; no existing assertion or owner-family coverage was removed.

## Validation

All listed official runs bind to candidate ce467895 and tree da90871b.

- Official CoreValidation: **1,002/1,002 cold and 1,002/1,002 warm PASS**, run 930c9c36accd4080ba3ea505d2d3c5d2. Protected manifest SHA-256: ca6bcabf2cdc5bcb891089f610bb9b0b11642280858c83681dbaee70d3c35b11. Stable input digest: d7470473ccd4c322096a45fd4bcd887c476390326abb09340d9e63a4991a0fed.
- Full official Unity EditMode: **3,297/3,297 PASS**, run 63984736de8c469e951eb5b03cc802d9. Protected manifest SHA-256: 6c71366286b53ffe1e6b22f29ece5da442222394e62194905f669bbffb50146f.
- Separate official Smoke filter: **5/5 PASS**, run def9f45dcfb64cb8bb9f09ba0a7b2726. Protected manifest SHA-256: 4cde80f001b7632255473bb9ccaebff54ecbfc8af3df0dede42841a7ad1fd196.
- git diff --check from the review base through candidate tip: **PASS**.

The Unity runs verified stable before/after source identity. The CoreValidation runner noted the worktree contains a Unity Library; this run does not claim Library independence. Original reports, logs, and owner-only run manifests remain in protected local retention; this record does not claim a public normalized evidence archive.

## Preserved contracts and boundary

The 89-family owner registrations, exact owner rebinding and census capture, mixed-roster compatibility fallback, P12 owner identities, revisions, shared mutation epoch, reservation behavior, capture tokens, and restore paths are unchanged. The former host operation-count callback read the same shared Core operation count, so its removal does not remove an independent counter.

This slice does not complete DailyV1Application.TryStart, shared detached restoration, the executable .NET host, or T/T+1/T+2 continuation. No CORE-F closure or canonical promotion is implied.