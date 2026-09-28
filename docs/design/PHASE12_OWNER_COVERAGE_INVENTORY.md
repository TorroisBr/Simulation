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
`308e24d0744112e8f2b741521b8b3e4acb51ebbf` (Actor Choice code promotion
`0cd4281804ecc6a2d110352d1a238959e93867f0`);
P14 historical promotion State `f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`
(P14-A code `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`); current P14
canonical State/Brief `4caecbbfb0464c965811402b3c11d8717605114a` is a docs-only
update; P18 canonical code tip
`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7` (P18-A/B/C, additive P18-A
continuation extension, and the P18-D sale-receipt/runtime-lease prerequisites
are promoted; the canonical State header retains stale pre-promotion blocker
wording). P18-D's approved technical design is candidate
`9ed6d90455cc793244ee7207adb62960e45a9972`; the current pushed integration
candidate `6fcbfab` adds the resumable boundary coordinator and receipt-backed
PlaceContent, logger, Justice, Crime, merchant-plan, and City economy adapters,
but still has no chronological `SimulationRuntime`
composition or complete daily profile. P18-D remains in progress; P12-A does
not compose intraday state. P20 canonical/State
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

### P12-B entrypoint and mutation-invalidation census — partial (2026-09-28)

This source pass is against the selected bootstrap candidate `af656e7`, which
remains unpromoted, and current P18 canonical `9e790c5` / P18-D integration
`6fcbfab`. It maps known synchronous surfaces; it is not an exhaustive live
owner census because the promoted combined profile and all supported callers
have not been established.

| Entry surface | Synchronous path and owner reach | Thread/operation evidence and gap |
|---|---|---|
| Selected SampleScene bootstrap | `TesteSimulacao.Start` → private `InitializeSimulation` → synchronous `SimulationGenesisPipeline.ExecuteStages` and composition publication. The scene binds `TesteSimulacao` to `Simulation-GeneralTest.asset`. | Unity lifecycle supplies the expected main-thread entry, but this code does not capture/verify a thread identity or register bootstrap as an active operation. No task/thread/coroutine is used in this bootstrap path. |
| Selected day input | `TesteSimulacao.Update` handles Space → private `Simulate` → repeated `SimulationRuntime.AdvanceDay` calls. Each call synchronously executes the current daily core. | `SimulationRuntime`'s advance lease excludes reentrant advance calls only. P18-D exclusively owns extending the lease/`SimulationRuntime` path through timeline, continuation, subphases and successful P18-C handoff; P12 must wait for explicit handoff. |
| Other bootstrap-facing commands | `TesteSimulacao.TryStartTravelParty` and `TryStartExpedition` synchronously enter the published runtime/system. The `Runtime`/`Bootstrap` properties also expose the runtime and stores/systems to same-process callers. | No common operation-scope registry wraps these calls or direct owner APIs. The selected scene has no external `WorldCommand` queue. `WorldObserverTimeController.AdvanceOneDay` is another callable advance path, but its component is absent from this SampleScene; revisit only if it is bound into a supported profile. |
| Included mutable truth | Public runtime façade methods include roster/person registration and lifecycle, materialization, population transitions, genealogy, legacy route/observation/plan and travel/expedition operations; direct store references expose further mutations. The owner groups are the included City/market/account, NPC/Person/population, legacy spatial/site, selected economy/merchant/Justice/Crime/Knowledge, P11 ActorChoice, and other profile-supported authorities listed above. | A method-to-owner-to-committed-write map is still required. Per-method proposals/queries must be classified by actual commit behavior; guard bindings are not evidence that a write was counted or invalidated. |
| Excluded or conditional authorities | Runtime surfaces also expose P8-B..E, P10 LocalTopology, P14 material-flow, P18 temporal, P19 extension, P20 activity, and other conditional authorities. | These remain outside this profile. The inventory must identify each corresponding store/provider, prove explicit empty/not-composed state at admission, and reject populated or unverified state; do not infer emptiness from the initial scene alone. |

**Revision and receipt evidence already present:** `AuthoritativeMutationGuard`
(`AuthoritativeMutationGuard.cs`) exposes only Healthy/Faulted health and a
one-way binding; it has no mutation epoch, owner-thread check, or active-operation
count. Individual owners provide useful but nonuniform witnesses: Market,
Inventory, MoneyAccount and SettlementPopulation revisions; revisions on some
spatial, knowledge, political, property, force and conflict stores; and
operation-specific receipt/revision pairs. `CityRuntime` currently has a
private daily-economy receipt revision, not a revision for all City truth.
`NpcRuntime` and `PersonStore` expose no general mutation revision. P18-D's
receipt-backed PlaceContent, logger, Justice, Crime, merchant-plan and City
economy steps validate only their own operation snapshots/revisions; they do
not form a profile-wide owner epoch or cover direct mutations in those owners.
`SimulationRuntime.PoliticalWorldRevision` covers only its political subset.
Identity allocators, record sequence counters, and diagnostics revisions are not
a substitute for mutation invalidation.

**Remaining evidence gap:** for every supported synchronous caller, enumerate
its owner(s), exact committed write(s), available pre/post revision or receipt,
and whether a commit can bypass that witness. Add explicit owner-thread capture,
all in-flight operation coverage, and unknown/missing-owner rejection evidence
to the eventual B implementation contract. Complete this matrix against the
promoted combined bootstrap/provider graph and revalidate after P18-D releases
its exclusive runtime/day-loop window. Until then this is partial inventory
evidence only: it does not establish P12-B implementation readiness, P12-A
readiness, implementation authorization, or canonical promotion.
| Included authority group | Exact immutable export | Staged hydration | Concrete gap blocking P12-A |
|---|---|---|---|
| Profile/build/content/provider identity; effective configuration, calendar, completed-day boundary and capture eligibility | No | No | No versioned P12 envelope/admission manifest or successful-advance token binds the runtime, configuration, content/providers and exact quiescent boundary. `SimulationBootstrapComposition` and runtime wiring describe construction, not a sealed continuation capture. |
| Semantic/runtime IDs, allocator/high-water marks, record sequence and identity indexes | No | No | IDs and registries are used by live owners and partly observed by diagnostics, but there is no complete immutable owner export or restore API that reinstates exact identities/counters and validates global uniqueness before publication. |
| Bootstrap roots: Cities/markets/accounts, NPC roster and mutable NPC/action/inventory/plan state | No | No | Unity assets and `[SerializeField]` fields describe authored inputs and selected object fields; they do not capture all evolved owner state, revisions, causal ordering and references. Owner-specific value DTOs, exact admission checks and validated hydration constructors are missing. |
| Population aggregates, Persons, residence/lifecycle/materialization and genealogy | No | No | In-memory stores, registrations, clones and transaction snapshots support runtime operations/rollback, not a complete immutable representation and staged restore of aggregate/Person/NPC links, dates, relations, allocator state and owner revisions. |
| Knowledge, directives, P11 actor-choice dispositions, travel parties, expeditions and other active commitments | No | No | Diagnostic projections cover selected fields; there is no owner export/hydration contract covering all observations/provenance/freshness, terminal input idempotency/sequence, commitment progress/costs and reciprocal references. P11's current canonical SimulationRuntime composes ActorChoiceStore by default even though the selected Unity bootstrap has no external WorldCommand service/queue; preserve complete records/dispositions, duplicate command IDs and next sequence, and reject in-flight inputs. The queue remains excluded. |
| P9-A/P9-B genesis identity, P9-B authored inputs/outputs and selected P8-A Hex/Location/scale provenance | No | No | The selected manifest is P9-B `unity-authored-bootstrap/authored-geography-v1`, stage `p9.genesis.authored-geography/v1`, with its P9-A stage lineage. `Simulation-GeneralTest.asset` authors exactly one Hex (`hex/sample-origin`, axial `(0,0)` under `axial-hex-v1`), terrain `terrain/sample-plains` revision `sample-world-v1`, exactly one anchored Location (`location/sample-origin` → that Hex), and one scale context (`world-scale/Simulation-GeneralTest/v1`, source `profile/Simulation-GeneralTest` v1, `1 km` per neighbor step). P9 canonical closure is now `82396ae`; P9-B implementation remains `d9a62d7` (`00395ef` implementation). The live genesis manifest/pipeline is not a P12 export or staged hydrator. Capture and restore the exact P8-A facts and P9 provenance through their owners; do not rerun genesis. |
| Official daily-domain owner state selected by effective configuration (economy, demography, mortality, trade/Knowledge sharing, crime/social appraisal, and related provider context) | No | No | The profile has no inventory-backed exact export and hydrate implementation for every actually composed provider/owner, including causal random/draw context, mutations, revisions and reconstructible projections. A provider list or runtime clone does not close this gap. |
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
P12-A. P18-A/B/C, the additive P18-A continuation extension, and P18-D
receipt/lease prerequisites are promoted at current P18 canonical code
`9e790c5`. P18-D integration candidate `6fcbfab` adds the resumable boundary
coordinator and receipt-backed PlaceContent, logger, Justice, Crime,
merchant-plan, and City economy adapters; it has not composed the
chronological runtime path or completed the daily profile. The selected
bootstrap does not compose the P18 timeline, activity/availability runtime,
continuation extension, or intraday state; no P18 state is claimed. P20-A is promoted at
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
