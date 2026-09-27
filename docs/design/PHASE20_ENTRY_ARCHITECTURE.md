# Phase 20 Entry Architecture — bounded multi-participant proving slice

**Status:** `ENTRY_ARCHITECTURE_READY`; independent entry review **PASS**. This
document proposes a bounded decomposition and does not authorize implementation,
define P20 checkpoint IDs, or promote any capability. It is subordinate to
`SIMULATION_ARCHITECTURE.md` §§11–12, 91–93 and the Phase 20 Brief.

## Purpose and readiness

The proposal makes the Phase 20 objective concrete enough for bounded technical
design: a temporary activity instance coordinates two independently deciding
Persons around one scheduled synthetic operation. The fixture is deliberately
small. Its two-Person requirement and lack of differentiated roles apply only to
this proving slice; they do not establish universal activity cardinality, role
policy, or an arrangement catalog.

Current P18 canonical tip `18ecc6e56d3c6303edfaf8a38257355d262a6ea5`
promotes P18-A (`985c56c`), P18-B
(`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`), and P18-C
(`ab05ecfe976e80badf6f509b8e9be25ff556ca23`). All relevant A/B/C
capabilities are now promoted. P18-C provides per-Person availability-driven
decisions from lifecycle receipts but no general request-enqueue API or
durable P20 formation-decision store; those remain explicit P20 adapter/API
work. P18-A/B/C contracts inform entry and technical design. P18-B includes
stale-node skipping without consuming the
dispatch cap and the bounded ActivityLifecycle composition/owner-dispatch
path. P18-B's `TrySchedule` is an atomic full, nonempty participant-set
operation; it does not expose a partial-acceptance scheduling API. Its start
validator returns a bool/disposition and cannot atomically couple P20's
synthetic effect with `Scheduled → Active`. P20 must design/review these
missing interfaces and transaction boundary rather than treating them as
delivered B behavior. P20 must also revalidate stable instance/revision
references and due-work invalidation against the promoted APIs. Runtime
execution remains `WAIT_DEPENDENCY` on independently reviewed P20
formation/transaction APIs and targeted revalidation against promoted A/B/C;
the former P18-C promotion gate is cleared. P18 work does not wait for P20, so this adds no P18 → P20 cycle. There
is no blanket dependency on all P18-D migrations, P19, P8 travel, P12/P13
persistence, or a persistent Group/Organization.

## Bounded proving fixture

Use the Phase 20 Brief's permitted **synthetic test operation**. It has no
gameplay meaning and introduces no robbery, travel, work, social, military, or
other production consumer. A test arranges two distinct `PersonId` participants
and an operation definition. The concrete activity instance receives a stable
identity independent of either Person, the proposer, or any materialized
`NpcRuntime`; its definition and instance remain separate.

The selected proving rule is a single required participant set of exactly two
distinct Persons, with no role distinction. It keeps the validation surface
bounded while proving that instance identity and coordination are not owned by
one participant. No general role schema or policy for other activities follows
from this fixture.

Each Person receives an independent accept-or-decline decision opportunity.
The fixture records each decision against that Person's semantic identity; one
Person cannot accept, decline, or recruit on behalf of the other. Decisions use
only the deciding Person's permitted Knowledge. Either decline leaves the
instance unformed and not executable.

Each individual acceptance is retained as a P20-owned decision plus a
noncommitting reservation intent for that Person and proposed interval. The
intent records what the participant accepted; it does not create an active
P18-B commitment or reserve availability. Once the complete required set has
accepted, P20 revalidates it and submits the whole set atomically to P18-B
`TrySchedule`, which transitions the existing `Proposed` instance to
`Scheduled` and atomically creates all participant commitments and start due
work. An intervening conflicting commitment makes full-set scheduling fail
coherently; P20 records `NotFormed`, while the existing instance remains
`Proposed` with no Schedule receipt, commitments, or due work. Both participants
may use the same fixture interval for simplicity; this is a scoped test choice,
not a universal activity/role rule.
Acceptance, reservation intent, committed availability, and execution remain
distinct facts.

## Formation, scheduled start, and terminal outcomes

P18-B `TryPropose` creates the stable `Proposed` instance before participant
decisions. The intended proving outcome schedules only after both distinct
required Persons have accepted. Their retained reservation intents are not
active commitments; P20 revalidates the full set and submits that existing
Proposed instance and complete participant set to P18-B `TrySchedule`, whose
transaction transitions it to `Scheduled` and atomically creates all matching
participant commitments and start due work. Partial decision retention
remains P20-owned; P18-B exposes no partial-set scheduling API. On decline or
an intervening commitment conflict, P20 records `NotFormed`; the P18-B
instance remains `Proposed` with no Schedule receipt, active commitments, or
start/completion due work. The existing P18 timeline/scheduler owns the
logical start boundary; the activity layer does not create a second clock or
authoritative agenda.

Because accepted actor decisions are handed off only after a successful P18-C
advance, a timed start must satisfy the promoted C/A sealed-input rule:
`start > max(CurrentInstant, InputsSealedThrough ?? CurrentInstant)`, using
one checked logical tick beyond that maximum. A tick overflow or proposed
start at or before this bound records P20 `NotFormed` and must not partially
schedule the P18-B instance or publish commitments/due work. P18-B's weaker
`start >= now` check is not sufficient for this post-advance handoff.

At that boundary, P18-B's start validator supplies a bool/disposition result;
it does not transactionally couple `Scheduled → Active` with a P20 operation
effect. `ActivityLifecycleStore` retains each commitment while the instance
is `Scheduled` or `Active` for its planned interval `[start, end)`; successful
start does not consume or release commitments. Completion, cancellation, or
interruption releases them and invalidates pending due work through the
existing lifecycle owner. P20 needs a reviewed coordinated owner/API boundary
that validates the complete required set and publishes the lifecycle
transition plus both participant-specific synthetic results together, or
publishes neither. Its checks must include:

- the instance is still scheduled and both required `PersonId`s remain present;
- both accepted decisions, reservation intents, and P18-B commitments match
  the instance and required interval;
- current factual availability and operation preconditions still hold for each
  required participant; and
- the operation has not already started or applied its effects.

That atomicity is a P20-required API/transaction design, not current P18-B
behavior. A stale or conflicting participant state must not start half the
activity or publish only one participant's effect. Participant iteration and
any result ordering must be deterministic by a
semantic order, such as stable `PersonId` order where order is otherwise
irrelevant.

The fixture requires explicit dispositions, but the transitions below are
unresolved P20 interface and transaction work; they are not implied by
`TrySchedule` or the bool/disposition start validator:

| Condition | P20 outcome and P18-B mapping | Commitment / due-work result |
|---|---|---|
| Either participant declines before full-set scheduling | P20 `NotFormed`; the existing P18-B instance remains `Proposed` and has no Schedule receipt. | No active commitment or start/completion due work is created. |
| A required participant or matching commitment is missing at scheduled start | P20 disposition `FailedToStart`; P18-B state `Cancelled` with `FailedStart` receipt. | The coordinated terminal transition releases commitments and invalidates pending start/completion work. |
| Revalidation finds stale/unavailable/conflicting state | P20 disposition `FailedToStart`; P18-B state `Cancelled` with `FailedStart` receipt; apply no operation effect. | Release all instance commitments and invalidate pending due work in the same coordinated P20 boundary. |
| Explicit cancellation before or during execution | P20 outcome `Cancelled`; P18-B state `Cancelled` with `Cancel` receipt. | The coordinated terminal transition releases commitments and invalidates pending due work. |
| Both participants pass coordinated start validation | P20 outcome `Started`; P18-B state `Active` with `Start` receipt. | Keep commitments active throughout the planned interval; lifecycle completion/termination releases them. |

The coordinated P20 transaction/API described here is a required design
capability, not an existing P18-B API. It must atomically couple the exact
P18-B lifecycle state/receipt and due-work invalidation with P20 participant
effects/disposition; it does not reinterpret P18-B's existing ownership. These
are target outcomes for the proving slice, subject to P20 API design and
review. It does not support automatic recruitment, activity retry, withdrawal
after start, mid-execution composition changes, or rollback of already-applied
effects. Stale descriptor invalidation/retry behavior remains required P20
design and targeted validation. Cancellation and failed start preserve the
instance's causal outcome; they do not erase prior decisions or silently
rewrite history.

## Effects and authority

The synthetic operation produces a shared execution context, but keeps each
Person's result distinct. Its test-only domain authority computes/applies one
participant-specific result for each accepted participant from current facts
and the inputs explicitly supplied to that operation. It must not read another
Person's hidden Knowledge or make outcomes identical merely because execution
is shared.

The promoted ActivityLifecycle composition/owner-dispatch path coordinates
lifecycle dispatch; it does not supply P20's all-or-none coupling of the
`Scheduled → Active` transition with a synthetic effect. P20 design must
specify that bounded API/transaction for this fixture. It does not imply a
universal effects engine or authority to mutate arbitrary domain truth. A
future consumer spanning multiple authorities needs its own reviewed coherent
commit contract; this entry proposal does not invent cross-domain
transactions.

## Causal state and determinism

Any later implementation/design must retain or unambiguously reconstruct the
compatible operation definition/version, stable activity instance ID, required
participant IDs and decisions, reservation intervals, scheduled boundary,
lifecycle outcome, operation inputs, and each participant effect already
applied. Reconstructible indexes are derived. Exact persistence format is out
of scope.

For equivalent authoritative state, effective configuration/calendar,
compatible content, deterministic randomness, and ordered inputs, formation,
start validation, outcomes, and participant effects must be deterministic.
Materialization, load order, UI state, dictionary iteration, or incidental host
order cannot select a participant or result.

## Design and review gates

Before any implementation is considered, bounded technical design must map the
proposed transitions onto promoted P18-A/B/C capabilities and specify
noncommitting partial reservation intents, complete-set `TrySchedule`
submission, intervening commitment conflict, stale revision invalidation/retry,
decline and cancel/release, and the coordinated `Scheduled → Active` plus
synthetic-effect transaction. P18-B's bool/disposition start validator does
not provide effect coupling. Targeted validation must cover these APIs/transactions, deterministic
ordering, and reconstruction inputs. Independent
technical review must pass before implementation readiness. Independent entry
review of this proposal passed on the entry candidate; it is not a substitute
for that later technical review.

The two-Person fixture, role-free formation, common test interval, and
test-owned all-or-none effect boundary are recommended bounded choices within
the accepted Brief semantics. The current architecture and Brief leave no
material product decision necessary to make this entry proposal. Exact APIs,
schemas, general role/count policies, and any real consumer remain technical or
future consumer design questions.

## Explicit exclusions

This proposal adds no persistent Group/Organization, game mechanic, general
workflow/planner/negotiation system, universal Activity or effect engine,
mod loader/registry, P19 adapter, save schema, travel integration, multi-worker
consumer, or War/unit scheduling. It does not change the architecture, roadmap,
Phase 20 Brief, or Phase State.
