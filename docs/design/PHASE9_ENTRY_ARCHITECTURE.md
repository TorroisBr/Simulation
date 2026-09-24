# Phase 9 Entry Architecture Proposal — UNAPPROVED

**Status:** exploratory entry proposal only. It is not an approved architecture,
checkpoint contract, implementation plan, or permission to begin implementation.
No Phase 9 checkpoint IDs are assigned or approved here.

**Review baseline:** `codex/phase9/GenesisEntryArchitecture` at
`1f4651e99db2c357dd3be3c6b9284d104379f706`.

**Authority:** `docs/SIMULATION_ARCHITECTURE.md` remains the semantic authority.
The Phase 9 and Phase 8 Briefs and `docs/ROADMAP.md` define subordinate scope
and dependencies. This proposal records observed code and possible boundaries;
it does not settle the open decisions below.

## 1. Current facts at the reviewed baseline

- `docs/phases/PHASE9_BRIEF.md` says Phase 9 is `ENTRY_ARCHITECTURE_READY`,
  with no schedulable implementation checkpoint and no approved P9 IDs. It
  requires deterministic, complete initial World Truth before the first
  actually simulated boundary. It explicitly defers generation passes, the ID
  algorithm, mod schema, and exact content catalog.
- The Phase 8 Brief says Phase 8 is `READY_FOR_TECHNICAL_DESIGN` and has no
  implementation. The current checkout contains the consolidated spatial
  semantic direction, but no P8-A technical design artifact or promoted P8
  spatial capability was found at this baseline. This proposal therefore
  distinguishes the accepted semantic direction from the missing technical
  contract and runtime capability.
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
- `SpatialAuthorityStore` currently owns `HexRecord` identities,
  `LocationRecord` identities with one `AnchorHexId`, and local-topology
  bindings. `HexRecord` has no semantic coordinate or terrain; the store does
  not provide adjacency, passages, traversal or generation. The existing
  `SpatialNetworkRuntime` is a separate legacy location/route model.
- `SimulationConfigurationResolver` implements
  `Defaults → Preset → World overrides → Content overrides → EffectiveSimulationConfiguration`.
  `SimulationRuntime` separately receives a calendar definition and owns an
  effective calendar copy. Architecture §§12–13 make both the effective
  configuration and calendar authoritative execution inputs; neither is a
  substitute for the concrete world content or full initial World Truth.
- `WorldObserverDemoBootstrap` is another hand-authored demonstration bootstrap
  with fixed example IDs. No general genesis/generation service or
  `PHASE9_STATE.md` exists in the tracked baseline.

These facts describe current implementation, not a recommendation to preserve
the legacy startup arrangement.

## 2. Objective and exclusions

The Phase 9 objective, as written in its Brief and Architecture §12, is to
establish the configured initial world as complete semantic `World Truth`
before the first actually simulated boundary. Generated initial facts must use
the same domain ontology and authorities as authored facts. The generation
source may be composed from stages, but it must not be a rendering instruction
or a second runtime mutation authority.

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

## 3. Candidate boundary sketch — UNAPPROVED

The following is a discussion aid only; it names no classes, interfaces,
ownership transfer API, or required implementation sequence:

```text
authored inputs + accepted generation inputs
                    │
                    ▼
resolve compatible content, effective configuration/calendar,
and the generation context needed for determinism
                    │
                    ▼
construct initial domain facts through their owning authorities
  (spatial facts require the relevant P8 authority)
                    │
                    ▼
validate completeness and cross-domain references
                    │
                    ▼
compose one runtime world from the established initial truth
                    │
                    ▼
first actually simulated boundary
```

Whether these are separate components, stages in a host composition root, or
some other bounded arrangement is not decided. A later technical design may
choose the concrete arrangement only after semantic and product scope is
accepted.

## 4. Candidate scope and dependency map — UNAPPROVED

| Candidate work group (no checkpoint ID) | Possible scope for review | Dependency / readiness note |
|---|---|---|
| Initial-world scope and inputs | Define which initial facts are in the supported Phase 9 profile; separate authored input, generated input, resolved configuration/calendar, and compatible content. | Entry discussion can proceed now. A materially different promise for generated scale or population is a product decision. |
| Identity and provenance boundary | Specify which identities are semantic and durable, what input/provenance must be recoverable, and how compatible content/configuration is identified. | Requires an architecture decision on generated identity. The ID algorithm is explicitly deferred and must not be invented in this proposal. |
| Spatial initial facts | Populate factual geography and anchored locations in the accepted spatial ontology. | Spatial design that depends on technical representation must wait for the P8-A technical contract. Runtime integration needs the relevant promoted spatial authority. It does not require all of P8-E. |
| Domain-owned initial facts | Establish the selected population, Persons, settlements, places, relationships, starting state and other chosen facts through their existing domain owners. | Depends on accepted scope, identity and each included domain's contracts. The inventory must not imply every existing domain store is automatically part of the Phase 9 profile. |
| Complete-state composition and checks | Show that the selected inputs resolve into one valid initial world with deterministic ordering and valid cross-domain references before simulation starts. | Depends on the selected fact producers and on the chosen world-composition boundary. Avoid making `SimulationRuntime` a generation dumping ground. |
| Pre-start boundary and reconstruction evidence | Mark the transition from initial setup/backstory to the first actually simulated boundary and declare the initial causal inputs/state that later reconstruction must recover. | Depends on the accepted boundary semantics and composition model. Save/replay mechanics remain Phase 12/13 concerns. |

Candidate dependency direction, subject to entry review:

```text
accepted scope and execution inputs ───────────────┐
                                                   ├─→ domain-owned initial facts
P8-A semantic/technical spatial contract ─────────┘             │
P8-A promoted spatial authority ─→ spatial integration           │
identity/provenance decisions ───────────────────────────────────┤
                                                                ▼
                                             complete-state checks/composition
                                                                │
                                                                ▼
                                             first simulated boundary
```

This is not an approved Phase 9 DAG. It deliberately assigns no P9 checkpoint
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
| Spatial authority | `SpatialAuthorityStore` and the P8-A track own factual Hex/Location semantics; legacy `SpatialNetworkRuntime` remains transitional. | Do not let Phase 9 and P8-A writers concurrently own spatial identity/storage. Genesis implementation that uses the new authority waits for its promotion. |
| Diagnostics | World-state snapshots/canonical writers project current facts for inspection. | Diagnostics can help compare generated worlds but are not primary truth or a substitute for the complete state. Diagnostics-core edits would be a shared hotspot. |

## 6. Reconstruction-sensitive initial inputs and state

The initial boundary must be reproducible from compatible semantics and
recoverable causal inputs, or the complete authoritative initial state must be
preserved by the later persistence/reconstruction mechanism. Architecture
§§12, 91–92 and the Phase 9 Brief make a seed alone insufficient. The entry
design should classify at least:

- all authoritative domain facts at the first simulated boundary, including
  semantic identities, references and cross-store relations;
- the initial spatial identities/topology/anchors and any other spatial truth
  included in the chosen profile;
- population aggregates and the selected Person/representation/lifecycle
  facts, while preserving `PersonId` across materialization and dormancy;
- effective configuration and calendar, plus the compatible simulation/content
  versions and definitions needed to interpret the state;
- authored and generated inputs, the seed/random context and relevant causal
  ordering or context used during generation;
- identity inputs and any allocator/sequence state that later affects
  authoritative identity, references or outcomes;
- the declared start boundary and any initial knowledge or commitments that
  affect decisions after that boundary.

The technical choice between retaining the resulting initial state and
reconstructing it from compatible inputs belongs to the later persistence and
reconstruction designs. Neither diagnostics, `History`, nor a generated
backstory log alone is sufficient proof of the initial truth.

## 7. What must wait for Phase 8

- Spatial-specific technical design that assumes coordinate representation,
  coordinate/identity mapping, or details of Hex/Location composition must use
  the reviewed P8-A technical contract once available; §69 intentionally leaves
  those implementation details open.
- Code that constructs or registers Phase 9 geography in the new spatial model
  must wait for promotion of the relevant P8-A spatial capability. A candidate
  P8-A branch or an unreviewed technical sketch is not a promoted capability.
- If accepted Phase 9 scope includes passages, local topology, or other P8
  behavior, that part must additionally wait for the relevant P8 contract and
  promoted capability. This is consumer-specific; P8-E civil travel is not a
  blanket prerequisite for initial geography.
- Non-spatial entry architecture, scope questions, and reconstruction-sensitive
  state inventory may continue alongside P8, provided they do not silently
  assume missing P8 types or semantics.

## 8. Unresolved architecture and product decisions

These are questions for entry review, not decisions made by this artifact.

| Question | Kind | Why it remains open |
|---|---|---|
| What does “complete initial World Truth” cover for the first supported Phase 9 profile: which domain facts/stores and which optional capabilities? | Scope / product boundary | The Brief states completeness but does not define a content or domain inventory. A broad reading could silently pull later phases into P9. |
| What semantic inputs make a generated identity stable across compatible regeneration, edits, and authored/generated combinations? | Architecture | The architecture requires stable semantic identity and deterministic generation, while the Brief explicitly defers the ID algorithm. The answer must preserve PersonId/HexId/LocationId semantics and avoid RuntimeId identity. |
| How should the generation context and random authority be partitioned so unrelated random consumption cannot change initial facts? | Architecture | Causal RNG properties are decided, but streams/derivation and their relationship to the existing runtime source remain implementation decisions. |
| Which source/version compatibility information is needed for authoring definitions, generated content, effective configuration, and later reconstruction? | Architecture / product | The architecture requires compatible content and recoverable provenance; no Phase 9 manifest or compatibility promise is defined. |
| Which exact event marks the first actually simulated boundary, especially when the calendar starts at a nonzero date or the world is pre-aged? | Architecture | Calendar day and simulation-history boundary are explicitly distinct; setup, backstory and normal runtime mutation must not blur together. |
| How are initial individual NPCs represented in the supported profile when the legacy bootstrap creates NpcRuntime without PersonId, given the persistent Person ontology? | Architecture / product | The current legacy path and the Person-backed model differ. The profile and compatibility/migration treatment are not specified here. |
| Which initial Knowledge, commitments or other non-World-Truth state is authored/generated, and whose perspective owns it? | Architecture / product | Knowledge remains distinct from World Truth; initialization must not grant universal knowledge as an accidental side effect. |
| What world scale, population scale, degree of procedural generation, and amount of pre-simulation backstory should the initial profile promise? | Product | The Brief establishes no product gate yet; a concrete generation guarantee may require user intent. |

No algorithm, content catalog, generation pass, world scale, or product promise
is selected in this proposal. If entry review determines that any open question
changes domain meaning, it must be resolved through the applicable architecture
or product gate before the affected technical design proceeds.

## 9. Proposal boundary

This document records a reviewable Phase 9 entry proposal at the stated
baseline. It does not alter `SIMULATION_ARCHITECTURE.md`, the Roadmap, any
Phase Brief or State, and it does not make Phase 9 implementation-ready.
