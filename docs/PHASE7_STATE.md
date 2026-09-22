# Phase 7 — Checkpoint A — Current State

## Canonical baseline

- Architecture baseline: `e4ac516daeb70f3a7fe40acf797d41090f82e818`.
- Checkpoint branch: `codex/phase7/ArmedForceFoundation`.
- Validated implementation commit: `7a983a9d15caa4c785ef41de78e607f83d6b4c23`.

## Delivered

The checkpoint introduces a pure C# armed-force foundation owned by
`ArmedForceStore`:

- stable `ArmedForceId`, `ContingentId`, and force/person-reference IDs;
- generic parent hierarchy with parent existence, self-parent, cycle, and
  terminated-parent validation;
- deterministic ID-sorted force snapshots and pre-order hierarchy traversal;
- detach/reattach state that preserves parent linkage and force identity;
- optional opaque operational location reference without movement semantics;
- aggregate contingents with stable identity, amount, open origin reference,
  open service type, and deterministic extensible characteristics;
- PersonId-based commander and relevant-Person references validated against
  `PersonStore`, without requiring or discovering `NpcRuntime`;
- active/terminated lifecycle with retained records, no ID reuse, revision
  tracking, atomic validate-then-apply mutations, and invariant reporting.

No ArmedForce state was added to `SimulationRuntime`, `AdvanceDay`,
`ConflictFoundation`, or Unity object discovery.

## Validation

- `ArmedForceFoundationTests`: `8/8`.
- Person regression filter: `126/126`.
- Population regression filter: `130/130`.
- `ConflictFoundationTests`: `16/16`.
- Lifecycle regression filter: `38/38`.
- ALL EditMode: `1467/1467`.
- Official EditMode `Smoke` filter: `5/5`.
- `git diff --check`: clean for the implementation commit.
- The project-declared Unity Editor `6000.3.9f1` was used directly in batchmode.
  The repository validation wrapper could not acquire its process snapshot in
  this environment because `Get-CimInstance` returned access denied.

## Deferred

War, Battle, Conflict/War state, tactics, morale, cohesion, readiness,
logistics, funding/pay, movement, scouting, military knowledge, recruitment,
mobilization accounting, casualties, capture, desertion, defection, mutiny,
military control, occupation, war goals, ceasefire, peace, diplomacy,
Campaign, Polity, WarAI, and daily military processing remain deferred.

Split/secession, true schism, absorption, and genuine merger operations are
also deferred. The identity and lifecycle model preserves the IDs and records
needed for those future explicit transitions without choosing continuity by
manpower or commander retention.

## Known limitations

The store is currently a composable standalone world-truth foundation. It is
not yet projected into `SimulationRuntime`/`Simulation.Core`, the general
WorldState snapshot/diagnostics catalog, save/load, or a historical event log.
Those integrations require their own consumer and checkpoint semantics.
