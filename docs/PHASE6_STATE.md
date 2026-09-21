# Phase 6 — Current Canonical State

## Canonical

Canonical branch:

`codex/phase6/canonical`

Phase 6 bootstraps from the immutable, validated Phase 5 canonical SHA:

`3c3a5a7fa5bac8f301b98ec92307eadb19af25ff`

The canonical branch includes Checkpoint A political truth foundations at
`fb69f7c` (`Record Phase 6 Checkpoint A`), the historical Checkpoint B
baseline integration at `e505b84`, and the externally approved Checkpoint B
final state from the current recognition/faction-tenure integration.
The historical baseline and the current fix integration are recorded below
separately.

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

Checkpoint B external approval is complete:

- no further Checkpoint B code changes are pending;
- do not begin Checkpoint C in this task;
- the remaining Phase 6 completion candidate and deferred-work record remain
  future work.

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

Political knowledge is holder-scoped by stable PersonId, InstitutionId, or
FactionId. Faction knowledge is independent of member knowledge and is not
propagated through membership. Claim recognition observations are keyed by
claim and recognition institution, so institutional perspectives remain
independent. The world owns the authoritative store, exposes defensive
snapshots, and increments a knowledge revision on holder or observation
changes. Future observations and unregistered holders are rejected.

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
Recognition is a separate institution-scoped relation keyed by stable
`(ClaimId, InstitutionId)` identity. The same claim may be recognized,
withdrawn to `Unrecognized`, recognized again, or contested/rejected by
different institutions at the same time; each relation has stale-safe
transitions and auditable history. Terminal resolution preserves its
resolution day. Runtime construction clones and validates claims and
recognition relations against current world truth. Deterministic snapshots,
canonical output, diffs, and invariant validation cover both claim lifecycle
and recognition relations. Knowledge can record an institution-specific
`Unrecognized` perspective, which remains distinct from no observation.

Faction truth is world-owned by stable `FactionId` records and a separate
PersonId-based affiliation relation store. The store keeps stable affiliation
tenure IDs, historical records, and an active `(FactionId, PersonId)` index;
policy is evaluated by the proposal system rather than embedded in storage.
Supported policy semantics are cannot-leave, leave-without-rejoin, and
leave-and-rejoin, with explicit expulsion permission. Affiliation add/end
operations are stale-safe, world-day guarded, and bound to the originating
world store. Completed affiliation records preserve the explicit historical
end reason `VoluntaryLeave` or `Expulsion`. Runtime clones rebind the faction
store to the receiving PersonStore; diagnostics cover deterministic faction
and affiliation snapshots, output, diffs, and invariants.

Political knowledge, support, derived legitimacy, decision history, and
succession selection are now integrated for Checkpoint B. Recognition remains
explicit institutional state: factual death does not itself create vacancy,
and recognition does not rewrite genealogy, property ownership, or incumbency.

### Authoritative Phase 6 political semantics

The following product and architecture definitions are authoritative for
Checkpoint B and any later Phase 6 work:

- Multiple competing claims may target the same office, property, institution,
  or Person. Faction affiliation does not imply support, loyalty, knowledge, or
  recognition. Eligibility, claim, recognition, support, influence, and final
  decision remain distinct concepts.
- Legitimacy and influence are contextual and derived from explicit inputs.
  There is no universal highest-raw-support winner, universal support threshold,
  or automatic civil war. Comparable support may leave a dispute unresolved;
  escalation is an explicit decision and physical conflict reuses existing
  action/conflict execution.
- Faction membership is policy-driven per faction and preserves affiliation
  periods. A faction's official position is governed by explicit authority,
  influence, or decision rules; it is not a universal Person power score or an
  automatic sum/majority of support.
- WORLD TRUTH, KNOWLEDGE, INTERPRETATION, and POLITICAL POSITION remain
  separate. Knowledge is holder-scoped for Persons, factions, and institutions
  and is not synchronized through members. A heterodox decision may be
  deliberate rather than ignorant; minimal decision reasoning is sufficient for
  the current foundation, without implementing a full ideology model.
- Recognition and support never make a claim true, and recognition does not
  resolve a factual dispute. Execution always revalidates current world truth.
- Selecting a successor and possessing or assuming an office are separate
  concepts. The architecture may represent a pending or scheduled selection and
  delayed investiture, but not every office requires delay; the current B
  integration keeps execution explicit and revalidates at the domain boundary.
  A selected successor may die or lose validity before assumption.
- Affiliation, recognition, support, faction positions, institutional decisions,
  succession selections, and future disputes must remain historically auditable.
  History is downstream evidence, not primary world truth.

The semantic chain for political reasoning is therefore:

`WORLD TRUTH → KNOWLEDGE → INTERPRETATION → POLITICAL POSITION → COLLECTIVE/INDIVIDUAL DECISION → POLITICAL OUTCOME/SELECTED INTENT → DOMAIN EXECUTION → HISTORY`

Diplomacy, warfare strategy, full government/taxation, macroeconomics,
religion, culture, romance/fertility, persistence/networking, Timeline
architecture, procedural narrative, and UI-heavy political screens remain
outside this checkpoint. Any deferred investiture, richer faction governance,
or generalized dispute/escalation layer requires an explicit future design
checkpoint.

### Checkpoint B historical baseline integration and review record

The historical baseline integration branch was
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
- political history and knowledge diagnostic hardening:
  `561c3e1`, `8989cb5`, `1a1a05f`, `b50c9f2`.
- parsed knowledge diagnostic invariants and complete endpoint catalogs:
  `8178052`, `b26a204`, `d5136f3`.
- orphan provenance and future decision-history rejection:
  `cb573bd`.
- typed political diagnostics, stale-knowledge separation, and atomic decision
  binding: `9c28902`, `3e52d76`.
- caller-store isolation in world composition: `e505b84`.

Independent reviews rejected and then verified the resolved issues: malformed
and delimiter-colliding support diagnostics, claim/office knowledge coverage,
deterministic equal-day provenance, vacancy recognition semantics, world-owned
knowledge/decision mutation boundaries, authoritative stale revisions,
office/institution decision context, deterministic decision diagnostics,
cross-world succession transition rejection, orphan provenance, future imported
history, typed claim-state validation, stale-knowledge separation, atomic
decision binding, and caller-store alias isolation. The final independent
review approved `e505b84`; no self-approval was used.

Final validation on the historical baseline implementation tip `e505b84`:

- Political knowledge/support integration: `6/6`;
- political succession/decision integration: `13/13`;
- ALL EditMode: `1412/1412`;
- official complete Smoke filter (`EditMode -TestFilter Smoke`): `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green.

The complete Smoke suite is the five-test EditMode `Smoke` filter. The
separate narrower `PlayModeSmokeTests` class previously passed `3/3`; the
PlayMode platform itself discovers zero tests because those smoke tests are
EditMode tests that enter play mode manually.

### Checkpoint B — external review fix candidate (historical record)

The external architecture review accepted the existing decision, support,
stale-guard, diagnostics, Smoke, and no-political-`AdvanceDay` boundaries but
blocked the then-current Checkpoint B baseline `9bbf371` on two issues:

- `PoliticalClaimRecord` no longer owns a single global recognition. The
  authoritative `PoliticalClaimRecognitionRecord` is keyed by stable
  `RecognitionId` derived from `(ClaimId, InstitutionId)`, with one current
  state and isolated recognition history per institution. Knowledge observes
  the same institution-scoped perspective, and legitimacy inputs carry an
  explicit recognition-perspective institution.
- faction affiliations now have stable tenure IDs, historical records, and an
  active pair index. Membership policy is evaluated by the proposal system and
  supports cannot-leave, leave-no-rejoin, leave-and-rejoin, and explicit
  expulsion permission without storing redundant membership on PersonRuntime.

Faction is also a first-class political knowledge holder, independent of its
members; no membership-to-knowledge propagation was introduced.

The earlier isolated implementation branch was
`codex/phase6/PoliticalRecognitionFactionTenure`, reviewed independently and
integrated on `codex/phase6/PoliticalRecognitionFactionTenureIntegration`.
Its pre-withdrawal/end-cause implementation tip was `12d0fd1`, and it was
already promoted to canonical as `dcf5f222` before the final external review
below.

Focused coverage includes multi-institution recognition and history
isolation, institution-specific knowledge and legitimacy perspective,
leave/rejoin and no-rejoin policies, cannot-leave, expulsion permission,
active-tenure uniqueness, faction holders, faction/member knowledge
independence, and deterministic diagnostics.

Validation on the integration tip:

- independent review: ALL EditMode `1420/1420`;
- final integration ALL EditMode: `1420/1420`;
- official complete Smoke filter: `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green;
- `SimulationRuntime.AdvanceDay` untouched.

### Checkpoint B — externally approved final state

The final external review of canonical `e132eae` is complete and Checkpoint B
is externally approved. The reviewed final state is implemented on the
integration branch `codex/phase6/PoliticalRecognitionFactionTenureIntegration`
and included in canonical:

- institution-scoped recognition can transition
  `Recognized → Unrecognized → Recognized` while preserving stable relation
  identity, history, stale guards, independent institutional perspectives,
  and deterministic diagnostics;
- political knowledge can explicitly record institution A's known
  `Unrecognized` position, while absence of an observation remains distinct;
- faction affiliation history preserves `VoluntaryLeave` versus `Expulsion`
  without introducing a generic event/reason framework.
- Faction remains an independent political knowledge holder;
- political decisions remain separate from execution;
- `SimulationRuntime.AdvanceDay` remains unchanged.

Focused coverage includes the withdrawal/restoration history, untouched
parallel institution, explicit known-unrecognized knowledge, absence-versus-
known-unrecognized distinction, affiliation end reasons, and diagnostic
end-reason output.

Final validation on the current integration tip:

- focused Checkpoint B coverage: `46/46`;
- ALL EditMode: `1421/1421`;
- official complete Smoke filter (`EditMode -TestFilter Smoke`): `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green;
- `SimulationRuntime.AdvanceDay` unchanged.

`SimulationRuntime.AdvanceDay` remains unchanged, so no long-run validation
was required for this fix.

Checkpoint B is externally approved. Do not begin Checkpoint C in this task.

### Post-Checkpoint B — architecture alignment and foundation hardening

The post-Checkpoint-B architecture alignment is integrated and validated on
the current canonical promotion candidate. It is a technical hardening wave,
not Checkpoint C and not a new political feature layer.

The authoritative configuration flow is now:

`authoring/assets → defaults/preset/world/content overrides → EffectiveSimulationConfiguration → composition validation → domain projections/systems`

Travel, crime, guard enforcement, merchant trade, and commercial knowledge no
longer consume competing raw bootstrap policy values after resolution.
`SimulationModuleSet` remains only as a local Unity bootstrap compatibility
view; it preserves requested modules and does not normalize away Merchant when
Economy is disabled or define semantic enablement. Enabled capability without
an available implementation fails composition explicitly.

The effective projections now include merchant trade policy and commercial
knowledge settings. Merchant repositioning authorizes only a new autonomous
reposition intention; existing plans may still execute or replan. Global trade
amount and former hidden merchant constants are effective configuration, while
`minimumProfitPerItem` is content on `NpcJobData` and can vary by merchant/job.

Crime enablement and crime autonomy are separate. Explicitly requested crime
actions remain eligible when Crime is enabled but autonomous crime is disabled;
disabled crime does not stop existing hidden-state timers. Sentence progression
is independent of GuardCrime enablement. No guard-autonomy policy was invented;
GuardCrime remains an enforcement capability.

Authoritative runtime randomness is pure C# and context/stream scoped. NPC
decision, crime target selection, action success, and seeded conflict draws no
longer depend on Unity's global random state. Demographic sampling remains its
existing stable identity/day-scoped implementation. Ordering that can affect
truth is stabilized by runtime IDs, action IDs, or domain keys; diagnostics,
queries, previews, and recording do not consume authoritative randomness.

The historical Checkpoint-B baseline integration remains
`codex/phase6/PoliticalSuccessionIntegration`; the later institution-scoped
recognition/faction-tenure fix integration remains
`codex/phase6/PoliticalRecognitionFactionTenureIntegration`. This alignment
wave is integrated separately as
`codex/phase6/ArchitectureAlignmentFoundationHardeningIntegration`.

Focused alignment coverage includes effective configuration precedence and
immutability, explicit composition failures, effective travel cost, Crime
autonomy/explicit actions, disabled-crime consequences, sentence progression,
merchant limits/repositioning/content margin, commercial knowledge settings,
deterministic random streams, conflict randomness, and stable insertion order.
The complete validation record for this wave is maintained with the promotion
commit and includes ALL EditMode, official Smoke, long-run regression, and
`git diff --check`.

### Architecture alignment conformance closure — validated

The local conformance audit blockers are resolved on the integration tip
`4f5521e` and are part of the current canonical promotion candidate. The Unity
bootstrap now composes the CrimeSystem infrastructure whenever Justice exists,
including when `Crime.Enabled` is false, so existing hidden-state timers still
progress. The CrimeSystem is registered as an NPC action provider only when
Crime is enabled; infrastructure availability therefore does not re-enable
normal or autonomous crime origination. A real `TesteSimulacao` composition
test covers timer expiration, provider absence, and the absence of a new
criminal decision.

Fatal conflict consequences retain their existing injury algorithm and
probabilities, but contextual conflict random sources derive each fatal draw
from stable conflict, resolution, side, participant, and consequence identity.
Unrelated sequential stream consumption and semantically irrelevant
participant insertion order therefore do not shift an independent fatal draw.
Sources without contextual support retain the legacy test-double fallback.
`SimulationRuntime.AdvanceDay` remains unchanged.

Final conformance-closure validation on the integration tip:

- focused bootstrap composition: `1/1`;
- Conflict suites: `129/129`;
- Crime suites: `7/7`;
- DeterministicRandom: `4/4`;
- SimulationRuntime orchestration: `10/10`;
- long-run regression: `7/7`;
- ALL EditMode: `1441/1441`;
- official complete Smoke filter: `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green;
- no changes to `SimulationRuntime.AdvanceDay`.

This closes the architecture-alignment/foundation-hardening follow-up only.
Checkpoint C has not started. Known deferred work remains categorized as
optional cleanup (legacy capability/configuration paths and related naming),
deferred architecture (calendar fallback, capability discovery, lazy loading,
Active/Dormant expansion, generic Organization, and future political daily
processing), and future persistence/platform work (RNG/config persistence,
replay/save, networking, and multiplayer concerns).

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
- Checkpoint B: externally approved. The historical baseline integration
  remains `e505b84` on `PoliticalSuccessionIntegration`; the current fix
  integration is `PoliticalRecognitionFactionTenureIntegration`, and its
  validated result is promoted to `codex/phase6/canonical`. Checkpoint B is
  complete; do not begin Checkpoint C in this task.
- Optional Checkpoint C: only for a genuinely substantial new architecture
  layer such as autonomous simulation, generalized political relationships, or
  major knowledge/`AdvanceDay` integration.
- Final checkpoint: Phase 6 completion candidate, with all required validation
  and intentionally deferred future work recorded.

Do not stop after ordinary feature work, reviews, integrations, or promotions.
Continue autonomously until a defined checkpoint or an exceptional stop
condition is reached.
