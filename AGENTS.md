# Simulation — Agent Instructions

## Source of truth

This repository is a persistent-world simulation platform.

Before doing Phase 6 work, always read:

`docs/PHASE6_STATE.md`

The authoritative Phase 6 branch is:

`codex/phase6/canonical`

Phase 6 feature work must start from the current HEAD of that branch unless an explicit integration task says otherwise. `docs/PHASE5_STATE.md` and `codex/phase5/canonical` are the completed Phase 5 baseline and are not active Phase 6 work.

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

Phase 6 establishes minimal, composable foundations for politics, claims, legitimacy, factions, support, recognition, and institutional power. Do not absorb diplomacy, warfare, macroeconomics, fertility, persistence, networking, UI-heavy political screens, or Timeline architecture into Phase 6.

Political architecture must preserve these boundaries:

- a claim does not make its assertion true;
- factual truth, knowledge, and institutional recognition remain separate;
- faction membership and support are relation/store-owned facts, not redundant PersonRuntime collections;
- legitimacy is derived from explicit inputs unless stored state is justified by architecture;
- political decisions propose or select outcomes, while existing domain systems execute validated world mutations;
- factual death does not directly create vacancy, and political recognition does not directly rewrite genealogy, property ownership, or office incumbency;
- PersonId identity persists across materialization, dormancy, and death.

## Orchestrator behavior

Before starting a new implementation wave:

1. Inspect the current canonical branch and HEAD.
2. Read `docs/PHASE6_STATE.md` and consult `docs/PHASE5_STATE.md` for the completed baseline.
3. Inspect relevant current code.
4. Reconstruct or update the remaining Phase 6 dependency graph.
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

`codex/phase6/<FeatureName>`

Integration branches:

`codex/phase6/<FeatureA><FeatureB>Integration`

Canonical:

`codex/phase6/canonical`

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

Use `phase6_worker` for bounded implementation.

Use `phase6_reviewer` after implementation and before integration.

Use `phase6_integrator` when combining approved independent branches.

Use `phase6_validator` after integration and before canonical promotion.

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

For meaningful Phase 6 changes, run relevant targeted EditMode suites and affected Phase 5 regression suites.

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

Phase 5 foundations are completed and immutable in concept. Do not redesign them merely because Phase 6 political features depend on them.

Do not implement later roadmap items early merely because they seem related. Keep separate unless explicitly required:

- diplomacy between nations;
- warfare strategy or a second combat system;
- full government, taxation, or macroeconomics;
- religion, culture, romance, fertility, or pregnancy;
- persistence/API or multiplayer/networking;
- Timeline architecture;
- procedural narrative generation or UI-heavy political screens.

Within Phase 6, prefer small political truth foundations followed by explicit recognition, knowledge, decision, and integration tasks. Do not turn `SimulationRuntime.AdvanceDay` into a political dumping ground; autonomous daily behavior requires explicit semantics, ordering tests, and long-run validation.

## Canonical promotion

When a Phase 6 integration is fully validated:

1. record its final SHA;
2. update `docs/PHASE6_STATE.md`;
3. move `codex/phase6/canonical` to the validated commit;
4. push;
5. verify local/remote synchronization;
6. recompute the remaining dependency graph;
7. continue Phase 6 without waiting for the user to supply the next task.

Do not ask the user what the next task is when the roadmap already determines it.

## When to stop and ask the user

Stop only for:

- a genuine product/design decision not determined by current architecture;
- mutually incompatible architecture choices with meaningful consequences;
- an unrecoverable environment/tooling problem;
- required permission that cannot be safely obtained automatically;
- a defined Phase 6 human checkpoint or completion of Phase 6.

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

## Routine local command authorization

The user explicitly authorizes routine local repository and Unity development operations required to complete approved Phase 6 work.

Do not ask the user for conversational confirmation before performing routine operations listed below.

### Read/search commands

You may autonomously use, including equivalent variants:

- Get-Content
- Get-ChildItem
- Select-String
- Test-Path
- Resolve-Path
- rg
- grep
- find
- ls
- dir
- cat

You may read all files required for the active task in:

- the canonical Phase 6 worktree;
- assigned isolated Phase 6 feature worktrees;
- assigned integration worktrees;
- repository metadata required for Git operations;
- Unity project files;
- Unity logs;
- Unity test-result files.

### Git authorization

You may autonomously use normal non-destructive Git operations required by the approved workflow, including:

- git status
- git diff
- git log
- git show
- git rev-parse
- git branch
- git fetch
- git switch
- git checkout
- git add
- git commit
- git cherry-pick
- git merge
- git worktree list
- git worktree add
- git worktree remove for clean completed temporary worktrees
- git push

You may create approved Phase 6 branches and isolated worktrees without asking for conversational confirmation.

You may commit and push completed feature/integration branches and canonical promotions when required by the approved orchestration workflow.

Never:

- force-push;
- rewrite shared history;
- delete main;
- modify or merge main;
- delete tags;
- discard user changes;
- run reset --hard against work that is not explicitly disposable;
- perform destructive cleanup against uncommitted user work.

If a routine Git operation requires a Codex/OS sandbox approval, request only the minimum technical permission required.

Do not reinterpret a sandbox permission prompt as a product or architecture decision.

### Unity authorization

You may autonomously:

- invoke the installed Unity Editor or Unity CLI;
- use batchmode;
- run EditMode tests;
- run PlayMode tests when required;
- run filtered or targeted test suites;
- run the official Smoke suite;
- run long-run simulation tests when required;
- inspect Unity-generated logs and test-result files;
- retry failed Unity invocations when the failure is environmental rather than a test failure.

Do not ask for conversational confirmation before running Unity validation required by AGENTS.md, docs/PHASE6_STATE.md, or affected Phase 5 baseline suites.

A failed test is not by itself a reason to ask the user what to do.

Diagnose whether the failure is caused by implementation, test expectation, environment, or invocation and proceed according to the approved roadmap.

### Permission prompts

These instructions grant task-level authorization.

They do not override operating-system, sandbox, or Codex permission enforcement.

If the runtime itself requires approval for a routine authorized operation, request only that technical approval and continue immediately afterward.

Do not stop an orchestration wave or checkpoint merely because a routine command required environment approval.
