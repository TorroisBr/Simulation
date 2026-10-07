# P20-C — Two-Person Joint Civil Travel: refreshed technical design record

**Checkpoint:** P20-C. **Date:** 2026-10-07.
**Disposition:** refreshed technical contract — PASS / READY_FOR_IMPLEMENTATION,
retaining the semantic design with bounded current-base corrections;
independent review is recorded in [P20C_TECHNICAL_REVIEW.md](P20C_TECHNICAL_REVIEW.md).
This record and its immutable source together form the current technical
contract; historical P20-B travel documents alone are not current instructions.

## Exact bases and source provenance

| Authority | Verified canonical ref / SHA |
|---|---|
| Architecture | `codex/architecture/world-identity-projection` — `e16796014d348e3b59da7ed848101c4c03926ba5` |
| P8 | `codex/phase8/canonical` — `470667d37863384edadb3d93ef64d8004aff46a3` |
| P18 | `codex/phase18/canonical` — `8ac2d7885ea1f00d544d88a64bf918a411934f7f` |
| P20 | `codex/phase20/canonical` — `fe4909a0fc371a2fedb55cb9cef086e5dbf63526` |
| P12 | `codex/phase12/canonical` — `94551b08be8cc9347de35eae5051b8e578ea4c1e` |

PR #1, head `9f24c4483bb4db0364cd3699f24f0e3d0868717c`, supplies the
accepted identity handoff. Phase 20 planning candidate
`22be1e07ef7a6dc40a82b7ebf490c103a4a293d7` supplies preliminary revalidation,
not independent design approval or P20-C code promotion.

The complete retained source is
`docs/architecture/P20B_TECHNICAL_DESIGN.md` at design content tip
`8afc463fb71112a0c7b8902e7e5673aee9e31bd9` on
`codex/architecture/p20b-technical-design`; the historical independent review
is retained at branch tip `d80ec06f48500a0ee80d6e05136ad50a6978a670`.
Its architecture base was `f6924e63d8e5731da1d33021d0361e7defe6dad7`.
Do not rename, edit or reinterpret those artifacts or P20-B's promotion record.
All travel references to P20-B in that source denote P20-C in this refreshed
contract. P20-A remains synthetic; actual promoted P20-B remains Daily census.

## Retained scope and owners

Two distinct Persons independently assent to one common supported civil
Hex-to-Hex leg. Two equivalent required traveler slots are a fixture policy,
not a limit on generic Activity cardinality, roles, contribution or intervals.

P18 owns definition/version, ActivityInstanceId, lifecycle, reservations,
participants, transition receipts and due-work. P20 owns proposal, independent
assent/decline and AbortAfterLeg intent. P8 owns each Person's route plan,
position/transit and travel facts. Individual Knowledge and any effects stay
with individual/domain owners. No second lifecycle, reservation store, clock,
scheduler, persistent Group/Party or synthetic Person is introduced.

Assent alone reserves no time. After both accept, P18 reserves both together
with `duration: null` and `[start, infinity)` commitments. Start revalidates
both Persons, commitments and current passage under each movement context.
One private replacement root per affected P8 store includes both Persons;
no sequence of independent root replacements may start only one traveler.
P18 and P8 changes install coherently through timeline owner-facts commit,
after all guards/preparation, with no fallible operation after first install.

Travel progress and arrival remain explicit, per Person; no automatic intraday
travel advancement is added. The first arrival commits its individual P8 facts.
The second arrival commits its P8 position/plan together with P18 Completed
and release of both reservations. Pre-start cancellation releases the pair.
Active abort records AbortAfterLeg; both finish their already-started leg, then
second arrival settles Interrupted and releases both. No teleport, rollback,
next leg, post-start participant deletion or inferred shared Knowledge.

## Current-base corrections and API mapping

These bounded clarifications supersede implementation-era proposals in the
retained source; they introduce no new gameplay or persistence scope.

| Historical conceptual boundary | Current P20-line evidence / required interpretation |
|---|---|
| New JointCivilTravelStore/System and JointTravelId | Existing `P20JointCivilTravelOwner` in `P20JointCivilTravel.cs` owns coordination. The referenced P18 ActivityInstanceId also identifies the joint undertaking; no separate identity allocator is required. Definition/version remain P18-owned. |
| Inviter assent at proposal creation | Current `TryCreate` creates no assent. Both named Persons explicitly record their own decision through `TryRecordAssent`; invitation never implies consent. |
| Proposed prepared insertion and schedule seams | P20-line `ActivityLifecycleStore.TryProposeWithParticipant` and its coordinated `TrySchedule` overload provide the narrow seams. Prepare/revision-check/private publication obligations remain normative; presence of those methods is not proof that every failure path passes. |
| Proposed composite start / consumer terminal seams | Existing `IActivityLifecycleTransitionParticipant`, `IActivityLifecycleTransitionCommit`, and `TryConsumerManagedTerminal` on the P20 line implement the intended shape. Exactly one participant binds per lifecycle composition; the P20-A synthetic fixture remains separate. Ordinary P18 duration-bearing behavior and unrelated activities must remain protected. |
| P8 batch/final-arrival preparation | P20-line `TryPrepareJointCivilLeg` and `TryPrepareFinalArrival`, position/plan batch preparation and `CanInstall` guards provide the shape. The selected proof uses Accepted one-leg plans; support for an already-Active plan is not required for closure or a license to enlarge the consumer. |
| P20 read-only at lifecycle transitions | Proposal/assent/abort semantics stay P20-owned; a retained `ExpectedLifecycleRevision` is a concurrency/reconstruction token, not a second lifecycle authority. If retained, it must remain coherent at **every** P18 transition, including FailedStart. |
| FailedStart terminal naming | P18 represents failure as Cancelled plus FailedStart receipt/disposition; no new terminal enum is needed. No P8 start roots install, both reservations release and P20/P18 state must remain reconstructible. |
| Future start boundary | Current proposal/assent validate start strictly after max(current logical tick, inputs sealed through), with checked overflow rejection. Rejected work leaves authoritative roots and causal ordering unchanged. |
| New P12 admission work | P20-B already promotes the absent/empty census boundary on the P20 line. Reuse it and validate integration with the current P12 line; do not duplicate the checkpoint or claim cross-line composition from either branch alone. |

These methods exist on the P20 canonical line; that does not establish that
standalone P8/P18/P12 canonicals contain the same integration. Master must
compare actual common bases/diffs and serialize hotspot composition. No whole
Phase reopening or upstream re-promotion is required solely by this record.

## Required bounded correction: FailedStart revision coherence

The retained design is not approved unchanged: current P20 code's empty
`JointTransitionCommit.CommitFailedStart()` conflicts with restoration's
exact `ExpectedLifecycleRevision == P18.Revision` check.

Choose the existing retained-token model, preserving that equality; do not
relax restore validation to accept arbitrary stale tokens or add a second
lifecycle. When preparing the due start, prebuild the P20 replacement row/root
for **either** outcome with expected P18 lifecycle revision `n + 1`, checked
for overflow. Keep P20's assent/abort coordination revision and causal order
unchanged: updating the expected lifecycle token is not a new assent/input.

A genuine start-precondition rejection still supplies a guarded coordinated
commit. `CommitFailedStart` installs only the prebuilt P20 replacement with
that token in the same P18/timeline transition that commits Cancelled +
FailedStart receipt, releases both commitments and invalidates pending work.
It must cover rejection by P18's start validator as well as P20/P8 validation.
No P8 start roots install on that branch, even if their preparation succeeded.
No new allocations, callbacks or validation may fail after first installation.
Successful start installs the travel batch and the same coherent P20 token.

A stale captured P20/P18 revision or malformed/missing required coordination
state is an invalid/retryable prepared commit, not permission to terminalize
with an unreconstructible link. Reject before any owner installation or due
reference consumption; re-resolve current truth on a legitimate retry.
Prebuild/check all writes under the serialized owner boundary and preserve
ordinary P18 and P20-A behavior.

Required regression: create/assent/schedule the pair, force current-passage or
P18 start-validation failure, execute due start, snapshot P18/P20, privately
restore their complete state and rebuild due work. Assert equal lifecycle
tokens, Cancelled/FailedStart identity/disposition/tick/order, no pending start,
both commitments released, unchanged P8 position/plan roots, and no later
start on retry. A stale/future/corrupt token must still reject private restore
without publication. Include failures from both validators; test pre-commit
staleness separately from a committed factual FailedStart.

This fixes a specified implementation boundary under accepted semantics.
The Architect supplies the contract only; Master owns correction and exact
code validation. Current existing code does not yet satisfy this criterion.

## Reconstruction-sensitive state

Retain compatible world/definition identity; creation and activity identity;
selected segment and participant PersonIds; requested logical start; every
assent/decline payload, causal input identity, accepted tick/order and proposal
revision; AbortAfterLeg identity/tick/order; coherent expected lifecycle token
when retained; P18 state/revisions/commitments/receipts/causal sequence and
pending-work facts; each P8 route/position/transit/progress and separate
Knowledge/effects; effective movement/configuration/content context when causal.
Derived lookup indexes, queue nodes and NPC materialization are not primary
truth. Queries/preview consume no RNG or causal sequence.

Snapshot/restore must validate identities, lifecycle links/revisions, causal
order and temporal bounds privately before publication. A failed-start,
declined proposal, scheduled pair, staggered arrival and pending abort must
remain reconstructible. No save schema, loader, storage or fork is introduced.
A future adapter cannot recover discarded causality.

## P12 and existing-code boundary

P20-B remains promoted only as Daily-profile census admission. Its historical
implementation repository tree is
`62f8f3f3ad803e3f8eca832f7e39cff8196d5b85`; P20 canonical `Assets` tree is
`8b579d9f61ad3145b535b27cdd71a37d512a32b1`.
Equal executable content does not promote P20-C or transfer historical test
coverage from admission to the whole travel contract.

Daily-v1 still admits absent/empty P20 ownership and rejects populated P20
state; P18/P8 unsupported state must obey their own profile boundaries.
Current P12 admission/census evolution is a compatibility check, not a full-P12
capability prerequisite. Runtime must validate explicit live composition.
Scenes/prefabs/convenience configuration do not determine profile scope.

## Required Master validation and exclusions

Audit existing code before selecting any delta. Require exact-base/code review
of independent decisions, partial/refused proposals, coherent creation and
reservation, conflicts/stale work, successful atomic two-person start and each
failure guard, distinct passage contexts/Knowledge, per-person progress and
staggered arrival, second-arrival terminal commit, cancellation, AbortAfterLeg,
unrelated lifecycle protection and isolated P20-A composition.

Validate reconstruction for every lifecycle disposition, including FailedStart
token coherence; deterministic causal/participant order; rejected mutation
non-effects; current P12/P20 integrated absent/empty acceptance and populated
rejection. Run focused and risk-appropriate P8/P18/P20/P12 regressions plus
independent exact-tip implementation review. Broaden testing only for changed
behavior; documentation-only reconciliation runs no Unity tests.

Excluded: implementation in the Architect stream; multi-leg/group travel,
permanent Party/Group, recruitment AI, universal role/contribution solver,
workflow/dispatcher framework, new needs/economy effects, War/gang/robbery,
P18-D migration, external ingress unless separately scoped, save/load/fork,
P19 loader, Daily profile expansion and Phase closure.
