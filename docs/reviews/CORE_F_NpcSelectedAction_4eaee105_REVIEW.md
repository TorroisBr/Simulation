# CORE-F selected NPC action decision review

**Status:** `VALIDATED_CANDIDATE` for this bounded Workstream B slice only.
This is not approval of the complete CORE-F application/host package and does
not authorize Core canonical promotion.

## Exact identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Reviewed code commit: `4eaee1056d3cb7494e629f4edf40da0f8649690a`
- Reviewed code tree: `1107412aab82384f3e2f2abf0000d8ecbfbf12ee`
- Candidate base: `b12aeb21564c88e16fca12b3c85ee3bfd5058e94`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Exact-tip independent review: `PASS`

The candidate code commit is the exact code identity reviewed and validated.
The review record is documentation-only and must preserve the reviewed code
tree if committed after that code commit.

## Scope reviewed

The selected Daily-v1 action decision path evaluates admitted action and
status definitions from the immutable typed Core graph. It retains the
existing effective-utility precedence, required-status eligibility, ordinal
action ordering, selected enablement rules, weighted selection boundary, and
`npc-decision|RuntimeId|day` random-stream behavior. The Unity boundary maps the
selected typed action back to the exact bound authoring asset for existing
providers, actuation, and the P12 current-action identity slot.

Independent review initially identified incomplete fail-closed handling when
a selected graph action had no live asset binding. The final reviewed tip
requires total exact node-to-live-asset binding before exposing selected action
candidates. A destroyed-binding regression verifies that the entire inventory
is rejected before weighting or random consumption.

No P12 D/G wire or snapshot format was changed. The slice does not claim that
NPC runtime state, status lifecycle, inventory conversion, action providers,
detached restoration, or the shared host has moved into Core.

## Validation evidence

- Focused .NET Core tests: `5/5 PASS`.
- Focused Unity EditMode regression: `1/1 PASS` on Unity `6000.3.9f1`.
- `git diff --check`: `PASS`.
- Focused .NET TRX SHA-256: `AB9D62E4631566DB23B35A94CA1D40A142680E7E6478317BF0CAD5B2EE734E0C`.
- Focused Unity XML SHA-256: `4D27962FBE5F0CD22869861F81B6177CA06C1047C7D46ED86D52AF9D70AFCD65`.
- Focused Unity log SHA-256: `6B3592F07EB6E4F6F17DD5FEC47F2303D2AC8F03D99E4ED4C68D4BC58C342167`.

The original test outputs remain retained in the isolated candidate worktree.
They are not represented as a full CORE-F evidence package. Full Core, Unity,
Smoke, persistence, detached-host, and T/T+1/T+2 acceptance remain mandatory
for the complete package.

## Review verdict and integration constraints

The independent reviewer verified utility precedence, graph-node identity,
required-status filtering, deterministic ordering and weighted draw behavior,
provider invocation, selected enablement, exact asset identity, the restored
graph fail-closed edge, and unchanged P12 current-action compatibility. No
actionable finding remains in this bounded slice.

Because `SimulationRuntime.cs` and `NpcDecisionSystem.cs` are changed, any
later integration that changes their code must receive proportional focused
validation and a fresh exact-tip review. This review is not package-level
validation, not a canonical promotion record, and not a Phase closure record.
