# Phase 10 Entry Architecture Proposal — UNAPPROVED

**Status:** bounded entry recommendation; independent review passed at content
commit `027006843327d226e00faf2de05422ca622bf2f4`. This document does not approve
a checkpoint, assign checkpoint IDs, authorize implementation, or expand the
promoted P9-A capability. The P8-E promotion impact update passed
independent review at `b71505c`; the status does not approve a checkpoint or
authorize implementation. The baseline-only refresh at `329ce823` was
independently revalidated against Phase 8 canonical `470667d`; it preserves the
earlier semantic PASS. This refresh records the later P9-A promotion and
current alignment constraints; it does not substitute for independent review
of the refreshed baseline or resolve P10's product/checkpoint gate.

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
Unity-bootstrap profile and a dependency-aware pre-start pipeline; it does not
deliver generated local sites, P8 spatial outputs in that profile, or local
topology composition. Thus the P9 genesis/pipeline foundation edge is satisfied
for design, while any P10 contributor integration must prove it against the
actual promoted P9 APIs and remain separately scoped/validated. P8-C's promoted
City/Site anchor bindings are available within their delivered owner contract;
they do not migrate `LocalTopologyStore` to a neutral `LocationId` owner. That
local-topology ownership seam remains an implementation dependency. The
proposed profile consumes no P8-E route-plan or civil-travel behavior, so P8-E
adds no dependency. P8-B passages are conditional on accepted profile content.

The architecture alignment records preserve deterministic, dependency-aware
extensible generation, declared contributor inputs/outputs and causal
provenance, and separate new-world composition from retrofit. P19's public
loader/API remains deferred. P18/P20 are conditional only if the selected
initial profile actually includes temporal or multi-participant facts.

## 1. Purpose and consumer boundary

Phase 10 should let a selected pre-start profile compose authored or generated
local sites and their local sublocations/topology as part of the same complete
initial World Truth established by Phase 9. A domain site remains its own
domain-owned fact and points to a stable spatial `LocationId`; local structure
uses `LocalTopology` / `SubLocation` semantics beneath that Location. Hex,
Location, and sublocation are distinct levels in the existing ontology. Sharing
a Hex creates neither containment nor connectivity.

This proposal recommends a first bounded consumer profile containing one
domain-owned local site attached to a canonical P8 Location, with a finite
LocalTopology of places and connections, all composed before the first
simulated boundary. The exact site kind, topology content and required domain
stores remain to be named and approved in a P10 checkpoint contract. This
recommendation does not imply a product catalog or gameplay mechanic.

Authored and generated local facts must enter the same semantic authorities
and pass the same invariants. There is no generator-only spatial truth and no
parallel local-location model. Rendering coordinates, scene presence, loaded
Unity objects, or observation do not create or define these facts.

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

P10 is a contributor to P9's dependency-aware initial-generation pipeline (or
an explicitly compatible subpipeline preserving the same contract). A local
stage declares stable semantic stage/contributor identity and compatible
version, typed inputs/outputs, owning authorities, explicit dependencies,
conflict behavior, provenance, and purpose-scoped random context. It consumes
only declared upstream results; eligible contributions have deterministic
ordering and deterministic conflict resolution. Registration order, runtime
ID allocation, collection iteration, host scheduling and a shared sequential
random stream do not determine output.

The recommended lifecycle is:

```text
resolve compatible profile and inputs
→ validate the dependency graph and contributor contracts
→ build candidate local facts against unpublished domain authorities
→ resolve Location, site, topology and cross-domain references
→ validate the complete selected initial World Truth
→ publish one complete composition before the first simulated boundary
→ close genesis privilege; later changes use ordinary runtime authorities
```

Failed stage or whole-profile validation must not expose partially published
local state. Generation proposes facts to the authorities that own them; it is
not an independent mutation authority. Runtime construction/founding, automatic
world expansion, lazy existence from observation, and post-start changes under
genesis privilege are out of scope. Installing a future contributor does not
rerun initial stages or rewrite an existing world's historical layout;
explicit retrofit/migration is separate later work.

## 4. Identity, provenance, and reconstruction obligations

Every generated or authored instance that participates in authoritative local
truth needs a stable semantic identity distinct from its definition ID,
display name, Unity object, or transient runtime ID. The exact ID derivation,
collision handling and version strategy are technical-design decisions; this
entry proposal does not invent them. Local topology identity and references
must remain resolvable through the canonical Location anchor and the site that
owns the relevant domain semantics.

The initial result must be reconstructible under its declared compatible
profile. The later implementation/persistence contracts must retain either
the complete authoritative initial facts or sufficient compatible inputs to
reproduce them, including:

- Hex, Location and anchor identities/topology, plus local place/connection
  identities, containment, entry points and authored traversal facts that
  affect later decisions;
- owning site/domain identities and all cross-authority references;
- selected profile, simulation/content/contributor versions, effective
  configuration and calendar, authored inputs and stage dependency/order
  provenance;
- root seed and purpose-scoped random derivation inputs for every generated
  contribution; and
- the first simulated boundary and any included initial knowledge or
  commitments that alter subsequent behavior.

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
| P8-C canonical City/Site anchors and Person positions | Satisfied for the delivered City/Site anchor binding and Person-position contracts; does not include LocalTopology migration or a neutral LocationId topology owner. |
| P8-B canonical passage facts | Conditional on local profile including passages. |
| P8-D canonical route Knowledge/plan capability | Available if the selected profile consumes it; no blanket dependency. |
| P8-E canonical civil travel | No blanket P10 dependency; the proposed local-topology profile creates no route-plan or travel facts. |
| P9-A authored-bootstrap genesis | Promoted at P9 canonical closure tip `96f2c1a`; its pre-start pipeline and authored profile are available. The State/Brief-only closure advance preserves that capability and content boundary. It does not itself compose P10 local-site outputs. |
| P10 pipeline consumer integration | Design must revalidate its contributor/stage contract against the promoted P9 API and demonstrate candidate ownership, validation and complete-world publication for the accepted profile before integration. No new P9 promotion is implied by this entry refresh. |
| P10 approval gates | Independent review of refreshed entry; explicit acceptance of the first profile/domain inventory and site/topology semantics; technical design and independent review; explicit checkpoint IDs and implementation authorization; relevant promoted P8/P9 authorities and LocalTopology ownership seam. |

Phase 10 remains `WAIT_DEPENDENCY` for implementation. This proposal is design
work only. The P9-A genesis foundation and P8-C anchor capability are now
promoted, but the P10 profile is not accepted and the neutral LocalTopology
composition seam is not delivered. The bounded single-site recommendation
remains a candidate: accepting it requires choosing the site kind, participating
domain authorities and exact local topology/content semantics. Deferring or
changing that scope leaves the corresponding consumer contract and dependencies
to be revised before checkpoint approval. Do not infer gameplay or an unbounded
content catalog. No checkpoint IDs or capability promotions are inferred here.

P18 is not a blanket local-genesis dependency; a selected initial timed
activity consumes relevant promoted temporal contracts. P20 is conditional on
actual multi-participant activity/participation facts. Neither Sleep, Dreams,
robbery/gangs, rituals, War gameplay, nor MegaEventos is part of this proposal.

## 7. Implementation validation obligations

After an approved checkpoint and relevant capability promotion, candidate
validation should prove that:

1. authored and generated local facts use the same Location/LocalTopology
   ontology and domain authorities;
2. stable identities and references do not depend on runtime allocation or
   source collection order;
3. dependency ordering, contribution conflicts and purpose-scoped randomness
   are deterministic under compatible inputs;
4. missing dependencies, invalid local ownership/topology and complete-profile
   invariant failures stop before publication;
5. a failed stage cannot expose partial initial state, while post-start changes
   require normal runtime authority; and
6. provenance contains the profile/content/contributor and causal random inputs
   needed by the chosen reconstruction guarantee.

This documentation proposal requires independent review and `git diff --check`.
It changes no executable code; Unity validation is not applicable at this
entry stage.
