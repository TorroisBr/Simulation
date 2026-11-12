# Exact-Tip Review — CORE-F Selected NPC Membership Graph Transaction

**Verdict:** VALIDATED_CANDIDATE — PASS for the six-file change in this commit. No actionable findings.

## Candidate identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Candidate SHA: bdfc406056bee8836b0d815349aa6a4cf4bcf9b4
- Direct parent/base SHA: 68699734ba26957987a438e8fa77a99c00cb2b32
- Core canonical observed during review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Review scope: the six files changed by the candidate commit only. This does not approve earlier branch commits, promote code, or claim completion of CORE-F.

## Files reviewed

- Assets/_Project/Scripts/SimulationRuntime.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs
- tests/Simulation.Core.Tests/DailyV1ActorTurnExecutionStageTests.cs
- docs/validation/CORE_F_SelectedNpcMembershipGraphTransaction_20261112/VALIDATION.md
- docs/validation/CORE_F_SelectedNpcMembershipGraphTransaction_20261112/UnityFull/coref-selected-npc-membership-graph-68699734.zip
- docs/validation/CORE_F_SelectedNpcMembershipGraphTransaction_20261112/UnityFull/coref-selected-npc-membership-graph-68699734.manifest.json

## Review result

Core prepares either a graph-only change when no actor roster is bound, or a graph-plus-roster change when one is present. Graph-plus-roster publication validates and replaces both snapshots together; both forms retain a compare-and-swap rollback path. Existing actor-turn roots are rebound with graph replacement when bound.

Unity registration publishes Core membership before selected-owner-provider binding and runtime-identity census registration, then updates the host registry and roster. Failures in the later provider or identity steps request rollback and undo any provider binding. Unregistration publishes the Core removal before clearing city presence and removing the host registry entry; failed city-presence clearing requests rollback. Unselected NPC compatibility handling and retained runtime-identity census behavior remain unchanged.

The added Core regression covers graph-only addition and removal, deferred publication, absence of synthetic roster creation, and rollback. Existing graph-plus-roster atomicity coverage remains in place. No defect was found in the reviewed paths.

git diff --check passed for the parent-to-candidate diff. No tests were run during this independent review.

## Recorded validation

The candidate validation record reports:

- CoreValidation cold and warm: 1,103/1,103 PASS each; run d690e7758c1b4b3d834b7e4987df3533; recorded working-tree input digest d09257a6ac8622c11b4edc3ab2b83e94281add161fa6d057af3a0994739c2588.
- Focused bootstrap/composition EditMode: 41/41 PASS; run 73d5132394064d9a8a45f9499aca9438.
- Full Unity EditMode: 3,318/3,318 PASS; run e0325aa28af94cc6afdddcc450ded71f.
- Filtered EditMode Smoke: 5/5 PASS; run 7d774117167148389d7bd9184c8f1938. The record says all five filtered identities occur in the full EditMode report.

The Unity archive is 175,583 bytes. Its actual SHA-256 matches the validation record and adjacent manifest: 9c0aad5fa693b4a36f45747f316e88a5d75146fe745dd22bdbed9eabde66b84e. The manifest has one run and records parent 68699734ba26957987a438e8fa77a99c00cb2b32 as HEAD. Its source hashes match the reviewed SimulationRuntime.cs and DailyV1ExecutionBoundaryState.cs files. The recorded ProjectSettings/ProjectSettings.asset input hash also matches the current file.

## Evidence limits and integration constraints

- Core TRX/logs and the original Unity XML/logs remain in protected private E: evidence. The Core test counts and input digest were taken from the validation record; the Core raw evidence was not independently opened or repackaged.
- The graph-only regression is Core-level. Recorded Unity integration coverage verifies the graph-plus-roster selected registration/unregistration path; it does not separately demonstrate graph-only Unity host composition.
- The review worktree contained unrelated local changes and untracked/private evidence. They were preserved. The Unity manifest includes the local ProjectSettings input hash noted above; this review did not clean or alter the worktree.
- No canonical promotion was performed or implied.