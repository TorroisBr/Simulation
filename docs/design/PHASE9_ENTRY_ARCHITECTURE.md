# Phase 9 Entry Architecture Proposal — UNAPPROVED

**Status:** exploratory entry proposal only. It is not an approved architecture,
checkpoint contract, implementation plan, or permission to begin implementation.
No Phase 9 checkpoint IDs are assigned or approved here.

**Review baseline:** canonical architecture/roadmap refresh at
`c285466c355103d3637ac165246591b72eb7bda0`, revalidated against current
Phase 8 state and initialization/content code. This refresh updates the
proposal only; it does not approve its architecture or authorize implementation.

**Current dependency review:** `docs/PHASE8_STATE.md` on
`codex/phase8/canonical` at `1d65e59a4864391f3ed56334454f6eb4b6d71584`
(the Phase 8 State at reviewed baseline `c285466c355103d3637ac165246591b72eb7bda0`).
P8-A/B/C are canonical. P8-D's separate integration candidate needs targeted
architecture-impact/temporal-profile revalidation before promotion; it is not
currently a promoted capability. P8-E is design-approved but waits for
promoted P8-D and that impact review. A P9 profile that consumes route-plan or
route-Knowledge capability inherits the relevant P8-D promotion edge; P8-E
remains no blanket genesis prerequisite.

**Authority:** `docs/SIMULATION_ARCHITECTURE.md` remains the semantic authority.
The Phase 9 and Phase 8 Briefs and `docs/ROADMAP.md` define subordinate scope
and dependencies. This proposal records observed code and possible boundaries;
it does not settle the open decisions below.

## 1. Current facts at the reviewed baseline

- `docs/phases/PHASE9_BRIEF.md` says Phase 9 is `ENTRY_ARCHITECTURE_READY`,
  with no schedulable implementation checkpoint and no approved P9 IDs. It
  requires deterministic, complete initial World Truth before the first
  actually simulated boundary. It defers concrete pass decomposition and
  algorithms, the ID algorithm, mod schema, and exact content catalog; the
  dependency-aware pipeline/contribution contract is now explicit.
- The original proposal described a P7-era state before P8-A existed. Current
  P8 State records P8-A/B/C as canonical. P8-A establishes factual Hex
  geography, scale provenance, terrain identity/revision and anchored
  Locations; P8-B establishes passage/barrier/crossing-condition facts; P8-C
  composes City/Site anchor bindings and Person-level `At`/`InTransit` truth in
  runtime and diagnostics. P8-D/E retain separate readiness gates; only a P9
  profile that consumes route-plan or route-Knowledge capability has a hard
  P8-D promotion edge.
- `TesteSimulacao.InitializeSimulation` is the current authored startup path.
  It reads `SimulationConfigData`, constructs a `SimulationTime`, random
  source, `RuntimeIdAllocator`, `RuntimeIdentityRegistry`, and legacy
  `SpatialNetworkRuntime`, then constructs cities, sites, routes and NPCs from
  authored data and assembles systems. The flow includes separate initial
  knowledge and initial-warrant setup. It is a host bootstrap, not a reusable
  deterministic genesis service or an atomic initial-world package.
- `SimulationConfigData` contains authored lists for cities, sites, named NPCs,
  actions and other definitions/state, plus calendar and a `useFixedSimulationSeed`
  / `simulationSeed` pair. The startup path uses that optional seed for its
  current deterministic random source. The seed has no defined Phase 9
  generation contract and is not, by itself, sufficient continuation state.
- `RuntimeIdAllocator` allocates sequential, per-kind runtime IDs. It does not
  allocate `PersonId`, `HexId`, or `LocationId`, and no generated-identity
  algorithm exists in the inspected scripts. `RuntimeIdentityRegistry` indexes
  runtime representations such as NPCs, cities, locations, routes and sites;
  it is not the semantic identity authority for an initial world.
- `PersonId` is a stable semantic wrapper around a caller-supplied string.
  `PersonStore` owns registered `PersonRuntime` records and their optional
  materialization binding. `SimulationRuntime.TryRegisterNpc` accepts a
  Person-backed NPC only when its Person and materialization binding already
  agree. The legacy startup's `CreateNpcRuntimes` constructs NPCs with runtime
  IDs but does not establish Person records or PersonId bindings.
- `SimulationRuntime` receives preconstructed runtime objects and optional
  stores/configuration/calendar. During composition it clones several supplied
  authorities, binds world mutation guards and registers the NPC roster. Its
  constructor is not itself a domain-neutral generation pipeline or a declared
  pre-start sealing boundary.
- Current P8-A owns factual Hex/Location identities, authored axial geography,
  scale provenance, terrain identity/revision and anchors; P8-B owns passage
  facts; P8-C owns City/Site anchors and Person-level `At`/`InTransit` position.
  The inspected `SpatialNetworkRuntime` remains a separate legacy
  location/route model; P9 must not duplicate or treat it as the promoted P8
  geography authority.
- `SimulationConfigurationResolver` implements
  `Defaults → Preset → World overrides → Content overrides → EffectiveSimulationConfiguration`.
  `SimulationRuntime` separately receives a calendar definition and owns an
  effective calendar copy. Architecture §§12–13 make both the effective
  configuration and calendar authoritative execution inputs; neither is a
  substitute for the concrete world content or full initial World Truth.
- `WorldObserverDemoBootstrap` is another hand-authored demonstration bootstrap
  with fixed example IDs. No general genesis/generation service or
  `PHASE9_STATE.md` exists in the tracked baseline.

The current initialization path remains a host-owned, hand-ordered bootstrap;
no reusable genesis pipeline was found. These facts describe current
implementation, not a recommendation to preserve the legacy startup arrangement.

## 2. Objective and exclusions

The Phase 9 objective, as written in its Brief and Architecture §12, is to
establish the configured initial world as complete semantic `World Truth`
before the first actually simulated boundary. Generated initial facts must use
the same domain ontology and authorities as authored facts. The generation
source may be composed from stages, but it must not be a rendering instruction
or a second runtime mutation authority.

For spatial facts, current P8 State is the promoted capability inventory.
Initial geography can use canonical P8-A. P8-B is available if the selected
initial profile includes passage facts; P8-C is available if it establishes
Person positions or City/Site bindings. Only a profile that includes route
plans or route Knowledge depends on promoted P8-D capability; P8-E remains
outside any blanket P9 prerequisite.

An initial world may have generated backstory before simulation begins. That
backstory can explain initial facts, but it is not simulated history and has no
guarantee of an independently forkable interior. The initial state at the first
simulated boundary must remain reconstructable under compatible semantics.
After that boundary, initial-generation privilege ends; changes use the normal
runtime authorities.

The proposal does not include runtime expansion or construction, existence
created by observation/loading, local generation catalogs or mod schemas,
renderer authority, a save/replay implementation, universal event sourcing,
or the whole Phase 8 civil-travel slice. It does not define generation passes,
an ID algorithm, a content catalog, or exact world scale. If an initial-world
profile needs local sites or local topology, that scope must be coordinated
with the relevant P8/P10 contracts rather than assumed here.

## 3. Candidate pipeline contract — UNAPPROVED

The updated Briefs and architecture alignment settle the pipeline shape at the
semantic level. A later technical design may choose concrete classes and APIs,
but any supported profile must represent a dependency-aware ordered pipeline,
not an opaque whole-world generator. Each stage and contributor declares stable
semantic identity, compatible version/provenance, inputs, outputs and explicit
dependencies. Outputs that become authoritative facts are routed to their
owning domain authorities.

```text
authored inputs + selected generation contributors
   → resolve compatible definitions, effective configuration/calendar,
     contributor versions and generation context
   → validate stage identities, versions, declared inputs/outputs and DAG
   → run stages in deterministic dependency/contributor order
   → stage candidate facts through their owning domain authorities
   → validate complete candidate World Truth and cross-domain references
   → publish/compose the complete initial world as one pre-start result
   → establish the first actually simulated boundary
```

Graph validation and complete-world validation happen before publication; a
failed stage or invariant must not expose a partially generated initial world.
Stages build against unpublished candidate authorities owned by the relevant
domains; only the validated complete result is handed to runtime composition.
The mechanism for candidate isolation and publication remains for technical
design. Generation does not become a second truth store or runtime mutation
authority. This pipeline contract does not require
or wait for P19's public loader/API; selected compatible contributors can be
composed through the eventual semantic seam without implementing that platform.

### Candidate ordered data flow

These are semantic stage roles, not approved stage IDs, a required catalog, or
an instruction that every profile must implement every row. Technical design
must give every actual stage a stable ID and compatible version, declare its
input/output contracts, contributor provenance, dependency edges and
deterministic tie-break behavior.

| Ordered role | Declared inputs | Declared outputs | Ordering / ownership constraint |
|---|---|---|---|
| Resolve profile and context | Authored world inputs; selected contributor set; effective configuration and calendar; compatible simulation/content versions | Immutable resolved profile, provenance, generation context and accepted contributor/stage graph | Resolve and validate compatibility before any stage runs. |
| Establish spatial substrate (when selected) | Resolved profile; authored geography/scale/terrain inputs | P8-owned Hex, Location and selected passage/anchor facts | Use P8-A authority; include P8-B facts only when selected; include P8-C bindings/Person positions only when selected. |
| Produce dependent domain facts | Declared earlier stage outputs and authored domain inputs | Facts for only the included domains, such as population, Person records, settlements, relationships or selected starting state | Edges name the exact consumed outputs. Contributors at the same dependency level use stable contributor/stage identity ordering; conflicts follow a declared deterministic policy, never registration or collection order. |
| Add optional local or temporal facts | Earlier outputs plus the relevant accepted/promoted domain contract | Local topology/content under P10 scope, or temporal activity/commitment state only when the selected profile includes it | Local facts depend on the relevant P8/P9 capability. Temporal facts add only the applicable P18 contract/capability; multi-participant activity/participation facts additionally depend on relevant P20 contracts/capability only when selected. Neither P18 nor P20 is a blanket P9 gate. |
| Validate and publish | All candidate outputs in unpublished candidate domain authorities | One complete validated initial world and its provenance | Validate references, invariants and completeness before exposing the candidate; generated facts remain in their owning stores. |

Randomness used by a stage is derived from stable causal context for that
purpose (including compatible stage/contributor identity and declared causal
inputs), so unrelated random consumption or unrelated stage additions cannot
shift its outcomes. The concrete derivation/ID algorithms remain deferred.
Stable semantic IDs and provenance are required; runtime IDs remain
representation identifiers and cannot stand in for durable generated identity.

The first simulated boundary follows completion and publication of the whole
selected initial profile, including any initial knowledge/commitments that the
profile explicitly owns. A configured calendar date, pre-aged Person, or
generated backstory does not by itself mean that a simulated boundary has
already occurred.

## 4. Candidate scope and dependency map — UNAPPROVED

| Candidate work group (no checkpoint ID) | Possible scope for review | Dependency / readiness note |
|---|---|---|
| Initial-world scope and inputs | Define which initial facts are in the supported Phase 9 profile; separate authored input, generated input, resolved configuration/calendar, and compatible content. | Entry discussion can proceed now. A materially different promise for generated scale or population is a product decision. |
| Identity and provenance boundary | Specify stable semantic identities, compatible contributor/stage versions, inputs and provenance needed for the supported profile and reconstruction. | The requirements are settled by Architecture §§8, 12, 91–92 and the 2026-09-26 alignment. The exact ID/compatibility algorithms remain technical-design decisions. |
| Dependency-aware generation pipeline | Resolve and validate the selected stage/contributor DAG, deterministic contribution/conflict ordering, per-purpose random context, and explicit new-world participation. | Required by the current P9 Brief and architecture alignment. It does not imply P19 loader/API work. |
| Spatial initial facts | Populate factual geography and anchored locations in the accepted spatial ontology. | P8-A is canonical. Add P8-B if the selected profile authors passage facts; add P8-C if it establishes Person positions or City/Site bindings. Route plans/route Knowledge require promoted P8-D only when selected. P8-E is not a blanket dependency. |
| Domain-owned initial facts | Establish the selected population, Persons, settlements, places, relationships, starting state and other chosen facts through their existing domain owners. | Depends on accepted scope, identity and each included domain's contracts. The inventory must not imply every existing domain store is automatically part of the Phase 9 profile. |
| Complete-state validation and publication | Validate the stage graph, complete candidate World Truth, cross-domain references and profile-specific readiness before the world is exposed to runtime. | Depends on selected fact producers. Publication mechanism is technical design; do not turn `SimulationRuntime` into a generation dumping ground. |
| First simulated boundary and reconstruction evidence | Establish the first actually simulated boundary only after the complete selected initial state exists; retain causal inputs/provenance needed by later reconstruction. | Initial-generation privilege ends there. Save/replay mechanics remain Phase 12/13 concerns. |
| Explicit retrofit/migration (later, optional) | Apply a selected contributor's explicit domain migration to an already-running world at a declared post-start boundary, with explicit input boundary and compatibility rules. | Separate later module/domain migration scope. The retrofit result is a change at that boundary; installing or enabling a contributor never reruns historical genesis or silently changes prior placement/state. |

Candidate dependency direction, subject to entry review:

```text
resolved compatible profile + stable contributor/stage contracts
                                │
                                ▼
                    validated dependency DAG
                                │
                                ▼
       deterministic ordered stages / per-purpose random contexts
                                │
                                ▼
                facts in owning domain authorities
                                │
                                ▼
       full-world validation → atomic publication/composition
                                │
                                ▼
                   first simulated boundary

P8 capabilities, P18 temporal contracts, P20 multi-participant activity
contracts, and P10 local generation attach only to the stages/profile that
consume them.
```

This is still an unapproved candidate pipeline and assigns no P9 checkpoint
IDs. P10 consumes relevant P9 genesis/provenance and P8 local-anchor contracts.
P12 can inventory continuation state in parallel, but complete save coverage
must represent the initial World Truth and its relevant inputs. P13 requires
P12 continuation plus recoverable initial and changed state; neither P12 nor
P13 changes the P9 rule that the interior of generated backstory is outside
the fork guarantee.

## 5. Existing ownership boundaries and likely collision areas

| Concern | Existing boundary observed | Phase 9 implication / collision risk |
|---|---|---|
| Authoring and content definitions | `SimulationConfigData` and domain `*Data` assets are authoring sources. | Resolve authoring into runtime facts; do not mutate assets to express world changes or turn effective configuration into a full content snapshot. |
| Effective policy and calendar | Configuration resolver owns policy/parameter composition; `SimulationRuntime` owns its effective configuration and calendar for the run. | Genesis inputs need compatible effective values before construction. Config, calendar and content versions have related but distinct ownership. |
| Semantic identity | `PersonId` and spatial IDs are distinct from representation IDs. Person and spatial stores own their records; the legacy registry indexes RuntimeIds. | Identity touches Person/population, spatial, and runtime composition together. `RuntimeIdentity.cs`, `PersonId.cs`, spatial identity and initialization code are high-collision areas. |
| Domain World Truth | Person, population, institutions, properties, political, conflict and other stores/services own their domain facts and transitions. | A generator should prepare/route facts through these authorities rather than keep parallel copies. A Phase 9 scope audit must name included authorities before implementation. |
| Runtime composition and first-boundary execution | `SimulationRuntime` composes authorities and enforces a shared mutation guard; `TesteSimulacao` currently owns the host bootstrap sequence. | Likely hotspots are `SimulationRuntime.cs`, `TesteSimulacao.cs`, configuration composition and mutation-guard binding. Any parallel work needs isolated worktrees and explicit integration ownership. |
| Spatial authority | Current P8-A owns factual Hex/Location/terrain semantics; P8-B owns passage conditions; P8-C owns City/Site anchors and Person `At`/`InTransit` positions. Legacy `SpatialNetworkRuntime` remains a separate model in the inspected initialization path. | P8-A/B/C capabilities are canonical. P9 composes through their owners, not duplicate stores. Scope determines whether B/C facts are included; route-plan or route-Knowledge scope depends on P8-D promotion. |
| Diagnostics | World-state snapshots/canonical writers project current facts for inspection. | Diagnostics can help compare generated worlds but are not primary truth or a substitute for the complete state. Diagnostics-core edits would be a shared hotspot. |

## 6. Reconstruction-sensitive initial inputs and state

The initial boundary must be reproducible from compatible semantics and
recoverable causal inputs, or the complete authoritative initial state must be
preserved by the later persistence/reconstruction mechanism. Architecture
§§12, 91–92 and the Phase 9 Brief make a seed alone insufficient. The entry
design should classify at least:

- all authoritative domain facts at the first simulated boundary, including
  semantic identities, references and cross-store relations;
- P8-A Hex/Location identities, axial geography, scale provenance and terrain
  identity/revision; plus P8-B passage/barrier/crossing-condition facts and
  P8-C City/Site anchor bindings and Person `At`/`InTransit` positions when
  included in the chosen profile. `NpcRuntime` location fields alone do not
  represent the Person-level positions;
- population aggregates and the selected Person/representation/lifecycle
  facts, while preserving `PersonId` across materialization and dormancy;
- effective configuration and calendar, plus the compatible simulation/content
  versions and definitions needed to interpret the state;
- authored and generated inputs; stable stage/contributor IDs and compatible
  versions; declared dependencies, inputs/outputs, deterministic contribution
  and conflict order; provenance for authoritative contributor outputs;
- each stage's causal random context and purpose-scoped derivation inputs, so
  unrelated random consumption cannot shift its results;
- for any later explicit retrofit, its selected contributor/version, input
  boundary, compatibility decision and resulting domain mutation boundary;
- identity inputs and any allocator/sequence state that later affects
  authoritative identity, references or outcomes;
- the declared start boundary and any initial knowledge or commitments that
  affect decisions after that boundary.

The technical choice between retaining the resulting initial state and
reconstructing it from compatible inputs belongs to the later persistence and
reconstruction designs. Neither diagnostics, `History`, nor a generated
backstory log alone is sufficient proof of the initial truth.

## 7. Current Phase 8 dependency status

- Spatial-specific design and implementation can use the current canonical
  P8-A contract and authority; those dependencies are satisfied at the cited
  P8 state.
- If accepted Phase 9 scope includes initial passage facts, use canonical P8-B
  passage/barrier/crossing-condition authority. If it establishes Person
  positions or City/Site bindings, use canonical P8-C stores/composition and
  preserve Person-level `At`/`InTransit` separately from materialized NPC
  location fields.
- If accepted scope includes route plans or route Knowledge, only that component
  waits for P8-D implementation and promotion. P8-E civil travel is not a
  genesis prerequisite. P8-D's other consumers and P8-E retain their own
  readiness and promotion gates in the current Phase 8 State.
- Non-spatial entry architecture and reconstruction-sensitive inventory can
  proceed independently, but must keep the initial profile's chosen P8 scope
  explicit.

## 8. Unresolved architecture and product decisions

The current architecture and alignment already settle the pipeline invariants
below. The remaining open items are bounded profile/product choices or
algorithm details for later technical design; they do not reopen those
invariants.

| Question | Kind | Why it remains open |
|---|---|---|
| Which domain facts and optional capabilities belong to the first supported profile? | Scope / product boundary | The Brief requires a semantically complete configured initial world but defines no catalog or universal inventory. A profile must name included domains/capabilities and may not imply later-phase scope. |
| What exact generated-ID derivation and compatibility/version representation will implementation use? | Technical design | Semantic identity must be stable and distinct from definition/runtime identity; the algorithm and concrete manifest/compatibility representation remain deferred. |
| What world scale, population scale, procedural-generation extent and pre-simulation backstory should the first profile promise? | Product | No product gate is established. If the selected guarantee requires a product choice, surface it before the affected technical design is approved. |
| Which optional initial Knowledge, commitments or temporal activity/participation state does the selected profile include? | Profile / domain scope | Knowledge remains perspective-owned and distinct from World Truth. Temporal activity state is included only when selected and uses relevant P18 contracts; multi-participant facts additionally use relevant P20 contracts only when selected. Neither phase is a blanket dependency. |

No concrete algorithm, content catalog, world scale, or product promise is
selected in this proposal. The dependency-aware stage/contributor contract,
stable identity/provenance requirement, deterministic contribution and
purpose-scoped randomness rules, pre-publication validation, and explicit
new-world/retrofit boundary are current constraints, not open questions. Any
choice that changes domain meaning still requires the applicable architecture
or product gate before affected technical design proceeds.

## 9. Proposal boundary

This document records a reviewable Phase 9 entry proposal at the stated
baseline. It does not alter `SIMULATION_ARCHITECTURE.md`, the Roadmap, any
Phase Brief or State, and it does not make Phase 9 implementation-ready.

**Independent document review:** PASS against proposal commit
`8a610a3c97762ee87026289361c286361136f906`. This records review of the entry
proposal only; no P9 checkpoint IDs are approved and no implementation is
authorized.
