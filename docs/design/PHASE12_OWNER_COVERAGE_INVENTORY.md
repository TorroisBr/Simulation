# P12-A owner coverage inventory — evidence only

**Profile:** `UnityBootstrap-Daily-v1` (the bounded SampleScene
`Simulation-GeneralTest.asset` → `TesteSimulacao.InitializeSimulation` daily
profile). **Reviewed source baseline:** this P12 revalidation candidate starts
at `4a1d36407487e3d342785d4d895993044d610cf4`. Following `git fetch --all`
on 2026-09-28, exact upstream tips checked:
architecture `c285466c355103d3637ac165246591b72eb7bda0` and both alignment
records: intraday `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`, multi-participant
`c285466c355103d3637ac165246591b72eb7bda0`; P8 State `470667d37863384edadb3d93ef64d8004aff46a3`; P9 canonical
closure `82396ae7ffaf407fda278928da456b06dc5394d4` (P9-B code integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`, implementation
`00395ef80cfa2364d34ed2170e0735d3a4b1513d`, promotion State/status record
`14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`); P10 canonical/Brief
`252ad6b9a507f1c001c05a1e19c2546ebd0707a2` (P10-A code
`9501bf076d506fb64d6ee3e6d178574fff36e153`, State record
`9e79b58397dc9a89ddcc562be139b79987cb55b9`); P11 canonical closure
`308e24d0744112e8f2b741521b8b3e4acb51ebbf` (code promotion
`0cd4281804ecc6a2d110352d1a238959e93867f0`, promotion State
`0803670cfa2c39163b54ff46a21daa06df5a16f6`);
P14 historical promotion State `f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`
(P14-A code `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`); current P14
canonical State/Brief `4caecbbfb0464c965811402b3c11d8717605114a` is a docs-only
update; P18 canonical code tip `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`
promotes the economy sale-owner receipt/prepared-install capability and the
per-runtime serialized advance lease. The current P18 State evidence tip
is `f1cfed3`; it records the current status without changing canonical P18.
The approved P18-D technical design alone did not establish
full-consumer readiness. The earlier assembled P18-D integration snapshot is
`5d7eb2687c3866fb2399faf8b366a299484d8dd4` (code integration
`3d9c0ea508e542e8a7e92a4982fbec3e29560e81`), including the reviewed
CommercialKnowledge sharing receipt/prepared-install owner and its focused
29/29 suite; it did not wire the full chronological consumer into
`SimulationRuntime`. Since that integration snapshot, isolated owner work has
advanced: local-observation/SellGoods foundation `P18DSellGoodsIntegration`
`43363dd`; demography owner `P18DDemographyOwner` `ddcac0b` (independent
focused review PASS); actor-choice bridge `P18DActorChoiceDecisionBridge`
`4016a73` with execution record at `924cfee9b41c77274141795f0f7ddcd117819f89`
(exact-tip review PASS; focused 5/5), alongside separately reviewed candidate
`P18DActorDecisionBridge` `10dcfda`; and merchant trade-state owner
`P18DMerchantTradeStateOwner` `b05feafd4f95b1a3a559e6d58de339334df57365`
(independent exact-tip PASS; Merchant 8/8 and local observation 6/6 with
retained XMLs). The reviewer noted only a nonblocking diagnostic-message
difference when fallback and redirect diagnostics coincide; simulation
effects and owner results are unchanged. The current composite candidate
`3ddf8476842ad24b9234ccaa65a530722ead8eb4` integrates these bounded owner steps
with chronological `SimulationRuntime` ordering and successful P18-C handoff
when an explicit intraday profile is selected. Runtime implementation remains
`3ddf847`; test-only consumer replay regression is `0887d18`, with
comment-accuracy follow-up `6a4d971` carrying fresh validation.
The candidate is pushed but not canonical; it does not make P18 state part of
this P12 profile. P18-D exact-tip implementation review covered code `6a4d971`
and State `254385e`; State tip `f1cfed3` records the PASS. P18-D remains open pending canonical promotion and
explicit runtime-hotspot handoff. Validation reruns are needed only if review
requests fixes.
P20
canonical/State
`7a81cc0ecbc511dd36c248ec62c7b20f7e477f53` (P20-A promoted, code
`22df7b307528e705e6e84d1d8d54852a17cfc848`); and the current architecture
alignments.

Current-base revalidation also inspected P9/P11 composition candidate
`af656e7710fce0ba171fae1d6684331d2dc0b743`, which is validated but
unpromoted, plus both current alignment records. Candidate composition evidence
does not substitute for canonical live-composition evidence. The exhaustive
source-method census below is a source/API map only: it is not a live
owner-revision census, committed-mutation notification proof, or complete
invalidation guarantee.

### Current P18-source revalidation — 2026-09-29

Revalidated this inventory against promoted P18 canonical code `9e790c5`
(`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`) and the current pushed, not-yet-
canonical P18-D consumer candidate `3ddf8476842ad24b9234ccaa65a530722ead8eb4`.
The candidate is based on integration merge `a2a8eddc4cd51c17cdc94208c1817fd948432113`,
which includes P14 canonical State/Brief `4caecbb` and P14-A code `c44904b`.
Unlike the earlier isolated owner candidates (`3cf89c0`, `89f5c55`, and
`5d7eb268`), this candidate now composes chronological P18-D advance,
crossed-boundary owner steps, and the successful post-advance P18-C handoff
when an explicit `P18DIntradayProfile` is supplied. It also routes temporal
P11 ActorChoice inputs through the P18-C decision bridge and the keyed
SellGoods consumer. This is candidate-only code; it has not changed P18
canonical behavior or the selected P12 bootstrap.

The P18-D candidate also contains P14-A local daily material-flow code from
the merge. Its intraday composition rejects a City with local material flow
before a P18-D advance until the temporal owner adapter is integrated. The
accepted P12 profile remains the legacy SampleScene/`Simulation-GeneralTest`
daily profile: the selected `TesteSimulacao` construction does not supply a
`P18DIntradayProfile`, and its authored Cities do not configure P14-A local
material flow. Do not infer P18 or P14 inclusion in P12-A from the candidate
merge.

The accepted `UnityBootstrap-Daily-v1` profile continues to exclude all P18
temporal state, including timeline, activity lifecycle/availability,
continuation extension, ActorDecisionRequestState, P18-D execution progress,
and intraday progress. P18 `SimulationTime` clock projection is an open
compatibility seam for any later temporal consumer: its mapping from the daily
profile's completed-day boundary has not been composed or validated. Record it
as a dependency to resolve if a profile ever includes P18 time, not as new P12
temporal scope.

The P18 identity/cardinality contracts remain unchanged and are preserved by
the current candidate: `PersonId` remains distinct from `ActivityInstanceId`;
an activity has one or more participants; and a transition/source receipt may
fan out to participant-specific operations or requests, each with its own
stable operation/request identity. The two-Person P20 fixture does not impose
a universal participant count. These constraints do not make activities or
P18 temporal state part of this profile.

This remains a documentation-only cross-branch inventory. The proposal branch
does not inherit P11 executable code; P11 canonical was inspected separately
for current composition facts. P11's canonical `SimulationRuntime` composes an
`ActorChoiceStore` by default. The selected bootstrap has no external
`WorldCommand` service/queue; if an implementation candidate uses current P11
composition, preserve every terminal choice input/disposition, duplicate
WorldCommand-ID history, and next sequence. Reject `Pending` (including
deferred) and `ConsumedAwaitingTerminalAttempt` state. `Rejected`,
`AttemptReturned`, and `AttemptThrew` are terminal; after a throw, capture is
allowed only at a later successful daily boundary while the runtime is healthy.
The exact candidate `af656e7` contains P11 composition, but remains unpromoted;
canonical combined composition must be revalidated before this becomes live
profile evidence. P18-A/B/C and the additive P18-A continuation extension are
promoted at P18 canonical `9e790c5`; the extension was accepted under contract
`2175bf2` (acceptance record `9de70ae`) and promoted at integration `1dd0479`.
The P18-D candidate's timeline, activity lifecycle/availability, temporal
ActorChoice bridge, P18-C request receipts, per-request execution progress, and
commercial-sharing owner are not composed by the selected legacy
`TesteSimulacao` profile. Its optional intraday SellGoods consumer calls the
promoted keyed-sale API, so `EconomyTransactionService.keyedSaleReceipts` may
be populated when that candidate profile executes. This does not change the
P12-v1 boundary: its legacy daily SellGoods path does not call
`TryExecuteKeyedMarketSale`. The service is composed in P12-v1, so treat its
retained receipt collection as a known-empty conditional section and require
an exact-zero census; reject populated or unverified receipt state. If a P12
profile later admits the P18-D consumer, re-inventory and export/hydrate the
sale receipts and all composed P18 causal owners rather than applying the
legacy exact-zero assumption. The current P12 profile claims no P18-D or P14-A
consumer inclusion.

This is a read-only gap inventory from the current runtime composition and
owner APIs. **No profile-included group currently has a demonstrated complete,
exact immutable P12 export plus staged hydration path.** Existing construction,
clone, transaction rollback, Unity serialization and diagnostic snapshot APIs
do not establish those capabilities. There is no P12 save envelope, complete
owner hydration factory graph, or staged runtime publication/single-swap path.
The profile therefore remains `WAIT_DEPENDENCY`.

## Selected Unity bootstrap evidence

`SampleScene.unity` selects `Simulation-GeneralTest.asset`. That authored asset
references two Cities (`City-CampoVerde` and `City-SerraDeFerro`), 10 NPC
configuration rows, 12 action definitions, five job definitions, two NPCs
with initial inventories of four units each, and no initial warrants. It
serializes the Economy, Merchant, Crime, and GuardCrime modules. These are
starting-content facts only; they do not cap evolved NPC counts, inventory,
warrants, plans, or other runtime state. The effective configuration resolves
which optional systems actually run.

The normal bootstrap constructs City/NPC/site/route state, `RuntimeIdentityRegistry`,
legacy `SpatialNetworkRuntime`, `ScheduledDirectiveStore`,
`EconomyTransactionService`, `ExpeditionStore`, travel/travel-party/expedition
services, and `JusticeSystem`. It composes `MerchantSystem` and commercial
Knowledge sharing when effective merchant policy enables them, `CrimeSystem`
infrastructure for the configured justice domain, and crime/guard action
providers when the corresponding effective policies enable them.
`SimulationRuntime` composes `ActorChoiceStore` by default. History/domain-event
and decision stores/recorders are also created for audit/diagnostic records;
those records do not substitute for their owning world-truth stores. This
evidence bounds the profile's real composition; it does not demonstrate export
or staged hydration for any of these owners.

### Omitted noncausal read-model census classification

The selected `NpcDecisionStore`, `DomainEventStore`, `HistoryStore`, and
`NpcChronicleService` are classified as `OmittedNonCausalReadModel`, not as
serialized owner sections. Source evidence: `NpcDecisionStore` in
`DecisionRecords.cs` records and indexes decision records; `DomainEventStore`
in `DomainEvents.cs` records events and supplies participant queries;
`HistoryPolicy` selects some `DomainEventStore.Record` outputs for the
`HistoryStore` subset; and `NpcChronicleService` in `NpcChronicle.cs` derives
Chronicle entries by querying the decision and event stores. The source audit
also found event reads for observer feeds in `WorldObserverReadModel.cs` and
command-result reporting in `CoreWorldCommandHandlers.cs`. These records are
not future-decision inputs or authoritative world truth. History is a subset
of DomainEventStore output, not a separate owner; Chronicle is a derived query.

The census role requires the known/versioned `NpcDecisionStore` and
`DomainEventStore` read-model owners to report current cardinality and revision,
but permits nonzero rows. `HistoryStore` is reported only as the
`HistoryPolicy`-selected subset of `DomainEventStore`, not as a separate owner;
`NpcChronicleService` is a derived view with no independent owner census. No
exact live boundary counts have yet been demonstrated through a P12
owner-census API. These rows are omitted from serialized owner
sections/hydration and do not advance the world-truth mutation epoch or count
toward continuation-owner completeness. The shared `SimulationRecordSequence`
remains a separate P12-C causal root and must retain its exact next value. This
does not promise historical, Chronicle, activity-feed, or UI-feed parity.

This classification does not relax profile exclusions: populated P8-B through
P8-E, P10, P14, P18, P19, or P20 authoritative state still rejects admission.
External `WorldCommand` service/queue composition and unknown or injected
providers remain independently prohibited. The classification is not a P12-A
readiness claim.

### Exact P9-B/P11 composition audit — 2026-09-27

The additive integration candidate `af656e7710fce0ba171fae1d6684331d2dc0b743`
was inspected locally as a two-parent merge: first parent P11 canonical
`308e24d0744112e8f2b741521b8b3e4acb51ebbf`, second parent P9 canonical
`82396ae7ffaf407fda278928da456b06dc5394d4`, with common base
`470667d37863384edadb3d93ef64d8004aff46a3`. It is a validated composition
candidate, not a canonical promotion; no remote fetch was performed for this
audit. The selected `SampleScene.unity` MonoBehaviour references the asset with
GUID `ba87bf49ee034da6bda3daeef8e40c3f`,
`Simulation-GeneralTest.asset`. `TesteSimulacao.InitializeSimulation` runs the
declared P9 genesis stages, validates the candidate composition, then publishes
one `SimulationBootstrapComposition` whose `Runtime` is the P11-based
`SimulationRuntime`. The selected-profile test checks both the P9-B geography
and the P11 store on that published runtime.

The GeneralTest module list resolves to Economy, Merchant, GuardCrime, and
Crime. `TesteSimulacao` resolves `EffectiveSimulationConfiguration` from those
content overrides; effective values, not the module-list compatibility view,
define providers. `RebuildSystems` always composes legacy travel, travel-party,
expedition, scheduled-directive, justice, and NPC-decision systems. With this
profile it additionally composes merchant trade and commercial Knowledge
sharing, crime infrastructure and its enabled action provider, and the guard
action provider. City production, free-population consumption and market-price
updates run through `CityRuntime` when effective Economy is enabled. Natural
mortality and aggregate demography are disabled by the selected asset's default
overrides; no sample provider is injected. The fixed-seed toggle is false, so
the current bootstrap constructs one `DeterministicRandomSource(0)` shared by
`SimulationRuntime`, `CrimeSystem`, and `NpcDecisionSystem`. The completed
P12-C RNG census on this validated composition candidate found only pure keyed
draws, each using default draw index 0: `npc-decision|<NpcRuntimeId>|<day>`,
`crime-steal|<thief NpcRuntimeId>|<day>`, and
`action-success|<actor key>|<decision key>|<action key>|<day>`. The current
profile retains no `DeterministicRandomStream` or `CreateStream` cursor. Thus
minimum continuation evidence is the effective seed/source provenance and
provider/algorithm/build compatibility, together with the saved IDs and clock
values needed to reconstruct these keys; mutable-stream state is explicitly
empty. An unknown or injected provider, or any retained mutable cursor, rejects
this profile until re-audited. Conflict, battle and demo stream providers are
not selected and remain excluded. Hash-based demographic providers are disabled
by the selected defaults; re-audit them if the profile/effective configuration
changes. This census characterizes the validated candidate, not canonical or
promoted P9-B/P11 composition.

| Accepted P12 checkpoint | Exact live owners/sections for this composition | Current boundary and implementation gap |
|---|---|---|
| **B — admission and completed boundary** | `SimulationBootstrapComposition`/`SimulationGenesisManifest`; selected config and effective provider graph; `SimulationTime`, `SimulationCalendar`, `SimulationRuntime` mutation health and successful advance boundary. | The genesis manifest/fingerprint is not a P12 envelope or boundary token. The runtime's current mutation guard and daily calls do not provide a complete profile census, mutation epoch, thread/operation lease, or capture eligibility protocol. |
| **C — identity, genesis, random roots** | `RuntimeIdAllocator`, `RuntimeIdentityRegistry`, `SimulationRecordSequence`; the shared `DeterministicRandomSource`; P9-A/P9-B manifest, stage lineage, authored inputs/outputs and provenance; `SpatialAuthorityStore` P8-A Hex, anchored Location and scale. | Allocator, registry, sequence and random source are bootstrap/runtime-owned; no exact immutable export or staged restore path was found. For the validated `af656e7` candidate, the RNG provider census is complete for selected paths: one seed-0 source shared by runtime/crime/NPC decisions; keyed draw index 0 for the three key families above; no retained stream cursor. Preserve effective seed/source provenance and provider/algorithm/build compatibility, plus IDs/clock needed to reconstruct keys; encode mutable-stream state as explicitly empty and reject unknown/injected providers or cursors. Nonselected conflict/battle/demo streams remain excluded, and disabled demographic hash providers require re-audit if profile defaults change. Never rerun genesis. P8-A cardinality is exactly one Hex/one anchored Location/one scale context. |
| **D — factual roots and Person/population relations** | The two `CityRuntime` identities, selected definition/reference links and population aggregates; configured NPC roster/`NpcRuntime` identities and the core actor facts assigned to D (life/condition, base status, current location, inventory and money); `PersonStore`, `GenealogyStore`, and legacy `SpatialNetworkRuntime` locations/routes, `ExplorableSiteStore`, and legacy City/Site links. | The 10 configured NPC rows start NPC-only; runtime-created Person and genealogy authorities start empty. Keep `NpcRuntimeId` distinct from `PersonId`. D and E/F must not independently serialize overlapping fields: CityRuntime and NpcRuntime adapter boundaries are called out below. Initial empty site/warrant/directive lists do not bound later supported state. |
| **E — core and selected daily-domain owners** | City-owned daily economy sections from the same two `CityRuntime` roots: market/custody, stocks and balances, production inputs/results, free-population consumption, and mutable price/owner revisions read by later execution. Also provider-owned `MerchantSystem`/commercial sharing state, `JusticeSystem`, `CrimeSystem`, `CrimeSocialAppraisalWorldState`, enabled crime/guard providers, and every instantiated core `InstitutionStore`, `OfficeStore`, property/estate, claim/recognition, faction/support/Knowledge/decision, armed-force/manpower/position, Conflict, War, and Battle authority. `LocalTopologyStore` is instantiated empty and is an explicit empty/reject-if-populated section. | `CityRuntime` is one concrete owner spanning D roots/population and E economy; one owner adapter must take a single consistent snapshot and emit separate semantic sections under the same revision, and staged hydration must reconstruct the owner once from both sections. Runtime core authorities are constructed even when empty; every supported populated authority needs its own export/hydrator, revision and relation checks. P10 Ruin/LocalTopology facts are excluded. |
| **F — Knowledge, directives, P11 choices and commitments** | NPC spatial/commercial/exploration Knowledge; `ScheduledDirectiveStore`; P11 `ActorChoiceStore`; `TravelPartyStore`, `ExpeditionStore`, active travel/expedition progress, and `NpcRuntime` current action and merchant-plan commitments. | `NpcRuntime` is one concrete owner spanning D actor identity/factual state, E-provider-written outcomes, and F Knowledge/commitment fields. Use one owner snapshot/revision and one staged `NpcRuntime` hydrator; assign each value to one semantic section and merge those sections before hydration, with no duplicate field export/write. E captures distinct provider-owned stores; when an E provider writes an NpcRuntime field, the NpcRuntime section carries that value. `MerchantSystem` provides E behavior while its active plan is F owner state. `ActorChoiceStore` is present by P11's default `SimulationRuntime` constructor path, but no external `WorldCommand` service/queue is composed. Preserve full terminal history, duplicate-ID history and sequence; pending/deferred or consumed-awaiting-terminal entries reject capture. No Knowledge or active commitment has complete export/hydration. |
| **G — staging, graph validation, publication and parity** | The entire B–F section set and exact composition; fresh runtime graph, owner validators, mutation guard, indexes, and one bootstrap/session publication point. | `TesteSimulacao` publishes genesis once but has no private P12 restore stage, complete cross-owner validator, atomic replacement protocol, or continuation-parity suite. Existing diagnostic snapshots, cloning and transaction rollback are not substitutes. |

The following authorities are composed empty or excluded and must remain
explicitly represented/checked according to the accepted profile. The
`SpatialAuthorityStore` has only the selected P8-A geography; its P8-B passage
child, P8-C canonical City/Site anchors and Person positions, P8-D route
Knowledge/plans, and P8-E travel authorities remain empty. Existing legacy
`SpatialNetworkRuntime` routes and legacy travel/party/expedition state are
included under D/F and do not stand in for P8 authorities. `LocalTopologyStore`
is created empty by `SimulationRuntime`; the P10 Ruin/LocalTopology output is
not composed and populated P10 facts must not slip into this profile. GeneralTest
has no P14-A exogenous material-source configuration; legacy City production
and ordinary market stock remain included D/E. The selected bootstrap does not
compose P18 timeline/activity/availability/continuation state, P19 module or
loader state, P20 shared activities, P13 reconstruction/fork state,
`PlaceContentStore`, `AdventureExpeditionAutonomySystem`, or an external P11
WorldCommand queue. Excluded populated state must reject admission rather than
be silently omitted.

## Exhaustive candidate-source mutation census — `af656e7`

The source-method census below is exhaustive for the candidate code snapshot
`af656e7710fce0ba171fae1d6684331d2dc0b743`. It classifies supported public
mutation surfaces, available guard/revision evidence, live mutable APIs, and
the consequences for continuation invalidation and export. It is not a live
owner census or proof of complete revisions: no P12 owner census/revision
providers, mutation epochs, operation scopes, exact exports, or hydration were
implemented by this audit. Refresh it against the actual canonical composition
after the combined P9-B/P11 application composition is promoted and before
claiming P12-B readiness.

| Owner group | Supported public mutation examples and owner guards | Revision evidence | Live API exposure; continuation consequence |
|---|---|---|---|
| **B — Runtime/admission** | `SimulationRuntime.TryAdvanceDay`/`TryAdvanceDays`, direct `SimulationTime.AdvanceDay`, and runtime registration/transition entrypoints are public mutation surfaces. Runtime-bound authorities may bind `AuthoritativeMutationGuard`. | The guard is a `Healthy`/`Faulted` latch, not a world revision, epoch, serial-operation tracker, or commit-notification source. Direct `SimulationTime.AdvanceDay` can advance the clock without the complete daily loop. No global mutation epoch or general active-operation registry exists. | `SimulationRuntime` exposes `PersonStore`, `ActorChoiceStore`, and services/stores. Runtime/day-loop success alone cannot prove that the full boundary is current or quiescent. P12-B needs explicit owner thread and operation scopes plus invalidation from every included committed mutation. |
| **C — Identity, RNG, sequence** | `RuntimeIdAllocator.AllocateNpcId`/other allocation methods, `RuntimeIdentityRegistry`, `SimulationRecordSequence`, and the shared deterministic random provider are causal roots; preserve the exact allocator/high-water, identity, record-sequence, and random-provider state required by this profile. | The source census found no complete continuation revision/export path for allocator, registry, record sequence, or random source. The candidate RNG draw/provider audit remains candidate-scoped and does not create a mutation epoch. | Runtime-owned providers/roots are reachable through composition. Capture exact owner state and provider compatibility; reconstruct keyed draws from their saved IDs/clock only as specified in the candidate RNG census. The shared record sequence remains causal even though decision/event read-model rows are omitted. |
| **D — Roots, demography, spatial** | Examples include `SimulationRuntime.TryRegisterPerson`, `TryApplyPersonDeath`, `TryMaterializePerson`, `TryApplyImmigration`, population transitions, genealogy changes, NPC register/unregister/materialization, and spatial/site operations. `SettlementPopulationRuntime` has a per-settlement revision; C spatial and Person spatial-position stores also expose revisions. | `PersonStore`/`PersonRuntime`, genealogy, explorable-site store, legacy `SpatialNetworkRuntime`, and NPC roster/materialization lack complete revisions. Population revision does not cover residence, death, or materialization changes to Person/NPC facts; spatial revisions are limited to their respective stores. | `CityRuntime` and `NpcRuntime` are live mutable roots, including mutable `ImportantNpcs` and `CurrentStatus` collections. Public owner methods and live objects can change facts between daily calls. Export must capture all D-owned facts and relations from exact owners; invalidate for mutations outside population revision as well. |
| **E — City/domain/political/military** | City economy and account/inventory/market operations; crime, justice, outcome, trade and commercial Knowledge operations; political methods including `TryApplyPoliticalClaimRecognition`, `TryApplyPropertyTransfer`, and `TryApplyOfficeSuccession`; force, spatial-force, conflict, war and battle transitions. Some stores bind the health guard and some expose guarded methods. Battle outcome application has staged validation. | `MoneyAccount`, `Inventory`, `Market`, and `CityRuntime` have no general revisions. Crime/justice/outcome and NPC commercial/spatial Knowledge, directive, travel, and expedition owners also lack complete revisions. Political and military transition stores have selective revisions, not universal coverage; `PoliticalWorldRevision` is partial. | Mutable NPC data and subowner references, plus exposed property/estate, `ArmedForce`, conflict/war/battle stores, allow mutation through APIs that bypass `SimulationRuntime` wrappers; some paths also bypass wrapper day validation/world-revision pathways. Export and invalidation must be owner-authoritative and cover direct APIs, not just runtime wrappers. |
| **F — Knowledge, directives, ActorChoice, commitments** | `TryRecordSpatialObservations`, `TryAcceptSpatialRoutePlan`, `TryStartTravelParty`, scheduled-directive operations, and ActorChoice lifecycle operations are representative supported direct paths. ActorChoice preserves ordered disposition/sequence history. | ActorChoice has no global-world revision. ScheduledDirective, TravelParty, Expedition, and NPC commitment/Knowledge state lack complete owner revisions. | Stores and NPC/subowner references remain directly mutable. Preserve the complete ActorChoice history and next sequence; capture current directive/Knowledge/commitment owners once, with invalidation for their direct mutation APIs. Pending/deferred or consumed-awaiting-terminal ActorChoice entries remain ineligible under the accepted boundary rule. |

`SimulationRuntime` executes the daily path across time, demography, directives,
City economy, Knowledge, NPC actions, justice/crime, merchant plans, and legacy
travel/expeditions. Scheduled directives enter through
`ScheduledDirectiveSystem`; ActorChoice processing reads `ActorChoiceStore` and
its runtime input-capture path is `TryCaptureActorChoiceInput`. This path list
does not replace the exhaustive source-method census above. The census is
source evidence only: it does not implement owner revisions, mutation epochs,
operation scopes, exports, or hydration. Keep `NpcDecisionStore` and
`DomainEventStore` as known/versioned `OmittedNonCausalReadModel` entries;
`HistoryStore` is only their policy-selected subset and `NpcChronicleService`
is a derived view. These are not authoritative mutation owners; retain the
shared `SimulationRecordSequence` separately as a causal scalar.

### P12-B entrypoint and mutation-invalidation census — partial (2026-09-29)

This source pass inspects the selected bootstrap candidate `af656e7`, P18
canonical `9e790c5`, and current pushed P18-D consumer candidate `3ddf847` /
integration merge `a2a8edd` (which also merges P14 canonical `4caecbb`). The
combined P9-B/P11 application composition remains validated but unpromoted, so
this is a source/API map, not a complete live owner census.

| Entry surface | Synchronous path and owner reach | Current evidence and remaining gap |
|---|---|---|
| Selected SampleScene bootstrap | `TesteSimulacao.Start` → private `InitializeSimulation` → P9 genesis stages and composition publication. The scene selects `Simulation-GeneralTest.asset`. | Bootstrap is synchronous on Unity's lifecycle thread, but it records/verifies no owner-thread identity or active bootstrap operation scope. No task/thread/coroutine is used by this selected path. |
| Selected day input | `TesteSimulacao.Update` handles Space → private `Simulate` → repeated `SimulationRuntime.AdvanceDay` calls. | P18 canonical `SimulationRuntime` has a serialized advance lease for its guarded `AdvanceDay`/`AdvanceDays` entrypoints. That lease does not cover direct owner writes, bootstrap, or all command/transaction scopes. P18-D candidate `3ddf847` extends the lease across chronological timeline/continuation work and P18-C handoff only when an explicit intraday profile is supplied; it is not canonical and has not handed off the hotspot. P12-B must wait for canonical promotion and explicit hotspot handoff; exact-tip review passed and validation reruns only if review requests fixes. |
| Other bootstrap-facing commands | `TesteSimulacao.TryStartTravelParty` and `TryStartExpedition` synchronously enter the published runtime/system. `Runtime`/`Bootstrap` properties expose the runtime, stores, and systems to same-process callers. | No shared operation-scope registry wraps these entrypoints or all direct owner APIs. The selected scene has no external `WorldCommand` queue. `WorldObserverTimeController.AdvanceOneDay` exists but is not bound into this SampleScene; revisit it only if included in a supported profile. |
| Included mutable truth | Runtime façade and exposed owners cover City/market/account, NPC/Person/population, legacy spatial/site, selected economy/merchant/Justice/Crime/Knowledge, P11 ActorChoice, and other profile-supported authorities listed above. Public operations include lifecycle/materialization/population, genealogy, route/observation/plan, travel/expedition, trade, and daily-economy paths. | A method-to-owner-to-committed-write map remains required. Classify proposals/queries by actual commit; guard bindings alone do not prove a committed write was counted or invalidated. Direct owner APIs and live mutable references can bypass runtime wrappers. |
| Excluded or conditional authorities | Runtime surfaces can expose P8-B..E, P10 LocalTopology, P14 material flow, P18 temporal, P19 extension, P20 activity, and other conditional authorities. | These remain outside this profile. Each corresponding provider/store must report known explicit-empty or not-composed state at admission; populated or unverified state must reject. Initial-scene emptiness is not evidence for evolved runtime state. |

**Revision, receipt, export, and hydration evidence:**
`AuthoritativeMutationGuard` is a Healthy/Faulted latch with one-way binding; it
has no mutation epoch, owner-thread check, or active-operation count. Individual
owners expose nonuniform witnesses, including revisions on Market, Inventory,
MoneyAccount, SettlementPopulation, selected spatial/Knowledge/political/
property/force/conflict owners, and operation-specific receipts. `CityRuntime`
has a private daily-economy receipt revision, not a revision for all City
truth. `NpcRuntime` and `PersonStore` have no general mutation revision.
`SimulationRuntime.PoliticalWorldRevision` covers only its political subset.
Identity allocators, record sequences, and diagnostic revisions are not global
mutation invalidation.

The P18-D candidate `3ddf847` now wires the chronological runtime consumer and
post-success P18-C handoff in its optional profile. It contains the
recipient-owned commercial-sharing receipt/source snapshot and can populate
the canonical economy keyed-sale receipt owner; it also carries the
P18-C/P11 temporal correlation and execution-progress state needed by that
consumer. These are candidate-only capabilities, not canonical P12 profile
composition, owner census, or a profile-wide mutation epoch. P14-A
local-material-flow code is present through `a2a8edd`, but P18-D composition
rejects such Cities before intraday advance until a temporal adapter exists.
Neither the candidate receipts nor existing P12 construction/clone/rollback
helpers provide exact immutable owner export or staged hydration. No included
P12 authority has demonstrated both capabilities.

**Remaining census gap:** for every supported synchronous caller, map each
owner to exact committed writes, available before/after revisions or receipts,
and any write that bypasses them. Add explicit owner-thread capture, coverage
for every in-flight operation, and unknown/missing-owner rejection. Complete
this matrix against a promoted combined bootstrap/provider graph and
revalidate after P18-D's consumer candidate is promoted and explicitly hands
off its exclusive runtime/day-loop window. Its exact-tip review and validation
have passed; reruns are needed only if review requests fixes. This
partial pass
does not establish P12-B implementation readiness, P12-A readiness,
implementation authorization, or canonical promotion.

P12-B remains not implementation-ready: P9-B and P11 are individually
canonical, but their combined application composition `af656e7` is still an
unpromoted candidate, and the live profile/provider inventory must be validated
against the exact promoted composition. Complete live owner-census/revision
proofs are also missing. P18-D2's per-runtime advance lease is promoted at P18
canonical `9e790c5`. P18-D consumer candidate `3ddf847` implements the
chronological advance/handoff path but remains unpromoted and owns the
`SimulationRuntime` source hotspot until promotion and explicit handoff. Exact-tip
review and validation have passed; reruns are needed only if a review requests
fixes. The lease does not replace P12-B's missing operation scopes or
mutation invalidation.

`SimulationRuntime.TryAdvanceDay` and
`TryAdvanceDays` enter `AdvanceDayAfterClockAdvance`, which advances time,
demography, directives, `CityRuntime.SimulateProductionDay`,
`SimulateConsumptionDay` and `UpdateMarketPrices`, Knowledge, NPC action
providers through `TryExecuteCurrentAction`, justice/crime, merchant plans,
and legacy travel/expeditions. Scheduled directives enter through
`ScheduledDirectiveSystem`; actor-choice processing reads `ActorChoiceStore`,
while its internal runtime capture path is `TryCaptureActorChoiceInput`.
Supported direct owner APIs and transactional actions can also mutate
City/NPC, Knowledge, spatial, political, force/conflict and commitment owners
between days. Therefore P12-B cannot rely on `TryAdvanceDay` alone for
invalidation. The candidate source-method census is complete, but live owner
census/revision providers and mutation notification across those direct paths
are still missing. Shared code
hotspots are `SimulationRuntime.cs` (daily loop, owner composition and runtime
stores), `TesteSimulacao.cs` (bootstrap/provider wiring and publication),
`RuntimeIdentity.cs`/random source (C roots), and the domain store/system files
owned by D–F. Any implementation wave touching `SimulationRuntime.cs` or the
daily loop must be serialized against P18-D's ownership work; D and E can only
parallelize with isolated owner/file boundaries, and F depends on their stable
sections.

**Candidate validation evidence:** the integration implementer reports, on
exact candidate `af656e7`, bootstrap 14/14, ActorChoice 24/24, Spatial 99/99,
ALL EditMode 1742/1742, official Smoke 5/5, and staged/unstaged
`git diff --check` passed. The retained result artifact available for this
inventory is the final Smoke XML (5/5); the other Unity result XML files were
rotated/removed by the harness. These results validate composition behavior,
not P12 export, hydration, owner census, or readiness. Current remote refs were
refreshed on 2026-09-28; `af656e7` remains unpromoted, so keep P12-A at
`WAIT_DEPENDENCY`.

| Included authority group | Exact immutable export | Staged hydration | Concrete gap blocking P12-A |
|---|---|---|---|
| Profile/build/content/provider identity; effective configuration, calendar, completed-day boundary and capture eligibility | No | No | No versioned P12 envelope/admission manifest or successful-advance token binds the runtime, configuration, content/providers and exact quiescent boundary. `SimulationBootstrapComposition` and runtime wiring describe construction, not a sealed continuation capture. |
| Semantic/runtime IDs, allocator/high-water marks, record sequence and identity indexes | No | No | IDs and registries are used by live owners and partly observed by diagnostics, but there is no complete immutable owner export or restore API that reinstates exact identities/counters and validates global uniqueness before publication. |
| Selected deterministic random provider and continuation state | No | No | The validated `af656e7` composition has one shared seed-0 `DeterministicRandomSource` and only pure keyed draws at index 0 for NPC decisions, crime steals, and action success; no mutable stream cursor is retained, so the profile's mutable-stream section is explicitly empty. No exact immutable provider/seed compatibility export or staged restore exists. Capture effective seed/source provenance, provider/algorithm/build compatibility, and the saved IDs/clock needed to reconstruct keys; reject unknown/injected providers or cursors. Conflict/battle/demo stream sources are excluded, and disabled demographic hash providers require re-audit if selected defaults change. |
| Bootstrap roots: Cities/markets/accounts, NPC roster and mutable NPC/action/inventory/plan state | No | No | Unity assets and `[SerializeField]` fields describe authored inputs and selected object fields; they do not capture all evolved owner state, revisions, causal ordering and references. Owner-specific value DTOs, exact admission checks and validated hydration constructors are missing. |
| Population aggregates, Persons, residence/lifecycle/materialization and genealogy | No | No | In-memory stores, registrations, clones and transaction snapshots support runtime operations/rollback, not a complete immutable representation and staged restore of aggregate/Person/NPC links, dates, relations, allocator state and owner revisions. |
| Knowledge, directives, P11 actor-choice dispositions, travel parties, expeditions and other active commitments | No | No | Diagnostic projections cover selected fields; there is no owner export/hydration contract covering all observations/provenance/freshness, terminal input idempotency/sequence, commitment progress/costs and reciprocal references. P11's current canonical SimulationRuntime composes ActorChoiceStore by default even though the selected Unity bootstrap has no external WorldCommand service/queue; preserve complete records/dispositions, duplicate command IDs and next sequence, and reject in-flight inputs. The queue remains excluded. |
| P9-A/P9-B genesis identity, P9-B authored inputs/outputs and selected P8-A Hex/Location/scale provenance | No | No | The selected manifest is P9-B `unity-authored-bootstrap/authored-geography-v1`, stage `p9.genesis.authored-geography/v1`, with its P9-A stage lineage. `Simulation-GeneralTest.asset` authors exactly one Hex (`hex/sample-origin`, axial `(0,0)` under `axial-hex-v1`), terrain `terrain/sample-plains` revision `sample-world-v1`, exactly one anchored Location (`location/sample-origin` → that Hex), and one scale context (`world-scale/Simulation-GeneralTest/v1`, source `profile/Simulation-GeneralTest` v1, `1 km` per neighbor step). P9 canonical closure is now `82396ae`; P9-B implementation remains `d9a62d7` (`00395ef` implementation). The live genesis manifest/pipeline is not a P12 export or staged hydrator. Capture and restore the exact P8-A facts and P9 provenance through their owners; do not rerun genesis. |
| Official daily-domain owner state selected by effective configuration (economy, demography, mortality, trade/Knowledge sharing, crime/social appraisal, and related provider context) | No | No | The profile has no inventory-backed exact export and hydrate implementation for every actually composed provider/owner, including mutations, revisions and reconstructible projections. This includes `EconomyTransactionService.keyedSaleReceipts`: the selected legacy profile must census it as exactly empty and reject populated or unverified state until a supported keyed-sale consumer is included. The RNG provider/key census for the validated candidate is recorded above, but its compatibility/provenance is not exported or restored. A provider list or runtime clone does not close these gaps. |
| Core political, institutional, property, faction/support/recognition, force/manpower/position, conflict/war/battle authorities present in the selected runtime composition | No | No | Owner stores and some transaction snapshots/diagnostic records exist, but no complete profile export/hydration graph restores populated truth, relations, terminal outcomes, revisions and allocator state in owner dependency order. Empty-at-start does not establish handling of later populated state. |
| Mutation health and derived runtime indexes/projections | No | No | No capture lifecycle protocol proves a quiescent, non-faulted boundary across all supported writes. No staged restore validates every owner before binding a fresh guard, rebuilding only owner-derived indexes and publishing exactly once. |

`WorldStateSnapshot` and canonical diagnostic/digest output are comparison and
diagnostic projections, not reconstructible truth or a save DTO: their field
coverage is selective and they do not define owner-authorized restoration.
Likewise, Unity `[SerializeField]` and `[Serializable]` support in scene/assets
is incomplete for runtime continuation: it neither guarantees inclusion of
all causal state nor supplies compatibility validation, owner hydration,
cross-reference/cardinality checks, or atomic publication.

## Explicit profile boundary

P8-B through P8-E are explicit empty sections for this profile; populated state
must reject admission. P10-A is promoted (`9501bf0`, with current P10
canonical State/Brief tip `252ad6b`), but its Ruin/LocalTopology output is not
composed by the selected GeneralTest bootstrap and remains excluded. P14-A is
promoted (`c44904b`,
historical promotion State `f8a61fe`; current P14 State/Brief `4caecbb` is
docs-only) but the selected GeneralTest City assets do not configure its
exogenous local material source; their existing `productionConfigs` are the
legacy economy producer and do not make P14-A material-flow state present.
P18 intraday state, P19 module state, P20 shared activities, P13 historical
reconstruction/fork guarantees, and generated P9/P10 content remain outside
P12-A. P18-A/B/C, the additive P18-A continuation extension, the sale-owner
receipt/prepared-install, and the per-runtime serialized advance lease are
promoted at P18 canonical `9e790c5`. Current P18 State evidence tip
`f1cfed3` is not promoted. The current P18-D consumer runtime implementation
remains `3ddf847`; test-only commits `0887d18` and `6a4d971` contain the
consumer replay regression and its wording correction, with fresh validation
without changing runtime implementation. The earlier P18-D integration snapshot is
`5d7eb268` (code integration `3d9c0ea`), including the CommercialKnowledge
sharing receipt; it remains unpromoted and does not wire the full chronological
consumer into `SimulationRuntime`. Current P18-D consumer candidate `3ddf847`
does wire that path in its optional intraday profile, but remains unpromoted;
its candidate-only receipt, owner, and runtime changes do not update this
inventory's canonical P12 profile. The selected bootstrap does not compose the
P18 timeline, activity/availability runtime, continuation extension, or
intraday state; no P18 state is claimed. P20-A is promoted at
current P20 canonical/State `7a81cc0`,
but shared activities are not composed by this profile and no P20 state is
claimed.

This inventory records implementation evidence and gaps only. It is not P12-A
scope acceptance (recorded separately), implementation authorization, proof of
save/load, or a claim of profile closure. Keep P12-A at `WAIT_DEPENDENCY` until
every included owner has exact immutable export and staged hydration coverage,
and the live canonical composition/profile inventory is demonstrated. Separate
implementation authorization remains an independent gate.

**Source basis:** `SimulationRuntime.cs`, `SimulationModuleSet.cs`,
`TesteSimulacao.cs`, `RuntimeIdentity.cs`, owner/store implementations under
`Assets/_Project/Scripts`, and `Diagnostics/WorldStateSnapshot.cs`,
`WorldStateCanonicalWriter.cs`, and related snapshot projections. Existing
clone and transactional restore helpers were classified by their actual
runtime/rollback contract; none was treated as a P12 serializer or staged
hydrator.

Mutation census references include `SimulationRuntime.cs` and
`SimulationTime.cs`; `CityRuntime.cs`/`NpcRuntime.cs` and
`RuntimeIdentity.cs`; `Population/SettlementPopulationRuntime.cs`,
`Population/SettlementPopulationMembershipSystem.cs`,
`Population/NpcResidenceMigrationSystem.cs`, and
`Population/NpcPopulationLifecycleSystem.cs`; spatial owner/store files;
`ActorChoiceStore.cs`; directive/travel/expedition owners; and the political,
property/estate, armed-force, conflict, war, battle, economy, crime, and justice
owner/store implementations. The exact references and classifications are for
candidate snapshot `af656e7710fce0ba171fae1d6684331d2dc0b743` and must be
refreshed against the promoted canonical composition.

P9's current extensibility constraints still apply to this inventory: preserve
stable stage/contributor identity and compatible version, declared inputs and
outputs, dependencies, deterministic contribution order and causal provenance.
Generated outputs are historical owner state; installation cannot regenerate
them. P19 loader/module-state support and explicit retrofit remain deferred and
are not required for this official profile. P20 remains conditional: if a later
profile includes shared activities, retain a stable `ActivityInstanceId`
independent of `PersonId`, activity definition, and participant identity. The
architecture's one-or-more cardinality applies; the promoted P20-A exactly-two
Person fixture is not a universal rule.
