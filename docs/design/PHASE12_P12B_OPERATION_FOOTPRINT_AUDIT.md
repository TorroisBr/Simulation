# P12-B selected-profile operation footprint audit

**Status:** Read-only source revalidation at P12 canonical `81ddfe4`. This
record is evidence for the accepted P12-B owner/operation inventory. It does
not wire the continuation protocol, establish runtime quiescence, or make
P12-B/P12-A ready.

## Protocol wiring and boundary result

Repository search under `Assets/_Project/Scripts` finds no production calls to
`ContinuationCensusProtocol.TryEnterOperation`, `SimulationOperationScope`,
`BindOwnerThread`, or `NotifyCommittedMutation(s)`. They appear only as
protocol definitions; tests exercise the isolated kernel. The operation
tracker is not currently connected to bootstrap, owner systems, or the daily
loop.

The operation map below records source-level outer paths, not a claim that the
profile has registered their exact owner-section set. Existing providers are
named where available. Missing provider/owner evidence remains a blocker.

| Operation family | Outer path and affected state | Existing section/revision evidence | Bypass, failure, and commit notes |
|---|---|---|---|
| Selected daily advance | `SimulationRuntime.TryAdvanceDay` / `TryAdvanceDays` advance the clock, call the daily boundary, then run Justice, Crime, configured economy/merchant behavior and actor processing sequentially. This is not one cross-domain transaction and has no full-day rollback. | The P18 lease remains reentrancy protection only. There is no P12-wide day or operation witness. P18 timeline/prepared-step methods and P14 local material flow are not this selected profile's daily path. | Public `SimulationTime.TryAdvanceDay` bypasses the outer runtime lease. The thread is not bound by P12. Any future runtime scope must cover the complete outer daily operation; it must not be inferred from `advanceLeaseHeld`. |
| City/NPC roster and projections | `TryRegisterNpc` / `TryUnregisterNpc`, `CityRuntime.AddImportantNpc` / `RemoveImportantNpc`, `NpcRuntime.SetCurrentPresence`, and travel start/advance/cancel paths change roster or paired City/NPC projections and travel fields. | `SimulationRuntime` roster has no count/revision provider. `Cities` and each City’s `ImportantNpcs` expose backing lists. NPC/City have no composite revision; account/inventory children only have local revisions. | Public leaf mutators bypass any wrapper-only scope. `TryUnregisterNpc` does not remove City projection itself; projection must be separately accounted. NPC current status, action, travel/merchant plan objects, Knowledge, inventory and account are live child owners. |
| Person/population lifecycle | Named birth spans Person membership, optional Genealogy edges, and a settlement aggregate. Materialization/adoption spans Person binding, NPC identity/runtime roster, and optional City presence. Death and resident lifecycle span Person/NPC and settlement population; residence migration pairs origin/destination population before Person/NPC residence assignment. | Existing sections include `p12d.person.membership`, `p12d.person.materialization-binding`, `p12d.genealogy.parentage`, and dynamic `p12d.city-population.aggregate/{cityRuntimeId}` / `p12d.city-population.operation-receipts/{cityRuntimeId}`. They are separate partial witnesses, not a complete lifecycle stamp. Person, NPC, City projections have no composite revision. | Public static system entrypoints can bypass `SimulationRuntime` wrappers. Birth and materialization have compensation branches; some cleanup results are ignored, and some wrapper paths update political revision inconsistently. Death/migration apply after aggregate changes without full rollback. The successful outer operation must name every affected section together. |
| Economy and merchant | Daily City production, consumption and price refresh mutate markets. Merchant `TryExecuteAction` can commit account, inventory, market, travel, plan and Commercial Knowledge changes. `EconomyTransactionService` purchase/sale/trade/consumption/travel-charge methods are individual transactions, not one economy-day transaction. | The composed P18 keyed-sale receipt section exists but its consumer is explicitly outside the selected profile and must remain exact-zero. Ordinary account/inventory/market, Merchant and Commercial Knowledge sections lack complete P12 providers despite some owner-local revisions. | Transaction methods compensate earlier legs on some failures; local revisions can still advance through successful compensations. Direct MoneyAccount/Inventory/Market and CommercialKnowledge mutators bypass the transaction/service boundary. P14-A local material flow is not configured in this selected profile. |
| Justice, Crime, and appraisal | `BeginSimulationDay` sequences Justice day start, Crime hidden-status advance, sentence advance and wanted sync. Crime actions can transfer money, update theft outcome/Knowledge/appraisal, create warrants, change hidden/status state, and then record events. Arrest, sentence, escape and sync paths mutate several owners. | No P12 census sections cover ordinary Justice, Crime, warrants, theft outcomes, CrimeKnowledge, social appraisal, or ordinary Commercial Knowledge. P18 step receipts/revisions are not the selected P12 daily owner witnesses. | Daily processing is sequential, not rolled back as one operation. Crime paths compensate some money or appraisal legs but do not define one cross-owner transaction for the complete action. The authored bootstrap NPCs have no `PersonId`, so the Person-backed theft sink branch is not exercised by them. |
| Persistent conflict and terminal battle | Persistent Conflict/War/Battle stores have direct owner registration/participant/start/end commits. `ConflictResolutionService.Compute` is proposal-only; `TryResolveAndApply` applies consequences sequentially and records an event afterward. `BattleOutcomeApplicationService.Apply` prepares and commits battle terminal state, manpower/source population and armed-force mirrors as one bounded multi-owner operation. | Existing sections include `p12e.conflicts`, `p12e.wars`, `p12e.battles`, `p12e.contingent-manpower.states`, armed-force sections, per-City population, and the separate `p12c.simulation-record-sequence`. | Battle failure restores its captured snapshots/revisions and faults the guard if restore is unproven. Conflict apply has no equivalent whole-operation rollback. Event append is post-commit. These paths are explicit APIs, not automatic per-day actor-loop transactions. |
| Knowledge, directives, choices, travel and expeditions | SpatialKnowledge discovery is called from bootstrap, individual/party travel, merchant observation and daily runtime refresh. Directives are added, selected and later terminally marked. ActorChoice dispatch records a decision/action, marks dispatch started, executes the domain effect, then records returned/threw. Travel parties and expeditions span NPC travel, economy, knowledge, party/expedition, and event state. | ActorChoice providers exist as `p12f.actor-choice-inputs` and `p12f.actor-choice-temporal-inputs`; both share one store identity and revision, so a mutation invalidates both if both are admitted. SpatialKnowledge, directives, individual travel, party, expedition and per-NPC LocalTopologyKnowledge lack complete census providers/revisions. | SpatialKnowledge’s public lists and NPC child objects have mutation escapes (see blocker record). Directives return live objects and a mutable action definition. Travel/party and expedition stores expose live objects and leaf mutators. Party start has compensation; party advance can partially progress earlier members; expedition operations cross party/economy/knowledge and store owners. No P12 operation scope or notification is called. |

## Revision-visible rollback and invalidation

The current coordinator only accepts a new baseline for changed witnesses
through `NotifyCommittedMutations`; inventory assessment rejects revision
drift when no such refresh occurred. Birth compensation can restore Person or
Genealogy cardinality while their monotone revisions remain advanced; the
population owner may also retain a changed receipt revision. Thus “logical
state restored” does not imply “census revision unchanged.” The earlier rule
that full rollback does not notify is safe only when every registered witness
has its exact baseline identity, cardinality, and revision at outer-operation
exit. Transient writes that are exactly restored to that baseline before exit
need no refresh.

**Recommended technical rule for review:** at outer-operation exit, if any
registered owner witness differs from its baseline—even when compensation
restored the same cardinality but left a newer revision—refresh the full
changed-section set in one
`NotifyCommittedMutations` call. This conservatively advances the eligibility
epoch and invalidates prior capture evidence. Failed preflight/no-op paths
whose complete witnesses remain unchanged do not notify. Do not silently
rebaseline after revision drift. If the accepted P12-B contract instead
requires a rollback to leave the epoch unchanged, it needs an explicit,
independently reviewed way to verify full restoration and refresh monotone
baselines without granting stale capture eligibility; the current kernel has
no such operation.

## Dependency/readiness disposition

This audit advances source evidence only. It does not clear P12-B's complete
owner inventory, operation matrix, owner-thread proof, exact-zero evidence, or
shared invalidation wiring. P12-A remains `WAIT_DEPENDENCY`; P12-C/D/E/F/G
remain governed by their recorded canonical dependencies. The per-NPC
SpatialKnowledge census design is a B inventory need, but its F owner
implementation must respect the Brief's C/D/E-before-F dependency. No
`SimulationRuntime`, `SimulationTime`, daily-loop, or P18-lease edits are
authorized until the B gates are closed.

Source details for the newly discovered registry sibling-revision rule are in
`PHASE12_P12B_REGISTRY_COMMIT_BOUNDARY_DESIGN.md`; newly discovered mutable
owner escapes are listed in `PHASE12_B_BLOCKER_RESOLUTION.md`.
