# P12-B RuntimeIdentityRegistry outer-commit boundary design

**Status:** Bounded design/evidence record; no implementation is authorized by
this document. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

**Evidence tip:** P12 canonical `81ddfe4`; selected-profile source basis
`ec75e6a`; SpatialNetwork census code `555594e` promoted at `46c457f`.

**Scope:** Clarify the exact invalidation obligation for mutations that change
the shared `RuntimeIdentityRegistry` revision, including mutations nested
inside another owner operation. This is a work package within accepted P12-B,
not a new checkpoint ID. It does not connect the coordinator to runtime,
change registration semantics, add a lock/lease, or make the selected profile
admitting.

## Evidence

`RuntimeIdentityRegistry` owns eight typed dictionaries and one monotone
`censusRevision`. Every successful `RegisterNpc`, `RegisterCity`,
`RegisterLocation`, `RegisterRoute`, `RegisterExplorableSite`,
`RegisterLocalPlace`, `RegisterLocalConnection`, or `RegisterNotableItem`
increments that shared revision once. A successful non-empty
`TryRegisterLocalTopologyMembers` batch increments it once. An empty batch,
rejected registration, duplicate/collision, or revision-exhausted write does
not change it.

The eight `RuntimeIdentityRegistryCensusProvider` sections all read this same
revision and the same owner instance:

1. `p12c.runtime-identities.npcs`
2. `p12c.runtime-identities.cities`
3. `p12c.runtime-identities.locations`
4. `p12c.runtime-identities.routes`
5. `p12c.runtime-identities.explorable-sites`
6. `p12c.runtime-identities.local-places`
7. `p12c.runtime-identities.local-connections`
8. `p12c.runtime-identities.notable-items`

`RuntimeIdentityCensusTests.TypedRegistrationsUpdateOnlyTheirOwnIndexAndSharedRevision`
already establishes that each successful typed registration changes only its
own cardinality while advancing the revision reported by all eight witnesses.
The selected `UnityBootstrap-Daily-v1` profile publishes 10 NPC, 2 City, 2
legacy Location, 2 Route, and zero in the remaining four indexes at registry
revision 16. Its separate `SpatialNetworkRuntime` has 2 locations, 2 routes,
and revision 4.

## Supported outer write paths

The source call-site audit identifies these production paths:

| Outer path | Registry write | Other owner sections affected by the same successful operation |
|---|---|---|
| `TesteSimulacao.InitializeSimulation` | City, ExplorableSite, NPC registration; network registration also writes Location/Route identities | Bootstrap-built City/NPC and spatial-network state; composition is published only after construction succeeds |
| `SpatialNetworkRuntime.RegisterLocation` / `RegisterRoute` | One Location or Route identity | The corresponding network location or route index and shared network revision |
| `LocalTopologyStore.TryAddTopology` | One atomic LocalPlace/LocalConnection identity batch when non-empty | The LocalTopology store and the topology's published state; LocalTopology is not composed in the selected P12 profile |
| `LocalTopologyStore.TryAddPlace` / `TryAddConnection` | One LocalPlace or LocalConnection identity | The existing LocalTopology and its child graph; this P10 owner is not composed in the selected P12 profile |
| `PlaceContentStore.TryRegisterNotableItem` | A NotableItem identity only when the exact item is not already in the registry | PlaceContent membership. If the exact identity was registered earlier, the registry revision does not change; content attachment still changes its own owner |
| `WorldObserverDemoBootstrap` | Demo City/Site and network identities | Diagnostic demo composition, outside the selected profile |

Other direct `RuntimeIdentityRegistry.Register*` call sites found by source
search are test fixtures. They do not add runtime operations to the selected
profile.

## Invalidation contract

`ContinuationCensusProtocol.NotifyCommittedMutations` validates and refreshes
only the section IDs supplied by its caller. It does not discover every
section whose owner revision changed and does not group nested calls. The
current kernel is not connected to the selected bootstrap or any runtime
writer.

Therefore the existing API's exact caller obligation is:

- After any committed operation that advances `RuntimeIdentityRegistry`'s
  revision, include **all eight** registry section IDs in the one
  `NotifyCommittedMutations` call. Cardinality changes in only one typed index
  do not narrow this set: all eight witness revisions changed.
- Include every other registered owner section whose revision changed in that
  same logical outer commit. In the two `SpatialNetworkRuntime` registration
  paths, this adds both `p12d.legacy-spatial-network.locations` and
  `p12d.legacy-spatial-network.routes`, because both share the network
  revision. A direct registry write changes no network section.
- Notify once, only after the complete owning operation commits. Do not notify
  from the registry leaf when it is nested under SpatialNetwork, LocalTopology,
  bootstrap, or PlaceContent installation. An intermediate notification could
  publish partial owner baselines and would incorrectly count a later-rolled-
  back operation as a committed write.
- Failed preflight, rejected writes, idempotent no-ops, and empty topology
  batches do not notify because no owner revision advanced. The RuntimeIdentity
  registry has no removal/rollback API. If a future supported outer operation
  compensates a registry write or another owner write while leaving a
  monotone witness revision advanced, it must refresh every changed baseline
  once at outer-operation exit. The current protocol can do this only through
  `NotifyCommittedMutations`, which also advances the mutation epoch; leaving
  those revisions stale makes the next complete assessment fail closed. A
  no-op/full rollback that leaves every registered witness revision unchanged
  needs no notification.
- For bootstrap, establish the initial profile baseline only after the
  complete runtime composition has been successfully published. Do not assess
  or notify against partially built registry/network state.

If all eight registry sections are registered and only one is supplied after
a successful write, the unreported seven keep stale baselines; complete
inventory assessment must fail closed. This is a detectable omission, not a
valid partial epoch update. A caller that supplies one section per nested
writer would also advance the epoch more than once and expose intermediate
baselines, violating the outer-commit rule.

The operation-tracker methods currently have no production call sites:
`TryEnterOperation`, `BindOwnerThread`, and `NotifyCommittedMutation(s)` are
used by the isolated protocol tests, not by bootstrap, owner services, or the
daily loop. The existing `TryAdvanceDay`/`TryAdvanceDays` lease is a
reentrancy guard; direct `SimulationTime.TryAdvanceDay` remains outside it.
Other transaction-shaped boundaries can be mapped from source—economy
transactions, Person/population lifecycle, battle terminal resolution, and
prepared Commercial Knowledge sharing—but direct leaf APIs and child-store
mutators remain callable outside those boundaries. Mapping these groups to
their complete changed-section sets is still outstanding. In particular,
PersonStore/Genealogy rollback paths can restore cardinality while advancing
their monotone revisions; see the reviewed source footprint in
`PHASE12_P12B_OPERATION_FOOTPRINT_AUDIT.md`. The generic “full rollback does
not notify” line in the earlier blocker matrix applies only when every
registered witness revision is unchanged and must be reconciled with these
revision-visible compensation paths before runtime wiring.

The targeted post-promotion owner revalidation also found selected-state
revision bypasses outside the registry: per-NPC `SpatialKnowledgeRuntime`
returns its mutable location/route lists, and `NpcRuntime` exposes mutable
status, current-action, travel-plan, and merchant-plan child objects. The full
findings are recorded in `PHASE12_B_BLOCKER_RESOLUTION.md`. They must be
closed or explicitly captured in the eventual operation/owner contract before
the complete profile can admit capture.

## Design disposition and remaining gate

No registry-local observer/callback should be added at this stage. It cannot
know whether its leaf write is nested in a multi-owner commit, whether the
outer owner later rolls back, or which non-registry sections changed. No
`SimulationRuntime`, `SimulationTime`, or P18 advance-lease changes are
authorized by this evidence.

The smallest safe implementation must wait until the remaining P12-B source
map identifies every supported outer operation and the selected live owner
inventory is complete enough to register its section set. Next evidence work
is a per-operation footprint matrix recording outer API, changed sections,
commit point, rollback/compensation, local revision behavior, and direct leaf
bypasses; the matrix must cover advance/clock, economy/merchant,
Person/population lifecycle, Justice/Crime/core resolution, and
Knowledge/directives/choices/travel/expeditions. In parallel, remaining owner
cardinality/revision gaps and composed-empty versus absent roles need exact
runtime evidence. At the resulting reviewed boundary, the implementation can
reuse `NotifyCommittedMutations` and make each reviewed outer owner supply the
complete changed-section set exactly once. If source evidence demonstrates
that this cannot be done without a new coordinator batch API, define and
accept that API as a separate bounded checkpoint before implementation; do
not add it speculatively here.

This design resolves the shared-registry revision/cardinality assumption but
does not establish complete write coverage, owner-thread/quiescence, runtime
exact-zero witnesses, or capture eligibility. Required independent review
must verify this contract against the exact source tip before it is used to
authorize later owner adapters.
