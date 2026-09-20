# Phase 6 — Current Canonical State

## Canonical

Canonical branch:

`codex/phase6/canonical`

Phase 6 bootstraps from the immutable, validated Phase 5 canonical SHA:

`3c3a5a7fa5bac8f301b98ec92307eadb19af25ff`

At bootstrap, no Phase 6 domain implementation has been added. The first
commit on this branch is limited to orchestration and state documentation.

The completed prior-phase baseline remains documented in:

`docs/PHASE5_STATE.md`

Do not use the frozen historical branch
`codex/phase6/DemographicAgeFoundation` or the frozen Timeline spike as a
Phase 6 architecture source.

## Phase 6 purpose

Phase 6 establishes composable foundations for:

- political claims;
- legitimacy and institutional recognition;
- factions and affiliation;
- support, opposition, and alignment where explicit semantics justify them;
- institutional political decisions;
- competing succession claims and political selection;
- political conflict state;
- political knowledge and information asymmetry;
- deterministic political diagnostics and history outputs.

The target is emergent political behavior from explicit world facts,
knowledge, institutions, relationships, claims, recognition, and decisions.
This is not a scripted political minigame and is not a complete diplomacy,
warfare, government, economy, or UI phase.

## Inherited architecture contracts

The Phase 5 contracts are foundations, not targets for redesign:

`WORLD TRUTH != KNOWLEDGE != INSTITUTIONAL RECOGNITION`

The semantic chain remains:

`WORLD TRUTH → KNOWLEDGE → DECISION → ACTION/PLAN → EXECUTION CONTEXT → DOMAIN OUTCOME → DOMAIN EVENT → HISTORY/STATS/UI`

Decision systems may use knowledge. Execution must revalidate current world
truth. Events and history are downstream representations, never primary truth.

Population representation remains:

`Population aggregate → Person → NpcRuntime → Active/Dormant`

PersonId identity, factual birth/death, derived age and maturity, residence,
genealogy, institutions, offices, vacancy recognition, property ownership,
estates, and explicit succession transitions remain world-owned according to
the Phase 5 state document.

Political systems must not:

- make a claim true merely because it exists;
- rewrite genealogy, property ownership, office incumbency, or factual death;
- equate factual death with institutional vacancy;
- store redundant faction or relationship collections on PersonRuntime;
- bypass existing domain execution APIs;
- add political work to `SimulationRuntime.AdvanceDay` without explicit daily
  semantics, ordering tests, and long-run validation.

## Bootstrap status

Completed:

- verified Phase 5 local, upstream, and remote synchronization;
- created `codex/phase6/canonical` from the exact Phase 5 final SHA;
- migrated root orchestration instructions to Phase 6 while preserving the
  completed Phase 5 baseline;
- prepared the Phase 6 state document and Phase 6 agent-role definitions.

Pending:

- read-only architecture audit;
- exact Phase 6 dependency graph;
- first political truth foundation wave;
- independent reviews, integrations, validation, and checkpoint promotion.

## Initial dependency graph

The graph is provisional until the architecture audit is complete.

### Audit — MUST happen first

Inspect institutions/offices/succession, Person/genealogy, knowledge,
NPC decisions/actions, conflict, diagnostics/history, configuration, and
existing relationship concepts. Record current types, ownership, extension
points, and semantic conflicts.

### Political truth foundations — PARALLEL WITH ISOLATION only when contracts remain disjoint

- claim records and claim lifecycle;
- minimal faction identity and affiliation relations, if the audit confirms a
  missing foundation;
- narrowly typed support or alignment relations, only where semantics and
  ownership are explicit;
- deterministic diagnostics for each stable truth store.

Avoid shared edits to `SimulationRuntime.cs`, diagnostics core, and
configuration core in parallel. Integrate these foundations intentionally.

### Recognition and derived legitimacy — MUST WAIT for political truth

Recognition state, recognized authority, and any legitimacy output must consume
explicit claims, factual eligibility, support, and institutional context. A
numeric legitimacy score is not primary truth unless later architecture proves
that stored state is necessary.

### Knowledge — MUST WAIT for stable political truth

Add only the political facts and claims that existing knowledge architecture can
represent. Do not build a complete information-propagation simulation before
truth ownership is stable.

### Political decisions and succession selection — MUST WAIT for recognition and
knowledge contracts

Political systems may propose or select a candidate or recognized claimant.
Existing vacancy, office assignment, property, estate, and succession execution
must remain the validation and mutation boundary.

### Political interaction and autonomous behavior — MUST WAIT for explicit
decision contracts

Competing claims, faction support, institutional disagreement, conflict state,
and NPC/institution decisions are later waves. Autonomous daily processing is
not assumed; explicit transitions and scheduled directives are preferred.

## Branch and review policy

Feature branches use:

`codex/phase6/<FeatureName>`

Integration branches use:

`codex/phase6/<FeatureA><FeatureB>Integration`

Workers write only in isolated worktrees and never approve their own work.
Every meaningful feature receives an independent architecture review with
findings classified as `BLOCKING`, `IMPORTANT`, or `NON-BLOCKING`.

## Validation policy

Each foundation requires targeted EditMode tests, affected Phase 5 regression
suites, and `git diff --check`.

Every major canonical checkpoint requires relevant targeted suites, ALL
EditMode, the complete official Smoke suite, and `git diff --check`. Long-run
validation is required when a change affects `AdvanceDay`, autonomous politics,
recurring political processes, or long-horizon NPC behavior.

## Human checkpoints

- Checkpoint A: stable political truth foundations are canonical.
- Checkpoint B: politics integrates with knowledge, institutions, claims or
  recognition, succession selection, and NPC/institution decisions.
- Optional Checkpoint C: only for a genuinely substantial new architecture
  layer such as autonomous simulation, generalized political relationships, or
  major knowledge/`AdvanceDay` integration.
- Final checkpoint: Phase 6 completion candidate, with all required validation
  and intentionally deferred future work recorded.

Do not stop after ordinary feature work, reviews, integrations, or promotions.
Continue autonomously until a defined checkpoint or an exceptional stop
condition is reached.
