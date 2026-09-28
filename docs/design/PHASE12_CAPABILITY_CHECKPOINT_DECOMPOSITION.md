# P12-A capability checkpoint decomposition — proposal only

**Status:** Proposed dependency decomposition for the already accepted
`P12-A — UnityBootstrap-Daily-v1` scope. These labels are temporary
descriptors, not checkpoint IDs or accepted scopes. This document does not
amend the P12-A scope record, authorize implementation, or establish that any
capability exists.

**Evidence baseline:** owner inventory candidate
`e995e2ae4c10bac1d30d1d158c75216f73842480`; inventory references the
authoritative architecture `c285466`, P8 State `470667d`, P9 closure
`82396ae` and P9-B implementation integration `d9a62d7`, P11 closure
`308e24d`, P14 State `4caecbb`, P18 canonical/State `ba8076c`, and P20 State
`7a81cc0`, plus both current architecture alignment records. Revalidate these
references and the actual canonical runtime composition before any checkpoint
implementation is scheduled.

**Current evidence:** the inventory demonstrates no included owner with a
complete exact immutable export and staged hydration path. No proposed
capability checkpoint below currently meets its gates. `P12-A` remains
`WAIT_DEPENDENCY`; accepted scope is not implementation authorization.

## Boundary and decomposition rule

The sole profile is the accepted `UnityBootstrap-Daily-v1`: the
SampleScene-selected `Simulation-GeneralTest.asset` through the validated
`TesteSimulacao.InitializeSimulation` path, one compatible build/runtime and
current-host numeric profile, built-in providers selected by effective
configuration, and capture only after a successful completed daily advance.
The selected profile includes P9-B authored geography and exactly the P8-A
Hex/anchored-Location/scale facts described in the accepted contract. It has
no external `WorldCommand` service or queue. Preserve P11 `ActorChoiceStore`
terminal causal history where that store is composed.

The groups below follow shared owner dependencies and the accepted restore
order. A section is complete only when its domain authority can export exact
immutable values and hydrate those values into a private staged composition;
DTOs without owner hydration, diagnostic snapshots, runtime clones, rollback
snapshots, and Unity asset serialization do not satisfy the gate. Use stable
typed IDs and validate owner revisions, allocators, relationships, and
cardinality. Do not implement a parallel authoritative store.

## Proposed dependency graph

```text
Profile admission / completed-boundary lifecycle
                 │
                 ▼
Identity, P9-B genesis provenance, and deterministic-random roots
                 │
          ┌──────┴─────────┐
          ▼                ▼
Bootstrap factual     Profile-selected core and
roots + population    official daily domain owners
          └──────┬─────────┘
                 ▼
Knowledge, directives, P11 terminal choices, and active commitments
                 │
                 ▼
Private staged restore, whole-graph validation, single publication,
round-trip/parity and rejection evidence
```

The factual roots and core/official domain owner groups may be developed in
parallel after the identity/genesis roots **only** where their owner factories
and file/hotspot ownership are isolated. Their combined candidate must be
integrated before dependent Knowledge and commitment hydration is considered
complete. The final whole-graph/publication checkpoint depends on every
included owner section and the live profile inventory.

## Proposed capability checkpoints

### Profile admission and completed-boundary lifecycle

**Purpose and dependencies:** Establish the exact supported composition and
the only permitted daily capture point. This is the prerequisite for every
owner export and staged restore group; it is not itself a save/load
capability. It has no dependency on the other proposed capability groups.

**Coverage deliverables:**

- A versioned admission manifest binds the selected profile, compatible
  build/runtime and numeric environment, authored content/provider identity,
  effective configuration, calendar, P9-B profile/schema identity, and
  actually composed built-in providers. It rejects unsupported compositions
  before owner export or live-object allocation.
- A capture token is issued only after `SimulationRuntime.AdvanceDay` returns
  successfully. It identifies the runtime, absolute day, and completed
  advance sequence. Any supported authoritative mutation after token issue
  invalidates it; a later successful advance issues a new token.
- Capture is restricted to the simulation owner thread, between operations,
  with a healthy mutation guard and no advance, command, transaction, or
  bootstrap work in progress. Reentrant/concurrent capture or mutation is
  rejected. A failed/throwing advance never earns a boundary token, even if
  time was partially advanced.
- Required versus explicitly empty profile sections and the rejection rules
  are enumerated. A populated excluded owner is an admission failure, never
  silently omitted.

**Likely areas to inventory/own:** `TesteSimulacao.InitializeSimulation`,
`SimulationBootstrapComposition`, `SimulationRuntime` daily advance and
mutation lifecycle, effective configuration/calendar resolution, and the
future P12 envelope/admission coordinator. Shared `SimulationRuntime` and
daily-loop ownership must be explicit.

### Identity, genesis, and deterministic roots

**Dependencies:** Profile admission/lifecycle contract.

**Coverage deliverables:**

- Exact typed identities and sequence state from their owners: per-kind
  `RuntimeIdAllocator` high-water/next values, `SimulationRecordSequence`,
  and any allocator proven to be used by this composition. Preserve identity
  distinctions, validate global uniqueness and monotonicity, and restore
  without allocating replacements.
- The selected P9-B manifest and lineage: profile/schema identity and
  fingerprint; inherited P9-A stage identities; P9-B stage identity/version
  and dependencies; authored inputs/outputs; seed/config/calendar; provenance
  and first simulated boundary. Hydration retains historical output and
  manifest evidence; it never reruns genesis.
- Exact selected P8-A facts under their owner: one authored Hex
  `hex/sample-origin` at `(0,0)` under `axial-hex-v1`, terrain identity and
  revision, one `location/sample-origin` anchored to that Hex, and the
  authored `SpatialWorldScaleContext`. Enforce the profile's exact
  one-Hex/one-Location cardinality and reject missing, duplicate, malformed,
  or extra facts.
- The pinned built-in deterministic random provider identity and every
  continuation-relevant seed/stream/draw context used by the included
  composition. Establish from actual provider-use inventory whether seed-only
  capture is sufficient; do not assume it.
- Immutable owner exports and private-stage hydrators for these sections,
  plus uniqueness, reference, schema, and allocator validation.

**Likely areas to inventory/own:** `RuntimeIdentity` and identity registries /
allocators, `SimulationRecordSequence`, deterministic-random provider and
consumers, P9 genesis/profile manifest handoff, P8-A `SpatialAuthorityStore`,
and the selected bootstrap profile manifest. Do not infer P8 identity from
legacy spatial runtime state.

### Bootstrap factual roots and Person/population relations

**Dependencies:** Profile admission and identity/genesis/deterministic roots.

**Coverage deliverables:**

- City, market, market-item, account/custody, stock/balance, and other
  mutable economic-root facts required by the selected bootstrap, with exact
  item/city/account definition compatibility and owner revisions.
- The configured NPC roster and each evolved `NpcRuntime` fact: stable
  runtime ID, mutable condition/life/status, location and destination,
  current action/behavior and hidden-day state, inventory and money account,
  plan/commitment fields, and definition identity. Roster order is preserved
  only where the existing owner uses it as a semantic tie-break.
- Legacy authored spatial and exploration truth used by the selected
  bootstrap: `SpatialNetworkRuntime` locations/routes, `ExplorableSiteStore`
  site identity and state, site/exploration facts, supported legacy City/Site
  anchors, and the existing legacy position/location links that connect NPCs,
  Cities, routes, and sites. Export each fact from its current domain owner;
  preserve exact typed IDs, route/site/location endpoints, reciprocal links,
  relation cardinality, owner revisions and any state that affects later
  exploration or travel. Hydration restores the recorded links and progress;
  it must not rediscover routes, regenerate sites, re-anchor entities, or
  infer a replacement location from P8 geography.
- These are the profile's existing legacy spatial/site authorities, not the
  P8-owned spatial extension. Keep their identities and link semantics
  distinct from P8-A `HexId`, `LocationId`, and scale provenance. P8-A
  geography is covered by the identity/genesis checkpoint above; legacy
  routes/locations/sites remain covered here even though P8-B through P8-E
  authorities are excluded. In particular, these legacy City/Site anchors
  and position links are not the P8-C anchor/`PersonSpatialPositionStore`
  facts, and legacy routes are not P8-B passage or P8-D route-plan facts.
- Population aggregates, `PersonStore`, genealogy, residence/lifecycle and
  materialization relationships as reachable in the live profile composition.
  Keep Person identity and NPC runtime identity separate: NPC-only rows stay
  NPC-only, and a dormant/non-materialized Person remains addressable by
  `PersonId`. Preserve birth/death dates and owner truth; derive age/maturity
  from calendar/configuration rather than serializing mutable age.
- Owner factories restore roots before references; cross-check account,
  inventory, City/market, NPC/Person, residence, parentage, aggregate and
  materialization bindings, plus legacy City/Site anchor and
  location/route/site/NPC links. Cover both initially empty and
  evolved/populated states; initial asset counts are not maximum
  cardinalities.
- Reconstruction tests round-trip empty and populated legacy network and
  exploration state, including stable route/site/location identities,
  endpoints, supported anchor and position-link cardinality, reciprocal
  bindings, exploration progress, and active references. Vary or remove a
  required endpoint/anchor, duplicate a stable identity, or create a dangling
  link and prove staged hydration rejects before publication. Compare the
  restored owner truth and supported relationships after identical subsequent
  daily inputs; diagnostics alone are not reconstruction evidence.

**Likely areas to inventory/own:** `CityRuntime`, `MarketRuntime`,
`MarketItemRuntime`, economy/account and inventory owners; `NpcRuntime` and
NPC/action state owners; `SpatialNetworkRuntime`, `ExplorableSiteStore` and
the current legacy City/Site anchor and position/link owners;
`SettlementPopulation*`, `PersonStore`, `GenealogyStore`,
residence/lifecycle/materialization authorities. Shared `PersonStore`,
population and spatial/site ownership require explicitly coordinated
ownership windows. The current composition inventory must identify precise
implementations and providers before any per-class implementation plan is
approved.

### Profile-selected core and official daily-domain owners

**Dependencies:** Profile admission and identity/genesis/deterministic roots;
root truth from the preceding group where referenced entities/accounts are
required. This group may be implemented alongside the factual-root group only
with isolated ownership; integration and joint staged validation are required
before dependent groups close.

**Coverage deliverables:**

- Exact owner exports and staged hydrators for every core authority actually
  composed by the selected `SimulationRuntime`, including populated political,
  institutional, office/tenure, property/estate, claims/recognition,
  faction/support, force/manpower/position, conflict/war/battle state, and
  terminal outcomes. An owner being empty at bootstrap does not exclude later
  supported populated state.
- Exact owner exports and staged hydrators for every optional official daily
  provider that the live effective configuration actually composes:
  economy/demography/mortality, merchant trade and Knowledge sharing,
  crime/justice/social appraisal, guard/crime action providers, and related
  provider context. Preserve causal inputs/random context, revisions, current
  commitments, and mutable owner facts; rebuild only owner-defined derived
  indexes and projections.
- A mechanically generated or otherwise auditable inventory maps each
  effective configuration choice to the exact instantiated service/provider,
  owning stores, required envelope sections, hydrators, and tests. Absent or
  unsupported injected providers reject admission.
- Every relation hydrates after its referenced roots and validates typed
  identity, target existence, cardinality, reciprocal links, owner invariants,
  revision/fingerprint state, and terminal disposition.

**Likely areas to inventory/own:** actual `SimulationRuntime` composition,
`SimulationModuleSet` and `TesteSimulacao` provider wiring; political,
institutional, property, force, manpower, conflict/war/battle stores; official
daily economy, demography, mortality, merchant, justice/crime/social
appraisal and guard providers. Use domain-owner subgroups only as isolated
implementation tasks under this one profile coverage checkpoint; a catalog of
providers or files is not completion.

### Knowledge, directives, P11 choices, and active commitments

**Dependencies:** identity/genesis roots, factual roots, and all required core
and official owner sections. These facts refer to those roots and cannot be
validated as a closed graph earlier.

**Coverage deliverables:**

- Knowledge owner truth: holder identity, observed fact, source/provenance,
  freshness and observed/received boundary, and causal stale-check revision.
  Do not regenerate Knowledge from current world truth.
- `ScheduledDirectiveStore`: directive identity, target day/mode/operation,
  actor and action-definition references, processing state and disposition;
  loading never replays an already processed directive.
- Current P11 `ActorChoiceStore` records, complete dispositions, duplicate
  command-ID idempotency history, and next sequence. Preserve terminal
  `Rejected`, `AttemptReturned`, and `AttemptThrew` records. Reject
  `Pending` (including deferred choices) and
  `ConsumedAwaitingTerminalAttempt`. A thrown attempt is capturable only at a
  later successful daily boundary while the runtime remains healthy.
- Active travel, party, expedition, merchant and other supported commitment
  owner facts: stable IDs, member/actor bindings, progress, costs, lifecycle,
  and reciprocal site/market/NPC relationships. Hydration resumes exact work;
  it does not decide, replan, replay, cancel, or apply effects again.
- External `WorldCommand` service/queue composition is not part of this
  profile; admission rejects it. Applied effects remain in their domain truth
  owners and are not replayed from P11 or event/history summaries.

**Likely areas to inventory/own:** Knowledge stores and sharers; `ScheduledDirectiveStore`;
P11 `ActorChoiceStore`; `TravelSystem`, `TravelPartyStore`, `ExpeditionStore`,
merchant commitment owners and their current reciprocal references. The
current live composition determines which conditional commitments/providers
are in fact included.

### Staged restore, graph validation, atomic publication, and parity

**Dependencies:** Every preceding profile, identity, root, domain, Knowledge,
directive, and commitment section has an exact reviewed export/hydrator, plus
the live profile/provider inventory is current.

**Coverage deliverables:**

- Parse into inert values and verify schema, required/empty sections, digest,
  profile/build/numeric identity, content/provider manifest, effective config,
  calendar and completed boundary before allocating live objects.
- Create a fresh private staged composition; hydrate identities before owner
  roots, roots before relations/Knowledge, and owner truths before directives
  and active commitments. Never clear or mutate the currently authoritative
  runtime while constructing a candidate.
- Rebuild only owner-derived registries/indexes/caches/projections. Validate
  per-owner invariants, global typed-ID uniqueness, allocator high-water marks,
  reference targets, definitions, reciprocal bindings, required cardinality,
  causal revisions, provider composition and profile boundary. Do not silently
  repair or default missing causal facts.
- Bind one fresh healthy mutation guard only after every included owner passes
  staged validation. Publish with one owner-controlled runtime reference swap;
  until that swap, failure leaves the running world authoritative and the
  staged candidate unreachable. Dispose/detach old runtime only through its
  owner lifecycle after publication.
- Evidence includes owner round trips for empty/populated facts and
  relationships; rejection before publication for malformed, missing,
  incompatible, dangling, duplicate, excluded-populated and invalid-boundary
  cases; failure preserving the live runtime; and continuation parity from the
  same daily boundary with identical later inputs, compared across all
  included owner truth, commitments, identity/sequence, revisions and random
  outcomes. Canonical diagnostic comparison is supplemental only for fields
  it actually covers.
- Complete the applicable domain and Phase 5–8 regressions, ALL EditMode,
  complete official Smoke and `git diff --check`; add long-run validation if
  daily-loop/long-horizon semantics change.

**Likely areas to inventory/own:** future P12 envelope and capture coordinator,
staging-composition/hydration factories, runtime publication owner, owner
invariant validators and test infrastructure. `SimulationRuntime`,
`TesteSimulacao`, daily advance, diagnostics, and the shared persistence
composition are hotspots and require explicit ownership. This is the final
integration group, not a generic serialization framework.

## Explicit exclusions and rejection behavior

This decomposition preserves the accepted profile boundary. It does not add
product scope. The following remain excluded and must be empty/not composed or
cause profile admission to reject:

- Any populated canonical P8-B, P8-C, P8-D, or P8-E authority state, including
  passage, canonical presence anchors, Person spatial positions, spatial
  Knowledge/route plans, and P8-E travel authorities; only the selected P8-A
  Hex, anchored Location, and scale facts are included. This exclusion
  does not remove the existing legacy `SpatialNetworkRuntime`
  locations/routes, `ExplorableSiteStore` truth, supported legacy City/Site
  anchors, or legacy position links explicitly included in the factual-root
  checkpoint. Legacy locations and routes are not substitutes for P8-A
  identities, and P8-A geography does not replace legacy link state.
- Generated P9/P10 worlds or generated content; only selected P9-B authored
  bootstrap provenance and its exact selected P8-A output are retained.
- P10 Ruin/LocalTopology output (not composed by this profile).
- P14-A material-flow state (not configured by the selected GeneralTest City
  assets).
- P18 timeline, intraday state, activity lifecycle/availability, external
  inputs or continuation extension state (not composed by the selected legacy
  daily bootstrap).
- P19 code mods, loader/module-owned state or retrofit.
- P20 shared activities and participant commitments.
- P13 historical reconstruction, replay-history or independent-fork
  guarantees.
- External P11 `WorldCommand` service/queue and in-flight actor choices.

P14 legacy economy production or ordinary merchant/economy owner state is not
P14-A material-flow state. Absence from the initial asset does not prove an
owner can never become populated during supported play; the profile inventory
must consider evolved state for every included authority.

## Readiness and human gates

The latest evidence says no profile-included group has demonstrated the
required exact immutable export plus staged hydration. The accepted P12-A
contract itself names no implementation checkpoint IDs for its descriptive
closure sequence. The temporary labels in this proposal therefore require
explicit checkpoint-scope acceptance before they become implementation
contracts. That acceptance is distinct from the already recorded P12-A scope
acceptance.

After accepted capability scopes, continue design/review and owner inventory
work as allowed by the Execution Model. Do not schedule P12-A implementation
until all of the following are demonstrated against the current canonical
composition: complete export and staged hydration coverage for every included
owner; successful integration, graph validation, publication and parity gates;
and a refreshed live profile/provider inventory. Separate implementation
authorization remains an independent human gate. P12-A stays
`WAIT_DEPENDENCY` until those conditions and authorization are recorded.

No capability delivery, save/load support, profile closure, implementation
readiness, P13 guarantee, or new product scope is asserted by this proposal.
