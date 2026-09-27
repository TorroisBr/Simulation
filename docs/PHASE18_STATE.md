# Phase 18 State — Intraday Temporal Execution v1

**Status:** PHASE 18 IN PROGRESS — P18-A, P18-B, and P18-C promoted; P18-D design revision in progress

**Current canonical base:** `codex/phase8/canonical` at `470667d37863384edadb3d93ef64d8004aff46a3`

**Architecture update:** `c285466c355103d3637ac165246591b72eb7bda0`

**Integration branch:** `codex/phase18/TimelineIntegrationPostP8E`
**P18-A refreshed source commit:** `4dea565a95a05ff03f61a9c18cfc163d47e509e1` (merges the unchanged P18-A implementation onto promoted P8-E canonical); later merge `b974276` synced canonical State/Roadmap-only updates at `77f3e1a`, with no P18 source changes.

**P18-A code promotion:** user-approved candidate
`985c56c40fc01dc6a4d392120e2d32151a558d03` was promoted to
`codex/phase18/canonical`; this State-only commit records the promotion.

The candidate now includes current P8 canonical at `470667d` through the
State-only merge `84d4977`. The upstream delta from `77f3e1a` changes only
`docs/PHASE8_STATE.md`; no P18 source or test code changed. The architecture
baseline remains `c285466`. The `77f3e1a` reference above is retained as the
historical base of the earlier review, not the current integration base.

**Final refreshed review:** PASS at candidate/State commit `a1463e8d46ed8e4526de0b0aa7a8c86d6ecf666e`.

**Current canonical-base refresh review:** PASS at exact candidate tip
`4c87ed9ec191cf04d235ed5cdf236c9e4eec93e1`. The refresh brings in only the
P8 closure State update and updates this State's base reference; P18-A source
and tests are unchanged. Architecture `c285466` and both current alignment
records remain applicable. Temporal identity/cardinality and the shared-
instance multi-participant tests remain compatible with P20. The prior P18-A
implementation review and validation therefore remain valid; no targeted
Unity revalidation was required.

**P18-C design revalidation:** PASS at exact refreshed design tip
`d090b54` against promoted P18 canonical `3d4fe829f4be41fc9e9bb11052a320c3eb00d94d`.
Independent review verified current P18-B transition/availability APIs, the
additive lifecycle-receipt requirement, post-advance timeline handoff, causal
and reconstruction inventories, and PersonId decision ownership with a
separate ActivityInstanceId. One-participant fixtures remain compatible with
P20; they do not establish one-to-one Activity/Actor cardinality. This was a
documentation/design review; no new code or test result is claimed.

## Phase status

| Work | Status | Evidence / prerequisite |
|---|---|---|
| P18-A — Logical Timeline Scheduler | PROMOTED | Original source commit `dd4d46ae1a4519d5ae07931965fbb6156692830e`; integrated as `1af92a1` plus collision-safe identity fix `350c6a0`, then refreshed against P8-E canonical in `4dea565`. The P18-A source diff is unchanged. Refreshed `LogicalTimelineTests` 18/18 (`Temp/ValidationResults/EditMode-20260926-212439-794ceaae956f47f7aebde4fade3d05bc.xml`), ALL EditMode 1717/1717 (`Temp/ValidationResults/EditMode-20260926-212600-77374b90b7ad4504b1c0408a27e4dbec.xml`), and complete Smoke 5/5 (`Temp/ValidationResults/EditMode-20260926-212641-e7c2128b74f54c5eb8e7872fd2700d85.xml`); `git diff --check d95b60d 4dea565` passed. Independent refreshed review at `a1463e8` and exact-base refresh review at `4c87ed9` passed, confirming the alignment/cardinality/reconstruction boundaries. |
| P18-B — Activity Lifecycle | PROMOTED | Implementation `97918cbbe4238a65a216b1a1f0ef84c70b4d080c` was promoted through integration commit `8f0cc4a6764abfc238e2497e4fad19487c6db82f` after independent review and user approval. A foreign timeline cannot dispatch lifecycle facts. Integration validation: ActivityLifecycle 16/16 (`EditMode-20260927-015240-ce080762386744cd9063484f033c5531.xml`), LogicalTimeline 19/19 (`EditMode-20260927-015352-fd3eb5a52fc9441ab7df66ba3b693dd0.xml`), ALL EditMode 1734/1734 (`EditMode-20260927-015417-28accf72f1ca42e79738072790b84001.xml`), official Smoke 5/5 (`EditMode-20260927-015453-960eaed7ce0443329ae1d54f33b4aacf.xml`), and `git diff --check` passed. |
| P18-C — Availability-Driven Decisions | PROMOTED | Source tip `ab05ecfe976e80badf6f509b8e9be25ff556ca23` (review-corrected State/promotion tip `7aa76268c49058fedb997392e676c6a29169c8b0`), based exactly on `39bd42e9e78f80c1e40b35b099e980ee8bc44a43`; architecture/alignment baseline `c285466c355103d3637ac165246591b72eb7bda0`. Independent implementation re-review passed after preserving distinct same-actor/same-tick causal receipts and retrying uncommitted execution with the same stable proposal ID. Focused ActorAvailabilityDecision 5/5 (`EditMode-20260927-031900-b609bdccc79143319b5e3ab54106cb84.xml`), ActivityLifecycle 17/17 (`EditMode-20260927-031918-7c40152d1d6545e29f127d8709a89542.xml`), ALL EditMode 1740/1740 (`EditMode-20260927-031934-f18b558a02a84818985c816b664106ab.xml`), and complete official Smoke 5/5 (`EditMode-20260927-032011-5c51d8ccde5a4fc8bec922bebcd88dd0.xml`) passed; `git diff --check` passed. User approved promotion; `codex/phase18/canonical` is promoted at `7aa7626`. |
| P18-D — Consumer Integration | DESIGN REVISION; IMPLEMENTATION BLOCKED | P18-A/B/C are promoted, and the selected P8/P11 capabilities are available. The current design candidate separates atomic boundary activation from its resumable daily barrier, but independent review requires a deterministic rule for selecting among same-actor requests at one instant and retaining later requests for a subsequent meaningful boundary. Implementation also requires an accepted additive P18-A returned-facts/subphase contract and the separate `SimulationRuntime` ownership window; P14 remains excluded. No blanket P20/P19 dependency is implied. |

## P18-C implementation ownership

Promoted source `ab05ecfe976e80badf6f509b8e9be25ff556ca23` is based exactly on
`39bd42e9e78f80c1e40b35b099e980ee8bc44a43` and owns the additive receipt log in
`ActivityLifecycle.cs`, new PersonId-keyed decision/coordinator contracts, and
their focused EditMode tests. The coordinator is composed with the same
`SimulationTimeline` and `ActivityLifecycleStore`; tests exercise committed
availability transitions and post-advance handoff without adding a second
scheduler. This bounded core does not own `SimulationRuntime.cs`, `CityRuntime.cs`,
or the P14 material-flow files. It does not claim that the legacy daily runtime
has migrated; a selected live consumer and daily compatibility adapter remain
P18-D integration work after P18-C promotion. The implementation review
requested corrections to same-tick causal request handling and executor retry
semantics; both were addressed and independently re-reviewed at the exact
promoted tip. Promotion was approved and fast-forwarded to
`codex/phase18/canonical` at `7aa76268c49058fedb997392e676c6a29169c8b0`.

## Architecture constraints carried forward

- The intraday/extensibility alignment is active: logical instants and stable
  causal ordering are required where the selected consumer needs them;
  extensibility-compatible identity/data shape is a review constraint now.
  Public Mod API and loader mechanics remain deferred to P19.
- The multi-participant alignment is active: ActivityInstanceId is not
  PersonId, NpcRuntimeId, or participant identity. Participant cardinality is
  not fixed by one-actor examples. P20 does not wait for P18-D or P19.
- P18-A's `(worldId, profileId, absoluteDay)` boundary identity and
  `(ownerId, workId, revision, occurrence)` work identity use injective,
  length-prefixed encoding. Pending boundary day/identity and the owner's
  idempotency/effect state are explicit reconstruction facts.
- P18-A does not integrate into `SimulationRuntime.AdvanceDay`; legacy daily
  execution remains authoritative until a selected P18-D consumer migrates.
- P18-A has no dependency on P8-E, P11, or P20. P18-D may depend on the
  specific P8-E/P11 consumer contracts it selects. P20 depends on relevant
  P18-A/B/C outputs but not P18-D or P19.

## Promotion impact and next work

P11 Actor Choice is canonical at closure tip `308e24d0744112e8f2b741521b8b3e4acb51ebbf`
(code `0cd4281`). P18-A has no P11 dependency, and P11's promotion changed no
P18-A source; the P18-A candidate and its validation remain applicable
(`UPSTREAM_IRRELEVANT`). P18-D may select the bounded actor-command consumer
alongside only the specific promoted spatial/travel capability its reviewed
scope requires.

P18-B's independently reviewed design is at `002ddc394339f924739f67fc4a8ff4420d3aecf3`;
its implementation is promoted. P18-C is now promoted at `7aa7626`, so any
remaining P18-D design/candidate must be revalidated against this canonical tip.
The current P18-D design revision addresses atomic activation versus its
resumable barrier, but independent review found an unresolved deterministic
selection rule for multiple same-actor requests at one instant; P18-D remains
design work and implementation is blocked on an accepted P18-A additive
returned-facts/subphase contract and a separate `SimulationRuntime` ownership
window. P20's entry and technical design are reviewed; its implementation
prerequisites A/B/C are now promoted, so recompute readiness from P20's exact
brief rather than waiting on P18-D or P19, neither of which is a blanket P20
dependency.
