# Phase 7 — Checkpoint B — Current State

## Baselines

- Architecture baseline: `e4ac516daeb70f3a7fe40acf797d41090f82e818`.
- Checkpoint B implementation baseline: `6409dbf5c264378bda6c0c25d83105100c3260ff`.
- Checkpoint A implementation retained from `7a983a9d15caa4c785ef41de78e607f83d6b4c23`.
- Checkpoint branch: `codex/phase7/ArmedForceWorldComposition`.

## Delivered

Checkpoint B composes the Checkpoint A ArmedForce foundation into world state
and diagnostics without introducing War, Battle, movement, or daily military
processing.

- `SimulationRuntime` owns a cloned `ArmedForceStore` bound to the resolved
  authoritative `PersonStore`; the clone preserves IDs, hierarchy, lifecycle,
  composition, relevant-Person references, and revision without retaining an
  external mutable store reference.
- Contingent identity now requires immutable origin and service-type semantics.
  Amount and extensible characteristics may be replaced; origin/service
  mutation fails atomically with stable revision and state.
- Organizational contingent aggregation is explicit and includes detached and
  terminated structural descendants as composition/history. It is not a
  current-manpower, battle-strength, or location query.
- World snapshots capture force identity/lifecycle/parent/detachment/location/
  commander, contingent provenance/composition, relevant Person references,
  and ArmedForceStore revision with deterministic ordering.
- Canonical writer, human diagnostics formatter, diff, and snapshot invariant
  validation report ArmedForce state. Diagnostics validate parent existence,
  self-parent/cycles, active children under terminated parents, detached roots,
  commander/relevant-Person references, contingent force references, and stable
  identities. The store remains the mutation authority.
- Person references remain PersonId-based and do not require materialized
  `NpcRuntime` representations.

No ArmedForce configuration flag, RNG, save/load, event sourcing, recruitment,
population accounting, or military daily tick was added. `ConflictFoundation`
was not remodeled.

## Validation

- Focused ArmedForce suites: `16/16`.
- Diagnostics/orchestration/ConflictFoundation regression filter: `98/98`.
- Person regression filter: `128/128`.
- Population regression filter: `130/130`.
- ALL EditMode: `1475/1475`.
- Official EditMode `Smoke` filter: `5/5`.
- `git diff --check`: clean.
- Unity `6000.3.9f1` was used directly in batchmode. The repository wrapper
  could not acquire its process snapshot in this environment because
  `Get-CimInstance` returned access denied.

## AdvanceDay and architecture conformance

`SimulationRuntime.AdvanceDay` was not changed. No military processing or RNG
was added to the daily cadence; focused coverage confirms ArmedForce revision
and state remain unchanged across one day advance.

`docs/SIMULATION_ARCHITECTURE.md` was not changed. A read-only conformance
review found no contradiction requiring an architecture decision. The design
preserves ArmedForce != Faction/Institution/Polity/generic Organization,
Person != NpcRuntime, physical separation != organizational separation, and
manpower source != allegiance/loyalty/command.

## Deferred

Persistent War/Battle/strategic Conflict state, battle resolution, tactics,
morale, cohesion, readiness, supply, logistics, funding/pay, requisition,
foraging, military movement, scouting, military knowledge, recruitment,
mobilization accounting, casualties, capture/custody, desertion, defection,
mutiny, military control, occupation, war goals, ceasefire, peace, taxation,
diplomacy, Campaign, Polity, WarAI, and daily military autonomy remain
deferred.

Explicit continuity operations for secession, true schism, absorption, and
genuine merger remain deferred. The current identity/lifecycle/store contracts
preserve the records and IDs required for those future explicit transitions
without choosing continuity by manpower or commander retention.

Save/load, persistence, and migration are also deferred. The snapshot and
canonical writer are diagnostics projections, not serialization contracts.

## Known limitations

ArmedForce remains composed at the current `SimulationRuntime` boundary; no
broader `Simulation.Core` migration was started. The composition API is ready
for a future boundary migration, but this checkpoint intentionally does not
add a military subsystem, operational manpower query, or autonomous consumer.
