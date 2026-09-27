# Phase 20 Entry Architecture — bounded multi-participant proving slice

**Status:** Entry review **PASS**, including the formation-close clarification,
at docs candidate `5ea4283b0c0dc15336c3eed477dd24ef276897f4` against architecture
`c285466` and current P18 State `311baa9`. This document proposes a bounded
decomposition and does not authorize implementation, define an approved P20
checkpoint ID, or promote any capability. It is subordinate to
`SIMULATION_ARCHITECTURE.md` §§11–12, 91–93 and the Phase 20 Brief.

## Purpose and readiness

The proposal makes the Phase 20 objective concrete enough for bounded technical
design: a temporary activity instance coordinates two independently deciding
Persons around one scheduled synthetic operation. The fixture is deliberately
small. Its two-Person requirement and lack of differentiated roles apply only to
this proving slice; they do not establish universal activity cardinality, role
policy, or an arrangement catalog.

Accepted P18-A/B/C contracts may inform entry and technical design. Runtime
execution remains `WAIT_DEPENDENCY` until the relevant P18-A timeline/scheduler,
P18-B lifecycle, and P18-C availability/decision capabilities are promoted and
the bounded technical design is independently reviewed. P18 work does not wait
for P20, so this adds no P18 → P20 cycle. There is no blanket dependency on all
P18-D migrations, P19, P8 travel, P12/P13 persistence, or a persistent
Group/Organization.

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
only the deciding Person's permitted Knowledge. A partial response set remains
pending on the `Proposed` instance. A recorded decline does not by itself close
formation or make the instance terminal.

On acceptance, the fixture records that Person's decision and reservation intent
for the operation's future interval. It does not install an active commitment
for only that Person. Both participants use the same fixture interval for
simplicity. This is a scoped test choice, not a rule that every activity or role
shares one interval. Acceptance, reservation intent, authoritative commitment,
availability at start, and execution are distinct facts.

## Formation, scheduled start, and terminal outcomes

An explicit, bounded formation-close attempt evaluates the recorded decisions;
there is no timeout or implicit close. Before that attempt, a missing response
leaves the instance `Proposed` and pending. If a required response is still
missing or is declined at close, record P20 `NotFormed` while retaining the
P18-B instance as `Proposed`, with no Schedule receipt, active commitments, or
start/completion due work. If both required Persons accepted, revalidate the
complete set and call P18-B `TrySchedule` once. Only its atomic success installs
both commitments, participant relations, due work, and the Scheduled
transition/receipt. A conflict or stale pre-schedule revalidation records
`NotFormed` and leaves the instance `Proposed` without those scheduled facts.
The existing P18 timeline/scheduler owns the logical start boundary; the
activity layer does not create a second clock or authoritative agenda.

At that boundary, one coordinated validation checks, before any operation
effect:

- the instance is still scheduled and both required `PersonId`s remain present;
- both decisions and reservations match the instance and required interval;
- current factual availability and operation preconditions still hold for each
  required participant; and
- the operation has not already started or applied its effects.

Validation and start publication form one coherent transition. A stale or
conflicting participant state cannot start only half of the required activity
or publish one participant's effect before the other is accepted for start.
Participant iteration and any result ordering must be deterministic by a
semantic order, such as stable `PersonId` order where order is otherwise
irrelevant.

The fixture exposes these outcomes explicitly:

| Condition | Instance outcome | Reservation outcome |
|---|---|---|
| Formation-close attempt finds a required decision missing or declined | `NotFormed` | Preserve the `Proposed` instance and decision facts; install no commitments or due work. |
| Complete-set revalidation or `TrySchedule` fails before scheduling | `NotFormed` | Preserve the `Proposed` instance; install no commitments or due work. |
| A required participant or matching reservation is missing at scheduled start | `FailedToStart` | Release both instance reservations as one transition. |
| Revalidation finds stale/unavailable/conflicting state | `FailedToStart` | Release both instance reservations as one transition; apply no operation effect. |
| Explicit cancellation before start | `Cancelled` | Release both instance reservations as one transition. |
| Both participants pass coordinated start validation | `Started` | Retain both commitments through the Active interval; release them at the supported terminal transition. |

These are terminal outcomes for the proving slice. It does not support automatic
recruitment, retry, withdrawal after start, mid-execution composition changes,
or rollback of already-applied effects. Those require a separately bounded
consumer contract. Cancellation and failed start preserve the instance's
causal outcome; they do not erase prior decisions or silently rewrite history.

## Effects and authority

The synthetic operation produces a shared execution context, but keeps each
Person's result distinct. Its test-only domain authority computes/applies one
participant-specific result for each accepted participant from current facts
and the inputs explicitly supplied to that operation. It must not read another
Person's hidden Knowledge or make outcomes identical merely because execution
is shared.

The activity lifecycle coordinates validation and the start boundary; it does
not own a universal effects engine or mutate arbitrary domain truth. For this
fixture, one synthetic domain authority owns both test results and exposes a
single all-or-none application boundary after coordinated validation. A future
consumer spanning multiple authorities needs its own reviewed coherent commit
contract; this entry proposal does not invent cross-domain transactions.

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
proposed transitions onto promoted P18-A/B/C contracts and settle concrete
ownership/interfaces, reservation conflict and stale-state handling, atomic
start/effect publication, cancellation/release behavior, deterministic
ordering, reconstruction inputs, and targeted regression coverage. Independent
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
