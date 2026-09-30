# P12-B per-NPC SpatialKnowledge census design

**Status:** Bounded P12-B census design independently reviewed PASS at
candidate `cfcc2fe1116d7f557ff958434a1218caedd15607` against canonical base
`81ddfe4bd0620b1b61a0a52aa074ef2ed57c2833`. The accepted P12-B prerequisite
authorization covers this census-only implementation. This design adds no
P12-F export/hydration capability and grants no canonical promotion.

**Evidence base:** P12 canonical `81ddfe4`; selected profile source basis
`ec75e6a`; current owner/write-path details in
`PHASE12_B_BLOCKER_RESOLUTION.md` and
`PHASE12_P12B_OPERATION_FOOTPRINT_AUDIT.md`.

## Gap and bounded outcome

The selected `UnityBootstrap-Daily-v1` runtime contains ten NPC-owned
`SpatialKnowledgeRuntime` objects. Each has exact location and route knowledge
cardinality and one shared local revision. The selected day-zero profile reads
20 known locations and 10 known routes total, with revision 3 on each owner.
No P12 census provider exposes these installed owners.

Two source issues prevent these local revisions from serving as reliable B
evidence:

1. `KnownLocationRuntimeIds` and `KnownRouteRuntimeIds` return their mutable
   backing lists through `IReadOnlyList<string>`; callers can cast and change
   membership without advancing `Revision`.
2. `DiscoverId` currently appends a new ID when `Revision == long.MaxValue`
   but cannot increment the revision. This changes cardinality without a new
   stamp. `RuntimeIdentityRegistry` already rejects a saturated revision
   before owner mutation; use that monotone-witness rule here.

The proposed P12-B slice closes only these evidence holes and adds passive
owner providers. It does not serialize Knowledge, alter discovery/action
semantics below revision exhaustion, create an epoch callback, or register the
sections in the incomplete P12-B runtime protocol.

## Owner sections

Create two schema-v1 providers for each installed NPC owner, with a stable
section ID derived from the NPC `RuntimeId`:

| Section pattern | Exact value |
|---|---|
| `p12f.spatial-knowledge.locations/{npcRuntimeId}` | `KnownLocationRuntimeIds.Count` |
| `p12f.spatial-knowledge.routes/{npcRuntimeId}` | `KnownRouteRuntimeIds.Count` |

Each provider uses the exact installed `SpatialKnowledgeRuntime` object as
`OwnerInstanceIdentity` and reports its shared `Revision`. The composition
collection is built from published `SimulationRuntime.NpcRuntimes` sorted by
ordinal `RuntimeId`, making owner registration deterministic and distinguishing
each actor's Knowledge from aggregate counts. A repeated discovery changes no
cardinality or revision. A successful new location or route discovery changes
one cardinality and the common revision, so both section witnesses for that
NPC receive a new revision.

The selected authored profile must report ten distinct location owners and
ten distinct route owners, totaling 20 locations/10 routes; each owner reports
2/1 respectively at revision 3. This is a live published runtime assertion,
not a count reconstructed from authoring inputs.

## Required owner behavior and evidence

- Preserve the existing discovery order and enumeration contract. Return a
  read-only collection view that cannot be cast back to the mutable `List`;
  repeated reads continue to observe owner changes through the supported
  discovery methods. The census provider reads exact counts directly from the
  owner.
- Before adding a previously unknown nonblank ID, reject the operation if the
  shared revision is saturated. Do not mutate the list unless the revision can
  advance. Existing/blank discoveries remain no-op failures and do not change
  either section count or revision.
- Add focused tests for the two provider identities/counts, deterministic
  owner ordering, repeated-read stability, location and route discovery,
  duplicate/blank no-op behavior, inability to mutate through the returned
  collection, snapshots/views observing supported changes, and saturation
  rejection before cardinality changes.
- Extend the selected-profile bootstrap test to assert both sections for each
  of the ten installed NPC owners with exact IDs/schema/identity/count/revision
  and aggregate values 20/10. Repeated census reads must not mutate state.
- Do not add a provider for LocalTopologyKnowledge, AdventureSiteIntel,
  ExplorableSiteKnowledge, or CommercialKnowledge in this slice. Their owner
  inventory and export order remain governed by P12-F and its dependencies.

## Dependency classification

This is only a census adapter and monotone-revision correction needed by
P12-B's complete live owner inventory. It does not deliver the P12-F
Knowledge/commitment export or staged-hydration work. The P12 Brief's
C/D/E-before-F implementation edge remains in force for all P12-F work. The
independent reviewer must confirm that this narrowly scoped B evidence slice
does not cross that F boundary; otherwise keep it `WAIT_DEPENDENCY` until
C/D/E are canonical.

After an accepted B adapter, later epoch wiring must notify both location and
route sections for one NPC after any successful discovery because they share
one revision. No capture protocol call is added here. Owner-thread binding,
operation quiescence, complete C/D/E/F write coverage, other per-NPC Knowledge
owners, and P12-A export/hydration remain unresolved.
