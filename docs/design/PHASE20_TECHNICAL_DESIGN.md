# P20 — Multi-participant Synthetic Operation Technical Design

**Refresh base:** latest P18 canonical tip
`311baa930227371a807fb324ff11fc024800ddf9` on `codex/phase18/canonical`,
merged into this design branch. This latest canonical update changes only
`PHASE18_STATE.md`; the relevant promoted P18 contracts and code remain those
already reviewed at `18ecc6e56d3c6303edfaf8a38257355d262a6ea5`. The design
originated at `97b97c5c7523f39f3645bc018c82dbbab633648f` and remains subordinate
to architecture `c285466c355103d3637ac165246591b72eb7bda0`.
**Current P18 capability revalidation:** P18-A, P18-B, and P18-C are promoted
on the latest canonical State at `311baa9`. Their source tips are P18-A `985c56c40fc01dc6a4d392120e2d32151a558d03`,
P18-B `97918cbbe4238a65a216b1a1f0ef84c70b4d080c`, and P18-C
`ab05ecfe976e80badf6f509b8e9be25ff556ca23` (promotion/State tip
`7aa76268c49058fedb997392e676c6a29169c8b0`). P18-B includes stale-node
skipping without consuming the dispatch cap and the bounded
ActivityLifecycle composition/owner-dispatch path. P18-C adds the committed
transition-receipt log on the lifecycle owner, PersonId-keyed actor decision
requests, post-successful-advance coordination, Knowledge-bounded planning,
stable semantic proposals, and committed-versus-retryable executor results.
These are available dependencies, not remaining P20 implementation tasks.
P20 still owns multi-person partial decision/formation coordination, complete
required-set submission, P20-specific start validation, coordinated lifecycle
plus effect transaction, and coherent cancellation/release semantics. Phase 8
canonical docs/State tip
`470667d37863384edadb3d93ef64d8004aff46a3` includes the P8-E promotion
`d95b60d174cb0b17df09e2775b3cbd134c74b21f`; `77f3e1a47a1e007492a794ea777d681a21a36d09`
is the earlier review tip. P20's synthetic shared-activity contract consumes
no P8 travel capability. P9-A (`988b6f5d14e12359e93464bae5e0048ca970ad86`)
and P11 Actor Choice (`0803670cfa2c39163b54ff46a21daa06df5a16f6`) are
upstream-irrelevant to this synthetic operation. The architecture baseline remains
`c285466c355103d3637ac165246591b72eb7bda0`, with both alignment records
current. Activity instance identity remains independent of participant identity
and supports the architecture's one-or-more participant cardinality.
**Authority:** `docs/SIMULATION_ARCHITECTURE.md` §§11–12, 91–93;
`docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`; the Phase 20 Brief and entry
proposal (refreshed at `ff8908ff81f53c6392535f6a23d0bd954b86220b`);
reviewed P18-A, P18-B, and P18-C technical designs; and both dated
architecture alignment records.
**Status:** The technical design at candidate `6a0d164` passed independent
review. This candidate clarifies when partial formation becomes terminal;
that clarification is pending independent refresh review. Checkpoint `P20-A —
Synthetic Multi-participant Operation` remains proposed separately in
`PHASE20_P20A_CHECKPOINT_PROPOSAL.md`; its ID and scope have not been accepted.
No implementation authorization, capability promotion, or persistence schema
is granted.
**Independent technical-design review history:** Earlier PASS at content commit
`a85ab673c41154b7ac9be3943b3e0f2cba2c41e7` after the decline lifecycle
mapping correction and refreshed PASS at `3c69fee`. The current P20 technical
design at `6a0d16494735853ce35a8974ab348551650afd6b` was independently reviewed
against promoted P18-A/B/C and P18 State `bcb3f67`; the latest P18 State-only
tip `311baa9` records that PASS and does not change relevant contracts. Review
confirmed the pre-schedule Proposed instance, atomic full-set commitments,
sealed-input start bound, and separate activity/participant identity. This
checkpoint proposal adds no new simulation semantics; its own documentation
diff still requires independent review before publication.

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
| Definition/version, stable instance ID, lifecycle/revision, participant relations and commitments | Promoted P18-B activity domain. Instance identity is independent of definition, `PersonId`, and `NpcRuntime`. `TrySchedule` atomically schedules a full nonempty participant set; it has no partial-acceptance scheduling API. Its rebuildable P18-A descriptor resolves the authoritative instance/revision before transition. |
| Each Person's accept/decline proposal and decision boundary | Promoted P18-C provides `PersonId`-keyed decision request/proposal types, Knowledge-bounded snapshot/planner ports, and executor result semantics. Its current coordinator creates requests from committed P18-B transition receipts; it exposes no general P20 request-enqueue API or durable formation-decision store. P20 must define the supported request trigger/adapter and compose individual outcomes into a separate formation authority; one Person cannot decide for another. Current truth and commitments are revalidated at execution/start. |
| Individual availability and commitment facts | P18-B and its individual availability authority own these facts; P18-C consumes committed transition receipts after successful advance and queries current availability. P20 must design how per-Person decisions/reservation requests are retained and how the complete set reaches `TrySchedule`; the P20 coordinated write is not a delivered B/C API. |
| Synthetic operation preconditions and participant results | Proposed test-only synthetic domain authority. P20 must define the all-or-none result transaction and couple it with lifecycle transition through a reviewed API; this does not exist in the promoted B start validator. |

P18-A's due-work identity names the activity instance/domain transition, never
an implicit single actor. P18-B's participant relation remains stable-ID based
and its promoted `TrySchedule` accepts a nonempty participant set, rejects
duplicate/blank IDs, and atomically creates commitments for the full supplied
set; it imposes no universal fixed cardinality. P18-C's decision
request/proposal is individually keyed by `PersonId` and may reference an
activity instance through stable semantic boundary data without making the
instance an actor-owned child. Definition, instance, and each participant
relation carry compatible stable semantic IDs; runtime references and
collection order are not causal identity.

P18-B's promoted ActivityLifecycle composition and owner-dispatch path is the
bounded lifecycle/dispatch capability P20 can build on. P18-C's promoted
coordinator drains P18-B transition receipts only after successful timeline
advance, checks current lifecycle availability, plans from a Knowledge-bounded
snapshot, and retries an uncommitted execution with the same stable proposal
identity; committed rejections consume that request. The coordinator currently
derives requests from lifecycle receipts and exposes no general request
injection API or durable P20 formation-decision storage. This does not provide
P20-specific partial participant decisions, all-required-set formation, or
shared outcome semantics. P20 must design and review its request
trigger/adapter, partial per-Person decision retention, full-set submission to
`TrySchedule`, decline/cancel/release and stale-retry interfaces, and a
coordinated lifecycle-plus-effect transaction. The existing start validator
returns a bool/disposition and cannot atomically couple `Scheduled → Active`
with P20 effects. P20 must not
introduce a parallel lifecycle dispatcher or assume owner dispatch supplies
that cross-participant transaction.

## 3. Bounded formation and commitments

The fixture creates one supported operation definition/version and uses
P18-B `TryPropose` to create one stable `Proposed` activity instance before
participant decisions. Its identity is allocated from a deterministic
accepted input/owner operation identity or persisted domain sequence, as
specified by P18-B. It arranges exactly two distinct required `PersonId`s in
stable semantic order. Duplicate identities reject the proposal without
mutation. There are no roles or optional participants in this fixture.

Each required Person independently receives an accept-or-decline opportunity.
The decision reads only that Person's allowed Knowledge and records its
decision identity, logical boundary, and relevant source revision. P20 needs a
bounded owner/API to retain these partial per-Person decisions; P18-B does not
expose partial-acceptance scheduling. A partial response set, including a
recorded decline, remains pending on the P18-B `Proposed` instance until an
explicit bounded formation-close attempt. There is no timeout or implicit
close. At that attempt, a still-missing or declined required decision maps to
P20 `NotFormed`; this is not a P18-B lifecycle state or receipt. The existing
instance remains `Proposed`, with no Schedule receipt, active commitments, or
start/completion due work. A missing decision must not be treated as
acceptance. No acceptance is inferred from another participant, the proposer,
a roster, shared context, or the UI.

Each acceptance records only that Person's decision and reservation intent for
the common half-open fixture interval `[start, end)`, using integer P18-A ticks
and checked range. It does not create an active P18-B commitment. On a
formation-close attempt where every required Person has accepted, P20
revalidates the full accepted set and current eligibility, then calls P18-B
`TrySchedule` once with the complete participant set and interval.
`TrySchedule` is the authority that atomically validates
conflicts and installs all commitments, due work, participant relations, and
the Scheduled transition/receipt. P20 must not require matching active
commitments before this call because no P18-B partial-reservation API exists.
If a new conflict intervenes after P20's revalidation, `TrySchedule` fails
coherently (for example `ParticipantConflict`): no participant is committed,
no due work is published, and P20 records `NotFormed` before scheduling. The
already-created P18-B instance remains `Proposed` with no Schedule receipt,
active commitments, or start/completion due work. P20 must preserve this
Proposed/unscheduled lifecycle fact; it must not imply the instance was never
created or claim a Scheduled receipt/commitment.

When accepted actor decisions reach formation through P18-C, they are handed
off only after a successful advance. Therefore any timed start must satisfy
the promoted P18-C/P18-A sealed-input rule. Let `sealedThrough` be
`InputsSealedThrough ?? CurrentInstant`; require
`start > max(CurrentInstant, sealedThrough)`, with earliest permitted start
`checked(max(CurrentInstant, sealedThrough) + 1 tick)`. If the proposed start
is at or before this bound, or computing the next tick overflows, P20 records
`NotFormed` atomically in its formation state and does not call `TrySchedule`;
the P18-B instance remains
`Proposed` without a Schedule receipt, commitments, or due work. P18-B's
`start >= now` check alone does not enforce this post-advance bound.

This proof uses one common interval to keep its validation surface bounded.
It does not require all future activities or role-specific commitments to use
the same interval. Interval overlap and incompatibility are decided by the
existing availability authority; P20 adds no general reservation solver.

## 4. Scheduled start, effects, and terminal handling

At the scheduled instant, P18-A dispatches the stable instance start reference
in deterministic causal order. P18-B's start validator returns a
bool/disposition; P20's current-truth validation must be composed into this
decision so a rejected start follows P18-B's FailedStart path. The validator
alone cannot atomically couple the `Scheduled → Active`
transition with P20's synthetic effect. P20 needs a reviewed coordinated
owner/API transaction for that coupling. P18-B commitments cover the half-open
interval `[start, end)`: they remain present through both Scheduled and Active
states, and a successful start does not consume or release them. Completion or
terminal cancellation/interruption releases them. The complete-set validation must
confirm:

- the instance remains Scheduled at that revision and has exactly the two
  distinct required Persons for this fixture;
- both independent acceptances and their reservation intents remain present,
  and P18-B holds the full scheduled commitment set;
- both Persons remain eligible under current domain truth at the exact start
  instant, and P18-B still holds each matching commitment for this instance
  and fixture interval (the actors' own scheduled commitments are expected to
  make them unavailable to other activities during that interval);
- the operation's current factual preconditions hold; and
- the start/effect idempotency identity has not already committed.

If validation succeeds, the synthetic domain authority computes results from
current facts and explicit operation inputs. P20 must design a reviewed
all-or-none API transaction that couples the `Scheduled → Active` lifecycle
state and Start receipt with both participant results/effects. The current B
start validator does not provide this effect coupling. Participant iteration
and result publication use ascending semantic `PersonId` order. Retry cannot
publish one result without the other or apply an effect twice. Successful
start leaves both P18-B commitments installed until completion/termination.
The actual start instant equals the due instant. Completion at the planned end
is a P18-B lifecycle transition; it is not a second operation effect. This
test-only boundary does not establish a cross-domain transaction framework.

P20 terminal labels map to the existing P18-B lifecycle facts as follows:

| P20 outcome | Timing | P18-B lifecycle/receipt and commitment behavior |
|---|---|---|
| `NotFormed` | At explicit formation close with a still-missing/declined decision, or before `TrySchedule` succeeds due to stale revalidation, participant conflict, a late/sealed start, or tick overflow | The P18-B instance remains Proposed with no Schedule receipt, active commitments, or start/completion due work. Record the P20 formation disposition atomically. |
| `FailedToStart` | Scheduled start validation or P20 current-truth validation fails | P18-B state becomes `Cancelled` and emits `FailedStart`; start/completion due work for the instance is invalidated and all its commitments are released. The P20 label is a disposition, not a new lifecycle state. |
| `Cancelled` | Explicit cancellation after scheduling | P18-B state becomes `Cancelled` and emits `Cancel`; due work is invalidated and all instance commitments are released. |
| `Started` | Coordinated scheduled-start validation succeeds | P18-B state becomes `Active` and emits `Start`; P20 effects commit together, and commitments remain installed through the Active interval. |

P18-B already commits its own lifecycle transition, receipt, due-work update,
and commitment release coherently for FailedStart and explicit cancellation.
However, P20 has no delivered API that atomically couples those facts with
P20-owned effects/results and its terminal outcome record. P20's proposed
coordinated owner/API transaction must commit the applicable lifecycle state
and receipt, effect/result disposition, commitment release, and due-work
invalidation as one coherent outcome; a failed transaction publishes none of
the P20 effect/result and cannot leave stale work or held commitments. For a
successful start, that same boundary couples Active state/Start receipt with
the all-or-none effects and retains commitments. These composition APIs and
cross-authority guarantees are P20 design work, not delivered P18-B/C
capabilities. Do not infer B already supplies this combined behavior or
partial proposals. The proof adds no recruitment, withdrawal after start,
mid-execution roster change, or generic interruption policy.

## 5. Determinism and failure behavior

The same compatible definition/version, instance and participant IDs, logical
instant/calendar, effective configuration, current authoritative inputs,
deterministic random context if actually consumed, and ordered accepted inputs
must yield the same decisions, formation, start disposition, and individual
results. Use persisted semantic sequences and stable IDs; do not depend on
dictionary/set enumeration, materialization, UI state, host time, registration
order, or `NpcRuntime` presence.

Planning and read-only queries do not mutate state. P18-B skips stale owner
nodes without consuming the dispatch cap. P20 must still define and targetedly
validate stale instance/revision resolution, invalidation and retry behavior
for its descriptors and proposed transactions; the promoted scheduler behavior
does not by itself establish P20 owner-state semantics. P20's required
transaction must publish no partial reservation, lifecycle, sequence
allocation, effect, or success signal on failure. Cancellation/failure
dispositions and availability notification timing remain part of the P20 API
design. P18-C's one-attempt-per-actor/causal-boundary and uncommitted retry
semantics are promoted constraints. P20 must validate composed multi-person
requests against them: terminal committed rejections consume a request, while
uncommitted execution retries the same stable proposal identity.

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
- each participant's accepted decision and reservation intent before
  scheduling; after scheduling, each authoritative commitment's participant,
  interval, owning source revision, and availability facts needed to validate
  it;
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

- existing one-participant P18 instances remain valid and preserve their
  lifecycle and due-work behavior without P20-only formation, coordination, or
  effect requirements; promoted B accepts any nonempty participant set while
  rejecting duplicate/blank participant IDs, and P20's exact-two condition is
  only this synthetic proof's fixture policy;
- independent decisions and Knowledge boundaries for two distinct Persons;
- partial per-Person decisions and reservation intents remain Proposed without
  active commitments until an explicit bounded formation-close attempt; a
  still-missing or declined decision at close records `NotFormed` without
  scheduling;
- at close with every required Person accepted, P20 revalidates and submits the
  complete nonempty set through P18-B `TrySchedule`; empty or incomplete sets
  cannot create a Scheduled instance, and duplicate PersonId or reservation
  conflict cannot partially mutate it;
- deterministic ordering under participant insertion/materialization
  permutations;
- the complete set is revalidated before `TrySchedule`, which creates both
  commitments and the stable instance's due work atomically; an intervening
  conflict leaves the instance Proposed with no commitments or due work and
  yields P20 `NotFormed` while preserving that Proposed instance;
- after the successful-advance P18-C handoff, with
  `sealedThrough = InputsSealedThrough ?? CurrentInstant`, a timed start is
  strictly later than `max(CurrentInstant, sealedThrough)`; a sealed/late
  start or checked next-tick overflow records `NotFormed` without scheduling,
  commitments, or due facts;
- the P18-B bool/disposition start validator is mapped to a P20 reviewed
  `Scheduled → Active` plus two-result transaction; failure/retry cannot create
  partial lifecycle or duplicate effects;
- a successful start retains commitments throughout `[start, end)`; completion,
  FailedStart, and explicit cancellation release them, with FailedStart mapping
  to P18-B `Cancelled` + `FailedStart` receipt and explicit cancellation to
  `Cancelled` + `Cancel` receipt, while due-work invalidation is coherent;
- P18-A same-instant ordering and non-reentrant dispatch, P18-B full-set
  `TrySchedule`/stale-node behavior, and P18-C Knowledge/availability/retry
  behavior remain intact; and
- stable identity and causal state survive clone/dormancy/materialization
  permutations within the later supported runtime scope.

The completed independent design review compared these obligations to actual
promoted P18-A/B/C code at `18ecc6e`. Before implementation, the P20
partial-decision
API, complete-set schedule call, decline/cancel/release handling, P20 stale
retry composition, and atomic lifecycle/effect transaction are required
design/API work, not delivered P18-B/C capabilities. The current P20 documents
define candidate contracts only; their existence does not establish runtime
capabilities.

## 8. Dependency gate and exclusions

P20 runtime implementation has its relevant P18-A timeline/scheduler,
P18-B lifecycle, and P18-C availability/decision prerequisites promoted; the
latest canonical State is `311baa9`, with code sources listed above. The former
P18-C promotion gate is cleared. P20-A remains gated on explicit checkpoint
acceptance and independent review of the current formation-close clarification.
The prior technical-design review remains recorded at its cited candidate. Its P20-owned
APIs/transactions must still be implemented and independently reviewed as
described above; the proposal creates no promoted capability. P18-B stale-owner skipping and bounded lifecycle
composition are available; P18-C transition receipts and post-advance
actor-decision coordination are available. P20 does not wait for P18-D or P19,
and no blanket dependency on either is introduced. P18 does not depend on P20,
so no dependency cycle is introduced.

During implementation and candidate review, verify P20's stable instance/revision
resolution, due-work invalidation/retry assumptions, coordinated required-set
validation, and atomic lifecycle/effect composition against exact promoted
A/B/C APIs. Skipping a stale node without consuming the dispatch cap is a
scheduler behavior, not proof that P20's stale owner descriptor or multi-owner
transition is correct. No incompatibility is currently asserted. Both current
alignment records remain active review constraints: use stable semantic
identity and deterministic composition; preserve ActivityInstanceId
independently from PersonId and participant cardinality; keep Unity
presentation out of domain authority; and defer public Mod API/loader
mechanics to P19. Neither alignment adds a blanket P18-D or P19 dependency to
P20.

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

No additional product choice is identified within the entry-approved scope.
The prior technical-design review records the API and transaction contracts;
this candidate's formation-close timing clarification awaits independent
review. Code-level mapping and behavior remain subject to independent
implementation review against current promoted P18 capabilities. The proposed
P20-A checkpoint is not accepted and authorizes no implementation.
