# CORE-F initial-genesis publication reentrancy — exact-tip review

**Status:** VALIDATED_CANDIDATE for this bounded correction only. This record does not accept or promote the complete CORE-F application/host package.

## Exact identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Prior candidate/base: faf8b03c5f7e1d887c6b1945108430130c3dcc61
- Reviewed candidate: bec3460e15370b6fcdf3afe102ce760dbb983799
- Reviewed tree: ac691e79dd486b07a1de65e4ba8e7b70ea03a5cb
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate remote was synchronized at the reviewed commit.

The complete candidate delta from its parent is limited to:

- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs

## Review result

Independent exact-tip review: **PASS — no actionable findings**.

The prior review finding was that a callback during initial-genesis preparation could reenter TryPublishInitial before the prepared candidate was stored and publish a different session. The correction rejects publication when initial preparation has already been attempted but no exact prepared candidate is available. It preserves publication of the prepared candidate after successful preparation and the established direct-publication path when preparation has not started.

The regression exercises reentrant publication from the preparation callback, verifies the unrelated session is not published, and verifies the exact prepared candidate can publish after preparation completes. Existing assertions were not weakened. Review was against the exact candidate delta and tree above; the reviewer made no source edits.

## Validation references

Exact-tip validation is recorded in [VALIDATION.md](../validation/CORE_F_CoreOwnedRuntimeApplication/Evidence_bec3460/VALIDATION.md). The full cumulative CORE-F package remains incomplete and unpromoted. This correction does not establish detached-host acceptance, complete P12 restoration, or CORE-F completion.
