# P12-A owner coverage inventory — evidence only

**Profile:** `UnityBootstrap-Daily-v1` (the bounded SampleScene
`Simulation-GeneralTest.asset` → `TesteSimulacao.InitializeSimulation` daily
profile). **Reviewed source baseline:** this P12 revalidation candidate starts
at `4a1d36407487e3d342785d4d895993044d610cf4`. Exact upstream tips checked:
architecture `c285466c355103d3637ac165246591b72eb7bda0` and both alignment
records; P8 State `470667d37863384edadb3d93ef64d8004aff46a3`; P9 canonical
closure `82396ae7ffaf407fda278928da456b06dc5394d4` (P9-B code integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`, implementation
`00395ef80cfa2364d34ed2170e0735d3a4b1513d`, promotion State/status record
`14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`); P10 canonical/Brief
`252ad6b9a507f1c001c05a1e19c2546ebd0707a2` (P10-A code
`9501bf076d506fb64d6ee3e6d178574fff36e153`, State record
`9e79b58397dc9a89ddcc562be139b79987cb55b9`); P11 canonical closure
`308e24d0744112e8f2b741521b8b3e4acb51ebbf` (code promotion
`0803670cfa2c39163b54ff46a21daa06df5a16f6`);
P14 historical promotion State `f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`
(P14-A code `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`); current P14
canonical State/Brief `4caecbbfb0464c965811402b3c11d8717605114a` is a docs-only
update; P18 canonical/State
`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7` (P18-A/B/C, additive P18-A
continuation extension, and P18-D sale receipt/prepared-install plus
serialized advance-lease prerequisites promoted; P18-D's reviewed consumer
design is implementation-ready); P20 canonical/State
`7a81cc0ecbc511dd36c248ec62c7b20f7e477f53` (P20-A promoted, code
`22df7b307528e705e6e84d1d8d54852a17cfc848`); and the current architecture
alignments.

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
The P12 candidate must include the promoted P11 runtime (or prove equivalent
composition) before claiming this coverage. P18-A/B/C and the additive P18-A
continuation extension are promoted at current P18 canonical `9e790c5`; the
extension was accepted under contract `2175bf2` (acceptance record `9de70ae`)
and implemented at integration `1dd0479`. The selected legacy
`TesteSimulacao` profile still does not compose the P18 timeline, activity
lifecycle, availability-decision services, or continuation extension. Their
promoted code does not make that state part of this profile.

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

Some owners bind to `AuthoritativeMutationGuard`, but this does not establish
that every direct owner mutator routes through that guard or through one
serializer-facing API. `SimulationRuntime.TryAdvanceDay` and
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
invalidation. It needs a complete owner-operation census and mutation
notification across those direct paths. Shared code
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
not P12 export, hydration, owner census, or readiness.
The original 2026-09-27 audit did not fetch remote refs; the 2026-09-28
refresh fetched them and confirmed `af656e7` remains unpromoted. Keep P12-A
at `WAIT_DEPENDENCY`.

| Included authority group | Exact immutable export | Staged hydration | Concrete gap blocking P12-A |
|---|---|---|---|
| Profile/build/content/provider identity; effective configuration, calendar, completed-day boundary and capture eligibility | No | No | No versioned P12 envelope/admission manifest or successful-advance token binds the runtime, configuration, content/providers and exact quiescent boundary. `SimulationBootstrapComposition` and runtime wiring describe construction, not a sealed continuation capture. |
| Semantic/runtime IDs, allocator/high-water marks, record sequence and identity indexes | No | No | IDs and registries are used by live owners and partly observed by diagnostics, but there is no complete immutable owner export or restore API that reinstates exact identities/counters and validates global uniqueness before publication. |
| Selected deterministic random provider and continuation state | No | No | The validated `af656e7` composition has one shared seed-0 `DeterministicRandomSource` and only pure keyed draws at index 0 for NPC decisions, crime steals, and action success; no mutable stream cursor is retained, so the profile's mutable-stream section is explicitly empty. No exact immutable provider/seed compatibility export or staged restore exists. Capture effective seed/source provenance, provider/algorithm/build compatibility, and the saved IDs/clock needed to reconstruct keys; reject unknown/injected providers or cursors. Conflict/battle/demo stream sources are excluded, and disabled demographic hash providers require re-audit if selected defaults change. |
| Bootstrap roots: Cities/markets/accounts, NPC roster and mutable NPC/action/inventory/plan state | No | No | Unity assets and `[SerializeField]` fields describe authored inputs and selected object fields; they do not capture all evolved owner state, revisions, causal ordering and references. Owner-specific value DTOs, exact admission checks and validated hydration constructors are missing. |
| Population aggregates, Persons, residence/lifecycle/materialization and genealogy | No | No | In-memory stores, registrations, clones and transaction snapshots support runtime operations/rollback, not a complete immutable representation and staged restore of aggregate/Person/NPC links, dates, relations, allocator state and owner revisions. |
| Knowledge, directives, P11 actor-choice dispositions, travel parties, expeditions and other active commitments | No | No | Diagnostic projections cover selected fields; there is no owner export/hydration contract covering all observations/provenance/freshness, terminal input idempotency/sequence, commitment progress/costs and reciprocal references. P11's current canonical SimulationRuntime composes ActorChoiceStore by default even though the selected Unity bootstrap has no external WorldCommand service/queue; preserve complete records/dispositions, duplicate command IDs and next sequence, and reject in-flight inputs. The queue remains excluded. |
| P9-A/P9-B genesis identity, P9-B authored inputs/outputs and selected P8-A Hex/Location/scale provenance | No | No | The selected manifest is P9-B `unity-authored-bootstrap/authored-geography-v1`, stage `p9.genesis.authored-geography/v1`, with its P9-A stage lineage. `Simulation-GeneralTest.asset` authors exactly one Hex (`hex/sample-origin`, axial `(0,0)` under `axial-hex-v1`), terrain `terrain/sample-plains` revision `sample-world-v1`, exactly one anchored Location (`location/sample-origin` → that Hex), and one scale context (`world-scale/Simulation-GeneralTest/v1`, source `profile/Simulation-GeneralTest` v1, `1 km` per neighbor step). P9 canonical closure is now `82396ae`; P9-B implementation remains `d9a62d7` (`00395ef` implementation). The live genesis manifest/pipeline is not a P12 export or staged hydrator. Capture and restore the exact P8-A facts and P9 provenance through their owners; do not rerun genesis. |
| Official daily-domain owner state selected by effective configuration (economy, demography, mortality, trade/Knowledge sharing, crime/social appraisal, and related provider context) | No | No | The profile has no inventory-backed exact export and hydrate implementation for every actually composed provider/owner, including mutations, revisions and reconstructible projections. The RNG provider/key census for the validated candidate is recorded above, but its compatibility/provenance is not exported or restored. A provider list or runtime clone does not close these gaps. |
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
P12-A. P18-A/B/C and the additive P18-A continuation extension are promoted
at current P18 canonical `9e790c5`. P18-D's reviewed technical design
`9ed6d90` is implementation-ready after promotion of its sale receipt and
serialized advance-lease prerequisites. Candidate `343bb9f` is retained on
its P18-D feature branch and is under implementation/review; it adds several
owner seams but does not yet compose the timeline into `SimulationRuntime`
or satisfy the daily-owner inventory. It is not promoted and does not
establish P18-D delivery. The selected bootstrap does
not compose the P18 timeline, activity/availability runtime, continuation
extension, or intraday state; no P18 state is claimed. P20-A is promoted at
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

P9's current extensibility constraints still apply to this inventory: preserve
stable stage/contributor identity and compatible version, declared inputs and
outputs, dependencies, deterministic contribution order and causal provenance.
Generated outputs are historical owner state; installation cannot regenerate
them. P19 loader/module-state support and explicit retrofit remain deferred and
are not required for this official profile. P20 remains conditional: if a later
profile includes shared activities, retain a stable `ActivityInstanceId`
independent of `PersonId`, activity definition, and participant identity. The
architecture's one-or-more cardinality applies; the proposed P20-A exactly-two
Person fixture is not a universal rule.
