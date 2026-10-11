# P12-G target-vector precheck mutation — independent review

Status: **VALIDATED_CANDIDATE**

Date: 2026-10-11

## Exact review target

- P12 canonical/base: `ab7c4b4e8347546c6bd8f4353d98353176b99807`
- Cumulative code candidate: `3afda34a8102214403cf633023938ae5d45f2d57`
- Code Git tree: `45437123894742748fb108d86fbca0c4bd7681f5`
- Code Assets tree: `d27f432cabffe4f0fd446ee56d60e198534df8d0`
- Evidence tip reviewed: `58ac5ac4`
- Evidence tip parent: code candidate `3afda34a8102214403cf633023938ae5d45f2d57`
- Reviewed test-file blob: `0ad044e5c5c44ea5de01fd4a010d31f8bb258c34`
- Candidate branch: `codex/phase12/P12GSuccessfulDirectiveTravelPartyParity` (local; no remote candidate ref)

## Review result

The independent reviewer inspected both cumulative test additions, the current P12-G contract, the exact validation XMLs and hashes, and the candidate file boundary. No actionable findings.

The successful directive/TravelParty case uses normal bootstrap and domain APIs, confirms the party is active when the due EscapePrison effect succeeds, compares included-owner projections and continuation roots across source/uninterrupted/restored runs, and checks no replay at the next boundary.

The target-vector case uses the existing private-candidate test observer at `TargetOwnerVectorCaptured`. It reserves an NPC identity with the staged composition's normal `RuntimeIdAllocator`, adds the NPC through `TryRegisterNpc`, and verifies later vector recapture rejects the changed candidate. The source session, completed-boundary token, owner projection, and health remain intact, and a subsequent valid restore succeeds. No production code or gameplay contract changed.

The working and indexed test-file blobs match the reviewed code blob. Exact code-range `git diff --check` passes. Validation evidence and hashes are recorded in [P12GTargetVectorPrecheckMutation/Final/VALIDATION.md](../validation/P12GTargetVectorPrecheckMutation/Final/VALIDATION.md): focused 1/1, `SimulationRuntimeAdmissionTests` 194/194, ALL EditMode 2868/2868, and official Smoke 5/5, all with zero failures, skips, and inconclusive tests. The independent reviewer recomputed each XML, compressed-log, and decompressed-log hash and matched the manifest.

The candidate contains no unrelated `ProjectSettings` edits or `.meta` files.

## Limits

This records one bounded private-target roster-mutation rejection and retry case, plus the named successful scheduled-action/active-TravelParty continuation-parity case. It does not establish full P12-G completion, exhaustive owner/provider inventory, complete writer/shared-epoch coverage, runtime-wide quiescence, universal rejection or atomicity coverage, universal no-replay/parity, capture eligibility, export/hydration, P12-A readiness, P13 readiness, or Phase closure. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.

No remote write or canonical promotion was performed by this review. The prior publication attempt was rejected by automatic security review because it would export project-derived validation artifacts to `https://github.com/TorroisBr/Simulation` without explicit approval for that exact payload.
