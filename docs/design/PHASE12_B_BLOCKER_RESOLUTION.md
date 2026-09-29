# P12-B blocker resolution and dependency plan

**Status:** Source/API evidence pass complete; no runtime census, owner
invalidation capability, capture fence, or P12 readiness is claimed. This
record decomposes the already accepted P12-B–P12-G scopes. It adds no
checkpoint ID, product behavior, or implementation authorization.

**Evidence baseline:** P12 canonical `04105d31e88fca97888dddb8e974236a7f4b6804`;
the executable source tree is identical to validated composition `ec75e6a`.
Four independent read-only source audits covered live composition and
cardinality, C/D mutators, E/F mutators, and thread/quiescence/exact-zero
paths. No files or tests were changed by those audits. The current checkout
also has two unrelated untracked `.meta` files; they are outside this record.

## Evidence completed and evidence still missing

The audits close the *static source-map* portion of the P12-B blocker. They
identify the selected composition, reachable owner instances, mutator entry
points, current local revisions, mutable escapes, and known absent/conditional
sections. They do not prove a live evolved owner count, a revision-bound
runtime census, that every successful commit invalidates capture eligibility,
or that runtime capture is restricted to a quiescent owner thread.

| Obligation | Source evidence now available | Capability/evidence still required |
|---|---|---|
| Live owner/cardinality census | The `Simulation-GeneralTest` composition path and its provider choices are mapped. Startup authors two Cities and ten NPC configuration rows; the selected runtime starts `PersonStore` empty. The census distinguishes causal owners, continuation commitments, conditionals, excluded state, and noncausal read models. | At a live boundary, each expected owner must report exact owner identity, section/schema version, cardinality, and owner revision from the actual published composition. Startup/configuration counts cannot stand in for evolved rosters or mutable owner contents. |
| Committed-write invalidation | Successful mutation entrypoints are mapped below, including owner-local revisions and public bypasses. Multi-owner transactions, rollback-only paths, queries, plans and proposals are distinguished. | A successful authoritative commit must notify the one P12-B epoch after the owning commit (or whole multi-owner commit). Failed preflight and full rollback do not notify; a successful compensating write does. Prove every included public path is covered or no longer supported outside an instrumented boundary. |
| Owner-thread/quiescence | `Start → InitializeSimulation`, `Update → Simulate`, Space-day advancement, and wrapper travel/expedition calls are synchronous in the inspected source path. No explicit thread/task/coroutine launch was found. | Bind and verify the actual runtime owner thread; track bootstrap, outer advance and supported in-flight multi-owner operations; reject capture during any scope or from a different thread. Static absence of thread creation does not prove callers cannot invoke exposed APIs from another thread. |
| Runtime exact-zero witnesses | The profile has explicit exclusions and known conditional-empty sections. Composed-but-empty is distinct from not composed. | Obtain owner-backed exact-zero values at the same live boundary and revalidate them after collection. Do not infer zero from scene startup, a skipped consumer, or a missing runtime reference. |

## Selected-profile census targets

The following groups are the census surface, not a claim that every owner is
already exportable or hydrated. Registration must bind a concrete instance;
missing, duplicate, unsupported, or unversioned entries fail closed.

| P12 owner group | Concrete selected-profile owners to witness | Important exactness / current gap |
|---|---|---|
| C — causal roots | `RuntimeIdAllocator` (14 per-kind counters), `RuntimeIdentityRegistry` (8 typed indexes), `SimulationRecordSequence`, concrete deterministic-random provider and use/stream state, P9-B genesis manifest/provenance, P8-A geography facts. | Allocator, registry, sequence and RNG lack complete census/export/hydration witnesses. P8-A has local revision/counts, but the composed spatial authority also owns P8-B–E child sections; their explicit empty state must be witnessed separately and cannot be inferred from P8-A’s positive cardinality. |
| D — factual roots | Runtime City and NPC roster/composite state; each City’s Market, accounts, inventories and Population; Person/Genealogy/lifecycle owners; legacy `SpatialNetworkRuntime`, `ExplorableSiteStore`, and `LegacySpatialAnchorBindingStore`. | City/NPC have no composite revision. `Cities` and `ImportantNpcs` expose backing lists; legacy network exposes mutable collections. Person/lifecycle commits can span owners without a capture scope. Site identity is a root; exploration progress lives in expedition/Knowledge owners. |
| E — official/core domains | Effective-config-selected City economy, transaction service and child accounts/inventories/markets; Merchant and Commercial Knowledge sharing; Justice/Crime/appraisal; composed political, institution, property/estate, armed-force/manpower/position, conflict/war/battle stores and enabled action providers. | Local guards/revisions are not a global invalidation epoch. Direct child or system mutators bypass coordinator-level evidence. Provider membership must follow resolved effective configuration, not the serialized module list. |
| F — knowledge/commitments | Political/Crime and per-NPC exploration/adventure Knowledge; directives; default `ActorChoiceStore`; travel and party owners; `ExpeditionStore`/runtime/system. | Several per-NPC knowledge owners, directives, travel and expedition state lack complete revisions; expedition store exposes mutable runtime references. Actor-choice terminal history and sequence are causal continuation state. |
| Census-only read models | `DomainEventStore` and `NpcDecisionStore` may be counted/versioned as known read models; History is a selected subset, Chronicle is a derived view. | They are not authoritative owner sections and do not invalidate the world-truth epoch. `SimulationRecordSequence` is a separate causal C root and must be retained exactly. |

For exact-zero evidence, record these roles separately:

- **Required/populated:** P8-A’s profile facts require exactly one authored
  Hex, one anchored Location, and the authored scale context. These are
  positive-cardinality witnesses, not “nonzero is fine.”
- **Composed/known empty:** `EconomyTransactionService.keyedSaleReceipts` and
  `NpcDecisionRecorder.occurrenceReceipts` exist in reachable composed owners,
  while the selected legacy daily profile does not invoke their P18-D writers.
  Their live counts must be witnessed as exactly zero and tied to owner
  identity/revision.
- **Composed/known empty:** P8-B passage, P8-C City/Site binding and Person
  position, P8-D route Knowledge/plan, and P8-E travel-authority sections are
  composed in the selected profile and explicitly empty. Each must provide a
  live exact-zero witness; these sections are not absent merely because no
  facts are present.
- **Not composed:** P10's `LocalTopologyStore`/PlaceContent and autonomy
  services, P14-A local material flow, P18 temporal/intraday state, P19
  loader/module state, P20 activity state, and external WorldCommand
  service/queue are absent from this selected composition. Record the absent
  composition role; do not invent a count for a nonexistent instance.
- **Omitted read model:** events/decisions/history/chronicle are not causal
  truth sections. Count the selected stores only for census completeness;
  their row counts are not required to be zero.

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
| D: legacy spatial/site | `SpatialNetworkRuntime.RegisterLocation`/`RegisterRoute`; `ExplorableSiteStore.Add`; `LegacySpatialAnchorBindingStore.TryBindCity`/`TryBindSite`/`TryBind`. | Network collections are mutable/downcastable and lack revision; site store lacks revision; anchor store has only its local revision. Exploration progress is covered under F, not site identity. |
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
  P8-B–E passage/binding/position/Knowledge/plan/travel sections are composed
  empty and need exact-zero witnesses independent of the required P8-A
  geography counts. `LocalTopologyStore` is a separate absent P10 owner in
  this profile.
- Keyed deterministic `NextUnit` is pure for fixed seed/key/index in the
  selected provider; if any retained `DeterministicRandomStream` is created,
  `NextUnit` advances its private cursor and `CreateStream` must be reflected
  by the provider-use census. Do not infer stream absence from the interface.

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
  `RegisterRoute`; `ExplorableSiteStore.Add`; and
  `LegacySpatialAnchorBindingStore.TryBindCity`, `TryBindSite`, and
  `TryBind`.

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

1. **B technical contract and source evidence — complete enough for review.**
   The four audits now provide composition references, method-level
   C/D/E/F write maps, synchronous entrypoint evidence, and zero-role
   classification. Independent exact-tip review must confirm omissions and
   the P18 lease seam before this contract is used to schedule code.
2. **B coordinator protocol — next bounded design/implementation slice.**
   Specify one versioned owner-section witness registration contract (owner
   identity, role, schema, exact count, local revision/snapshot stamp), one
   monotonically invalidating epoch, owner-thread binding, and operation-scope
   accounting. Missing adapters or inconsistent witnesses return
   `OwnerCoverageIncomplete`; no capture token is issued. Do not wire
   `SimulationRuntime` or claim a live profile witness until the owner groups
   below exist. The canonical P18 advance lease remains the only advance lease.
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

The complete **static** source map is ready for independent review as a bounded
P12-B technical evidence artifact. No implementation is claimed ready yet:
the actual owner witness providers, commit notifications, owner-thread/scope
capability, and runtime exact-zero tests do not exist. After the review, the
next implementation decision is limited to the already accepted P12-B
coordinator protocol and fail-closed registration seam; any owner mutation
instrumentation must be integrated by its accepted C/D/E/F owner group. The
live profile, P12-G and P12-A remain blocked until the required owner witness
set is complete and demonstrated on the normal bootstrap.
