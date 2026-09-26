# Phase 18 — Intraday Temporal Execution v1

**Authority:** planning scope under `../SIMULATION_ARCHITECTURE.md` §§11–12,
91–92. **Readiness:** `READY_FOR_TECHNICAL_DESIGN` for P18-A; no implementation
has started. Phase numbering preserves existing IDs, not execution order.

## Objective and closure

One world advances logical time through intraday boundaries deterministically.
Activities may occupy intervals within/across days; actors choose again when
available. Start/completion and supported cancellation/interruption are explicit
transitions. Passive processes remain independent of actor choice. Existing
daily cadences and selected travel/actor-input consumers integrate through the
same timeline without double processing or a second authority.

Prove these properties with focused fixtures using existing consumers and
synthetic test operations, not new sleep, dreams, theft, needs or job gameplay.

The activity definition is distinct from its concrete instance; instance identity
and lifecycle are not permanently owned by exactly one actor or `NpcRuntime`.
P18 may implement/prove a one-participant slice, while keeping its temporal
subject and availability/commitment seams compatible with P20. This is a boundary
constraint now, not a requirement to implement participant roles or recruitment.

## Approved planning checkpoints

| ID | Closure boundary | Dependencies |
|---|---|---|
| P18-A — Logical Timeline and Due-work Scheduler | Monotonic logical intraday time/calendar mapping; central deterministic due-work scheduling; pure queries; explicit same-instant/zero-duration/stale-work rules. | Canonical calendar, determinism, mutation/input contracts; bounded technical design and independent review. |
| P18-B — Activity Lifecycle | Domain-owned duration, start/completion, commitment and availability; supported cancellation/interruption semantics; no duplicate pending authority. | P18-A stable contract for design; promoted capability for integration. |
| P18-C — Availability-driven Actor Decisions | Re-evaluation on relevant availability/condition/input boundaries, Knowledge-bounded decision and factual execution checks, instantaneous actions without infinite loops. | P18-A/B contracts and relevant promoted capabilities; current decision/action authority. |
| P18-D — Bounded Consumer and Daily Compatibility Integration | Selected existing actor-input/travel and daily processes advance chronologically with one authority; legacy profile boundaries explicit. | P18-A/B/C; only the actual P8-E/P11 or other consumer capabilities selected in reviewed scope. |

A → B → C → D is the core integration path. A's daily compatibility design
must account for existing `AdvanceDay` from entry; D performs the selected
consumer migration. Technical designs may proceed on accepted contracts, but
code integrations wait for promoted capabilities. Interruption is required
only where the selected existing consumer supports it; no universal rollback
or universal activity superclass is prescribed.

P20 layers multi-participant formation/execution on the relevant A/B/C capability.
P18 does not depend on P20; P20 need not wait for every legacy consumer in D.
P18-B/C review must reject a permanent one-to-one Activity/Actor identity or
NPC-only lifecycle assumption. Actor availability and commitments remain
individual even when a later shared instance references several actors.

## Gates and boundaries

- **Hard contracts:** one timeline; calendar occurrence is not domain effect;
  decision uses allowed Knowledge and execution revalidates current truth;
  passive processes are not actor activities; materialization is not availability.
- **Technical gates:** close time precision/range/overflow, calendar projection,
  same-time input/work ordering, reentrancy, cancellation/invalidation,
  stale queued work, exactly-once completion, zero-duration termination,
  ownership of agenda facts versus derived indexes and atomic failure behavior.
  No schemas/classes/priority values are fixed by this Brief.
- **Availability:** an actor may decide when available; relevant notifications
  do not reveal hidden truth. Duration may influence utility without universal
  utility-per-hour. Date/time windows and world conditions are supported
  constraints; the concrete behaviors that use them are separate consumers.
- **Capabilities:** no dependence on P9 genesis, P14 jobs, Sleep or P17 War.
  Intraday integrations need the actual selected consumer APIs, not whole phases.
- **Compatibility:** retain explicit daily-profile behavior and historical
  semantics. `AdvanceDay` becomes an advance-to-day-boundary adapter in an
  intraday world, not a clock jump followed by retroactive catch-up. Do not
  silently reinterpret old histories, current P8 fixtures or existing directives.
- **Exclusions:** concrete gameplay, per-frame/per-actor polling, universal
  temporal framework, mod loader, save/replay implementation, renderer, final
  utility/duration/travel formulas and wholesale conversion of all daily domains.
  Multi-participant role/formation/reservation coordination and shared execution
  belong to P20; no gang/group manager or participant solver is built in P18.
- **Reconstruction:** time/calendar, activity IDs/commitments/lifecycle/progress,
  availability, pending work or its deterministic reconstruction inputs,
  causal sequence/tie-break state, external input boundaries, versions/configuration
  and random context must be recoverable. No opaque delegates or host object
  references may be the only representation of causal work.
- **Validation:** chronological interval/daily-boundary and same-time ordering;
  repeated availability decisions; instant-action termination; completion and
  stale/cancelled work; runtime isolation; dormant/unloaded equivalence; query
  purity; input retention and affected decision/travel/daily regressions.
  Daily-loop integrations require long-run evidence and current promotion gates.
- **Hotspots:** time/calendar, `SimulationRuntime`/`AdvanceDay`, action/decision,
  command capture, travel, diagnostics. Isolate writers and integrate serially.
- **Unlocks:** timed work/opportunities, travel/waiting and future rest/needs;
  temporal P12/P13 coverage; relevant P14/P15/P16 timed consumers; P19 temporal
  hooks and P20's participant layer. Exact classes/schemas remain technical design.
