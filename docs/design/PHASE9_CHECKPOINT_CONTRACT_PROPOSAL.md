# Phase 9 First-Profile Checkpoint Contract Proposal — UNAPPROVED

**Status:** proposed bounded contract for independent review and formal
acceptance. This document assigns no approved P9 checkpoint ID, changes no
Phase Brief/State/Roadmap, and authorizes no implementation.

**Product scope:** the user-approved first delivery is the existing authored
Unity bootstrap world as a proving profile for a generic deterministic genesis
pipeline. World and population extent come from authored configuration. The
profile promises no procedurally generated terrain, settlements, population,
or pre-simulation backstory. Later procedural consumers remain possible through
reviewed seams, but are outside this profile.

**Baseline:** canonical `codex/phase8/canonical` at
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, containing architecture/roadmap
refresh `c285466c355103d3637ac165246591b72eb7bda0`; refreshed entry proposal
`05ba224da8cead8221d12fb1b9dc64da5a3b61d2`; and refreshed technical-design
proposal `7bb3312c7b045067dd2dee55451d71c8fd796880`. P8-A through P8-D are
canonical; P8-E remains a design-approved candidate with implementation and
promotion pending in the current State.

**Authority:** `docs/SIMULATION_ARCHITECTURE.md` remains controlling. The P9
Brief, refreshed entry/technical proposals, Phase 8 State, and the
`INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` records are subordinate and inform
conditional dependencies below.

## 1. Contract objective

The first accepted P9 checkpoint should replace the hand-ordered authored
startup sequence with a deterministic, dependency-aware pre-start pipeline for
the bounded authored-bootstrap profile. It must publish one complete initial
World Truth through the same domain owners used after startup, before the first
actually simulated temporal boundary. This profile proves the pipeline and its
determinism; it does not create content or increase authored world/population
extent.

The pipeline is a composition mechanism, not a second truth store, renderer
instruction, public mod API, or runtime mutation authority. A stage may resolve
and propose facts, but each authoritative output is validated and retained by
its owning domain authority. Once the complete result is published, initial
generation privilege ends and normal runtime mutation rules apply.

## 2. Supported authored-bootstrap profile

### Inputs in the observed host path

`TesteSimulacao.InitializeSimulation` is the observed proving entry point. It
reads the configured `SimulationConfigData`, creates time/random/identity
infrastructure, materializes runtime objects, initializes selected starting
state, assembles systems, and then constructs `SimulationRuntime`. The proposed
profile is limited to this ordinary Unity-hosted path and its configured
content; it does not promise arbitrary hand-assembled runtimes, injected
providers, external contributors, P19 loader/API support, or runtime extension
installation.

The observed authored inputs include:

| Authored input | Existing use in `TesteSimulacao` | Contract boundary |
|---|---|---|
| Calendar, enabled modules, and configuration overrides | Resolves `CalendarDefinition`, `SimulationModuleSet`, and `EffectiveSimulationConfiguration`. | Preserve configuration policy/parameters and calendar as separate resolved authoritative inputs. Record the effective values/revisions and compatible source versions needed to interpret the initial state. |
| `Cities` and `CityData.connections` | Creates City runtimes, legacy spatial locations, and travel routes with authored travel-day values. | Preserve configured extent and authored references. This legacy spatial model is not the P8 factual geography authority. Any P8 spatial outputs are conditional and must use P8 owners. |
| `ExplorableSites` | Creates site runtimes and legacy locations/routes; authored initial knowledge may refer to them. | Preserve selected authored site instances and references. No generated sites or local topology are promised. |
| `Npcs` (`NpcSimulationConfig`) and referenced `NpcData` | Creates one named `NpcRuntime` for each successfully accepted authored config entry, with starting city and initial money; applies configured inventory. | World/population extent is the accepted authored configuration. No seed-driven additions, aggregates, or procedural population are in scope. The current source does not create `PersonRuntime`/`PersonId` records for these NPCs; see the identity gate below. |
| `Actions`, statuses/jobs and content definitions | Supplies configured action/content definitions and state references to host/domain systems. | Resolve compatible references before materialization; do not mutate authoring assets or equate a content definition ID with a semantic Person/world identity without an accepted rule. |
| Initial known sites and bootstrap knowledge | Bootstraps selected site, spatial, and commercial Knowledge for named runtime NPCs. | Knowledge remains perspective-owned and distinct from World Truth. Include only currently selected authored initial Knowledge; do not expose hidden facts to every actor. |
| Initial warrants and scheduled directives | Initializes Justice state and the configured future directive store. | Keep initial authoritative warrant facts in their domain owner; retain directives as scheduled inputs/commitments with their own boundary semantics. Neither is simulated history merely because it exists at startup. |
| Fixed-seed setting | Current host passes `simulationSeed` when `useFixedSimulationSeed` is true and `0` otherwise to its existing random source. | This observed behavior is not yet a P9 root-seed/provenance contract. Any stage that consumes randomness must declare its causal random purpose and version; the first profile adds no random world-content generation. |

`simulationName`, logging settings, report text, renderer state, and host
object-discovery order are not authoritative genesis outputs. Output extent is
defined by validated authored configuration/content, not by a requested
procedural scale.

### First-profile output inventory

The profile inventory below follows observed bootstrap outputs. It is not a
claim that every current host object is World Truth or that every domain store
is automatically included.

| Output category | Profile content | Owner and required distinction |
|---|---|---|
| Resolved run context | Effective configuration, effective calendar, compatible simulation/content context, selected built-in stage set, and declared seed/random context. | Configuration resolver and calendar own their respective resolved values. These do not replace domain facts. |
| Authored macro-world representations/facts | Configured cities, configured connections/routes and configured explorable sites that pass validation. | Domain authorities own facts. Runtime `CityRuntime`, `ExplorableSiteRuntime`, `SpatialLocationRuntime`, and legacy route IDs are representations; sequential `RuntimeIdAllocator` output is not a durable semantic-ID scheme. |
| Named authored actors | Configured NPC definition, starting-city reference and configured initial money/inventory. Count/extent follows accepted authored entries; there are no added people or population aggregates in the first profile. | Existing NPC/runtime and inventory authorities own their state. Person-level identity/membership must remain distinct and requires the explicit resolution in §7 before the contract can be formally accepted. |
| Initial perspective state | Only the initial Knowledge and warrants/directives selected through the current host path. | Knowledge remains observer-owned; warrants belong to their legal domain; scheduled directives are not past simulation history. |
| Execution composition | Domain systems and services needed to run the supported daily profile. | Composition is not itself a truth-generating stage; systems do not gain extra authority from being constructed. |

The first profile contributes no procedural terrain, settlement placement,
population, local topology, or backstory. Future profiles may add dependent
stages after their product/content and domain contracts are accepted.

## 3. Proposed stage roles and dependency contract

The following are proposed semantic roles, not checkpoint IDs or final stage
identifier strings. Their boundaries are grounded in the current authored
bootstrap. Formal acceptance must assign each implemented stage a stable
semantic identifier, compatible version, and provenance representation.

| Order / role | Declared inputs | Candidate output | Required dependency and failure boundary |
|---|---|---|---|
| Resolve and validate profile | One selected `SimulationConfigData`; its referenced definitions; compatible simulation/content versions; calendar and configuration inputs; selected built-in contributors; declared seed/random context. | An immutable resolved profile/context, normalized references, effective configuration/calendar, and validated dependency graph. | Runs first. Reject unresolved/duplicate identities, invalid references, incompatible definitions, missing required dependencies, cycles, ambiguous order, or unsupported profile selections before candidate facts are produced. |
| Prepare authored world and spatial substrate | Resolved profile plus authored city/site/connection data. | Candidate city/site facts and representations; spatial facts only in the explicitly selected accepted spatial ontology. | Precedes actor placement and any dependent Knowledge. Do not duplicate P8 truth in the legacy `SpatialNetworkRuntime`. P8-A/B/C/D edges apply only to selected factual geography, passages, anchors/Person positions, or route plan/Knowledge outputs. |
| Prepare authored actor and domain facts | Resolved profile, validated world references, authored NPC and initial-state definitions. | Candidate named actors and their configured starting state; selected owner-routed initial warrants/directives and initial Knowledge. | Depends on accepted world references and relevant identity contracts. Knowledge output is perspective-owned. A failed actor/domain contribution rejects the candidate; do not silently skip a required authored row and publish a partial profile. |
| Validate complete profile | All proposed outputs in unpublished candidate authorities and the resolved profile. | Validation evidence/result for completeness, domain invariants, identity/cardinality, references, temporal bounds, selected spatial scope, and provenance. | Runs only after all selected producers. Any failure leaves no result visible to the host/runtime. The supported profile's completeness inventory must be explicit, not inferred from which objects happened to instantiate. |
| Publish and establish pre-start composition | Validated candidate result and its provenance. | One complete initial composition handed to/bound by the runtime host; declaration of the first actually simulated boundary occurs afterward. | A single handoff/seal, not a series of mutations on an observable live world. A failed publication cannot leave partial World Truth. After publication, ordinary runtime authorities apply. |

Only built-in host/domain-owned contributors are selected in this first
profile. Independent contributors must declare stable semantic identity and
version, typed inputs/outputs, owner for each authoritative output,
compatibility requirements, and explicit DAG dependencies. Registration,
asset enumeration, dictionary order, and runtime ID allocation order are not
stage/contributor identities or conflict-resolution rules. The first profile
does not promise a general provider registry, third-party contributor support,
or a P19 public seam.

## 4. Identity, provenance, randomness, and conflicts

### Identity

- Preserve authored content/definition identity where the referenced domain
  defines it; keep it distinct from an instance identity, `PersonId`, P8
  spatial IDs, and runtime representation IDs.
- Semantic identities required by causal facts or reconstruction must be
  stable for the compatible profile and inputs, and recorded in the result.
  They must not be derived from incidental registration order or used as
  aliases for `RuntimeIdAllocator` values.
- The current Unity NPC bootstrap creates representation IDs but no Person
  records/PersonIds. The concrete authored source/mapping needed for the
  supported profile is unresolved; no implicit `NpcData.DefinitionId` to
  `PersonId` equivalence is approved by this proposal. Formal checkpoint
  acceptance must select an authored stable Person identity or a reviewed,
  versioned deterministic mapping and define duplicate/collision behavior.
- Likewise, current city/location/route materialization uses legacy runtime
  IDs. It does not provide the authored `HexId`/`LocationId`/terrain provenance
  inputs required to claim canonical P8 geography. P8 mapping is conditional
  as specified in §6; the proposal does not guess that mapping.

### Provenance and compatibility

The published initial composition must retain enough compatible causal inputs
to identify: simulation version; supported profile/version; effective
configuration and calendar values/revisions; selected authored definitions and
versions; ordered built-in stage/contributor identities and versions; declared
dependencies; each authoritative output's owner and causal input identity;
and the declared initial boundary. A content name or preset name alone is not
sufficient where its referenced values can change. Exact manifest format,
version-comparison policy, and serialization are deferred to the accepted
checkpoint and later persistence designs.

### Randomness

No procedural world values are promised in the first profile. Any stage that
does consume authoritative randomness must declare its named purpose, causal
inputs, algorithm/derivation version, and root-seed source; independent
purposes must not share a consumption-order-sensitive stream. The compatible
random context/provenance must be recoverable with the initial output. The
current `useFixedSimulationSeed` fallback behavior alone is not sufficient
evidence of this contract. Exact derivation and draw algorithms remain an
acceptance gate; this proposal does not choose one.

### Contribution conflicts

The resolved DAG must reject duplicate stage/contributor identities, missing
or incompatible dependencies, undeclared input reads/writes, cycles,
ambiguous ordering, and conflicting writes without an accepted policy.
Dependency order is deterministic; independent eligible built-in stages use
their accepted stable identities as tie-breaks. First-profile outputs should
be disjoint by owner/key where possible. Duplicate authored semantic identity
or two writes to one authoritative fact are rejected before publication
unless the accepted contract names a deterministic domain-owned merge rule.
Never select a winner by last writer, source enumeration, or RuntimeId order.

## 5. Validation, atomicity, and acceptance evidence

The candidate contract should require evidence that:

1. the same compatible authored profile, definitions/versions, effective
   configuration/calendar and declared random inputs produce the same
   authoritative initial composition across supported hosts;
2. irrelevant host collection/discovery order cannot change authoritative
   output, while explicit authored input changes affect only declared
   dependents;
3. invalid/missing/duplicate/incompatible declarations, unresolved authored
   references, cycles, and write conflicts fail before any candidate is
   published;
4. each included output is present exactly according to the accepted authored
   profile inventory, and profile completeness is validated against the
   relevant owner/domain invariants;
5. a failed stage, owner validation, cross-domain check, or publication leaves
   no partial initial world visible;
6. semantic identity/provenance does not use runtime allocation order, and all
   cross-domain references resolve in their owning authorities; and
7. there is no simulated history before the first actually simulated boundary;
   after publication all further World Truth changes use ordinary runtime
   authorities.

Required concrete test fixtures, full P9 integration suites and repository
promotion gates belong to the accepted implementation checkpoint. This
proposal changes no executable content and has no Unity test requirement.

## 6. Conditional dependency edges

- P8-A is required only if this profile publishes authored factual Hex,
  Location, terrain identity/revision, or anchors. P8-B is required only for
  included passage/barrier/crossing facts. P8-C is required only for included
  City/Site anchors or Person-level positions. These are canonical at the
  stated baseline.
- P8-D is canonical and is a dependency only if a selected profile output
  includes route plans or route Knowledge. The current proposal's first
  profile does not add these outputs.
- P8-E remains a design-approved candidate with implementation/promotion
  pending in the current State. Its civil-travel slice is not a blanket P9
  prerequisite.
- A daily first profile with no initial temporal facts does not wait for P18.
  Any future profile that initializes timed activities consumes the relevant
  accepted/promoted P18 contract/capability. P20 is conditional only on
  selected shared activity/participation facts. P18 does not wait on P20.
- P19 loader/public API, arbitrary external-provider support, local generation
  catalogs, and mod lifecycle are not built or required by this first profile.
  Later consumers can bind reviewed contributors to accepted semantic
  contracts after those consumers/contracts exist.
- P10 local generation is a later consumer of the P9 genesis/provenance and
  relevant P8 local-anchor contracts. P12/P13 continuation/reconstruction
  remain responsible for preserving/recovering initial and later state.

## 7. Material unresolved technical gates

These items do not reopen the approved first-delivery product scope. They must
be resolved in the formal contract before implementation authorization:

1. **Person identity bridge:** the existing authored `NpcSimulationConfig`
   path yields `NpcRuntime` objects but no `PersonStore`/`PersonId`. Select the
   authoritative authored Person identity source or a versioned deterministic
   mapping, and define duplicate/collision behavior without treating runtime
   IDs or definition IDs as semantic identity by assumption.
2. **Spatial owner mapping:** the authored Unity bootstrap currently makes
   `SpatialNetworkRuntime` locations/routes. Decide which of these facts the
   first profile must publish through canonical P8 authorities and how
   authored references map to P8 identities. If a category is excluded, say
   so explicitly in the profile inventory. Do not duplicate P8 or quietly
   promote the legacy network to factual geography.
3. **Stable profile/content compatibility:** no profile manifest or
   compatibility/version representation is established by this proposal.
   Select what changes invalidate compatibility and what authored values are
   included in the causal input identity.
4. **Stage IDs and random algorithms:** assign stable stage/contributor IDs
   and compatible versions to the accepted roles, plus an exact per-purpose
   random derivation only for any stage that consumes randomness. The proposal
   intentionally makes no algorithm choice.
5. **Atomic publication seam:** current `TesteSimulacao` constructs objects in
   sequence and then composes `SimulationRuntime`; candidate isolation,
   complete-profile validation, and the single visibility boundary need a
   bounded API/ownership design.

No product-scope choice, P9 checkpoint ID, or implementation approval is
pending in this proposal. The gates above are technical and ownership
resolution requirements, not permission to begin implementation.

## 8. Proposal boundary and required review

This is a proposed contract input for review, not an accepted P9 checkpoint
contract. It does not amend `SIMULATION_ARCHITECTURE.md`, Phase 9 Brief/State,
the Roadmap, or Phase 8 State. It does not authorize implementation or claim
that the current Unity bootstrap already meets the deterministic-genesis
contract.

Required next gates are independent architecture/design review of this
proposal, formal human acceptance of the checkpoint contract (including the
technical gates in §7), and separate implementation authorization under
repository policy. No checkpoint IDs are assigned here.
