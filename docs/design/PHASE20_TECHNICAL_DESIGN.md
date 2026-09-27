# P20 — Multi-participant Synthetic Operation Technical Design

**Design base:** `97b97c5c7523f39f3645bc018c82dbbab633648f` on
`codex/phase20/MultiParticipantTechnicalDesign`, descended from canonical
architecture `c285466c355103d3637ac165246591b72eb7bda0`.
**Prior P18 baseline:** P18-A (`0b52898a479fe48ea8fb2fd7b2c43af82445f26c`)
was promoted; P18-B/C were then unpromoted. This is historical context only.
**Current canonical impact revalidation:** P18 canonical tip
`3d4fe829f4be41fc9e9bb11052a320c3eb00d94d` promotes P18-A from source
`985c56c` and P18-B from source
`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`. P18-C design candidate remains
`97b97c5`; C capability is unpromoted.
P18-B includes stale-node skipping without consuming the dispatch cap and its
bounded ActivityLifecycle composition/owner-dispatch path. P20 may rely on
those promoted capabilities, while still revalidating its stable instance/
revision references, due-work invalidation/retry behavior, and coordinated
transition integration against the actual A/B APIs. P20 runtime therefore
remains `WAIT_DEPENDENCY` on the unpromoted relevant P18-C capability and
independently reviewed P20 APIs/transactions; P18-A/B are available. Phase 8
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
proposal; reviewed P18-A, P18-B, and P18-C technical designs; and both dated
architecture alignment records.
**Status:** Proposed technical design only. No implementation authorization,
checkpoint IDs, capability promotion, persistence schema, or Phase State change.
**Prior independent technical design review:** PASS at content commit
`a85ab673c41154b7ac9be3943b3e0f2cba2c41e7` after the decline lifecycle
mapping correction. This refresh adds an explicit single-participant
compatibility validation and current P8-E impact note. Independent refreshed
re-review **PASS** on content commit `3c69fee`; the targeted review also
confirmed current P8-E status, P18-A/B/C identity and cardinality seams, and no
P18-D/P19/travel blanket dependency. That review predates P18-A/B promotion
and the current P8 docs tip. This refresh itself does not approve
implementation or create checkpoint IDs; refreshed independent review remains
required.

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
| Each Person's accept/decline proposal and decision boundary | P18-C using that Person's stable `PersonId` and own permitted Knowledge. A separate decision for each required participant is recorded; one Person cannot decide for another. Current truth and commitments are revalidated at execution/start. |
| Individual availability and commitment facts | P18-B and its individual availability authority own these facts. P20 must design how per-Person decisions/reservation requests are retained and how the complete set reaches `TrySchedule`; the P20 coordinated write is not a delivered B API. |
| Synthetic operation preconditions and participant results | Proposed test-only synthetic domain authority. P20 must define the all-or-none result transaction and couple it with lifecycle transition through a reviewed API; this does not exist in the promoted B start validator. |

P18-A's due-work identity names the activity instance/domain transition, never
an implicit single actor. P18-B's participant relation remains stable-ID based.
P18-C's actor decision state remains individually keyed by `PersonId` and may
reference the instance without making it an actor-owned child. Definition,
instance, and each participant relation carry compatible stable semantic IDs;
runtime references and collection order are not causal identity.

P18-B's promoted ActivityLifecycle composition and owner-dispatch path is the
bounded lifecycle/dispatch capability P20 can build on. P20 must still design
and review partial per-Person decision retention, full-set submission to
`TrySchedule`, decline/cancel/release and stale-retry interfaces, and a
coordinated lifecycle-plus-effect transaction. The existing start validator
returns a bool/disposition and cannot atomically couple `Scheduled → Active`
with P20 effects. P20 must not introduce a parallel lifecycle dispatcher or
assume owner dispatch supplies that cross-participant transaction.

## 3. Bounded formation and commitments

The fixture creates one supported operation definition/version and one
Proposed activity instance with a stable identity allocated by a deterministic
accepted input/owner operation identity or persisted domain sequence, as
specified by P18-B. It arranges exactly two distinct required `PersonId`s in
stable semantic order. Duplicate identities reject the proposal without
mutation. There are no roles or optional participants in this fixture.

Each required Person independently receives an accept-or-decline opportunity.
The decision reads only that Person's allowed Knowledge and records its
decision identity, logical boundary, and relevant source revision. P20 needs a
bounded owner/API to retain these partial per-Person decisions; P18-B does not
expose partial-acceptance scheduling. A decline's mapping to a P20
`NotFormed` disposition and any reservation release are unresolved P20
transition/transaction design, not an existing P18-B decline API. `NotFormed`
is a P20 outcome label, not a new P18-B lifecycle state. A missing decision
must not be treated as acceptance. No acceptance is inferred from another
participant, the proposer, a roster, shared context, or the UI.

Acceptance requests that Person's commitment for the same half-open future
fixture interval `[start, end)`, with integer P18-A ticks and checked range.
The per-Person decision/commitment mutation and conflict behavior are
unresolved P20 API/transaction work. P20 must define how a partial decision
and its reservation request are retained without scheduling the activity.
Once both decisions and matching reservations form a complete nonempty
required set, P20 submits the whole set to P18-B `TrySchedule`, whose promoted
contract atomically schedules a full participant set. P20 must revalidate the
actual revision and due-work result and define coherent failure behavior for
conflict or stale set; it cannot assume a per-Person partial scheduling API.

This proof uses one common interval to keep its validation surface bounded.
It does not require all future activities or role-specific commitments to use
the same interval. Interval overlap and incompatibility are decided by the
existing availability authority; P20 adds no general reservation solver.

## 4. Scheduled start, effects, and terminal handling

At the scheduled instant, P18-A dispatches the stable instance start reference
in deterministic causal order. P18-B's start validator returns a
bool/disposition; it cannot atomically couple the `Scheduled → Active`
transition with P20's synthetic effect. P20 needs a reviewed coordinated
owner/API transaction for that coupling. Its complete-set validation must
confirm:

- the instance remains Scheduled at that revision and has exactly the two
  distinct required Persons for this fixture;
- both independent acceptances and matching reservations remain present;
- both Persons remain eligible and available under current domain truth at the
  exact start instant, and each commitment still matches the fixture interval;
- the operation's current factual preconditions hold; and
- the start/effect idempotency identity has not already committed.

If validation succeeds, the synthetic domain authority computes results from
current facts and explicit operation inputs. P20 must design a reviewed
all-or-none API transaction that publishes P18-B's `Scheduled → Active`
transition and both results together; the current B start validator does not
provide this effect coupling. Participant iteration and result publication
use ascending semantic `PersonId` order. The transaction must make retry
unable to publish one result without the other or apply an effect twice. The
actual start instant equals the due instant. Completion at the planned end is
a P18-B lifecycle transition; it is not a second operation effect. This
test-only boundary does not establish a cross-domain transaction framework.

P20's dispositions for missing/stale participants, failed start, explicit
cancellation, decline, and reservation release remain unresolved interfaces
and transaction design. P20 must define how each maps to promoted B lifecycle
transitions, invalidates due work through stable instance revision, releases
matching commitments coherently, and preserves decisions and causal outcomes.
Do not assume B already provides this combined behavior or partial proposals.
The proof adds no recruitment, withdrawal after start, mid-execution roster
change, or generic interruption policy.

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
design. P18-C's one-attempt-per-actor/causal-boundary and refusal/stale retry
semantics remain to be validated against the unpromoted C capability.

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

- existing one-participant P18 instances remain valid and preserve their
  lifecycle and due-work behavior without P20-only formation, coordination, or
  effect requirements;
- independent decisions and Knowledge boundaries for two distinct Persons;
- P20 can retain partial per-Person decisions without scheduling, then submit
  the complete nonempty required set through P18-B `TrySchedule`; empty or
  incomplete sets cannot create a Scheduled instance;
- one decline, one missing decision, duplicate PersonId, and reservation
  conflict each prevent formation/start without partial mutation;
- deterministic ordering under participant insertion/materialization
  permutations;
- a valid pair of commitments schedules one stable instance and start due
  fact, while stale revisions and conflicting/unavailable current truth fail
  closed;
- the P18-B bool/disposition start validator is mapped to a P20 reviewed
  `Scheduled → Active` plus two-result transaction; failure/retry cannot create
  partial lifecycle or duplicate effects;
- cancellation, decline, failed start, and stale queued work release or
  invalidate the correct commitments/work atomically;
- P18-A same-instant ordering and non-reentrant dispatch, P18-B full-set
  `TrySchedule`/stale-node behavior, and P18-C Knowledge/availability/retry
  behavior remain intact; and
- stable identity and causal state survive clone/dormancy/materialization
  permutations within the later supported runtime scope.

The design review must compare these obligations to actual promoted P18-A/B
capabilities and the still-unpromoted P18-C design before implementation. The
P20 partial-decision API, complete-set schedule call, decline/cancel/release
handling, stale retry semantics, and atomic lifecycle/effect transaction are
required design/API work, not delivered P18-B capabilities. The current
documents and candidate designs define contracts only; their existence does
not establish runtime capabilities.

## 8. Dependency gate and exclusions

P20 runtime implementation requires the **promoted P18-A timeline/scheduler
and P18-B lifecycle, the relevant P18-C availability/decision capability once
promoted, and independently reviewed P20 APIs/transactions for the gaps
described above**. P18-A/B are now promoted
at canonical tip `3d4fe829f4be41fc9e9bb11052a320c3eb00d94d`; their source
promotions are `985c56c` and
`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`, respectively. P18-B's stale-node
behavior skips stale owner references without consuming the dispatch cap, and
its bounded ActivityLifecycle composition/owner-dispatch path is available.
P20 may rely on those A/B capabilities. P18-C remains unpromoted; its design
candidate is `97b97c5`. Availability and
independent actor-decision behavior remain implementation dependencies. P20
does not wait for all P18-D migrations or P19. P18 does not depend on P20, so
no dependency cycle is introduced.

Before implementation, revalidate P20's stable instance/revision resolution,
due-work invalidation/retry assumptions, coordinated required-set validation,
and atomic lifecycle/effect composition against the promoted A/B APIs. In
particular, skipping a stale node without consuming the dispatch cap is a
scheduler behavior, not proof that P20's stale owner descriptor or multi-owner
transition is correct. No stale-node incompatibility is currently asserted;
the prior P18-A-only warning is superseded by P18-B promotion and this scoped
revalidation requirement.

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
