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

Current P18 canonical tip `3d4fe829f4be41fc9e9bb11052a320c3eb00d94d`
promotes P18-A (`985c56c`) and P18-B
(`97918cbbe4238a65a216b1a1f0ef84c70b4d080c`). P18-C design candidate
`97b97c5` remains unpromoted. Promoted P18-A/B contracts inform entry and
technical design. P18-B includes stale-node skipping without consuming the
dispatch cap and the bounded ActivityLifecycle composition/owner-dispatch
path. P18-B's `TrySchedule` is an atomic full, nonempty participant-set
operation; it does not expose a partial-acceptance scheduling API. Its start
validator returns a bool/disposition and cannot atomically couple P20's
synthetic effect with `Scheduled → Active`. P20 must design/review these
missing interfaces and transaction boundary rather than treating them as
delivered B behavior. P20 must also revalidate stable instance/revision
references and due-work invalidation against the promoted APIs. P18-C availability/decision
capability remains unpromoted; runtime execution remains `WAIT_DEPENDENCY` on
relevant P18-C capability and independently reviewed P20 API/transaction
design, in addition to revalidation against promoted A/B. P18 work does not
wait for P20, so this adds no P18 → P20 cycle. There
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

The exact relationship between each acceptance and reservation remains a P20
design question. P18-B `TrySchedule` accepts an atomic full nonempty set; it
does not provide partial acceptance. P20 must specify whether individual
decisions/commitment requests are retained in a separate proposal owner and
how a complete accepted set is assembled and submitted to `TrySchedule`.
Both participants may use the same fixture interval for simplicity; this is a
scoped test choice, not a universal activity/role rule. Acceptance,
reservation, availability at start, and execution remain distinct facts.

## Formation, scheduled start, and terminal outcomes

The intended proving outcome schedules only after both distinct required
Persons have accepted and matching reservations are present. P20 still needs
an API/design for retaining partial per-Person decisions and submitting the
complete set to P18-B `TrySchedule`; partial Proposed state is not an existing
P18-B scheduling capability. The existing P18 timeline/scheduler owns the
logical start boundary; the activity layer does not create a second clock or
authoritative agenda.

At that boundary, P18-B's start validator supplies a bool/disposition result;
it does not transactionally couple `Scheduled → Active` with a P20 operation
effect. P20 needs a reviewed coordinated owner/API boundary that validates the
complete required set and publishes the lifecycle transition plus both
participant-specific synthetic results together, or publishes neither. Its
checks must include:

- the instance is still scheduled and both required `PersonId`s remain present;
- both decisions and reservations match the instance and required interval;
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

| Condition | Instance outcome | Reservation outcome |
|---|---|---|
| Either participant declines | `NotFormed` | Release any reservation already made for this instance. |
| A required participant or matching reservation is missing at scheduled start | `FailedToStart` | Release both instance reservations as one transition. |
| Revalidation finds stale/unavailable/conflicting state | `FailedToStart` | Release both instance reservations as one transition; apply no operation effect. |
| Explicit cancellation before start | `Cancelled` | Release both instance reservations as one transition. |
| Both participants pass coordinated start validation | `Started` | Consume/close the start reservations according to the P18 commitment contract. |

These are target outcomes for the proving slice, subject to P20 API design and
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
proposed transitions onto promoted P18-A/B contracts and the P18-C design, then
specify partial-decision retention, complete-set `TrySchedule` submission,
reservation conflict/rollback, stale revision invalidation/retry, decline and
cancel/release, and the coordinated `Scheduled → Active` plus synthetic-effect
transaction. P18-B's bool/disposition start validator does not provide effect
coupling. Targeted validation must cover these APIs/transactions, deterministic
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
