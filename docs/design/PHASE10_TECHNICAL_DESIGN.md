# Phase 10 Technical Design Proposal — UNAPPROVED

**Status:** bounded technical design proposal. This document assigns no
checkpoint IDs, does not approve the first content profile, and does not
authorize implementation. Phase 10 remains `WAIT_DEPENDENCY` for implementation.

**Independent technical-design review:** pending.

**Baseline:** Phase 8 canonical architecture baseline
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`; reviewed Phase 10 entry proposal
`b0158984c70c008cacdfe4390463b9250ad1f0ad`; reviewed Phase 9 entry proposal
`5c1e39957440aee767bfe16d56faa01bee1660aa`; reviewed Phase 9 technical design
`a89ada7692fd9950fc8424cadaacefaae14ccb80`. The canonical Phase 8 State at
this baseline records P8-A/B/C/D promoted. P9 proposals define a compatible
pipeline contract but do not constitute a promoted P9 implementation
capability.

## 1. Recommendation and bounded first profile

When its product scope and dependencies are approved, Phase 10 should add one
domain-owned local site to the normal authored single-player bootstrap profile,
anchored to an existing canonical P8 `LocationId`, with a finite local topology
of named semantic places and explicit connections. The same authorities and
invariants must accept authored and generated instances. This proposal does
not select the domain/site kind, topology catalog, layout algorithm, or exact
included stores; those remain explicit first-profile/checkpoint scope.

This recommendation is deliberately narrower than a general settlement,
interior, or world-expansion system. It includes only the site and local facts
required to prove the one selected consumer's initial-world composition. It
does not define a catalog of gameplay sites or imply that every P8 Location
has a local topology. The profile is daily-only and adds no initial activities,
route plans/Knowledge, or multi-participant facts. P18 and P20 are therefore
not blanket dependencies. If accepted profile scope later includes timed or
shared activity facts, add only the relevant promoted P18/P20 contracts and
reconstruction inputs.

This design specifies the integration boundary and implementation sequence,
not API/class names or durable semantic algorithms. IDs, merge policy,
compatibility rules, and the exact profile inventory must be fixed by the
approved checkpoint before implementation.

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
references are presently City/Site runtime-owner references, and its macro
location link is a legacy runtime reference; it does not provide the required
neutral `LocationId`-based composition contract. P8-C establishes City/Site
anchor bindings and Person positions, not a LocalTopology migration. The
implementation must bridge this explicitly: the selected domain site keeps
its owned domain identity and P8 `LocationId`, while topology ownership is
resolved through a stable semantic spatial owner/reference that can identify
that Location without promoting `RuntimeId` to semantic identity. The
integration must preserve existing City/Site consumers during migration or
provide a bounded adapter at the composition boundary; it must not silently
reinterpret legacy IDs or change unrelated P8 behavior.

The exact neutral-owner representation and compatibility/migration mechanics
are technical implementation choices within the accepted architecture. They
must be reviewed against current `LocalTopologyStore` consumers before code
work. Sharing a Hex establishes neither domain containment nor local
connectivity. A local connection must be an explicit topology fact; geographic
adjacency, same-Hex placement, rendering coordinates, or mere co-location do
not imply traversal or access.

## 3. Pipeline participation and candidate ownership

P10 is a bounded contributor to the dependency-aware P9 genesis pipeline (or
an explicitly compatible subpipeline preserving its full contracts). It is
not an independent generator authority. The selected built-in contributor is
registered through the host composition root; it declares stable stage and
contributor identity/version, typed input and output contracts, its owner
mapping, explicit dependencies, conflict behavior, provenance, and
purpose-scoped random needs.

The local stage consumes the already validated P8 location/anchor facts and
the selected domain site's authored or generated definition inputs. It
produces proposed domain-site and local-topology facts for isolated candidate
authorities owned by those domains. Outputs do not become live by mutating
`SimulationRuntime`, the current world, Unity assets, or authoring data.

Recommended composition sequence:

```text
resolve selected compatible profile and immutable authored/configuration inputs
→ validate P9 stage/contributor identities, versions, inputs/outputs and DAG
→ validate P8 Location/anchor dependencies and selected domain owner mapping
→ execute local contributor after its declared substrate/site dependencies
→ construct candidate site and local-topology facts in owner-scoped candidates
→ resolve all Location, site, place, containment, entry and connection references
→ validate local and complete selected-profile World Truth
→ publish the complete candidate composition once, before first simulated boundary
→ seal genesis; later site/topology changes require ordinary runtime authority
```

Validation of the stage graph, contributor compatibility, references and
complete profile occurs before publication. A failed contributor or invariant
discards the unpublished candidate. No observer or runtime consumer can see a
partially composed site/topology. The publication operation is a single
composition handoff/seal; it is not a sequence of live domain mutations.
Concrete candidate-store cloning/ownership and the atomic handoff mechanism
must align with the accepted P9 implementation boundary when that capability
exists. Do not expand `SimulationRuntime` into the local generator or make
runtime construction/founding inherit genesis privilege.

## 4. Stable identity, provenance, dependency order, and randomness

Every authoritative site, local place, connection, and other identity-bearing
initial fact needs stable semantic identity distinct from definition ID,
display name, Unity object identity, collection position, or `RuntimeId`.
Cross-references use those semantic identities and canonical `LocationId`;
runtime handles may be attached later for representation lookup only. Exact
identity derivation, namespace, collision policy, and compatibility version
are not selected here. They must be explicit in the accepted checkpoint and
must not depend on allocation or registration order.

The local contributor declares exact required upstream outputs and compatible
versions. Resolve the P9 DAG before executing any stage. Execute deterministic
topological order; among eligible independent stages use stable semantic
stage/contributor identities as a tie-break. Disjoint output ownership is
preferred. Any overlap must use an explicitly named deterministic merge or
conflict policy. Never let source list order, registration timing, hash-map
iteration, host scheduling, runtime identity allocation, or implicit
last-writer-wins decide the result.

Randomness, if the selected local generation algorithm needs it, is scoped to
named local-generation purposes and derived from stable causal inputs: root
seed, compatible profile/version, contributor/stage IDs and versions, and
declared upstream semantic input identities. Independent purposes must not
share a consumption-sensitive sequential stream. Adding an unrelated stage or
changing its random draw count must not shift this local result; a declared
causal input change may. The purpose keys and derivation/version provenance
are part of compatibility. This proposal intentionally chooses no hash,
PRNG, ID, or conflict-resolution algorithm.

The published causal provenance for the selected profile must identify its
simulation/profile compatibility, effective configuration values or stable
revision, effective calendar, included content/definition identities and
versions, root seed/random algorithm, selected stages/contributors and
versions, declared dependency/order facts, and the causal inputs for each
local contribution. Asset names or preset labels alone are insufficient when
their values can change.

## 5. Pre-start and existing-world mutation boundary

Local authored and generated facts are both initial World Truth. Authoring
assets are immutable inputs, not mutable-world storage. The site and topology
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
- root seed/random algorithm plus each named purpose and causal input used by
  local generation; and
- the actual first simulated boundary and any included initial Knowledge or
  commitments that alter subsequent behavior.

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
| P8-A/B/C/D | Canonical promoted contracts are available. This profile requires P8-A Location/anchor truth and the P8-C-relevant site anchor contract; P8-B is conditional on chosen passage facts. P8-D route Knowledge/planning and P8-E civil travel are not blanket P10 dependencies. |
| P9 | Reviewed entry and technical proposals provide the intended pipeline contract, but P9 implementation/capability is not promoted. P10 may design against the contract; actual generator composition must wait for the relevant promoted P9 capability. |
| LocalTopology | Existing store is not yet a LocationId-neutral owner contract. The bounded migration/adapter and its consuming integrations must be implemented and promoted before P10 can publish these facts. |
| P18/P20 | Not required by the recommended daily-only, single-site profile. Add dependencies only if the approved scope actually initializes temporal activity or multi-participant facts. |
| Product/checkpoint gate | First site kind, included authorities, exact topology content/semantics, and compatibility/identity algorithms require explicit checkpoint acceptance. No Phase 10 checkpoint IDs currently exist. |

P10 remains `WAIT_DEPENDENCY` for implementation. This proposal is design
evidence only and does not satisfy the product/checkpoint gate, P9 promotion,
or LocalTopology migration. It preserves the entry proposal's one-site
recommendation while keeping implementation gated until the first content
profile is explicitly accepted. There is no additional unresolved semantic
choice asserted by this document.

After a checkpoint and the relevant capabilities are accepted/promoted,
implementation validation must demonstrate:

1. authored and generated inputs compose through the same Location and local
   topology authorities;
2. P8-C's existing City/Site anchor behavior remains valid while topology
   owners resolve through stable spatial identity, not runtime allocation;
3. equivalent compatible inputs produce equivalent semantic facts under
   reordered authored collections and stage registration;
4. changing unrelated contributor random consumption does not shift local
   outcomes, while declared causal input changes may;
5. missing dependencies, incompatible versions, cycles, ambiguous writes,
   invalid containment/entry/connectivity, or unresolved cross-authority refs
   fail before publication;
6. stage failure or full-profile invariant failure leaves no partial live
   world, and post-start mutation requires ordinary domain authority; and
7. retained provenance/reconstruction inputs identify every causal value
   required by the selected profile without claiming persistence delivery.

This proposal changes no executable code. It requires independent technical
review and `git diff --check`; Unity tests are not applicable to this document
alone. Implementation must later run relevant spatial/local-topology and
profile suites plus the repository's required regression gates after an
approved checkpoint.
