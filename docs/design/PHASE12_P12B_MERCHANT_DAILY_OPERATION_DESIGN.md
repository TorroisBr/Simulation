# P12-B selected daily Merchant operation — technical design

**Status:** Documentation-only design candidate refreshed onto P12 canonical
a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab. Pending independent exact-tip
design review. It adds no checkpoint ID, product behavior, architecture rule,
implementation readiness, State claim, or promotion.

## Authority and source baseline

- Architecture: promoted WorldId/factual-projection baseline
  codex/architecture/world-identity-projection at
  451340c56e9b676bf6ea43412bcb856b9ccde3de.
- P12 canonical base/source tree:
  a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab /
  609e775654a24f3e80a350629fd6c645840d270e.
- Promoted P12 direct NPC-owner invalidation ancestor: integration
  9d1474b4299d8e888dd387e02e9018d9e8627f84, code
  c49f957e45c3059231e9ec66e4010a7c3a389988, tree
  627af2fbd7f93e0025106ee5ba87e72bae6c4ed2.
- Promoted NPC-to-NPC transfer candidate: code tip 2f2b731866aea86eb52ef2b51eb687c88bf91bc4, tree 862bceb6164bf5ddeed49ca8007d9be3ed4670d6; exact-tip review 09f9f49ef85faf5c24eae57acba4426ca0bc39f8. Promotion State is at 73abeb64ca9a2001d38d07b78b6db6728a7ba9ba. Its named runtime.economy.money-transfer operation is distinct from this Merchant operation.
- Roadmap, Execution Model, P12 Brief/State and operation/epoch matrix are
  read as they stand at this canonical base; they are not edited here. The
  operation/epoch matrix was refreshed at a66c215 after transfer promotion;
  superseded historical rows are not current status.
- Current operation/epoch matrix:
  docs/design/PHASE12_B_BLOCKER_RESOLUTION.md. Accepted P12-B scope and
  implementation prerequisite authority are in docs/phases/PHASE12_BRIEF.md
  and docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md.
- PHASE12_STATE.md at the base records P12-B incomplete, P12-A
  WAIT_DEPENDENCY, P13 blocked, and Phase 12 open. This design changes none of
  those statuses.

The Brief authorizes prerequisite capability work within accepted P12-B
through P12-G scopes. This design only narrows a technical boundary;
independent review and the normal implementation candidate/review process
remain required. NPC money transfer is now canonical; its SimulationRuntime
changes are part of this base. Merchant daily integration remains a separate
serialized hotspot change and must preserve the promoted transfer behavior.

## Contract and temporal identity

Cover only the normal selected-profile daily call at
SimulationRuntime.cs:3875-3880 when the effective profile enables
configuration.MerchantTrade.Enabled, for the exact NpcRuntime at the current
roster ordinal. The caller is
MerchantSystem.AdvanceNpcTradeState(npcRuntime), MerchantSystem.cs:45-122.
When enabled, the daily pass attempts at most one call per roster entry in its
deterministic order, while earlier directive/choice branches can skip the call;
method eligibility guards also skip non-merchants, dead,
traveling, or city-less actors. The invocation identity is runtime/profile,
the SimulationTime.AbsoluteDay used by observations, and the exact roster
NpcRuntime instance identified by RuntimeId. This bounds temporal
cardinality for the normal pass, but is not a durable occurrence receipt.

This identifies the supported call boundary; it is not a durable occurrence
receipt or an exactly-once promise. AdvanceNpcTradeState currently has no
idempotency ledger, can be invoked again by another caller, and returns void.
The design does not deduplicate calls or change day/roster semantics.
AbsoluteDay is not proof that the daily boundary completed.

SimulationRuntime already registers runtime.advance-day and operation
families in SimulationRuntime.cs:123-127, 1008-1024, 3665-3672. The
canonical runtime also registers runtime.economy.money-transfer. Add the
stable operation ID runtime.merchant.advance-npc-trade-state to that existing
pre-seal inventory. Enter one nested SimulationOperationScope on the bound
runtime owner thread around each selected per-NPC invocation. This accounts
for that call inside the outer daily operation; it does not make the protocol
a lock or establish quiescence for unregistered operations.

Before the call, bind the exact roster member and every required owner section
to the same current NPC/runtime composition; validate exact owner witnesses
against accepted baselines using existing admission/protocol mechanisms. If a
profile composes merchant trade but required owner providers or operation
registration are absent, reject/fault the admitted boundary before calling
the domain method. Other standalone MerchantSystem entrypoints are not
implicitly enrolled.

## Reachable writes and owner evidence

The following are write effects reached from this invocation. Market,
market-counterparty, Inventory, and other actors are read-only here. Rows
marked missing are prerequisites to later implementation.

| Reachable effect | Current source and temporal/cardinality behavior | Owner/witness requirement |
|---|---|---|
| Discover current Location, then observe current Market items and liquidity | ObserveCurrentMarket calls SpatialKnowledge.DiscoverLocation and ObserveMarket (MerchantSystem.cs:273-323). Market observations key by (LocationRuntimeId, ItemDefinitionId); liquidity keys by location. Observed and received days use current AbsoluteDay. Rows add/replace independently per item, then liquidity can add/replace. | Exact NPC SpatialKnowledgeRuntime and CommercialKnowledgeRuntime instances; per-NPC providers exist. Spatial revision is shared by location and route sections. Commercial revision is shared by market, liquidity, and share-receipt sections. If either shared revision changes, notify all sibling section IDs for that NPC, including unchanged-cardinality sections, so every section baseline advances together. |
| Normalize/clear stale merchant plan | NormalizeTradePlan (MerchantSystem.cs:902-921) can clear a local merchant's stale plan, inactive plan, or a plan whose item is missing from Inventory. NpcRuntime.ClearMerchantTradePlan also clears TravelPlan when its reason is Trade (NpcRuntime.cs:677-686). | Exact embedded MerchantTradePlanRuntime and NpcTravelPlanRuntime objects for this NPC. Neither exposes a local revision/census provider today. Add narrow monotone local revisions and exact-owner witnesses. Each plan owner
is a required singleton section per rostered NPC (cardinality exactly one),
even when its plan data is empty; its revision records successful changes to
the embedded facts. |
| Reconcile changed destination and Trade travel intent | Wrong-target branch may set a travel intention or clear Trade travel then redirect the merchant plan to CurrentCity (MerchantSystem.cs:62-76). SetTradeTravelPlan sets intent only after route feasibility/cost checks (MerchantSystem.cs:924-938); it charges nothing. | Same exact merchant/travel-plan owner instances and witnesses. A successful no-op setter must not advance a revision. |
| Wait-state and target changes | Amount-zero clears the plan; unavailable knowledge increments destination wait; a useful local buyer/profitable market resets wait; redirect changes target and installs Trade travel intent; no redirect increments wait (MerchantSystem.cs:78-122). MerchantTradePlanRuntime mutators are NpcActionRuntime.cs:304-376; NpcTravelPlanRuntime mutators are NpcActionRuntime.cs:145-188. | Same exact embedded owner witnesses. Current RedirectTo, wait, Set, and Clear methods lack revision/saturation handling. Instrument only these owner-local mutations so a real change advances that owner revision and saturation rejects before that mutation. Do not revise unrelated plan APIs. |
| Optional redirect decision record | Redirect calls NpcDecisionRecorder.Record before changing the plan (MerchantSystem.cs:103-115). The recorder allocates a decision ID and record sequence then appends to NpcDecisionStore (DecisionRecords.cs:793-847); failed append can leave allocator/sequence progress, and caller still redirects with null decision ID. | Include exact RuntimeIdAllocator decision-counter and SimulationRecordSequence witnesses when recording is attempted. Passive providers exist (RuntimeIdAllocatorCensusProviders.cs:22-40; SimulationRecordSequenceCensus.cs:4-23), but passive census does not prove registration/invalidation. NpcDecisionStore is a separate append/read-model owner without local revision/provider; current blocker matrix classifies decision/event read models separately from authoritative epoch owners. Do not claim its append advances the world-truth epoch. Reconcile its P12-F continuation treatment separately. |

CommercialKnowledgeRuntime.RecordObservation and
RecordLiquidityObservation advance one shared local revision on successful
add/replace; they return false for stale/duplicate or saturation
(CommercialKnowledge.cs:412-518). The daily method ignores that return value.
SpatialKnowledgeRuntime.DiscoverLocation (SpatialKnowledge.cs:7-54) is
idempotent for known/invalid IDs and increments the revision shared with route
knowledge. Preserve these current semantics.

Merchant plan OriginCity and TargetCity are NonSerialized object references
(NpcActionRuntime.cs:252-301); this operation neither changes that
representation nor defines persistence/hydration. Use existing RuntimeId and
location IDs for identity, not object hashes, roster index alone, or new
gameplay IDs.

### Required owner matrix for later implementation

1. Reuse the existing per-NPC SpatialKnowledge and CommercialKnowledge census
   providers (SpatialKnowledgeCensusProviders.cs,
   NpcKnowledgeCensusProviders.cs:5-75), validating exact owner instances are
   registered. Their shared revisions require paired/all-sibling notifications.
2. Add narrow census/revision seams for embedded merchant and travel plans.
   Bind by exact owner object plus NPC RuntimeId; define empty and populated
   cardinality explicitly.
3. Include existing decision allocator and record-sequence sections in
   preflight/delta accounting when redirect recording is attempted. Their
   values are causal roots; passive witnesses or the recorder's occurrence
   receipt provider do not cover the NpcDecisionStore append.
4. Classify NpcDecisionStore separately as a read model and reconcile its
   reference/lifecycle/export treatment under P12-F before any persistence
   claim. Do not convert it to world-truth epoch authority without a reviewed
   contract.
5. Confirm exact registered profile owners and that all changed revision
   sections remain in one accepted protocol baseline. This is a bounded
   consumer map, not the complete selected-profile owner census.

## Commit, invalidation, and failure semantics

AdvanceNpcTradeState has early returns and sequentially commits knowledge and
plan facts; it is not an atomic multi-owner transaction. There is no rollback
contract. Preserve that behavior.

- Validate exact actor, operation registration, owner thread, and every
  required pre-write baseline before the first supported write. Operation-scope entry failure, wrong-thread, stale/unregistered owner, or missing required section rejects the call and faults the P12 adapter closed before the method runs.
- Run the existing method in semantic order. No actual owner revision change
  means no mutation notification. On normal return with changes, batch all
  changed registered owner section IDs into one NotifyCommittedMutations call,
  advancing the protocol epoch once for this outer per-NPC invocation. For
  shared SpatialKnowledge or CommercialKnowledge revisions, include every
  sibling section whose witness observes the same underlying revision.
- If the method throws after owner changes, those changes remain committed.
  In a finally path, report exact changed section witnesses before disposing
  the operation scope, then rethrow the original exception. Bookkeeping
  failure faults admission closed and does not roll back or rewrite the domain
  outcome. This follows the current P12 post-commit contract.
- NpcDecisionRecorder.Record is not an all-or-nothing transaction: ID/sequence
  allocation may advance even if the store refuses the record. Preserve its
  null result and the existing redirect behavior. Tests must distinguish
  allocator/sequence delta from a decision-store append.
- Local plan revision saturation must reject before changing that plan owner.
  Existing Knowledge saturation rejects its observation without changing its
  local owner. Do not claim whole-invocation rollback when an earlier
  observation already committed.
- ContinuationCensusProtocol.TryEnterOperation
  (ContinuationCensusProtocol.cs:1232-1268) is an active-call counter, not a
  lock. Keep the operation scope active through domain execution and
  post-commit accounting. All protocol failure paths fail closed.

This batching boundary does not subsume runtime.advance-day, add a daily
operation receipt, guarantee exactly-once execution, or prove all other
writers of these owners are covered.

## Focused test matrix for a later implementation candidate

Use selected-profile runtime-admission fixtures and assert domain facts, exact
owner identities/cardinalities/revisions, and protocol epoch. A passing test
proves only this slice.

1. **Boundary/temporal identity:** merchant disabled, null service,
   non-merchant, dead, absent-City, and traveling actor preserve current
   no-write behavior. Verify selected daily invocation uses exact roster
   actor and AbsoluteDay. Wrong-thread, unregistered operation, mismatched
   owner binding, stale owner, and missing section reject before
   ObserveCurrentMarket.
2. **Spatial/commercial knowledge:** first discovery versus duplicate; market
   row add/replace, stale/equal observation, empty Market, malformed row,
   liquidity add/replace/no-op; current City/location/item IDs and current/
   received day; shared revisions update all sibling baselines; saturation
   causes no local write.
3. **Plan normalization:** local merchant clears stale plan; inactive plan;
   Inventory lacks item; amount <= 0; missing observation increments wait;
   local buyer/profitable market resets wait; infeasible target clears only
   Trade travel intent and redirects locally; feasible target sets intent;
   redirect records a decision then changes target/travel intent; no redirect
   increments wait. Assert no-op and real-change revision behavior.
4. **Decision identity:** successful redirect allocates unique DecisionId and
   sequence entry; rejected NpcDecisionStore.Record after allocation leaves
   allocated counters/sequence visible and follows current null-decision
   redirect behavior; no redirect allocates neither. Verify current
   read-model classification.
5. **Post-commit/partial failure:** no changed owners means no epoch; multiple
   changed sections in one invocation advance once; exception after knowledge
   but before plan update reports committed owners without undo; exception
   after redirect recording reports causal roots before rethrow; epoch
   saturation faults closed while retaining prior commits; local owner
   saturation refuses that owner change. Operation active count exits on all
   returns/throws.
6. **Scope guards:** direct TryExecuteAction, separate ObserveCurrentMarket
   calls on arrival/other scheduled paths, and other direct plan mutation
   paths do not become covered implicitly. Keep their writer mappings open.

## Dependencies and integration order

1. Reconfirm P12 canonical and source immediately before review and any
   implementation. NPC transfer is already promoted at code tip
   2f2b731866aea86eb52ef2b51eb687c88bf91bc4 and recorded in State at
   73abeb64ca9a2001d38d07b78b6db6728a7ba9ba; retain its exact review and
   validation evidence. Any later SimulationRuntime integration must start
   from the newest canonical tip and revalidate the combined hotspot.
2. Independently review this exact-tip design against architecture §5 (World
   Truth versus Knowledge), §16 (Knowledge), Economy/Merchant boundaries,
   accepted P12-B contracts, P12 State, and actual source/test behavior.
3. Resolve the exact-owner providers and local revisions for merchant plan,
   travel plan, and allocator/sequence delta mapping. Recheck P12-F's
   decision-record treatment. If review finds genuine semantic/product
   ambiguity, stop only for that issue; current source does not require a new
   product choice.
4. After design review and readiness check, implement with isolated ownership.
   Serialize later SimulationRuntime edits against the promoted transfer
   implementation and
   other runtime-hotspot work. Bind/register the named operation only after
   all required owners and callbacks are available.
5. Validate focused Merchant/Knowledge/owner/protocol suites, then selected
   profile composition and required full EditMode/official Smoke gates for
   the integrated candidate, plus git diff --check. Re-run exact-tip code and
   docs review after any base/tree change.

## Explicit exclusions and readiness limits

- MerchantSystem.TryExecuteAction and SimulationRuntime.TryExecuteAction.
- Every EconomyTransactionService transaction: NPC money transfer, open-market
  purchase/sale, travel charge, or rollback paths.
- Travel execution, route/market/economy mutation, money/inventory commits,
  CityRuntime/MarketRuntime writes, and arbitrary direct child-owner calls.
- Separate ObserveCurrentMarket callers (including arrival/scheduled paths),
  Knowledge sharing, initial scenario Knowledge, commercial scouting, and
  other Merchant actions.
- P18 urgency, intraday steps, keyed occurrences, activity lifecycle,
  external-input behavior, and TryAdvanceNpcTradeStateOccurrence.
- Complete P12 owner census, shared-epoch completeness, runtime-wide
  owner-thread/quiescence, capture eligibility, export/hydration, or P12-A
  integration. P13 history/fork guarantees and unrelated phase scope are not
  created by this design.

P12-B remains incomplete; P12-A remains WAIT_DEPENDENCY; P13 remains blocked;
Phase 12 remains open. Design review validates only this technical boundary
and source map. It does not authorize/start implementation, promote a
candidate, or close a phase.
