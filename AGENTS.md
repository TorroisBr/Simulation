# Simulation — Agent Instructions

## Source of truth

This repository is a persistent-world simulation platform.

Before doing Phase 5 work, always read:

`docs/PHASE5_STATE.md`

The authoritative Phase 5 branch is:

`codex/phase5/canonical`

Feature work must start from the current HEAD of that branch unless an explicit integration task says otherwise.

Do not reconstruct current architecture from old branches when `docs/PHASE5_STATE.md` already defines the current state.

## Core architecture

Preserve these principles:

`WORLD TRUTH != KNOWLEDGE != INSTITUTIONAL RECOGNITION`

The semantic chain is:

`WORLD TRUTH → KNOWLEDGE → DECISION → ACTION/PLAN → EXECUTION CONTEXT → DOMAIN OUTCOME → DOMAIN EVENT → HISTORY/STATS/UI`

Decision systems may use knowledge.

Execution must revalidate current world truth.

Events and history are not primary world truth.

Population representation is:

`Population aggregate → Person → NpcRuntime → Active/Dormant`

A Person may exist without a materialized NpcRuntime.

Materialization must not change aggregate population.

Residence is Person-level demographic truth for Person-backed NPCs.

Age and maturity are derived from birth date/calendar/configuration and must not become mutable state on PersonRuntime.

Genealogy is relation-based world truth using PersonId. Do not put parent/child collections directly on PersonRuntime.

Deep politics is not part of Phase 5.

## Orchestrator behavior

Before starting a new implementation wave:

1. Inspect the current canonical branch and HEAD.
2. Read `docs/PHASE5_STATE.md`.
3. Inspect relevant current code.
4. Reconstruct or update the remaining Phase 5 dependency graph.
5. Identify every currently unblocked task.
6. Classify each task as:
   - MUST WAIT
   - PARALLEL WITH ISOLATION
   - PARALLEL SAFE
7. Evaluate both file/Git conflicts and semantic/architectural conflicts.
8. Parallelize independent work when that materially saves time.
9. Do not serialize independent work unnecessarily.
10. Do not create excessive parallel branches when integration debt would outweigh the benefit.

## Parallel implementation safety

Use subagents freely in parallel for:

- repository exploration;
- architecture analysis;
- test discovery;
- code review;
- diff review;
- regression analysis.

Parallel code-writing tasks require isolated Git worktrees.

Never allow two writing agents to modify the same checkout concurrently.

Never allow two parallel implementation agents to own the same architectural hotspot unless their work has been explicitly separated by a stable contract.

Typical shared hotspots include:

- `SimulationRuntime.cs`
- Diagnostics core
- daily simulation loop / `AdvanceDay`
- PersonStore
- Population stores/systems
- lifecycle systems

If isolated worktrees cannot be created because of permissions, do not perform concurrent writes in the same checkout.

Request only the permission required to create/use the worktree, or serialize the implementation.

## Branch rules

Feature branches:

`codex/phase5/<FeatureName>`

Integration branches:

`codex/phase5/<FeatureA><FeatureB>Integration`

Canonical:

`codex/phase5/canonical`

Workers never implement directly on canonical.

The canonical branch moves only after:

- implementation is complete;
- independent review passes;
- integration passes;
- required Unity tests pass;
- `git diff --check` passes.

Never:

- force-push;
- rewrite shared history;
- delete main;
- delete tags;
- merge or modify main;
- silently replace canonical with an unvalidated branch.

## Recommended agent roles

Use `phase5_worker` for bounded implementation.

Use `phase5_reviewer` after implementation and before integration.

Use `phase5_integrator` when combining approved independent branches.

Use `phase5_validator` after integration and before canonical promotion.

A worker does not approve its own work.

For meaningful architectural changes, use an independent reviewer.

## Review requirements

Review implementation against its actual base commit.

Inspect:

- full diff;
- architecture boundaries;
- mutation authority;
- transactional/atomic behavior;
- stale-state handling;
- deterministic behavior;
- accidental coupling;
- scope expansion;
- missing tests;
- shared hotspots.

Do not approve solely because tests are green.

## Integration requirements

Before integrating parallel branches:

- establish their common base;
- inspect changed files;
- inspect semantic overlap;
- choose explicit integration order;
- resolve conflicts intentionally;
- do not silently discard either branch's behavior.

After integration, rerun relevant targeted suites and full required regression gates.

## Test policy

Current canonical baseline is recorded in:

`docs/PHASE5_STATE.md`

For meaningful Phase 5 changes, run relevant targeted EditMode suites.

Before canonical promotion run:

- all relevant domain suites;
- ALL EditMode;
- official `Smoke` filter;
- `git diff --check`.

Official Smoke must execute the complete current Smoke suite.

Long-run is required only when changes affect the daily loop, long-horizon behavior, or when a regression specifically warrants it.

## Configuration architecture

Configuration resolution is:

`Defaults → Preset → World overrides → Content overrides → EffectiveSimulationConfiguration → Domain systems`

Policy asks whether something may happen or operate autonomously.

Parameters define rates, thresholds, frequency, or intensity.

Content defines concrete game objects and behavior.

Do not introduce special-case preset code.

## Scope discipline

Do not implement roadmap items early merely because they seem related.

Keep separate unless explicitly required:

- reproduction;
- fertility;
- pregnancy;
- deep politics;
- claims;
- factions;
- ideological systems;
- advanced succession politics;
- persistence/API;
- Timeline architecture.

Prefer small foundations followed by explicit integration tasks.

## Canonical promotion

When an integration is fully validated:

1. record its final SHA;
2. update `docs/PHASE5_STATE.md`;
3. move `codex/phase5/canonical` to the validated commit;
4. push;
5. verify local/remote synchronization;
6. recompute the remaining dependency graph;
7. continue Phase 5 without waiting for the user to supply the next task.

Do not ask the user what the next task is when the roadmap already determines it.

## When to stop and ask the user

Stop only for:

- a genuine product/design decision not determined by current architecture;
- mutually incompatible architecture choices with meaningful consequences;
- an unrecoverable environment/tooling problem;
- required permission that cannot be safely obtained automatically;
- completion of Phase 5.

Do not stop for routine implementation choices, naming, test execution, Git operations, cherry-picks, worktrees, or ordinary conflict resolution.

## Repository commands

You may autonomously use routine read/search commands including:

- `Get-Content`
- `Get-ChildItem`
- `Select-String`
- `rg`
- `grep`
- `find`
- `ls`
- `cat`

You may use normal Git commands, Unity CLI, compilation and tests as needed under the active permission policy.

Always leave completed worktrees clean and published unless a task explicitly says otherwise.
