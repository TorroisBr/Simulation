# P12-G Crime/Social runtime-ingress evidence — independent review

**Candidate:** `d859d0f36a34163a2f7dd46cbc77f7d859e46d73`
**Base:** P12 canonical `bc9ab6a27d5bb5c1e2de0987aa6377db6a96c37e`
**Assets tree:** `592bff43b5ef1497f72ea3acb8e13a349d63ca6d`
**Review:** exact-tip independent review PASS.

The focused test starts the authored Daily-v1 bootstrap, selects the installed
NPC whose default actions reference the authored Steal asset, restricts that
runtime's configured actions to Steal, binds same-City participants through
normal runtime Person admission, and calls `SimulationRuntime.TryAdvanceDay`.
It asserts the autonomous decision selected Steal; the TheftOutcome,
CrimeKnowledge, and SocialReaction owners each gained one row and local revision
1; the composite sink observed one active registered operation; the shared
mutation epoch increased; and the runtime returned to zero active operations
with registered-operation quiescence and roster census passing. `TryAdvanceDay`
owns the observed outer operation as `runtime.advance-day`.

The test disables the authored action's random failure only for this run and
restores the original value in `finally`. The configured-action list belongs to
the temporary bootstrap runtime, also destroyed in `finally`. No production
source, saved gameplay asset, operation ID, or Crime/Social semantics changed.

The candidate diff contains only this test, the source crosswalk update, and
its validation manifest/artifacts. All validation hashes match the retained
files: the focused class passed 12/12, ALL EditMode passed 2733/2733, official
Smoke passed 5/5, and `git diff --check` passed.

This closes only the selected Daily-v1 Crime/Social runtime-ingress evidence
item. It does not establish an exact shared-epoch increment, complete owner or
shared-epoch coverage, global quiescence, target-owner validation, restored
boundary admission, single-session publication, P12-G readiness, P12-A
readiness, P13 readiness, or Phase 12 closure. P12-G and P12-A remain
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.
