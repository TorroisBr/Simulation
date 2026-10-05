# P20-B joint civil travel — implementation progress

**Status:** P20-B promoted to `codex/phase20/canonical` at `142672b9eddd23013ff83b7b176979dd4cc9e3b6`; Phase 20 remains open.

**Implementation commit:** `de24dff356a54a0a4037e16c0ca5dc9ca379bc18`

**Code tree:** `62f8f3f3ad803e3f8eca832f7e39cff8196d5b85`

**Parent candidate:** `d14d235c86d8373e5f7e1c2ebf1f0e4507296222`

**P20 design handoff:** `d80ec06f48500a0ee80d6e05136ad50a6978a670`

**Architecture baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`

This additive commit retains the existing bounded P20-B implementation and adds causal identities/order for consent and `AbortAfterLeg`, derives public lifecycle state from P18, stages proposal and P20 owner installation in one timeline commit, and strengthens stale-start, current-passage, terminal-arrival, and clone-identity tests. It does not modify `SimulationRuntime`, bootstrap, or P12 admission.

## Focused validation

The initial focused run exposed test-fixture issues: unsealed timeline input windows and stale-write tests attempting initial-position registration after a Person already had a position. The fixtures were corrected to seal each advanced tick and to create valid intervening P8 mutations. The final exact code tree then passed:

| Suite | Result |
|---|---:|
| `P20JointCivilTravel` | 14/14 |
| `ActivityLifecycleTests` | 17/17 |
| `LogicalTimelineTests` | 38/38 |
| `P20SyntheticOperationTests` | 11/11 |
| `GeneralizedSpatialTravelTests` | 18/18 |
| `SpatialRoutePlanningTests` | 21/21 |
| `git diff --check` | PASS |

Raw Unity XML and log files for the passing runs are archived in `docs/validation/P20B/P20B-focused-20261004-e93731c.zip` (SHA-256 `7F2C43F12B0C80DF216B3B1923A3A00060785BB42B8C51675FA3828FD1181675`). They validate code tree `92ba3b27c65d18bb0c5786b8e99860116c94d22b` only; the later isolated time-binding and reconstruction source changes below are not covered by those runs.

## Remaining boundary

P20-B still requires the selected `UnityBootstrap-Daily-v1` fail-closed admission/inventory hook and its negative test before a complete P20 integration candidate can be claimed. That hook shares the bootstrap/admission hotspot being handled by P10; this progress commit deliberately leaves it untouched pending that integration boundary. ALL EditMode and official Smoke have therefore not been run for this incomplete integration candidate. No P20-B independent exact-tip implementation review or canonical promotion is claimed.

P20-B remains limited to the reviewed two-Person, one supported civil-segment proof and its P18/P8 ownership boundaries. This commit does not add Party/Group semantics, automatic progression, save/load, or broader P12 readiness.

## Follow-up: proposal-time binding and P20 reconstruction facts

The exact-tip implementation review identified three P20-owned gaps that can be handled without the P10 bootstrap/admission hotspot. This follow-up changes only the P20 owner and its formation/integration fixtures:

- Proposal creation now fixes a `ProposedStart` logical tick. Each recorded assent retains that same tick, consent after the target is no longer future is rejected, and scheduling has no later start argument that could substitute another time.
- `SnapshotOwnerState` enumerates complete P20 proposal/assent/abort facts in stable activity order. `TryRestoreOwnerState` restores them only against matching restored P18 lifecycle identity/revision/participant/start facts and stages replacement state atomically.
- Reconstruction fixtures cover independent assents and proposed time, plus active `AbortAfterLeg` identity, tick, and order after P18 lifecycle cloning.

**Validation:** not run in this follow-up because the P10 Unity validation slot is active. `git diff --check` passes. The follow-up is a source/test slice only; it is not independently reviewed, does not complete P20-B, and does not claim P12 admission coverage. Revalidate the changed fixtures when the serialized Unity slot is available.

## Continuation: bounded P20 reconstruction consistency

The isolated continuation tightens P20 owner restoration without changing the P18, P8, runtime, bootstrap, admission, or P14 hotspots:

- Restored assent events must have contiguous coordination order, nondecreasing accepted logical instants, and no instant later than the restored timeline.
- A pending `AbortAfterLeg` must follow every assent in coordination order, must not be future-dated, and an interrupted P18 instance must retain the corresponding abort intent.
- Formation fixtures cover restoring a partially formed proposal and continuing with the remaining independent assent, preserving a decline as `NotFormed`, and rejecting a P20 snapshot when the matching P18 lifecycle revision has changed.
- Integration fixtures reject a malformed abort fact whose order overlaps assent and an abort timestamp later than the restored timeline.

The runtime-owned P18 lifecycle/timeline binding and the `UnityBootstrap-Daily-v1` negative-admission test remain explicit integration work. They require the reserved P10/P14/P12 hotspot boundary and are not implemented here.

This restore path matches the P18 instance identity, creation identity, revision, lifecycle state, planned start, and participants through the current P18 snapshot API. Exact reconstruction of both open-ended P18 commitments and the P18/timeline pending-start index remains owned by the combined lifecycle/timeline integration and must be asserted there; this P20-only slice does not claim that broader reconstruction proof.

**Validation:** Unity was not run because P10 owns the serialized validation slot. This continuation is source/test coverage only, is not independently reviewed, and does not complete the P20-B integration candidate or claim P12 admission coverage. Run the focused P20 suite and required affected regressions on the combined integration tree after the hotspot window opens.

## Validated candidate continuation — 2026-10-04

**Candidate branch:** `codex/phase20/P20BCoreContinuation` at
`de24dff356a54a0a4037e16c0ca5dc9ca379bc18` (code tree
`62f8f3f3ad803e3f8eca832f7e39cff8196d5b85`), based on the existing isolated
P20-B continuation candidate at `d14d235c86d8373e5f7e1c2ebf1f0e4507296222`.
The P20-A canonical fixture composition remains separate and unchanged.

The selected `UnityBootstrap-Daily-v1` census now registers
`p12f.p20-joint-civil-travel` as an explicitly empty owner section. A P20 owner
with proposal or later state makes daily runtime census initialization fail
closed; an absent or empty P20 owner remains admitted. Focused tests cover both
cases. This does not expand daily-profile state coverage or implement save/load.

Unity validation ran sequentially on the code tree above with Unity
`6000.3.9f1`; no Unity process remained after each run:

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P20JointCivilTravelIntegrationTests` | 8/8 | `9558F095D838321C84A7A691B67D485A93CED3935211E8AFEABB5FAA0790EF12` | `EE0D11F0456426AF1790A3ADCAE02219A99A39DBEFE6772B9FB4BDD1D7D1BEC5` |
| ALL EditMode | 2273/2273 | `14384311AF626B9692E871EACFCEBFA275949DA48B333FE0D373D155D8D0ECFA` | `0114C7BC79AAB60984E1CEBFF6001764186A93C3AE25BA1E88B7B6F6BD82A705` |
| Official `-testFilter Smoke` | 5/5 | `FB03F93081542246D9C63E60C864DA9DFA5DB05189D635A2F7D036AED754AEEE` | `A1569A88B9F6E46229264556C31FDE5887F6B165B54278E34BAD0FCEC501E600` |
| `git diff --check` | PASS | — | — |

The XML and log files are preserved in
`docs/validation/P20B/P20B-validation-20261004.zip` (SHA-256
`8316130BF87ACDA926822C6E4060B1CC9E83B37AADE5854EF3A960EA6C7BA56A`). The
archive contains all six files named by the gates above. The first focused run
exposed that nonempty P20 state is rejected during runtime census initialization;
the assertion was corrected to the fail-closed construction behavior, then the
focused, ALL EditMode, and Smoke runs passed on the final code tree.

No ProjectSettings changes, unrelated `.meta` files, P20-A composition changes,
or `TesteSimulacao` edits are included. At the time this historical entry was written, independent review and promotion remained pending. The final review and promotion outcome appears below; Phase 20 closure was not claimed.

## Independent review and promotion outcome — 2026-10-04

Independent exact-tip review passed with no actionable findings for code `de24dff356a54a0a4037e16c0ca5dc9ca379bc18` / tree `62f8f3f3ad803e3f8eca832f7e39cff8196d5b85`; see `docs/design/PHASE20_P20B_IMPLEMENTATION_REVIEW.md`. The docs/evidence tip `142672b9eddd23013ff83b7b176979dd4cc9e3b6` contains no later `Assets` changes. The approved fast-forward advanced P20 canonical from `7a81cc0ecbc511dd36c248ec62c7b20f7e477f53` to `142672b9eddd23013ff83b7b176979dd4cc9e3b6`. P20-B remains bounded to explicitly-empty Daily census admission; Phase 20 remains open.
