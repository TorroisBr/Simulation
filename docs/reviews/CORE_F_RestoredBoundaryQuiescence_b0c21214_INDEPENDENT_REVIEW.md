# Independent review — CORE-F restored-boundary quiescence

**Verdict: PASS**

**Reviewed implementation:** `b0c212143a293a628341de3b41b7507724fd1772`
**Reviewed tree:** `1c456ef3188e1238671b449c488041d57ea81531`
**Base:** `a8070ad79748ffe2c63e8a101fdf9e3f239b75a0`
**Reviewer:** independent Codex review agent `/root/review_core_census_readiness`
**Review completed:** 2026-11-03

## Finding disposition

The prior review finding is closed. A restored target now passes a Core-owned
quiescence assessment before its mutation guard can bind. The assessment
rejects an unready or faulted selected-runtime census protocol, a non-owner
thread, an unsealed operation inventory, a replacement reservation, or any
active Core operation scope. This catches the failed `NpcMembership`
admission case where a host context exists with no registered Core scope.

The regression follows the Core-aware restore path under the exact application
replacement reservation. It injects a failed, unscoped membership context and
verifies rejection maps to `TargetAdmissionFailed`, the mutation guard remains
unbound, no restored candidate is returned, the original session remains
active, and its capture token remains valid. The reviewer found no other
actionable issue or scope expansion.

## Validation evidence inspected

The reviewer independently checked the official manifests and source-file
hashes recorded in
[`RestoredBoundaryQuiescence_20261103.md`](../validation/CORE_F_CoreOwnedRuntimeApplication/RestoredBoundaryQuiescence_20261103.md):

- CoreValidation cold/warm: 1002/1002 each, PASS.
- Focused Unity regression: 1/1, PASS.
- Full Unity EditMode: 3298/3298, PASS.
- Official Smoke: 5/5, PASS.
- `git diff --check`: PASS.

All Unity input snapshots were stable, and current source hashes matched the
captured inputs. The initial 3297/3298 run used the legacy restore overload and
did not exercise the Core finalizer; its disposition is documented in the
validation record and it is not counted as a passing run.

This review applies to the bounded candidate change only. It does not approve
CORE-F completion or canonical promotion.
