# Phase 10 Technical Design Proposal — UNAPPROVED

**Status:** bounded technical design proposal — UNAPPROVED. This document
assigns no checkpoint ID, changes no Phase State/Roadmap, and does not
authorize implementation. The user-approved first-profile scope is exactly
one `ExplorableSiteKind.Ruin` at an existing canonical P8 `LocationId`, with a
finite local topology limited to semantic places, one or more entry points,
containment only if needed, and explicit local connections. Entrance →
Courtyard → Inner Chamber may be a tiny deterministic proof fixture. P9
genesis/provenance is consumed; this profile adds no generated content. The
LocationId-neutral LocalTopology ownership/migration capability remains
unpromoted, and this refresh requires independent review.

**Prior independent technical-design review:** PASS for the candidate semantics
at its then-reviewed baseline; the present P9-promotion refresh requires
independent re-review.
The final P9-profile boundary wording clarification was independently reviewed
at P10 document commit `a914a0d7d9fb5f8f2f74d5b87b15923a12a9af78`. Review
approval does not constitute formal P10 acceptance or implementation approval.

**Reviewed baseline:** `codex/phase8/canonical` at
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, containing architecture refresh
`c285466c355103d3637ac165246591b72eb7bda0`; at that historical baseline,
Phase 8 State recorded P8-A/B/C/D promoted and P8-E design-approved with
implementation/promotion pending. The
review also covered the P9 checkpoint-contract proposal
`44f731490bc4fb9bade72784dd5c064d26e320df`, the intraday/extensibility
alignment at `c285466c355103d3637ac165246591b72eb7bda0`, and the
multi-participant-activity alignment at
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`. Existing reviewed entry baselines:
P10 `b0158984c70c008cacdfe4390463b9250ad1f0ad`; P9 refreshed entry
`05ba224da8cead8221d12fb1b9dc64da5a3b61d2` and technical design
`7bb3312c7b045067dd2dee55451d71c8fd796880`. Those reviews were design evidence;
P9-A's later promotion is recorded in the current-baseline revalidation below.

**Current baseline revalidation:** P9 canonical `codex/phase9/canonical` at
`96f2c1aaf742f313bbb9643e5f5b3d844c402c78`, with architecture baseline
`c285466c355103d3637ac165246591b72eb7bda0`, current P8 canonical State
`470667d37863384edadb3d93ef64d8004aff46a3`, and both current alignment
records (intraday/extensibility at `c285466`; multi-participant at
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`). The P9 advance from
`988b6f5d14e12359e93464bae5e0048ca970ad86` to
`96f2c1aaf742f313bbb9643e5f5b3d844c402c78` records Phase 9 State/Brief closure
only; it changes no P9-A capability, APIs, or content boundary. P8-A through
P8-E and P9-A are promoted. P9-A establishes the authored
Unity-bootstrap profile and a dependency-aware pre-start pipeline, but its
selected profile creates no local topology or P8-owned spatial outputs. The
P9 genesis/pipeline foundation edge is satisfied; P10's later local contributor
must still bind to the actual promoted P9 stage, candidate-publication and
provenance APIs, with its additional output capability separately reviewed
and validated. P8-C's City/Site anchor bindings are promoted and include the
`ExplorableSite` owner kind. Inspection shows, however, that `TryBindSite`
accepts a stable semantic key while composed-runtime owner resolution checks
that value with `ExplorableSiteStore.TryGetByRuntimeId`.
`LocalTopologyOwnerReference` also stores site and macro-location RuntimeIds
and validates them through the runtime identity registry. Stable authored Ruin
identity is therefore not proven to resolve through the current binding and
topology path. The bounded identity and LocationId-neutral topology seam
remains a capability prerequisite. P8-B passage facts are excluded. P8-E,
intraday P18, and multi-participant P20 are not dependencies for this
daily-only, single-site profile because it adds no route, travel,
temporal-activity, or shared-participation facts. Current extensibility
alignment requires declared stage/contributor identities and versions,
typed inputs/outputs, explicit dependencies, deterministic ordering/conflict
handling, purpose-scoped randomness where needed, causal provenance, and no
implicit regeneration/retrofit. P19 loader/API and retrofit remain deferred.

## 1. Recommendation and bounded first profile

The approved first profile is exactly one domain-owned
`ExplorableSiteKind.Ruin` at an existing canonical P8 `LocationId`, composed
before the first simulated boundary through P9's promoted authored-bootstrap
pipeline. P9 genesis/profile/provenance is consumed and preserved or
validated; P10 adds no generated content and does not alter P9's profile. The
Ruin has a finite topology of semantic places, one or more entry points,
containment only if needed, and explicit local connections. Entrance →
Courtyard → Inner Chamber may serve as a tiny deterministic proof fixture, not
as a general layout-generation requirement.

This profile excludes City, Market, population/NPC/economy, Passage/Route,
Knowledge, activities, loot, encounters, construction, and all other gameplay.
It does not imply that every P8 Location has a local topology. The profile is
daily-only and adds no initial activities, route plans/Knowledge, or
multi-participant facts. P18 and P20 are not current dependencies; P18/P20
become conditionally relevant only if a future explicitly accepted scope adds
their facts.

This design fixes the bounded implementation contract for independent review.
It does not assign a checkpoint ID, authorize implementation, or promote a
capability. Identity, compatibility, pipeline integration, and closure evidence
below apply only to this one accepted profile; they do not define a universal
Ruin schema or generation platform.

## 2. Spatial ownership and the P8-C seam

Use the existing spatial and domain owners; do not make `Location` a universal
world entity or introduce a parallel local map.

| Fact | Semantic owner / reference | Phase 10 treatment |
|---|---|---|
| Regional Hex, axial coordinate, scale provenance, terrain reference, and Location anchor | Promoted P8-A geography/Location authority | Consume an existing `LocationId` and its validated `AnchorHexId`; do not mint a second anchor or infer location from rendering. |
| Optional passage facts | Promoted P8-B passage authority | Excluded from the recommended first profile unless the selected local consumer requires a regional passage fact. |
| City/Site domain object and its macro Location binding | Relevant domain store plus promoted P8-C anchor binding | The domain object remains distinct from its `Location`; P8-C's current City/Site binding applies only to its delivered owner kinds. |
| Local places, containment, entry points, and traversable local connections | `LocalTopologyStore` / local-topology authority | Require a technical migration seam described below; do not represent local place identity as a runtime object ID. |
| Person spatial position | P8-C Person-level `At`/`InTransit` authority | Not changed or required by the recommended first profile. |

The current `LocalTopologyStore` is only a reusable foundation. Its owner
references are City/Site runtime-owner references, and its macro-location link
is a legacy runtime reference; it does not provide the required neutral
`LocationId`-based composition contract. P8-C establishes City/Site anchor
bindings and Person positions, not a LocalTopology migration. The approved
Ruin's stable semantic identity must bind to its existing P8 `LocationId` and
resolve without promoting `RuntimeId` to semantic identity. The adapter/store
migration below preserves existing City/Site behavior and does not reinterpret
legacy IDs or change unrelated P8 behavior.

The bounded migration contract is:

1. Preserve `LocalTopologyOwnerReference.ForCity(CityRuntime)` and
   `ForExplorableSite(ExplorableSiteRuntime)`, the existing runtime-ID index,
   and current City/Site consumer behavior.
2. Add a stable semantic site-owner representation containing owner kind
   `ExplorableSite`, `ExplorableSiteData.DefinitionId`, and canonical P8
   `LocationId`. Runtime site and legacy macro-location IDs may remain
   resolver handles but are excluded from semantic equality, stable keys, and
   fingerprints.
3. Add semantic lookup keyed by DefinitionId in `ExplorableSiteStore`. Resolve
   the stable owner through the existing `LegacySpatialAnchorBindingStore`
   site binding for that DefinitionId; verify it resolves to the selected
   canonical LocationId and exactly one site runtime handle. Missing,
   duplicate, or conflicting mappings reject the unpublished candidate.
4. Have the P10 composition adapter write local places and connections through
   the stable owner reference while retaining compatibility resolver handles.
   Keep existing City/Site topology records and runtime lookups intact; update
   any dual indexes atomically with publication.
5. Give each place and connection a stable ID scoped by the semantic site
   owner. Validate uniqueness in the candidate and against the published
   composition before publication.

This is a narrow store/API migration plus adapter, not a new local map or a
general Location entity. Sharing a Hex establishes neither domain containment
nor local connectivity. A local connection is explicit topology truth;
geographic adjacency, same-Hex placement, rendering coordinates, or mere
co-location do not imply traversal or access.

## 3. Pipeline participation and candidate ownership

P10 consumes the promoted P9 authored-bootstrap selection and genesis
provenance. It does not rerun genesis, replace the selected P9 profile, add a
content-generation algorithm, or produce new generated content. The current
`SimulationGenesisPipeline` has five fixed built-in stage IDs and a fixed DAG,
executed via `ExecuteStages(Action<string>)`; it has no contribution/output
registry. P10 therefore adds exactly one internal in-repository built-in
stage, `p10.genesis.ruin-local-topology/v1`, after
`p9.genesis.authored-world/v1` and before
`p9.genesis.validate-profile/v1`. It declares its version, dependency, typed
`P10LocalTopologyCandidate` output schema, and stable ExplorableSite/LocationId
output owner in provenance. The authored-actors stage retains its existing
dependency on authored-world and remains before validation. This is not a
public contributor registry, plugin API, or mod hook.

The stage builds an immutable candidate from selected P9 authored inputs and
the bounded local-profile fixture. Validation checks inherited P9 output and
the P10 candidate as one selected profile. The existing publish stage publishes
the complete composition once, after validation and before the first
simulated boundary. Outputs never become live through partial mutation of
`SimulationRuntime`, Unity assets, or authoring data.

Recommended composition sequence:

```text
resolve the P9 base-profile fingerprint/provenance and immutable inputs
→ validate the existing P8 Location/anchor and Ruin owner mapping
→ execute the one internal P10 built-in stage to form a typed candidate
→ validate the combined P9/P10 DAG, candidate and complete selected profile
→ resolve all Location, site, place, containment, entry and connection references
→ publish the complete P9/P10 composition once, before first simulated boundary
→ seal genesis; later site/topology changes require ordinary runtime authority
```

Validation of the combined stage graph, references and complete bounded
profile occurs before publication. A failed stage or invariant discards the
unpublished candidate. No observer or runtime consumer can see partial
site/topology state. P9's existing publish boundary remains the sole handoff;
its payload gains the typed P10 candidate and owner-completeness check, not
sequential live mutations. Do not expand `SimulationRuntime` into the local
composer or make runtime construction/founding inherit genesis privilege.

## 4. Stable identity, provenance, dependency order, and randomness

For this single profile, the Ruin semantic site key is exactly
`ExplorableSiteData.DefinitionId`; this does not define a universal Ruin
schema. Each place and connection has an explicit fixture-local key. Encode
IDs with the existing injective, length-prefixed
`WorldStateSnapshotValue.EncodeStableKey` convention and a typed, versioned
namespace such as `p10.local-topology/place/v1` or
`p10.local-topology/connection/v1`, followed by the DefinitionId and
fixture-local key as separate components. The stable owner reference also
contains canonical `LocationId`. RuntimeId, display name, source-list position,
and runtime allocation do not contribute to semantic identity. Duplicate
DefinitionIds, duplicate fixture keys, or any duplicate/collision in resulting
IDs reject the candidate before publication.

The internal P10 stage declares its identity/version, exact authored-world
dependency, typed candidate output schema/version, and stable site/Location
output owner. Preserve the P9 base profile contract and its current
fingerprint/provenance as independently identifiable evidence. The combined
profile has an explicit compatibility/schema version including the P10 stage
contract and extends the published fingerprint/provenance with the P10 stage
identity/version, dependency edge, output owner, selected Ruin DefinitionId
and LocationId, fixture keys/values, and stable place/connection IDs. Do not
overwrite or relabel the P9 base fingerprint as if P9 produced local topology.
Bump the combined schema whenever key encoding, stage semantics/order, output
schema, or fingerprint inputs change; reject incompatible versions before
publication.

The fixed P9 DAG gains only this one built-in stage. Execution order follows
explicit dependency edges; no generic contributor scheduler, registration
ordering, list ordering, map iteration, host scheduling, or last-writer-wins
policy participates. The P10 stage owns only its typed candidate output.

This profile introduces no local generation randomness. Preserve/validate the
root-seed and algorithm provenance selected by P9 as part of genesis identity;
do not consume randomness or rerun genesis to create this Ruin. Any later
randomized local-generation scope requires a separate accepted checkpoint.

The combined profile provenance identifies the P9 base-profile
identity/fingerprint, combined P10 compatibility schema, effective
configuration or stable revision, effective calendar, authored
content/definition identities and versions, root seed/random algorithm, the
five P9 stages plus P10 stage identity/version, DAG edges and output owners,
and P10 fixture inputs/results. Asset names or preset labels alone are
insufficient when their values can change. P10 adds no randomness and preserves
P9 seed provenance without rerunning genesis.

## 5. Pre-start and existing-world mutation boundary

The selected Ruin and topology facts are initial World Truth composed from the
approved profile inputs. Authoring assets are immutable inputs, not
mutable-world storage. The site and topology
must exist in the complete selected world before the first actually simulated
boundary, whether or not a scene is loaded or an observer can see them.
Generated explanatory backstory is not simulated history.

At the first boundary, initial-generation privilege closes. Any later creation,
founding, topology change, expansion, or retrofit must use the normal domain
runtime mutation authority with its own declared semantic operation and
boundary. Loading, observing, enabling a contributor, or installing a future
mod must not rerun genesis or silently rewrite a prior world's local layout.
Explicit existing-world migration is separate, later work and is not part of
P10's first profile.

## 6. Reconstruction inventory

The later persistence/reconstruction contract for a profile using this slice
must recover either its complete authoritative initial facts or sufficient
compatible causal inputs to reproduce them, including:

- canonical Hex, `LocationId`, anchor references and any selected passage facts;
- the domain site identity/type, its owning authority and Location binding;
- every local place/connection identity, typed reference, containment relation,
  entry point, traversal fact, and topology owner needed by later decisions;
- selected profile, simulation/content/contributor compatibility versions,
  effective configuration and calendar, authored input values/revisions, and
  the P9 dependency graph/order and conflict outcomes;
- P9 root seed/random algorithm and the selected P9 provenance envelope; and
- the actual first simulated boundary. This profile includes no initial
  Knowledge or commitments.

Derived indexes, runtime IDs, display names, scene data, diagnostic snapshots,
or generated narrative cannot replace authoritative state or causal inputs.
P10 does not implement persistence, replay, or historical backstory forks.

## 7. Extensibility constraints and deferred platform work

Current architecture constraints apply to the design now: keep local generation
separate from Unity presentation where practical; use semantic inputs/outputs
and domain ownership; use hooks or deterministic contributor pipelines when a
real composition need exists; and preserve a seam through which a compatible
future code mod can add domain state or behavior without rewriting the local
ontology or requiring a base-game manager for each mechanic. Validate
coherence, references and atomicity for supported normal game operations.
This is not adversarial security against the player modifying their own game.

P19 remains the dedicated future phase for public extension API, loader,
packaging, module lifecycle, runtime installation, compatibility platform and
retrofit/migration mechanisms. P10 must not create those facilities, a mod
schema, speculative universal registry, or loader-shaped abstractions without
a selected consumer. A built-in composition root is sufficient for this
bounded profile while preserving compatible semantic seams.

## 8. Dependencies, readiness, and validation obligations

| Track | Current dependency/readiness consequence |
|---|---|
| P8-A/B/C/D/E | All are canonical. This profile requires P8-A Location/anchor truth and P8-C's `ExplorableSite` owner-kind contract. The stable-key resolver and LocationId-neutral LocalTopology seam remain unsatisfied. P8-B/D/E are not dependencies for this profile. |
| P9-A | Promoted at P9 canonical closure tip `96f2c1a`; its authored-bootstrap genesis foundation and atomic publication boundary are available. The State/Brief-only closure advance preserves that capability and content boundary. P10 adds one internal stage and typed output to the fixed DAG; no P9 output registry is presumed. |
| LocalTopology | Existing store is not yet a LocationId-neutral owner contract. The bounded semantic owner/store adapter in §2 must be implemented and promoted before P10 publishes these facts; legacy City/Site runtime paths remain compatible. |
| P18/P20 | Not required by this daily-only, single-site profile. P18-A/P20 become conditionally relevant only if a future explicitly accepted scope adds temporal activity or multi-participant facts. |
| Product/checkpoint gate | The user accepted the bounded Ruin profile in §1. Stable identities, compatibility schema, stage/output contract, adapter migration, and closure tests are specified here for independent review; no Phase 10 checkpoint ID is approved. |

### Proposed checkpoint identity and closure (not approved)

Proposed identity: **P10-A — Ruin LocalTopology Genesis Composition**.
This is a proposal only, not an assigned roadmap/checkpoint ID or
implementation authorization. Its scope is the one approved Ruin profile and
the narrow stable-owner/LocationId-neutral LocalTopology seam needed to
publish it through normal authorities. Dependencies are promoted P8-A
Location/anchor truth, the P8-C `ExplorableSite` owner-kind contract plus a
bounded stable-owner resolution seam, and P9-A's actual promoted authored
genesis pipeline/provenance/publication interfaces. P8-B/D/E, P18/P20 and P19
are excluded from the dependency set for this profile.

Proposed closure evidence: exactly one Ruin resolves to one existing canonical
`LocationId`; its finite semantic places, entry point(s), any needed
containment, and explicit local connections validate as a coherent topology;
stable IDs are injective, namespaced/versioned, and independent of runtime
allocation and source-list order; duplicate/colliding inputs fail before
publication; stable site ownership resolves through P8 anchor binding and
semantic site lookup; candidate facts publish atomically before the first
simulated boundary through normal domain authorities; P9 base fingerprint and
provenance remain intact while combined provenance includes P10 stage and
outputs; genesis is not rerun; and the exclusions in §1 remain absent. Existing
City/Site topology and anchor consumers remain compatible. Independent design
review, explicit checkpoint acceptance, implementation, required validation,
and promotion remain separate gates.

P10 scope is accepted for technical design only. Implementation remains
`WAIT_DEPENDENCY`: the bounded owner/store migration is unpromoted, this
checkpoint contract awaits independent review and explicit checkpoint
acceptance, and no implementation authorization exists. P9-A and the P8-C
owner-kind contract are promoted, but neither delivers the Ruin's stable
LocationId/topology binding. Identity/versioning, finite topology, fixed-DAG
stage integration, compatibility schema, and closure tests are specified here
for review. No additional content or gameplay choice is implied.

After a checkpoint and the relevant capabilities are accepted/promoted,
implementation validation must demonstrate:

1. the selected P9 authored-bootstrap inputs compose one Ruin through the same
   Location and local-topology authorities, without local content generation;
2. P8-C's existing City/Site anchor behavior remains valid while topology
   owners resolve through stable spatial identity, not runtime allocation;
3. equivalent compatible inputs produce equivalent semantic facts under
   reordered authored collections and different runtime allocation; fixed DAG
   order remains the declared dependency order;
4. the profile preserves P9's root-seed/random provenance without consuming
   local randomness or rerunning genesis;
5. missing site anchors, duplicate semantic keys, encoded-ID collisions,
   incompatible P10 schema, invalid containment/entry/connectivity, or
   unresolved cross-authority refs fail before publication;
6. stage failure or full-profile invariant failure leaves no partial live
   world, and post-start mutation requires ordinary domain authority; and
7. retained provenance/reconstruction inputs preserve P9 base evidence and
   identify P10 stage, dependency, output owner and every causal value without
   claiming persistence delivery; and
8. existing City/Site anchor and LocalTopology factory/consumer paths
   continue to resolve through their RuntimeId compatibility handles.

This proposal changes no executable code. It requires independent technical
review and `git diff --check`; Unity tests are not applicable to this document
alone. Implementation must later run relevant spatial/local-topology and
profile suites plus the repository's required regression gates after an
approved checkpoint.
