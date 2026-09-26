# Phase 9 First-Profile Checkpoint Contract Proposal — UNAPPROVED

**Status:** proposed bounded contract. Independent review **PASS** at
`12313cb18d351041b75caa04d658b8fab5b75e9b` against the historical canonical
baseline `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, including architecture
update `c285466` and both 2026-09-26 alignment records. That review found no
blocking issue and confirmed the authored population/economy and order-sensitive
input corrections. This revision refreshes the proposal after P8-E promotion;
independent review of this refreshed text passed at
`4895cf91b8205d3ba75db8f2d9e3da8e76804fde`. Formal acceptance remains pending.
This document assigns no approved P9 checkpoint ID, changes no Phase
Brief/State/Roadmap, and authorizes no implementation.

**Product scope:** the user-approved first delivery is the existing authored
Unity bootstrap world as a proving profile for a generic deterministic genesis
pipeline. World and population extent come from authored configuration. The
profile promises no procedurally generated terrain, settlements, population,
or pre-simulation backstory. Later procedural consumers remain possible through
reviewed seams, but are outside this profile.

**Historical authoring baseline:** canonical `codex/phase8/canonical` at
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, containing architecture/roadmap
refresh `c285466c355103d3637ac165246591b72eb7bda0`; refreshed entry proposal
`05ba224da8cead8221d12fb1b9dc64da5a3b61d2`; and refreshed technical-design
proposal `7bb3312c7b045067dd2dee55451d71c8fd796880`. At that historical
baseline, P8-A through P8-D were canonical and P8-E implementation/promotion was
pending.

**Current revalidation baseline:** `codex/phase8/canonical` at
`77f3e1a47a1e007492a794ea777d681a21a36d09`, including P8-E promotion
`d95b60d174cb0b17df09e2775b3cbd134c74b21f`. The architecture baseline remains
`c285466c355103d3637ac165246591b72eb7bda0`; both alignment records remain
current. P8-A through P8-E are canonical at this baseline.

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
| `Cities` and `CityData` | Creates City runtimes and legacy locations/routes. `CityRuntime` initializes settlement population from `initialPopulation`, market stock from `marketItems`, counterparty liquidity from `marketLiquidity`, population economy from `populationConsumption`, and retains `productionConfigs` for daily production. | These are valid authored starting-world/economy inputs and are included in the profile fingerprint and output inventory. Preserve configured extent and referenced definitions. This legacy spatial model is not P8 factual geography; any mapped P8 outputs use P8 owners. |
| `ExplorableSites` | Creates site runtimes and legacy locations/routes; authored initial knowledge may refer to them. | Preserve selected authored site instances and references. No generated sites or local topology are promised. |
| `Npcs` (`NpcSimulationConfig`) and referenced `NpcData` | Creates one named `NpcRuntime` for each accepted authored config entry, with starting city and initial money; applies configured inventory. | World/population extent is authored configuration. No seed-driven additions or generated population aggregates are in scope; `CityData.initialPopulation` is a valid authored aggregate input. The first profile keeps these legacy bootstrap NPCs outside `PersonStore`; it creates Person state only if a config row supplies an explicit stable per-instance `PersonId`. |
| `Actions`, statuses/jobs and content definitions | Supplies configured action/content definitions and state references to host/domain systems. | Resolve compatible references before materialization; do not mutate authoring assets or equate a content definition ID with a semantic Person/world identity without an accepted rule. |
| Initial known sites and bootstrap knowledge | Bootstraps selected site, spatial, and commercial Knowledge for named runtime NPCs. | Knowledge remains perspective-owned and distinct from World Truth. Include only currently selected authored initial Knowledge; do not expose hidden facts to every actor. |
| Initial warrants and scheduled directives | `InitializeJusticeState` calls `JusticeSystem.CreateInitialWarrants` in config-list order, then syncs wanted statuses; `CreateScheduledDirectives` allocates DirectiveIds while traversing its config list. | Keep warrants in their legal owner and directives as scheduled inputs/commitments. Preserve each authored list's order in the causal fingerprint because warrant penalties can accumulate and directive/runtime IDs are allocated during traversal. Neither is simulated history merely because it exists at startup. |
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
| Authored macro-world and economy inputs | Configured City definitions and connections/routes, `initialPopulation`, initial market items/liquidity, population-consumption economy inputs and daily production configs, plus configured explorable sites. | These are valid profile inputs: City, population and market initialization becomes owner state, while production configs remain compatible daily-rule inputs. The profile adds no procedurally generated settlements, terrain, population, or economy facts. Current authored inputs do not provide canonical P8 Hex/Location identity and terrain provenance, so unmapped legacy locations/routes create no P8 facts. |
| Named authored actors | Configured NPC definition, starting-city reference, initial money/inventory and the row's authored initial known sites. Count/extent follows accepted authored entries; no new people, PersonIds, or aggregate population is generated. | Existing NPC/runtime, inventory and perspective-owned Knowledge authorities own these outputs. Legacy bootstrap NPCs remain outside `PersonStore` unless that config row carries an explicit stable per-instance `PersonId`; definition IDs and allocated runtime IDs never imply Person identity. |
| Initial perspective state | Only the initial Knowledge and warrants/directives selected through the current host path. | Knowledge remains observer-owned; warrants belong to their legal domain; scheduled directives are not past simulation history. |
| Execution composition | Domain systems and services needed to run the supported daily profile. | Composition is not itself a truth-generating stage; systems do not gain extra authority from being constructed. |

The first profile contributes no procedural terrain, settlement placement,
population, local topology, or backstory. It preserves the already-authored
`CityData` initial population, market stock/liquidity, population-economy
settings and production inputs as valid profile content. Only generated
additions are excluded. Future profiles may add dependent stages after their
product/content and domain contracts are accepted.

## 3. Proposed stage roles and dependency contract

The following are proposed built-in stage identities, not checkpoint IDs.
Changing a stage's inputs, outputs, or ordering semantics requires a new
compatible stage version. These are contract-level choices for the bounded
profile, not claims about existing implementation.

| Proposed stage identity | Declared inputs | Candidate output | Required dependency and failure boundary |
|---|---|---|---|
| `p9.genesis.resolve-profile/v1` | One selected `SimulationConfigData`; referenced definitions; profile schema/version; calendar and configuration inputs; built-in stage manifest; current bootstrap seed setting. | Immutable resolved profile, normalized references, effective configuration/calendar, and validated dependency graph. | Runs first. Reject unresolved/duplicate identities, invalid references, incompatible definitions, missing dependencies, cycles, ambiguous order, or unsupported profile selections before candidate facts are produced. |
| `p9.genesis.authored-world/v1` | Resolved profile plus authored city/site/connection, initial population, market/liquidity, consumption and production data. | Candidate City/site representations, authored population and economy facts, and legacy route references; canonical P8 spatial facts only when explicit authored identifiers/provenance map to P8 owners. | Precedes actor placement and dependent Knowledge. Unmapped legacy locations/routes remain in `SpatialNetworkRuntime`; do not invent P8 IDs/topology or duplicate P8 truth. P8-A/B/C/D edges apply only to selected mapped facts or downstream outputs. |
| `p9.genesis.authored-actors/v1` | Resolved profile, validated authored world references, NPC and initial-state definitions. | Candidate named legacy NPC runtime state and selected owner-routed initial warrants/directives/Knowledge. Create `PersonRuntime` only for a config row with explicit stable per-instance `PersonId`. | Depends on accepted world references. Knowledge remains perspective-owned. A failed or invalid required row rejects the candidate; it is not silently skipped to publish a partial profile. |
| `p9.genesis.validate-profile/v1` | All proposed outputs in unpublished candidate authorities and resolved profile. | Completeness and owner/domain validation result covering identity/cardinality, references, temporal bounds, selected spatial scope, and provenance. | Runs after producers. Any failure leaves no result visible to host/runtime. Completeness is checked against the explicit profile inventory, not inferred from instantiated objects. |
| `p9.genesis.publish/v1` | Validated candidate result and provenance. | One complete initial composition bound by the runtime host before its first `AdvanceDay`. | Publish by one host-visible composition handoff after validation; candidate stores are not reachable through the live runtime before that handoff. Failure cannot leave partial World Truth visible. After publication, ordinary runtime authorities apply. |

The v1 dependency edges are fixed: `resolve-profile` precedes both
`authored-world` and `authored-actors`; `authored-world` precedes
`authored-actors`; both producer stages precede `validate-profile`; and
`validate-profile` precedes `publish`. Authored city definitions are ordered
by ordinal `DefinitionId`; site configs by site `DefinitionId`, anchor-city
`DefinitionId`, then authored travel-days; connections by origin ID,
destination ID, then authored travel-days; NPC rows by ordinal
`NpcData.DefinitionId`. For `CityData.marketItems` and `productionConfigs`,
`NpcSimulationConfig.initialInventory` and `initialKnownExplorableSites`,
`SimulationConfigData.initialWarrants` and `scheduledDirectives`, preserve the
authored list order and include each element's zero-based position and consumed
values in the compatibility fingerprint. These lists are traversed by current
initialization/owner code, can contain repeated content references, or
allocate/combine runtime state during traversal; sorting them by definition
ID would change or ambiguously identify results. When a stable semantic sort
key is unique it defines order; when keys collide and order is meaningful,
the authored order is causal and fingerprinted. Duplicate output keys without
a domain-supported multiplicity rule fail validation. This makes runtime
allocation independent of incidental host enumeration while keeping
`DefinitionId` in its content-reference role.

Only the five built-in contributors above are selected in this first profile.
Registration, asset enumeration, dictionary order, and runtime ID allocation
order are not stage identities or conflict-resolution rules. The first
profile does not promise a general provider registry, third-party contributor
support, or a P19 public seam. Later contributors require a separately
accepted contract for typed inputs/outputs, output owners, compatibility, and
DAG dependencies.

## 4. Identity, provenance, randomness, and conflicts

### Identity

- Preserve authored content/definition identity where the referenced domain
  defines it; keep it distinct from an instance identity, `PersonId`, P8
  spatial IDs, and runtime representation IDs.
- Semantic identities required by causal facts or reconstruction must be
  stable for the compatible profile and inputs, and recorded in the result.
  They must not be derived from incidental registration order or used as
  aliases for `RuntimeIdAllocator` values. Existing `DefinitionId` values may
  identify referenced authored content and provide ordinal sort keys, but do
  not identify a Person or an instance of a runtime entity.
- The current Unity NPC bootstrap creates `NpcRuntime` representations but no
  Person records/PersonIds. For this profile, legacy bootstrap NPCs remain
  outside `PersonStore` unless their config row has an explicit stable
  per-instance `PersonId`. `NpcData.DefinitionId` may identify the referenced
  content definition but never aliases a Person or runtime instance. Since
  `NpcSimulationConfig` has no separate authored instance key, genesis v1
  accepts at most one NPC config row per `NpcData` definition; repeated rows
  fail validation rather than receiving order-derived identities. Duplicate
  explicit PersonIds reject the candidate; no Person ID is synthesized.
- Current city/location/route materialization uses legacy runtime IDs and
  authored city/site references, not authored canonical `HexId`/`LocationId`
  plus terrain provenance. Therefore the first profile publishes no P8 spatial
  fact for an unmapped legacy location/route; it keeps that data in the legacy
  runtime model. When an authored input does explicitly map to a P8-owned
  identity and provenance, publish it through that P8 owner and validate
  uniqueness there. No mapping or geography is inferred.

### Provenance and compatibility

The published initial composition must retain a versioned causal manifest
with these bounded fields: profile kind `unity-authored-bootstrap`; genesis
manifest schema version `1`; simulation build/contract version; effective
configuration and calendar values; stable identities and consumed values of
referenced authored definitions; the ordered stage identities/versions and
declared dependencies; per-output owner and causal input identity; the current
initial seed value/source; and the first actually simulated boundary. Compute
the build/contract version from an immutable build identifier (the repository
source commit is the proposed value); if no immutable build identifier is
available, v1 rejects publication rather than treating two unknown builds as
compatible. Every consumed authored object must have a stable content
identity; otherwise profile resolution rejects it. Compute an input
fingerprint as SHA-256 over canonical UTF-8 records with length-prefixed field
values; canonicalization uses invariant scalar formatting and ordinal ordering
for unordered keyed inputs. Preserve order only where the authored/domain
contract declares order meaningful. Exclude logging, display names, renderer
state, and incidental object-discovery order. A content or contract change
that changes the consumed manifest changes the fingerprint and is
incompatible with the prior profile result; no migration is implied. This is
a proposed genesis manifest, not a persistence format commitment.

### Randomness

No genesis stage in this profile consumes authoritative random draws or
promises procedural values. Record the seed value passed by the current host:
`simulationSeed` when `useFixedSimulationSeed` is true, otherwise `0`. This
seed is provenance only for genesis v1 and cannot affect any output in this
profile. A later stage that needs random draws must introduce a new accepted
genesis contract version that defines named purpose streams, derivation and
draw algorithms, causal inputs, and compatibility behavior; it cannot silently
consume the runtime's shared stream or change this profile's results.

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
For genesis v1, the fixed stage dependency edges and authored collection sort
keys in §3 are the complete ordering rule; there is no registration-order or
parallel-completion tie-break.

## 5. Validation, atomicity, and acceptance evidence

The candidate contract should require evidence that:

1. the same compatible authored profile, definitions/versions, effective
   configuration/calendar and seed provenance produce the same authoritative
   initial composition across supported hosts (the genesis stages consume no
   random draws in v1);
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
6. semantic identity/provenance does not use runtime allocation order; legacy
   NPC rows without explicit per-instance PersonIds remain outside
   `PersonStore`; unmapped legacy spatial representations create no P8 facts;
   and all cross-domain references resolve in their owning authorities; and
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
- P8-E is canonical at the current revalidation baseline. The selected
  authored-bootstrap profile creates no P8-E route-plan, transit-position, or
  travel-operation facts, so P8-E is not a blanket P9 prerequisite. A future
  profile that consumes its facts must use the P8-owned authorities and the
  relevant promoted P8-C/D/E capabilities. This preserves P8-D's rule of one
  active route plan per Person and P8-E's in-transit replacement guard; neither
  defines a permanent Activity-to-Person cardinality. No automatic travel or
  timed activity is introduced by this genesis profile.
- A daily first profile with no P18 activity/participation facts does not wait
  for P18. Existing scheduled-directive commitments remain under their current
  domain owner and semantics; this profile does not turn them into
  `ActivityInstance` state. Any future profile that initializes timed
  activities consumes the relevant accepted/promoted P18 contract/capability.
  Activity definitions, stable `ActivityInstanceId` values, and participant
  `PersonId` values remain separate identities; an activity may have one or
  multiple participants, so it is not keyed by one actor. P20 is conditional
  only when selected outputs include shared activity/participation facts. P18
  does not wait on P20, and P20 waits only on
  the relevant P18-A/B/C capabilities, not P18-D by phase number.
- P19 loader/public API, arbitrary external-provider support, local generation
  catalogs, and mod lifecycle are not built or required by this first profile.
  Current extensibility constraints still apply to seams and review: preserve
  declared stage inputs/outputs, dependencies, stable identities, deterministic
  ordering, and domain-owned publication without adding a speculative public
  loader/API. Later consumers can bind reviewed contributors to accepted
  semantic contracts after those consumers/contracts exist.
- P10 local generation is a later consumer of the P9 genesis/provenance and
  relevant P8 local-anchor contracts. P12/P13 continuation/reconstruction
  remain responsible for preserving/recovering initial and later state.

## 7. Bounded technical requirements and remaining gates

The following defaults narrow the prior technical questions without changing
product scope:

1. **Person identity:** legacy authored NPCs remain `NpcRuntime`-only and
   outside `PersonStore`. A config row enters Person-backed systems only when
   it carries an explicit, stable, per-instance `PersonId`; duplicate IDs
   reject the full candidate. Never synthesize one from `NpcData.DefinitionId`
   or a runtime ID. The current `NpcSimulationConfig` has no such field, so
   current bootstrap rows remain outside `PersonStore`; adding Person-backed
   rows requires an authoring-contract version change.
2. **Spatial facts:** current authored city/site/connection references remain
   in their existing legacy runtime model. Publish P8 facts only when an
   authored input already supplies an explicit P8 identity and required
   provenance; otherwise publish no corresponding P8 fact. Do not infer
   Hexes, topology, or mappings.
3. **Compatibility:** use the manifest fields and SHA-256 canonical input
   fingerprint specified in §4. Exact fingerprint mismatch means incompatible
   for genesis v1; no migration or compatibility guessing is promised. Missing
   immutable build or consumed-content identity rejects profile resolution.
4. **Stage identity and randomness:** use the five concrete stage identities
   in §3. Genesis v1 performs no random draws; the recorded current
   seed value is provenance only. Random-consuming stages require a separately
   accepted contract version with exact stream and draw rules.
5. **Publication:** construct candidate-owned stores unreachable from
   `SimulationRuntime`, validate the full profile, then expose the result with
   one host composition reference handoff before the first `AdvanceDay`.
   Validation or handoff failure leaves the live runtime unbound to the
   candidate. This publication seam is a requirement; the existing sequential
   `TesteSimulacao` construction does not yet implement it.

These choices become formal only if the proposal is accepted. The remaining
gates are formal acceptance of this contract and separate implementation
authorization; no checkpoint ID is assigned here.

## 8. Proposal boundary and required review

This is a proposed contract input for review, not an accepted P9 checkpoint
contract. It does not amend `SIMULATION_ARCHITECTURE.md`, Phase 9 Brief/State,
the Roadmap, or Phase 8 State. It does not authorize implementation or claim
that the current Unity bootstrap already meets the deterministic-genesis
contract.

Independent architecture/design review passed for the proposal revision
identified above. Remaining gates are formal human acceptance of its bounded
technical requirements and separate implementation authorization under
repository policy. No checkpoint IDs are assigned here.
