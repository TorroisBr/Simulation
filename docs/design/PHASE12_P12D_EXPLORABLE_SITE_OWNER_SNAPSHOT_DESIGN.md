# P12-D ExplorableSite Owner Snapshot and Staging Proposal

**Status:** Scoped technical-contract proposal only. It is not implementation-ready, does not extend the existing P12-D implementation authorization, and requires an independent exact-content review before any Site code work.

**Proposal base:** P12 canonical `da2a73896bc405ae6f11c536a5fbe8d471b00c21`.

**Existing design context:** `PHASE12_D_TECHNICAL_DESIGN.md` defines the Site facts, ordering, dependency order, rejection cases, and exclusions. `PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md` passes that overall design but authorizes implementation only for the isolated GenealogyStore slice. This proposal narrows the existing Site row into a reviewable owner contract; it does not supersede that verdict.

**Source basis at proposal base:** `ExplorableSiteRuntime.cs` requires non-empty site/runtime/definition/location IDs and rejects equality between site and location RuntimeIds. `ExplorableSiteStore.cs` owns ordered Sites, a local RuntimeId dictionary, owner Revision, and a location scan; Add is guarded and increments after publication. `Data/ExplorableSiteData.cs` is authoring data, so snapshots retain DefinitionId rather than an asset object. `RuntimeIdentity.cs` registers sites in a separate runtime identity index, and `RuntimeIdentityCensus.cs` witnesses that index independently of `ExplorableSiteCensusProvider.cs`. `P12RuntimeIdentitySpatialCensus.cs` assigns both Daily-v1 site sections ExplicitlyEmpty.

## 1. Scope and authority

Propose one generic, immutable schema-v1 snapshot and private staged reconstruction for the existing ExplorableSiteStore owner. The owner snapshot covers only the facts currently represented by ExplorableSiteRuntime:

- RuntimeId
- SiteInstanceId
- DefinitionId
- exact legacy SpatialLocationRuntime.RuntimeId reference
- position in ExplorableSiteStore order
- exact ExplorableSiteStore.Revision

The current runtime exposes no mutable site-progress field. The snapshot adds none. It stores identity strings and values, not live site, location, or authoring-definition objects. It does not serialize derived lookup structures.

The owner slice is limited to the site snapshot value type, owner-local capture and factory seams, and focused tests. A later D composition owns P12-B token binding, staged RuntimeIdentityRegistry integration, cross-owner validation, and publication. This owner factory does not capture or validate the P12-B token itself.

## 2. Snapshot and capture contract

Define an immutable schema-v1 value with one exact owner revision and an ordered, detached sequence of site rows. Each row contains RuntimeId, SiteInstanceId, DefinitionId, and LegacyLocationRuntimeId. Copy input rows and expose only immutable/read-only values so neither mutation of the source store nor mutation of an input list can change a captured snapshot.

Capture enumerates the owner's ordered Sites list once. With the owner unchanged and the same valid outer capture boundary, repeated captures have the same revision, row values, and order. Capture preserves exact string values and never derives or replaces an ID. It exports no Definition object, SpatialLocationRuntime reference, site progress, actor observation, P8 anchor, P8 LocationId, P10 LocalTopology value, or site-by-location result.

Site order is semantic: GetForLocationRuntimeId currently scans the ordered owner list and returns matches in that order. Multiple distinct site RuntimeIds may refer to the same legacy location and must retain their relative owner order.

Revision is copied and restored exactly; it is not recomputed by replaying Add. Existing Add increments Revision after the owner collections are published, and rejection leaves it unchanged. The P10 genesis rollback entry point also adjusts the store's revision; this proposal does not add or serialize P10 genesis behavior. The exact schema-v1 revision validity rule beyond preserving the captured nonnegative value is listed as an open review question below.

## 3. Private staged reconstruction

The proposed factory accepts a schema-v1 snapshot plus the exact staged legacy-location objects and compatible site-definition objects supplied by the enclosing D composition. It creates a new unpublished ExplorableSiteStore and constructs site rows only after all fields validate.

For every row, the factory requires non-empty exact RuntimeId, SiteInstanceId, DefinitionId, and LegacyLocationRuntimeId values; a RuntimeId unique among the site rows in this snapshot; a resolvable compatible definition identity; and an existing legacy location with the referenced RuntimeId. The constructed site must point to the exact staged location object resolved for that ID and to the selected compatible definition object. Missing, ambiguous, or incompatible definition resolution fails closed. No location or definition is synthesized.

The factory preserves every row's identity, definition identity, location binding, and order. It permits multiple sites on one location. It rebuilds the ordered owner list, the owner-local RuntimeId dictionary, and the read-only list view from those primary rows. GetForLocationRuntimeId remains a scan over the ordered list; no additional location index is added.

The staged owner receives the snapshot revision directly after validation rather than calling Add. Failure returns no staged owner and leaves the source store, staged locations, definition catalog, and any active runtime unchanged. The owner factory does not register sites in RuntimeIdentityRegistry and does not mutate any registry revision.

## 4. Owner and identity-registry validation

Owner-local validation rejects malformed rows, duplicate site RuntimeIds, unresolved location references, and incompatible definitions. The existing ExplorableSiteRuntime constructor also requires the site RuntimeId to differ from its location RuntimeId; preserve that invariant. Multiple sites per location are valid. Shared DefinitionIds are valid when they resolve to the same admitted compatible definition.

SiteInstanceId is preserved exactly and must be non-empty. The existing owner store and reviewed D design do not state whether SiteInstanceId must be unique across sites. Do not silently add that constraint in an implementation; settle it in the exact-content review.

RuntimeIdentityRegistry is a separate P12-C-owned registry with its own explorableSitesByRuntimeId index and cross-type RuntimeId collision checks. The later merged D stage must ensure each staged site RuntimeId is unclaimed across the applicable runtime identity domains, add each exact staged ExplorableSiteRuntime object once to the staged registry, and verify that registry lookups resolve to those same objects with matching cardinality. A collision or owner/registry disagreement rejects the unpublished combined stage.

The isolated ExplorableSiteStore factory only rebuilds its own indexes. It neither reaches into RuntimeIdentityRegistry nor publishes a partially populated registry. The exact handoff by which the merged D stage extends or validates the staged P12-C registry is an integration interface that needs review before that later work.

## 5. Daily-v1 admission remains exact-empty

For the accepted UnityBootstrap-Daily-v1 profile, the P12-B section `p12d.explorable-sites` remains required-empty, schema version 1, and bound to the exact installed ExplorableSiteStore and completed-boundary token. Its populated cardinality must be rejected at admission and cannot be accepted by a generic populated Site snapshot.

The separate P12-C RuntimeIdentityRegistry section `p12c.runtime-identities.explorable-sites` also remains ExplicitlyEmpty with zero cardinality for Daily-v1. Both checks stay in place: an empty site owner does not waive the registry's empty requirement, and the generic owner capability does not widen profile admission.

A future profile may use the generic site snapshot only after its own explicit profile inventory and admission contract requires and admits populated site-owner and identity-registry sections. This proposal changes no profile, P12-B token wiring, runtime/bootstrap composition, P12-A, or P13 behavior.

## 6. Focused behavior tests for a later authorized implementation

Add owner-focused tests for:

- Empty capture and repeated capture, preserving exact empty revision and detached empty rows.
- Populated capture with several sites, including multiple sites sharing one location, preserving owner order and all exact IDs.
- Detached capture: later source additions and mutations of supplied row collections do not change the captured value.
- Exact staged round trip of RuntimeId, SiteInstanceId, DefinitionId, location ID, order, and revision; verify the staged site holds the exact supplied definition and legacy-location objects.
- Duplicate RuntimeId, null or malformed rows, blank required IDs, incompatible or unresolved definitions, and absent location references fail before returning a staged owner.
- Multiple sites at one location are accepted and GetForLocationRuntimeId returns them in captured owner order.
- A failed stage leaves the source owner, source registry, active owner references, and supplied staged roots unchanged and exposes no partial result.

Keep registry collision and exact-object concordance tests at the later merged D integration seam, where RuntimeIdentityRegistry is staged. Those tests must cover duplicate site IDs and collisions with existing runtime-ID kinds, including location/route/site cases, plus rejection without publishing the combined stage.

Retain the selected-profile admission tests proving that populated `p12d.explorable-sites` and populated `p12c.runtime-identities.explorable-sites` remain rejected for Daily-v1. Existing census tests are passive owner/cardinality evidence and are not a substitute for export or hydration tests.

## 7. File and hotspot boundaries

An eventual isolated owner implementation may touch:

- `Assets/_Project/Scripts/ExplorableSiteStore.cs` for detached capture and a private exact-value factory.
- A new immutable owner snapshot value file under `Assets/_Project/Scripts/`, plus its Unity `.meta` file.
- A new focused `Assets/_Project/Tests/EditMode/Editor/ExplorableSiteSnapshotTests.cs` and its `.meta` file.

Do not include shared `RuntimeIdentity.cs`, `P12RuntimeIdentitySpatialCensus.cs`, `SimulationRuntime.cs`, `TesteSimulacao.cs`, bootstrap/admission composition, City/NPC owners, or unrelated `.meta`/ProjectSettings changes in this isolated slice. Their integration is serialized follow-up work: first agree the Site-to-registry handoff, then wire exact P12-B capture evidence and outer D validation at the reviewed shared seam.

## 8. Explicit exclusions and review gaps

This proposal excludes P10/P8 authority synthesis, P8-C City/Site-to-LocationId anchors, P10 LocalTopology, P12-B token wiring, bootstrap/runtime integration, P12-A, profile-wide D, P13, whole-D graph validation, continuation parity, and phase closure. It does not claim that an owner snapshot or local staged factory makes the selected Daily-v1 profile capable of storing sites.

Before any Site implementation is authorized, exact-content review should settle:

1. Whether SiteInstanceId is unique within an ExplorableSiteStore or only required to be non-empty and preserved.
2. The precise valid revision domain for schema v1, including how the current owner's Add and internal genesis rollback behavior constrain values, without adding P10 semantics to the restore contract.
3. The exact merged-stage seam that adds or cross-checks the staged site objects in the P12-C RuntimeIdentityRegistry while preserving owner-local isolation and all-or-none publication.
4. Whether the admitted definition resolver can prove a unique compatible DefinitionId without introducing definition versioning or new content identity rules.

Until those points are reviewed, this is a bounded proposal only. The existing exact-tip D design review still grants implementation readiness only to GenealogyStore.
