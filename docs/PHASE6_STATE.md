# Phase 6 — Current Canonical State

## Canonical

Canonical branch:

`codex/phase6/canonical`

Phase 6 bootstraps from the immutable, validated Phase 5 canonical SHA:

`3c3a5a7fa5bac8f301b98ec92307eadb19af25ff`

The canonical branch includes Checkpoint A political truth foundations at
`fb69f7c` (`Record Phase 6 Checkpoint A`). Checkpoint B is validated on the
Phase 6 integration tip before canonical promotion.

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
- completed the read-only Phase 6 architecture audit and dependency graph;
- implemented, independently reviewed, integrated, and validated the
  PersonId-based political claim foundation;
- implemented, independently reviewed, integrated, and validated the
  PersonId-based faction and affiliation foundation;
- validated the claim wave with focused EditMode `10/10`, ALL EditMode
  `1358/1358`, complete Smoke `3/3`, and `git diff --check` green.
- reached Checkpoint A with faction-focused EditMode `9/9`, combined ALL
  EditMode `1367/1367`, complete Smoke `3/3`, and `git diff --check` green.
- completed, independently reviewed, integrated, and validated political
  knowledge and support foundations;
- completed, independently reviewed, integrated, and validated derived
  legitimacy, typed political decision history, and office-bound succession
  selection;
- integrated world-owned decision history with authoritative world/knowledge
  revision checks and deterministic diagnostics.

Pending after Checkpoint B:

- external review of this checkpoint;
- optional Checkpoint C work only if a substantial autonomous or generalized
  relationship layer is explicitly justified;
- remaining Phase 6 completion candidate and deferred-work record.

## Phase 6 dependency graph

The audit is complete. The graph below records current wave status and
remaining dependencies.

### Audit — MUST happen first

Inspect institutions/offices/succession, Person/genealogy, knowledge,
NPC decisions/actions, conflict, diagnostics/history, configuration, and
existing relationship concepts. Record current types, ownership, extension
points, and semantic conflicts.

### Political truth foundations — PARALLEL WITH ISOLATION only when contracts remain disjoint

- claim records and claim lifecycle — COMPLETE and canonical;
- minimal faction identity and affiliation relations — COMPLETE and canonical;
- narrowly typed support relations — COMPLETE for Checkpoint B;
- deterministic diagnostics for each stable truth store — COMPLETE for
  Checkpoint B.

Avoid shared edits to `SimulationRuntime.cs`, diagnostics core, and
configuration core in parallel. Integrate these foundations intentionally.

### Recognition and derived legitimacy — COMPLETE for Checkpoint B

Recognition state, recognized authority, and any legitimacy output must consume
explicit claims, factual eligibility, support, and institutional context. The
legitimacy score is derived and is not stored as primary world truth.

### Knowledge — COMPLETE for Checkpoint B

Political knowledge is holder-scoped by stable PersonId or InstitutionId. It
stores typed observations with observed/received days and deterministic
provenance replacement. The world owns the authoritative store, exposes
defensive snapshots, and increments a knowledge revision on holder or
observation changes. Future observations and unregistered holders are rejected.

### Political decisions and succession selection — COMPLETE for Checkpoint B

Political decisions are immutable proposal/selection history. Succession
decisions identify their office, decider, candidate set, evidence/knowledge
references, and captured world/knowledge revisions. Registration rejects stale
or cross-world context. Political succession wraps the existing office
succession transition, rechecks current day, candidate fingerprint, office
identity, world ownership, and current domain truth, then delegates mutation to
the existing office API.

### Political interaction and autonomous behavior — DEFERRED

Diplomacy, warfare, macroeconomics, full government/taxation, religion,
culture, romance/fertility, persistence/networking, Timeline architecture,
procedural narrative/UI-heavy political screens, and autonomous daily political
processing remain outside Checkpoint B. `SimulationRuntime.AdvanceDay` was not
modified for politics.

### Current political claim foundation

The canonical claim model is world-owned and keyed by stable `PersonId` plus
typed office, property, institution, or Person targets. Claim existence does
not mutate office incumbency, property ownership, genealogy, or factual life.
Recognition is explicit institutional state with stale store and world-day
guards. Terminal resolution preserves its resolution day. Runtime construction
clones and validates claims against current world truth. Deterministic
snapshots, canonical output, diffs, and invariant validation cover the claim
records and their captured cross-store references.

Faction truth is world-owned by stable `FactionId` records and a separate
PersonId-based affiliation relation store. Affiliation add/end operations are
explicit, stale-safe, world-day guarded, and bound to the originating world
store. Runtime clones rebind the faction store to the receiving PersonStore;
diagnostics cover deterministic faction and affiliation snapshots, output,
diffs, and invariants.

Political knowledge, support, derived legitimacy, decision history, and
succession selection are now integrated for Checkpoint B. Recognition remains
explicit institutional state: factual death does not itself create vacancy,
and recognition does not rewrite genealogy, property ownership, or incumbency.

### Checkpoint B integration and review record

The validated integration branch is
`codex/phase6/PoliticalSuccessionIntegration`.

Feature and integration tips:

- knowledge foundation: `68cc100`;
- support foundation: `6926272`;
- B1 knowledge/support integration: `59be917`;
- legitimacy/decision foundation: `60cc622`;
- B1+B2 integration before succession: `34388e3`;
- succession integration worker: `f38a380`;
- authoritative decision/diagnostics hardening: `d5dfa9c`;
- world-bound succession transition hardening: `da133ef`.
- final decision-store, diagnostics, invariant, and world-revision hardening:
  `8b7a597`.
- final knowledge endpoint and decision-reference hardening: `3ba6bf9`.
- final revision-preservation and world-bound knowledge composition:
  `78f7cba`.
- monotonic revision preservation across world composition:
  `46a1c0c`.

Independent reviews rejected and then verified the resolved issues: malformed
and delimiter-colliding support diagnostics, claim/office knowledge coverage,
deterministic equal-day provenance, vacancy recognition semantics, world-owned
knowledge/decision mutation boundaries, authoritative stale revisions,
office/institution decision context, deterministic decision diagnostics, and
cross-world succession transition rejection. No self-approval was used.

Final validation on the integration tip `46a1c0c`:

- Political filter: `49/49`;
- succession integration: `8/8`;
- B3 knowledge/support and legitimacy suites: `13/13`;
- diagnostics, orchestration, institution, and succession regressions:
  `47/47`;
- B3 political succession/decision/knowledge-support suites: `21/21`;
- ALL EditMode: `1406/1406`;
- official complete Smoke filter (`EditMode -TestFilter Smoke`): `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green.

The complete Smoke suite is the five-test EditMode `Smoke` filter. The
separate narrower `PlayModeSmokeTests` class previously passed `3/3`; the
PlayMode platform itself discovers zero tests because those smoke tests are
EditMode tests that enter play mode manually.

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

- Checkpoint A: stable political truth foundations are canonical — REACHED at
  canonical promotion of integration tip `fb69f7c`.
- Checkpoint B: politics integrates with knowledge, institutions, claims or
  recognition, succession selection, and NPC/institution decisions — REACHED
  on the validated integration tip documented above; stop for external review
  after canonical promotion.
- Optional Checkpoint C: only for a genuinely substantial new architecture
  layer such as autonomous simulation, generalized political relationships, or
  major knowledge/`AdvanceDay` integration.
- Final checkpoint: Phase 6 completion candidate, with all required validation
  and intentionally deferred future work recorded.

Do not stop after ordinary feature work, reviews, integrations, or promotions.
Continue autonomously until a defined checkpoint or an exceptional stop
condition is reached.
