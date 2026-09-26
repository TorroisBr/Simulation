# P20 — Multi-participant Synthetic Operation Technical Design

**Design base:** `97b97c5c7523f39f3645bc018c82dbbab633648f` on
`codex/phase20/MultiParticipantTechnicalDesign`, descended from canonical
architecture `c285466c355103d3637ac165246591b72eb7bda0`.
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§11–12, 91–93;
`docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; the Phase 20 Brief and entry
proposal; reviewed P18-A, P18-B, and P18-C technical designs; and both dated
architecture alignment records.
**Status:** Proposed technical design only. No implementation authorization,
checkpoint IDs, capability promotion, persistence schema, or Phase State change.

## 1. Purpose and boundary

This design maps the entry-approved two-Person synthetic operation onto the
accepted P18 timeline, lifecycle, and actor-decision contracts. It proves that
a temporary activity instance can coordinate two distinct Persons while each
retains identity, Knowledge, and decision authority. The exact-two requirement,
role-free participant set, common fixture interval, and test-owned effect
transaction apply only to this proof. They do not narrow the architecture's
one-or-more participant rule for Scheduled and Active instances or establish a
general role/count policy.

The activity definition/version, stable `ActivityInstanceId`, and participant
relations are separate facts. Neither proposer nor participant owns the
instance. No persistent Group, Organization, NPC-owned activity, universal
activity/effect framework, or second clock is introduced. This synthetic
operation has no gameplay meaning.

## 2. Ownership and P18 contract mapping

| Concern | Authority and contract mapping |
|---|---|
| Logical instant, sealed inputs, due-work order and dispatch | P18-A. Start is a typed due-work reference ordered by exact `LogicalTick`, causal wave, stable owner/domain ID, `DueWorkId`, and persisted occurrence sequence. P20 owns no scheduler or clock. |
| Definition/version, stable instance ID, lifecycle/revision, participant relations and commitments | P18-B activity domain. Instance identity is independent of definition, `PersonId`, and `NpcRuntime`. Its rebuildable P18-A descriptor resolves the authoritative instance/revision before transition. |
| Each Person's accept/decline proposal and decision boundary | P18-C using that Person's stable `PersonId` and own permitted Knowledge. A separate decision for each required participant is recorded; one Person cannot decide for another. Current truth and commitments are revalidated at execution/start. |
| Individual availability and commitment facts | P18-B and its individual availability authority. Commitments are per `(ActivityInstanceId, PersonId)` and interval. P20 coordinates a complete required-set write; it does not create a second availability store. |
| Synthetic operation preconditions and participant results | One test-only synthetic domain authority. It validates current facts and atomically publishes both participant-specific results, or neither. P18-B lifecycle is not itself the operation effect. |

P18-A's due-work identity names the activity instance/domain transition, never
an implicit single actor. P18-B's participant relation remains stable-ID based.
P18-C's actor decision state remains individually keyed by `PersonId` and may
reference the instance without making it an actor-owned child. Definition,
instance, and each participant relation carry compatible stable semantic IDs;
runtime references and collection order are not causal identity.

## 3. Bounded formation and commitments

The fixture creates one supported operation definition/version and one
Proposed activity instance with a stable identity allocated by a deterministic
accepted input/owner operation identity or persisted domain sequence, as
specified by P18-B. It arranges exactly two distinct required `PersonId`s in
stable semantic order. Duplicate identities reject the proposal without
mutation. There are no roles or optional participants in this fixture.

Each required Person independently receives an accept-or-decline opportunity.
The decision reads only that Person's allowed Knowledge and records its
decision identity, logical boundary, and relevant source revision. A decline
maps to P18-B's `Proposed → Cancelled` transition, with P20's `NotFormed`
terminal disposition. The disposition and release of any instance reservation
already made are one coherent owner mutation. `NotFormed` is a P20 outcome
label, not a new P18-B lifecycle state. A missing decision leaves the instance
Proposed and non-executable. No acceptance is inferred from another
participant, the proposer, a roster, shared context, or the UI.

Acceptance requests that Person's commitment for the same half-open future
fixture interval `[start, end)`, with integer P18-A ticks and checked range.
Each participant's decision and individual commitment are committed together
after validation against current commitments and availability. A successful
first acceptance may remain as partial Proposed state; it is not executable.
The instance becomes Scheduled only when both required decisions and both
matching reservations exist. Publishing Scheduled state, the final activity
revision, and the start due-work fact is one coherent mutation. If that
transition fails, it publishes none of those changes. A reservation conflict
rejects that participant's decision/commitment mutation without partially
changing that participant's commitment state.

This proof uses one common interval to keep its validation surface bounded.
It does not require all future activities or role-specific commitments to use
the same interval. Interval overlap and incompatibility are decided by the
existing availability authority; P20 adds no general reservation solver.

## 4. Scheduled start, effects, and terminal handling

At the scheduled instant, P18-A dispatches the stable instance start reference
in deterministic causal order. The P20 activity owner resolves the current
instance and expected revision. Before publishing any start or effect, a
single coordinated validation confirms:

- the instance remains Scheduled at that revision and has exactly the two
  distinct required Persons for this fixture;
- both independent acceptances and matching reservations remain present;
- both Persons remain eligible and available under current domain truth at the
  exact start instant, and each commitment still matches the fixture interval;
- the operation's current factual preconditions hold; and
- the start/effect idempotency identity has not already committed.

If validation succeeds, the synthetic domain authority computes results from
current facts and explicit operation inputs, then commits both distinct
participant results under one all-or-none effect boundary. Participant
iteration and result publication use ascending semantic `PersonId` order.
P18-B's transition to Active and the operation effect use the reviewed owner
mutation boundary so a retry cannot publish one without the other or apply an
effect twice. The actual start instant equals the due instant. Completion at
the planned end is a P18-B lifecycle transition; it is not a second operation
effect. This test-only boundary does not establish a cross-domain transaction
framework.

If a required participant, reservation, or precondition is missing or stale at
start, no effect and no partial Active state are published. The instance
takes P18-B's supported terminal cancellation disposition with a P20
`FailedToStart` reason and releases both instance commitments; this does not
add a new generic lifecycle state. Explicit pre-start cancellation follows
P18-B's Cancel transition,
invalidates the pending due reference by revision, and releases both
commitments atomically. Either decline uses the same `Proposed → Cancelled`
transition with `NotFormed` disposition and releases any fixture reservation
already established. These terminal transitions
preserve decisions and causal outcomes; they do not erase history or roll
back effects that have already committed. No retry, recruitment, withdrawal
after start, mid-execution roster change, or generic interruption policy is
added.

## 5. Determinism and failure behavior

The same compatible definition/version, instance and participant IDs, logical
instant/calendar, effective configuration, current authoritative inputs,
deterministic random context if actually consumed, and ordered accepted inputs
must yield the same decisions, formation, start disposition, and individual
results. Use persisted semantic sequences and stable IDs; do not depend on
dictionary/set enumeration, materialization, UI state, host time, registration
order, or `NpcRuntime` presence.

Planning and read-only queries do not mutate state. Stale descriptors are
resolved against owner state and revision at dispatch; stale queue nodes cannot
consume a newer transition. Failed validation or transaction commit publishes
no partial reservation, lifecycle, sequence allocation, effect, or success
signal. Committed cancellation/failure is an explicit owner disposition, not
an implicit scheduler result. Availability notifications publish only after
their source transition commits. P18-C permits one attempt per actor and causal
decision boundary; a refusal/stale attempt does not cause an unbounded retry at
the same boundary.

## 6. Causal and reconstruction inventory

Any later save, clone, continuation, or historical reconstruction that claims
this operation is in scope must retain or deterministically rebuild:

- compatible activity definition identity/version and effective operation
  inputs/configuration;
- stable instance ID, creation identity, revision, state, scheduled/actual/
  terminal logical instants, and terminal disposition;
- the exact required `PersonId` set for the branch boundary and each independent
  decision identity/outcome, permitted decision inputs/Knowledge versions, and
  causal sequence;
- each reservation's participant, interval, owning source revision, and
  availability facts needed to validate it;
- P18-A calendar/tick version, current instant, sealed accepted input boundary
  and order, pending start/end due facts, causal wave/order/allocator state, and
  dispatch configuration;
- operation precondition/source revisions, effect idempotency identity, and
  each participant-specific effect already applied; and
- deterministic random state/context only if the operation actually consumes
  it.

Queue nodes and lookup indexes may be rebuilt only if their complete order and
validity follow from these facts. A fork preserves the participant set and
decisions at that boundary; it must not infer a later roster or replay a
committed effect. Events, history, diagnostics, object references, and
materialized NPCs cannot substitute for authoritative causal state. This is
not a P12/P13 schema or persistence implementation.

## 7. Validation obligations and review boundary

Before implementation review, the selected slice should demonstrate:

- independent decisions and Knowledge boundaries for two distinct Persons;
- one decline, one missing decision, duplicate PersonId, and reservation
  conflict each prevent formation/start without partial mutation;
- deterministic ordering under participant insertion/materialization
  permutations;
- a valid pair of commitments schedules one stable instance and start due
  fact, while stale revisions and conflicting/unavailable current truth fail
  closed;
- start validates the complete required set before either effect, and a
  failed transaction/retry cannot create partial or duplicate effects;
- cancellation, decline, failed start, and stale queued work release or
  invalidate the correct commitments/work atomically;
- P18-A same-instant ordering, non-reentrant dispatch, and retry behavior remain
  intact; P18-B lifecycle/commitment and P18-C Knowledge/availability regressions
  remain intact; and
- stable identity and causal state survive clone/dormancy/materialization
  permutations within the later supported runtime scope.

The design review must compare these obligations to actual promoted P18-A/B/C
capabilities before implementation. The current documents and candidate
designs define contracts only; their existence does not establish runtime
capabilities.

## 8. Dependency gate and exclusions

P20 runtime implementation requires the **relevant promoted P18-A timeline/
scheduler, P18-B lifecycle, and P18-C availability/decision capabilities**,
plus independent technical-design review of this bounded proposal. It does not
wait for all P18-D migrations or P19. P18 does not depend on P20, so no
dependency cycle is introduced. P18-A/B/C designs are accepted contracts, not
promoted code, and do not alone satisfy this implementation gate.

The work remains limited to the synthetic two-Person proof. It does not add
Sleep, Dreams, robbery, gangs, rituals, War, MegaEventos, co-travel, meals,
patrol, construction, multi-worker production, recruitment/negotiation, a
persistent Group, arbitrary group sizes, a universal role catalog, a full AI
planner, workflow/effect engine, P8 travel integration, a P19 loader/public
adapter, or P12/P13 persistence/replay. Current extensibility constraints
remain design constraints: semantic IDs, deterministic composition, and
domain-owned effects must allow future extensions without rewriting
`NpcRuntime` or creating one manager per mechanic; the public mod platform is
deferred to its documented Phase.

No unresolved semantic or product blocker is identified within the
entry-approved scope. Exact interfaces and code-level transaction composition
remain subject to independent technical review against the then-promoted P18
capabilities. This proposal authorizes no implementation and creates no
checkpoint ID.
