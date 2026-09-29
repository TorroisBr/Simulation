# Phase 18 State — Intraday Temporal Execution v1

**Status:** PHASE 18 IN PROGRESS — P18-A/B/C promoted, including the additive extension at `1dd0479` and the external-input/deferral adapter at integration tip `b75c5b8` (code `a535441`); P20-A is promoted (code `22df7b3`, P20 State tip `7a81cc0`). P18-D consumes the promoted P11 temporal capture contract. Its exact technical design passed independent review; the sale-owner receipt/prepared-install and per-runtime serialized advance prerequisites were implemented, validated, and promoted at `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`. P18-D runtime code is `3ddf8476842ad24b9234ccaa65a530722ead8eb4`; test-only consumer replay correction `0887d18` is pushed and has focused, full EditMode, Smoke, LongRun, and diff-check evidence. Exact-tip independent review is pending. The candidate composes the existing SellGoods action with intraday execution, accepts trusted WorldCommand choices at the current or next unsealed tick, and explicitly excludes P14 local material-flow cities pending a temporal owner adapter. Phase 18 remains open; this candidate is not promoted or closed.

**Canonical baselines:** P8 `codex/phase8/canonical` at `470667d37863384edadb3d93ef64d8004aff46a3`; P18 `codex/phase18/canonical` at `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`. The P18-D candidate also merges P14 `codex/phase14/canonical` at `4caecbbfb0464c965811402b3c11d8717605114a`.

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
| P18-A — Additive Boundary/Continuation | PROMOTED at integration tip `1dd0479` | Accepted contract `2175bf2` (acceptance `9de70ae`); corrected candidate `f1bfe818565c3fca81b373d1bc9a70a16f4eda10`; exact-tip independent review PASS; LogicalTimeline 35/35, ALL EditMode 1756/1756, official Smoke 5/5, and diff-check PASS (validation results worker-reported). |
| P18-B — Activity Lifecycle | PROMOTED | Implementation `97918cbbe4238a65a216b1a1f0ef84c70b4d080c` was promoted through integration commit `8f0cc4a6764abfc238e2497e4fad19487c6db82f` after independent review and user approval. A foreign timeline cannot dispatch lifecycle facts. Integration validation: ActivityLifecycle 16/16 (`EditMode-20260927-015240-ce080762386744cd9063484f033c5531.xml`), LogicalTimeline 19/19 (`EditMode-20260927-015352-fd3eb5a52fc9441ab7df66ba3b693dd0.xml`), ALL EditMode 1734/1734 (`EditMode-20260927-015417-28accf72f1ca42e79738072790b84001.xml`), official Smoke 5/5 (`EditMode-20260927-015453-960eaed7ce0443329ae1d54f33b4aacf.xml`), and `git diff --check` passed. |
| P18-C — Availability-Driven Decisions | PROMOTED | Source tip `ab05ecfe976e80badf6f509b8e9be25ff556ca23` (review-corrected State/promotion tip `7aa76268c49058fedb997392e676c6a29169c8b0`), based exactly on `39bd42e9e78f80c1e40b35b099e980ee8bc44a43`; architecture/alignment baseline `c285466c355103d3637ac165246591b72eb7bda0`. Independent implementation re-review passed after preserving distinct same-actor/same-tick causal receipts and retrying uncommitted execution with the same stable proposal ID. Focused ActorAvailabilityDecision 5/5 (`EditMode-20260927-031900-b609bdccc79143319b5e3ab54106cb84.xml`), ActivityLifecycle 17/17 (`EditMode-20260927-031918-7c40152d1d6545e29f127d8709a89542.xml`), ALL EditMode 1740/1740 (`EditMode-20260927-031934-f18b558a02a84818985c816b664106ab.xml`), and complete official Smoke 5/5 (`EditMode-20260927-032011-5c51d8ccde5a4fc8bec922bebcd88dd0.xml`) passed; `git diff --check` passed. User approved promotion; `codex/phase18/canonical` is promoted at `7aa7626`. |
| P18-C — External-Input/Deferral Adapter | PROMOTED at integration tip `b75c5b8` (code `a535441`) | Code integration `a535441` was assembled against P18 State tip `eabc1c2` (promoted extension code `1dd0479`) and promoted to `codex/phase18/canonical` at `b75c5b8`. Exact-tip independent implementation review passed. Temporal identity/cardinality, focused domain suites, ALL EditMode 1794/1794, complete Smoke 5/5, and diff-check passed. The promoted candidate State still carried stale pending-gate wording; this docs-only correction reconciles it. |
| P18-D prerequisite — Economy sale receipt/prepared install | PROMOTED at `9e790c5` | Stable proposal/fingerprint identity, first-execution current-truth snapshot, committed replay, retry only after proven-no-install, expected-revision preflight, and owner-local prepared installation across inventory, market, and account state. Receipt retention is scoped to the current `SimulationRuntime` lifetime; no restart/save/crash-recovery guarantee is claimed. |
| P18-D prerequisite — serialized runtime advance window | PROMOTED at `9e790c5` | A per-runtime, single-writer, non-reentrant lease covers `TryAdvanceDay` and the outer `TryAdvanceDays` call. It is not a general thread-safety promise. The P18-D consumer must keep its full chronological advance, boundary subphases, and successful post-advance P18-C handoff inside this lease. |
| P18-D — Consumer Integration | IMPLEMENTED CANDIDATE — VALIDATION PASS; EXACT-TIP REVIEW PENDING | Technical design candidate `9ed6d90455cc793244ee7207adb62960e45a9972` passed independent exact-tip review against P18 canonical `85f1f21`, P14 State `4caecbb` / code `c44904b`, P20 State `7a81cc0` / P20-A integration `1dcf67a` (implementation `22df7b3`), P11 canonical `308e24d`, architecture `c285466`, intraday-extensibility alignment `4b6dd1d`, and multi-participant alignment `c285466`. Implementation began with `90e3359`, merged the current P14 canonical at `a2a8edd`, and added runtime corrections at `3ddf8476842ad24b9234ccaa65a530722ead8eb4`; test-only consumer replay correction is `0887d18`, with review-accuracy comment correction `6a4d971`. It binds committed P11 receipts after successful advance, observes later triggers/deferrals, admits the exact allocated request, integrates Local SellGoods, and runs the selected daily owners chronologically under the promoted runtime lease while preserving P11 semantic validation. P14 local daily material-flow cities are rejected before any boundary mutation until a separately reviewed temporal owner adapter exists. The replacement replay regression completes the sale and normal P11/P18-C terminal reconciliation, activates a merchant plan, then rewinds only in-memory P18-D consumer execution flags and re-enters the resume path. Existing terminal records remain present. The test checks receipt identity, no repeated sale effects, and idempotent terminal reconciliation; it does not simulate a pre-terminal interruption or runtime restart. Exact-tip implementation review is pending; P14 remains excluded and no blanket P20/P19 dependency is implied. |

### P18-D runtime validation — code `3ddf847` (before consumer-replay review correction)

The pushed feature branch `codex/phase18/P18DConsumerRuntime` is based on the
P18-D consumer implementation `90e3359` and merges `codex/phase14/canonical`
at `4caecbb` in `a2a8edd`. The final correction at `3ddf847` routes the normal
trusted ActorActionChoice WorldCommand into the intraday timeline at the
current instant if open, otherwise the earliest next unsealed tick. Its
regression verifies the queued timeline input is committed to ActorChoice
state only when its target instant advances. The same correction rejects a
P14 local material-flow composition before any daily boundary mutation. The
initial replay regression exercised the MerchantSystem receipt owner directly;
independent review correctly found that insufficient for the P18-D consumer
obligation. The test-only follow-up below replaces that assertion with a
consumer-resume regression.

Validation on code `3ddf847` passed: P18DConsumerIntegration 9/9,
LocalDailyMaterialFlow 13/13, ActorActionChoiceCommand 6/6,
MerchantLiquidity 11/11, EconomyTransaction 45/45,
P18DDailyBoundaryStepProvider 6/6, P18DDailyBoundaryOwner 2/2,
SimulationRuntimeOrchestration 12/12, LogicalTimeline 38/38,
ALL EditMode 1921/1921, complete official Smoke 5/5, and
SimulationRuntimeLongRun 7/7. Result XML files are under
`Temp/ValidationResults`; all reported zero failed, inconclusive, or skipped
tests.

| Suite | Result | XML |
|---|---:|---|
| P18DConsumerIntegration | 9/9 | `EditMode-20260929-031923-4a164bbebe2348b08adb44ad52865aad.xml` |
| LocalDailyMaterialFlow | 13/13 | `EditMode-20260929-032004-b8e4521f9048490f88994af9df7c8eb2.xml` |
| ActorActionChoiceCommand | 6/6 | `EditMode-20260929-032015-8973a77f6c2748fa95ed1b3d75760e93.xml` |
| MerchantLiquidity | 11/11 | `EditMode-20260929-032025-301846aaaba248ed8ee870b5bc6559e7.xml` |
| EconomyTransaction | 45/45 | `EditMode-20260929-032037-ded2af517d9d4a9893b9555dbc8429cc.xml` |
| P18DDailyBoundaryStepProvider | 6/6 | `EditMode-20260929-032047-227ef607f0b34ea78331879777872acb.xml` |
| P18DDailyBoundaryOwner | 2/2 | `EditMode-20260929-032058-b288d4f28c4a41cca6a4a464f09952bc.xml` |
| SimulationRuntimeOrchestration | 12/12 | `EditMode-20260929-032109-173f30dfa60f46699f4897c29c3be2ec.xml` |
| LogicalTimeline | 38/38 | `EditMode-20260929-032120-885687b0f5b24387a9b44b6043767cda.xml` |
| ALL EditMode | 1921/1921 | `EditMode-20260929-032147-85498e9d45604bdbad5543090c18a944.xml` |
| Complete official Smoke | 5/5 | `EditMode-20260929-032223-12e9753102344e5caa1042fa04f14442.xml` |
| SimulationRuntimeLongRun | 7/7 | `EditMode-20260929-032250-53d1fd2cf59f4c3e8110d6b77c7f050d.xml` |

`git diff --check` passed for runtime code `3ddf847`. These runtime and domain
results remain applicable because `0887d18` changes only the test. They do not
authorize canonical promotion or Phase 18 closure.

### P18-D consumer replay review correction — `0887d18`

The independent review of `3ddf847` identified that direct replay through
`MerchantSystem.TryExecuteKeyedLocalMarketSale` did not test the consumer's
resume or terminal-reconciliation path. Commit `0887d18` replaces that test
with an intraday ActorChoice execution that commits a sale and completes normal
P11/P18-C terminal reconciliation. With an active merchant plan, the test then
rewinds only the in-memory P18-D consumer execution flags and re-enters
`TryResumeP18DActorChoice`; the committed owner receipt and first-pass terminal
records remain present. It verifies the same receipt instance, no additional
inventory/market/account changes, no duplicate trade log, and idempotent
terminal reconciliation. This tests post-terminal consumer replay within the
current runtime lifetime; it does not simulate a pre-terminal interruption or
make a restart or durable crash-recovery claim.

Exact candidate validation on `0887d18` passed P18DConsumerIntegration 9/9,
ALL EditMode 1921/1921, complete official Smoke 5/5, and
SimulationRuntimeLongRun 7/7. `git diff --check` passed.

### P18-D replay wording correction — `6a4d971`

The exact-tip review found that the test comment and State overstated the
scenario as an interruption before terminal reconciliation. The test first
completes the normal P11/P18-C terminal path, then rewinds only P18-D's
in-memory execution flags. Commit `6a4d971` corrects the comment; the State now
explicitly describes this as post-terminal consumer replay and makes no claim
that the test covers a pre-terminal interruption or runtime restart. The
correction does not change runtime behavior or test assertions.

Validation on code/test tip `6a4d971` passed P18DConsumerIntegration 9/9
(`EditMode-20260929-035716-18e523f59429490392524da2a891f26d.xml`), ALL EditMode
1921/1921 (`EditMode-20260929-035741-71c67f1e10c646fd82c93fd9e794e23c.xml`),
complete official Smoke 5/5
(`EditMode-20260929-035819-f7ae6c34e5f84d1fa01f36b48662aa1b.xml`), and
SimulationRuntimeLongRun 7/7
(`EditMode-20260929-035844-680167d74dc446a79ec91df751bbc580.xml`).
`git diff --check` passed. Exact-tip independent review is pending.

| Suite | Result | XML |
|---|---:|---|
| P18DConsumerIntegration | 9/9 | `EditMode-20260929-034122-810b5db82eb74bf7ba526ac1ac8c32e4.xml` |
| ALL EditMode | 1921/1921 | `EditMode-20260929-034205-41ced890b52d42b79ffc642d3d91d3e3.xml` |
| Complete official Smoke | 5/5 | `EditMode-20260929-034301-a13b2c0eba6646b8a46b483ba24af18c.xml` |
| SimulationRuntimeLongRun | 7/7 | `EditMode-20260929-034339-8a23805770824ccc9e39ae49787dc7b5.xml` |

These results do not authorize canonical promotion or Phase 18 closure.
Independent exact-tip implementation review remains required.

### P18-D prerequisite integration — promoted `9e790c5`

The reviewed code integration was based on P18 canonical `ba8076c3bc2c8c354a8755e6efaca30bfeab7bf7`, independently reviewed at exact tip `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`, and approved for canonical promotion. It combines the bounded economy sale-owner receipt/prepared-install capability with the per-runtime serialized advance lease. The runtime lease protects the existing advance entrypoints and is the ownership seam for the forthcoming P18-D chronological consumer; it does not itself deliver that consumer.

On the exact candidate tip, EconomyTransactionTests passed 45/45, ALL EditMode passed 1808/1808 (`EditMode-20260928-025030-63d4104987f8414ab0d311e21bb58220.xml`), official complete Smoke passed 5/5 (`EditMode-20260928-025133-46136d27c60f440aa256aaf0aa1925b3.xml`), and `SimulationRuntimeLongRunTests` passed 7/7 (`EditMode-20260928-025209-0da1288bb5d046e1a0b7cff86d01754c.xml`). `git diff --check` passed. The sale regression verifies an item-definition price change does not replace the market's cached sale price before the price-refresh operation. These results promote only the prerequisites; they do not claim a P18-D consumer migration or Phase 18 closure.

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
P18-D integration work. The serialized `SimulationRuntime` ownership window
and economy operation-receipt contract were promoted together at `9e790c5`;
the consumer migration must reuse them rather than introduce another receipt
owner or advance lock.
The implementation review
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

**P18-C external-input/deferral adapter:** independent design review PASS at
exact tip `358c65c85e1eafdd91ef4a6553ba3b0a8c4af249`; the implementation is
assembled and validated at code integration `a535441` against promoted P18-A
extension `1dd0479`, then promoted to `codex/phase18/canonical` at integration
tip `b75c5b8`. Exact-tip implementation review and post-extension validation
passed. The bounded design specifies
a P18-C request-state owner,
C-owned sequence allocation separate from lifecycle source sequences,
P11-owned exact temporal capture linked to P18-A's accepted reference, exact
P11 temporal boundary records, retained P11 Pending status during C-owned
deferral, and a distinct P18-D economy operation receipt with immutable request
correlation/current-truth snapshot.

The adapter code candidate is `652e16e` on integration branch
`codex/phase18/P18CExternalInputDeferralIntegration`, based on P18 canonical
`311baa9`. It retains the approved P11 actor-choice authority boundary. The
implementation review at `4825b41` and source-provenance architecture review
passed; the request-ID test fix at `dd90f37` and integration fixture correction
at `652e16e` each passed independent review. Integration review at `ff2631f`
passed code/composition and requested only that this State point to the current
candidate and retain validation evidence. This record corrects those two items.
Follow-up integration review at `6b3bec5` passed the corrected State and
retained XML evidence, confirming local/tracking/remote synchronization and a
clean range diff. At that pre-extension stage, this candidate had not been
promoted; the preserved implementation was subsequently rebuilt and
revalidated against the promoted P18-A extension below.

After canonical advanced from `311baa9` to `99cac77`, the adapter integration
was refreshed at `e2af495` and recorded at `e4ff318`; exact-tip review and the
focused/full validation listed below passed on that pre-extension tree. Those
results remain historical evidence for the preserved candidate, but do not
validate the current post-extension integration. Following P18-A extension
promotion (`1dd0479`, State tip `eabc1c2`), the integration candidate was
merged into branch `codex/phase18/P18CPostExtensionIntegration`. Revalidation
of temporal identity/cardinality, affected suites, and the full regression
gates passed on code integration `a535441`; docs-only status follow-ups are
`fe9a479` and `38999cb`. The independent exact-tip implementation review
passed on `a535441` and confirmed that the current timeline continuation
barrier prevents ordinary due work from dispatching until successful-advance
handoff completes. The adapter was promoted at canonical integration tip
`b75c5b8`; this State correction reconciles the promoted candidate's stale
pending-gate statement.

Temporal identity/cardinality was revalidated: `PersonId` identifies the
decision actor while `ActivityInstanceId` remains distinct; one-actor fixtures
do not establish one-to-one activity/actor cardinality. A `SourceReceiptId` is
shared causal provenance and may fan out to multiple P11 transitions;
store-wide unique `OperationId` values identify individual transitions and
exact retries. `ActorChoiceStoreTests.TemporalSourceReceiptCanFanOutAcrossInputsWithDistinctOperationIds`
and the actor-decision shared-receipt tests exercise these cases.

Focused validation on the reviewed `ff2631f` code tree passed:

| Suite | Result | XML under `Library/ValidationResults/P18CAdapter` |
|---|---:|---|
| ActorAvailabilityDecision | 10/10 | `EditMode-20260927-162914-dd6b36244668415c813334aea1e07b7f.xml` |
| ActorChoiceStore | 12/12 | `EditMode-20260927-162853-389f76a98e5c477d9c6bb7114ca6a00a.xml` |
| ActorChoiceRuntime | 11/11 | `EditMode-20260927-162925-d1634bb1f5bf40c7a6fa3f33797dbb03.xml` |
| ActorChoiceDiagnostics | 4/4 | `EditMode-20260927-162935-9a1936e29ef044d1af819c2ba7302e24.xml` |
| ActorActionChoiceCommand | 6/6 | `EditMode-20260927-162945-c2ee7d39e89741a5960055162595add6.xml` |
| ActivityLifecycle | 17/17 | `EditMode-20260927-163001-d9399682384343c29373fd898a7fd2fb.xml` |
| LogicalTimeline | 19/19 | `EditMode-20260927-163011-26281ff0a7bf4c79b00ad5828bc7a383.xml` |
| SimulationRuntimeOrchestration | 10/10 | `EditMode-20260927-163021-46cdb5e43dfb43f684d5c3ae518d7803.xml` |
| SpatialRoutePlanning | 20/20 | `EditMode-20260927-163031-065498b0037348b6aed7b06b263131da.xml` |
| ALL EditMode | 1778/1778 | `EditMode-20260927-163046-9329b9254f1b474ebf22d54f594ce980.xml` |
| Official complete EditMode `Smoke` | 5/5 | `EditMode-20260927-163119-37f10b734f524945af59cf5ec16022ed.xml` |

The XML files report `Passed` with zero failed, inconclusive, or skipped tests.
These results apply to the earlier pre-extension candidate and do not replace
the post-extension results recorded below. `git diff --check` passed. The first diagnostics run exposed a stale
reflection-fixture signature; it now matches the temporal constructor and all
four diagnostics tests pass. These artifacts are retained in the integration
worktree's dedicated Library validation directory so later Unity runs do not
replace the referenced evidence.

The complete EditMode run includes the current P9/P11 compatibility tests; the
earlier upstream P9-B/P11 run at `2d6b3ce` remains supporting evidence, not a
substitute for this historical candidate result. P18-C adapter promotion to
canonical `b75c5b8` satisfies the P18-D dependency on temporal input capture:
P11 `TryCaptureTemporal` and typed temporal lifecycle dispositions are
promoted in code `a535441`. P18-D consumes those APIs and preserves P11's
semantic validation. At the time of this P18-C adapter record, P18-D still
awaited the serialized `SimulationRuntime` ownership window and economy-owner
operation receipt with immutable proposal correlation and current-truth
execution snapshot. Both prerequisites were subsequently promoted at
`9e790c5`; this adapter itself does not implement the P18-D SellGoods consumer
or claim a daily runtime migration.

Post-extension validation on code integration `a535441` passed:

| Suite | Result | XML under `Library/ValidationResults/P18CPostExtension` |
|---|---:|---|
| Temporal source-receipt fanout / distinct operation IDs | 1/1 | `EditMode-20260927-194223-335ca38ee84b4cc8900d7b1808a44129.xml` |
| ActorAvailabilityDecision | 10/10 | `EditMode-20260927-194237-4067adcb6ff1451b9cbeb5ac9a27e2f5.xml` |
| ActorChoiceStore | 12/12 | `EditMode-20260927-194259-14cfc5728ca34412b9204f1f31aa5db4.xml` |
| ActorActionChoiceCommand | 6/6 | `EditMode-20260927-194313-57fc327f21cb493bbe60c832017197d3.xml` |
| ActivityLifecycle | 17/17 | `EditMode-20260927-194326-eb72171d9fe14107aded02a686bd0d89.xml` |
| LogicalTimeline | 35/35 | `EditMode-20260927-194340-979a68f4a97242e580c0f985b58269fc.xml` |
| ActorChoiceRuntime | 11/11 | `EditMode-20260927-194826-bcc59af572184a009ba1df7b69d3fdac.xml` |
| ActorChoiceDiagnostics | 4/4 | `EditMode-20260927-194839-421cbd5f3c804a9ab29aa55e0e0a9450.xml` |
| SimulationRuntimeOrchestration | 10/10 | `EditMode-20260927-194853-6e438ba68319464dac92ef519ecf1aff.xml` |
| SpatialRoutePlanning | 20/20 | `EditMode-20260927-194906-41bc315b75dd4bbaa901660ffd995688.xml` |
| ALL EditMode | 1794/1794 | `EditMode-20260927-194921-baa9171b59084cf48142218ca7130700.xml` |
| Official complete EditMode `Smoke` | 5/5 | `EditMode-20260927-194951-6c1d34fbbdf14dec889095c79f1d8c62.xml` |

All XML results report zero failed, inconclusive, or skipped tests. The focused
fanout test preserves a shared causal source receipt across distinct per-actor
transitions with unique operation IDs. `git diff --check` passed. Independent
implementation review passed on exact code tip `a535441`; the only requested
follow-up was a docs status correction, completed in `38999cb` and independently
re-reviewed. The code tree is unchanged by both docs-only follow-ups.

P11 Actor Choice is canonical at closure tip `308e24d0744112e8f2b741521b8b3e4acb51ebbf`
(code `0cd4281`). P18-A has no P11 dependency, and P11's promotion changed no
P18-A source; the P18-A candidate and its validation remain applicable
(`UPSTREAM_IRRELEVANT`). P18-D may select the bounded actor-command consumer
alongside only the specific promoted spatial/travel capability its reviewed
scope requires.

Historical context from prior candidate review: P18-B's independently reviewed design is at `002ddc394339f924739f67fc4a8ff4420d3aecf3`;
its implementation is promoted. P18-C is promoted at code tip `7aa7626`; the
current P18 canonical State tip is `99cac77` (the earlier State tip was
`18ecc6e`). P18-D's refreshed technical design candidate `aa5f182` passed
independent review against then-current P18 canonical `18ecc6e` and the
then-current P14 promotion State `f8a61fe` (current P14 State tip `4caecbb`);
it retains P14 exclusion and has no P20/P19 blanket
dependency. The earlier additive P18-A boundary/subphase design `6bbbc33` was
refreshed into contract `2175bf2`, accepted with durable acceptance record
`9de70ae`. The initial implementation candidate `3acb5b9` was returned
for correction
after review identified missing world/profile binding for restored continuation
and pending-signal identities, missing frozen-manifest equivalence on reload,
and C handoff occurring before `TryAdvanceTo` returns; review also requested an
invariant for conflicting already-indexed published facts. The corrected
implementation candidate is `f1bfe818565c3fca81b373d1bc9a70a16f4eda10`.
Exact-tip independent review passed: reconstruction validates world/profile
identity and the frozen manifest; post-success handoff is explicit and outside
the advancing call; and conflicting indexed facts are rejected by due instant
and causal sequence. Candidate validation passed LogicalTimeline 35/35, ALL
EditMode 1756/1756, official complete Smoke 5/5, and `git diff --check`
(worker-reported results; the independent review did not rerun Unity). The
corrected candidate was promoted through integration tip `1dd0479` to `codex/phase18/canonical`. P18-C's external-input/deferral adapter, including P11 temporal capture/dispositions, was subsequently promoted at `b75c5b8`. At that historical checkpoint P18-D still awaited its economy-owner operation receipt and separate `SimulationRuntime` ownership window; both prerequisites were later promoted at `9e790c5`.

At the then-recorded P18 checkpoint: P20 Entry Architecture `2f9c93b588ffccaae60aedf6c16191c1251f6a1f` and
Technical Design `6a0d16494735853ce35a8974ab348551650afd6b` passed their prior
design review; the current-base refresh `318cacb` independently passed review
against P18 State `eabc1c2` and code `1dd0479`. The refresh records
continuation-barrier ordering, returned-fact publication before ordinary
same-instant work, and post-successful-advance P18-C handoff. P20-A — Synthetic
Multi-participant Operation was accepted as bounded scope only on 2026-09-27;
the two-Person rule remains fixture-only. Feature implementation `22df7b3`
was integrated against P18 canonical `b75c5b8` at `ee8502f`; independent
integration review passed. P20SyntheticOperation 11/11, ActivityLifecycle
17/17, LogicalTimeline 35/35, ALL EditMode 1805/1805, official complete Smoke
5/5, and `git diff --check b75c5b8..ee8502f` passed. The P18-C adapter changes
no P20 code/API and does not change this slice's contract. P20-A was promoted
at code `22df7b3` (integration `1dcf67a`), with P20 State tip `7a81cc0`.
P20 remains independent of P18-D and P19.
