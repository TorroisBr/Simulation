# P12-B blocker resolution and dependency plan

**Status:** Static owner/write-map pass complete, with a partial live
day-zero census through the selected-profile bootstrap test. Complete owner
coverage, committed-write invalidation, runtime capture fencing, and P12
readiness are not claimed. This record decomposes the already accepted
P12-B–P12-G scopes. It adds no checkpoint ID, product behavior, or
implementation authorization.

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
| Live owner/cardinality census | The selected-profile test runs normal genesis against `Simulation-GeneralTest.asset` and inspects the published runtime before day one. It verifies City/NPC, population, market, inventory, and spatial-knowledge cardinalities; Person/Genealogy zero; P8-A geography; P8-B–D exact-zero owner counts/revisions; both receipt witnesses; and empty ActorChoice/directive/travel/expedition state. | This is still a partial live census: several directly observed collections have no owner revision/section-token API, and C roots plus some E/Justice owners are not published or censusable. Each included owner still needs exact identity, section/schema version, count, and revision in the actual capture provider. Source-derived counts cannot stand in for evolved state. |
| Committed-write invalidation | Successful mutation entrypoints are mapped below, including owner-local revisions and public bypasses. Multi-owner transactions, rollback-only paths, queries, plans and proposals are distinguished. | A successful authoritative commit must notify the one P12-B epoch after the owning commit (or whole multi-owner commit). Failed preflight and full rollback do not notify; a successful compensating write does. Prove every included public path is covered or no longer supported outside an instrumented boundary. |
| Owner-thread/quiescence | Source audit finds synchronous `Start → InitializeSimulation` and `Update → Simulate` paths. The selected-profile EditMode test calls `Start()` directly and captures no `Update` frame or managed-thread identity. | Bind and verify the actual Unity runtime owner thread; track bootstrap, outer advance and supported in-flight multi-owner operations; reject capture during any scope or from a different thread. The direct EditMode bootstrap test and static absence of thread creation do not prove normal-frame affinity or exclude external callers. |
| Runtime exact-zero witnesses | The profile has explicit exclusions and known conditional-empty sections. Composed-but-empty is distinct from not composed. | Obtain owner-backed exact-zero values at the same live boundary and revalidate them after collection. Do not infer zero from scene startup, a skipped consumer, or a missing runtime reference. |

## Selected-profile census targets

The following groups are the census surface, not a claim that every owner is
already exportable or hydrated. Registration must bind a concrete instance;
missing, duplicate, unsupported, or unversioned entries fail closed.

| P12 owner group | Concrete selected-profile owners to witness | Important exactness / current gap |
|---|---|---|
| C — causal roots | `RuntimeIdAllocator` (14 per-kind counters), `RuntimeIdentityRegistry` (8 typed indexes), `SimulationRecordSequence`, concrete deterministic-random provider and use/stream state, P9-B genesis manifest/provenance, P8-A geography facts. | Allocator, registry, sequence and RNG lack complete census/export/hydration witnesses. P8-A has local revision/counts, but the composed spatial authority also owns P8-B–E child sections; their explicit empty state must be witnessed separately and cannot be inferred from P8-A’s positive cardinality. |
| D — factual roots | Runtime City and NPC roster/composite state; each City’s Market, accounts, inventories and Population; Person/Genealogy/lifecycle owners; legacy `SpatialNetworkRuntime`, `ExplorableSiteStore`, and their existing runtime-ID location/site links. | City/NPC have no composite revision. `Cities` and `ImportantNpcs` expose backing lists; legacy network exposes mutable collections. Person/lifecycle commits can span owners without a capture scope. Site identity is a root; exploration progress lives in expedition/Knowledge owners. |
| E — official/core domains | Effective-config-selected City economy, transaction service and child accounts/inventories/markets; Merchant and Commercial Knowledge sharing; Justice/Crime/appraisal; composed political, institution, property/estate, armed-force/manpower/position, conflict/war/battle stores and enabled action providers. | Local guards/revisions are not a global invalidation epoch. Direct child or system mutators bypass coordinator-level evidence. Provider membership must follow resolved effective configuration, not the serialized module list. |
| F — knowledge/commitments | Political/Crime and per-NPC exploration/adventure Knowledge; directives; default `ActorChoiceStore`; travel and party owners; `ExpeditionStore`/runtime/system. | Several per-NPC knowledge owners, directives, travel and expedition state lack complete revisions; expedition store exposes mutable runtime references. Actor-choice terminal history and sequence are causal continuation state. |
| Census-only read models | `DomainEventStore` and `NpcDecisionStore` may be counted/versioned as known read models; History is a selected subset, Chronicle is a derived view. | They are not authoritative owner sections and do not invalidate the world-truth epoch. `SimulationRecordSequence` is a separate causal C root and must be retained exactly. |

### Live selected-profile day-zero evidence

`SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
passed 1/1 using the real
`Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset`. The test
creates `TesteSimulacao`, calls `Start()`, and inspects the published
`SimulationBootstrapComposition` before any day advance. It verifies:

- P8-A runtime `SpatialAuthorityStore`: one Hex and one anchored Location,
  with the selected profile's scale context and IDs.
- The two City and ten NPC roots; 1,800 aggregate population; ten market
  rows totaling 1,395 units; two NPC inventory rows totaling 8 units; and ten
  per-NPC spatial-Knowledge owners totaling 20 location observations and 10
  route observations, with revision 3 on each spatial-Knowledge owner.
- Person and Genealogy cardinality zero. City/NPC roster and public collection
  counts are real runtime reads, but do not have composite revision witnesses.
- `NpcDecisionRecorder.occurrenceReceipts`: exact cardinality 0 and revision
  0, with the same owner-instance identity on repeated reads.
- `EconomyTransactionService.keyedSaleReceipts`: exact cardinality 0 and
  revision 0, with the same owner-instance identity on repeated reads.
- `ActorChoiceStore` is composed, current day is 0, and History is empty.
- ActorChoice count, directives, travel parties, and expeditions are empty.
  These direct collection/count reads do not all carry an owner revision.
- P8-B passage options, barriers, option-state rows, barrier-state rows, and
  crossings are zero; passage is stamped by parent spatial revision 1. P8-C
  stable anchor bindings and Person positions are each `Count=0, Revision=0`.
  P8-D route Knowledge and route plans are each zero at revision 0. The P8-E
  coordinator is not a retained census section. P10 `LocalTopologyStore` is
  not composed.

This is a live published runtime composition, but the test manually invokes
`Start()` in EditMode. It does not exercise Unity `Update`, capture a managed
owner-thread identity, census all included owners, or establish quiescence.
It is not an evolved-boundary census. The broader startup figures below
remain source-derived expectations unless explicitly named above. The
unpublished allocator/identity/record/RNG roots and unwitnessed E/Justice
owners remain live census blockers; no reflection or “not called” inference
is substituted for their owner exports.

For exact-zero evidence, record these roles separately:

- **Required/populated:** P8-A’s profile facts require exactly one authored
  Hex, one anchored Location, and the authored scale context. These are
  positive-cardinality witnesses, not “nonzero is fine.”
- **Composed/known empty:** `EconomyTransactionService.keyedSaleReceipts` and
  `NpcDecisionRecorder.occurrenceReceipts` exist in reachable composed owners,
  while the selected legacy daily profile does not invoke their P18-D writers.
  Both now expose owner-issued count/revision witnesses on this feature branch;
  the selected-profile test observes exact zero for each. This closes only
  these two conditional sections, not all exact-zero or invalidation coverage.
- **Composed/known empty:** P8-B passage, P8-C City/Site-to-Location bindings
  and Person positions, P8-D route Knowledge/plans, and the P8-E component
  travel-authority sections are composed in the selected profile and
  explicitly empty. The P8-C binding owner is the runtime-installed
  `LegacySpatialAnchorBindingStore` (its historical class name does not make
  these rows the separate legacy City/NPC runtime-ID links under D). Each
  section must provide a live exact-zero witness; these sections are not
  absent merely because no facts are present.
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
  runtime `SpatialAuthorityStore` at revision 1. Allocator, registry and
  sequence do not yet expose exact owner-issued count/revision witnesses.
  Preserve the separate P12-C candidate
  `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835`: it contains
  reusable allocator and record-sequence snapshot seams, but is based on a
  stale sibling base and must be selectively reintegrated after B promotion.
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
  counts at capture.
  `SettlementPopulationRuntime` has its own count/revision but does not cover
  Person, residence, genealogy, death or materialization edges; `PersonStore`
  has no count/revision pair and `GenealogyStore` has count/records but no
  revision. `SpatialNetworkRuntime` exposes mutable collection views without a
  revision, and `ExplorableSiteStore` has no revision. Thus the derived startup
  counts do not yet form owner-backed census sections.
- **E owners:** the two `MarketRuntime` instances start with five item rows
  each and 1,395 units total; ten NPC-owned accounts/inventories are composed,
  with two authored NPC inventories containing four units each. Market and
  inventory revisions exist, but no market-row or aggregate NPC-account/
  inventory census was found. Several political stores expose local
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
  no revision; `ScheduledDirectiveStore`, `TravelPartyStore`, and
  `ExpeditionStore` expose collections but no sufficient count/revision pair.
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
| C roots | All `RuntimeIdAllocator.Allocate*` operations; `RuntimeIdentityRegistry.Register*` and local-topology batch registration; event/decision record allocation through `SimulationRecordSequence.Allocate`; any mutable RNG stream draw retained by the selected provider; P9 genesis publication; P8-A geography and any excluded child-store writes. | Private counters/indexes and record-sequence next value lack census/revision. Keyed RNG calls are pure for fixed seed/key/index, but the provider interface permits mutable streams; admission must bind concrete provider and use census. P8-A’s one revision spans P8-A and excluded children. |
| D: roster and NPC/City | `TryRegisterNpc`/`TryUnregisterNpc`; City/NPC presence and travel/status/action/life/hidden/plan setters; `AddImportantNpc`/`RemoveImportantNpc`; City production/consumption/price update; direct Market/Inventory/MoneyAccount writes. | `Cities` returns its backing list; `ImportantNpcs` and NPC status/child runtime objects are mutable. City/NPC lack a composite witness; children have only local revisions. |
| D: population/identity | Population transitions and paired migration; Person registration/binding/unbinding; genealogy add/remove; named birth; materialization/adoption; residence binding; immigration/emigration/resident death and Person death; NPC residence migration. | Birth/materialization/death/migration can expose an intermediate multi-owner graph before the final leg. Static public lifecycle entrypoints can bypass `SimulationRuntime` wrappers and partial political revision. Rollback is not a committed truth change if complete; failed restoration faults the runtime. |
| P8-C: canonical spatial presence | `LegacySpatialAnchorBindingStore.TryBindCity`/`TryBindSite`/`TryBind`; `PersonSpatialPositionStore.TrySetAt`, `TryBeginTransit`, `TryAdvanceTransit`, `TryArrive`, and P8-E prepared installs. P8-E outer travel operations may change more than one section and notify once after complete installation. | Both stores expose owner-local count/revision but have no census registration or P12 epoch connection. The class name `LegacySpatialAnchorBindingStore` is historical: P8-C owns its stable City/Site-to-P8-A-Location bridge. |
| D: legacy spatial/site | `SpatialNetworkRuntime.RegisterLocation`/`RegisterRoute`; `ExplorableSiteStore.Add`; embedded legacy City/NPC runtime-ID location/site links. | Network collections are mutable/downcastable and lack revision; site store lacks revision. These links are separate from P8-C's City/Site-to-`LocationId` bridge and Person positions. Exploration progress is covered under F, not site identity. |
| E: economy/merchant | Successful `EconomyTransactionService.Try*` transfers, trades, purchases/sales, consumption, travel charges and successful compensation; child Money/Inventory/Market writes; legacy City daily economy; Merchant state/knowledge/effect commits; Commercial Knowledge observations/share commit. | Transaction service is not guard/epoch-bound; child APIs are directly callable. City has only a staged daily receipt. Merchant urgency receipt covers only its staged step. Commercial Knowledge can be mutated directly. |
| E: justice/crime and core owners | Successful crime action/hidden-status effects; Justice warrant, arrest, sentence, escape, wanted-status and failed-escape transitions; crime outcome/Knowledge/appraisal commits; institution/office/tenure writes; force/manpower/position writes; conflict/war/battle registration, participant, start/end and final resolution; faction/claim/support/Political Knowledge/property/estate commits. | Guard binding and local revisions protect selected subsets only. Aggregate and cross-owner operations need one notification after successful installation; proposals/prepares/computes/queries do not notify. |
| F: knowledge/directives/choice | Per-NPC Adventure/Exploration Knowledge observations; directive add and terminal processing-state transitions; every successful `ActorChoiceStore` capture/defer/reject/dispatch/returned/threw transition; travel start/advance; party add/complete/remove and travel; expedition add/remove/complete/start/progress/traversal/resource/opposition/return/reconciliation. | Direct per-NPC writes can bypass guard-bound systems; directives have no revision; travel/party lack a complete receipt; ExpeditionStore and `ExpeditionRuntime` expose mutable commitment state. |

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
   P8-B–D, receipt, directive, ActorChoice, travel and expedition values from
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
3. **C owner witnesses/exports:** allocator/registry/record-sequence/RNG
   roots, P9-B genesis provenance, and exact P8-A facts. Capture exact
   identity/index state and provider-use state without rerunning genesis.
   Integrate only after the B protocol is promoted, as required by the Brief.
4. **D factual owner witnesses/exports:** City/NPC roots and child owners,
   population/Person/Genealogy lifecycle, legacy network/sites/anchors. Split
   work by owner and hotspot only after the reviewed B API; birth,
   materialization, death and migration must have a single outer in-flight
   scope and successful-commit notification.
5. **E core and official provider witnesses/exports:** economy and merchant
   owners, justice/crime/appraisal, and selected political/institutional/
   property/military/conflict stores. Develop alongside D only in isolated
   owner worktrees; integrate against the same B contract and actual effective
   provider set.
6. **F knowledge/commitment witnesses/exports:** directives, P11 choices,
   Knowledge, travel/party/expedition. Begin implementation only after C, D,
   and E canonical dependencies are met; preserve exact terminal ActorChoice
   state and reject nonterminal states as already specified.
7. **B runtime profile proof, then G:** compose the exact census from the
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

The accepted P12-B census work now has owner-issued witnesses for two
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

This slice owns the two receipt owners and narrow bootstrap witness
publication/tests. It does not touch the P18-owned `SimulationRuntime`
hotspot, City/NPC, Person/population, core political/military, other Knowledge,
directives, travel, or expedition owners. Its result is two composed-empty
exact-zero witnesses, not complete P12-B readiness.

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
backing list; `CityRuntime.ImportantNpcs` and multiple NPC child values are
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
it is not a complete live census or mutation-completeness proof. The
owner-local `NpcDecisionRecorder.occurrenceReceipts` witness candidate on
`codex/phase12/P12BOwnerReceiptCensus` at `2ef4a83` passed exact-tip review.
The current branch adds `EconomyTransactionService.keyedSaleReceipts`; its
45 focused transaction tests and the 1-test selected-profile bootstrap run
passed. The live run verifies both exact-zero receipt sections and P8-A
cardinality on the published day-zero runtime, but it does not invoke a Unity
`Update` frame or establish thread ownership/quiescence.

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
