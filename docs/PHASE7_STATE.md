# Phase 7 — Current Canonical State

## Checkpoint D6A — Military manpower foundation (approved and promoted)

- Common implementation baseline:
  `b04b69e42a8208d286e4d40427596019a3654d68`.
- Canonical architecture baseline before integration:
  `ffaf5418c79a42f4b4c6300c6ad8e4acda6edbbb` on
  `codex/phase7/canonical`. This commit consolidates the D6A/D6B/D7
  architecture in `docs/SIMULATION_ARCHITECTURE.md` and is the first parent
  of the D6A integration merge.
- Feature branch: `codex/phase7/MilitaryManpowerFoundation`, created from that
  common baseline. Implementation commit:
  `904a00d5879db941988885cbb0a4c6c21c4325f2`. The complete validated feature
  candidate, including its original state record, is
  `a316b460bd962eb5e2b0dc5ce261e9975bba8275`.
- Integration branch: `codex/phase7/D6ACanonicalIntegration`. Merge commit
  `f15dcf4318b9da1283d4351d4d5b977cc9cdafea` has the canonical architecture
  commit above as first parent and the D6A candidate as second parent; the
  merge base is `b04b69e42a8208d286e4d40427596019a3654d68`. The architecture
  document is unchanged by the merge. Following integration validation, this
  D6A state-document update is included in the promoted canonical tip.
- D6A is approved and promoted to `codex/phase7/canonical`. The feature branch
  remains unchanged. No D6B1, D6B2, or D7 implementation is included.

### Delivered

- `ManpowerSourceId` is stable ordinal semantic identity, separate from
  contingent origin/provenance. A read-only source snapshot provider supplies
  explicit military-allocatable capacity, optional factual living quantity,
  and a fingerprint that must change when either quantity changes. Source
  allocation is derived by checked summation of the living rosters of all
  contingents bound to that source; no mutable allocated counter or population
  mirror was added.
- `ContingentManpowerStateStore` owns one optional source binding and canonical
  living cohorts per `ContingentId`. Cohorts aggregate Healthy/Wounded,
  Free/Captured (with active `ArmedForceId` custodian), and
  Available/Unavailable. Equal semantic keys merge, order/fingerprints are
  deterministic, and Captured+Available is rejected. `LivingRosterAmount` and
  `AvailableAmount` are checked derived sums; no `CohortId` exists.
- Legacy records bootstrap without inventing a source: positive `Amount`
  becomes one Healthy/Free/Available cohort; zero becomes no cohorts.
  `ContingentRecord.Amount` remains an exact compatibility mirror. Managed
  `TryReplaceContingent` cannot change it, and ordinary managed registration
  starts at zero; source-backed allocation is required to add roster.
- Binding a nonempty legacy roster validates current source capacity and
  optional factual living quantity. A nonempty roster cannot change/clear its
  binding. Allocation requires an active force, an exact current source
  fingerprint, valid explicit cohort dimensions, checked arithmetic, and
  source conservation before mutation. Demobilization requires an explicit
  binding and exact cohort; if the source resolves it checks the supplied
  fingerprint, but it may safely reduce the military-owned allocation even
  when capacity has fallen or the source is unavailable. It never edits source
  facts. Captured cohorts must first be explicitly released/retargeted.
  Status redistribution preserves roster total and binding and requires an
  active force.
- Historical pre-D6A terminated forces with a positive legacy roster are
  preserved during composition and reported by invariants rather than silently
  rewritten. An explicit source binding may reconcile such a roster without
  reactivating the force; allocation remains blocked, and the roster can be
  demobilized and unbound only after reaching zero. If no source authority can
  resolve the legacy roster, its state is preserved and the inconsistency
  remains diagnostic. New force termination is blocked by direct positive
  roster or by captured cohorts for which the force is custodian; descendants'
  rosters are not aggregated.
- `SimulationRuntime` clones the ArmedForce store first, then clones/rebinds
  supplied manpower state or performs legacy bootstrap. No source-world
  `ArmedForceStore` reference is retained. D6A state is projected through
  snapshots, canonical output, diff, formatting, and world invariants, including
  source capacity/factual-quantity diagnostics.
- D3 snapshots capture roster, availability, canonical cohorts, and the
  participant-local manpower fingerprint. Zero available manpower cannot make
  a side eligible; only relevant participant state changes stale its context.
  D4 still represents each direct contingent, but hard-projects zero capability
  and skips the provider whenever that contingent has zero `AvailableAmount`.
  For positive availability, D4 passes the immutable factual snapshot to the
  provider; availability is not itself an automatic capability formula. D5
  rebuilds D3 and recomputes D4, so relevant availability changes invalidate
  plans while unrelated changes do not.
- No casualty rule, consequence policy, battle application, population death,
  Person accounting, event/history, persistence, or daily military behavior was
  added. No production manpower-source adapter or recruitment-capacity policy
  was invented; tests use a deterministic fake provider. `AdvanceDay` and
  `docs/SIMULATION_ARCHITECTURE.md` are unchanged.

### Validation and review

- D6A focused: `11/11`; D5 planning: `16/16`; D4 raw computation: `10/10`;
  D3 execution context: `11/11`.
- D2 ArmedForce spatial: `11/11`; D1 Battle spatial: `7/7`; D0
  SpatialAuthority: `8/8`.
- ArmedForce foundation: `10/10`; ArmedForce world composition: `6/6`;
  Persistent Conflict/War/Battle: `8/8`; ConflictFoundation: `16/16`;
  SimulationRuntime orchestration: `10/10`.
- Population filter: `131/131`; Person filter: `130/130`; lifecycle filter:
  `40/40`. These overlap with the complete suite. A focused D6A test confirms
  allocation changes only military roster state: settlement population and its
  revision remain unchanged, and no Person is added.
- ALL EditMode: `1557/1557`. Official complete EditMode `Smoke`: `5/5`.
- All listed focused filters, ALL EditMode, and official Smoke were rerun in
  the D6A integration worktree before promotion.
- Independent read-only review of the integration found no blocker. It noted
  one non-blocking diagnostic wording mismatch: a no-combat-elements message
  names `Amount` although eligibility uses `AvailableAmount`; the validated
  candidate was preserved without expanding this promotion into a code fix.
- `git diff --check` is clean for the final integration state. `AdvanceDay`
  has no diff; no long-run test was required because daily behavior is unchanged.

## Checkpoint D6B1 — Approved and promoted to canonical

- Previous canonical baseline: `1c3fa501e6cfb45a7eec3fbfb7386848d3d1c6af` on
  `codex/phase7/canonical`.
- Validated feature candidate: `codex/phase7/ManpowerSourceConsequencePlanning`
  at `8e6cc5793872367e4f43732e1b9bf901c327d495`. The feature branch remains
  unchanged. Implementation commit:
  `dd28f201561ad51a3c33a979d4ac824b6136bf77`.
- Promotion branch: `codex/phase7/D6B1CanonicalPromotion`, created at the
  candidate SHA above. Promotion adds this state-document update and
  fast-forwards canonical linearly; no merge commit or architecture
  reconciliation is needed.

### Delivered

- D6B1 establishes an explicit source-effect boundary: supported positive
  aggregate Death effects are routed by stable `ManpowerSourceId` through an
  explicit world-composed registration to the exact `CityRuntime` and
  `SettlementPopulationRuntime`. No source ID parsing or inferred settlement
  routing is used. Duplicate source IDs, duplicate settlement authorities,
  foreign city instances, and cross-`SimulationRuntime` registry reuse are
  rejected.
- The settlement registration supplies military capacity explicitly and
  independently from factual living population. A D6A snapshot provider alone
  does not imply that a D6B1 consequence planner is configured.
- The settlement adapter proposes the exact immutable
  `AggregateDemographyTransition` through `AggregateDemographySystem.TryPropose`.
  It uses the current represented-resident floor from
  `SettlementPopulationPresenceQuery`, protects
  `PopulationAfter >= RepresentedResidentFloor`, and rejects Death amounts
  outside the positive `Int32` range without truncation.
- Planning and freshness validation are non-mutating. The deterministic
  dependency fingerprint covers source/settlement identity, population
  revision/current value, represented-resident floor, planner identity/version,
  and effect kind/amount. Population and floor changes stale a proposal;
  capacity and unrelated source changes do not. Planner identity metadata is
  captured at composition and observable changes are rejected; arbitrary
  implementation immutability remains a composition contract.
- No Battle casualty decision, RNG, roster death, population mutation, source
  mutation, Battle outcome persistence/resolution, or event/history is added.

### Validation and review

- D6B1 focused: `23/23`; D6A manpower foundation: `11/11`; aggregate
  demography: `23/23`.
- D5 Battle outcome planning: `16/16`; D4 resolution computation: `10/10`;
  D3 execution context: `11/11`; D2 ArmedForce spatial: `11/11`; D1 Battle
  spatial: `7/7`; D0 SpatialAuthority: `8/8`.
- ArmedForce foundation: `10/10`; ArmedForce world composition: `6/6`;
  Persistent Conflict/War/Battle: `8/8`; ConflictFoundation: `16/16`;
  SimulationRuntime orchestration: `10/10`.
- Population filter: `136/136`; Person filter: `130/130`; lifecycle filter:
  `40/40`.
- ALL EditMode: `1580/1580`. Official complete EditMode `Smoke`: `5/5`.
- Independent read-only conformance review found no blocker. It confirmed the
  boundary is only explicit source effect → immutable source-domain proposal;
  no D6B2 casualty decision or D7 atomic application is present.
- `git diff --check` is clean. `SimulationRuntime.AdvanceDay` and
  `docs/SIMULATION_ARCHITECTURE.md` are unchanged. No long-run suite was
  required because D6B1 is non-mutating and adds no daily behavior.

### Current boundary after D6B1

D6B1 and D6B2 are approved and promoted to `codex/phase7/canonical`.
Checkpoint D6B2 is the current canonical checkpoint, described below. D7 is the
next checkpoint and remains **NOT STARTED**. Do not apply a Battle outcome,
mark a Battle Resolved, apply population deaths, mutate Person state, emit
outcome history, or add daily military behavior from D6A/D6B1/D6B2.

Cross-host equivalence of D4's existing floating-point arithmetic remains an
unrelated open limitation.

## Checkpoint D6B2 — Approved and promoted to canonical

- Previous canonical baseline: `59fdab5100e27672d9ef9096761d72ed04090790` on
  `codex/phase7/canonical`.
- Feature branch: `codex/phase7/BattleDirectConsequencePlanning`.
- Validated feature candidate (preserved unchanged):
  `450dfbf6389e807618231851cf533b3286ed3275`.
- Implementation commit: `16621d2eb91a90e6dd0298dcc5e7c3cc441a60ba`.
- Candidate state-document commit:
  `450dfbf6389e807618231851cf533b3286ed3275`.
- Promotion branch: `codex/phase7/D6B2CanonicalPromotion`, created directly
  from the validated candidate. Since the candidate was a linear descendant of
  the previous canonical baseline (ahead 2, behind 0), promotion uses a
  fast-forward with no merge commit.
- D6B2 is approved and promoted to `codex/phase7/canonical` after independent
  review and final validation in a clean promotion worktree.

### Delivered

D6B2 adds a world-composed, deterministic, non-mutating Battle-level direct
consequence planning boundary. It layers over a fresh D5 application plan and
the matching D3/D6A facts; it does not modify D5's plan or claim application
readiness.

- `BattleDirectConsequencePolicy` is explicit and optional. There is no
  production casualty rule or consequence RNG. Its stable identity and
  fingerprint capture the rule key, configuration identity/version, and D6B2
  plan/coverage versions. Observable rule-identity changes fail closed.
- One rule invocation receives one immutable, canonical input for the entire
  Battle. D5 remains the sole outcome authority; raw D4 values and live stores
  are not exposed to the rule.
- Only D3-captured, D6A-bound free and available cohorts are exposed. Every
  exposed cohort requires one explicit, checked-conservation partition.
  Healthy may remain healthy or become wounded; wounded cannot heal. Captured
  survivors require explicit unavailable custody by an active participant on
  another Battle side. Draw does not imply a winner or capture.
- Positive deaths require a source binding. D6B2 checked-sums deaths by
  `ManpowerSourceId`, preserves cohort-to-source traces, and requests exactly
  one D6B1 proposal per affected source. A failed/unsupported D6B1 proposal
  prevents any complete plan from being returned.
- Immutable post-consequence contingent projections are derived from the
  pre-state plus partitions. They preserve unexposed cohorts, merge equal
  semantic cohorts, and expose derived living-roster and available totals.
- The complete plan carries stable semantic fingerprints and validates the
  current day, D5 outcome/context, D6B2 policy, participant manpower,
  custodians, and affected D6B1 proposals. Capacity-only and unrelated-source
  changes do not invalidate an otherwise current proposal. Invalid partitions
  and destinations are validated in stable semantic order for deterministic
  diagnostics.
- Planning changes no Battle lifecycle/outcome, D6A manpower or legacy
  `Contingent.Amount`, source population/revision, ArmedForce position, Person,
  or simulation day. `SimulationRuntime.AdvanceDay` is unchanged.

### Validation and independent review

- D6B2 focused EditMode: `21/21`.
- D6B1: `23/23`; D6A: `11/11`; D5: `16/16`; D4: `10/10`; D3: `11/11`.
- D2 spatial: `11/11`; D1 spatial: `7/7`; D0 spatial: `8/8`.
- ArmedForce foundation: `10/10`; ArmedForce composition: `6/6`;
  Persistent Conflict/War/Battle: `8/8`; ConflictFoundation: `16/16`;
  SimulationRuntime orchestration: `10/10`.
- Population: `137/137`; Person: `130/130`; lifecycle: `40/40`;
  aggregate demography: `23/23`.
- Final promotion validation: ALL EditMode `1601/1601`; official complete
  EditMode `Smoke`: `5/5`; `git diff --check` clean.
- Independent read-only promotion review approved. It confirmed partitions are
  the sole authored consequence proposal, source death totals and post-state
  are derived, same-source deaths produce one traceable D6B1 proposal,
  freshness guards remain scoped, and D6B2 is non-mutating with D7 absent.
- No long-run suite was required because `AdvanceDay` is unchanged and D6B2
  adds no daily behavior.

### Promoted boundary and known limitations

D6B2 is **APPROVED AND PROMOTED TO CANONICAL**. No consequence is applied, no
`BattleOutcome` is persisted, and there is no D7 atomic application. Production
worlds without an explicit D6B2 policy fail as unconfigured; casualty
semantics remain entirely rule/configuration-owned. The post-consequence
projection is ephemeral and is not a persistence, event-history, or replay
contract. D7 is the next checkpoint and remains **NOT STARTED**.
`docs/SIMULATION_ARCHITECTURE.md` was not changed.

# Historical record — Checkpoint D5 — Canonical Promotion

## D5 baseline, integration, and canonical status

- Canonical baseline: `eef60513fed42e6b8a8660efd8e9cc9fe4f1538b` (D4 state).
- D5 feature branch: `codex/phase7/BattleOutcomePlanning`, ending at
  `e1e65b7bcf3db37c3300e9045d99931bc556bc92`.
- Implementation commit: `bcca17580ae216b4f20fc4e11a4082038cd015c8` —
  world-authorized Battle resolution
  policy, semantic outcome, immutable non-committable application plan,
  recomputation/current-plan validation, and focused tests.
- Consolidated architecture commit: `9af971bea53c5548c81e8506b033b92c5ea0f12a`
  on `codex/phase7/BattleRawResolution`; its only change relative to the common
  baseline is `docs/SIMULATION_ARCHITECTURE.md`.
- Integration branch: `codex/phase7/D5CanonicalIntegration`. Merge commit
  `99f0c284750b9f87fdaa21241b67b968277235d2` preserves the D5 candidate as its
  first parent and the architecture commit as its second parent.
- D5 is approved and promoted as the Phase 7 canonical checkpoint. The
  validated `codex/phase7/canonical` promotion contains both the D5
  implementation and the consolidated D6A/D6B/D7 architecture. This state
  document update is included in the final promotion commit.

## D5 delivered

D5 adds the first world-bound authorization and semantic outcome boundary. It
ends with an ephemeral proposal and does not apply Battle consequences or
mutate world truth.

- `BattleResolutionPolicy` is explicitly composed through `SimulationRuntime`.
  The runtime exposes only the immutable policy metadata and a bound
  `BattleOutcomePlanningService`; policy replacement is not supported. When
  no policy is supplied, the service remains available and reports
  `PolicyNotConfigured` rather than creating a production default.
- The policy captures the capability and contextual-random RuleKeys,
  immutable resolver settings, supported D4 projection version, required
  numeric execution profile key, and an explicit host-composition support
  declaration. Its SHA-256 semantic fingerprint uses canonical, ordinal,
  length-delimited values; it does not use object identity, `GetHashCode`,
  runtime addresses, or insertion order. Relevant configuration changes
  change the fingerprint.
- Numeric profile identity and current-host support are required policy
  inputs. Unsupported profiles fail before raw resolution. The compatibility
  declaration is explicit composition metadata; it does not establish
  cross-host or future `Simulation.Core` numeric equivalence. D4's float
  arithmetic remains a known limitation.
- Capability and random implementations are retained by reference only under
  their explicit contracts: composed capability rules remain immutable and
  pure, contextual random sources remain immutable/stateless, and every
  behavior/configuration change requires a new stable `RuleKey`. D5 captures
  and checks the keys at composition and planning boundaries; it rejects an
  observable key change.
- Battle policy is a domain-specific composed-policy bridge. D5 does not add
  Battle configuration to the broad `EffectiveSimulationConfiguration`
  foundation.
- The authoritative request begins with `BattleId`, optional D3
  `BattleExecutionPlan` commander metadata, and an optional expected causal
  fingerprint. The service reads the runtime's current logical day, rebuilds
  the current D3 context from its world-owned builder, and recomputes through
  its internally bound D4 service. Callers cannot supply an authoritative
  context, computation, capability provider, random source, or resolver
  settings.
- A supplied expected fingerprint is only a preview-confirmation
  precondition: `null` omits it, and any supplied value (including empty) must
  match the current authorized D4 causal fingerprint. Authorization always
  comes from the runtime policy and fresh recomputation.
- The recomputed context/day is revalidated after resolution as well as by D4
  before resolution. `TryValidateCurrent` reconstructs the context and
  recomputes under the same world policy, then checks current day, policy
  identity, context fingerprint, causal fingerprint, and semantic outcome.
  Unrelated force changes remain non-invalidating; current day, relevant
  participant position, and direct contingent changes make the plan stale.
- `BattleOutcome` is minimal: `Victory` or `Draw`; Victory maps through D4's
  explicit side mapping to one typed `BattleSideId`, while Draw has no winner.
  It carries the current logical day and immutable provenance for policy,
  profile, projection, D4 causal result, context, rules, and settings. Raw
  scores remain only on the plan's separate ephemeral D4 computation.
- `BattleOutcomeApplicationPlan` is immutable and ephemeral. It carries the
  outcome, policy identity, source context/dependencies, optional execution
  metadata, and D4 computation. `DirectConsequenceStatus` is
  `NotProvided`; completeness and commit-readiness are always false. D5 has no
  apply API and does not represent the missing consequence model as zero
  casualties.
- D5 does not change `PersistentBattleRecord` or `PersistentBattleStore`,
  snapshots/diagnostics, `Contingent.Amount`, lifecycle, population, position,
  events, or history. Battles remain `Active`; D5 provides no world mutation.
  `SimulationRuntime.AdvanceDay` is unchanged. The D5 feature branch did not
  edit `docs/SIMULATION_ARCHITECTURE.md`; canonical integration includes the
  separate consolidated architecture commit listed above.

## D5 validation

- D5 focused EditMode, rerun on the integrated tree: `15/15`.
- D4 raw resolution: `9/9`; D3 execution context: `10/10`.
- D2 ArmedForce spatial: `11/11`; D1 Battle spatial: `7/7`; D0
  SpatialAuthority: `8/8`.
- ArmedForce foundation: `10/10`; Persistent Conflict/War/Battle: `8/8`;
  ConflictFoundation: `16/16`; SimulationRuntime orchestration: `10/10`.
- ALL EditMode, rerun on the integrated tree: `1543/1543`. Official complete
  EditMode `Smoke`, rerun on the integrated tree: `5/5`.
- ALL EditMode included the diagnostics, Person, Population, lifecycle, and
  travel regression suites.
- Independent read-only architecture/correctness review: no remaining D5
  blocker. The review confirmed that provider immutability/purity and
  random-source statelessness are composition contracts; arbitrary
  implementations cannot be mechanically frozen by the runtime.
- Independent read-only conformance review of the integrated tree found no
  D5/D6 boundary blocker or conceptual contradiction. `git diff --check` is
  clean for the integrated state, including this state-document update.

## D5 boundaries and next gate

No long-run suite was required because `AdvanceDay` is unchanged and D5 adds no
daily behavior. At the time of D5 promotion, D6A was the next architecture
checkpoint and no D6A implementation was included in that promotion. That is
historical status; D6A was subsequently integrated and promoted as recorded at
the top of this file. D6B1/D6B2 and D7 remain later architecture-gated
checkpoints. Cross-host numeric
equivalence remains unresolved. Consequences, casualties, availability/custody
implementation, lifecycle transition, application, events/history, save/load,
and replay remained deferred at the D5 checkpoint.

## Historical record — Checkpoint D4

## D4 baseline, branch, and implementation

- Canonical architecture baseline: `1a100bd6b14ed3545e130578ae0a874ce9e6d2d4`.
- Checkpoint branch: `codex/phase7/BattleRawResolution`.
- Implementation commit: `fc397ada3bab4b675437d5f970b3eb675bf2d907` —
  deterministic Battle-to-Conflict raw projection, contextual random
  authority, immutable computation, and tests.

## D4 delivered

D4 adds the first narrow raw Battle computation boundary. It consumes an
explicit D3 `BattleExecutionContext`; it does not reconstruct or persist
execution state.

- `BattleResolutionComputationService` revalidates the context against current
  world state before invoking capability rules or randomness. Invalid and
  stale contexts return no computation and consume no random draws.
- Capability is supplied by an explicit pure
  `IBattleContingentCapabilityProvider` with a stable semantic `RuleKey`. There
  is no production default formula. A guard fails if any NPC participant ever
  reaches the aggregate-only resolver.
- Every direct contingent captured in the context projects to exactly one
  aggregate lower-level participant, including zero-amount contingents.
  Lower-level `Count` remains null; `Amount` remains a long in the typed
  mapping. Namespaced `SourceId` values derive from Battle, force, and
  contingent semantic IDs.
- All Battle sides are preserved in ordinal order. Transitional lower-level
  objective/stakes are `Other`/`Low`; no modifiers are added; lower-level
  `LocationRuntimeId` is null.
- The causal fingerprint uses the projection version, Battle/day, ordered
  side-force-contingent identities, captured contingent source/service/
  characteristics/amount, projected capability, capability and random
  authority RuleKeys, and explicit immutable resolver settings. Commander
  metadata and spatial references are excluded because neither participates
  in this capability rule or raw projection. The adapted ConflictId derives
  from BattleId, execution day, and this fingerprint.
- `DeterministicBattleConflictRandomSource` adapts the existing keyed
  deterministic random foundation. Its operation keys are stateless and
  contextual; unrelated Battle evaluation order does not advance a shared
  sequence.
- The returned `BattleResolutionComputation` is ephemeral and exposes the raw
  `ConflictResolutionResult` plus immutable typed side and participant
  mappings. It calls only `ConflictResolver.Resolve(..., constraints: null)`;
  no consequence resolver, world mutation, event, or history path is used.

## D4 validation

- D4 focused EditMode: `9/9`.
- ALL EditMode: `1528/1528`.
- Official complete EditMode `Smoke`: `5/5`.
- Independent read-only architecture/conformance review: no implementation
  boundary violations found; review-identified test gaps were covered before
  the final test runs.
- `git diff --check`: clean before the final documentation commit.
- `AdvanceDay` and `docs/SIMULATION_ARCHITECTURE.md`: unchanged.

## D4 known limitations and deferred

The lower-level resolver retains its existing float arithmetic. Repeatability
was tested in the current Unity/runtime environment, but cross-host or future
`Simulation.Core` numeric determinism is unproven and remains a required gate
before accepting or persisting a Battle outcome.

This computation is not an accepted or persistent Battle result. The Battle
remains `Active`; outcome acceptance, lifecycle transition, consequences,
casualties, retreat/rout/surrender, capture, logistics, movement, events,
history, persistence/replay, and implementation of later checkpoints remain
deferred. The post-D4 architecture gate has now defined the conceptual
sequence in `docs/SIMULATION_ARCHITECTURE.md`: D5 establishes authorized
resolution policy and an immutable outcome/application-plan contract without
mutation; D6 is the manpower/availability/casualty foundation; D7 is the later
atomic application boundary. D6 and D7 remain subject to their own gates.

D3 checkpoint record retained for historical context:

## D3 baseline, branch, and commits

- Canonical architecture baseline: `fc051d9dadefd2579dfc209ace4a7103b0ffc60a`.
- Checkpoint branch: `codex/phase7/BattleExecutionContext`.
- Implementation commit: `48ecd22` — BattleExecutionContext contracts, builder,
  dependency fingerprints, stale validation, runtime composition, and focused
  tests.

## D3 delivered

Checkpoint D3 adds the smallest ephemeral execution boundary between
persistent Battle state and a future Battle/Conflict outcome. It does not
resolve a Battle, consume RNG, mutate world state, or adapt ConflictFoundation.

- `BattleExecutionContext` is an immutable, non-authoritative capture of one
  explicit Battle execution attempt. It is not stored in
  `PersistentBattleStore`, does not receive a lifecycle, and is not projected
  into `WorldStateSnapshot`.
- `BattleExecutionContextBuilder` consumes the composed Battle, ArmedForce,
  ArmedForce spatial-position, SpatialAuthority, LocalTopology, and Person
  stores. It exposes `TryCreate` with explicit failure codes and a convenience
  overload without a plan.
- Eligibility requires an existing Active Battle, a started day, a valid typed
  Battle location, at least two sides, explicit participant bindings, active
  participant forces, current typed participant positions, and directional
  physical compatibility with the Battle location.
- Composition projects only each explicitly bound force's direct contingents.
  Parent/child hierarchy, detach state, commanders, allies, War, and
  Conflict are never expanded. Zero-amount contingents remain captured but do
  not satisfy a side's direct combat-element requirement.
- Every side must have at least one explicit participant force with a direct
  contingent whose amount is greater than zero. A command-only parent may
  coexist with an explicitly bound combat-capable child.
- Duplicate explicit ArmedForce bindings and duplicate ContingentId
  projections are rejected without mutating persistent Battle state.
- `BattleExecutionPlan` is ephemeral and contains only optional explicit
  `BattleSideId -> PersonId` side-commander metadata. It does not imply
  ownership, membership, allegiance, loyalty, office, co-location, or force
  command.
- Dependency fingerprints capture only the target Battle, explicit bindings,
  direct contingent composition, relevant current positions, and copied plan
  metadata. They use stable semantic keys and deterministic ordering rather
  than store revisions or unrelated world state.
- `TryValidateCurrent` rechecks the relevant world truth without mutation or
  RNG and distinguishes current, stale, and malformed contexts with
  deterministic stale reasons.
- `SimulationRuntime.BattleExecutionContextBuilder` is bound to the runtime's
  cloned stores. No context is retained as authoritative runtime state.

## D3 validation

- D3 focused EditMode: `10/10`.
- ArmedForce foundation regression: `10/10`.
- D2 ArmedForce spatial regression: `11/11`.
- D1 Battle spatial regression: `7/7`.
- Persistent Conflict/War/Battle regression: `8/8`.
- D0 SpatialAuthority regression: `8/8`.
- Person-related regression: `129/129`.
- Population regression: `130/130`.
- Lifecycle regression: `40/40`.
- ConflictFoundation regression: `17/17`.
- ALL EditMode: `1519/1519`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before documentation commit.

## D3 boundaries and known limitations

The context is an input capture and eligibility/stale boundary only. It has no
combat power formula, ArmyStrength, morale, cohesion, readiness, tactics,
plans/orders, operational groups, resolution, outcome, casualties, logistics,
movement, scouting, military knowledge, aftermath, or population accounting.
Person references are validated by PersonId existence only; no Person spatial
position or NpcRuntime relationship is inferred. The dependency fingerprint is
semantic diagnostic state, not save/load, replay, or event sourcing.

`PersistentBattleStore` still does not gate registration, participant binding,
or Battle start on participant position. An Active Battle may exist without a
currently eligible execution context. `AdvanceDay` remains unchanged and no
military daily processing or autonomy was added.

## D3 deferred

P7-D4 remains deferred: the ConflictFoundation adapter and the first Battle
resolution. Persistent strategic War behavior, tactics, morale, cohesion,
readiness, supply, logistics, funding/pay, requisition, foraging, military
movement, scouting, military knowledge, recruitment, mobilization, casualties,
capture/custody, desertion, defection, mutiny, military control, occupation,
war goals, ceasefire, peace, taxation, diplomacy, Campaign, Polity, WarAI,
continuity operations, save/load, replay, networking, and broad
`Simulation.Core` migration remain deferred.

## D3 architecture conformance

Read-only review found no contradiction with `docs/SIMULATION_ARCHITECTURE.md`.
ArmedForce remains distinct from Faction, Institution, Polity, and generic
Organization. Command, loyalty, allegiance, membership, funding, and control
remain distinct. Direct participant bindings remain Battle-owned and explicit;
physical position remains separate from organizational hierarchy and
detachment. Person identity remains PersonId-based and independent from
NpcRuntime. No Unity object identity, discovery order, or insertion order is
authoritative. No `ConflictFoundation` remodeling or Battle resolution was
added.

## D2 baseline, branch, and commits

- Canonical architecture baseline: `c7d3341200cfec64700c59429b15b1d7dd2a5992`.
- Checkpoint branch: `codex/phase7/ArmedForceSpatialPosition`.
- Implementation commit: `7480e5d` — authoritative ArmedForce spatial
  position state, runtime composition, and diagnostics integration.
- Focused test commit: `1234c93` — D2 spatial position coverage.
- Fixture correction commit: `4f7ee64` — runtime clone test setup.

## D2 delivered

Checkpoint D2 adds the minimum authoritative optional current physical
position bridge for ArmedForce. It does not implement movement, Battle
resolution, logistics, recruitment, or military daily processing.

- `ArmedForceSpatialStateStore` owns optional typed current positions keyed by
  stable `ArmedForceId`. It is separate from force identity, hierarchy,
  contingents, relevant Person references, lifecycle, detachment, and the
  legacy opaque location field.
- Position updates and clears require an existing active force, validate the
  typed reference against the supplied D0 `SpatialAuthorityStore`, apply
  atomically, advance a dedicated revision, and keep no-op operations at the
  same revision.
- Positions are optional and are never inferred or propagated between parent,
  child, contingent, commander, detachment, or reattachment. Detach/reattach
  preserves both force identity and any separately stored current position.
- Directional compatibility queries are deterministic and typed: Hex checks
  the resolved anchor Hex; Location checks exact Location identity; SubLocation
  checks exact SubLocation identity. Missing current position is a valid
  incompatible result, while invalid or unresolved references are query
  failures.
- `SimulationRuntime` composes the spatial state against its cloned
  `ArmedForceStore` and cloned `SpatialAuthorityStore`, retaining the explicit
  LocalTopology bridge when SubLocation resolution is required.
- Snapshot, canonical writer, formatter, diff, and invariant validation now
  project `ARMED_FORCE_POSITION` state and its revision deterministically.
  `OperationalLocationReference` remains only as a transitional legacy shim;
  it is never converted into typed position and is not emitted as the
  canonical ArmedForce position.
- No `NpcRuntime` is required or materialized by spatial position state.

## D2 validation

- D2 focused EditMode: `11/11`.
- ArmedForce regression: `29/29`.
- SpatialAuthority D0 regression: `9/9`.
- BattleSpatial D1 regression: `7/7`.
- Persistent Conflict/War/Battle regression: `8/8`.
- Person regression: `128/128`.
- Population regression: `130/130`.
- ConflictFoundation regression: `17/17`.
- Lifecycle regression: `40/40`.
- ALL EditMode: `1509/1509`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before final commit.

## D2 boundaries and known limitations

The spatial state is a current-position authority only. It has no movement,
route, adjacency, terrain, travel-time, scouting, knowledge, battle gating,
participant-position validation, or execution context. Position history is
represented by diagnostics snapshots/revisions only; no event sourcing or
save/load contract was added. LocalTopology remains the existing explicit
SubLocation bridge and is not redesigned here.

`AdvanceDay` was not changed. No military processing, autonomy, cadence, RNG,
population mutation, or ConflictFoundation remodeling was added.
`docs/SIMULATION_ARCHITECTURE.md` was not changed.

## D2 deferred

Persistent strategic War behavior, Battle resolution, tactics, plans, morale,
cohesion, readiness, supply, logistics, funding/pay, requisition, foraging,
military movement, scouting, military knowledge, recruitment, mobilization,
casualties, capture/custody, desertion, defection, mutiny, military control,
occupation, war goals, ceasefire, peace, taxation, diplomacy, Campaign,
Polity, WarAI, physical movement integration, participant-position gating,
continuity operations for secession/schism/absorption/merger, save/load,
replay, networking, and broad `Simulation.Core` migration remain deferred.

No P7-B, D3, or later checkpoint was started automatically.

P7-D3 remains deferred: physical Battle execution eligibility,
`BattleExecutionContext`, direct combat-element projection, side commander,
dependency fingerprints, and stale-context handling. P7-D4 remains deferred:
ConflictFoundation adapter and the first Battle resolution.

## D2 architecture conformance

Read-only review found no contradiction with
`docs/SIMULATION_ARCHITECTURE.md`. ArmedForce remains distinct from Faction,
Institution, Polity, and generic Organization. Command, loyalty, allegiance,
membership, funding, and control remain distinct. Physical separation remains
distinct from organizational separation. Person identity remains PersonId
based and independent from NpcRuntime. No Unity object identity or discovery
order is authoritative.

## D1 baseline, branch, and commit

- Canonical architecture baseline: `775edc758fcb8e0f0baf6768f2c89190eca403fd`.
- Checkpoint branch: `codex/phase7/BattleSpatialBinding`.
- Implementation commit: `9b100c7` — Battle spatial binding, world-bound
  composition, diagnostics, and focused coverage.

## D1 delivered

Checkpoint D1 makes `PersistentBattle` the first explicit consumer of the D0
world-bound `SpatialAuthority`, without implementing battle resolution,
movement, terrain, or military position validation.

- `PersistentBattleRecord` carries an optional typed D0 `SpatialReference`.
- Pending Battles may omit location or carry a valid Hex, Location, or
  SubLocation reference.
- Pending -> Active requires a valid explicit physical reference. The legacy
  `TryStart` overload only reuses a location already explicitly present on the
  Pending record; it never infers one from participants or runtime objects.
- `PersistentBattleStore` validates Hex, Location anchor, and LocalTopology
  SubLocation references against its bound spatial authority before mutation.
  Failed validation leaves both Battle state and store revision unchanged.
- `SimulationRuntime` composes BattleStore against its cloned world-bound
  `SpatialAuthorityStore`; source authority mutation does not mutate the
  composed runtime authority.
- Active Battle location is immutable in this slice. No move/change-location
  operation, resolution transition, winner, result, casualty, aftermath, or
  Battlefield entity was added.
- Battle diagnostics now project location, canonicalize its stable key, diff
  Pending location changes, format it, and validate missing/malformed or
  unresolved Hex/Location/SubLocation references.

## D1 validation

- D1 focused EditMode: `7/7`.
- Persistent Conflict/War/Battle regression: `8/8`.
- SpatialAuthority D0 regression: `8/8`.
- LocalTopology regression: `17/17`.
- ALL EditMode: `1498/1498`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before commit.

## D1 boundaries and known limitations

The LocalTopology bridge retains its existing transitional RuntimeId-based
owner/place contract. SimulationRuntime clones the spatial authority; the
optional LocalTopology bridge remains the supplied consumer boundary and is
not migrated or redesigned here. Diagnostics remain projections, not a
save/load contract.

`AdvanceDay` was not changed. No Battle starts automatically, no location is
assigned automatically, and no daily spatial or military processing was
added. `docs/SIMULATION_ARCHITECTURE.md` was not changed.

## D1 deferred

City/Site legacy mapping, Crossing references, Hex adjacency, terrain,
Travel rewrite, military movement, participant position validation, Battle
resolution, ConflictFoundation adapters, Battlefield/aftermath, operational
grouping/command, casualties, logistics, recruitment, mobilization,
knowledge, save/load, and broad `Simulation.Core` migration remain deferred.
No next checkpoint is started by D1.


## D0 baseline, branch, and commit

- Canonical architecture baseline: `9e922550d1bd0ed6ca6ddffc1538809960208a10`.
- Checkpoint branch: `codex/phase7/SpatialAuthorityBridge`.
- Implementation commit: `86632ea` — minimal Hex/Location spatial authority,
  typed spatial references, LocalTopology bridge, runtime composition, and
  deterministic diagnostics.
- Follow-up commit: `864c600` — include spatial authority in the human
  diagnostics formatter and its focused regression assertion.

## D0 delivered

Checkpoint D0 adds the smallest authoritative world-bound physical reference
layer without implementing a HexGrid, traversal, pathfinding, or Battle
resolution.

- `HexId` and `LocationId` are stable typed identities independent of Unity,
  `NpcRuntime`, `RuntimeIdAllocator`, discovery order, and insertion order.
- `HexRecord` is identity-only. `LocationRecord` has exactly one immutable
  `AnchorHexId`; registration validates the anchor before mutation.
- `SpatialReference` is typed for Hex, Location, and SubLocation. SubLocation
  references use explicit existing LocalTopology owner/place fields rather than
  an opaque string.
- `SpatialAuthorityStore` owns Hex/Location records, explicit topology-owner
  bindings, deterministic ordering, revision, cloning, resolution, and
  invariant validation. Mutations validate before applying and reject revision
  overflow without partial state changes.
- LocalTopology remains the local topology authority. The bridge resolves
  `SubLocation -> LocalTopology -> Location -> AnchorHex` only when the
  consumer supplies the existing `LocalTopologyStore`; City and ExplorableSite
  were not migrated into Location records.
- `SimulationRuntime` composes a cloned spatial authority without adding daily
  processing or changing `AdvanceDay`.
- Snapshot context, snapshot projection, canonical writer, diff, and invariant
  validation expose only the authoritative spatial records and revision.

## D0 invariants and determinism

Containment, same-Hex, and connectivity remain distinct. Hex adjacency,
traversability, distance, travel time, knowledge, and execution are not
introduced. Moving entities remain separate from Location. No terrain, weather,
travel costs, barriers, crossings, RNG, or Unity object identity was added.
Equivalent authoritative state produces sorted, insertion-order-independent
Hex/Location projections and canonical output.

## D0 validation

- D0 focused EditMode: `8/8`.
- ALL EditMode: `1491/1491`.
- Official EditMode `Smoke`: `5/5`.
- `git diff --check`: clean before documentation update.
- Unity `6000.3.9f1` was run in an isolated validation worktree because the
  primary checkout had an interactive Editor instance open.

## D0 deferred and known limitations

Full HexGrid, adjacency, terrain, barriers, crossings, scale/configuration,
route/travel rewrite, knowledge integration, movement, military use, Battle
location assignment, Battle resolution, aftermath, save/load, and broad
`Simulation.Core` migration remain deferred. Spatial authority does not yet
automatically map legacy City/Site runtime locations; such mapping remains an
explicit future integration. Spatial diagnostics are projections, not a
save/load contract.

`AdvanceDay` was not changed. `docs/SIMULATION_ARCHITECTURE.md` was not changed.

## Historical Checkpoint C — Baseline and branch

## Baseline and branch

- Canonical architecture baseline: `2419d786602e8fa1250df7a0ed7326e236218419`.
- Checkpoint branch: `codex/phase7/ConflictWarBattleState`.
- Implementation commits:
  - `e472b2d` — persistent Conflict/War/Battle contracts, stores, and
    `SimulationRuntime` composition.
  - `83ee003` — diagnostics projections, canonical/diff/formatter/invariants,
    and focused tests.

## Delivered

Checkpoint C adds the minimum persistent world-state boundaries for Conflict,
War, and Battle without implementing a war system or battle resolution.

- Stable typed `ConflictId`, `WarId`, and `BattleId` identities are independent
  of `NpcRuntime`, Unity object discovery, and insertion order.
- Conflict, War, and Battle have separate authoritative stores and separate
  domain-owned side and participant-binding types. There is no global SideId,
  universal actor model, or participant hierarchy.
- The only concrete participant kind introduced is explicit `ArmedForceId`.
  Bindings are owned by their Conflict/War/Battle store, require an existing
  force, and new current bindings require an active force.
- Conflict and War support `Active -> Ended`; Battle supports `Pending -> Active`.
  Battle resolution is explicitly deferred; `Resolved` is representable only
  as a diagnostics/historical enum and cannot be registered or transitioned by
  this checkpoint.
- War optionally references a Conflict. Battle optionally references a War
  and/or Conflict. When both Battle references exist, contradictory War-to-
  Conflict links are rejected.
- Store mutations validate before applying, advance only after successful
  application, and leave state/revision unchanged on failure. Historical
  references to terminated ArmedForces remain representable in registered
  records; newly added bindings cannot target terminated forces.
- Stores are cloned into `SimulationRuntime` against the resolved
  `ArmedForceStore`. Snapshot context, canonical output, human formatter, diff,
  and world invariant validation expose the new state deterministically.
- `ConflictFoundation` and `ConflictResolutionService` remain unchanged and
  lower-level; no adapter was introduced.

## Invariants preserved

Armed Force remains distinct from Faction, Institution, Polity, and generic
Organization. Conflict/War/Battle side identity is domain-owned. Command,
loyalty, allegiance, membership, funding, and control remain distinct.
Person identity remains `PersonId`-based and does not require an
`NpcRuntime`. Physical separation does not imply organizational separation;
no hierarchy propagation is performed for participant bindings.

No event sourcing, DomainEvent additions, RNG, Unity-dependent domain state,
global ad-hoc lists, or military daily processing was added.

## Validation

- Checkpoint C focused tests: `8/8`.
- Focused/regression filter covering ArmedForce diagnostics, runtime
  orchestration, ConflictFoundation, Person, and Population: `245/245`.
- ALL EditMode: `1483/1483`.
- Official EditMode `Smoke` filter: `5/5`.
- `git diff --check`: clean before commit.
- Unity `6000.3.9f1` was used directly in batchmode. The repository wrapper
  was not used because its `Get-CimInstance` process snapshot was denied in
  this environment.

## AdvanceDay and architecture conformance

`SimulationRuntime.AdvanceDay` was not changed. No military processing,
autonomy, cadence, or RNG was added. `docs/SIMULATION_ARCHITECTURE.md` was not
changed. Read-only conformance review found no design contradiction requiring
an architecture decision.

## Deferred

Persistent strategic war behavior, battle resolution/result payloads, tactics,
plans, morale, cohesion, readiness, supply, logistics, funding/pay,
requisition, foraging, movement, scouting, military knowledge, recruitment,
population mobilization accounting, casualties, capture/custody, desertion,
defection, mutiny, military control, occupation, war goals, ceasefire, peace,
taxation, diplomacy, Campaign, Polity, and WarAI remain deferred.

Also deferred are participant kinds beyond ArmedForce, side switching/exit and
re-entry semantics, ancestor/descendant overlap policy, Battle location,
continuity operations for secession/schism/absorption/merger, save/load,
replay, networking, and broad `Simulation.Core` migration.

## Known limitations

The stores are composed at the current `SimulationRuntime` boundary. Snapshot
and canonical output are diagnostics projections, not save/load contracts.
No consumer performs military simulation or modifies population/manpower as a
consequence of these records.

## Recommendation for P7-D

Add the next boundary only after choosing and testing the explicit domain
consumer for these persistent records. Keep participant bindings explicit and
domain-owned, and do not add battle resolution, military daily processing, or
continuity heuristics to the foundation layer.
