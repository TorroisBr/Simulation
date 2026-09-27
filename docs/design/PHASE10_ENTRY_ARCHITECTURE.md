# Phase 10 Entry Architecture Proposal — UNAPPROVED

**Status:** bounded entry recommendation; independent review passed at content
commit `027006843327d226e00faf2de05422ca622bf2f4`. This document does not approve
a checkpoint, assign checkpoint IDs, authorize implementation, or expand the
promoted P9-A capability. The P8-E promotion impact update passed
independent review at `b71505c`; the status does not approve a checkpoint or
authorize implementation. The baseline-only refresh at `329ce823` was
independently revalidated against Phase 8 canonical `470667d`; it preserves the
earlier semantic PASS. This refresh records the later P9-A promotion and
current alignment constraints. The user has accepted one bounded first profile
for design; this refresh does not substitute for independent review or
authorize implementation/checkpoint identity.

**Historical baseline:** architecture, Roadmap, Execution Model and Phase 8 State at
`c285466c355103d3637ac165246591b72eb7bda0`; reviewed P9 entry proposal
`5c1e39957440aee767bfe16d56faa01bee1660aa`; reviewed P9 technical-design
proposal `a89ada7692fd9950fc8424cadaacefaae14ccb80`. Since that design base,
P8-D was promoted to canonical at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`.

**Current revalidation baseline:** P9 canonical `codex/phase9/canonical` at
`96f2c1aaf742f313bbb9643e5f5b3d844c402c78`, with current architecture baseline
`c285466c355103d3637ac165246591b72eb7bda0`, the canonical Phase 8 State, and
the intraday/extensibility and multi-participant alignment records referenced
there. The P9 advance from `988b6f5d14e12359e93464bae5e0048ca970ad86` to
`96f2c1aaf742f313bbb9643e5f5b3d844c402c78` records Phase 9 State/Brief closure
only; it changes no P9-A capability, APIs, or content boundary. P8-A through
P8-E and P9-A are promoted. P9-A delivers the authored
Unity-bootstrap profile and a dependency-aware pre-start pipeline; the
accepted Ruin profile consumes that genesis/provenance and adds no generated
content. Any P10 composition must preserve and validate the actual promoted
P9 APIs. P8-C's City/Site anchor bindings include the `ExplorableSite` owner
kind; however, composed-runtime resolution checks `TryBindSite`'s stable-key
value via `ExplorableSiteStore.TryGetByRuntimeId`. `LocalTopologyOwnerReference`
also stores site and macro-location RuntimeIds. The stable Ruin identity and
LocationId-neutral topology seam therefore remain an actual bounded
implementation dependency. The
proposed profile consumes no P8-E route-plan or civil-travel behavior, so P8-E
adds no dependency. P8-B passages are conditional on accepted profile content.

The architecture alignment records preserve deterministic, dependency-aware
extensible generation, declared contributor inputs/outputs and causal
provenance, and separate new-world composition from retrofit. P19's public
loader/API remains deferred. P18/P20 are conditional only if the selected
initial profile actually includes temporal or multi-participant facts.

## 1. Purpose and consumer boundary

The accepted first profile composes exactly one `ExplorableSiteKind.Ruin` at
an existing canonical P8 `LocationId` as part of the complete initial World
Truth established through P9. It consumes P9's authored-bootstrap selection
and provenance; it does not rerun genesis or add generated content. The Ruin
remains its own domain-owned fact; local structure uses `LocalTopology` /
`SubLocation` semantics beneath that Location. Hex, Location, and sublocation
are distinct levels in the existing ontology. Sharing a Hex creates neither
containment nor connectivity.

The finite LocalTopology is limited to semantic places, one or more entry
points, containment only if needed, and explicit local connections. Entrance →
Courtyard → Inner Chamber may be a tiny deterministic proof fixture. Scope
excludes City, Market, population/NPC/economy, Passage/Route, Knowledge,
activities, loot, encounters, construction, and all other gameplay. This does
not imply a product catalog or gameplay mechanic.

The Ruin and topology facts must enter the normal semantic authorities and
pass their invariants. There is no parallel local-location model. Rendering
coordinates, scene presence, loaded Unity objects, or observation do not create
or define these facts.

## 2. Spatial contract and P8 boundary

Use the canonical P8-A Hex/Location identities, anchor references, geography
and scale provenance when the selected profile includes those facts. Use the
canonical P8-C City/Site anchor bindings where its existing owner kinds apply.
P8-C also delivers Person-level `At`/`InTransit` positions; those positions
remain separate from `NpcRuntime` fields. P8-B passage truth is included only
if the selected local profile actually creates passage facts. P8-D route
Knowledge/plan behavior is not a blanket dependency; a profile that uses it
must bind to the now-promoted P8-D authority and APIs. P8-E is also canonical
but optional for the proposed local-topology profile, which creates no route or
civil-travel facts.

The existing `LocalTopologyStore` is a reusable foundation, not proof that
P8-C delivered a `LocationId`-based local-world composition contract. Its
current owner references are City/Site and its macro-location link is a legacy
runtime reference. Architecture §69 directs that LocalTopology accept a
neutral spatial owner when needed. P10 design must therefore specify how the
chosen profile binds local topology to the same canonical Location identity,
without making `Location` a universal `WorldEntity`, promoting runtime IDs to
semantic IDs, or claiming P8-C already migrated LocalTopology. This is an
explicit technical-design/integration seam; do not silently bridge it with
rendering or legacy identity.

## 3. Pre-start lifecycle and P9 contribution ordering

P10 consumes P9's promoted authored-bootstrap selection and genesis
provenance; it does not rerun genesis, add a local generation algorithm, or
produce generated content. Its bounded composition contribution must preserve
P9's stage identity/version, dependency provenance and publication contract,
and compose exactly one Ruin plus finite topology facts from declared profile
inputs. Stable semantic identities and typed references must be independent
of runtime allocation and collection order.

The recommended lifecycle is:

```text
resolve the selected P9 authored-bootstrap profile and immutable inputs
→ validate/preserve P9 dependency and provenance records
→ build one Ruin and finite topology candidate against unpublished authorities
→ resolve Location, site, topology and cross-domain references
→ validate the complete selected initial World Truth
→ publish one complete composition before the first simulated boundary
→ close genesis privilege; later changes use ordinary runtime authorities
```

Failed composition or whole-profile validation must not expose partially
published local state. The composition proposes facts to the authorities that
own them; it is not an independent mutation authority. Runtime construction/founding, automatic
world expansion, lazy existence from observation, and post-start changes under
genesis privilege are out of scope. Installing a future contributor does not
rerun initial stages or rewrite an existing world's historical layout;
explicit retrofit/migration is separate later work.

## 4. Identity, provenance, and reconstruction obligations

The Ruin instance, places and connections that participate in authoritative
local truth need stable semantic identities distinct from definition IDs,
display name, Unity object, or transient runtime ID. The exact ID derivation,
collision handling and version strategy are technical-design decisions; this
entry proposal does not invent them. Local topology identity and references
must remain resolvable through the canonical Location anchor and the site that
owns the relevant domain semantics.

The initial result must be reconstructible under its declared compatible
profile. The later implementation/persistence contracts must retain either
the complete authoritative initial facts or sufficient compatible inputs to
reproduce them, including:

- P9 selected profile and genesis/provenance identity, plus Hex, Location and
  anchor identities/topology, local place/connection
  identities, containment, entry points and authored traversal facts that
  affect later decisions;
- owning site/domain identities and all cross-authority references;
- selected profile, simulation/content/contributor versions, effective
  configuration and calendar, authored inputs and stage dependency/order
  provenance;
- P9 root seed/random algorithm provenance (this profile adds no random
  contribution); and
- the first simulated boundary. This profile includes no initial Knowledge or
  commitments.

Generated explanatory backstory is not simulated history. Diagnostics,
derived indexes and display names do not replace authoritative facts or causal
inputs. Do not claim persistence/replay implementation in P10.

## 5. Extensibility now and later

Current design constraints include Unity-independent domain/application seams
where practical, semantic hooks instead of polling, stable contributor/stage
identity, declared dependencies and deterministic composition where independent
contributors have a real need. A future code mod must be incorporable as an
official expansion without rewriting the local-world ontology. These are
review constraints for P10's contracts now.

P19's public extension API, loader, packaging, module lifecycle, runtime
installation, compatibility platform and retrofit tooling remain deferred.
P10 may compose the selected built-in contributors through the host composition
root and preserve a compatible seam; it must not invent a P19 loader, mod
schema, universal registry, speculative hooks, or adversarial-player security
boundary. Domain validation protects coherent supported operations, not the
player from their own modifications.

## 6. Dependencies, readiness and approval gates

| Dependency or gate | Phase 10 effect |
|---|---|
| P8-A canonical geography and Location identity | Available for relevant spatial facts. |
| P8-C canonical City/Site anchors and Person positions | `ExplorableSite` owner kind exists, but stable-key resolution is checked against site RuntimeId and LocalTopology remains runtime-ID based; the narrow Ruin identity/LocationId seam is still needed. |
| P8-B canonical passage facts | Excluded from this profile. |
| P8-D canonical route Knowledge/plan capability | Available if the selected profile consumes it; no blanket dependency. |
| P8-E canonical civil travel | No blanket P10 dependency; the proposed local-topology profile creates no route-plan or travel facts. |
| P9-A authored-bootstrap genesis | Promoted at P9 canonical closure tip `96f2c1a`; its pre-start pipeline and authored profile are available. The State/Brief-only closure advance preserves that capability and content boundary. It does not itself compose P10 local-site outputs. |
| P10 pipeline consumer integration | Design must revalidate its contributor/stage contract against the promoted P9 API and demonstrate candidate ownership, validation and complete-world publication for the accepted profile before integration. No new P9 promotion is implied by this entry refresh. |
| P10 approval gates | User-accepted first-profile scope is exactly one Ruin and the finite topology in §1. Independent review of refreshed entry/design, explicit checkpoint acceptance, required promoted P8/P9 authorities, and the LocalTopology ownership seam remain before implementation. |

P10 scope is accepted for design only; implementation remains
`WAIT_DEPENDENCY`. The P9-A genesis foundation and P8-C owner-kind contract are
promoted, but the bounded stable Ruin/LocationId topology seam is not delivered.
No checkpoint ID is assigned, and this entry proposal does not authorize
implementation or capability promotion. No gameplay or unbounded content
catalog is inferred.

P18 is not a blanket local-genesis dependency; a selected initial timed
activity consumes relevant promoted temporal contracts. P20 is conditional on
actual multi-participant activity/participation facts. Neither Sleep, Dreams,
robbery/gangs, rituals, War gameplay, nor MegaEventos is part of this proposal.

## 7. Implementation validation obligations

After an approved checkpoint and relevant capability promotion, candidate
validation should prove that:

1. the selected P9 authored-bootstrap inputs compose one Ruin through the same
   Location/LocalTopology ontology and domain authorities, without local
   content generation;
2. stable identities and references do not depend on runtime allocation or
   source collection order;
3. P9 dependency/provenance inputs remain intact and composition is
   deterministic under compatible profile inputs;
4. missing dependencies, invalid local ownership/topology and complete-profile
   invariant failures stop before publication;
5. a failed stage cannot expose partial initial state, while post-start changes
   require normal runtime authority; and
6. provenance contains the profile/content/contributor and causal random inputs
   needed by the chosen reconstruction guarantee.

This documentation proposal requires independent review and `git diff --check`.
It changes no executable code; Unity validation is not applicable at this
entry stage.
