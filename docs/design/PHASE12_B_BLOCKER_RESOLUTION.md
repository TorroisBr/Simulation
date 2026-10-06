# P12-B blocker resolution and dependency plan

**Status:** Static owner/write-map pass complete, with a partial live
day-zero census through the selected-profile bootstrap test. Current P12
canonical at the direct-owner candidate base is `2f7c7422812de40aa1223e8310dcbd9f5d8ca474`; it includes the
previously promoted passive owner witnesses, the reviewed NPC-trade
invalidation slice, and the promoted Market-operation invalidation bundle.
The latter's code candidate is `b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`
(tree `88ec452a979a7439b825858a6b1b271f8bc6c948`) and its exact-tip review
passed. Complete owner coverage, complete shared-epoch invalidation, runtime
capture fencing, and P12 readiness are not claimed. This record decomposes
already accepted P12-B–P12-G scopes. It adds no checkpoint ID, product
behavior, or implementation authorization.

**Evidence baseline:** P12 canonical
`04105d31e88fca97888dddb8e974236a7f4b6804`; the selected-profile source basis
is validated composition `ec75e6a`. Independent read-only audits covered
composition/cardinality, C/D mutators, E/F mutators, and
thread/quiescence/exact-zero paths. On the `e1d947d` code candidate,
`ContinuationCensusProtocolTests` passed 18/18, `EconomyTransactionTests`
passed 45/45, and the selected-profile live bootstrap test passed 1/1. Their
XML results are respectively
`Temp/ValidationResults/EditMode-20260929-185655-26d236ff9f1e4026a2f50afe631ba117.xml`,
`EditMode-20260929-184219-213abe140c7d4a5cbc004da037370fe3.xml`, and
`EditMode-20260929-185153-b56ff944324d4e1683d6ecd53e5d633c.xml` in the
Unity project. The profile test ran against the published live day-zero
composition; its limited assertions are recorded below. No test establishes
full owner coverage or owner-thread quiescence. Unity-generated settings and
`.meta` changes in the checkout are outside this record.

## Evidence completed and evidence still missing

The audits close the *static source-map* portion of the P12-B blocker. They
identify the selected composition, reachable owner instances, mutator entry
points, current local revisions, mutable escapes, and known absent/conditional
sections. They do not prove a live evolved owner count, a revision-bound
runtime census, that every successful commit invalidates capture eligibility,
or that runtime capture is restricted to a quiescent owner thread.

| Obligation | Source evidence now available | Capability/evidence still required |
|---|---|---|
| Live owner/cardinality census | The selected-profile test runs normal genesis against `Simulation-GeneralTest.asset` and inspects the published runtime before day one. It verifies City/NPC, population, market, inventory, and spatial-knowledge cardinalities; Person/Genealogy zero; positive P8-A geography; P8-B–D exact-zero child-owner counts/revisions; P8-A–D and receipt witnesses; the legacy SpatialNetwork's exact 2 locations/2 routes at revision 4; and empty ActorChoice/directive/travel/expedition state. Promotion `0021b0a` adds a roster-following pair of SpatialKnowledge sections per registered NPC and accounts for fixed PersonStore witnesses at the supported membership/materialization boundary. Promotion `10fb58d` adds one passive exact-owner/cardinality/revision witness per City for `ImportantNpcs` and checks reciprocal membership against the live NPC roster. Promotion `15e5a54` adds a roster-following Inventory section per registered NPC, bound to the exact installed NPC and Inventory owners. | This is still a partial live census: City/NPC composite roots, several directly observed collections, some C roots, and E/Justice owners are not covered by complete owner witnesses. The available passive witnesses are not registered as a complete profile inventory. Each included owner still needs exact identity, section/schema version, count, and revision in the capture provider. Source-derived counts cannot stand in for evolved state. |
| Committed-write invalidation | Successful mutation entrypoints are mapped below, including owner-local revisions and public bypasses. Multi-owner transactions, rollback-only paths, queries, plans and proposals are distinguished. The promoted dynamic NPC boundary advances its partial epoch for supported roster/materialization deltas. City `ImportantNpcs` now has a local membership revision and passive witness; the Inventory witness observes its owner-local revision. | A successful authoritative commit must notify the one P12-B epoch after the owning commit (or whole multi-owner commit). Failed preflight and full rollback do not notify; a successful compensating write does. The promotions do not connect SpatialKnowledge discoveries, City/NPC projection, direct InventoryRuntime writes, unrelated PersonStore writers, or remaining C/D/E/F writes to the shared epoch. Prove every included public path is covered or no longer supported outside an instrumented boundary. |
| Owner-thread/quiescence | Source audit finds synchronous `Start → InitializeSimulation` and `Update → Simulate` paths. The selected-profile EditMode test calls `Start()` directly and captures no `Update` frame or managed-thread identity. Promotion `0021b0a` checks the owner thread for the local NPC membership census context and fail-closes on a mismatched nested entry. | Bind and verify the actual Unity runtime owner thread; track bootstrap, outer advance and every supported in-flight multi-owner operation; reject capture during any scope or from a different thread. The local membership check does not prove normal-frame affinity or exclude external callers across the runtime. |
| Runtime exact-zero witnesses | The profile has explicit exclusions and known conditional-empty sections. Composed-but-empty is distinct from not composed; passive owner witnesses now exist for P8-A–D, both receipt ledgers, legacy SpatialNetwork locations/routes, dynamic NPC SpatialKnowledge and per-NPC Inventory, per-City presence, scheduled directives, TravelParty instances, active Expeditions, and ExplorableSite records. The selected-profile test observes empty directives, exact 0/0 TravelParty, Expedition, and ExplorableSite witnesses, with stable installed-owner identities. | Add witnesses for remaining included owners and revalidate the full set. These owner-local providers do not establish complete census registration, shared-epoch invalidation, or capture eligibility. Do not infer zero from scene startup, a skipped consumer, or a missing runtime reference. |

## Selected-profile census targets

The following groups are the census surface, not a claim that every owner is
already exportable or hydrated. Registration must bind a concrete instance;
missing, duplicate, unsupported, or unversioned entries fail closed.

| P12 owner group | Concrete selected-profile owners to witness | Important exactness / current gap |
|---|---|---|
| C — causal roots | `RuntimeIdAllocator` (14 per-kind counters), `RuntimeIdentityRegistry` (8 typed indexes), `SimulationRecordSequence`, concrete deterministic-random provider and use/stream state, P9-B genesis manifest/provenance, P8-A geography facts. | Allocator, registry, and sequence have partial passive census witnesses but no complete export/hydration contract; the selected deterministic provider still needs evidence of retained use/stream state. P8-A has passive local revision/count witnesses but still lacks export/hydration; the composed spatial authority also owns P8-B–E child sections, whose distinct states cannot be inferred from P8-A’s positive cardinality. |
| D — factual roots | Runtime City and NPC roster/composite state; each City’s Market, accounts, inventories and Population; Person/Genealogy/lifecycle owners; legacy `SpatialNetworkRuntime`, `ExplorableSiteStore`, and their existing runtime-ID location/site links. | City/NPC have no composite revision. `Cities` still exposes its mutable backing list; `ImportantNpcs` is now a City-owned live read-only projection with a local revision and passive reciprocal witness, not a composite City/NPC revision. The legacy network now has exact passive location/route count and revision witnesses, but direct registry writers and shared-epoch invalidation remain outside that local witness. Person/lifecycle commits can span owners without a capture scope. Site identity is a root; exploration progress lives in expedition/Knowledge owners. |
| E — official/core domains | Effective-config-selected City economy, transaction service and child accounts/inventories/markets; Merchant and Commercial Knowledge sharing; Justice/Crime/appraisal; composed political, institution, property/estate, armed-force/manpower/position, conflict/war/battle stores and enabled action providers. | Local guards/revisions are not a global invalidation epoch. Direct child or system mutators bypass coordinator-level evidence. Provider membership must follow resolved effective configuration, not the serialized module list. |
| F — knowledge/commitments | Political/Crime and per-NPC exploration/adventure Knowledge; directives; default `ActorChoiceStore`; travel and party owners; `ExpeditionStore`/runtime/system. | Several per-NPC Knowledge owners still lack complete revision/export coverage. Directives, travel parties, and expeditions now have owner-local count/revision witnesses, but lack immutable export/staged hydration and shared-epoch invalidation. Expedition collections still expose live runtime objects rather than detached owner values. Actor-choice terminal history and sequence are causal continuation state. |
| Census-only read models | `DomainEventStore` and `NpcDecisionStore` may be counted/versioned as known read models; History is a selected subset, Chronicle is a derived view. | They are not authoritative owner sections and do not invalidate the world-truth epoch. `SimulationRecordSequence` is a separate causal C root and must be retained exactly. |

### Live selected-profile day-zero evidence

`SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
passed 1/1 using the real
`Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset`. The test
creates `TesteSimulacao`, calls `Start()`, and inspects the published
`SimulationBootstrapComposition` before any day advance. It verifies:

- P8-A runtime `SpatialAuthorityStore`: one Hex, one anchored Location and one
  scale context at revision 1, with the selected profile's semantic IDs and
  provenance. Promoted `p8a.hexes`, `p8a.locations`, and `p8a.scale-context`
  witnesses report separate 1/1/1 cardinalities, installed owner identity,
  and the shared spatial revision.
- The two City and ten NPC roots; each City's `ImportantNpcs` projection is
  witnessed against the live NPC roster with exact City identity, count,
  membership revision, uniqueness and reciprocal City/Location checks; 1,800
  aggregate population; ten market
  rows totaling 1,395 units; two NPC inventory rows totaling 8 units; and ten
  per-NPC spatial-Knowledge owners totaling 20 location observations and 10
  route observations, with revision 3 on each spatial-Knowledge owner.
- The installed legacy `SpatialNetworkRuntime` reports two runtime-ID
  locations and two routes at shared revision 4 through fixed schema-v1
  witnesses. These are independent of the positive P8-A `LocationId` and
  separate from P8-C City/Site-to-Location bindings.
- Person and Genealogy cardinality zero. The City presence projection has its
  own per-City passive revision witness; City/NPC roots and their broader
  public collection counts still do not have composite revision witnesses.
- `NpcDecisionRecorder.occurrenceReceipts`: exact cardinality 0 and revision
  0, with the same owner-instance identity on repeated reads.
- `EconomyTransactionService.keyedSaleReceipts`: exact cardinality 0 and
  revision 0, with the same owner-instance identity on repeated reads.
- `ActorChoiceStore` is composed, current day is 0, and History is empty.
- ActorChoice count, directives, travel parties, and expeditions are empty.
  The directive witness has the exact installed store identity and matches its
  live count and revision; TravelParty and Expedition witnesses report exact
  cardinality 0 and revision 0 on their installed owners. These local witnesses
  do not constitute complete P12-B census registration or epoch invalidation.
- The composed ExplorableSite provider is bound to the installed site store.
  The selected-profile test now reads its exact 0-count/0-revision witness,
  verifies the installed owner identity, and verifies stable identity/count/
  revision on a repeated read. This is day-zero evidence only.
- P8-B `p8b.passage-option-barrier-state` and `p8b.crossings` are exact-zero
  passive witnesses with stable installed-owner identities and parent spatial
  revision 1. P8-C `p8c.city-site-location-bindings` and
  `p8c.person-positions` are exact-zero passive witnesses at revision 0. P8-D
  route Knowledge and route plans are now exact-zero schema-v1 passive
  witnesses at revision 0 using their installed runtime owners. The P8-E
  coordinator is not a retained census section. P10 `LocalTopologyStore` is
  not composed.

The P8-A/B/C/D and legacy SpatialNetwork adapters produce section/schema
owner witnesses, but none is registered in a complete P12-B runtime census.
These reduce the live-evidence gap without closing the B inventory or
section-registration requirement.

This is a live published runtime composition, but the test manually invokes
`Start()` in EditMode. It does not exercise Unity `Update`, capture a managed
owner-thread identity, census all included owners, or establish quiescence.
It is not an evolved-boundary census. The broader startup figures below
remain source-derived expectations unless explicitly named above. The
allocator/identity/record witnesses remain passive inventory rather than
export/hydration evidence; retained deterministic RNG use and unwitnessed
E/Justice owners remain live census blockers. No reflection or “not called”
inference is substituted for owner exports.

For exact-zero evidence, record these roles separately:

- **Required/populated:** P8-A’s profile facts require exactly one authored
  Hex, one anchored Location, and the authored scale context. These are
  positive-cardinality witnesses, not “nonzero is fine.”
- **Composed/known empty:** `EconomyTransactionService.keyedSaleReceipts` and
  `NpcDecisionRecorder.occurrenceReceipts` exist in reachable composed owners,
  while the selected legacy daily profile does not invoke their P18-D writers.
  Both expose owner-issued count/revision witnesses on canonical; the
  selected-profile test observes exact zero for each. This closes only
  these two conditional sections, not all exact-zero or invalidation coverage.
- **Composed/known empty:** P8-B passage, P8-C City/Site-to-Location bindings
  and Person positions, P8-D route Knowledge/plans, and the P8-E component
  travel-authority sections are composed in the selected profile and
  explicitly empty. P8-B/C/D now have passive owner-issued schema-v1
  witnesses. The P8-C binding owner is the runtime-installed
  `LegacySpatialAnchorBindingStore` (its historical class name does not make
  these rows the separate legacy City/NPC runtime-ID links under D). These
  sections are not absent merely because no facts are present.
- **Not composed:** P10's `LocalTopologyStore`/PlaceContent and autonomy
  services, P14-A local material flow, P18 temporal/intraday state, P19
  loader/module state, P20 activity state, and external WorldCommand
  service/queue are absent from this selected composition. Record the absent
  composition role; do not invent a count for a nonexistent instance.
- **Omitted read model:** events/decisions/history/chronicle are not causal
  truth sections. Count the selected stores only for census completeness;
  their row counts are not required to be zero.

## Source-derived startup census (not an evolved-boundary witness)

The following values are derived from the selected authored profile and its
normal construction path. Other than the explicitly listed day-zero live
checks above, these are startup expectations and API-coverage evidence, not a
complete live P12 boundary census. Every adapter must read the installed
runtime owner after clone/publication and report its exact current count and
revision.

- **C roots:** one `RuntimeIdAllocator` has 14 private next-value families.
  After authored bootstrap, NPC/City/legacy Location/legacy Route next values
  are respectively 11/3/3/3; the other ten families remain 1. The shared
  `RuntimeIdentityRegistry` starts with 10 NPC, 2 City, 2 legacy Location and
  2 legacy Route identities, plus zero Site/LocalPlace/LocalConnection/
  NotableItem identities (16 total). One shared `SimulationRecordSequence`
  remains at next=1 because initialization emits no events or decisions. The
  selected profile uses one seed-zero deterministic provider through three
  pure keyed call families at default draw index 0 and retains no mutable RNG
  stream. P9-B provenance is one immutable manifest; P8-A installs one Hex,
  one anchored Location and one separate scale context, with the cloned
  runtime `SpatialAuthorityStore` at revision 1. The promoted passive
  allocator, registry and sequence witnesses improve live owner evidence but
  do not provide immutable export/hydration or complete RNG-use coverage.
  Preserve the separate P12-C candidate
  `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835`: it contains
  reusable allocator and record-sequence snapshot seams, but is based on a
  stale sibling base and remains blocked until P12-B is complete and the
  candidate is selectively revalidated.
  Do not take its full `DecisionRecords.cs` diff, which removes the current
  P18 receipt owner and P11 `ActorChoice` type; registry/RNG/genesis/P8-A
  export and staged reconstruction evidence remain outstanding.
- **D roots:** two City owners start with population aggregates 1,000 and 800
  (each local population revision 0). Ten NPC runtimes start, five attached to
  each City; the legacy spatial network starts with two City locations and two
  directed routes. `PersonStore` and `GenealogyStore` start empty; the authored
  NPCs have no `PersonId`. `ExplorableSiteStore` starts empty. These counts can
  evolve, while the live City/NPC roots expose mutable relationships and lack
  a composite revision; they must not be substituted by asset/configuration
  counts at capture. Each City's `ImportantNpcs` projection now separately
  reports an exact owner/count/revision witness and reciprocal roster check;
  this does not revise the broader City/NPC composite.
  `SettlementPopulationRuntime` has its own count/revision but does not cover
  Person, residence, genealogy, death or materialization edges; `PersonStore`
  has no count/revision pair and `GenealogyStore` has count/records but no
  revision. The selected profile's `SpatialNetworkRuntime` now has passive
  owner witnesses for exact location/route counts and shared revision; its
  public collections are detached read-only snapshots. `ExplorableSiteStore`
  exposes owner-local `Count`/`Revision`, but these are not a shared P12 epoch
  or export/hydration contract. City/NPC and other derived startup counts do not yet form a
  complete owner-backed census.
- **E owners:** the two `MarketRuntime` instances start with five item rows
  each and 1,395 units total; ten NPC-owned accounts/inventories are composed,
  with two authored NPC inventories containing four units each. Market and
  InventoryRuntime revisions exist. Rostered per-NPC Inventory row-count and
  revision witnesses are now promoted, but market rows still have no row-level
  witness and no broader account/commerce aggregate census was found. Several
  political stores expose local
  Count/Revision (`PoliticalClaimStore`, `FactionStore`, `PoliticalSupportStore`,
  `PoliticalKnowledgeStore`, `ArmedForceStore`); institution, office,
  property/estate, political-decision, manpower/spatial-force, persistent
  conflict/war/battle, and complete Justice/warrant sections lack sufficient
  public count/revision witnesses. `CrimeSocialAppraisalWorldState` spans
  multiple stores, so one substore count is not complete. Store defaults or
  no authored row are genesis expectations, not substitutes for witnesses.
- **F owners:** each of ten NPC-held `SpatialKnowledgeRuntime` owners starts
  with two known legacy locations and one known route, and has an owner-local
  `Revision`. `ActorChoiceStore` starts empty and exposes `Count`/inputs but
  no revision. `ScheduledDirectiveStore`, `TravelPartyStore`, and
  `ExpeditionStore` now have local count/revision census witnesses, but still
  lack complete export/staged hydration and shared-epoch invalidation.
  Commercial observations require per-NPC owner evidence because the merchant
  bootstrap path is effective-config- and actor-dependent. P10 exploration
  content/autonomy and P18-D temporal/local-observation owners are not composed
  by this selected profile; do not label them empty stores.
- **Conditional receipt owners:** this branch provides exact owner-issued
  census witnesses for both `NpcDecisionRecorder.occurrenceReceipts` and
  `EconomyTransactionService.keyedSaleReceipts`. The selected-profile test
  confirms both at `Count=0, Revision=0` with stable owner identity. Their
  commit hooks still are not connected to the shared P12-B invalidation epoch.
- **P8-B–E sections:** installed passage options, barriers, option-state rows,
  barrier-state rows and crossings each start at zero. P8-C's
  `LegacySpatialAnchorBindingStore` and `PersonSpatialPositionStore` each
  expose exact `Count=0, Revision=0`; P8-D route Knowledge exposes
  `ObservationCount=0, Revision=0`, and route plans expose
  `PlanCount=0`, `History.Count=0`, `Revision=0`. The passage child uses the
  parent `SpatialAuthorityStore.Revision` as its invalidation stamp. P8-E's
  coordinator has no retained state and is not a census section. These store
  APIs are synchronous but unsynchronized; the values do not prove a
  quiescent runtime read.

## Committed-write invalidation map

The commit map below is the supported source-level scope for the accepted
P12-C/D/E/F owner work. Notify only after a successful commit. For a coupled
multi-owner write, notify once after the outer commit is complete. Existing
local receipts/revisions are useful owner evidence, but none substitutes for
the P12-B invalidation epoch.

| Owner group | Successful writes that require coverage | Current bypass or witness gap |
|---|---|---|
| C roots | All `RuntimeIdAllocator.Allocate*` operations; `RuntimeIdentityRegistry.Register*` and local-topology batch registration; event/decision record allocation through `SimulationRecordSequence.Allocate`; any mutable RNG stream draw retained by the selected provider; P9 genesis publication; P8-A geography and any excluded child-store writes. | Passive allocator, registry, and sequence witnesses now expose selected owner cardinality/revision, but they are not registered in a complete capture inventory and do not provide export/hydration. Direct identity-registry registration remains a P12 epoch path even when it bypasses `SpatialNetworkRuntime`. Keyed RNG calls are pure for fixed seed/key/index, but the provider interface permits mutable streams; admission must bind concrete provider and census retained stream/use state. P8-A’s one revision spans P8-A and excluded children. |
| D: roster and NPC/City | `TryRegisterNpc`/`TryUnregisterNpc`; City/NPC presence and travel/status/action/life/hidden/plan setters; `AddImportantNpc`/`RemoveImportantNpc`; City production/consumption/price update; direct Market/Inventory/MoneyAccount writes. | `Cities` returns its backing list. `ImportantNpcs` is now a City-owned live read-only view with a City-local revision and passive reciprocal witness, but City/NPC still lack a composite witness; NPC status/child objects and other child owners have separate mutation paths. |
| D: population/identity | Population transitions and paired migration; Person registration/binding/unbinding; genealogy add/remove; named birth; materialization/adoption; residence binding; immigration/emigration/resident death and Person death; NPC residence migration. | Birth/materialization/death/migration can expose an intermediate multi-owner graph before the final leg. Static public lifecycle entrypoints can bypass `SimulationRuntime` wrappers and partial political revision. Rollback is not a committed truth change if complete; failed restoration faults the runtime. |
| P8-C: canonical spatial presence | `LegacySpatialAnchorBindingStore.TryBindCity`/`TryBindSite`/`TryBind`; `PersonSpatialPositionStore.TrySetAt`, `TryBeginTransit`, `TryAdvanceTransit`, `TryArrive`, and P8-E prepared installs. P8-E outer travel operations may change more than one section and notify once after complete installation. | Both stores expose owner-local count/revision but have no census registration or P12 epoch connection. The class name `LegacySpatialAnchorBindingStore` is historical: P8-C owns its stable City/Site-to-P8-A-Location bridge. |
| D: legacy spatial/site | `SpatialNetworkRuntime.RegisterLocation`/`RegisterRoute`; `ExplorableSiteStore.Add`; embedded legacy City/NPC runtime-ID location/site links. | `SpatialNetworkRuntime` now returns detached read-only collection views and reports local section counts/revision, but its local revision is not connected to the shared P12 epoch. Direct `RuntimeIdentityRegistry.RegisterLocation/RegisterRoute` writes can bypass the network owner. `ExplorableSiteStore` exposes owner-local `Count`/`Revision`, but has no immutable export/staged hydrator or shared epoch. These links are separate from P8-C's City/Site-to-`LocationId` bridge and Person positions. Exploration progress is covered under F, not site identity. |
| E: economy/merchant | Successful `EconomyTransactionService.Try*` transfers, trades, purchases/sales, consumption, travel charges and successful compensation; child Money/Inventory/Market writes; legacy City daily economy; Merchant state/knowledge/effect commits; Commercial Knowledge observations/share commit. | Transaction service is not guard/epoch-bound; child APIs are directly callable. City has only a staged daily receipt. Merchant urgency receipt covers only its staged step. Commercial Knowledge can be mutated directly. |
| E: justice/crime and core owners | Successful crime action/hidden-status effects; Justice warrant, arrest, sentence, escape, wanted-status and failed-escape transitions; crime outcome/Knowledge/appraisal commits; institution/office/tenure writes; force/manpower/position writes; conflict/war/battle registration, participant, start/end and final resolution; faction/claim/support/Political Knowledge/property/estate commits. | Guard binding and local revisions protect selected subsets only. Aggregate and cross-owner operations need one notification after successful installation; proposals/prepares/computes/queries do not notify. |
| F: knowledge/directives/choice | Per-NPC Adventure/Exploration Knowledge observations; directive add and terminal processing-state transitions; every successful `ActorChoiceStore` capture/defer/reject/dispatch/returned/threw transition; travel start/advance; party add/complete/remove and travel; expedition add/remove/complete/start/progress/traversal/resource/opposition/return/reconciliation. | Direct per-NPC writes can bypass guard-bound systems. Directives, TravelParty, and Expedition owners now expose local count/revision witnesses, but no complete export/hydration or shared epoch; party/expedition values remain live runtime objects. |

### Exact method families from the source audit

This list names the selected-profile paths found in source, so owner
implementation and review can check each one against an actual post-commit
notification or a documented, closed outer transaction. An entry can change
several owners; it is not a requirement to emit one notification per internal
field write.

**P12-C roots**

- `RuntimeIdAllocator`: `AllocateNpcId`, `AllocateCityId`,
  `AllocateLocationId`, `AllocateRouteId`, `AllocateEventId`,
  `AllocateDirectiveId`, `AllocateDecisionId`, `AllocateTravelPartyId`,
  `AllocateOrganizationId`, `AllocateExplorableSiteId`,
  `AllocateExpeditionId`, `AllocateLocalPlaceId`,
  `AllocateLocalConnectionId`, and `AllocateNotableItemId`.
- `RuntimeIdentityRegistry`: `RegisterNpc`, `RegisterCity`,
  `RegisterLocation`, `RegisterRoute`, `RegisterExplorableSite`,
  `RegisterLocalPlace`, `RegisterLocalConnection`, `RegisterNotableItem`,
  and internal batch `TryRegisterLocalTopologyMembers`.
- `SimulationRecordSequence.Allocate`, consumed by selected
  `DomainEventRecorder.Record` and `NpcDecisionRecorder.RecordWithParticipants`
  (and the selected recorder’s other record paths). A sequence increment is
  causal even if a later read-model append fails.
- `SpatialAuthorityStore.TryComposeGeography`, `TryRegisterHex`,
  `TryRegisterCrossing`, `TryRegisterLocation`, `TryBindLocalTopology`;
  composed child passage writes `PassageAuthority.TryRegisterConnection`,
  `TryRegisterWildernessRule`, `TryRegisterBarrier`,
  `TryChangePassageCondition`, and `TryChangeBarrierCondition`.
  P8-B–E passage/binding/position/Knowledge/plan sections need exact-zero
  witnesses independent of the required P8-A geography counts. Read the
  installed `SimulationRuntime` clone and its child stores, not pre-clone
  genesis authorities. `LocalTopologyStore` is a separate absent P10 owner in
  this profile.
- Keyed deterministic `NextUnit` is pure for fixed seed/key/index in the
  selected provider; if any retained `DeterministicRandomStream` is created,
  `NextUnit` advances its private cursor and `CreateStream` must be reflected
  by the provider-use census. Do not infer stream absence from the interface.

**P8-B–E exact-zero live sections**

- P8-B passage option/barrier state is read from the installed
  `Runtime.SpatialAuthorityStore.PassageAuthority`; crossings are counted by
  the installed parent store. Passage snapshots and crossing count can be
  paired with the parent `SpatialAuthorityStore.Revision`, which advances for
  these commits. The selected profile starts all of these sections at zero;
  local-topology bindings are a separate P10 section.
- P8-C canonical City/Site-to-Location bindings and Person positions have
  owner-local `Count` and `Revision` witnesses on the runtime-installed
  `LegacySpatialAnchorBindingStore` and `PersonSpatialPositionStore`. P8-D
  route Knowledge has
  `ObservationCount`/`Revision`; route plans/history have
  `PlanCount`/`Revision`. These four selected live stores start empty.
- P8-E's travel transaction coordinator retains no section state. Census its
  position, route-plan, route-Knowledge and passage component owners; do not
  invent a fifth coordinator-owned zero. These synchronous stores do not expose
  concurrent-read safety, so this census does not prove runtime quiescence.

**P12-D factual roots and multi-owner lifecycle**

- `SimulationRuntime.TryRegisterNpc` / `TryUnregisterNpc`; direct
  `SimulationTime.AdvanceDay` / `TryAdvanceDay`; outer
  `SimulationRuntime.AdvanceDay`, `TryAdvanceDay`, `AdvanceDays`, and
  `TryAdvanceDays` paths. The P18 lease protects only guarded advance calls;
  direct clock writes still require coverage.
- City/NPC projection and state mutators:
  `CityRuntime.AddImportantNpc` / `RemoveImportantNpc`;
  `NpcRuntime.SetCurrentAction`, `SetCurrentActionRuntime`, `TryApplyInjury`,
  `TryApplyDeath`, `ApplyConflictConsequence`, `AddStatus`, `RemoveStatus`,
  `SetCurrentPresence`, `AddMoney`, `TrySpendMoney`, both `StartTravel`
  overloads, `SetActiveTravelPartyId`, `ClearTravelStartedToday`,
  `AdvanceTravelDay`, both `CancelTravel` overloads, both `SetTravelPlan`
  overloads, `ClearTravelPlan`, `HideForDays`, `AdvanceHiddenDay`,
  `ClearHidden`, `SetMerchantTradePlan`, and `ClearMerchantTradePlan`.
  `CurrentStatus`, `CurrentActionRuntime`, plans, inventory and account
  references also expose child state beyond a single NPC revision.
- City/economy roots:
  `CityRuntime.SimulateProductionDay`, `SimulateConsumptionDay`,
  `UpdateMarketPrices`; `MarketRuntime.AddStock`, `RemoveStockUpTo`,
  `UpdatePrices`; `InventoryRuntime.AddItem` / `RemoveItem`; and
  `MoneyAccountRuntime.TryCredit` / `TryDebit`. `BuyItem` / `SellItem` route
  through the transaction service and are also covered under E.
- Population and Person authority:
  `SettlementPopulationSystem.TryApply` and
  `TryApplyTransitionWithReceipt`; `NpcResidenceMigrationSystem.TryApply`
  (both overloads); `PersonStore.TryRegister`; named birth
  `PersonBirthLifecycleSystem.TryApplyNamedBirth`; materialization/adoption
  `PersonMaterializationSystem.TryMaterializePerson` /
  `TryBindExistingNpcToPerson`; residence
  `PersonResidenceMembershipSystem.TryBindExistingResident`; immigration,
  emigration, resident death and conflict-injury resident death through
  `NpcPopulationLifecycleSystem.TryApply*`; Person death and death-with-injury
  through `PersonDeathLifecycleSystem.TryApply*`; and
  `GenealogyStore.TryAddParentage` / `TryRemoveParentage` plus the public
  `PersonGenealogySystem` entrypoints. Include the `SimulationRuntime` wrapper
  overloads where they are the selected call path. Multi-owner birth,
  materialization, death and migration need one operation scope spanning all
  legs and one invalidation after the committed install.
- Legacy spatial/site roots: `SpatialNetworkRuntime.RegisterLocation` /
  `RegisterRoute`; `ExplorableSiteStore.Add`; and the existing embedded
  legacy City/NPC location and site links. P8-C City/Site-to-Location bindings
  and Person positions are tracked separately above.

**P12-E core and official daily owners**

- `EconomyTransactionService.TryTransferMoney`, `TryExecuteNpcTrade`,
  `TryExecuteOpenMarketPurchase`, `TryExecuteOpenMarketSale`,
  `TryExecuteMarketPurchase`, `TryExecuteMarketSale`,
  `TryExecutePopulationConsumption`, `TryChargeTravelGroup`,
  `TryChargeTravel`, and successful `TryRestoreTravelCharge`. Its keyed sale
  receipt writer `TryExecuteKeyedMarketSale` is composed but outside this
  profile’s consumer and must remain exactly empty.
- Direct child writes are `MoneyAccountRuntime.TryCredit` / `TryDebit`,
  `InventoryRuntime.AddItem` / `RemoveItem`, and `MarketRuntime.AddStock`,
  `RemoveStockUpTo`, `UpdatePrices`; all can change causal state outside the
  transaction owner.
- Merchant commits include `AdvanceNpcTradeState`,
  `BootstrapInitialKnowledge`, `ObserveCurrentMarket`, and successful
  `TryExecuteAction`; the narrow P18 step path
  `TryCommitPlanUrgencyStep` and excluded keyed path
  `TryExecuteKeyedLocalMarketSale` are not the selected daily consumer.
  Commercial Knowledge commits include `RecordObservation`,
  `RecordLiquidityObservation`, and successful sharing-batch `TryCommit`.
- Crime commits include `TryExecuteAction`, `HandleActionFailure`, and
  `AdvanceHiddenStatuses`. Justice commits include `BeginDay`,
  `CreateInitialWarrants`, `CreateOrIncreaseWarrant`, `Arrest`,
  `AdvanceSentences`, `ApplyEscapeSuccess`, `SyncWantedStatuses` /
  `SyncWantedStatus`, and `RegisterFailedEscape`; also the successful
  wanted-record transitions `AddPenalty`, `Resolve`, `AdvanceDay`,
  `RegisterFailedEscape`, and `ClearArrestedToday`.
- Crime appraisal commits include
  `TheftOutcomeStore.TryRecord` / `TryAcceptTheftOutcome`,
  `CrimeKnowledgeStore.TryRecord` / `TryAcceptTheftOutcome`, and aggregate
  `TryRecordKnowledgeAndAppraise`.
- Institutional commits: `InstitutionStore.TryRegister`;
  `OfficeStore.TryRegister`, `TryAssignIncumbent`, and both
  `TryVacateOffice` forms. Armed-force commits: `TryRegister`, `TryReparent`,
  both `TryDetach` forms, `TryReattach`, `TrySetOperationalLocation`,
  `TryAssignCommander`, `TryAddRelevantPerson`, `TryRegisterContingent`,
  `TryReplaceContingent`, `TryTerminate`; position writes
  `TrySetPosition` / `TryClearPosition`. Manpower commits:
  `TryRegisterContingent`, `TrySetSourceBinding`, `TryAllocate`,
  `TryDemobilize`, `TryRedistribute`, and internal `TryCommitBattleBatch`.
- Conflict/war/battle commits include each store’s `TryRegister`,
  `TryAddParticipantBinding`, and `TryEnd` / `TryStart` as applicable,
  terminal battle application, and successful
  `ConflictResolutionService.TryResolveAndApply` / `TryApply`. `Compute` is
  proposal/computation only.
- Political/property commits include faction `TryRegister` /
  `TryRegisterAffiliation` and internal affiliation applies; claim
  `TryRegister` and recognition/resolution applies; support `TryRegister` and
  add/end applies; political Knowledge holder/record/observation writes;
  property `TryRegister` / transfer apply; and estate opening/register through
  `EstateOpeningSystem.TryApplyOpening` / `TryOpenEstate`.

**P12-F Knowledge and commitments**

- Per-NPC Knowledge writes:
  `AdventureSiteIntelKnowledgeRuntime.RecordObservation` overloads;
  `ExplorableSiteKnowledgeSystem.RecordInitialScenarioKnowledge` /
  `RecordDirectObservation`; and
  `ExplorableSiteKnowledgeRuntime.RecordObservation`.
- Directives: `ScheduledDirectiveStore.Add`,
  `ScheduledDirective.MarkSucceeded` / `MarkFailed` / `MarkSkipped`, and
  successful selection/processing through `ScheduledDirectiveSystem.PrepareDay`
  / `TryTakeDirective` plus the processing path. A no-directive query or
  uncommitted preparation is not a write.
- Actor choices: `ActorChoiceStore.TryCapture`, `TryCaptureTemporal`,
  `TryDefer`, `TryReject`, `TryMarkDispatchStarted`,
  `TryRecordAttemptReturned`, `TryRecordAttemptThrew`, and temporal disposition
  variants.
- Travel: `TravelSystem.TryStartTravel` overloads and `AdvanceTravels`;
  travel parties: `TravelPartyStore.Add`, `Complete`, `Remove`,
  `TravelPartySystem.TryStartTravelParty`, and `AdvanceParties`.
- Expeditions: `ExpeditionStore.Add` / `Remove` / `Complete`;
  `ExpeditionSystem.TryStartExpedition`, `TryBeginExploration`,
  `TryContinueExploration`, `TryExploreLocalPlace`,
  `TryTraverseLocalConnection`, resource/item retrieval, opposition
  resolution, `TryBeginReturn`, and `ReconcileAfterTravel`; direct
  `ExpeditionRuntime` progress/commitment changes also require closure or an
  owner-notifying seam.

The implementation review must check the complete method matrix against the
actual changed diff, including internal `TryCommit` sites and public leaf
mutators. Audit-only decisions, reads, `Can*`, `TryCreate*`, `TryPrepare*`,
`TryPropose*`, and full rollback are not commits. A successful compensation
that changes world truth is a commit and invalidates.

## Dependency-safe capability sequence

These are work packages inside accepted P12-B–G, not new checkpoint IDs.
Every consumer uses one reviewed owner-witness and invalidation contract; do
not add a second lease, global lock, or duplicate authoritative store.

1. **B source map and live census evidence — substantially advanced, not
   complete.** The audits provide the selected composition, method-level
   C/D/E/F writer matrix, source-level thread/quiescence limits, and zero-role
   classification. The selected-profile day-zero test now reads City/NPC,
   population, market, inventory, per-NPC spatial-Knowledge, Person/Genealogy,
   P8-A–D, receipt, directive, ActorChoice, travel and expedition values from
   the published runtime. Hidden C roots and several E/Justice owners remain
   without census APIs; direct collection counts without a revision are not
   B witnesses. Independent exact-tip review must confirm the map omissions
   before it can schedule owner instrumentation.
2. **B coordinator protocol and conditional receipt witnesses — implemented
   as a non-admitting feature candidate.** The protocol defines versioned
   owner-section evidence (owner identity, role, schema, exact count, local
   revision/snapshot stamp), one monotonically invalidating epoch,
   owner-thread binding, and operation-scope accounting. Missing adapters or
   inconsistent witnesses return `OwnerCoverageIncomplete`; no capture token
   is issued. The receipt owners expose exact live evidence, but no profile
   census is registered and runtime admission is not wired. Do not wire
   `SimulationRuntime` or claim readiness until the remaining owner groups
   below have reviewed witnesses. One successful outer commit that changes several owner
   sections validates and refreshes all changed witnesses before advancing
   the shared epoch exactly once; partial/duplicate/unknown notifications fault
   closed. Baseline assessment also validates the full section set before
   publishing its new baselines. The canonical P18 advance lease remains the
   only advance lease.
3. **P8-C exact-zero witness adapters — completed and promoted.** The passive
   schema-v1 providers for `p8c.city-site-location-bindings` and
   `p8c.person-positions` use the installed cloned runtime stores as owner
   identities. Their targeted mutation tests and selected-profile exact-zero
   assertions passed; they do not alter P8 mutation semantics, register into
   the incomplete profile protocol, emit epoch notifications, or expose staged
   source stores. See `docs/design/PHASE12_P8C_CENSUS_CANDIDATE.md`. These
   unsynchronized providers remain unusable for capture until
   owner-thread/quiescence proof.
4. **C owner witnesses/exports:** allocator/registry/record-sequence/RNG
   roots, P9-B genesis provenance, and exact P8-A facts. Capture exact
   identity/index state and provider-use state without rerunning genesis.
   Integrate only after the B protocol is promoted, as required by the Brief.
5. **D factual owner witnesses/exports:** City/NPC roots and child owners,
   population/Person/Genealogy lifecycle, legacy network/sites/anchors. Split
   work by owner and hotspot only after the reviewed B API; birth,
   materialization, death and migration must have a single outer in-flight
   scope and successful-commit notification.
6. **E core and official provider witnesses/exports:** economy and merchant
   owners, justice/crime/appraisal, and selected political/institutional/
   property/military/conflict stores. Develop alongside D only in isolated
   owner worktrees; integrate against the same B contract and actual effective
   provider set.
7. **F knowledge/commitment witnesses/exports:** directives, P11 choices,
   Knowledge, travel/party/expedition. Begin implementation only after C, D,
   and E canonical dependencies are met; preserve exact terminal ActorChoice
   state and reject nonterminal states as already specified.
8. **B runtime profile proof, then G:** compose the exact census from the
   normal P9-B/P11 bootstrap; prove thread/scope rejection, post-commit
   invalidation, positive required cardinalities, exact conditional zeros,
   absent-section classification, and revalidation after collection. P12-G
   performs private staged hydration, whole-graph validation and publication
   only after owner exports/hydrators and this live inventory are complete.
   P12-A remains `WAIT_DEPENDENCY` and separately gated.

The accepted Brief orders B before C, C before D/E, and D/E before F; G waits
for B–F plus current live-profile inventory. D/E may prepare only in isolated
work against the reviewed B contract. The fact that B’s first coordinator
candidate fails closed before all owner adapters exist is intentional; it
does not make owner groups ready on an unpromoted API.

### Bounded owner-witness capabilities completed off the runtime hotspot

The accepted P12-B census work now has owner-issued witnesses for six
conditional exact-zero sections, without editing `SimulationRuntime`, its
daily advance wrappers, `SimulationTime`, or the P18 lease seam:

1. `NpcDecisionRecorder.occurrenceReceipts` reports section/schema identity,
   concrete recorder-instance identity, exact count, and local revision. The
   prior `codex/phase12/P12BOwnerReceiptCensus` candidate at `2ef4a83` passed
   exact-tip review and successful, failed, and replayed receipt evidence.
2. `EconomyTransactionService.keyedSaleReceipts` now reports the same fields
   from its receipt owner. Every new ledger entry and replacement advances
   revision; replay and fingerprint-collision reads do not. The selected
   bootstrap publishes only this detached witness, not the service handle.
3. The live selected-profile bootstrap test observes exact zero and stable
   owner identity for both receipt sections. `EconomyTransactionTests` passes
   45/45; the bootstrap test passes 1/1. These two sections do not certify the
   rest of the census, connect commits to the shared P12-B epoch, or issue a
   capture token.
4. P8-C provides installed-owner witnesses for City/Site anchor bindings and
   Person positions. `PersonSpatialPresenceTests` passes 9/9 and the selected
   bootstrap profile asserts both exact-zero identities and revisions. This
   work is promoted at `481358d1f8967d1c0199370601597c329fce69b2`.
5. P8-B provides an installed passage-child witness and a parent crossing
   witness. The selected profile asserts exact zero at parent revision 1;
   passage tests cover successful and rejected mutations, condition changes,
   and separate crossing cardinality. `SpatialPassageAuthorityTests` passes
   13/13, bootstrap composition 14/14, ALL EditMode 1954/1954, and Smoke 5/5.
   This work is promoted at `04d39b23b8509609dcd96990a214922dc0220e8b`.

These bounded slices add only passive witnesses and their tests. None edits
the P18-owned `SimulationRuntime` hotspot or connects to the shared epoch.
They do not establish a complete census, owner-thread/quiescence, or P12-B
readiness.

The source audits also give the bounded design for later runtime proof:
bind owner-thread identity at completed bootstrap, expose active-operation
quiescence around the existing outer advance lease, close direct clock
advancement after binding, and scope every supported multi-owner operation.
The P12-B technical design currently forbids editing those runtime hotspots
until its complete owner/revision census and committed-mutation mapping gates
are cleared. Therefore this is **not** part of the first implementation
slice. Preserve that gate; do not widen or replace P18's single lease.

After B's reviewed protocol is stable, owner census/revision adapters and
commit notifications are prepared under C/D/E/F on isolated owner branches.
Only after the documented dependency edges are met and B's hotspot gate is
reconciled may runtime thread/quiescence enforcement be integrated. Until
every owner adapter exists and every direct committed write is covered, the
eligibility coordinator remains fail-closed and issues no token.

## Runtime proof requirements

The current `SimulationRuntime.advanceLeaseHeld` is an unsynchronized
reentrancy guard around advance calls. It is not thread affinity, a mutation
epoch, or capture quiescence. Public `SimulationTime.TryAdvanceDay` bypasses
the complete outer advance path. `SimulationRuntime.Cities` exposes the
backing list; the City-owned `ImportantNpcs` view has a local revision but
does not provide a City/NPC composite revision. Multiple NPC child values are
mutable. Person birth/materialization/death/migration and other owner
operations may cross several authorities without a capture-wide scope.

The source inspection found no explicit spawned worker/thread/async/coroutine
in the selected update path, but that only describes the path inspected. The
runtime proof must bind the actual Unity owner thread at successful bootstrap;
reject capture from any other thread; track bootstrap, the outer daily advance,
and each supported multi-owner operation; invalidate immediately after each
committed write; and reject if an owner can mutate outside a registered,
observable path. Do not treat “no async found” or the P18 lease as proof.

At capture, issue no token unless all required witnesses have current matching
owner identity, role/schema, exact count and revision; no mutation occurred
since the successful daily boundary; the owner thread matches; no advance or
other supported operation is active; and the post-collection witness set is
identical. A zero count without a live owner and revision is not a witness.

## Readiness result

The complete **static** source map remains a bounded P12-B evidence artifact;
it is not a complete live census or mutation-completeness proof. The owner-local
receipt witnesses and P8-A–D witnesses described above are promoted on
canonical. The live run verifies P8-A positive cardinalities and P8-B/C/D
exact-zero identities on the published day-zero runtime, but it does not
invoke a Unity `Update` frame or establish thread ownership/quiescence. Other
C/D/E/F owners and committed-write coverage remain incomplete.

The follow-on `codex/phase12/P12BCoordinatorProtocol` candidate adds a
standalone, non-admitting protocol kernel for versioned section requirements,
owner-backed providers, fail-closed coverage assessment, a one-way owner
thread binding, committed-mutation epoch notifications, and accounting scopes
for explicitly registered operations. Its focused suite exercises missing,
duplicate, unexpected, wrong-schema and wrong-owner evidence; exact-zero roles;
unnotified revision drift; commit notification; nested scopes; and off-thread
rejection. The kernel is not connected to the selected bootstrap or runtime,
does not issue tokens, and cannot assert that a caller's inventory is complete.
Its setup and fixture results do not establish P12-B readiness.

Owner-local adapters and commit notifications for C/D/E/F still require their
documented dependency order and exact reviewed B API. The live profile still
needs the complete actual owner/cardinality set, every supported committed
write mapped to a notification or a closed boundary, and the exhaustive
owner-thread/operation inventory. Do not edit `SimulationRuntime`,
`SimulationTime`, or the P18 advance lease until those gates are satisfied.
P12-G and P12-A remain blocked until the complete owner witness set is
demonstrated on the normal bootstrap and all required downstream exports and
hydrators exist.

### P8-D witness promotion — 2026-09-29

The reviewed P8-D passive route-owner census candidate was approved and
fast-forwarded from P12 canonical `c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2`
to candidate tip `d92fdfb6b5ceb517c210be7cea5faab52ebb5641`. Its code-bearing
commit is `f0575ef43a77898aae8fb8565d4b709b850a46d8`; independent exact-tip
review passed, with the durable record at
`codex/phase12/P12BP8DReviewRecord` tip
`934741142cd74e06d2c284af45a62a068fa3d9de`.

P8-D now has passive schema-v1 witnesses for installed route Knowledge and
route-plan stores. The exact-zero selected-profile values and mutation/replay/
history cardinality behavior passed the focused suites, ALL EditMode and
official complete Smoke; exact result paths are in
`docs/design/PHASE12_P8D_CENSUS_CANDIDATE.md`. This does not provide complete
P12-B owner coverage, shared committed-write invalidation, owner-thread or
quiescence proof, or capture eligibility. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.

### Cumulative passive owner-witness extension promoted at `b889b47`

The reviewed cumulative census stack was promoted to P12 canonical at
`b889b4747738d933fe48311ef89fc33a40e3dfa0` from base
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`. Its code tree
`65ebc7f7ab6dd834e2326ff426600483a74c162c` passed the complete EditMode
suite `1985/1985`, official complete `Smoke` `5/5`, and `git diff --check`.
Independent exact integration review passed at `35ec988`; the final
docs-only candidate tip check passed at `b889b47`.

In addition to earlier receipt, P8, and RuntimeIdentity witnesses, the stack
adds passive owner/cardinality witnesses for `SimulationRecordSequence`,
`ActorChoiceStore`, `RuntimeIdAllocator`, `ArmedForceStore`, contingent
manpower, armed-force position, persistent Conflict/War/Battle, Estate and
Property, and Institution/Office. Their slice-level contracts and focused
validation are linked from `docs/PHASE12_STATE.md` and the corresponding
`docs/design/PHASE12_*_CENSUS_CANDIDATE.md` records.

This promotion improves owner inventory evidence only. The selected-profile
census is still incomplete; there is no shared committed-write invalidation
for all supported owners, owner-thread/quiescence proof, or capture token.
City/NPC composite owners, Person/Genealogy/population, economy and Justice/
Crime, remaining Knowledge/commitment owners, and the accepted export and
staged-hydration work remain open. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.

### Dynamic NPC SpatialKnowledge family promoted at `0021b0a`

With explicit approval, the reviewed candidate was promoted on the local P12
canonical branch from `0a37e9f` to `0021b0a`; executable code is `2c782a7`. It follows the installed
NPC roster with exact per-owner SpatialKnowledge identities and combines
family changes with the affected PersonStore sections at supported roster and
materialization/adoption boundaries. Exact-tip review and validation are
recorded in `docs/design/PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md`
and the linked candidate note.

The cross-thread correction is local to this membership context: mismatched
nested entry faults the partial census and cannot join or reconcile through
the owner thread's live context. It does not establish runtime-wide owner
thread binding or quiescence. City `ImportantNpcs` now has its separate
passive local witness; SpatialKnowledge content changes, direct Inventory
write invalidation, unrelated PersonStore writers, remaining profile owner
sections, shared-write invalidation, and capture eligibility remain blockers.
P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.

### Dynamic per-NPC Inventory witness promoted at `15e5a54`

With explicit approval, candidate
`codex/phase12/P12BNpcInventoryCensus` was fast-forwarded from canonical
`f961477` to `15e5a54`. Each currently rostered NPC contributes one exact
schema-v1 Inventory witness for its installed `InventoryRuntime`. The family
reconciles with the existing dynamic SpatialKnowledge and fixed PersonStore
sections at the supported roster/materialization boundary. Same-roster owner
replacement faults census rather than rebinding a section. Independent review
passed against the exact base and candidate tip; its durable record is on
`codex/phase12/P12BNpcInventoryCensusReviewRecord` at `f98c5d4`.

This closes only the missing per-NPC Inventory passive owner witness. It does
not connect `InventoryRuntime.AddItem`/`RemoveItem` to the shared epoch, prove
the composed City set is complete, cover out-of-composition `StartingCity`
references, or add owner-thread/quiescence, capture, export, or hydration.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P12-B Market operation invalidation — 2026-10-01

This matrix addendum updates the Market-related D/E writer rows above. The
reviewed candidate was based on P12 canonical
`b8a7da54864bee3fb9b8916793240e91fbce0955` and, with approval, its reviewed
bundle was fast-forwarded to `codex/phase12/canonical` at
`b77e154e86b510c9f47ea9042fe5a1edb39749b0`. Independent
exact-tip code review PASSed for candidate `b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`
(tree `88ec452a979a7439b825858a6b1b271f8bc6c948`); durable review evidence is
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_REVIEW.md`. The State,
candidate, and matrix refresh from the candidate branch are retained as
durable promotion evidence.

| Bounded writer family | Reviewed coverage in the promoted code | Current limit after this promotion |
|---|---|---|
| `runtime.economy.market-purchase` / `runtime.economy.market-sale` | The composed exact City Market owners are registered before profile sealing. Bound Open-market purchase/sale preflight the exact rostered NPC account and Inventory plus Market identity/revision before owner writes; each committed account, Inventory, Market revision, and successful compensation is reported within the named operation scope. The exact Market wrapper service is shared with `MerchantSystem`. | This covers these two Market transaction families only. Other EconomyTransactionService families, Merchant plan/Knowledge effects, travel charges, and account-backed City/Counterparty paths remain outside this operation slice. It is not a complete E-owner map or capture boundary. |
| Direct Market owner mutators | `AddStock`, `RemoveStockUpTo`, and changed `UpdatePrices` preflight the owner thread and exact Market baseline, then report the successful Market revision. Price refresh refuses a changed-price commit when the revision is exhausted. Tests verify changed-price revision/epoch advancement and no-op stability. | Direct Market calls do not require an active named operation scope. They notify only their Market section and do not prove a universal boundary around all public child-owner writes. |
| Daily City production, Free consumption, and price refresh | The existing synchronous day path runs inside `runtime.advance-day`; Market commits notify their owner while that scope is active. Candidate tests verify production, Free-consumption, and changed-price daily commits each advance the Market revision and partial epoch. | This is the selected daily path only. It does not cover every `CityRuntime`/Market caller, every supported owner, or operation completeness outside the scoped day advance. |

This is a reviewed partial shared-epoch extension, not complete shared-epoch
coverage. The exact validation counts and XML/log hashes are recorded in
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_CANDIDATE.md`. P12-B
remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. No
complete owner coverage, capture eligibility, export, or hydration is
claimed.

## Current post-Market P12-B blocker classification — 2026-10-01

| Classification | Current evidence and remaining blocker | Readiness |
|---|---|---|
| **OWNER_CENSUS_GAP** | The partial coordinator seals 144 selected-profile day-zero sections: the prior 142-section set plus two exact Market sections added by the promoted Market-operation candidate. This is not the complete effective-profile owner set. Many promoted passive witnesses remain outside sealed registration; City/NPC composite facts and remaining Justice, Crime, economy, and other mutable owners remain gaps. | **DESIGN_REQUIRED** to reconcile the complete live owner/cardinality set; do not repeat already promoted Market or per-NPC MoneyAccount census. |
| **OPERATION_COVERAGE_GAP** | Promoted bounds cover NPC trade, Open-market purchase/sale, direct Market commits, selected daily Market commits, bootstrap publication, advance, and NPC membership. Direct per-NPC MoneyAccount/Inventory writes, other `EconomyTransactionService.Try*` families, Merchant commits, travel charges, and other D/E/F operation families remain uncovered. | **DESIGN_REQUIRED** for bounded uncovered writer and outer-operation mapping. |
| **SHARED_EPOCH_GAP** | Existing notifications cover the exact reviewed NPC-trade and Open-market owner commits, selected direct/daily Market commits, and supported membership reconciliation. `MoneyAccountRuntime.TryCredit/TryDebit` and `InventoryRuntime.AddItem/RemoveItem` advance local revisions without shared-epoch notification. Nested execution must avoid double notifications when these writes occur inside instrumented trade/Open-market paths; prepared installs need explicit classification. | **DESIGN_REQUIRED** for the reviewed direct NPC-owner invalidation contract. |
| **ADMISSION_QUIESCENCE_GAP** | The selected `UnityBootstrap-Daily-v1` adapter binds its Unity owner thread and scopes publication/advance. It is not exhaustive runtime-wide proof for every writer or cross-owner operation. | **WAIT_DEPENDENCY** on complete owner and operation inventory/coverage. |
| **CAPTURE_ELIGIBILITY_GAP** | No `CaptureEligible` or token API exists. Complete owner/cardinality registration, supported-write coverage, healthy idle admission, and stable before/after witnesses are not established. | **WAIT_DEPENDENCY** on the preceding census, operation, and quiescence obligations. |
| **OTHER_TRUE_BLOCKER** | P12-A still needs validated live-profile inventory, complete owner export/staged hydration, and its separate implementation authorization. P13 remains blocked on P12 continuation and recoverable causal history. | **WAIT_DEPENDENCY**. |

Historical snapshot — 2026-10-01 canonical: direct per-NPC MoneyAccount
and Inventory write invalidation was the next bounded design target. The
candidate and exact review now recorded in the addendum below address that
target; this paragraph is retained only to preserve the earlier blocker
history. The next operation design is the separate NPC money-transfer scope,
which remains dependency-gated on promotion of those direct-owner hooks.

### Reviewed P12-B runtime-admission adapter — implementation boundary

The earlier hotspot hold above predates the refreshed, independently
reviewed design `docs/design/PHASE12_P12B_RUNTIME_QUIESCENCE_DESIGN.md` at
`e5a32b8ec226204e751e1da41dfcf2546a760718` and the explicit acceptance of the
P12-B prerequisite scopes. For this bounded adapter only, that reviewed design
now authorizes work in `SimulationRuntime`, `SimulationTime`,
`ContinuationCensusProtocol`, and the selected bootstrap entry.

The permitted slice is limited to `UnityBootstrap-Daily-v1`: capture and bind
the actual Unity Start-thread identity, account for the synchronous validation
tail through successful publication, scope nonzero daily and multi-day runtime
advances, and route that profile's runtime-owned direct clock calls through
the same daily operation. The P18 projected-clock path remains unchanged. This
adapter adds no owner sections, shared mutation-epoch wiring, generic
registration, or capture token.

This addendum supersedes the earlier sentence forbidding all edits to those
runtime hotspots only for the named adapter slice. It does not resolve the
remaining live owner/cardinality inventory, committed-write invalidation,
unaccounted-operation, or full-profile eligibility blockers. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`; no downstream checkpoint is
made ready by this adapter alone.

### Selected-profile ExplorableSite census revalidation — 2026-10-01

The existing
`SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
now reads `ExplorableSiteCensusProvider` from the published composition. It
asserts section/schema, the exact installed `ExplorableSiteStore` identity,
cardinality 0, revision 0, and stable owner identity/cardinality/revision on a
second read. The test-only change is commit `20861f32e48f2f8b794fa44e10cf63fa7e6f0d05`.

Validation on that code/test tree passed: the selected-profile test 1/1, ALL
EditMode 2107/2107, official Smoke 5/5, and `git diff --check`. XML/log
artifacts are retained under `Library/ValidationResults/P12BExplorableSiteProfile`,
`P12BExplorableSiteAll`, and `P12BExplorableSiteSmoke` respectively. The XML
SHA-256 values are `24cabb4a35733455e9f4adc77a3785486f3e6c281a9ffccbf4618d21e88bb6fa`,
`3f7f97b87696c615979a85daa864444713001acc2af4c32191c0b0f321f77d35`, and
`c4f5796e8b5748cd953f0e86e6b940ad08c05095be3d07e771181571ade2f2df`;
the corresponding log hashes are `e7ef9e297cd7ff3b65e2209334ad52fa81c20aab04240174bb98ff99a95d33e0`,
`dae046a9bf06d2c97cbfda295069fa98d8e1c09e44d54fc1760a2fffc760fdfd`, and
`ba694751cf592accea36d6eaa6e5ca73d0524b017403c2e68f234d174b4a4eae`.

This closes the exact-zero ExplorableSite witness at selected-profile day
zero. It does not prove evolved-boundary cardinality, register the section in
a complete P12-B census, connect writes to the shared epoch, or establish
capture eligibility, quiescence, export, or hydration. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`.

### Per-NPC MoneyAccount census promoted at `9f615d8`

The reviewed partial census now publishes one exact installed `MoneyAccountRuntime` identity/cardinality/local-revision witness per currently rostered NPC. Candidate and exact-tip review evidence are linked from `docs/design/PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_CANDIDATE.md` and `docs/design/PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_IMPLEMENTATION_REVIEW.md`.

This closes only the per-NPC passive identity/cardinality census gap. `TryDebit`/`TryCredit` remain direct child writes with local revision only; they are not connected to the shared protocol epoch. The promotion does not establish complete owner coverage, committed-write invalidation, quiescence, capture eligibility, export, or hydration. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.



## Direct NPC owner-write invalidation candidate refresh

This addendum refreshes the post-Market writer and operation classification
against P12 canonical 2f7c7422812de40aa1223e8310dcbd9f5d8ca474 and approved
architecture baseline 451340c56e9b676bf6ea43412bcb856b9ccde3de.

| Obligation | Current evidence | Remaining gap / readiness |
|---|---|---|
| Direct per-NPC owner writes | Candidate c49f957e45c3059231e9ec66e4010a7c3a389988 (tree 627af2fbd7f93e0025106ee5ba87e72bae6c4ed2), based on P12 canonical 2f7c742, passed exact-tip independent review and required validation. It preflights exact owner identity, owner-thread, local revision and shared protocol baseline; direct account debit/credit and Inventory add/remove notify once after commit; roster rebinding and existing trade/Open-market duplicate suppression are covered. | Validated candidate is not canonical until its separate human promotion gate. It does not close P12-B or establish universal shared-epoch coverage. |
| NPC money transfer | Reviewed bounded design 30c00d78fcb68c2969ac2eac3ed694423c55783e and durable exact-tip review 884aa1b27f1de3a3d330eb75053bab01ffec7cc2 on codex/phase12/P12BMoneyTransferOperationDesign. | WAIT_DEPENDENCY on canonical direct per-NPC owner hooks. After that promotion, refresh the contract against the promoted tip and implement the named multi-owner transfer boundary, including compensation. No current implementation or transfer coverage is claimed. |
| Other EconomyTransactionService and D/E/F writers | Existing NPC trade, Open-market operations, selected Market direct/daily commits, and candidate direct NPC account/Inventory writes are separately bounded. | Other transfer/travel/merchant/crime/justice/population/knowledge and public writer families remain uncovered; no complete operation inventory or capture boundary. |
| Owner inventory and quiescence | Promoted partial day-zero census and runtime admission evidence remain. | Complete effective-profile owner coverage, global owner-thread/quiescence, stable capture eligibility, export and hydration remain absent. P12-A stays WAIT_DEPENDENCY. |

This candidate adds no owner family and no product behavior. It does not infer
that every P12-supported operation participates in the shared epoch. P12-B
remains incomplete and P13 remains blocked.

The approved WorldId/factual-projection architecture at
451340c56e9b676bf6ea43412bcb856b9ccde3de does not change this candidate's
account/Inventory mutation semantics. The WorldId design is an independent
capability track; WI-A and P12 must nevertheless serialize integration work on
SimulationRuntime. Candidate integration against the updated runtime is a
revalidation obligation, not a reason to discard this direct-owner candidate.


## Current P12-B operation/epoch matrix — 2026-10-02 post money-transfer promotion

This addendum supersedes older candidate-status rows below where they describe
direct per-NPC MoneyAccount/Inventory invalidation or the NPC-to-NPC
money-transfer boundary as unpromoted or uncovered.

| Area | Current canonical evidence | Remaining blocker |
|---|---|---|
| Per-NPC MoneyAccount and Inventory direct writes | Promoted with integration tip `9d1474b4299d8e888dd387e02e9018d9e8627f84`; code `c49f957e45c3059231e9ec66e4010a7c3a389988`, tree `627af2fbd7f93e0025106ee5ba87e72bae6c4ed2`. Exact per-NPC owner identity, local revision, owner-thread and shared protocol baseline are preflighted; successful direct commits notify the partial epoch once, with duplicate suppression inside the separately scoped NPC trade and Open-market operations. Exact-tip review and focused/full validation are retained in the candidate/review records. | No other owner families or unscoped cross-owner transactions are covered. This is not complete shared-epoch coverage or P12-B readiness. |
| NPC-to-NPC money transfer | Promoted in implementation candidate `codex/phase12/P12BMoneyTransferImplementation` at P12 canonical `2f2b731866aea86eb52ef2b51eb687c88bf91bc4`; code `66415aefe6834fc73e9e6c3b22fe0ee3058cfa11` (tree `862bceb6164bf5ddeed49ca8007d9be3ed4670d6`), exact-tip independent review record `09f9f49ef85faf5c24eae57acba4426ca0bc39f8`. It implements reviewed design `ea1d5b58ab8c5204c7559d13be10034e3a8732eb` / design review `74dc9b5360fb356eca58874878942ee90b786fd8`: stable operation ID, exact rostered source/destination MoneyAccount sections, one named scope across debit, credit, compensation, and result selection; P12-bound raw-account overload rejects before write; finite zero transfer remains a successful no-op. Focused tests 28/28, 45/45, and 12/12; ALL EditMode 2152/2152; official Smoke 5/5; `git diff --check` PASS. | Only the exact NPC-to-NPC transfer boundary is covered. Other EconomyTransactionService methods, Crime outcome/reverse transfer, other account owners, complete owner/operation inventory, universal shared-epoch coverage, capture, and P12-B readiness remain unresolved. |
| Remaining selected-profile owner inventory | The promoted census and writer evidence remain partial; several passive providers remain outside sealed protocol registration, and City/NPC composites plus Justice, Crime, remaining economy, and other mutable owners have not been shown complete. | Complete live owner/cardinality census and reconcile it with the exact supported profile before capture claims. |
| Other operation families and shared epoch | Existing bounded coverage includes NPC trade, Open-market purchase/sale, selected direct/daily Market writes, direct per-NPC account/Inventory writes, the exact NPC-to-NPC money transfer above, selected publication/advance scopes, and supported membership reconciliation. | Other EconomyTransactionService methods, merchant plan commits, travel charges, Crime/Justice, Knowledge, and other supported public writers remain uncovered. Avoid duplicate or universal claims. |
| Runtime admission, quiescence, capture | WI-A is canonical at `93b6f0e8cedcb63437cbf5fa5de3461853c81462`, based on promoted P12 tip `9d1474b4299d8e888dd387e02e9018d9e8627f84`; its implementation/review is retained. Combined P12/WI-A hotspot revalidation passed at exact WI-A code `3b39e0d89858dce517ad72cbb76da621eb954bad`; durable record is `codex/phase12/P12BWIARuntimeHotspotRevalidation` tip `313095ecec87b11928708607821c6b9ad9b5e275`. | This validates only the WI-A/P12 composition. FR-B needs its own serialized runtime integration and exact Faction/Person read-cut proof. No global quiescence proof or capture token exists. |
| Downstream continuation | P12-A remains WAIT_DEPENDENCY; P12-C through P12-G retain their documented prerequisite edges; P13 remains blocked. | Do not infer readiness from either promotion. |

At that matrix baseline, the promoted direct-owner and NPC-to-NPC transfer
slices remained bounded. The following post-Merchant refresh supersedes its
then-next-operation note; other uncovered supported writers remain to be
enumerated without broadening any promoted operation.

## Current P12-B owner/operation matrix — 2026-10-02 post FR-B core promotion

This refresh uses P12 canonical code tip
`451d06e62b7c95d7b600b77f10b0510049d62961`, after the approved bounded FR-B
core promotion. It retains the post-Merchant operation map and adds the exact
FR-B core result and current WI-A revalidation status.

| Area | Current canonical evidence | Remaining blocker |
|---|---|---|
| Per-NPC MoneyAccount/Inventory direct writes | Promoted at integration tip `9d1474b4299d8e888dd387e02e9018d9e8627f84`; exact identity, local revision, owner-thread, protocol baseline, post-commit invalidation, and duplicate suppression within the separately scoped NPC-trade/Open-market operations are bounded there. | No other owner families or unscoped cross-owner transactions are covered. |
| NPC-to-NPC money transfer | Promoted at `2f2b731866aea86eb52ef2b51eb687c88bf91bc4`, code `66415aefe6834fc73e9e6c3b22fe0ee3058cfa11`, review `09f9f49ef85faf5c24eae57acba4426ca0bc39f8`. | Only the exact source/destination MoneyAccount transfer, compensation and result boundary is covered. Other economy methods, Crime/reverse transfer, travel charges and other account owners remain open. |
| Selected daily Merchant operation | Promoted at `5e643b7e4535b5bb699eb021d3b313243bf45658`; code/test tip `3ed113bf35546964cd4a56f424578dc1213f41fa`, tested tree `876e810757c55b098d805e6e9ac012da7e6cab4d`, exact-tip review `5f4fd440720ff0d9e5461edab06a299a9a1a6830`. It scopes only the existing normal `MerchantSystem.AdvanceNpcTradeState` call and admitted/batched mutations for its reviewed Merchant-owned Knowledge and plan owners. Direct supported owner-thread plan writes outside the nested operation refresh the corresponding baseline and invalidate the changed plan owner. | The global decision allocator, record sequence and decision read model are outside this slice. This does not cover every Merchant entrypoint, unrelated Knowledge owner, travel charge or other public writer. See the candidate evidence for the exact owner identities/cardinalities and tests. |
| Remaining selected-profile owner inventory | Promoted passive witnesses and writer evidence remain partial; several providers are outside the sealed protocol, and City/NPC composites plus Justice, Crime, remaining economy and other mutable owners are not shown complete. | Reconcile the full live owner/cardinality set with the accepted profile and exact supported writers before any capture-readiness claim. |
| Runtime admission/quiescence and capture | WI-A canonical remains `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5`. Current-base revalidation candidate `codex/wia/P12CurrentRevalidation` is code `19bcf3415555b5ed336664e9dee925d4497b219c` (tree `5b82e0e67a2f454850c4a8da70729443016c61c4`), evidence/review tip `5858029d811e733dc9e2252eb800cf69fb101ddf`; six focused/full gates and exact-tip review passed against prior P12 canonical `d6e52dc`. | P12 canonical has since advanced to `451d06e` with FR-B core. Preserve the candidate and rebase/revalidate it against the current tip before relying on or promoting the combined integration. It does not prove FR-B's exact read cut, global quiescence, or capture eligibility. |
| FR-B factual-read core | Promoted at P12 code tip `0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` (tree `09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`), canonical promotion/evidence tip `451d06e62b7c95d7b600b77f10b0510049d62961`; exact review, focused 8/8, ALL EditMode 2168/2168, and Smoke 5/5 are recorded in `docs/design/FRB_FACTUAL_READ_FOUNDATION_CANDIDATE.md`. It adds contracts, admission, coordinator, and FactionStore/PersonStore read-time guards. | No production `SimulationRuntime`/bootstrap reader composition or exact selected-profile Faction/Person read cut exists. Readers must supply immutable copied facts. This is not complete profile coverage, shared-epoch coverage, capture eligibility, export/hydration, or P12-B readiness. |
| Downstream continuation | P12-A remains `WAIT_DEPENDENCY`; P12-C remains blocked on P12-B; P12-D/E/F/G retain their documented prerequisite edges; P13 remains blocked on promoted continuation plus recoverable causal inputs and initial-world/mutation semantics. | Do not infer readiness from the Merchant operation, FR-B core, WI-A identity publication, or passive census evidence. |

The Merchant and FR-B promotions cover only their bounded operations/core.
They do not establish complete owner or operation coverage, complete
shared-epoch coverage, capture eligibility, export, hydration, P12-B
completion or P13 readiness. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, and P13 remains blocked.

## Current P12-B owner/operation/runtime matrix — 2026-10-02 post WI-A integration

This refresh uses P12 canonical integration tip
`ae0a0b4fed921dfb3fd7e173a4787309120bece2`, after the approved WI-A
World Identity current-base integration/revalidation. It supersedes the
previous row that described the old-base WI-A candidate as awaiting refresh.

| Area | Current evidence | Remaining blocker |
|---|---|---|
| WI-A/P12 bootstrap composition | Promoted code `242ae6bf81c2f4da832be1e7948bbaac004a620b` (tree `284e7a9a229671419c4ea0c545c02b4513e41717`) is based on P12 canonical `66f91c68d367e03703ab014046d5e62c3f89ebbe`. Exact-tip independent review and current-base focused/full validation passed; the durable record is `docs/design/PHASE12_P12B_WIA_POST_FRB_REVALIDATION.md`. | This is a bounded World Identity publication/composition integration. It does not establish full owner coverage, universal shared-epoch coverage, runtime-wide quiescence, capture eligibility, or P12-B readiness. |
| FR-B factual-read core | Promoted core remains at code `0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` (tree `09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`), canonical evidence tip `451d06e62b7c95d7b600b77f10b0510049d62961`. | Production runtime composition and selected-profile Faction/Person read-cut work remain a separate WI/FR workstream. They are not delivered by WI-A integration and do not use the partial P12 epoch as global coherence. |
| Remaining owner/operation/epoch coverage | Promoted census and operation evidence remains partial, including only the bounded operation families listed above. | Reconcile remaining live owner/cardinality, committed-write invalidation, shared-epoch, admission/quiescence, and capture-eligibility gaps. Do not default to another passive census when an operation/admission blocker is higher value. |
| Downstream continuation | P12-A remains `WAIT_DEPENDENCY`; P12-C through P12-G retain their explicit dependencies; P13 remains blocked. | WI-A integration does not imply persistence, save/load, fork semantics, P12-A readiness, P12-B completion, or P13 readiness. |

The WI-A promotion changes no P12-B readiness label. P12-B remains
incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.

## Current P12-B blocker refresh — 2026-10-02 post record-sequence promotion

The current canonical base is `e64caf08e7ada24a0f6b8c193207a6242018896d`.
The prior post-WI-A matrix above is historical at `ae0a0b4`; this addendum
refreshes only the affected record-sequence and queued FR-B rows while
retaining the wider owner/operation audit and its known omissions.

| Area | Current evidence | Remaining blocker |
|---|---|---|
| Selected-profile `SimulationRecordSequence` | Promoted code `cd7ca4498d2c1d3591c227bd9011429f9bd06d8f`, exact tree `5282d65fbc4311bb6b907770a4e6fa363ad633df`; exact-tip review and focused 6/6, ALL EditMode 2180/2180, Smoke 5/5 validation are retained in the candidate/review records. Successful selected production-path `Allocate()` calls invalidate the sequence owner and partial shared epoch once, including nested registered scopes. | Event, Decision, occurrence-receipt, and preceding domain writes remain outside this slice. This is not complete C-root or shared-epoch coverage and provides no capture eligibility. |
| FR-B factual-read live integration handoff | Reviewed source candidate `codex/frb/frb-live-integration` at `5c43733088bfbe860183f849d16295b542c1f465`, formerly based on P12 canonical `1ac675cc558aa919a749167647c10506c11303fc`; handoff/review evidence is retained at `codex/frb/frb-live-integration-review` `f4e23a7`. It touches `SimulationRuntime` and `SimulationBootstrapComposition`, which also changed in the P12 promotion. | Recompose on `e64caf0` at the next serialized hotspot boundary. Compare the resulting code tree with the reviewed tree; rerun validation for the actual delta and obtain fresh exact-tip review if code changes. This work does not block independent owner/source audits. |
| Highest-value remaining P12-B gap | The accepted source map identifies cross-owner operations whose commits span multiple factual owners; `TravelPartySystem.AdvanceParties` is a concrete selected-profile path that may mutate party, spatial position, route Knowledge, and NPC/City travel state in one continuation. Current promoted operations cover only their named boundaries. | After the FR-B hotspot handoff is safely rebased/recomposed, audit `AdvanceParties` against its exact current call graph and owner hooks. Produce a bounded operation design only if source evidence confirms uncovered commits and a single existing outer boundary can own them without changing gameplay semantics. Do not claim global operation/epoch completeness. |
| Global status | Canonical Phase 12 State still marks P12-B INCOMPLETE, P12-A `WAIT_DEPENDENCY`, P13 BLOCKED; P12-C through P12-G retain their documented dependency edges. | The record-sequence promotion changes no downstream readiness. No complete census, global quiescence, capture eligibility, export, or hydration is implied. |

This refresh selects a source-audit target, not a newly authorized implementation
checkpoint. The FR-B handoff remains serialized with other edits to
`SimulationRuntime`/bootstrap; unrelated P12 evidence and owner work can
continue on isolated branches.

## Current P12-B blocker refresh — 2026-10-02 post FR-B live integration

P12 canonical is now at promotion record `28d33a2c8f10ddb724819893a0b5f2804a6f5b0b`.
Its code tree is the independently reviewed current-base integration
`aeb76c687d00a49f505ab264a58a508a20e4923b` (tree
`862eb904c6679a66d4dd2a2e2ad7f174ec8479c2`), based on prior canonical
`1dce6d54a33ac1b1778b1a44a7794512a58416e8`.

| Area | Current canonical evidence | Remaining blocker |
|---|---|---|
| FR-B selected-profile live integration | The composed runtime exposes a factual-read coordinator. Only `UnityBootstrap-Daily-v1` binds its exact FactionStore/PersonStore pair; availability follows successful publication and healthy bootstrap closure. Owner-thread, healthy-idle admission and logical-day/exact-store-revision bracketing guard the synchronous read cut. The partial P12 epoch is health-only; other profiles remain unavailable and requested readers remain unsupported. Exact-tip review, focused 21/21, ALL EditMode 2182/2182, Smoke 5/5 and diff-check PASS are retained in the promotion record. | This selected daily read surface is not P12-A capture eligibility, complete owner coverage, whole-world coherence, complete shared-epoch coverage, export or hydration. No reader is implemented. |
| Cross-owner continuation: `TravelPartySystem.AdvanceParties` | The post-record-sequence source audit remains applicable: the recomposition changed `SimulationRuntime`, bootstrap composition, factual-read admission, the Unity entry point and bootstrap tests; it did not change `TravelParty.cs` or its direct owner-write implementations. The runtime call still occurs inside the existing `runtime.advance-day` outer operation. The path has committed party/NPC travel-state, arrival City projection, per-NPC SpatialKnowledge and party-completion effects. | This is the highest-value remaining operation gap supported by the current source map. Next, record the exact ordered commit points, partial-progress/error semantics, owner-section mapping and existing mutation notifications in a bounded nested-operation design. Preserve normal gameplay semantics, make no global operation/epoch claim, and serialize only actual SimulationRuntime/runtime-owner edits. |
| Other uncovered writers and owner census | Existing census and operation evidence remains partial. Promoted direct NPC-owner, MoneyAccount transfer, Merchant, Market, identity, City/NPC, record-sequence, quiescence, and FR-B slices cover only their recorded owners and boundaries. | Continue against the accepted live profile owner/operation matrix. Prefer cross-owner commit boundaries and missing owner notifications over duplicating passive census. No P12-B completion or capture eligibility follows. |
| Downstream readiness | P12-A remains `WAIT_DEPENDENCY`; P12-B remains INCOMPLETE; P12-C through P12-G retain their documented gates; P13 remains BLOCKED. | The FR-B integration changes no dependency label and makes no export/hydration or causal-history capability available. |

The FR-B integration is promoted and its hotspot is released for subsequent
bounded work. The TravelParty source audit was against the preceding canonical
and remains source-valid because the promoted code delta does not touch its
system or owner mutators; recheck the call path at the implementation boundary.
The next design is an operation-coverage proposal only, not an implementation
candidate or readiness claim.


## Current P12-B owner/operation/epoch matrix — 2026-10-02 post TravelParty advance promotion

This refresh uses canonical tip `4a9a6977d7b0a2b4a7258559fe127946337d17fc`,
which follows `6b30d86c3214a98603bea809154e2dc06047d6a3` by a clean approved
fast-forward. Exact reviewed code is `ac0bcffe4d345c81d77bfa56b19e3591a9ebb46c`
(tree `14e2f4e485a83791af43b781546bd6f90b3913f5`). The candidate's exact-tip
implementation review and retained validation are documented in
`PHASE12_P12B_TRAVEL_PARTY_ADVANCE_IMPLEMENTATION_REVIEW.md` and
`PHASE12_P12B_TRAVEL_PARTY_ADVANCE_CANDIDATE.md`.

| Writer/owner family | Current canonical coverage | Remaining boundary |
|---|---|---|
| `TravelPartySystem.AdvanceParties` | Promoted as a nested `runtime.travel-party.advance` operation inside `runtime.advance-day`. The exact party store, per-NPC travel progress, arrival City-presence projection, per-NPC SpatialKnowledge, and record-sequence changes are deduplicated and validated as one bounded changed-section set. Existing partial progress and event-failure behavior are preserved. | Covers this selected daily path only. It does not imply all allocator counter families, all travel starts/reconciliation, or global operation/epoch coverage. |
| Runtime ID Event counter | The passive `p12c.runtime-id-allocator.events` witness reports the exact allocator owner, cardinality one, and local revision (`nextEventSequence - 1`). The P12 runtime protocol does not register this section or bind mutation callbacks for `AllocateEventId()`. `DomainEventRecorder.Record` consumes an EventId before constructing/storing the event, so successful allocation remains a committed owner write even if later event construction or storage fails. | Highest-value next bounded invalidation target: register and bind only this exact Event counter for the selected profile; preflight its existing baseline and report the successful allocation after its counter advances. Respect existing nested TravelParty/Merchant batching where those contexts are active. Failed/exhausted allocation does not advance or notify. Other thirteen allocator counters remain outside this slice. |
| Solo travel start | Current source path remains `TravelActionProvider` → `TravelSystem.TryStartTravel` → travel charge → `NpcRuntime.StartTravel`/City presence → origin/route SpatialKnowledge → `NpcTravelStartedEvent` and record sequence. Existing hooks cover several owners, but no named outer `runtime.travel.start` P12 operation exists and Event counter invalidation is still absent. | Re-audit after the Event counter hook. If source assumptions still hold, design the smallest selected-profile nested operation with exact owner preflight and changed-section batching; preserve compensation, event-failure, and normal travel semantics. |
| Downstream readiness | P12-B is still a partial foundation; P12-A remains `WAIT_DEPENDENCY`; P12-C through P12-G retain their documented edges; P13 remains blocked. | This promotion changes no readiness edge. No complete owner census, operation or shared-epoch coverage, global quiescence, capture eligibility, export, or hydration follows. |

The Event counter target is within the previously accepted P12-B prerequisite
capability authorization. It is not a new checkpoint ID or architecture
decision. Review and implementation must remain limited to the existing
selected-profile Event counter witness and successful allocator writes.

## Current P12-B owner/operation/epoch refresh — 2026-10-03 post Event-counter promotion

P12 canonical is `55ac2ebdec4bdbcda6085668188730b7bcb9cd5a`, a clean
fast-forward from `aa8f0305bea9f10c15045e07400d8785c2bd9e23`. The Event-counter
implementation is code `a573e5120951f8ac10c2da5b6ad79e066991a57a`, tree
`8e3e2966601d834c2c23429e253d02a9d1a7bb8c`; exact review and validation are
retained in the candidate/review documents named by the current Phase 12
State.

| Area | Current canonical evidence | Remaining blocker |
|---|---|---|
| Selected-profile Event counter | Exact `RuntimeIdAllocator` Events section is registered and bound in `UnityBootstrap-Daily-v1`. A successful `AllocateEventId()` validates exhaustion, owner thread, exact baseline, and epoch capacity before advancing the allocator; the post-commit notification enters the current TravelParty/Merchant batch when one is active, otherwise advances the partial epoch. Failed/exhausted preflight does not consume or notify. | Only the Event counter is covered by this allocator slice. The other thirteen allocator counters, Decision/read-model relationships, and preceding domain writes remain separate. The already-promoted `SimulationRecordSequence` hook remains its own bounded owner slice. This is not complete allocator-root or shared-epoch coverage. |
| Solo travel-start path | Current selected-profile call path is `TravelActionProvider.TryExecuteAction` → `TravelSystem.TryStartTravel` → `EconomyTransactionService.TryChargeTravel` → `NpcRuntime.StartTravel` → origin/route `SpatialKnowledge` discoveries → `DomainEventRecorder.Record` (Event ID then record sequence) → `TravelActionProvider.ClearTravelPlan`. The runtime invokes the action provider from its daily actor turn. | The call has no named P12 `runtime.travel.start` operation. Account, NPC travel-state, City presence, Knowledge, Event, sequence, and travel-plan notifications currently commit independently; there is no preflight of that exact owner set or one close-time changed-section batch. Design the smallest selected-profile operation, including charge compensation and post-start event-failure semantics, without changing gameplay behavior. |
| Runtime admission/quiescence | The selected profile binds its Unity owner thread and scopes bootstrap/publication and daily advance; direct owner callbacks remain partial. The new Event callback fails closed when its owner/epoch notification cannot be recorded. | This is not exhaustive runtime-wide owner-thread/quiescence proof. The travel operation must compose inside the existing daily advance without claiming global quiescence or capturing unrelated day work. |
| Capture eligibility and downstream DAG | P12-A remains `WAIT_DEPENDENCY`, P12-B `INCOMPLETE`; P12-C is blocked on B, P12-D/E on B and C, P12-F on C/D/E, P12-G on B–F plus validated live profile inventory; P13 remains blocked on continuation and recoverable causal history. | Complete live owner/cardinality inventory, supported-write invalidation, quiescence, capture eligibility, export, and hydration remain unresolved. Event promotion creates no new READY numbered-phase checkpoint. |

The source re-audit confirms solo travel-start as the highest-value next
bounded operation gap. `TryStartTravel` charges before installing NPC travel
state; if the NPC transition rejects, existing code attempts a charge restore.
After successful travel-state installation it performs two Knowledge writes
and records the start event. Event/sequence or later event-storage failure
does not roll back the already installed travel state. The bounded contract
must preserve these existing commit and failure semantics. It covers only the
normal selected-profile Travel action entry, not every standalone owner API
or unrelated travel/party/expedition behavior.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked. No complete owner or shared-epoch coverage, global quiescence,
capture eligibility, export, or hydration is implied.

## Current P12-B owner/operation/epoch matrix — 2026-10-03 after solo travel and WX-D integration

P12 canonical is `55a93ba58b572dafb70647f387148c0a4bde97c3`, reached by the approved WX-D fast-forward from the post-solo State tip `4d9f48fceea4e0742e7ebc2c5199df5163051b7c`. The WX-D producer is outside the P12 owner/operation inventory and changes no continuation authority. Its exact review and combined-tree validation are recorded in `docs/design/WXD_V2_POST_SOLO_TRAVEL_INTEGRATION_REVIEW.md` and `docs/design/WXD_V2_POST_SOLO_TRAVEL_INTEGRATION.md`.

| Area | Current canonical evidence | Remaining blocker / next action |
|---|---|---|
| Solo travel-start | Selected `UnityBootstrap-Daily-v1` Travel action enters the bounded nested `runtime.travel.start` operation. It preflights the exact installed NPC/provider/account/travel/plan/Knowledge/presence/Event/record-sequence witnesses and batches changed sections at close while preserving charge compensation and post-start failure semantics. Code `fe0e0be`, tree `d72e84d`; evidence is in the P12 State and exact-tip review. | Covered only for the normal selected Travel action entry. Standalone owner methods, daily observation/plan cleanup outside the named boundary, TravelParty and Expedition are separate paths. No global operation claim. |
| Selected-profile Event counter | The exact `p12c.runtime-id-allocator.events` section is registered and bound. Successful Event allocation preflights owner-thread, exact local revision, and epoch capacity, then reports the committed counter through the existing shared-epoch batching path. | Other allocator counters remain unwired; this establishes only the Events section. |
| Selected-profile Decision counter | `RuntimeIdAllocatorCensusProvider` defines `p12c.runtime-id-allocator.decisions`, cardinality 1, with local revision `nextDecisionSequence - 1`; however, `SimulationRuntime` currently creates/registers/binds only the Events provider. `NpcDecisionRecorder.RecordWithParticipants` and `TryRecordOccurrenceOnce` call `AllocateDecisionId()` before sequence allocation and decision-store append. | Highest-value next bounded committed-write gap: bind and invalidate only the Decisions counter section in `UnityBootstrap-Daily-v1`, preserving ID consumption and fail-closed bookkeeping behavior. The counter is a causal write even if subsequent sequence construction or store append fails. Do not wire other allocator counters or claim complete allocator-root coverage. |
| TravelParty advance | The selected `TravelPartySystem.AdvanceParties` path is a promoted nested operation with exact party/NPC/presence/Knowledge/record-sequence changed sections and retained partial-progress/error semantics. | Other party entry/return/reconciliation paths remain separate. |
| Daily Merchant and NPC trade/transfer | Selected daily Merchant commit, NPC-to-NPC trade, and money-transfer slices are promoted with their bounded owner sets and compensations. | Remaining direct Merchant, economy, crime/justice and other owner writes still need source-specific mapping and implementation. |
| Expedition / directives / actor choices | Passive exact-owner census evidence exists for the selected Expedition, ScheduledDirective, and ActorChoice owners. The source map still identifies Expedition start/advance/exploration/return, directive terminal transitions, and ActorChoice terminal transitions as separate writes; the existing census does not connect them to the shared epoch. | Preserve as separate operation/writer families. Do not add them to the Decision-counter slice. Revisit after the small central allocator hook, using current source call graphs and actual profile composition. |
| Runtime admission and downstream readiness | The selected profile has the bounded owner-thread/admission adapter and partial shared epoch; canonical State still records incomplete live owner coverage and supported-write coverage. | Not a global owner-thread/quiescence proof and not capture eligibility. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. |

### Next dependency-safe P12-B task

Proceed with the exact `RuntimeIdAllocator.AllocateDecisionId()` committed-write boundary. Reuse the existing census section/provider and the Event-counter's reviewed bind/preflight/notify pattern; retain a distinct Decision section and no new operation or product behavior. Audit record creation ordering and post-allocation failures, then prepare a bounded technical design, independent review, implementation, focused/regression/full validation, and exact-tip code review. The existing accepted P12-B capability authorization covers this selected owner invalidation; no new architecture or product scope is introduced. Other allocator counters, all-operation completeness, complete owner coverage, capture eligibility, export, hydration, and downstream readiness remain unclaimed.

The refreshed numbered-phase DAG is unchanged: P12-A is `WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus live-profile inventory; P13 remains blocked on P12 continuation and recoverable causal inputs/history. The WX-D handoff creates no Phase 19 readiness or P12 edge.

## Current P12-B owner/operation/epoch refresh — 2026-10-03 after Decision-counter promotion

Canonical is `22e5e51ac1383f91c30ea9d1c15351020dd93b38`. The Decision-counter
code is `52154053219e30e679bc400adf55c2f577bd8106`, exact tree
`ddd3684daf264a15af9c217c8edb4e390e82602d`; design and independent exact-tip
review, focused/full validation, and artifact hashes are linked from the P12
State and candidate record.

| Writer/owner family | Current selected-profile coverage | Remaining boundary |
|---|---|---|
| RuntimeIdAllocator Event counter | Successful `AllocateEventId()` is bound to the exact cardinality-one Events witness and partial epoch; it joins a current TravelParty/Merchant batch when applicable. | Only the Events counter is covered by that slice. |
| RuntimeIdAllocator Decision counter | Successful `AllocateDecisionId()` is now bound to the exact cardinality-one Decisions witness and partial epoch, with preflight before cursor advance and notification after commit; existing nested batching remains. | No other allocator counter, DecisionStore/read-model, occurrence-receipt, or ActorChoice write is included. |
| TravelParty advance and solo travel start | The selected daily paths are wrapped in their reviewed nested operations with bounded owner sets and existing partial-commit/compensation semantics preserved. | Other party/expedition lifecycle paths and all-operation completeness remain open. |
| ActorChoice lifecycle | P11's exact ActorChoice store and transition semantics are canonical. `SimulationRuntime` exposes capture/defer/reject/dispatch/returned/threw transition paths; current P12 registration is not shown among the registered selected-profile census providers. | Audit current provider/guard wiring and actual daily-boundary reachability. The bounded next design candidate is post-commit invalidation for the existing ActorChoice owner transitions only, preserving transition order and no-fallback behavior. Do not widen to decision read models or other operations. |
| Other accepted P12-B owner families | Market, Merchant, money transfer, NPC owner writes, selected City/presence and quiescence adapters remain covered only within their recorded exact slices. | Continue the committed-write map by existing owner and supported selected-profile paths; do not infer general coverage from census witnesses. |
| Downstream readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E on B and C; P12-F on C/D/E; P12-G on B–F and validated live-profile inventory; P13 remains blocked. | Decision-counter promotion unlocks no downstream checkpoint. No full census/epoch, capture, export, hydration, or closure is claimed. |

The Decision counter closes the prior central allocator gap only for the
selected `AllocateDecisionId()` writer. The next bounded audit is the
ActorChoice store lifecycle because it is already a supported P11 state owner
and can be mutated on the selected actor-turn path. Confirm exact P12 owner
binding, all successful transition commit points, temporal owner identity and
cardinality assumptions, and enclosing daily-operation behavior before
implementation. If existing P12 scope and code leave that boundary unambiguous,
continue with bounded design/review/implementation; otherwise isolate the
specific missing evidence rather than proposing broad owner registration.

## Current P12-B owner/operation/epoch refresh — 2026-10-03 post ActorChoice invalidation

The ActorChoice implementation was promoted at
`3b25852bfc678095dd97327aecfaa2559b151bc1` by clean fast-forward from
`f1ec63ea7fa0592b3a280e138a80023e3cacc6b7`; the current canonical tip is
`a6470ad1fea3134d08219c43b41fac6ebf0a44ab`, which adds only the State/matrix
evidence correction. ActorChoice implementation code is
`0bb87c89662397857c7e55267bbc60f32ce0676a`, tree
`aa570c943299eafe52c9a5a9b05a9487bdfd5add`; exact-tip review and validation
are recorded in the Phase 12 State and linked candidate/review evidence.

| Writer/owner family | Current selected-profile coverage | Remaining boundary |
|---|---|---|
| RuntimeIdAllocator Event and Decision counters | Successful `AllocateEventId()` and `AllocateDecisionId()` writes preflight their exact cardinality-one sections and partial epoch before advancing, then report committed revisions through existing batching/direct notification. | Other allocator counters and preceding domain writes remain separate; no complete allocator-root or shared-epoch coverage is claimed. |
| P11 ActorChoice lifecycle | The exact selected-profile `ActorChoiceStore` owner section is registered. Successful P11 capture/disposition commits preflight and notify after commit; active TravelParty/Merchant/SoloTravel collectors remain the batching path. | P18 temporal ActorChoice remains a distinct, unregistered owner section. The P11 slice does not cover other stores or complete actor/operation coverage. |
| ScheduledDirective lifecycle | `TesteSimulacao` creates the selected profile's `ScheduledDirectiveStore` and `ScheduledDirectiveSystem`. The normal day path calls `PrepareDay`; the actor-turn path calls `TryTakeDirective`, then may mark a directive Succeeded, Failed, or Skipped. `PrepareDay` can also mark duplicate-target or unresolved-actor rows Skipped. `TryTakeDirective` changes only transient system lookup state. Current P12 `SimulationRuntime` has no `ScheduledDirectiveCensusProvider` registration or mutation bind. | Next bounded design: reuse the store-owned successful Add/terminal-transition commit boundary, register the exact composed store section in P12, preflight before supported selected-profile owner commits, and notify after commit. Preserve selection, take, action, terminal-state, and error semantics. Do not claim global quiescence or complete P12-B. |
| Existing ScheduledDirective census foundation | Store-local monitor/revision and `ScheduledDirectiveCensusProvider` are already canonical at `03ffa1031c3a7f125d1d8cff00f72d22749816bb`; the exact-store provider is exposed by `SimulationBootstrapComposition` at `72239ad`. The P12 runtime protocol still does not register the section or bind mutation callbacks. | Preserve and reuse the canonical witness. The older static review `f906453a35d91523600e82f9b3ce7c3cd103f7b1` reviewed only the local store/provider and predates composition exposure; it did not run Unity tests and is not review of P12 epoch integration. The active gap is P12 runtime registration, preflight, and post-commit notification on the current base. |
| Downstream readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F and validated live-profile inventory; P13 remains blocked. | ActorChoice invalidation adds no readiness edge. No capture eligibility, complete profile owner/write coverage, export, hydration, or Phase closure is inferred. |

### Next dependency-safe P12-B task

Produce a bounded design and independent review for ScheduledDirective
selected-profile owner invalidation. The smallest useful slice binds the
existing store-local commit boundary to the exact selected-profile P12
provider. Cover terminal transitions from normal directive
preparation/processing and any supported post-publication Add path. Initial
genesis Adds occur before the profile baseline and remain represented by the
initial witness, without synthetic runtime notifications. Census sampling
and mutation preflight/notification must use the exact store installed in the
published composition. Preserve transient `TryTakeDirective` semantics and
all existing directive behavior. Reuse the already canonical owner-local
revision/provider implementation; do not rebuild a passive census slice.
This work is within the accepted P12-B prerequisite capability scope; it does
not add a checkpoint ID or broaden P12-A.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked. The numbered-phase DAG is unchanged.

## P12-B owner/operation refresh — 2026-10-03 after ScheduledDirective invalidation

Current P12 code/review tip is `f23cbd9b6959771249e8f4446808b8402fbd8b9f`; reviewed executable code is `b8dced9666438d736c8bd2b417390d52988f3c78`, tree `248abaacad1538a40a4b0a7af4e1898749993128`. The independent exact-tip review and validation are linked from `PHASE12_STATE.md`. The current architecture baseline is `3bf09249b7dd9e255c3493aacfd75c96080a31e3`.

| Writer/owner family | Current selected-profile evidence | Remaining boundary |
|---|---|---|
| ScheduledDirective store and lifecycle | P12 now binds the exact composed cardinality-one section. Supported post-bind Add and terminal state transitions preflight before mutation and notify after commit, including duplicate/unresolved `PrepareDay` skips. Transient `TryTakeDirective` remains non-authoritative. | This covers only the selected store and supported paths in the implementation review. It does not establish complete profile owner or write coverage, capture eligibility, or export/hydration. |
| P12-F Expedition owner/start | The store-local exact-owner census and mutation seams exist, but Expedition checkpoint state belongs to accepted P12-F and P12-F is blocked on P12-C, P12-D, and P12-E. | Excluded from the current P12-B next-work set. Do not treat the old Expedition start audit as P12-B readiness or schedule it ahead of its documented P12-F dependencies. |
| Daily selected profile and new P15/P16 state | `UnityBootstrap-Daily-v1` has not been broadened. P15-A adds a separate `StructureStore`; P16-A adds supply/receipt state to the ArmedForce spatial authority already visible to the P12 owner matrix. | P15 must prove explicit daily-profile exclusion. P16 must fail closed if unsupported P16 state appears in the daily profile and requires a negative admission test. Serialize P16 ArmedForce-store and all SimulationRuntime/bootstrap admission changes with P12-B. |
| Downstream readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus complete live-profile inventory; P13 reconstruction/fork remains blocked. | No complete live owner census, complete shared epoch, global owner-thread/quiescence, capture eligibility, export, or hydration is claimed. |

The current P15-A and P16-A implementation candidates are isolated from the P12 runtime/bootstrap window while their owner-level files are developed. They are based on the preceding P12 code tip and must be refreshed/recomposed against the current P12 canonical branch before hotspot integration, followed by the profile-specific negative admission evidence required by the architecture.

## Current P12-B blocker refresh — 2026-10-05 population owner commit gap

This refresh supersedes the prior post-ScheduledDirective “Expedition start” next-task statement. P12 canonical remains `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. The runtime/source comparison uses P16 canonical `75a27d7ac97e66c2762835ccea7950a945c2f20d`, a descendant of that P12 tip; it includes the same P12 protocol plus the P15/P16 composition-safety additions. Latest architecture planning is `ffd75652d89d862b83d634868c560f8540869b89`; its P12 capability audit preserves B→C→D/E→F→G→A dependencies and the §92A exclusion/rejection rule for new authoritative owners.

| P12-B surface | Current code/evidence | Remaining gap and bounded next action |
|---|---|---|
| Sealed P12 owner inventory and operation set | `SimulationRuntime.InitializeNpcRosterCensusProtocol` registers Person membership/materialization; roster-following SpatialKnowledge, travel-state, Inventory, MoneyAccount, NPC Knowledge and plans; SimulationRecordSequence; RuntimeIdAllocator Event/Decision; ScheduledDirective; P11 ActorChoice; TravelParty; per-City presence and Market sections. Registered operation IDs include bootstrap publication, daily advance, TravelParty advance, solo travel start, NPC trade/transfer, Market purchase/sale, and Merchant daily trade. | The census remains partial. The canonical source has several additional owner-local census providers exposed at bootstrap that are not members of the sealed P12 protocol. Do not call those owners covered merely because a provider exists or a test can read it. |
| Per-City SettlementPopulation aggregate | `SettlementPopulationCensusProvider` publishes exact-owner aggregate and operation-receipt sections per City. `SimulationBootstrapComposition.SettlementPopulationCensusProviders` exposes those witnesses. The population census design/candidate and cumulative promotion record preserve this surface; P12 State still classifies it as passive evidence. | `SimulationRuntime.InitializeNpcRosterCensusProtocol` does not register these population sections or bind a P12 mutation callback on `SettlementPopulationRuntime`. The aggregate owner therefore is not connected to this runtime’s sealed inventory/epoch. |
| Daily aggregate demography and natural mortality | The selected `Simulation-GeneralTest.asset` has absent serialized flags, so `SimulationConfigData.CreateConfigurationOverrides` supplies explicit false values; the resolver therefore produces `NaturalMortalityPolicy.Disabled` and `AggregateDemographyPolicy.Disabled`. Defaults are also disabled. `AdvanceDayAfterClockAdvance` calls `DailyDemographicSystem.Advance`, but neither population writer branch executes for this profile. | Preserve both false effective flags at P12 admission and fail closed if either is enabled. The current P12 Daily profile has no population commit from its daily demography phase; owner registration is still required. A future profile enabling either consumer requires separate operation-specific design/review and cannot inherit this proposal's coverage. |
| Person/NPC life-residence witnesses | Existing Person sections `p12d.person.membership` and `p12d.person.materialization-binding` both use `PersonStore.Revision`; neither changes when nested `PersonRuntime` death/residence changes. Existing NPC travel-state, child-owner, and City important-presence sections likewise do not witness `NpcRuntime` life/residence. `PersonRuntime` and `NpcRuntime` have no corresponding local revision hooks. | Proposed exact singleton families are `p12b.person-life-residence/<PersonId>`, `p12b.npc-life-state/<RuntimeId>`, and a conditional `p12b.npc-residence/<RuntimeId>` for non-Person-backed NPCs, with owner-local revisions and dynamic roster reconciliation. These are design proposals only; no existing section may be relabeled to claim these facts. |
| Population/lifecycle ingress and operation sets | Runtime wrappers include immigration/emigration/resident death and direct Person death; static APIs also exist for NPC lifecycle, Person death, and paired residence migration. Residence-only binding and named birth/materialization write the proposed life/residence facts too. `ConflictResolutionService` is invoked by `CoreWorldCommandHandlers`/WorldObserver demo; the accepted P12 profile excludes external `WorldCommand` composition. | Proposed operation IDs and exact owner changed sets are listed in the revised design. `runtime.npc-membership` needs its dynamic witness set extended for materialization; new residence-bind, population lifecycle, unkeyed Person-death, and migration operation IDs do not exist today. Keyed/receipt-bearing Person death and all named birth fail closed on P12-bound runtimes pending an owner contract; named birth remains P12-D scope behind P12-B. Direct static ingress on P12-bound owners must reject unless it holds the matching operation context. Conflict command ingress is excluded by P12-A; unclassified direct conflict mutation must fail closed on P12-bound owners. |
| Paired residence migration | `NpcResidenceMigrationSystem.TryApply` calls `SettlementPopulationRuntime.TryApplyPairedMigration`, which commits both City aggregates before changing NPC residence. No P12 operation scope surrounds the pair. | Proposed `runtime.population.residence-migration` preflights origin/destination population plus NPC and bound-Person life/residence sections, then notifies once after all owners commit. Until those exact witnesses and enforcement exist, paired migration remains uncovered. |
| Protocol operation/epoch seam | `TryEnterOperation` enforces registered ID and bound-thread operation counting. `TryValidateUnchangedSections` validates current baselines; `TryValidateMutationEpochCapacity` only rejects max epoch; `NotifyCommittedMutations` validates changed witnesses and increments epoch after the write. None reserves epoch capacity or binds expected changed sections to an operation. | A bounded operation commit context must add baseline validation, one reserved epoch increment, required-scope gating at all ingress paths, and a single post-success notification. Existing active-operation counting is not reservation. The detailed API requirements and evidence are in the revised design. These are implementation obligations for the reviewed, authorized bounded sub-slice; they do not claim that any code has been delivered. |
| Temporal receipt-backed population operations | `DailyDemographyBoundaryStepProvider` uses receipt-backed demography under the separate P18-D intraday profile. | The provider is outside the accepted P12 Daily profile and is excluded from this design. If composed under P12 in future, require its own admission rejection or reviewed operation-level adapter; do not infer coverage from population provider registration. |
| Existing P12 mutations and nested batches | Record-sequence, Event/Decision counters, directives, P11 ActorChoice, rostered NPC owners, City presence, Market, TravelParty, TravelParty advance, solo travel-start, and Merchant operations have their reviewed direct or operation-batched hooks. The selected GeneralTest demographic flags are both disabled; `runtime.advance-day` is an activity count, not an epoch batch. | Do not add a population callback inside shared `TryApply`. Bind future population census sections and place any supported lifecycle notification at the complete outer operation boundary. Preserve existing operation partial-progress behavior; do not add a generic operation framework. |
| P16 and P17 composition | P16 runtime rejects P15/P16 proving state under P12 Daily admission. P17-A’s reviewed contract requires P17 War state to reject under Standard/P12 and prohibits pairing the P17-A profile with P12; P17 implementation shares `SimulationRuntime`/admission ownership. | The P17 admission proof is a P17 integration requirement and must serialize at the runtime hotspot. It does not make population or P17 War state P12-covered and does not require a blanket P12 closure prerequisite. |
| Expedition | `ExpeditionStore`/`ExpeditionSystem` have owner-local census/mutation seams, but the capability DAG assigns Expedition/active commitments to P12-F. | P12-F is blocked on C/D/E. Do not use Expedition start as the next P12-B task or bypass those edges. |
| Readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B+C; P12-F waits on C/D/E; P12-G waits on B–F and a validated live-profile inventory; P13 remains blocked. | The SettlementPopulation invalidation design is independently reviewed PASS at exact tip `15baaf9b772ceca223f3f5efeaca61ddd68401eb`. This bounded sub-slice is `READY_FOR_IMPLEMENTATION` under the accepted P12-B capability authorization; no implementation is delivered yet and P12-B remains incomplete. |

The independently reviewed design in [`PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN.md`](PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN.md) is `READY_FOR_IMPLEMENTATION` as one bounded P12-B sub-slice under the existing accepted capability authority; it is not a new checkpoint ID. Implementation must add Person/NPC life-residence revisions and providers, dynamic-family reconciliation, the reviewed per-ingress operation scopes and changed-section sets, local-revision/epoch capacity reservation, and one complete post-commit notification. Keyed/receipt-bearing Person death remains excluded until its receipt owner has a supported contract; named birth remains fail-closed on a P12-bound runtime until its P12-D owner contract exists, preserving B→D dependency order. Before implementation, refresh the P12/P16/P17 refs and serialize changes at the shared `SimulationRuntime` hotspot. P18-D temporal demography, conflict/WorldCommand gameplay, population export/hydration, global quiescence, capture eligibility, P12-A readiness, and P12-B completion remain unclaimed. No product semantics or additional checkpoint ID is introduced.

## Current P12-B blocker refresh — 2026-10-05 after SettlementPopulation lifecycle invalidation

P12 canonical is `16d09ece1958c73152bf3f04c82ac4a0d177bfbe`. The selected-profile SettlementPopulation/person/NPC lifecycle invalidation code is `f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01`, tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380`; exact-tip review and validation are recorded in the P12 State and the candidate/review artifacts. Architecture remains `ffd75652d89d862b83d634868c560f8540869b89`.

| P12-B surface | Current canonical evidence | Remaining gap / bounded next action |
|---|---|---|
| SettlementPopulation and Person/NPC lifecycle | Exact City aggregate/receipt owners and Person/NPC life-residence witnesses are registered. The reviewed supported lifecycle/residence/migration paths preflight all changed owners and one epoch reservation, then notify after full success. Selected-profile mortality and aggregate-demography writers must remain disabled; unsupported injury, keyed death, named birth, and external ingress fail closed as recorded. | This completes only this named owner/operation family. It does not imply all population writes, complete owner inventory, or all-operation epoch coverage. |
| NPC MoneyAccount and Inventory direct writes | Current canonical has exact per-NPC MoneyAccount identity/cardinality/local-revision sections and per-NPC Inventory row-count/local-revision sections. Transaction/service callers have bounded path-specific notices. A separate direct-owner invalidation design passed exact-tip design review on older canonical base `b77e154`; its implementation branch `766137aea8535c1d8f6e5529856228db6890e718` is based on `2f7c742`, predating current P12 code and later EconomyTransaction/SimulationRuntime integration. | Revalidate the design against current architecture and the promoted population/runtime code, then recompose the existing candidate on current canonical. Preserve exact owner identity, Inventory alias batching, existing outer scopes, committed-leaf semantics, and no duplicate service notifications. Re-run affected focused/regression/full validation and obtain fresh exact-tip code review if the tree changes. This work adds no new player/actor authorization, operation semantics, or P12 readiness claim. |
| P12-F Expedition inventory path | The candidate design updates `TryRetrieveTargetResource` to observe result-bearing Inventory admission while the exact performer Inventory is bound. It adds no Expedition census or operation scope. | Keep this as a narrow Inventory API consumer correctness fix only; Expedition state/operation capture remains P12-F and blocked on P12-C/D/E. Do not schedule or claim Expedition checkpoint delivery. |
| Runtime admission and shared epoch | Current protocol still represents a partial selected-profile witness and operation set; P12-bound writes with unsupported or mismatched owners fail closed where reviewed. | No global owner-thread/quiescence, complete shared epoch, capture eligibility, export, or hydration is established. |
| Readiness | P12-B `INCOMPLETE`; P12-A `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B+C; P12-F waits on C/D/E; P12-G waits on B–F plus validated live-profile inventory; P13 blocked. | Population promotion changes no downstream readiness edge. |

The existing direct NPC owner candidate is retained, not restarted or promoted from its old base. The design's exact-tip PASS remains useful as design evidence, but its old-base code has not been validated against canonical `16d09ec`; it is not a current validated candidate. No Unity validation is being inferred from the branch tip. The next executable work is the bounded revalidation/recomposition described above, subject to the refreshed design review confirming that its current owner and transaction boundaries remain compatible.

## Current P12-B owner/operation/epoch refresh — 2026-10-05 after Population and source reconciliation

The refreshed remote P12 canonical is `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`. It includes the selected-profile SettlementPopulation/person/NPC lifecycle invalidation promotion (`f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01`, tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380`) and subsequent documentation-only evidence. The root checkout and existing P12 worktrees contain unrelated user edits and validation artifacts; they were left untouched.

The earlier “next direct NPC MoneyAccount/Inventory invalidation” entry is stale. Direct-owner code commit `c49f957e45c3059231e9ec66e4010a7c3a389988` is an ancestor of this canonical and provides the committed owner hooks. The old implementation ref `766137aea8535c1d8f6e5529856228db6890e718` has the same tree, `627af2fbd7f93e0025106ee5ba87e72bae6c4ed2`; preserve that ref as historical evidence, but do not repeat its implementation.

| Selected-profile surface | Current evidence | Remaining boundary |
|---|---|---|
| NPC MoneyAccount/Inventory direct writes | Exact per-NPC sections and direct committed-write hooks are present in current canonical source; the former stale candidate is tree-identical to the delivered code. | No new work remains for this exact direct-owner slice. Other economy paths remain individually bounded. |
| Legacy daily Crime/Justice path | GeneralTest composes Crime and GuardCrime. The normal day path calls Justice BeginDay, Crime hidden-status advancement, sentence advancement, and wanted-status synchronization in that order. P12 Daily does not use the separate P18 receipt-backed step commits. Exact-tip design review passed at `bc7a0dc93a4e2c8034087ad4c97d84f37ed11c39`. | The design follow-up records that all four calls remain inside the existing single `runtime.advance-day` operation, with no nested protocol operation and sequential per-call mutation-epoch reservations. Exact-tip re-review of that correction is pending; implementation is not READY_FOR_IMPLEMENTATION yet. |
| Other Crime/Justice writers | Theft, arrest, escape, failed escape, generic status changes, and CrimeSocialAppraisal stores have distinct mutation paths. | They remain outside the daily slice and need a later source-specific operation map. No complete Crime/Justice coverage follows. |
| Downstream readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F and validated live-profile inventory; P13 remains blocked. | This reconciliation and design add no downstream readiness, capture, export, hydration, global-quiescence, or Phase-closure claim. |

This correction supersedes the stale next-task sentence above without rewriting historical promotion records. P12-B remains a partial owner/operation/epoch foundation.

## Current P12-B owner/operation/epoch refresh — 2026-10-05 Crime/Justice same-day actions

Remote P12 canonical remains `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`. The `5d5539d` Crime/Justice design review remains valid for the four legacy calls inside `BeginSimulationDay`, but not for the rest of the selected daily advance. `AdvanceDayAfterClockAdvance` continues the same outer `runtime.advance-day` operation through actor turns and supported directive handling.

| Selected-profile surface | Current evidence | Remaining bounded action |
|---|---|---|
| Legacy daily Crime/Justice | Justice `BeginDay`, Crime hidden-day advancement, Justice sentence advancement, and wanted-status synchronization run in order under the single outer daily operation. The 5d review confirmed per-call reservations without nested registered scopes. | Preserve the per-call reserved boundary design, including partial-commit notification and no-op release. |
| Same-day Crime/Guard and status writes | Normal, ActorChoice, requested-directive, and forced-Escape paths can write the same NPC status/hidden or Justice records before the outer daily operation exits. A one-time action batch would race for epoch capacity with independent owner callbacks. | Current revised design requires action-owner admission scopes and immediate per-leaf baseline/capacity preflight plus exact notification; no action-level epoch reservation or delayed flush. Include failed-Escape penalties and actor/target status effects. Await exact-tip independent design review. |
| Mutable owner aliases and receipt steps | `GetActiveWarrant(s)` exposes mutable wanted rows; sentence and NPC status/hidden mutators are public; Crime/Justice also expose receipt-backed P18 commits. | Bind row and direct status/Justice mutators to the exact P12 runtime and reject unsupported P12-bound writes before mutation. Keep P18 receipt sections empty by rejecting those commits under the selected Daily profile. |
| Other Crime-related owners | CrimeSocialAppraisal and its TheftOutcome, CrimeKnowledge, and SocialReaction stores are composed; some actor paths can write them. | Remain uncovered and separately ranked in the owner matrix. This sub-slice does not imply complete Crime/Justice or P12-B coverage. |
| Readiness | P12-B `INCOMPLETE`; P12-A `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus validated live-profile inventory; P13 remains blocked. | No readiness edge changes until a bounded candidate is implemented, reviewed, and promoted. |

The revised technical design remains within the accepted P12-B capability work. It adds no crime/arrest/escape rule, no general operation framework, and no P12-A, P13, capture, export, hydration, global-quiescence, or Phase-closure claim.

The exact design candidate `452426687a57c1cfebb3f007511c5e23a21273a2` (tree `e970567ae059dc8c537739469cfff65c0d67fa6b`) passed independent exact-tip review against P12 canonical `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`, architecture `ffd75652d89d862b83d634868c560f8540869b89`, intraday alignment `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`, and multi-participant alignment `c285466c355103d3637ac165246591b72eb7bda0`. Review record: `PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_DESIGN_REVIEW.md`. The bounded selected-profile NPC status/hidden and Justice slice is implementation-ready under the previously accepted P12-B prerequisite-capability authority. Focused, affected, and full validation remain outstanding. CrimeSocialAppraisal-related owner coverage remains a separate blocker.

## Current P12-B owner/operation/epoch refresh — 2026-10-05 after Crime/Justice promotion

P12 canonical advanced from `39e275f39e1602d3fa109d3a1bb9acd60585a3f2` through reviewed promotion bundle `37371317d7c8a27eb795a10fb78676a4a6946173`; the Phase 12 State records the exact code/review/validation boundary. Architecture remains `ffd75652d89d862b83d634868c560f8540869b89`, with the intraday/extensibility alignment at `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194` and multi-participant alignment at `c285466c355103d3637ac165246591b72eb7bda0`.

| Selected-profile surface | Current evidence | Remaining bounded action |
|---|---|---|
| Crime/Justice status and Justice facts | Implementation `92be026fea6515094be7140230194ba626eb090a`, tree `e3f844fe67e38df7c96f3db88d792c176c1b9dfd`, is promoted. It covers the reviewed legacy daily and same-day actor/directive write paths only; exact implementation review and validation are recorded in Phase 12 State and the linked review/archive. | Preserve its path-specific operation and per-leaf invalidation boundaries. It does not close unrelated crime owners or any global owner/epoch requirement. |
| CrimeSocialAppraisal singleton and child stores | `SimulationRuntime` constructs one `CrimeSocialAppraisalWorldState`, which owns one `TheftOutcomeStore`, one `CrimeKnowledgeStore`, one `SocialReactionStore`, and their existing integration. All three stores expose result-bearing mutation APIs. `TryAcceptTheftOutcome` records an outcome then knowledge/reaction and compensates on failure; `TryRecordKnowledgeAndAppraise` is separately exposed. Current P12 Daily SampleScene has no `PersonId` on its legacy NPCs and starts with an empty PersonStore; normal theft checks required Person identity before transferring money. | These child stores are not registered as P12 owner sections or connected to the shared epoch. Audit exact store identities/cardinality, empty baseline, every direct/integration writer, PersonStore/runtime entry paths, and fail-closed behavior. Prepare an independently reviewed technical design; do not add appraisal gameplay, assume the profile can contain Persons, or infer readiness from an empty initial snapshot. |
| Other Crime/Justice surfaces | The prior source matrix identifies independent crime/social state and actor paths beyond the promoted status/Justice rows. | Rank each from current selected-profile reachability and existing owner authority. Do not claim all Crime/Justice owners complete from this promotion. |
| Runtime admission and shared epoch | The selected profile still uses a partial sealed owner/operation set. | No complete owner/operation/shared-epoch coverage, global owner-thread/quiescence proof, capture eligibility, export, or hydration is established. |
| Downstream readiness | P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus validated live-profile inventory; P13 remains blocked. | This promotion changes no dependency edge or downstream readiness. Phase 12 remains open. |

The highest-value next work is the bounded CrimeSocialAppraisal owner/writer audit and technical design above. It reuses the existing profile composition and accepted P12-B capability scope; it does not create a new checkpoint ID. No implementation readiness is claimed until independent design review confirms the exact empty-state and post-bind mutation boundary against current canonical code.
### CrimeSocialAppraisal follow-up correction — 2026-10-05

The selected profile still starts with an empty PersonStore and legacy NPCs without `PersonId`, but `SimulationRuntime.TryRegisterPerson` can add Persons after publication through the P12 membership path. The TheftOutcome, CrimeKnowledge, and SocialReaction stores therefore cannot be treated as permanent empty owners. The bounded design covers exact local row counts/revisions and direct/composite writes, including per-store revision headroom, the nested theft/appraisal compensation sequence, and one shared epoch per outer logical commit. Exact design tip `7770119d7ae4dd69186ff6394946168db1473527` passed independent review and is READY_FOR_IMPLEMENTATION under the accepted P12-B capability authorization; see `docs/design/PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_DESIGN_REVIEW.md`. The initial zero baseline remains useful validation, not a sufficient capture contract. This design does not complete P12-B or establish P12-A readiness.

## Current P12-B owner/operation/epoch refresh — 2026-10-05 after Crime/Social Appraisal promotion

Remote `codex/phase12/canonical` is `1a073df83050da9bed5f7e3e48a27b9952cf62eb`; architecture is `ffd75652d89d862b83d634868c560f8540869b89`. The Crime/Social Appraisal implementation is promoted at code `ec956c7247c39f8984f7c5a6bca3d813091a0047`, tree `61956354146f3437396d93d91c4df246da8df89d`, with exact-tip review R1 and validation recorded in the State and candidate artifacts.

| Daily-v1 surface | Current evidence | Remaining boundary |
|---|---|---|
| Crime/Social Appraisal | The exact singleton TheftOutcome, CrimeKnowledge, and SocialReaction stores are in the selected-profile section inventory. The bounded coordinator covers reviewed direct writes, nested theft acceptance, and standalone knowledge/appraisal; it tracks local count/revision and shared-epoch changes. Exact code/review/test evidence is promoted. | This closes only these three owners and listed paths. It does not prove all Crime/Justice writes, all writers into the shared epoch, or complete P12-B coverage. |
| Population and lifecycle | The promoted population adapter registers per-City aggregate/receipt and Person/NPC lifecycle sections and six population/person operation IDs. Effective selected-profile natural mortality and aggregate demography remain disabled as tested; excluded direct ingress remains fail-closed under its reviewed contract. | Preserve the promoted operation boundary; no generalized lifecycle-operation coverage follows. |
| Runtime operation inventory | `SimulationRuntime.cs` currently registers membership, bootstrap publication, daily advance, solo travel start, TravelParty advance, NPC trade/transfer, Market purchase/sale, and Merchant daily trade. `P12PopulationLifecycleCensus.cs` adds six bounded lifecycle IDs. Crime/Social Appraisal uses its own small coordinator enum (`TheftAcceptance`, `KnowledgeAndAppraisal`), not a general operation framework. | These are implemented bounded sets, not a complete supported-writer inventory. The outer daily operation counts activity; it does not itself mean every changed owner is registered or epoch-notified. |
| Older operation-footprint audit | `PHASE12_P12B_OPERATION_FOOTPRINT_REFRESH.md` at `4f12586` remains useful historical source evidence, but it predates later P12 integrations and includes P12-F Expedition paths. It explicitly leaves complete owner/cardinality and changed-section mapping open and does not authorize another runtime callback/operation ID. | Do not wire a callback or add an operation ID from that older matrix alone. First reconcile the current Daily-v1 source delta and classify supported versus excluded paths. |
| Remaining blocker | Promoted Crime/Justice, Crime/Social, population, and existing travel/economy slices can be marked covered within their exact reviewed boundaries. Other owners and direct public mutation paths remain to be reconciled against effective Daily-v1 reachability and shared-epoch notifications. | The next bounded task is a current-base, Daily-v1-only owner/operation/epoch delta audit. Select a code checkpoint only if that audit proves one specific supported ingress, exact owner set, temporal identity/cardinality, and commit boundary. No implementation checkpoint is presently marked READY. |
| Readiness | P12-B `INCOMPLETE`; P12-A `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B+C; P12-F waits on C/D/E; P12-G waits on B–F plus live-profile inventory; P13 remains blocked. | No owner-completeness, shared-epoch completeness, global quiescence, capture eligibility, export, hydration, downstream readiness, or Phase-closure claim. Keep Expedition work deferred to P12-F. |

This refresh supersedes the preceding “CrimeSocialAppraisal is the next audit/design/implementation” statements. It does not select a new implementation surface by guesswork. A fresh source audit is the current executable blocker-resolution task; do not infer readiness from the promoted evidence alone.

### Audit disposition — individual travel advancement

Current source review traced the selected daily call from `SimulationRuntime.AdvanceDayAfterClockAdvance` to `TravelSystem.AdvanceTravels`. Each committed `ClearTravelStartedToday` or `AdvanceTravelDay` transition reaches the installed NPC's `NpcRuntime` P12 travel-state admission callback. The runtime validates the exact per-NPC travel-state section and any changed City-presence sections before mutation, then publishes the changed sections to the shared protocol after the commit; grouped TravelParty advance has its separate reserved operation scope. This closes the old audit lead for those individual travel-state leaves within this precise path. It does not cover every public travel entry point or all P12 owner/epoch writers, and it does not justify a new `AdvanceTravels` runtime operation ID.

The remaining P12-B task is still a current Daily-v1 owner/operation/epoch inventory across all effective writers. Do not treat the old audit's other leads as implementation-ready until the same identity, cardinality, ingress, and notification mapping is completed for each.

## P12-B owner/operation/epoch refresh — after normal TravelParty start promotion — 2026-10-05

P12 canonical now includes the bounded selected-profile normal
TravelParty-start operation at `9395c43e8ad8ae516a2fba554dc8e48d0b5895a5`.
Its executable code is `2bc6d3264c76347eed21b70dcfcde98533f7aa66`, tree
`9043a8da8718a364f602eee56aaf48f83f06ad51`; exact-tip independent review
and exact-tree validation are linked from `PHASE12_STATE.md`.

| Daily-v1 surface | Current evidence | Remaining boundary |
|---|---|---|
| Normal TravelParty group start | The selected-profile path opens `runtime.travel-party.start` before ID allocation, binds its exact TravelParty allocator witness and required owners, preflights local revision capacity, and batches successful committed writes into one epoch. A post-write notifier fault preserves the committed start, fault-closes the protocol, and exits the owner-thread scope with zero active count. | Direct P12-bound unwrapped group starts reject before domain writes. This does not cover Expedition start/return, every TravelParty API, arbitrary standalone writers, or all supported operation paths. |
| Individual travel advancement | The prior source audit traces selected-day `TravelSystem.AdvanceTravels` committed leaves through exact per-NPC travel-state admission and required City-presence notifications. | This remains a separate reviewed path; do not infer it is covered by the group-start operation or add a general TravelSystem operation ID from this evidence. |
| Remaining owner / writer inventory | Existing promoted census and bounded adapters remain valid within their recorded owners and paths. | Continue the Daily-v1 effective-owner/operation/epoch delta audit for all remaining reachable committed writers, direct public mutation paths, and exact cardinality/identity. No additional implementation checkpoint is marked READY by this promotion alone. |
| P12-F Expedition | Expedition owner/start work remains assigned to accepted P12-F and depends on P12-C, P12-D, and P12-E. | Keep it deferred; do not reclassify it as P12-B implementation readiness. |
| Readiness | P12-B remains INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus validated live-profile inventory; P13 remains blocked. | No complete owner census, shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A/P13 readiness, or Phase closure is claimed. |

The next P12-B task remains the source-driven Daily-v1 owner/operation/epoch
delta audit. Select another bounded implementation only after documenting
effective reachability, exact owner identity/cardinality, successful commit
boundaries, and which shared epoch changes. This is blocker-resolution work,
not a new product scope or checkpoint ID.

## Daily-v1 profile mismatch resolved — 2026-10-06

The former P10-A profile mismatch above is resolved by the user's decision to
preserve the accepted P9-B-only P12 scope. `SampleScene.unity` now selects
`Simulation-DailyV1.asset`, which retains the selected authored geography and
City/NPC inputs but clears `authoredP10RuinSite`. The existing
`Simulation-GeneralTest.asset` remains the separate P10-A proving profile.
When that P10-A config is submitted with `UnityBootstrap-Daily-v1`, bootstrap
rejects it during profile resolution before identity allocation or owner
publication. The general P10-A profile remains valid outside Daily-v1.

The current exact-profile test now sets the Daily-v1 admission context and
verifies the live owner/cardinality inventory. It reports RuntimeIdentity
cardinalities `{ NPC: 10, City: 2, Location: 2, Route: 2, ExplorableSite: 0,
LocalPlace: 0, LocalConnection: 0, NotableItem: 0 }`; P8-A has one Hex, one
anchored Location, and one scale context. It verifies the manifest is the
P9-B authored-geography profile, the P10 stage and topology owners are absent,
and every registered witness matches the exact runtime owner, cardinality,
and revision. Focused P10-A and P10-B regressions confirm the separate P10
profiles still pass and are rejected by Daily-v1. Validation is recorded in
`docs/validation/P12DailyProfileSeparation/VALIDATION.md`.

This removes the profile-identity blocker only. Continue the remaining
source-driven P12-B census, committed-write invalidation and shared-epoch
audit against the corrected profile. P12-B remains INCOMPLETE; P12-A remains
WAIT_DEPENDENCY; P13 remains BLOCKED. No complete owner/epoch coverage,
quiescence, capture eligibility, export or hydration is claimed. P12-F
Expedition remains assigned to its documented P12-C/D/E prerequisites.

## P12-B live-profile inventory blocker — P10-A profile mismatch — 2026-10-05

The Daily-v1 scene/config mismatch is recorded in
[PHASE12_DAILY_V1_P10A_PROFILE_RECONCILIATION_AUDIT.md](PHASE12_DAILY_V1_P10A_PROFILE_RECONCILIATION_AUDIT.md).
The accepted P12 Brief says the profile is P9-B authored geography and
excludes P10-A Ruin/LocalTopology, while SampleScene selects
`UnityBootstrapDailyV1` with `Simulation-GeneralTest.asset`, whose authored
P10 Ruin enables that genesis stage. The existing sample-profile census test
uses the same asset but no Daily-v1 admission context; it therefore cannot
close the P12 identity/cardinality question. The earlier “P10
LocalTopologyStore is not composed” statement describes neither that sample
test nor the current Daily-v1 scene and is superseded here.

| Surface | Current evidence | Remaining blocker |
|---|---|---|
| P10-A in SampleScene Daily-v1 | Scene profile value is 1; the selected asset has a P10-A Ruin, and the normal stage pipeline composes its exact site and LocalTopology facts. The profile fingerprint identifies P10-A. | Decide whether Daily-v1 must use a separate P9-B-only config (preserving accepted P12 scope) or P12 must accept the P10-A profile and include its state in the supported continuation contract. Do not change the scene/config or widen the profile silently. |
| P12-B census and operation work | TravelParty start remains valid for its exact reviewed owners. | Do not claim the current effective profile inventory is complete; profile identity and included LocalTopology owner set remain unsettled. P12-B stays incomplete. |
| Downstream readiness | P12-A remains WAIT_DEPENDENCY; P12-C through P12-G retain their documented dependencies; P13 remains blocked. | No downstream readiness, capture, export, hydration, or Phase closure follows. |

The profile identity/composition choice is the current gate for the complete
live-profile inventory. P12-F Expedition remains deferred to its documented
P12-C/D/E dependencies.
