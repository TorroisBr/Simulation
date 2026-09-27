# Phase 18 — Intraday Temporal Execution v1

**Authority:** planning scope under `../SIMULATION_ARCHITECTURE.md` §§11–12, 91–92. **Readiness:** P18-A/B/C core capabilities and the accepted additive A extension are promoted.
The additive P18-A boundary/continuation contract was accepted at `2175bf2`; corrected implementation candidate `f1bfe818565c3fca81b373d1bc9a70a16f4eda10` passed exact-tip review and required validation and was promoted at integration tip `1dd0479`.
The preserved P18-C external-input/deferral adapter code integration `a535441` is assembled against current P18 canonical State tip `eabc1c2` / extension code tip `1dd0479`. Post-extension exact-tip implementation review passed; the focused suites, ALL EditMode 1794/1794, and complete Smoke 5/5 passed. Its canonical promotion approval remains a separate pending gate. P18-D remains dependency-gated. Phase numbering preserves existing IDs, not execution order.

P18-D's existing P11 external-input consumer also requires the separate
P18-C external-input/deferral adapter design at
`../design/PHASE18_C_EXTERNAL_INPUT_ADAPTER_DESIGN.md`. The design passed
independent review at exact tip `358c65c85e1eafdd91ef4a6553ba3b0a8c4af249`;
its implementation passed post-extension review and validation at code tip
`a535441`, with canonical promotion pending. The design adds a P18-C
request-state owner and typed P11-owned temporal capture record linked to
P18-A's accepted input reference, plus a typed temporal transition stream
while preserving legacy daily records/APIs; P18-C deferral leaves P11 Pending.
Post-P9-B/P11 compatibility
validation at `2d6b3ce` passed focused suites 14/13/24/6, ALL EditMode
1742/1742 and official Smoke 5/5; this is upstream compatibility evidence,
not adapter test evidence. The adapter's pre-extension focused/full results are
historical only; its assembled post-extension tree passed focused and full
validation and exact-tip independent review. This does not promote the adapter.
P18-D implementation
remains blocked until the independently validated adapter integration is
promoted and the serialized `SimulationRuntime` ownership window is available;
the accepted P18-A extension is already promoted. The P18-D
SellGoods consumer also requires a stable
proposal-ID operation receipt from the existing economy owner, including an
immutable request fingerprint and first-execution current-truth snapshot.
Preserve trusted local-input scope, P11's retained-input authority and exact
P18-C/P18-A causal identities; no second input queue or scheduler is allowed.

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
| P18-A — Logical Timeline, Due-work Scheduler and Accepted Boundary Continuation Extension | Monotonic logical intraday time/calendar mapping; deterministic due-work scheduling; pure queries; explicit same-instant/zero-duration/stale-work rules; accepted additive atomic boundary activation, frozen manifest, distinct resumable continuation/step identities, barrier and returned-fact publication. | Canonical calendar, determinism, mutation/input contracts; bounded technical design and independent review. Extension contract `2175bf2` accepted; corrected implementation candidate `f1bfe818565c3fca81b373d1bc9a70a16f4eda10` passed exact-tip review and required validation and was promoted at integration tip `1dd0479`. |
| P18-B — Activity Lifecycle | Domain-owned duration, start/completion, commitment and availability; supported cancellation/interruption semantics; no duplicate pending authority. | P18-A stable contract for design; promoted capability for integration. |
| P18-C — Availability-driven Actor Decisions | Re-evaluation on relevant availability/condition/input boundaries, Knowledge-bounded decision and factual execution checks, instantaneous actions without infinite loops. | P18-A/B contracts and relevant promoted capabilities; current decision/action authority. |
| P18-D — Bounded Consumer and Daily Compatibility Integration | Selected existing actor-input/travel and daily processes advance chronologically with one authority; legacy profile boundaries explicit. | P18-A/B/C; only the actual P8-E/P11 or other consumer capabilities selected in reviewed scope. |

A → B → C → D is the core integration path. A's daily compatibility design
must account for existing `AdvanceDay` from entry; D performs the selected
consumer migration. Technical designs may proceed on accepted contracts, but
code integrations wait for promoted capabilities. Interruption is required
only where the selected existing consumer supports it; no universal rollback
or universal activity superclass is prescribed.

The P18-A extension publishes complete returned timeline facts after its
continuation completes but before ordinary work at that instant; P18-C source
signals remain withheld until the outer advance returns successfully. This
separation is part of the accepted P18-A contract and does not itself deliver
the P18-C external-input/deferral adapter.

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
