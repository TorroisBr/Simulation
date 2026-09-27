# Accepted-scope checkpoint P20-A — Synthetic Multi-participant Operation

**Status:** Scope accepted by the user on 2026-09-27. Acceptance covers only
the checkpoint ID and bounded synthetic proof below; it does not claim a P20
capability or permit implementation before independent review of the refreshed
P20 documentation against current P18 canonical.

## Baseline and design evidence

- Architecture: `c285466c355103d3637ac165246591b72eb7bda0`.
- Current P18 canonical State tip: `eabc1c24a0ba8951ded87280472cc7137e741434`;
  promoted code integration: `1dd0479626ddf00bf08aa66533f54fef7328a421`.
  P18-A includes the additive boundary continuation, returned-fact publication,
  and post-successful-advance P18-C handoff contract; this refresh records those
  requirements below and requires independent current-base review.
- Promoted prerequisites: P18-A timeline/scheduler plus continuation extension
  `1dd0479626ddf00bf08aa66533f54fef7328a421`,
  P18-B activity lifecycle `97918cbbe4238a65a216b1a1f0ef84c70b4d080c`, and
  P18-C availability/actor decisions `ab05ecfe976e80badf6f509b8e9be25ff556ca23`.
- P20 entry architecture `2f9c93b588ffccaae60aedf6c16191c1251f6a1f` and
  technical design `6a0d16494735853ce35a8974ab348551650afd6b` have independent
  design-review **PASS** recorded in the latest P18 State. Review covered the
  Proposed-before-schedule instance, complete-set atomic commitments, the
  post-advance sealed-input start bound, and distinct activity/participant
  identities. P18-A due start-work remains behind its continuation barrier;
  this does not change P20's bounded cardinality or grant P20 ownership of
  continuation state.
- Current review constraints include both
  `../architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
  `../architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`: deterministic logical
  ordering and semantic identities; `ActivityInstanceId` independent of any
  `PersonId`; no fixed general participant cardinality; semantic extension
  seams now, with public Mod API/loader deferred to P19.

P18-D and P19 are not prerequisites. This synthetic proof consumes no P8
travel, P11 input, P14/P15 production, persistent Group/Organization, or
military capability.

## Bounded proof

Use only a synthetic test operation with no gameplay meaning. The test arranges
one compatible operation definition/version and exactly two distinct required
`PersonId`s in stable semantic order. Exact two is a fixture policy for this
proof, not a universal P20 role/count policy. P18-B's supported nonempty
participant-set contract and the architecture's one-or-more cardinality remain
unchanged. The instance has a stable `ActivityInstanceId` separate from the
definition, either participant, proposer, and any `NpcRuntime`.

Create a P18-B `Proposed` instance before collecting decisions. Each Person gets
an independent accept-or-decline opportunity using only that Person's allowed
Knowledge; neither participant, proposer, roster, nor UI may decide for the
other. Retain each decision and its causal identity/boundary. Acceptance records
that Person's reservation intent for one common future half-open interval
`[start, end)`; it does not install a partial active commitment. Missing or
declined decisions never count as acceptance. Partial responses leave the
instance `Proposed` while formation remains open. There is no timeout or
implicit close: P20 evaluates completeness only on an explicit, bounded
formation-close attempt. If a required decision is still missing or is declined
at that attempt, P20 records `NotFormed` atomically; the P18-B instance remains
`Proposed` with no Schedule receipt, active commitments, or start/completion due
work. Duplicate PersonIds reject the proposal before mutation, publishing no
instance or due work.

At a formation-close attempt where both have accepted, revalidate the full
required set and current eligibility, then make one P18-B `TrySchedule` call
with both participants and the common interval. P18-B atomically installs the
complete commitment set, participant relations, due work and Scheduled
transition/receipt. A conflict or stale revalidation before scheduling yields
`NotFormed`; the already-created instance remains `Proposed` without a Schedule
receipt, active commitments or due work.

P18-C hands off accepted actor decisions only after successful advance. Require
the scheduled start to be strictly later than
`max(CurrentInstant, InputsSealedThrough ?? CurrentInstant)`. A late/sealed start
or checked next-tick overflow yields `NotFormed` without scheduling.

At the scheduled instant, the P20 start is ordinary P18-A due work. If a
boundary continuation is pending at that instant, the P18-A barrier retains
this work until continuation completion; P20 must not bypass the barrier or
advance past its boundary. P18-A publishes all returned timeline facts after
continuation completion and before dispatching ordinary same-instant work.
P18-C source signals remain withheld until the outer timeline advance returns
successfully. P20 neither owns nor implements continuation state. Then validate
the complete set together: the instance is
still Scheduled at the expected revision; both required Persons and matching
commitments remain present; both are currently eligible; the operation's
current factual preconditions hold; and the effect identity has not committed.
If any required participant is missing, unavailable, or stale, produce
`FailedToStart`, apply neither participant's effect, and use the P18-B
FailedStart cancellation path to invalidate the instance's due work and release
all of its commitments. Explicit pre-start cancellation uses P18-B Cancel and
performs the same coherent release/invalidation. The P20 disposition must be
committed coherently with the corresponding P18-B lifecycle state/receipt,
commitment release and due-work invalidation; neither failure nor cancellation
may publish only part of that outcome. No effect is implicitly rolled back
after start.

On success, one reviewed P20 owner/API transaction couples the P18-B
`Scheduled → Active` transition and Start receipt with both separately
identified participant results. The synthetic authority computes each result
from current facts and explicit inputs for that Person; the fixture must make
the results observably distinct to prove per-participant effects, without
reading the other Person's hidden Knowledge. Commit both or neither, with stable
`PersonId` ordering and idempotent retry. P18-B commitments remain installed
through the Active interval and are released by the existing terminal lifecycle
transition. No withdrawal, roster change, mid-execution loss policy, or
general rollback is added.

## Reconstruction and validation

The later implementation must retain or deterministically reconstruct the
compatible definition/version and inputs; stable instance ID, revision and
lifecycle; required Person IDs and each independent decision/input boundary;
reservation intents and installed commitments with intervals/source revisions;
calendar/tick, sealed input boundary/order and pending due work; causal order
and allocator state; current precondition revisions; the effect idempotency
identity; and each Person-specific result already applied. Persist random
context only if the operation consumes randomness. Event/history/log output,
runtime object references, incidental collection order and materialized NPCs
cannot replace these facts. This is not a save/replay schema.

Implementation validation must cover at least:

- existing one-participant P18 activity/lifecycle behavior remains compatible;
- each Person's decision is independent and Knowledge-bounded;
- an unanswered partial response remains Proposed before formation-close; a
  still-missing or declined decision at explicit formation-close, duplicate
  PersonId, reservation conflict, stale state, late/sealed start and overflow
  create no partial commitment or start;
- the Proposed instance remains accurately represented when formation fails;
- exactly one complete-set `TrySchedule` publishes both commitments and due
  work atomically;
- missing/lost or newly unavailable required participants fail the coordinated
  start with no effects and release all commitments;
- explicit cancellation invalidates pending work and releases both commitments;
- successful start atomically publishes Active/Start plus the two distinct,
  separately identified results, with no duplicate effect on retry;
- stable IDs and results do not vary with participant insertion order,
  materialization, UI state, or incidental iteration order; and
- a pending P18-A boundary continuation blocks a P20 start due at the same
  instant; upon completion, returned timeline facts publish before that start,
  and P18-C signals are handed off only after successful outer advance return;
- promoted P18 timeline/lifecycle/availability regressions remain green.

## Acceptance and next gate

Acceptance of `P20-A` approves only this bounded checkpoint scope and ID. It
does not approve a concrete gameplay consumer, a general activity framework,
arbitrary group sizes or roles, cross-domain transactions, a mod API/loader,
save/replay, or any other Phase 20 scope. After independent review of the
current-base documentation refresh, implementation must start from the then-
current canonical dependency base in an isolated worktree, map the reviewed
contracts to concrete APIs, and pass independent implementation review and the
required validation gates before any promotion request. Until that review
passes, this proposal authorizes no code changes. The
accepted scope does not establish a general participant count: exactly two
distinct PersonIds is only this fixture's policy, while the architecture's
one-or-more participant cardinality remains unchanged. Implementation starts
only after independent review of the current-base P20 documentation refresh.
