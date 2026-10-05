# P12-B Selected-Profile Operation Footprint Refresh

**Status:** Read-only source revalidation at P12 canonical
`70bc1e50a7107a1489614a62f5f34694b6b52498`. This refresh corrects the
earlier operation-footprint evidence and separates the outer operations that
were previously grouped together. It is evidence for the accepted P12-B scope;
it adds no checkpoint, implementation authorization, domain behavior, or
capture eligibility.

## Baseline and stale evidence

The earlier `PHASE12_P12B_OPERATION_FOOTPRINT_AUDIT.md` is available on
`codex/phase12/P12BRegistryCommitBoundaryDesign` at `8962a566`, based on
`81ddfe4`. That branch is 72 paths different from current canonical and is not
a current-base candidate. Its statement that production does not call the
operation protocol is stale after the approved runtime-admission promotion.

This review reads the current canonical implementation and the promoted
runtime-admission candidate record. `PHASE12_B_BLOCKER_RESOLUTION.md` remains
the broader owner, writer, and cardinality map. The rows below add the current
outer-operation boundaries and identify where that map still cannot support
runtime mutation wiring.

## Current partial adapter boundary

`SimulationRuntime.InitializeNpcRosterCensusProtocol` currently registers
fixed `PersonStore` membership and materialization-binding sections, then the
dynamic per-NPC SpatialKnowledge, Inventory, and NPC-Knowledge families. It
registers `runtime.npc-membership` for the runtime and, for the selected
`UnityBootstrap-Daily-v1` context, also registers
`runtime.bootstrap-publication` and `runtime.advance-day`. That selected
context is captured at Unity `Start` and checked against both thread identity
and managed-thread ID.

The bootstrap scope starts at the profile-validation stage, after the runtime
and this partial census exist. Earlier genesis stages are outside that scope.
The daily scope covers the selected runtime day or nonzero day batch, including
the runtime-owned clock dispatch. A scope counts admitted work; it does not
lock every owner or imply that all writes in it refresh the census epoch.

The only production path found that reconciles commits into the partial shared
epoch is the NPC membership scope around `TryRegisterNpc`,
`TryUnregisterNpc`, `TryMaterializePerson`, and
`TryBindExistingNpcToPerson`. Its exit checks the PersonStore revision delta
and reconciles the registered dynamic NPC families. The generic
`NotifyCommittedMutations` API has no other production caller. No runtime
operation is registered for Expedition, TravelParty, economy, Justice, Crime,
political/military transitions, or the other direct owner APIs below.

Passive section providers and selected-profile day-zero witnesses now exist
for additional owners, including City presence, ScheduledDirective,
TravelParty, Expedition, ExplorableSite, P8 sections, and persistent
Conflict/War/Battle. Their existence and exact-zero observations do not mean
they are registered in the partial runtime protocol. The promoted adapter
therefore supplies only partial admission and partial membership invalidation.

## Outer-operation matrix

The owner families and leaf writers are enumerated in
`PHASE12_B_BLOCKER_RESOLUTION.md`, section “Exact method families from the
source audit.” This matrix records the enclosing operations and the current
scope boundary. A listed outer operation may contain several independently
committed owner writes; no row implies a whole-operation rollback.

| Outer operation | Possible committed owners and causal effects | Existing boundary, revisions, and gaps |
|---|---|---|
| Selected bootstrap: `TesteSimulacao.Start` → `InitializeSimulation` | Genesis authorities and the runtime composition are built and published. | `runtime.bootstrap-publication` begins at `p9.genesis.validate-profile`, after the runtime and initial partial census are created. Earlier genesis stages are not counted by this scope. Failure latches/revokes selected-profile publication. This is not a census of all bootstrap owners. |
| Runtime NPC membership: `TryRegisterNpc`, `TryUnregisterNpc`, `TryMaterializePerson`, `TryBindExistingNpcToPerson` | Runtime NPC registry/list, PersonStore membership/materialization binding where applicable, and the registered per-NPC census families. Person materialization can also change its selected City projection. | These wrappers enter the local membership scope and reconcile only the currently registered fixed/dynamic sections. `PersonStore` revision deltas are checked. Other `PersonStore` writes, population, genealogy, and direct City/NPC writes do not inherit this scope. |
| Other Person/population lifecycle: `TryRegisterPerson`, named birth, death, immigration/emigration, resident death, residence binding/migration, genealogy | Person membership/life/binding, Genealogy, settlement aggregates, NPC roster or residence/status depending on the operation; some wrappers also advance the partial political-world revision. | These public lifecycle wrappers do not enter `NpcMembershipCensusScope`. They can span several commits and compensation steps. Restored cardinality can retain advanced owner revisions. Direct lifecycle-system/store calls bypass wrapper-level observation. Their provider witnesses are not a complete set in the runtime protocol. |
| Selected daily operation: `TryAdvanceDay` / nonzero `TryAdvanceDays` | Clock; placement/content and demography; Justice/Crime; directives; enabled economy/merchant; Knowledge; expedition autonomy and starts; actor choice/actions; individual and party travel; arrival observations; Expedition reconciliation; event/decision `SimulationRecordSequence`. | One `runtime.advance-day` scope spans the complete selected day or day batch. The day loop is sequential, not cross-domain transactional; completed owner writes are not rolled back as a whole if later work fails. The scope alone does not notify changed sections. For this selected context, public clock advance dispatches through the runtime; direct owner writes and non-selected profiles remain outside that routing contract. |
| City, market, inventory, account, transaction, merchant | Daily production/consumption/price updates; transaction-service operations can change money, inventory, market, travel and receipts; merchant operations can also change plans and Commercial Knowledge. | Owners have selected local revisions, but no composite City/economy revision or registered full section set. Failed transaction paths may compensate previously committed legs; successful compensation may still advance local revisions. Public child-owner methods bypass service/runtime wrappers. Keyed sale receipt is only a composed exact-zero owner for this profile. P14 material flow is not composed. |
| Justice, Crime, and appraisal | Day start, hidden-status advance, sentences, wanted-status synchronization, arrest/escape, Crime actions, theft outcomes, Crime Knowledge, and social appraisal may change different owner stores and NPC fields. | Daily calls occur inside `runtime.advance-day`, but no Justice/Crime operation ID or complete owner registration/notification exists. Action paths can also be called outside the day. Prepared receipts and selective revisions do not describe every committed write or provide whole-action rollback. |
| Political, institution, property, force, conflict, war, and battle | Runtime wrapper transitions and direct store APIs commit one or more political/military owners. Conflict apply records its event after effect commits; battle application spans terminal battle state, manpower/source population, and armed-force mirrors. | Passive count/revision providers now exist for several stores, including Conflict/War/Battle, but the complete set is not registered. Proposal/compute APIs are not commits. Battle application has a bounded rollback/fault path; the wider conflict and political APIs do not share a capture-wide transaction or epoch callback. |
| Directives and ActorChoice | `ScheduledDirectiveStore.Add` and terminal processing; ActorChoice capture, defer, reject, dispatch-start, returned/threw dispositions; event/decision sequence and action effects. | Their owner witnesses exist, and both ActorChoice sections share one store identity/revision. Daily actor processing is within the outer day scope, but direct public store/system calls remain possible and neither family is connected to the shared epoch. Dispatch-start is an intermediate owner commit before the action attempt completes. |
| Individual travel: `TravelSystem.TryStartTravel` / `AdvanceTravels` | Travel start can charge money, mutate NPC travel/presence and its City projection, record SpatialKnowledge and append an event. Arrival advances individual travelers, records Knowledge, and may allocate a record sequence. | Direct costs are compensated if NPC travel cannot start, but local revisions can advance through compensation. `AdvanceTravels` walks multiple NPCs and may commit earlier progress before later entries. Travel owner census evidence is passive and not registered as a protocol section. |
| TravelParty start: `TravelPartySystem.TryStartTravelParty` | Allocates a party ID; starts multiple NPCs and changes their City projections; charges group travel; adds the party; sets party IDs; records an event; and records route/location Knowledge. | The method compensates member travel, costs, party membership, and active-party bindings along failure paths. Compensation itself can advance owner revisions; party/event/Knowledge writes may already have happened at separate owners. It is nested during Expedition start/return and callable directly. No dedicated runtime operation or registered party section covers it. |
| TravelParty advance: `TravelPartySystem.AdvanceParties` | Per-member travel-day changes; on full arrival, member and City projections, Knowledge, event/sequence, active-party bindings, and party completion. | It iterates parties and members. Member travel can advance before the method finds that the group has not all arrived; partial progress is a committed state. A `TravelParty` owner witness exists but is passive and not registered. |
| Expedition start: `ExpeditionSystem.TryStartExpedition` | Allocates an Expedition ID; adds/fences an Expedition; nests TravelParty start; commits Expedition travel state; records the start event. Nested travel can mutate NPC, City presence, economy, party, Knowledge, and record sequence. | ExpeditionStore reserves and revisions its add/state/removal commits. If nested travel or the following transition fails, compensation removes the preparing Expedition; nested successful compensations may have their own monotone revisions. Failed event recording is logged after committed start and does not roll the start back. No Expedition operation ID or registered Expedition section exists. |
| Expedition daily autonomy: `AdventureExpeditionAutonomySystem.AdvanceActiveExpeditions` | Snapshots active Expeditions, begins the daily autonomy pass, and branches by state. Exploration may change Expedition progress/visits/objectives, per-NPC exploration/local-topology Knowledge, place content, inventory/notable-item custody, conflict outcomes, return TravelParty, and event/record sequence. The autonomy system also keeps day reservations and failed-execution keys. | The method runs inside the selected `runtime.advance-day` scope in normal daily flow, but each owner still commits independently and has no shared-epoch batch notification. Branches vary per active Expedition; there is no single fixed changed-section set. Its nested start/explore/retrieve/opposition/return methods remain public or have public system entrypoints outside this day scope. The transient autonomy state is not represented by the Expedition census witness. |
| Direct Expedition exploration/effect APIs: `TryBeginExploration`, `TryContinueExploration`, `TryExploreLocalPlace`, `TryTraverseLocalConnection`, `TryRetrieveTargetResource`, `TryRetrieveNotableItem`, `TryResolvePlaceOpposition` | Expedition owner changes; local or site Knowledge; place-content inventory; NPC Inventory or notable-item custody; Conflict/Battle effects; objective completion and event/record sequence. | `ExpeditionRuntime` mutations route through the attached store and advance its local revision, while several APIs reserve and commit objective completion around another owner operation. This protects local store consistency only. No common outer operation scope or changed-section set is registered. Direct attached-runtime leaf mutators and public owner methods remain accessible. |
| Expedition return: `ExpeditionSystem.TryBeginReturn` | Commits Expedition return state; starts a real return TravelParty with member/cost/Knowledge/event effects; commits the party association to the Expedition; then records a lifecycle event. | The method holds the TravelParty store mutation window. It can commit and compensate Expedition state multiple times; it may remove a newly created party after an association failure. Local revisions record those commits. The method can also be entered from autonomous progression outside a separate Expedition scope. |
| Expedition travel reconciliation: `ExpeditionSystem.ReconcileAfterTravel` | For arrivals, commits `TravelingToSite → AtSite` and attempts an arrival event. For completed returns, `TryFinalizeCompletion` marks completed/removes the active Expedition in one store commit, then records the completion event. | In the normal daily flow this is the final stage under `runtime.advance-day`; there is no nested Expedition operation or section notification. Both overloads are public, so direct calls can execute outside the daily scope. The direct `ExpeditionRuntime.TryComplete` leaf also delegates to store completion without this event path; no production caller was found. A supported future operation would need to span each full loop and distinguish Expedition revision change from successful record-sequence change. |

## Revalidation result and next evidence boundary

The following earlier claims are stale: that the selected profile has no
production operation scopes, no captured Unity owner-thread identity, or a
direct clock bypass of its complete daily operation; and that the current
composition lacks the passive owner witnesses listed above. The following
remain current: the runtime protocol does not register the complete selected
owner set; its daily/bootstrap operation counts do not refresh all committed
owner baselines; direct public owners can bypass outer runtime calls; local
revisions do not form a shared epoch; and full owner-thread/quiescence,
capture-eligibility, export, and staged hydration are not delivered.

This refresh does **not** authorize the next `SimulationRuntime` callback or
operation ID. The complete effective-profile owner/cardinality inventory and
the changed-section mapping for all supported paths remain open. The Expedition
rows show why `ReconcileAfterTravel` alone is not a complete Expedition
operation matrix: start, daily autonomy, exploration/effects, return, travel,
and direct leaf paths have distinct owner commits and bypasses. Before any new
runtime wiring, the owner-section inventory and supported-operation matrix
must be reconciled together and independently reviewed against this exact
canonical tip.

P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`. Nothing here
infers complete census coverage, shared-epoch coverage, capture eligibility,
export/hydration, or a new gameplay behavior.

## Current runtime-registration delta after P12 Crime/Social promotion — 2026-10-05

Remote P12 canonical is `1a073df83050da9bed5f7e3e48a27b9952cf62eb`. The current `SimulationRuntime` global operation registration is membership, bootstrap publication, daily advance, solo travel start, TravelParty advance, NPC trade and money transfer, Market purchase and sale, Merchant daily trade, plus six bounded population/person lifecycle IDs. The newly promoted Crime/Social Appraisal coordinator registers no general runtime operation IDs: its internal operation boundary is only TheftAcceptance and KnowledgeAndAppraisal. The owner sections and successful reviewed changes join the existing protocol witness/epoch as documented in the P12 candidate and State.

The old outer-operation table remains a source-audit record, not a current authorization list. It predates Crime/Social and later lifecycle integration, groups possible mutation families that have different admission and commit semantics, and includes Expedition/P12-F rows. Its explicit warning remains in force: do not add a runtime callback or operation ID from the old matrix alone. The current code operation lists above do not prove all Daily-v1 owners or writers are covered.

The next evidence boundary is a current-canonical `UnityBootstrap-Daily-v1` delta mapping of effective owners, temporal identity/cardinality, supported commit ingress, and existing changed-section/epoch notifications. Mark each already-promoted slice closed only within its exact reviewed boundary; exclude P12-F Expedition. Choose another bounded implementation only if the reconciled source proves an exact supported gap and the accepted P12-B capability authority covers it. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

### Source-audit disposition — individual daily travel commits

`SimulationRuntime.AdvanceDayAfterClockAdvance` invokes `TravelSystem.AdvanceTravels` on the selected daily path. `AdvanceTravels` commits travel progress through `NpcRuntime.ClearTravelStartedToday` and `NpcRuntime.AdvanceTravelDay`; those mutators use the already-bound per-NPC P12 travel-state boundary. The runtime checks exact NPC travel-section identity and affected City-presence sections before each mutation and notifies the shared protocol after each committed leaf. TravelParty advance is separately admitted through its reserved outer-operation context. Therefore absence of a distinct `runtime.travel.advance-individual` ID in the current registration list is not, by itself, an uncovered P12-B writer for these paths. Broader travel APIs remain bounded by their own reviewed contracts; this finding does not claim complete travel or P12-B coverage.
