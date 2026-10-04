# P10-B — Deterministic Local Topology Generation v1

**Checkpoint:** P10-B, bounded technical design only  
**Classification:** `READY_FOR_TECHNICAL_DESIGN`; this document is not implementation authorization or a promoted capability.  
**Design baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7` (promoted architecture/planning baseline; branch `codex/architecture/p10b-technical-design`).  
**Scope:** one generated Ruin site at the selected P9-B canonical Location in a pre-start genesis profile.  
**Review:** independent technical review is required; author does not self-approve.

## 1. Decision and intended proof

P10-B adds one site-scoped, deterministic generator that builds a small finite graph of semantic LocalPlaces, explicit connections, containment where useful, and entry point(s). The generator selects structural features by deterministic rules and choices. It must not select one complete graph from a fixed catalog of whole-layout templates. A few deterministic fixture inputs must demonstrate at least two valid graphs; no pairwise uniqueness guarantee is imposed.

The generated result is part of initial World Truth and is committed through the existing LocalTopology authority before the first simulated boundary. A run with identical compatible world/genesis inputs, stable site-instance identity, effective generator rules/revision and seed context produces the same semantic topology and IDs. P10-A's fixed authored profile remains supported as a compatibility path; its `DefinitionId`-keyed single site and `Entrance → Courtyard → Inner Chamber` graph are not reinterpreted as P10-B output.

## 2. Evidence and assumptions

- `docs/SIMULATION_ARCHITECTURE.md` §69 establishes `Location → LocalTopology/SubLocation`, the distinction between anchor and domain instance, Ruin archetype versus site instance, reproducibility, structural rules rather than whole-layout selection, and separation from content. §92A requires reconstruction-ready identity/state and fail-closed P12 profile admission for new authoritative state.
- `docs/architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md` (promoted 2026-10-03 direction), `docs/phases/PHASE10_BRIEF.md`, and `docs/ROADMAP.md` identify P10-B as design-ready, not implementation-ready; P9 ordered genesis, P8 Location/site anchor, and P10-A topology ownership are dependencies. The selected proof needs no second Location.
- `ExplorableSiteRuntime` already carries `RuntimeId`, `DefinitionId` and `Location` separately. `LocalTopologyOwnerReference.ForExplorableSite` uses site RuntimeId plus Location RuntimeId. `LocalTopologyStore` validates the owner and publishes one topology per owner. `LocalTopologyBlueprint`/`LocalTopologyBuilder` provide a private draft-to-store build seam; `LocalTopologyPublicationAtomicityTests` cover rejection without partial topology/member registration.
- P9 genesis composition exposes the canonical spatial authority and profile manifest. The generator must consume the selected profile's actual Location/anchor and must not synthesize or substitute geography.
- No `docs/PHASE10_STATE.md` exists in the baseline checkout. P10-A promotion and exact capability facts are therefore taken from the promoted direction/Brief and current code/tests; this design does not claim additional P10-A delivery.

## 3. Ownership and implementation boundary

Keep the generator pure until publication:

1. **Genesis composition owner** (the existing P10 profile composer, after P9 spatial composition) resolves the selected canonical Location and site definition, creates a stable site-instance identity, builds an immutable generation request, and stages the resulting site and topology in the private genesis draft.
2. **P10-B generator** (new small domain service, e.g. `RuinLocalTopologyGenerator`) accepts a validated request and returns an immutable `LocalTopologyBlueprint` or equivalent generated draft. It has no RuntimeIdentityRegistry/store access and cannot mutate the world, consume shared RNG, allocate sequence IDs, or read Unity objects.
3. **Existing `LocalTopologyBuilder`/`LocalTopologyStore`** remain the publication authority. Build and validate all members while private; atomically register the owner/site and all places/connections and publish only after whole-result validation succeeds. Failure returns a diagnostic and leaves the published stores, identity registry and genesis draft unchanged.
4. **Genesis profile admission/reconstruction owners** record the exact effective generation inputs and initial generated state under the P12 §92A contract. This is an integration requirement, not a request to add P12 persistence support in P10-B.

Do not expand LocalTopology into a dungeon framework. Keep City topology behavior and P10-A profile behavior unchanged. Do not introduce a new universal spatial/domain owner.

## 4. Identity migration and stable IDs

### 4.1 Site identity

The Ruin `ExplorableSiteData.DefinitionId` remains immutable archetype/content identity. `SpatialLocationRuntime.RuntimeId`/canonical `LocationId` remains anchor identity. Neither is the site instance key. P10-B must set an explicit stable `SiteInstanceId` (persisted semantic ID; in current runtime adapters it may be represented by `ExplorableSiteRuntime.RuntimeId`) in the genesis request and on the site instance. It must be distinct from definition and Location IDs and must resolve through existing identity validation.

For the bounded profile, derive/resolve this ID from an explicit stable profile instance key in the effective genesis input, not a mutable allocator counter, display name, Unity InstanceID, load order or incidental list position. Preserve the P10-A mapping as an adapter/migration rule for its existing one-instance profile. Never reinterpret the P10-A definition key as globally unique across sites. The explicit key permits multiple instances of one definition when a future profile supplies distinct canonical Locations; P10-B itself still creates one site.

### 4.2 Generated member IDs

Derive topology, place, and connection semantic IDs from a canonical, unambiguous tuple containing `SiteInstanceId`, generator identity/version, rules revision, member kind, and stable member key. Use a namespace-safe deterministic encoding (length-prefixed UTF-8 fields and a fixed digest/encoding, or an equivalently collision-resistant project ID scheme); validate uniqueness inside the draft and against `RuntimeIdentityRegistry` before commit. Member keys come from semantic generation roles/indices assigned by sorted deterministic traversal, never object hashes or unordered enumeration.

IDs must not depend on random draw count before the member, allocation order, collection iteration order, or display text. Existing local topology has no separate published topology RuntimeId in its owner contract, so do not fabricate a new topology identity unless implementation finds a concrete persisted consumer; the required IDs are site, places and connections.

## 5. Deterministic generation context

Add a typed immutable request carrying at least:

- compatible world/genesis identity and effective initial seed context (from selected genesis inputs, not ambient process RNG);
- stable `SiteInstanceId`, Ruin `DefinitionId`, canonical Location identity and validated anchor reference;
- generator identity/version and effective `TopologyRulesRevision`;
- explicit bounded parameters used by v1 generation.

Use a generator-owned semantic random context. Its root is a deterministic derivation from the canonical encoding of `(world/genesis identity, effective seed context, SiteInstanceId, generator identity/version, rules revision)`. Random decisions are addressed by stable semantic draw keys (and a local retry/index only when needed), or by an isolated per-site stream whose creation is derived from that root and whose draw ordering is entirely specified. Prefer keyed draws for independent structural choices so unrelated profile/RNG consumption cannot perturb the Ruin. Do not consume global simulation RNG or rely on host RNG APIs. Specify byte order, string encoding, hash/PRNG algorithm and range mapping in implementation; version any change that changes results. Use rejection sampling for bounded integer ranges to avoid modulo bias. Fixed-point/integer weights and canonical tie-breaking are preferred over platform-sensitive floating-point branching.

Record the effective seed context, generator ID/version, rules revision and any effective parameters in genesis provenance sufficient to recompute the draft. The generated topology itself remains authoritative initial state; provenance is not a substitute for that state. Same compatible inputs reproduce; different compatible seed or site identity are allowed to produce a different topology, with no guarantee they must.

## 6. V1 finite graph contract

Make limits explicit constants/effective parameters and validate before publication. Proposed initial envelope: 4–12 places inclusive, 1–24 undirected connections, exactly one designated entry in the proof profile, maximum containment depth 4, and every place reachable from entry through connections. These bounds are a v1 workload limit, not universal LocalTopology constraints. The implementation must retain the architecture's ability to represent multiple entries where a later approved consumer needs them.

For each generated graph:

- place IDs and connection IDs are unique; every endpoint belongs to the same draft/site;
- connections are non-self, have finite positive traversal cost, and are normalized/canonical where direction is not semantically distinct;
- at least one entry point exists and belongs to the graph;
- all places are reachable from an entry by explicit connections (containment alone does not imply traversal);
- containment is acyclic, same-topology only, and obeys maximum depth; avoid containment unless the selected structural rules need it;
- graph is connected, has no duplicate unordered edge unless parallel connections are explicitly permitted by current LocalTopology semantics (v1 should emit none);
- stable ordering is ordinal by semantic member key before IDs and blueprint members are produced;
- generator rejects impossible/contradictory effective parameters with a diagnostic; it does not silently clamp an incompatible ruleset.

The algorithm should grow a graph through bounded local decisions (for example, begin with an entry-adjacent spine and make keyed branching/loop/containment choices, then validate/connectivity-repair only by a deterministic documented rule). That is an implementation direction, not a prescribed universal dungeon grammar. It must not encode a handful of whole-graph fixtures as selectable templates.

## 7. Atomic pre-start composition and P12 boundary

Generation occurs after the selected P9-B profile has composed and validated its Hex/Location through `SpatialAuthorityStore.TryComposeGeography`, and after the actual canonical Location is resolved. Generate and validate in private memory. Compose the Ruin owner and complete local topology in the same pre-start genesis transaction/draft; run owner-anchor, graph, ID, duplicate, selected-profile and cross-store validation before any externally observable publication. Commit all selected P10 facts as one unit. A rejected generation or identity conflict publishes no site, topology, LocalPlace or LocalConnection and leaves no consumed authoritative random state.

This is genesis composition only. After the first simulated boundary, edits require ordinary reviewed runtime mutation authority; P10-B adds no install, regeneration, retrofit, construction or runtime expansion API.

The site/topology owner must expose exact initial-state export/hydration/validation seams sufficient for eventual continuation and fork. Until the selected P12 profile explicitly supports and tests this owner, keep it excluded by construction from `UnityBootstrap-Daily-v1` or ensure that profile's admission rejects it with a tested diagnostic. Do not claim Save, load, replay or fork delivery from a P10-B design or implementation alone.

## 8. Expected implementation files (not created here)

Exact names may follow repository conventions during implementation; likely hotspots:

- `Assets/_Project/Scripts/ExplorableSiteRuntime.cs` (explicit instance identity semantics only if its current RuntimeId cannot carry the stable key) and the runtime identity/profile adapter;
- new pure P10 generator/request/rules/provenance types near genesis composition;
- `Assets/_Project/Scripts/SimulationGenesisPipeline.cs` and its genesis composition owner (stage integration only; preserve P9 ordering and P10-A path);
- `Assets/_Project/Scripts/LocalTopologyFoundation.cs` / blueprint builder only if current validated private draft and atomic store commit need a narrowly scoped extension;
- profile/input/provenance composition and selected-profile admission seam, with no expansion of `UnityBootstrap-Daily-v1`;
- focused EditMode tests adjacent to `LocalTopologyPublicationAtomicityTests`, `ExplorableSiteFoundationTests`, and genesis composition tests.

Do not edit `docs/SIMULATION_ARCHITECTURE.md`, P9 promoted provenance rules, P8 anchor semantics, Unity renderer, content/loot/encounter stores, Save implementation, or runtime mutation authorities as part of this design-only checkpoint.

## 9. Test and review plan for implementation

1. **Identity/migration:** same archetype at different stable instance keys yields distinct site IDs; definition and Location IDs remain distinct; P10-A legacy profile still composes and resolves with its existing single-site behavior; global RuntimeIdentityRegistry rejects collisions.
2. **Determinism:** same compatible request across fresh runtimes gives byte/semantic-equal sorted places, containment, entries, connections, costs and IDs; reordered input collection has no effect; unrelated RNG consumption has no effect; different seed and different site key fixtures can yield different graphs; no assertion that all pairs differ.
3. **Algorithm range:** a bounded deterministic matrix of compatible seed/site inputs generates more than one valid graph; every result stays within configured finite limits and is not a selection from complete layout templates.
4. **Graph validation:** reject empty/missing entry, unreachable place, cross-site endpoint, duplicate IDs/edges, self-edge, invalid cost, containment cycle/depth violation, impossible parameter set, malformed revision and foreign Location/anchor.
5. **Atomicity:** induce invalid graph, site/member ID collision, stale/wrong P9 Location and late profile validation failure; assert no site/topology/member registrations, no partial genesis facts, and no shared RNG advancement. Successful commit publishes complete owner and graph together.
6. **Reconstruction gate:** assert effective seed/context, generator and rules revisions are included in profile provenance; assert the exact initial generated owner state is available to reconstruction seam or selected `UnityBootstrap-Daily-v1` rejects the owner. This is a required P10 implementation gate, not a P12 feature claim.
7. Run existing P10-A/P8/P9 composition regressions and `git diff --check`; independent review checks scope, determinism/replay sensitivity, identity migration and publication atomicity. No Unity tests were run for this documentation-only design.

## 10. Dependencies, exclusions, closure

**Hard dependencies:** promoted P9-B ordered genesis/profile provenance; P8 canonical Location/anchor and ExplorableSite contract; P10-A LocalTopology owner/build/store authority; compatible effective seed/rules identity available at genesis. Spatial tests or fixtures cannot replace selected-profile canonical Location resolution.

**Explicit exclusions:** second Ruin/second Location in the shipped proof; choosing pre-authored whole layouts; loot, traps, encounters, inhabitants, resources, exploration knowledge, RPG-system tables, dungeon-room content schemas; universal graph/dungeon engine; broad ruleset/mod format or loader; runtime construction/expansion; existing-world retrofit/regeneration; new travel/activity/actor mechanics; Save/P12 implementation; architecture or Phase State edits.

**Implementation-ready only after independent design review** confirms identity mapping for the P10-A compatibility path, the generator's versioned random derivation can be represented by selected genesis inputs/provenance, and the bounded graph's semantic validation fits current LocalTopology contracts. Then dispatch implementation in an isolated branch; serialize integration on genesis composition and LocalTopology publication hotspots. Promotion and phase closure remain separate gates.

## 11. Files inspected

- `AGENTS.md`; `.agents/skills/checkpoint-technical-design/SKILL.md`
- `docs/SIMULATION_ARCHITECTURE.md` (§§69, 92A and determinism/RNG invariants); `docs/ROADMAP.md`; `docs/EXECUTION_MODEL.md`
- `docs/phases/PHASE10_BRIEF.md`; `docs/architecture/P10_P14_P20_NEXT_SCOPE_DECISIONS.md`
- `Assets/_Project/Scripts/ExplorableSiteRuntime.cs`; `Assets/_Project/Scripts/Data/ExplorableSiteData.cs`; `Assets/_Project/Scripts/LocalTopologyFoundation.cs`; `Assets/_Project/Scripts/Data/LocalTopologyData.cs`; `Assets/_Project/Scripts/SimulationGenesisPipeline.cs`
- `Assets/_Project/Tests/EditMode/Editor/ExplorableSiteFoundationTests.cs`; `LocalTopologyFoundationTests.cs`; `LocalTopologyPublicationAtomicityTests.cs`; genesis/profile tests found by repository search
- `docs/PHASE10_STATE.md` was checked for and is absent from this checkout.
