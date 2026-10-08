# P12-A owner coverage inventory — evidence only

**Current profile (2026-10-06):** `UnityBootstrap-Daily-v1` uses the bounded
SampleScene `Simulation-DailyV1.asset` → `TesteSimulacao.InitializeSimulation`
daily profile. `Simulation-GeneralTest.asset` remains the separate P10-A
Ruin/LocalTopology proving profile. Historical sections below that cite the
former SampleScene/GeneralTest configuration describe the earlier profile and
are superseded by the current live-profile correction recorded at the end of
this inventory. **Reviewed source baseline:** this P12 revalidation candidate starts
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
update. The P18 economy sale-owner receipt/prepared-install capability and
per-runtime serialized advance lease were first promoted at code tip
`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`; P18-D consumer code `6a4d971` was
promoted at code tip `f1cfed3`, with State child `2d314be` recording the exact-
tip review/validation. Current P18 canonical State tip is `8ac2d78`; the
user-approved closure marker `a49de9d` formally closes P18 within its recorded
scope and records the P18-to-P12-B `SimulationRuntime` hotspot handoff.
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
The bounded consumer is canonical at `f1cfed3`; it does not make P18 state part
of this P12 profile. P18-D exact-tip implementation review covered code
`6a4d971` and State `254385e`; State child `2d314be` records the promotion and
PASS. P18 is closed within scope; current canonical State tip is `8ac2d78` and
formal closure/handoff marker is `a49de9d`. The reviewed consumer rejects P14-A local-material-flow
Cities before mutation pending a temporal owner adapter. Validation reruns are
needed only if review requests fixes.
P20
canonical/State
`7a81cc0ecbc511dd36c248ec62c7b20f7e477f53` (P20-A promoted, code
`22df7b307528e705e6e84d1d8d54852a17cfc848`); and the current architecture
alignments.

The original source revalidation inspected P9/P11 composition candidate
`af656e7710fce0ba171fae1d6684331d2dc0b743` and both alignment records. Its
code-bearing refresh `ec75e6a0912704446fe47f9d727b4656709d05ab` was followed by
P12 canonical composition tip `36e3064e8f60e9c7e8914a23c62d380b708da587`,
independently revalidated against current P18 State tip `8ac2d78`; the only
tree delta from `ec75e6a` is the P18 State document. The historical `af656e7`
census is retained as a historical snapshot. The candidate-specific
source/API audit against `ec75e6a` is still not a live owner/revision census,
committed-mutation notification proof, or complete invalidation guarantee.
The refreshed live profile inventory records authored startup counts and
composition distinctions, but evolved owner revisions and complete
mutation-to-invalidation evidence remain outstanding; this source pass does
not establish P12 readiness.

### Historical P18-source revalidation — 2026-09-29 (before P18-D promotion)

This source pass revalidated the inventory against then-promoted P18 canonical
code `9e790c5` (`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`) and the then-current
pushed, not-yet-canonical P18-D consumer candidate
`3ddf8476842ad24b9234ccaa65a530722ead8eb4`.
The candidate is based on integration merge `a2a8eddc4cd51c17cdc94208c1817fd948432113`,
which includes P14 canonical State/Brief `4caecbb` and P14-A code `c44904b`.
Unlike the earlier isolated owner candidates (`3cf89c0`, `89f5c55`, and
`5d7eb268`), this candidate now composes chronological P18-D advance,
crossed-boundary owner steps, and the successful post-advance P18-C handoff
when an explicit `P18DIntradayProfile` is supplied. It also routes temporal
P11 ActorChoice inputs through the P18-C decision bridge and the keyed
SellGoods consumer. This was candidate-only code at the time of this historical
snapshot; the selected P12 bootstrap remains unchanged and excludes it.

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
The original candidate `af656e7` contains P11 composition. The code-bearing
composition `ec75e6a` passed exact-tip independent implementation review; its
post-P18-closure refresh and P12 canonical tip `36e3064` passed independent
exact-tip revalidation with only `docs/PHASE18_STATE.md` changed. The
composition is now canonical. This inventory refresh records authored startup
cardinalities and actual composed/empty/not-composed owners; the complete
live/evolved-owner census and mutation mapping remain outstanding. This
documentation follow-up is documentation-only; it adds no implementation
readiness. The
selected SampleScene bootstrap creates legacy NPCs without a
`PersonId`, and its `PersonStore` starts empty. P11 choice execution applies to
Person-backed NPCs, so the candidate proves `ActorChoiceStore` composition and
continuation ownership only; it does not prove SampleScene NPCs can execute
SellGoods choices. Any future Person-backed actor support requires a separate
upstream capability and does not expand P9-B scope. P18-A/B/C and the additive
P18-A continuation extension remain promoted; current P18 canonical State is
`8ac2d78`, formal closure marker is `a49de9d`, and code-bearing consumer tip
is `f1cfed3`. The extension was
accepted under contract
`2175bf2` (acceptance record `9de70ae`) and promoted at integration `1dd0479`.
The P18-D-specific timeline, activity lifecycle/availability, temporal
ActorChoice bridge, P18-C request receipts, per-request execution progress,
and keyed-sale receipt state are not composed by the selected legacy
`TesteSimulacao` profile because no intraday profile is passed. The generic
`MerchantSystem` and `CommercialKnowledgeSharingSystem` remain composed for
the enabled daily policies and are included under E/F; do not classify those
owners as absent. The optional intraday SellGoods consumer calls the
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
P12-C RNG census on the validated code composition now preserved by P12
canonical found only pure keyed
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
changes. The code-bearing composition is promoted to P12 canonical at
`36e3064`; the intervening change only refreshed the P18 State pointer.

**Current-base correction (2026-10-08):** P12 canonical advanced to
`66ea303f8a49f0784130c79400f07dc30f54a63d`; its current State marks P12-B
and P12-C complete within their bounded contracts, records the promoted
Genealogy owner slice, and permits continued isolated D/E work. Older
readiness statements and the City D/E allocation in the baseline table below
are historical. For current Daily-v1 owner assignment,
use the reviewed source crosswalk in `PHASE12_D_TECHNICAL_DESIGN.md` and
`PHASE12_E_TECHNICAL_DESIGN.md`. This inventory remains evidence; it does not
claim whole-phase export, hydration, or closure.

**P12-D owner progress (2026-10-08):** the isolated Genealogy owner snapshot
and private staged reconstruction slice is promoted at code
`2b3de7cd13e6f35d35d5dece56eefb64c1fd972a` (Assets tree
`127bc9dc10c0e69312ae98748f04becbe2d8ceda`), with exact-tip review PASS at
`9d101c50b8c6d1c161d741bcd71dd158008820f0`. It preserves the schema-v1
directed edge set and local revision, validates only local graph invariants,
and rebuilds only GenealogyStore-local indexes. PersonStore endpoint checks,
P12-B token/revision-vector capture binding, merged D validation, runtime
composition, and profile-wide hydration remain outstanding. The reviewed D
design audit identifies `SpatialNetworkRuntime` as the next isolated
implementation-ready owner; Person, Site, City/NPC and whole-D integration
retain their separate blockers and serialized ownership boundaries.

| Accepted P12 checkpoint | Exact live owners/sections for this composition | Current boundary and implementation gap |
|---|---|---|
| **B — admission and completed boundary** | `SimulationBootstrapComposition`/`SimulationGenesisManifest`; selected config and effective provider graph; `SimulationTime`, `SimulationCalendar`, `SimulationRuntime` mutation health, expected owner-section inventory, owner-thread/quiescence lifecycle, shared mutation epoch, and successful completed-advance boundary token. | P12-B's bounded admission, invalidation, serialized owner window, and completed-boundary evidence contract is promoted. B governs capture eligibility and transient owner identity/schema/cardinality/revision evidence; it does not export or hydrate the D/E/F domain values. P12-A remains blocked until every included owner has its own exact export and staged hydration plus the validated live profile inventory and separate authorization. |
| **C — identity, genesis, random roots** | `RuntimeIdAllocator`, `RuntimeIdentityRegistry`, `SimulationRecordSequence`; the shared `DeterministicRandomSource`; P9-B manifest, stage lineage, authored inputs/outputs and provenance; the P8-A Hex/anchored Location facts and separate selected `SpatialWorldScaleContext`; the existing `WorldId`. | P12-C's private all-or-none identity/genesis-provenance/deterministic-root composition is promoted. It stages these exact roots without rerunning genesis or publishing a replacement live runtime. P12-D/E consume them; P12-G still owns whole-graph composition/publication, and P13 fork/history guarantees remain outside this checkpoint. Preserve the selected seed/source provenance and provider/algorithm/build compatibility, exact IDs/clock values needed to reconstruct random keys, and explicit emptiness of unsupported streams. P8-A cardinality is one Hex, one anchored Location, and one scale context. |
| **D — factual roots and Person/population relations** | The two `CityRuntime` identities and definition/location roots; population aggregate/revision and committed population-operation receipts/revision; ordered City `ImportantNpcs` membership/revision; ordered market item roots and revision; counterparty/population-economy identities, liquidity/payment modes, optional account identities, balances and revisions; configured NPC D-owned roots; `PersonStore`, `GenealogyStore`, legacy `SpatialNetworkRuntime` locations/routes and `ExplorableSiteStore`. | The 10 configured NPC rows start NPC-only; runtime-created Person and genealogy authorities start empty. Keep `NpcRuntimeId` distinct from `PersonId`. Genealogy now has a promoted local schema-v1 immutable snapshot/staged factory preserving edge identity/order and owner revision; the later merged D stage must still validate Person endpoints. `SpatialNetworkRuntime` is the next reviewed isolated owner slice. City and NPC are concrete owners: D captures each once under the P12-B token and exact owner-component revision vector, then merges disjoint D/F values before one reconstruction. Preserve City `ImportantNpcs` order and cross-check reciprocal NPC current-City links. The P8-C stable City/Site-to-`LocationId` bridge remains separately owned by the runtime-installed `LegacySpatialAnchorBindingStore`. |
| **E — core and selected daily-domain owners** | `MerchantSystem` and commercial-sharing provider behavior/configuration; `JusticeSystem`, `CrimeSystem`, `CrimeSocialAppraisalWorldState`, enabled crime/guard providers, and every instantiated core `InstitutionStore`, `OfficeStore`, property/estate, claim/recognition, faction/support/decision, armed-force/manpower/position, Conflict, War, and Battle authority. Current Daily-v1 has no separate E-owned `CityRuntime` value section; economy providers mutate the D-owned City/market/population/account roots described above. For this exact `TesteSimulacao` profile, `LocalTopologyStore` is not composed (`SimulationRuntime.localTopologyStore` remains null); admission must distinguish not-composed from composed-empty and reject unexpected injection/population. | Each populated E authority needs its own exact owner fields, provider identity, mutation/revision paths, immutable export, staged factory and relation checks. E must not re-export D-owned City roots, F-owned Knowledge observations, or P18 daily continuation receipts. Runtime core authorities constructed empty still require evidence for later supported writes. P10 Ruin/LocalTopology facts are excluded from Daily-v1. |
| **F — Knowledge, directives, P11 choices and commitments** | NPC spatial/commercial/exploration Knowledge; `PoliticalKnowledgeStore` and commercial-sharing observations where composed; `ScheduledDirectiveStore`; P11 `ActorChoiceStore`; `TravelPartyStore`, `ExpeditionStore`, active travel/expedition progress, and the mutable `NpcActionRuntime`/travel/merchant commitment payloads retained by `NpcRuntime`. | `NpcRuntime` is one concrete owner spanning D identity/factual roots and F mutable payloads; E-provider-written outcomes stay in their assigned D/F field. D owns the `CurrentAction` definition reference, while F owns the mutable action-runtime payload and active plans. Capture the owner once under the same P12-B token and exact component vector; merge disjoint fields before one staged hydrator. `ActorChoiceStore` is present by P11's default `SimulationRuntime` constructor path, but no external `WorldCommand` service/queue is composed. Preserve full terminal history, duplicate-ID history and sequence; pending/deferred or consumed-awaiting-terminal entries reject capture. No Knowledge or active commitment has complete export/hydration. |
| **G — staging, graph validation, publication and parity** | The entire B–F section set and exact composition; fresh runtime graph, owner validators, mutation guard, indexes, and one bootstrap/session publication point. | `TesteSimulacao` publishes genesis once but has no private P12 restore stage, complete cross-owner validator, atomic replacement protocol, or continuation-parity suite. Existing diagnostic snapshots, cloning and transaction rollback are not substitutes. |

The following authorities are composed empty or excluded and must remain
explicitly represented/checked according to the accepted profile. The
`SpatialAuthorityStore` has the selected P8-A geography and a composed-empty
P8-B passage child. The separate runtime-installed P8-C
`LegacySpatialAnchorBindingStore` (historically named) owns the canonical
City/Site-to-`LocationId` bridge; P8-C `PersonSpatialPositionStore` owns Person
positions. Both sections, P8-D route Knowledge/plans, and the P8-E component
travel authorities remain empty. Existing legacy `SpatialNetworkRuntime`
routes and legacy travel/party/expedition state are included under D/F and do
not stand in for P8 authorities. The selected `TesteSimulacao` composition
supplies neither a `LocalTopologyStore` nor a prebuilt
`ArmedForceSpatialStateStore`. `SimulationRuntime` nevertheless constructs
its runtime-owned `ContingentManpowerStateStore` and
`ArmedForceSpatialStateStore` on the default composition path, bound to the
installed `ArmedForceStore` and spatial authority
(`SimulationRuntime.cs@ec75e6a:548–563,3370–3378`). The optional
`LocalTopologyStore` reference is null; the position store itself is
composed. At selected-profile day zero, manpower has zero states/revision and
the position store has zero positions/revision. Record both owners as
`COMPOSED_EMPTY`, not `NOT_COMPOSED`. Their startup emptiness does not exclude
later supported populated state; accepted P12-E still requires owner coverage
for it.
The P10 Ruin/LocalTopology output is not composed and populated P10 facts must
not slip into this profile. GeneralTest
has no P14-A exogenous material-source configuration; legacy City production
and ordinary market stock remain included D/E. The selected bootstrap does not
compose P18 timeline/activity/availability/continuation state, P19 module or
loader state, P20 shared activities, P13 reconstruction/fork state,
`PlaceContentStore`, `AdventureExpeditionAutonomySystem`, or an external P11
WorldCommand queue. Excluded populated state must reject admission rather than
be silently omitted.

### Historical exhaustive candidate-source mutation census — `af656e7`

The source-method census below is exhaustive for the candidate code snapshot
`af656e7710fce0ba171fae1d6684331d2dc0b743`. It classifies supported public
mutation surfaces, available guard/revision evidence, live mutable APIs, and
the consequences for continuation invalidation and export. It is not a live
owner census or proof of complete revisions: no P12 owner census/revision
providers, mutation epochs, operation scopes, exact exports, or hydration were
implemented by this audit. Its source pointers are historical and are
superseded for the refreshed composition by the candidate-specific audit below.
Neither table is a complete live owner/revision census or proof of P12-B
readiness.

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

### Historical P12-B entrypoint and mutation-invalidation census — partial (2026-09-29; before refreshed composition)

This historical source pass inspected the selected bootstrap candidate
`af656e7`, P18 canonical `9e790c5`, and then-current P18-D consumer candidate
`3ddf847` / integration merge `a2a8edd` (which also merged P14 canonical
`4caecbb`). This entrypoint snapshot predates the refreshed P9-B/P11
composition `ec75e6a`; the current candidate-specific source/API audit follows
this historical material. Neither audit is a complete live owner census.

| Entry surface | Synchronous path and owner reach | Current evidence and remaining gap |
|---|---|---|
| Selected SampleScene bootstrap | `TesteSimulacao.Start` → private `InitializeSimulation` → P9 genesis stages and composition publication. The scene selects `Simulation-GeneralTest.asset`. | Bootstrap is synchronous on Unity's lifecycle thread, but it records/verifies no owner-thread identity or active bootstrap operation scope. No task/thread/coroutine is used by this selected path. |
| Selected day input | `TesteSimulacao.Update` handles Space → private `Simulate` → repeated `SimulationRuntime.AdvanceDay` calls. | P18 canonical `SimulationRuntime` has a serialized advance lease for its guarded `AdvanceDay`/`AdvanceDays` entrypoints. That lease does not cover direct owner writes, bootstrap, or all command/transaction scopes. The P18-D consumer `3ddf847` extends the lease across chronological timeline/continuation work and P18-C handoff only when an explicit intraday profile is supplied; it is promoted at `f1cfed3`. P18-D has not handed off the hotspot to P12-B. At the time of this historical snapshot the candidate census was awaiting refresh; the source/API refresh is now recorded in the candidate-specific audit below. P18 review/validation does not establish a complete live census. |
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

The promoted P18-D consumer `3ddf847` wires the chronological runtime consumer and
post-success P18-C handoff in its optional profile. It contains the
recipient-owned commercial-sharing receipt/source snapshot and can populate
the canonical economy keyed-sale receipt owner; it also carries the
P18-C/P11 temporal correlation and execution-progress state needed by that
consumer. These are promoted optional-profile capabilities, not canonical P12 profile
composition, owner census, or a profile-wide mutation epoch. P14-A
local-material-flow code is present through `a2a8edd`, but P18-D composition
rejects such Cities before intraday advance until a temporal adapter exists.
Neither the candidate receipts nor existing P12 construction/clone/rollback
helpers provide exact immutable owner export or staged hydration. No included
P12 authority has demonstrated both capabilities.

**Historical remaining-census note:** for every supported synchronous caller, map each
owner to exact committed writes, available before/after revisions or receipts,
and any write that bypasses them. Add explicit owner-thread capture, coverage
for every in-flight operation, and unknown/missing-owner rejection. Complete
this matrix against a promoted combined bootstrap/provider graph and
revalidate against the promoted P18-D consumer and its actual runtime
composition. P18-D code `6a4d971` is promoted at P18 canonical `f1cfed3`, with
State child `2d314be` recording its exact-tip review and validation. That
evidence covers P18-D implementation, not the P12 census. At that historical
source pass, the runtime/day-loop hotspot handoff was unrecorded; the user
subsequently promoted formal closure marker `a49de9d`, now recorded by current
P18 State tip `8ac2d78`. This partial
source pass does not establish P12-B implementation readiness, P12-A
readiness, implementation authorization, or canonical promotion.

### Source/API mutation audit — code tip `ec75e6a` (partial historical audit)

This source/API audit is against exact code tip
`ec75e6a0912704446fe47f9d727b4656709d05ab`, originally assembled from
composition `af656e7` and promoted P18 canonical State child `2d314be` (P18-D
code `6a4d971`, canonical tip `f1cfed3`). That code tip is preserved by P12
canonical composition tip `36e3064`, and its exact-tip implementation review
passed. This documentation follow-up has
not itself been reviewed. File and line references below name
the `ec75e6a` tree specifically and are not stable line anchors.

This is a bounded static source/API map. It does **not** demonstrate a complete
live owner/revision census, enumerate every committed write on every supported
path, prove invalidation after committed writes, establish owner-thread or
quiescence, or provide exact immutable export/staged hydration. No P12
readiness follows from this audit or from P18-D's separate exact-tip review and
validation. The P18 `SimulationRuntime` hotspot handoff to P12-B is effective;
closure marker `a49de9d` is recorded by current P18 State tip `8ac2d78`. This
historical source audit remains partial and does
not establish P12 readiness.

| Area | Candidate-specific source/API evidence | What remains unproved for P12 |
|---|---|---|
| **Profile roots and cardinality** | `Simulation-GeneralTest.asset@ec75e6a:17–20,21–66,92–103` references exactly two City assets, ten NPC rows, and one authored Hex (`hex/sample-origin`, axial `(0,0)`), one anchored Location (`location/sample-origin`), and one `SpatialWorldScaleContext` (`world-scale/Simulation-GeneralTest/v1`, source `profile/Simulation-GeneralTest` v1, 1 km per neighbor step). `TesteSimulacao.cs@ec75e6a:178–195,969–987` composes the runtime and materializes those rows as `NpcRuntime` instances. `SimulationRuntime.cs@ec75e6a:458,487` creates default empty `PersonStore` and `GenealogyStore` when none are supplied; this bootstrap does not register Persons or parentage. | These are profile/source facts, not an admission-time live census. Capture must bind all listed roots and cardinalities to the exact profile and reject any evolved or substituted owner set. The ten sample actors are legacy NPC-only runtimes without `PersonId`; P11 choice execution is Person-backed. This candidate proves `ActorChoiceStore` composition/state ownership only, not that SampleScene NPCs can execute SellGoods choices. P9-B excludes population; do not infer a Person-backed sample actor or expand P9-B. |
| **Runtime, clock, and lease scope** | `SimulationRuntime.cs@ec75e6a:140,147–161,248–252,2533–2604,2624,4166–4218` exposes `SimulationTime` and owner stores, advances the clock at `TryAdvanceDay`/`TryAdvanceDays`, and uses `advanceLeaseHeld` around those advance entrypoints. `SimulationRuntime.P18D.cs@ec75e6a:229–250` also leases intraday advance. `SimulationTime.cs@ec75e6a:49` exposes direct `TryAdvanceDay`. | The lease is a guarded advance/reentrancy window. Its own source comment says it is not a thread lock; the bool lease does not wrap direct clock writes, direct owner APIs, bootstrap, or every command/transaction. Source scans of `SimulationRuntime`, P18D runtime, `SimulationTime`, and `AuthoritativeMutationGuard` found no owner-thread identity check or complete active-operation counter/scope. The P18-to-P12 hotspot handoff is effective at canonical `a49de9d`; P12 owner-thread identity and active-operation proofs remain missing. |
| **City, market, inventory, account, and economy** | `CityRuntime.cs@ec75e6a:31–81,419–564` exposes City subowners and mutable `ImportantNpcs`, and provides production/consumption/price refresh plus NPC add/remove. `MarketRuntime.cs@ec75e6a:160,194,216–236`, `InventoryRuntime.cs@ec75e6a:132,152`, and `MoneyAccountRuntime.cs@ec75e6a:93,106` expose stock, inventory, and balance mutations. `EconomyTransactionService.cs@ec75e6a:269–376,486,512,666–698,1061,1203,1285–1325` includes keyed sales and ordinary transaction/travel-consumption paths. | Market, Inventory, and MoneyAccount have local revisions; these are owner-local witnesses, not a runtime-wide epoch or proof that every path is covered. City has a daily-economy receipt revision, not a revision for all City truth. The audit must map each mutation to its actual commit and owner revision/receipt, including exposed child references and City lists. |
| **NPC and per-NPC mutable state** | `NpcRuntime.cs@ec75e6a:10–40,43–95,153–184,203–632` stores runtime identity, Person binding, action/life/injury, inventory/account, location/travel, plans, and Knowledge; public mutation methods cover action, status, lifecycle, presence, money, travel, and plans. `CurrentStatus` is a public mutable `List` (`:68`), and Inventory/account/Knowledge owners are reachable through live references (`:77–95`). | There is no general `NpcRuntime` revision. Private serialized fields and reachable owners mean a getter-only scalar surface does not imply immutable state. The current source pass does not prove all writes are counted or provide an owner-authorized immutable export. |
| **Spatial, site, and Knowledge authorities** | `SpatialAuthority.cs@ec75e6a:435–449,556,672,728,763` has an authority-local revision and public geography/topology registration/binding; `SpatialPassageAuthority.cs@ec75e6a:500–700` registers connections/barriers and changes passage conditions. `SpatialRouteKnowledgeStore.cs@ec75e6a:74–84` records route observations; `ExplorableSiteKnowledge.cs@ec75e6a:108–193`, `LocalTopologyKnowledge.cs@ec75e6a:268–296,608–682`, and `AdventureSiteIntelKnowledge.cs@ec75e6a:262–277,393` expose observation writes. | These revisions and stores cover different authorities. NPC knowledge objects and legacy `SpatialNetworkRuntime` remain separate roots. The audit does not establish one complete revision chain for their committed mutations, nor prove excluded P8-B..E/P10 topology or P14 material-flow state is zero. |
| **Directives, travel, expeditions, ActorChoice** | `ScheduledDirectives.cs@ec75e6a:151,196,293,335` accepts directives and exposes pending lists/dispatch operations. `TravelSystem.cs@ec75e6a:86–203` starts travel; `SimulationRuntime.cs@ec75e6a:2493–2522` starts parties. `ExpeditionSystem.cs@ec75e6a:78–754,1397–1401` mutates expedition progress and exposes mutable participant lists. `ActorChoiceStore.cs@ec75e6a:66–192,273–426` captures inputs and records deferral, rejection, dispatch, and attempt dispositions; runtime capture/processing paths are in `SimulationRuntime.cs@ec75e6a:2814–3052`. | The daily profile must preserve complete terminal P11 disposition/idempotency/sequence history and reject pending/deferred or consumed-awaiting-terminal state. These stores lack a complete profile-wide invalidation protocol; direct lists and calls need owner-specific coverage. P18 temporal requests, timeline/activity progress, and P18-D execution progress are outside `UnityBootstrap-Daily-v1`. |
| **Other composed authorities** | `SimulationRuntime.cs@ec75e6a:931–1108,1196–2464` exposes NPC/person lifecycle, demography, genealogy, faction, claims/support/knowledge, property/office, and succession operations. Representative direct stores include `PoliticalKnowledgeStore.cs@ec75e6a:92–293`, `PoliticalSupportStore.cs@ec75e6a:32–464`, and `ArmedForceStore.cs@ec75e6a:34,122,399–962`; conflict/war/battle and crime/justice systems also own public state transitions. | Some authorities have local revisions, staged transitions, receipts, or health-guard binding, but coverage is selective. `PoliticalWorldRevision` and diagnostic revisions are partial; direct owner APIs can bypass runtime wrappers. A complete owner-to-committed-write matrix and invalidation witness is still missing. |

**Guard and serialization limits.** `AuthoritativeMutationGuard.cs@ec75e6a:20–49`
contains only the Healthy/Faulted integrity latch and `CanMutate`; binding at
`:53–89` is one-way. It is not a mutation epoch, thread-affinity check,
committed-write notification, or count of active operations. A successful
guarded call therefore does not prove all other supported writes were covered
or that capture began while every owner was quiescent.

**Required exact-zero / reject witnesses.** Admission must inspect the actual
composed runtime, not only the authored startup assets. For every excluded or
conditional owner, an explicit exact-zero or not-composed witness is required;
populated, unknown, or unreadable state must reject. In particular:

- The selected daily profile excludes P18 temporal timeline/activity/request
  and execution-progress state. `NpcRuntime.cs@ec75e6a:36–37,94–95` still
  defines per-NPC P18-D `LocalKnowledgeObservationRuntime` and
  `MerchantTradeStateRuntime` owners, and P18-D providers operate on those
  owners (`SimulationRuntime.P18D.cs@ec75e6a:97–114`). Their mere default
  construction is not an exact-zero admission witness; populated or
  unverified per-NPC LocalKnowledge/TradeState must reject unless that profile
  is explicitly included.
- `EconomyTransactionService.cs@ec75e6a:267–269,380–455` retains a private
  keyed-sale receipt list and writes receipts during keyed execution. The
  selected P12 daily profile excludes the P18-D keyed-sale consumer, so it must
  prove this receipt owner is exactly empty or reject it. The current list has
  no public complete receipt snapshot/admission witness; construction-time
  emptiness alone is not enough.
- `NpcDecisionRecorder` is constructed by the selected bootstrap and owns the
  private `occurrenceReceipts` map in `DecisionRecords.cs`. Its sole writer at
  `ec75e6a` is `P18DMerchantTradeStateExecution.TryResumeP18DTradeStateWorkflow`;
  `UnityBootstrap-Daily-v1` supplies no `P18DIntradayProfile`, so that consumer
  cannot write the map in this profile. Classify it as a composed, conditional
  exact-empty section and require a live zero witness; reject populated or
  unverified state. This receipt map is distinct from the omitted
  `NpcDecisionStore` read-model rows. If a later profile includes P18-D,
  re-inventory and export/hydrate the recorder receipts.
- P8-B..E, P10 LocalTopology, P14 local material flow, P18 temporal/P19
  extension/P20 activity state, P13 reconstruction/fork state, and other
  excluded conditional owners must each return exact-empty or not-composed
  status. The P18-D optional-profile validation at
  `SimulationRuntime.P18D.cs@ec75e6a:116–151` rejects several unsupported
  compositions for P18-D; that is not a P12 daily-profile admission proof.

The refreshed candidate's bootstrap, ActorChoice, spatial, P18D consumer,
runtime orchestration, full EditMode, Smoke, LongRun, Spatial, and
ExplorableSite results are recorded below. Those exact-tip tests establish
composition/regression behavior only. Exact-tip implementation review passed
on `ec75e6a`; P12 canonical composition tip `36e3064` passed independent
exact-tip revalidation after its P18 State pointer refresh. This P12
documentation reconciliation has its own review pending. Composition
promotion is complete, while the live inventory/census gates remain. P12-B is
not READY, P12-A remains
`WAIT_DEPENDENCY`, and no P12 implementation readiness or authorization is
established.

P12-B remains not implementation-ready. The code-bearing P9-B/P11 composition
candidate `ec75e6a0912704446fe47f9d727b4656709d05ab`, based on original
integration `af656e7`, passed bootstrap 14/14, ActorChoice 40/40,
SpatialGeography 13/13, P18DConsumer 9/9, RuntimeOrchestration 12/12, ALL
EditMode 1934/1934, Smoke 5/5, LongRun 7/7, Spatial 100/100, and ExplorableSite
46/46. Its refreshed exact tip `36e3064e8f60e9c7e8914a23c62d380b708da587`
adds only `docs/PHASE18_STATE.md`, passed independent exact-tip revalidation,
and is promoted to P12 canonical; no Unity rerun is needed. The composition
remains unchanged from validated code tip `ec75e6a`; this inventory records
the selected startup profile and provider composition, while complete
evolved-owner/revision and committed-mutation proofs remain missing. The
per-runtime advance lease first promoted at P18 code tip
`9e790c5` remains in the current P18 canonical tree. The bounded P18-D
consumer was promoted at code tip `f1cfed3` (State child `2d314be`); exact
implementation review covered code `6a4d971` and its State/validation record.
That review's replay regression covers replay after normal terminal
reconciliation, not a pre-terminal interruption or runtime restart; keyed
receipts last only for the current `SimulationRuntime` lifetime, with no
save/crash-recovery guarantee. P18 closure and the exclusive runtime-hotspot
handoff to P12-B are recorded by closure marker `a49de9d` in current P18 State
tip `8ac2d78`. The lease does not replace
P12-B's missing owner-thread/active-operation scopes or mutation invalidation.

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
invalidation. The candidate-specific source/API audit records representative
entrypoints, but it is not a complete live owner/revision census. Live census
providers and mutation notification across direct paths are still missing. Shared code
hotspots are `SimulationRuntime.cs` (daily loop, owner composition and runtime
stores), `TesteSimulacao.cs` (bootstrap/provider wiring and publication),
`RuntimeIdentity.cs`/random source (C roots), and the domain store/system files
owned by D–F. The P18 hotspot handoff is effective; P12-B remains unable to
start runtime edits until its own readiness gates pass. D and E can only
parallelize with isolated owner/file boundaries, and F depends on their stable
sections.

**Candidate validation evidence:** the integration implementer reports, on
exact candidate `af656e7`, bootstrap 14/14, ActorChoice 24/24, Spatial 99/99,
ALL EditMode 1742/1742, official Smoke 5/5, and staged/unstaged
`git diff --check` passed. The retained result artifact available for this
inventory is the final Smoke XML (5/5); the other Unity result XML files were
rotated/removed by the harness. These results validate composition behavior,
not P12 export, hydration, owner census, or readiness. This validation belongs
to the original `af656e7` candidate; the refreshed `ec75e6a` exact-tip results
are recorded above. P12-A remains at
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
promoted; P18-D consumer code `3ddf847`, replay regression `0887d18`, and
wording correction `6a4d971` were promoted at P18 code tip `f1cfed3`, with
State child `2d314be` recording exact-tip review and validation. The P18
closure and hotspot handoff are recorded by formal closure marker `a49de9d` in
current P18 State tip `8ac2d78`. The
earlier P18-D integration snapshot `5d7eb268` (code integration `3d9c0ea`) was
incomplete and is retained only as historical evidence. The current P18-D
consumer wires the chronological path in its optional intraday profile, but
this does not update the accepted P12 profile. The
selected bootstrap does not compose the
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

## Current canonical refresh — 2026-09-29

P18 canonical State tip is `8ac2d7885ea1f00d544d88a64bf918a411934f7f`. The
user-approved closure marker `a49de9d` formally closes Phase 18 within its
recorded scope and makes the P18-to-P12-B `SimulationRuntime` hotspot handoff
effective. P18 optional intraday state remains outside
`UnityBootstrap-Daily-v1`; the handoff is ownership sequencing only and does
not establish P12-B readiness or authorize implementation.

The P9-B/P11 composition remains preserved by P12 canonical. P12 canonical
advanced from `36e3064e8f60e9c7e8914a23c62d380b708da587` to
`4d2a9ad5c7f98a7805dede72f9722aec063231e8`, then to
`0b5b4abb0d0a6064500adafe6a3454e41868c102`, through documentation-only
reconciliation and exact-tip revalidation records. The executable `Assets`
tree remains identical to validated code candidate
`ec75e6a0912704446fe47f9d727b4656709d05ab`; no composition or profile input
changed. The current tree retains the reviewed P18 State correction `8ac2d78`
and closure marker `a49de9d`. Independent exact-tip reviews and
`git diff --check` passed; the `ec75e6a` code review and Unity validation remain
applicable, so no Unity rerun is indicated for these documentation-only
updates.

The inventory remains partial. The selected profile's live owner/cardinality
census, complete mapping from each supported committed mutation to its owner
invalidation witness, and proof of owner-thread/quiescent capture are still
missing. Exact-zero witnesses for excluded or conditional authorities and
complete immutable export/staged hydration are also unproved. Keep P12-B
blocked on its accepted admission/eligibility requirements and P12-A at
`WAIT_DEPENDENCY`; the P18 handoff removes only the ownership prerequisite.
Preserve temporal identity and cardinality constraints in any future profile
extension: logical instant/order and persisted occurrence identity remain
distinct, `ActivityInstanceId` is separate from actor/person/participant
identity, and activities have one-or-more participants. P19's loader/module
work and P20 coordination remain deferred/conditional for this daily profile.

The existing P12-C identity/sequence snapshot branch is preserved but is not
current-base integrated: its allocator and `SimulationRecordSequence` snapshot
seams are reusable, while its `DecisionRecords.cs` diff removes promoted P18-D
recorder behavior. The exact revalidation and reintegration constraints are
recorded in [`../PHASE12_P12C_CANDIDATE_REVALIDATION.md`](../PHASE12_P12C_CANDIDATE_REVALIDATION.md).
P12-B remains its implementation/integration prerequisite.

### Post-promotion owner and writer audit — 2026-09-29

This read-only source pass rechecked the existing owner inventory after P12
canonical advanced to `0b5b4ab`. The executable source remains code tip
`ec75e6a`; these findings sharpen the known gaps and do not establish a live
census, implementation readiness, or P12-A readiness.

- **Identity and sequence roots:** `RuntimeIdAllocator` has fourteen private
  type-specific counters with no snapshot/revision/export/hydration API.
  `RuntimeIdentityRegistry` has eight typed dictionaries and public
  registration paths but no complete count/enumeration, revision, or removal
  witness. `SimulationRecordSequence` exposes allocation but not its next
  value or a snapshot/restore seam. These are required causal roots, not
  world-wide invalidation witnesses.
- **Person, genealogy, population:** `PersonStore.TryRegister` is a public
  guarded write with no store revision or committed-write notification;
  `PersonStore.Persons.Count` is observable but not revision-bound. Person
  lifecycle and genealogy writes use runtime/domain boundaries, while no
  composite Person/genealogy export, revision, or staged hydrator is present.
  Settlement population exposes a local revision and transition receipts, but
  those witnesses cover only the individual ledger and its receipt state is
  not a complete profile export.
- **Legacy spatial and site roots:** `SpatialNetworkRuntime` now returns
  detached read-only collection views and exposes exact location/route
  count/revision witnesses. Successful registrations advance its owner-local
  revision, but there is no shared P12 epoch or complete owner export/hydrator;
  direct `RuntimeIdentityRegistry` writes can bypass that owner. The
  `ExplorableSiteStore` provides guarded Add and owner-local `Count`/`Revision`,
  but no immutable export or staged hydrator. Its local revision is not a
  shared P12 epoch. Authored startup counts remain manifest evidence, not a
  live capture witness.
- **Selected daily-domain writes:** the source audit confirms public City
  production, consumption, price-refresh, and important-NPC membership paths
  bypass a composite City revision. Market, Inventory, and MoneyAccount local
  revisions cover only their own writes; they do not collectively invalidate
  the City/NPC snapshot. Merchant trade changes transaction, plan, action, and
  NPC owners; commercial Knowledge has operation-local revisions/receipts.
  Crime and Justice have several prepared-operation revisions/receipts, but
  public daily and action paths can also change NPC, sentence, hidden-status,
  outcome, knowledge, and social-reaction facts without a complete owner or
  world invalidation witness. None of these included owners has a demonstrated
  full immutable P12 export plus staged hydration path.
- **Institutional and military authorities:** the selected bootstrap supplies
  no source stores, so the runtime composes fresh-empty institution, office,
  property/estate, political, force, contingent-manpower-state,
  force-position, conflict, war, and battle owners. These are included owners,
  not excluded authorities. `InstitutionStore` and `OfficeStore` bind the
  health guard but have no local revision; other stores expose selective
  owner-local revisions that do not form a world epoch. The empty
  `SettlementManpowerSourceRegistry` and absent `LocalTopologyStore` are
  distinct `NOT_COMPOSED` cases. Startup construction is not a live exact-zero
  witness, and none of these included authorities has a complete immutable
  P12 export plus staged hydration path.
- **Admission consequence:** runtime owner counts/cardinalities are not
  revision-bound for the listed roots. P12-B must fail closed on missing or
  ambiguous witnesses and on any supported committed write that cannot
  invalidate eligibility. Direct mutable collections and child-owner
  references remain part of the census; authored initial emptiness cannot
  substitute for exact-zero admission evidence.

The audit did not find evidence that clears the recorded P12-B blockers or
changes the P12 checkpoint edges. P12-B remains blocked; P12-A remains
`WAIT_DEPENDENCY`.

### Exact authored startup roots — source census against canonical composition

The following authored startup cardinalities were checked against code tip
`ec75e6a` (preserved by P12 canonical tip `36e3064`). They identify the
supported profile roots at construction; they are not a census of a runtime
after it has advanced or a substitute for admission-time cardinality and
revision witnesses.

- `Simulation-GeneralTest.asset` selects two `CityRuntime` roots:
  `CAMPO_VERDE` (population 1,000) and `SERRA_DE_FERRO` (population 800), for
  aggregate population 1,800. The selected assets author ten NPC rows, five
  per City; these begin NPC-only, with `PersonStore` and `GenealogyStore` empty.
- The two Cities each author a market with five market-item rows (ten rows
  total), five legacy production-configuration rows (three plus two), two
  legacy City locations, and two directed City routes. The profile has two
  initial NPC-inventory rows of four units each.
- Spatial/genesis roots are exactly one P9-B Hex, one P8-A anchored Location,
  and one scale context. There are zero authored ExplorableSites, site
  locations/routes, scheduled directives, warrants, P11 choice inputs,
  TravelParties, and Expeditions at startup.
- `ActorChoiceStore`, `ExplorableSiteStore`, `ExpeditionStore`,
  `TravelPartyStore`, `ScheduledDirectiveStore`, and the selected empty P8
  binding/position/knowledge/plan/passage stores are composed empty. Generic
  `MerchantSystem` and `CommercialKnowledgeSharingSystem` are non-null because
  the Merchant module is enabled. The P18-D intraday profile is not supplied,
  so its timeline/activity/request owners are absent. `PlaceContentStore`, the
  autonomy system, and `LocalTopologyStore` are not composed; treat them as
  `NOT_COMPOSED`, not as empty stores.

Evidence: `Simulation-GeneralTest.asset:17–103`, the selected City asset
configurations, `TesteSimulacao` city/site/route/NPC creation paths, and the
`SimulationRuntime` default-store resolution in `ec75e6a`. These authored
counts bound the initial fixture only. Runtime evolution, direct mutable
references, and uneven owner revisions still require a complete live
owner/cardinality and committed-write census before P12-B can become ready.

### P12-B owner-window and composite-owner source revalidation — 2026-09-29

This bounded read-only audit covers the selected profile's runtime fence and
the shared City/NPC roots in executable code `ec75e6a` (preserved by P12
canonical `36e3064`). It records source gaps, not a complete all-owner mutation
census or implementation readiness.

- **Advance lease and capture window:** `SimulationRuntime.AdvanceDay` /
  `TryAdvanceDay` and `AdvanceDays` / `TryAdvanceDays` acquire
  `advanceLeaseHeld` and release it through `AdvanceLease.Dispose` in
  `SimulationRuntime.cs`. Optional P18 `TryAdvanceIntradayTo` uses the same
  lease, but the selected P12 daily profile does not compose P18 timeline
  state. The lease's unsynchronized `bool` prevents same-runtime reentrant
  advances; it is not a thread lock, owner-thread identity, or global active-
  operation scope.
- **Direct clock and owner entrypoints:** `SimulationRuntime` publicly exposes
  `SimulationTime` and owner stores. `SimulationTime.TryAdvanceDay` can advance
  the clock directly in this legacy profile without the daily path or advance
  lease. Person registration, ActorChoice capture/transitions, runtime person/
  death/materialization/population methods, spatial observation/route-plan
  mutations, and travel-party start are separate public entrypoints that do
  not acquire the advance lease. `TesteSimulacao.TryStartExpedition` directly
  calls the expedition system. No constructor/bootstrap code binds or checks a
  thread identity; no API proves capture is free of all in-flight owner work.
- **No global invalidation witness:** `AuthoritativeMutationGuard` and
  `MutationGuardBinding` expose health/fault and one-way binding, not a
  world-wide mutation epoch or committed-write notifications. Local locks and
  transactions such as `BattleOutcomeApplication`'s per-application lease,
  `PersonDeathLifecycle` receipts, and `SettlementPopulationRuntime` receipt
  gates protect individual operations only. Existing per-owner revisions are
  uneven and cannot prove complete capture invalidation or quiescence.
- **City composite owner:** `CityRuntime` has no whole-City revision. Its
  `ImportantNpcs` property exposes the backing mutable list; City membership
  changes through `AddImportantNpc` / `RemoveImportantNpc` and NPC presence
  changes through `NpcRuntime.SetCurrentPresence` update the projection without
  a City witness. The list is a derived index of NPC presence and must be
  rebuilt and validated after restore, not serialized as a second membership
  truth. Population, Market, account and daily-economy receipt revisions cover
  only their own facts. `TryCommitDailyEconomy` has an expected-state prepared
  installation boundary, while legacy production, consumption, and price
  refresh remain direct methods with only their relevant child witnesses.
  P14 `lastMaterialFlow` is outside this accepted P12 profile and remains
  excluded.
- **NPC composite owner:** `NpcRuntime` has no whole-NPC revision and exposes
  mutable `CurrentStatus`. Public state writers for action, injury/death,
  presence/travel, party markers, hidden state, residence fallback, and
  travel/merchant plans do not produce a composite witness. Inventory,
  account, Market, Knowledge, and optional P18 local-observation/trade-state
  revisions cover only those child facts; `NpcActionRuntime`,
  `NpcTravelPlanRuntime`, and `MerchantTradePlanRuntime` remain mutable through
  public NPC getters without a shared NPC stamp.
- **Accepted owner seam:** P12-D/E City projections and P12-D/E/F NPC
  projections must be derived from one detached snapshot per concrete runtime
  owner, bound to one owner revision and capture token, then merged and hydrated
  once. Preserve child revisions/receipts in their owning section, but do not
  treat their tuple as a composite owner revision. Remove mutable collection
  bypasses and route supported successful nested/direct writes through owner
  invalidation; advance owner stamps only after complete transaction install.
  Rebuild City membership from restored NPC presence and validate the
  relationship rather than persisting duplicate truth. P18 temporal state and
  P14 material flow remain excluded from `UnityBootstrap-Daily-v1`.

The audit closes neither the complete supported mutation census nor P12-B's
owner-thread, quiescence, and eligibility requirements. It sharpens the
accepted B/D/E/F capability work and keeps P12-B `WAIT_DEPENDENCY` until every
included owner and successful mutation path has a validated witness.

### P12-F commitment and Knowledge owner export/revision audit — 2026-09-29

This bounded read-only source audit uses the same executable composition
`ec75e6a` preserved by P12 canonical `36e3064`. It maps current P12-F owner
APIs; it is not a complete all-owner census and does not clear the documented
P12-C/D/E dependencies for P12-F.

- **Knowledge:** `SpatialRouteKnowledgeStore` has checked global/per-actor
  revisions and prepared installs, while `CommercialKnowledgeRuntime` has a
  revision plus share-operation receipts, and `PoliticalKnowledgeStore` has a
  revision/guard. These are not immutable value exports or staged hydrators.
  `SpatialKnowledgeRuntime` has a revision for internal prepared writes, but
  its read-only-typed `KnownLocationRuntimeIds` and `KnownRouteRuntimeIds`
  expose backing lists that callers can cast and mutate without a revision;
  its `long.MaxValue` write path can also succeed without incrementing the
  revision. Other included NPC Knowledge owners—Explorable Site, Local
  Topology, and Adventure Site Intel—have public observation writes without a
  common revision/receipt contract. These are NPC-held Knowledge owners,
  distinct from the absent P10 `LocalTopologyStore`. Read-only collection wrappers may also
  expose live observation objects. Preserve all observation provenance and
  freshness through detached owner values.
- **Scheduled directives:** `ScheduledDirectiveStore.Add` and owner-mediated
  terminal transitions advance a local store revision, and its passive census
  provider reports exact store identity, count, and revision. It has no
  export/restore API. Its `Directives` read-only wrapper and `GetPendingForDay`
  return live directive objects; the local witness is not a staged owner value
  or shared P12 epoch.
- **Actor choices:** `ActorChoiceStore` guards its write/transition methods
  and its read APIs return copies, but it has no owner revision or immutable
  export/staged restore API. Its private next-input sequence, command-ID
  history, temporal indexes and full terminal disposition history must be
  captured together; its internal `Clone` is not an export/hydration contract.
- **Travel parties:** `TravelPartyStore` guards Add/Complete/Remove and its
  travel-system paths advance a local revision; the passive provider reports
  exact active-party cardinality and revision. It still has no receipt,
  immutable export, or staged hydrator. Party progress/cost and reciprocal
  NPC-party state need one consistent staged boundary.
- **Expeditions:** `ExpeditionStore` serializes owner writes, advances a local
  revision for committed store/runtime mutations, and validates exact active
  instance identity/cardinality under its census read window. It has no
  immutable export or staged hydrator, and its collection APIs expose live
  `ExpeditionRuntime` instances rather than detached owner values.
- **NPC commitments:** `NpcRuntime` exposes live `CurrentActionRuntime` and
  `MerchantTradePlan` objects; `NpcActionRuntime` and the plan objects have
  public mutators, while the NPC API can replace or clear action/plan state.
  `NpcTravelPlanRuntime` and `MerchantTradePlanRuntime` each have internal
  `CaptureOwnerState`/`InstallOwnerState` transaction helpers that retain
  object references; they are not immutable durable exports. Mutable
  `CurrentActionRuntime` is a separate action owner without an export/revision
  contract. These action and plan fields belong to the same whole-NPC snapshot
  and revision as the P12-D/E/F NPC projections, not separate competing
  writers.

For P12-F, guarded operations, clones, and read-only collection interfaces do
not substitute for an immutable owner export plus private staged restoration.
Close or deliberately circumscribe the listed mutable escape paths within the
accepted profile, and make every supported successful write advance a witness
that P12-B can observe. Preserve ActorChoice sequence/idempotency, directive
lifecycle, Knowledge provenance/freshness, expedition progress, party
commitments, and NPC action/trade-plan state. P12-F remains downstream of
P12-C, P12-D, and P12-E;
this audit is an owner-inventory advance only, not implementation readiness.

### Cumulative passive owner-witness promotion — canonical `b889b47`

P12 canonical now includes the cumulative census stack promoted at
`b889b4747738d933fe48311ef89fc33a40e3dfa0` (code tree
`65ebc7f7ab6dd834e2326ff426600483a74c162c`). The exact integration review
passed at `35ec988`; the docs-only final tip check passed at `b889b47`; ALL
EditMode passed `1985/1985`, complete official Smoke `5/5`, and diff-check
passed on the code tree.

The promoted passive witnesses now cover record sequence, ActorChoice,
RuntimeIdAllocator, ArmedForce/manpower/position, Conflict/War/Battle,
Estate/Property, and Institution/Office in addition to earlier promoted
receipt, P8, and RuntimeIdentity owners. Individual section ownership,
cardinality, revision, and focused evidence are recorded in the candidate
documents referenced from `PHASE12_STATE.md`.

This does not prove complete effective-profile owner coverage or complete
committed-write invalidation. City/NPC composite state, population/Person/
Genealogy, economy and Justice/Crime, and additional Knowledge/commitment
owners still require bounded evidence and export/hydration work. These passive
witnesses are not shared-epoch wiring, owner-thread/quiescence proof, capture
eligibility, or a restore contract. P12-B remains incomplete and P12-A stays
`WAIT_DEPENDENCY`.

### P12-B passive count/revision witness reconciliation — 2026-10-01

Canonical code now composes fixed owner-local providers for scheduled
directives, TravelParty instances, active Expedition instances, and
ExplorableSite records. This supersedes the 2026-09-29 statements above that
the stores lacked local count/revision witnesses.

`SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
reads the directive provider against its exact installed store: the directive
list is empty, and the witness owner, cardinality, and revision match that
store. It reads the TravelParty and Expedition providers against the installed
owners at exact cardinality 0 and revision 0. Expedition cardinality counts
active `ExpeditionRuntime` instances, not member NPCs. The census provider
validates the expedition store's owner/index identity under its read window.

The profile composes an `ExplorableSiteCensusProvider` bound to its exact
`ExplorableSiteStore`; that owner exposes `Count`/`Revision`, and successful
`Add` advances its local revision. The selected-profile test now reads the
installed owner's exact 0-count/0-revision witness and verifies stable
owner identity, cardinality, and revision on a repeated read. This closes the
day-zero ExplorableSite witness gap only; it does not establish an evolved
boundary census or complete P12-B coverage.

These providers do not create complete P12-B owner registration, shared-epoch
invalidation, capture eligibility, owner-thread/quiescence proof, immutable
export, or staged hydration. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`.

### RuntimeIdAllocator Event allocation invalidation — 2026-10-03

P12 canonical now observes successful selected-profile `AllocateEventId()`
writes through the exact existing `p12c.runtime-id-allocator.events` witness.
The provider remains cardinality one and reports the allocator's local
revision (`nextEventSequence - 1`). The runtime preflights that section and
partial epoch before allocation, then publishes the committed revision through
the existing notification path; nested TravelParty/Merchant operations collect
the Event section into their changed-owner batch. Exhaustion/rejected preflight
does not consume an ID, and post-allocation event failure does not undo it.

This closes only the Event-counter mutation-invalidation gap for the selected
profile. The other thirteen allocator counter families and their supported
writers remain uncovered. The allocator still has no exact immutable
continuation export or staged hydration contract. This does not establish
complete C-root coverage, complete shared-epoch coverage, P12-B readiness, or
capture eligibility.

### Current Daily-v1 delta — canonical `1a073df` (2026-10-05)

The inventory below is cumulative historical evidence; older entries are not a substitute for this current-base reconciliation. P12 canonical now promotes the selected-profile Crime/Justice writer slice, Crime/Social Appraisal's exact singleton TheftOutcome/CrimeKnowledge/SocialReaction owners, and SettlementPopulation/Person/NPC lifecycle hooks in addition to the earlier runtime, travel, and economy boundaries. Their exact limitations and evidence are summarized in `PHASE12_STATE.md` and `PHASE12_B_BLOCKER_RESOLUTION.md`.

Current runtime operation registration in `SimulationRuntime.cs` includes `runtime.npc-membership`, `runtime.bootstrap-publication`, `runtime.advance-day`, `runtime.travel.start`, `runtime.travel-party.advance`, `runtime.economy.npc-trade`, `runtime.economy.money-transfer`, `runtime.economy.market-purchase`, `runtime.economy.market-sale`, and `runtime.merchant.advance-npc-trade-state`. `P12PopulationLifecycleCensus.cs` adds immigration, emigration, resident-death, residence-migration, person-death, and person-residence-bind IDs. The Crime/Social Appraisal coordinator has two bounded local operations, TheftAcceptance and KnowledgeAndAppraisal, and joins its reviewed mutations to the existing protocol epoch. These inventories are explicit implementation facts, not a complete list of all supported production writers.

The existing operation-footprint refresh at `4f12586` predates these promotions, and includes candidate rows for direct travel, political/conflict/battle paths, and P12-F Expedition consumers. Those rows remain audit leads only. In particular, no Expedition operation or P12-F capability is implied by this current P12-B inventory. Do not add another global runtime callback/operation ID until an exact current-base Daily-v1 audit establishes its owner identity/cardinality, effective profile reachability, all supported commit paths, existing epoch notices, and path-specific failure/compensation boundary.

**Baseline result (2026-10-06):** Crime/Social Appraisal was promoted; no next P12-B implementation surface had yet passed that baseline owner/operation/epoch audit. Its P12-B `INCOMPLETE` statement and the earlier City/E assignment are superseded by the current-base correction above and the 2026-10-08 canonical P12 State.

### Daily-v1 profile separation and day-zero census — 2026-10-06

The profile mismatch recorded above is resolved within the accepted scope.
`SampleScene.unity` selects `Simulation-DailyV1.asset`, a dedicated P9-B
authored-geography profile with the prior selected City/NPC inputs and no
P10-A Ruin. `Simulation-GeneralTest.asset` remains intact as the separate
P10-A Ruin/LocalTopology proving profile. Submitting that profile to the
Daily-v1 admission path is rejected before WorldId allocation and publication.
This does not exclude Ruins from future deliberately composed continuation
profiles.

The exact Daily-v1 composition test now sets the `UnityBootstrap-Daily-v1`
admission context and checks the effective day-zero owner/cardinality evidence:

- RuntimeIdentityRegistry has 10 NPCs, 2 Cities, 2 Locations, 2 Routes, and
  zero ExplorableSites, LocalPlaces, LocalConnections, and NotableItems; its
  eight sections bind to one registry owner at revision 16.
- P8 SpatialAuthority has one Hex, one anchored Location, and one scale
  context. Its separate Passage state and crossing witnesses are empty at
  revision 1. Legacy SpatialNetwork has two Locations and two Routes at
  revision 4.
- Each City has its own Market witness with five stock rows and local revision
  zero, and its own SettlementPopulation aggregate witness plus empty
  operation-receipt witness. The two population aggregates are one owner
  section each; the authored population sum is 1,800.
- Each City’s NPC-presence witness is bound to that City and matches the
  reciprocal runtime roster. Person membership and materialization binding
  both bind to the installed PersonStore and are zero at day zero.
- The ten NPCs each have an exact MoneyAccount owner/cardinality-one witness,
  an Inventory witness, two SpatialKnowledge witnesses, and ten typed/local
  Knowledge witnesses checked against the exact installed owners and their
  revisions. The selected profile has two inventory rows total and the
  expected initial authored spatial and merchant-Knowledge facts.
- Fixed day-zero witnesses are also checked for the RuntimeIdAllocator,
  record sequence, ActorChoice, scheduled directives, TravelParty,
  Expedition, ExplorableSite, ArmedForce/manpower/position, Conflict/War/
  Battle, Estate/Property, Institutions/Offices, Genealogy, decision receipts,
  and keyed-sale receipts. Their exact owner, schema, cardinality, and local
  revision are checked; excluded/unused sections are zero.

The exact result hashes and Unity suite counts are in
`docs/validation/P12DailyProfileSeparation/VALIDATION.md`. This closes the
current profile identity and day-zero inventory question only. It is not a
complete effective-profile owner census across evolved states or all writer
paths; it does not establish shared-epoch coverage, quiescence, capture
eligibility, export, hydration, P12-A readiness, or P13 readiness. P12-B
remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

### P12-E military owner registration and current Daily-v1 writer audit — 2026-10-06

Canonical P12 tip cda5a55 promotes the reviewed registration implementation
5b055be (tree c9e763e). The Daily-v1 selected-profile inventory now checks
268 sections, including exactly eight P12-E Required sections: ArmedForce
force/contingent/relevant-Person projections, contingent manpower, armed-force
positions, Conflict, War, and Battle. The three ArmedForce projections bind
to one exact owner and share its local revision. Validation and exact-tip
review evidence are linked from PHASE12_STATE.md.

The new sections are passive admission census only. Current production
post-publication call-site inspection found no normal Daily-v1 writer for these
P12-E owners; bootstrap clone writes occur before admission.
P16-A/P17-A military movement and War mutation facades require their separate
composition profiles. The registration adds no supported Daily-v1 operation
or epoch notice. The prior Daily-v1 day-path crosswalk remains current because
the code delta from 62e12f9 to cda5a55 only adds the P8-D/P12-E census
registration calls.

The remaining P12-B evidence is complete effective owner/cardinality coverage
across supported runtime evolution, the full supported-write/shared-epoch
matrix, runtime-wide owner-thread/quiescence, and a completed-boundary token
tied to a successful advance sequence. The FR-B current-day read is not such a
token. This inventory remains partial; P12-B is INCOMPLETE, P12-A is
WAIT_DEPENDENCY, and P13 is BLOCKED. No export, hydration, capture eligibility,
or Phase closure is implied.

### P12-B P8-A/B/C admission providers — canonical `2d61e19` (2026-10-07)

The selected Daily-v1 census now contains 275 sections after fast-forwarding
`14a4465` and recording the promotion State. The reviewed code is
`c2a21d8`, tree `24f2c317`. This increment registers the seven existing
spatial providers below against the exact cloned/runtime-installed owners:

| Section | Owner | Role / initial cardinality | Current Daily-v1 writer evidence |
|---|---|---|---|
| `p8a.hexes` | `SpatialAuthorityStore` | Required / 1 | Authored by P9 geography before runtime construction and census baseline; no current post-publication Daily writer found. |
| `p8a.locations` | `SpatialAuthorityStore` | Required / 1 | Authored by P9 geography before runtime construction and census baseline; no current post-publication Daily writer found. |
| `p8a.scale-context` | `SpatialAuthorityStore` | Required / 1 | Authored in the same pre-runtime P9 geography stage; no current post-publication Daily writer found. |
| `p8b.passage-option-barrier-state` | installed `SpatialAuthorityStore.PassageAuthority` | ExplicitlyEmpty / 0 | Mutator APIs exist; no current selected Daily production caller found. |
| `p8b.crossings` | `SpatialAuthorityStore` | ExplicitlyEmpty / 0 | `TryRegisterCrossing` has no current selected Daily production caller. |
| `p8c.city-site-location-bindings` | `LegacySpatialAnchorBindingStore` | ExplicitlyEmpty / 0 | P10 A/B are rejected before Daily-v1 identity allocation. Promoted code rejects P14-B before WorldId; P14-A is rejected later by the exact-zero P8-C census before publication. The follow-up candidate moves P14-A rejection to the same pre-identity profile boundary. Standalone P14 proving profiles remain outside selected Daily-v1 admission. |
| `p8c.person-positions` | `PersonSpatialPositionStore` | ExplicitlyEmpty / 0 | P8-E coordinator is composed but not invoked by the selected Daily-v1 production path. |

The exact selected-profile composition asserts section identity, schema, role,
owner identity, cardinality, revision, stable repeated witnesses, and the 275
total. Runtime admission rejects missing/wrong Required cardinalities and
nonzero ExplicitlyEmpty P8-B/C sections. The detailed source and callsite
delta is in [`PHASE12_B_BLOCKER_RESOLUTION.md`](PHASE12_B_BLOCKER_RESOLUTION.md)
under “P12-B P8-A/B/C selected-profile source crosswalk — canonical
`2d61e19`”; exact validation evidence is
[`P12BSpatialProfileCardinalityAdmission/VALIDATION.md`](../validation/P12BSpatialProfileCardinalityAdmission/VALIDATION.md).

This is an initial selected-profile admission inventory plus a P8-A/B/C
current-callsite reconciliation. It does not prove all 275 owners or all
evolved successful-write/epoch paths are exhaustive. No P8-B/C writer,
operation ID, mutation callback, or shared-epoch claim is added. P12-B remains
incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

The P14-A early Daily-v1 rejection correction is candidate code
`65315888a080d2df3fcddd7f977fe8136566a9bb` (tree
`ea356e4e1241b82395cfeb3821705589252d87a4`). It preserves P14-A as an
unscoped proving profile and leaves P8-C's selected-profile exact-zero role
unchanged. The selected Daily-v1 owner/cardinality recheck and all required
validation are recorded in [`P12DailyP14Admission/VALIDATION.md`](../validation/P12DailyP14Admission/VALIDATION.md).


### P12-B selected Daily-v1 temporal roster owner witnesses — promoted code `cb29987` (2026-10-07)

Promoted test-only code extends `SelectedDailyV1NpcMembershipCommitsIdentityAndKeepsItAfterUnregister` to inspect live owner-family providers at the initial 10-NPC roster, after adding one NPC, after unregistering it, and after rejecting a distinct owner with the unregistered ID, then re-registering the original owner. For each live roster it compares the complete provider-family count with the roster, then checks stable section IDs, exact owner identity, cardinality and local revision for per-NPC MoneyAccount, Inventory, two SpatialKnowledge sections and ten Knowledge sections. This preserves the distinction between registry identity retention and the current live owner set.

The exact selected-profile admission and owner/cardinality revalidation, temporal test, ALL EditMode, and Smoke results are recorded in [`P12DailyTemporalCensus/VALIDATION.md`](../validation/P12DailyTemporalCensus/VALIDATION.md). This witness does not prove every one of the 275 sections across evolved states or every successful writer/epoch path. It adds no runtime operation, mutation callback, shared-epoch behavior, quiescence, capture eligibility, export or hydration. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Independent exact-tip review is recorded in [`PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md`](PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md).
