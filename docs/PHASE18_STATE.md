# Phase 18 State — Intraday Temporal Execution v1

**Status:** PHASE 18 IN PROGRESS — P18-A/B/C core promoted; additive P18-A extension candidate reviewed/validated with canonical promotion pending; P18-D design reviewed, consumer prerequisites remain open

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
| P18-A — Additive Boundary/Continuation | IMPLEMENTED, REVIEWED, VALIDATED; PROMOTION PENDING | Accepted contract `2175bf2` (acceptance `9de70ae`); corrected candidate `f1bfe818565c3fca81b373d1bc9a70a16f4eda10`; exact-tip independent review PASS; LogicalTimeline 35/35, ALL EditMode 1756/1756, official Smoke 5/5, and diff-check PASS (validation results worker-reported). |
| P18-B — Activity Lifecycle | PROMOTED | Implementation `97918cbbe4238a65a216b1a1f0ef84c70b4d080c` was promoted through integration commit `8f0cc4a6764abfc238e2497e4fad19487c6db82f` after independent review and user approval. A foreign timeline cannot dispatch lifecycle facts. Integration validation: ActivityLifecycle 16/16 (`EditMode-20260927-015240-ce080762386744cd9063484f033c5531.xml`), LogicalTimeline 19/19 (`EditMode-20260927-015352-fd3eb5a52fc9441ab7df66ba3b693dd0.xml`), ALL EditMode 1734/1734 (`EditMode-20260927-015417-28accf72f1ca42e79738072790b84001.xml`), official Smoke 5/5 (`EditMode-20260927-015453-960eaed7ce0443329ae1d54f33b4aacf.xml`), and `git diff --check` passed. |
| P18-C — Availability-Driven Decisions | PROMOTED | Source tip `ab05ecfe976e80badf6f509b8e9be25ff556ca23` (review-corrected State/promotion tip `7aa76268c49058fedb997392e676c6a29169c8b0`), based exactly on `39bd42e9e78f80c1e40b35b099e980ee8bc44a43`; architecture/alignment baseline `c285466c355103d3637ac165246591b72eb7bda0`. Independent implementation re-review passed after preserving distinct same-actor/same-tick causal receipts and retrying uncommitted execution with the same stable proposal ID. Focused ActorAvailabilityDecision 5/5 (`EditMode-20260927-031900-b609bdccc79143319b5e3ab54106cb84.xml`), ActivityLifecycle 17/17 (`EditMode-20260927-031918-7c40152d1d6545e29f127d8709a89542.xml`), ALL EditMode 1740/1740 (`EditMode-20260927-031934-f18b558a02a84818985c816b664106ab.xml`), and complete official Smoke 5/5 (`EditMode-20260927-032011-5c51d8ccde5a4fc8bec922bebcd88dd0.xml`) passed; `git diff --check` passed. User approved promotion; `codex/phase18/canonical` is promoted at `7aa7626`. |
| P18-C — External-Input/Deferral Adapter | IMPLEMENTATION REVIEW AND BASE VALIDATION PASS; PROMOTION WAITS ON P18-A EXTENSION REVALIDATION | Integration candidate State tip `e4ff318`, code refresh tip `e2af495`, based on canonical `99cac77`; temporal-identity/cardinality and accepted-contract review passed. Preserve candidate; revalidate/reintegrate against P18-A extension `f1bfe81` after its promotion before promoting this adapter. |
| P18-D — Consumer Integration | TECHNICAL DESIGN REVIEW PASS; IMPLEMENTATION BLOCKED | Docs candidate `codex/phase18/P18DIntradaySellGoodsDesign` at `aa5f182f0f5a93f26092179d1708a1f3429fbb1f` passed independent refreshed review against then-current P18 State/canonical `18ecc6e` and P14 State `f8a61fe`. Implementation remains blocked on canonical promotion of corrected P18-A extension candidate `f1bfe81` (review and required validation passed; approval pending), promotion plus revalidation against that extension of the P18-C external-input/deferral adapter candidate recorded below, and the serialized `SimulationRuntime` ownership window. The adapter is a separately bounded capability, not the P18-D live consumer. P11 must own the exact intraday capture record linked to the P18-A accepted reference. The P18-D SellGoods slice must add an economy-owner operation receipt with immutable proposal correlation and current-truth execution snapshot; no such receipt currently exists. P14 remains excluded without a reviewed temporal owner adapter; no blanket P20/P19 dependency is implied. |

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

**P18-C external-input/deferral adapter:** independent design review PASS at
exact tip `358c65c85e1eafdd91ef4a6553ba3b0a8c4af249`; bounded scope is
`READY_FOR_IMPLEMENTATION`. The design specifies a P18-C request-state owner,
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
clean range diff. The candidate has not been promoted.

After canonical advanced from `311baa9` to `99cac77`, an independent impact
review classified the workflow-instruction-only change as
`UPSTREAM_IRRELEVANT`. The integration branch now composes original candidate
`1ba60f5` with current canonical `99cac77` at exact refreshed tip
`e2af495d9255e9a1b4d1c41e2ff0617831ab2a3d`; the refreshed tip contains the
current canonical commit as an ancestor. The only tree delta from `1ba60f5` is
the seven `AGENTS.md`/`.agents/skills` workflow files. P11/P18 code, briefs,
designs, temporal identity/cardinality contracts, and the validated executable
tree are unchanged. The independent exact-tip refresh review passed at
`e2af495`; the existing focused, ALL EditMode, and official Smoke results above
therefore remain applicable. `git diff --check 99cac77..e2af495` passed. The
refreshed integration candidate remains unpromoted pending its formal gate.
The State-only follow-up at `e73d203` passed independent factual re-review;
its only delta from reviewed source/integration tip `e2af495` is this State
record, with a clean range diff-check. The subsequent State-only candidate `e4ff318` updates the P18-A implementation result and requires post-extension adapter revalidation; its code and validation evidence are unchanged.

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
`git diff --check` passed. The first diagnostics run exposed a stale
reflection-fixture signature; it now matches the temporal constructor and all
four diagnostics tests pass. These artifacts are retained in the integration
worktree's dedicated Library validation directory so later Unity runs do not
replace the referenced evidence.

The complete EditMode run includes the current P9/P11 compatibility tests; the
earlier upstream P9-B/P11 run at `2d6b3ce` remains supporting evidence, not a
substitute for this integration result. P18-D remains BLOCKED pending canonical promotion of the reviewed P18-A extension, promotion and post-extension revalidation of the P18-C adapter integration candidate, and the serialized `SimulationRuntime` ownership window. The adapter does not
implement the P18-D SellGoods consumer or claim a daily runtime migration.

P11 Actor Choice is canonical at closure tip `308e24d0744112e8f2b741521b8b3e4acb51ebbf`
(code `0cd4281`). P18-A has no P11 dependency, and P11's promotion changed no
P18-A source; the P18-A candidate and its validation remain applicable
(`UPSTREAM_IRRELEVANT`). P18-D may select the bounded actor-command consumer
alongside only the specific promoted spatial/travel capability its reviewed
scope requires.

P18-B's independently reviewed design is at `002ddc394339f924739f67fc4a8ff4420d3aecf3`;
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
candidate has not been promoted; canonical promotion remains pending. P18-D
implementation remains blocked on promotion of this extension, promotion and
post-extension revalidation of the independently reviewed P18-C
external-input/deferral adapter candidate, and the separate `SimulationRuntime`
ownership window. P20 Entry Architecture
`2f9c93b588ffccaae60aedf6c16191c1251f6a1f` and Technical Design
`6a0d16494735853ce35a8974ab348551650afd6b` both passed independent refreshed
review against promoted P18-A/B/C and then-current P18 State `bcb3f67`; targeted
revalidation against accepted P18-A contract `2175bf2` found no P20 contract
change (`UPSTREAM_IRRELEVANT`). Review
confirmed the pre-schedule `Proposed` instance, atomic scheduling/commitments,
the sealed-input start bound, and distinct activity/participant identities.
P20's Brief still has no approved implementation checkpoint ID; design PASS
does not supply that authorization. P20's checkpoint gate is separate and does
not depend on P18-D or P19.
