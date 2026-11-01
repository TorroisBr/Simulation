# Independent Exact-Tip Review — Selected Core Escape Dispatch Review Fix

## Verdict

**VALIDATED_CANDIDATE — PASS. No actionable findings.**

## Identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate: `6d7d5ffd96fd4152bbfd2daf6181eff4f8961c0c`
- Tree: `ca9c3da4fac5fd6e21572dc4a8bee1af4ed011f5`
- Immediate parent: `934104f5174fd7e6283cac29be928d9a446d03e8`
- Full review base: `1255ed9a7e9f44191bead09d78482a1072bc97e6`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Findings and review

The prior review gaps are resolved:

1. The test now compares the P12 census mutation epoch immediately before and after rebuilding the Unity compatibility projection from the exact Core payload. The action-slot revision is also required to remain unchanged.
2. With the Unity action projection absent, the test removes live operation admission and confirms dispatch rejects before projection reconstruction. Justice and NPC revisions, action-slot revision, event allocator revision, record-sequence revision, bounty, sentence identity, arrest state, and census mutation epoch remain unchanged.

The successful Escape path still checks exact payload identity, expected Justice/NPC/event/sequence writes, mutation-epoch count, and final owner census. The reviewer found no weakened assertions or production defect. The final candidate diff contains only `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs`; the latest commit changes only the test file.

Committed source identities verified by the reviewer:

- `SimulationRuntime.cs`: `ce98e045da5f958a816fed0c41dbab7c57baa9f0647fa07da29aa5ca468f8a9c`
- `SimulationRuntimeAdmissionTests.cs`: `7469984778fbfe6bbd8d31aa9d56cbd9ba43ecaa699a81ded4c142ca9a276ae9`

Validation evidence is recorded in [`VALIDATION.md`](VALIDATION.md). No test rerun was needed after commit because the committed file bytes match the tested stable input snapshot.

## Integration limits

This is a validated candidate checkpoint, not a canonical promotion or CORE-F completion. Core canonical remains at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. Continue with the remaining approved shared runtime, application, and detached-host work under their existing contracts.
