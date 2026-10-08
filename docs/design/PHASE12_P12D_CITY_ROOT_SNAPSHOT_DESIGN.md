# P12-D City Factual-Root Snapshot and Staging - Checkpoint Proposal

**Status:** proposal for a bounded P12-D owner slice only. This is not an implementation authorization, an accepted checkpoint review, a delivered capability, P12-A readiness, or Phase closure.

**Canonical base:** <code>codex/phase12/canonical</code> at <code>aa1a40f2e6da53a1a7388601bd13052f1a535545</code> (tree <code>6280e31d7538478bd9fb361005bba13c625af016</code>). The <code>Assets</code> tree is <code>71d917e5bd4090368f5be1536a6cbb2e789bed64</code>.

**Review response:** This recomposition retains the earlier response to review <code>f136d31cc884f1776e3c269f737b8bf61cbc7286</code>, refreshes the base to current P12 canonical <code>aa1a40f2e6da53a1a7388601bd13052f1a535545</code>, follows the current canonical D §4 City-before-NPC relation-assembly sequence, and adds the omitted Market writer paths below. This exact current-base proposal remains pending independent review; earlier reviews do not review this revision.

**Scope:** exact detached export and unpublished exact-value staging for the current Daily-v1 <code>CityRuntime</code> factual root and its <code>MarketRuntime</code>, <code>MarketCounterpartyRuntime</code>, <code>PopulationEconomyRuntime</code>, and <code>SettlementPopulationRuntime</code> components. This is a sub-slice of existing P12-D, not a new checkpoint ID.

## Readiness and review boundary

P12-B and P12-C are promoted within their accepted scopes. The current P12-D technical design defines the City field boundary, but its exact review at <code>b9a0fd1b5941f0b7615a72acec39fd22e6c9ee0e</code> marks only the isolated GenealogyStore slice <code>READY_FOR_IMPLEMENTATION</code> and explicitly grants no implementation authorization beyond that slice. Current canonical §8 says every other owner needs its own current-source evidence and scoped readiness review. The P12 State still classifies City/population and the D/E/F NPC adapter as serialized shared-owner work. This proposal therefore remains **NOT READY FOR IMPLEMENTATION** until an independent exact-tip review accepts this City slice and its owner/staging seams.

The detached City record holds ordered <code>NpcRuntimeId</code> values, while live <code>CityRuntime.ImportantNpcs</code> holds <code>NpcRuntime</code> object references and the NPC owner carries the reciprocal City/location links. The graph-builder contract below resolves this with one City construction, one NPC construction, and one unpublished relation-assembly pass. The current P12 canonical D technical design §4 steps 3–4 explicitly prescribe the required sequence: construct each City exactly once with its private ordered-members backing list reserved; construct each NPC exactly once with direct references to its staged City/location; fill each City membership list exactly once after all NPCs exist; then validate reciprocal links before handing off the unpublished graph. This proposal follows that sequence. The earlier sequencing concern is resolved in current canonical.

## Owner projection

The accepted current-base D design is the semantic boundary. This slice captures these fields once from each exact installed owner:

| Owner | Detached values | Exact identity and revision facts |
|---|---|---|
| <code>CityRuntime</code> | City runtime identity, <code>CityData.DefinitionId</code>, legacy <code>SpatialLocationRuntime.RuntimeId</code>, ordered <code>ImportantNpcs</code> member runtime IDs | Preserve <code>ImportantNpcRevision</code>; do not create a City aggregate revision. <code>CityData</code> and <code>ItemData</code> remain admitted content identities, not copied Unity objects. |
| <code>SettlementPopulationRuntime</code> | Settlement runtime ID, <code>CurrentPopulation</code>, every retained committed operation receipt (operation identity, fingerprint, and full transition value) | Preserve aggregate <code>Revision</code> and receipt-ledger-local revision independently. A zero population still has one aggregate owner. |
| <code>MarketRuntime</code> | Market item rows in exact owner order and multiplicity; each row's item definition ID, <code>Amount</code>, <code>DesiredAmount</code>, and stored <code>CurrentPrice</code> | Preserve <code>MarketRuntime.Revision</code>. Do not recompute price during export or restore. |
| <code>MarketCounterpartyRuntime</code> | <code>CounterpartyRuntimeId</code>, <code>LiquidityMode</code>, and whether the account reference is absent | No independent counterparty revision exists. The City and Market must refer to the same counterparty instance. |
| <code>PopulationEconomyRuntime</code> | <code>CityRuntimeId</code>, <code>PopulationEconomicRuntimeId</code>, <code>PaymentMode</code>, and whether the account reference is absent | These identity/mode fields are immutable after construction; there is no separate component revision. |

For the current <code>UnityBootstrap-Daily-v1</code> composition, the City counterparty is <code>Open</code>, population consumption is <code>Free</code>, and both City <code>MoneyAccountRuntime</code> references are absent. <code>MoneyAccountRuntime</code> exposes balance/revision but has no stable account ID, and the current P12-B money-account census is per-NPC. The City adapter must reject a City whose market is <code>AccountBacked</code>, whose population payment mode is <code>AccountBacked</code>, or whose corresponding account reference is non-null. It must not invent an account ID, borrow an NPC witness, or encode an absent account as a zero-balance account. Supporting City accounts later requires an admitted City account identity and census contract.

The snapshot stores IDs and values only. It contains no live <code>CityData</code>, <code>ItemData</code>, <code>SpatialLocationRuntime</code>, <code>NpcRuntime</code>, account, or other Unity/runtime object reference.

## Capture evidence and consistency

Capture is valid only for the exact current Daily-v1 completed-boundary token and the shared transient capture stamp. The D/E/F capture orchestration obtains and prevalidates one token on the simulation owner thread, then passes that same token and its shared stamp to the City and NPC projections. City capture consumes these inputs; it does not acquire another token, mint a stamp, or add a capture lock. The detached export carries neither as save state. The orchestration retains the token, stamp, and copied owner vector until it performs the single post-copy token validation.

Use the exact schema-v1 section contracts already emitted by the current owners:

| Section | Exact owner | Expected cardinality | Revision |
|---|---|---:|---|
| <code>p12b.city.important-npcs/&lt;CityRuntimeId&gt;</code> | That exact <code>CityRuntime</code> | <code>ImportantNpcs.Count</code> | <code>ImportantNpcRevision</code> |
| <code>p12e.city-market-stock-rows/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | That City's exact <code>MarketRuntime</code> | <code>Items.Count</code> | <code>MarketRuntime.Revision</code> |
| <code>p12d.city-population.aggregate/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | That City's exact <code>SettlementPopulationRuntime</code> | <code>1</code> | <code>SettlementPopulationRuntime.Revision</code> |
| <code>p12d.city-population.operation-receipts/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | The same population owner | Retained operation-receipt count | Receipt-ledger-local revision |

<code>&lt;length&gt;</code> is the invariant-culture string length followed by <code>:</code> and the exact City runtime ID, matching the existing Market and population providers. The City presence provider uses its existing <code>SectionIdFor</code> format. Each vector entry must match the section ID, schema, required role, exact owner object identity, cardinality, and revision in the token. Do not add a fifth synthetic City revision.

The shared capture orchestration and City projection should:

1. The orchestration obtains and prevalidates the existing <code>DailyCaptureEligibilityToken</code> once; it requires the accepted Daily-v1 profile and a completed, quiescent boundary, and supplies the same token and capture stamp to City and D/E/F NPC projection capture.
2. The City projection selects and validates the four City-specific owner-section snapshots above from that token. Confirm the population owner's <code>SettlementRuntimeId</code> equals the City ID and the City's market points to the exact <code>City.MarketCounterparty</code>.
3. Copy City, market, and population values through owner-local exact-copy APIs. The population receipt rows and their ledger revision must be copied together under the existing <code>operationReceiptGate</code>; return detached immutable records sorted by operation identity for deterministic representation, without changing receipt semantics.
4. On that same serialized owner thread and within the prevalidated token/stamp capture interval, read the City P18 empty-state facts through the owner-local proof below. Fail closed for unsupported account-backed City state and excluded populated state.
5. The orchestration revalidates that same token once after all City and NPC projections have copied. A changed token, stamp/vector, owner identity, or owner field check rejects the whole capture; no partial package is returned.

The token's mutation epoch and complete section vector remain P12-B evidence. The shared orchestration owns token acquisition, pre/post validation, capture-stamp identity, and owner-thread admission. This City slice adds no capture lock, census registration, owner-thread mechanism, P12-B mutation wiring, or second City/NPC capture window.

## Excluded City state and fail-closed admission

Two City sub-states are outside this Daily-v1 D projection and must be absent:

- **P18 daily-economy continuation receipts.** <code>CityRuntime.dailyEconomyReceipts</code> and <code>dailyEconomyReceiptRevision</code> are not exported. If either is populated/advanced, reject the City package. These are distinct from <code>SettlementPopulationRuntime</code>'s D-owned population-operation receipts, which are included exactly.
- **P14 finite-source/material-flow state.** Reject when <code>HasLocalDailyMaterialFlow</code> is true, <code>FiniteProductionSources</code> is present, or <code>LastMaterialFlow</code> is present. Do not capture finite-source reserves, material-flow receipts, or derived flow results.

Canonical <code>CityRuntime</code> owns a private <code>Dictionary&lt;string, CityDailyEconomyReceipt&gt;</code> and a private <code>dailyEconomyReceiptRevision</code>, initialized to empty/zero. A new committed receipt installs a copied dictionary and increments the revision once; an exact replay commits no new receipt and does not increment it. No removal path exists in this owner. The smallest proof seam is an internal, read-only owner method that, in one call and without a lock or mutation, returns <code>count = dailyEconomyReceipts?.Count ?? 0</code> and the exact <code>dailyEconomyReceiptRevision</code>. Called only inside the shared prevalidated owner-thread capture interval, the City projection requires <code>count == 0</code> and <code>revision == 0</code>; every populated, advanced, negative, or otherwise inconsistent pair fails closed. This does not export P18 values or create a census section, synthetic City revision, or competing capture lock. It is safe only under the accepted completed-boundary/quiescent owner-thread contract and the shared orchestration's single after-copy token validation. Focused owner tests must reject both populated receipts and empty-but-advanced/inconsistent state.

## Supported writes, receipts, and rollback semantics

These are current owner paths the snapshot must preserve after their committed domain operations:

- <code>CityRuntime.AddImportantNpc</code> / <code>RemoveImportantNpc</code> and the reciprocal presence mutation path append/remove exact membership and advance <code>ImportantNpcRevision</code> for each effective change. The member order is semantically relevant because current actor-selection providers iterate this list. Preserve it byte-for-byte as an ordered identity sequence: do not sort it or rebuild it from the NPC roster.
- <code>MarketRuntime.AddStock</code>, <code>RemoveStockUpTo</code>, and <code>UpdatePrices</code> mutate ordered market rows and advance the market revision only on their effective commit rules. Economy transactions prepare replacement market rows and install them through the Market owner. Capture the committed rows and current stored prices; restoration must not execute production, consumption, trading, or price refresh.
- <code>SettlementPopulationRuntime.TryApplyTransition</code> updates aggregate population/revision without installing a receipt. <code>TryApplyTransitionWithReceipt</code> installs one new operation receipt with the aggregate change; an exact replay is a successful no-op, while a same-key conflict or stale/invalid transition fails without changing the owner. Successful paired migration changes both population aggregates/revisions and adds no operation receipt.
- <code>SettlementPopulationRuntime.RestoreSnapshot</code> is a rollback hook. It may rewind aggregate value/revision and prune receipts whose transition expected revision is at or after the rollback target. When it prunes one or more receipts, the receipt-local revision advances once if below <code>long.MaxValue</code>; it is not rewound with the aggregate. At saturation, new receipt installs are rejected and pruning can only change cardinality.

### Current selected-profile writer-to-census trace

This source crosswalk covers supported runtime commits that can change the City projection. The owner snapshot reads committed values; it does not repeat these writes or report their mutation epoch.

| Supported entrypoint | Committed City-owned changes | P12-B operation and witness |
|---|---|---|
| Daily advance: <code>SimulationRuntime.AdvanceOneDay</code> → <code>SimulateEconomyDay</code> → <code>CityRuntime.SimulateProductionDay</code> / <code>SimulateConsumptionDay</code> in <code>Free</code> mode / <code>UpdateMarketPrices</code> | Production uses <code>MarketRuntime.AddStock</code>; free consumption uses <code>RemoveStockUpTo</code>; price refresh uses <code>UpdatePrices</code>. Only successful effective owner commits change ordered row values/current stored prices and advance <code>MarketRuntime.Revision</code>. The Market owner reports its exact City section after each commit. | Named <code>runtime.advance-day</code> operation; exact <code>p12e.city-market-stock-rows/{length}:{CityRuntimeId}</code> witness. |
| Merchant purchase: selected <code>MerchantSystem</code> action calls <code>EconomyTransactionService.TryExecuteMarketPurchase</code> directly; <code>MarketRuntime.BuyItem</code> is a façade to the same service. | The existing transaction commits the exact NPC account, Market stock removal, and NPC Inventory owners in order, including supported compensation. The City projection includes only committed Market rows and Market-local revision; NPC/account payload stays with the merged NPC owner projection. | Named <code>runtime.economy.market-purchase</code> scope; Market section notification follows each committed Market revision. |
| Merchant sale: selected <code>MerchantSystem</code> action calls <code>EconomyTransactionService.TryExecuteMarketSale</code> directly; <code>MarketRuntime.SellItem</code> is a façade to the same service. | The existing transaction commits NPC account/Inventory and Market stock addition, with supported account compensation. The City projection includes only the final committed Market rows and local revision. | Named <code>runtime.economy.market-sale</code> scope; same exact per-City Market witness. |
| Population lifecycle and migration | Existing supported transitions update the exact <code>SettlementPopulationRuntime</code> aggregate and, where receipt-backed, its retained receipt set and separate receipt revision. City membership changes use <code>ImportantNpcRevision</code>; the snapshot preserves both owners' post-commit values. | Promoted owner/operation ledger maps the population aggregate and receipt sections plus affected <code>p12b.city.important-npcs/{CityRuntimeId}</code> section to the enclosing commits. |

<code>AccountBacked</code> City/population modes remain rejected by this profile boundary. P14 finite-source material flow, <code>CityRuntime.TryCommitDailyEconomy</code>, its P18 receipt state, and the account-backed consumption branch are excluded; they are not alternative writers admitted by this table. The accepted P12-B operation names and exact City Market witness are cross-referenced in the promoted owner/commit ledger and Market operation design below.

The export must represent the live post-commit or post-rollback owner values, including the exact receipt set and both population revisions. Never use <code>RestoreSnapshot</code>, rollback snapshots, or <code>PreparedMarketState</code> as persistence adapters. A new exact population copy/factory path is needed because the rollback API does not restore the retained receipt set.

<code>CityRuntime.TryPrepareDailyEconomyStep</code> / <code>TryCommitDailyEconomy</code> can commit Market/account results and City daily receipts as one daily operation. Any non-empty/advanced City daily receipt state is excluded and rejects this slice. The City daily receipts are not interchangeable with population receipts. P14 paths are rejected as described above.

## Exact staged factory seam

The owner-local factory is private/internal to the D staging assembly and publishes nothing. It resolves IDs against already admitted content and the already staged legacy spatial graph:

- Resolve the exact <code>CityData.DefinitionId</code>, each market <code>ItemData.DefinitionId</code>, and legacy location runtime ID. Reject missing, duplicate/ambiguous, or incompatible resolution; never allocate replacement runtime IDs or rerun bootstrap/genesis.
- Construct one staged <code>MarketCounterpartyRuntime</code> only for the supported <code>Open</code>/no-account state, and share that exact instance with the staged <code>MarketRuntime</code> and <code>CityRuntime</code>.
- Construct <code>MarketRuntime</code> directly from exact row values and stored prices plus its exact revision. Do not seed from <code>CityData.marketItems</code>, call <code>AddStock</code>, or call <code>UpdatePrices</code>.
- Construct <code>SettlementPopulationRuntime</code> directly from its exact aggregate, aggregate revision, detached receipt records, and receipt revision under an owner-specific exact factory. Validate receipt shape and unique operation identities. Do not call <code>RestoreSnapshot</code>.
- Construct one staged <code>PopulationEconomyRuntime</code> from its exact supported identity/mode (<code>Free</code>, no account); do not recreate balances from initial purchasing power.
- Construct one <code>CityRuntime</code> from the exact runtime/definition/location identities, exact nested owner instances, the captured <code>ImportantNpcRevision</code>, and a private backing list reserved for its ordered members. Do not call the normal constructor that seeds market and population from definitions, or replay <code>AddImportantNpc</code> or NPC presence writes to restore membership.
- After constructing each staged NPC once, resolve the captured ordered NPC IDs through the shared graph builder and fill the City's backing list exactly once. Validate each member's reciprocal <code>CurrentCity</code> and <code>CurrentLocation</code> against the staged City/location. No NPC fields or NPC serialization belong to this City owner slice.

The serialized shared D/E/F City/NPC stage owns one private graph builder and consumes the single token, shared capture stamp, and owner-component vector supplied by the capture orchestration. Construction keeps the existing City-before-NPC owner creation order while resolving the reference cycle without gameplay mutations or a second owner construction:

1. Resolve and validate all captured City, location, definition, and NPC IDs, including duplicate/dangling IDs, City ordered member IDs, and each NPC's captured current-City/current-location IDs, before accepting any stage result.
2. For each City, create one private mutable backing list for its final ordered <code>ImportantNpcs</code> references. Construct that final <code>CityRuntime</code> exactly once through its owner-local exact factory, with exact nested owner state, captured <code>ImportantNpcRevision</code>, and that backing list. The list is private to the unpublished graph builder; the City's public view remains read-only.
3. Construct each final <code>NpcRuntime</code> exactly once through the shared D/E/F exact factory, resolving its captured current-City ID to the already constructed City and its current-location ID to the staged location. This factory assigns exact references directly; it must not call the public starting-City constructor, <code>CityRuntime.AddImportantNpc</code>, or <code>NpcRuntime.SetCurrentPresence</code>, which are gameplay mutation paths.
4. For every City, resolve its captured ordered ImportantNpc IDs through the exact staged NPC identity map and fill its private backing list once in that order, without sorting, replaying presence operations, or changing the captured revision. Validate there are no duplicate/dangling members and that each membership ref is exactly reciprocal with the NPC's captured/current staged City and location. Also reject any NPC whose current-City ref claims a City that omits it from its ordered list.
5. Return the private City/NPC graph only after all owner and reciprocal-link validation succeeds. On any failure, discard the entire unpublished staged graph; the active runtime is untouched. The City and NPC objects are each constructed once, and final graph linking is a single prepublication relation assembly, not a second constructor or owner hydrator.

The City owner worker owns the exact City factory and its backing-list contract; the NPC owner worker owns the exact NPC factory. The single serialized D/E/F City/NPC integration worker owns the graph builder, identity resolution, reciprocal validation, and private package handoff. This is the sequence already specified by current P12 canonical D design §4 steps 3–4: City once with pending ordered IDs/private list reserved, NPC once with direct staged City/location references, then membership fill once and reciprocal validation before private handoff. No separate D design amendment is needed. No City/NPC owner may be constructed or captured independently by parallel workers.

## Focused validation obligations for the later implementation

These are design gates, not tests run by this proposal:

1. Capture empty and populated City roots; prove repeat captures at an unchanged valid token have identical detached values and order, and later source mutations cannot change an earlier export.
2. Verify every City token section above has the exact schema, role, owner identity, cardinality, and revision. Reject missing, duplicate, stale, wrong-owner, wrong-cardinality, wrong-revision, wrong-thread, unsupported-profile, and changed-token capture. Assert no City aggregate revision is introduced.
3. Round-trip City/definition/legacy-location IDs; market item IDs, row order/multiplicity, stock, desired stock, stored current price and revision; counterparty and population-economy IDs/modes/account absence; population value/revision; and the full population receipt set/revision. Confirm restoration executes no economic operation or repricing.
4. Preserve an <code>ImportantNpcs</code> insertion order that differs from NPC roster order, preserve its revision exactly, and later reject duplicate/dangling/non-reciprocal IDs in the merged D graph without sorting or silently dropping members.
5. Cover direct and receipt-backed population transitions, exact replay, conflicting replay, stale rejection, paired migration, rollback receipt pruning, same-cardinality replacement, and receipt-revision saturation. Verify snapshots after rollback preserve the owner state as it exists after rollback.
6. Reject an account-backed counterparty, account-backed population payment mode, or any non-null City MoneyAccount reference; do not accept an NPC account witness as City coverage.
7. Test the owner-local P18 proof accepts empty/zero only and rejects populated, empty-but-advanced, negative, or otherwise inconsistent count/revision pairs; read both facts together on the owner thread. Also reject any P14 finite-source/material-flow state.
8. Keep owner tests separate from the later P12-G live/capture integration and full continuation parity gates; this slice does not claim either.
9. Prove City and D/E/F NPC projections receive the exact same token and shared capture-stamp identity/value and exact owner-section vector, with one prevalidation and one after-copy validation owned by orchestration. For graph staging, prove each City and NPC factory runs once, ordered membership may differ from NPC roster order, no gameplay presence path changes the revision, and dangling or non-reciprocal IDs fail with no published package.

No Unity tests are run for this proposal.

## File ownership and implementation order

For a later isolated implementation, the City owner worker owns only:

- <code>Assets/_Project/Scripts/CityRuntime.cs</code>
- <code>Assets/_Project/Scripts/MarketRuntime.cs</code>
- <code>Assets/_Project/Scripts/MarketCounterpartyRuntime.cs</code>
- <code>Assets/_Project/Scripts/PopulationEconomyRuntime.cs</code>
- <code>Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs</code>
- a new D City snapshot/owner-factory source file and its focused EditMode tests

This worker does **not** own <code>NpcRuntime</code>, <code>SimulationRuntime</code>, <code>TesteSimulacao</code>, bootstrap composition, P12-B registration/protocol, P14/P18 owner implementation, P12-G publication, the shared D/E/F graph-builder source file, or whole-D validation. The current City/NPC shared window is serialized: the shared orchestration supplies one prevalidated token, one capture stamp, and one identical City/NPC owner-component vector; a single integration worker constructs each City once before each NPC, resolves each NPC's direct City/location references, then fills each City's ordered member list once and validates the reciprocal graph. The shared D/E/F integration worker owns that builder; it does not duplicate the City/NPC owner adapters. Later G integration validates the full graph and performs one publication.

Other exclusions: new gameplay; P10/P18/P14 fields; Person or genealogy facts; NPC-owned payload fields or serialization; D/E/F duplication; P12-A or whole-D profile composition; P13 fork/history; save envelope, storage/migration format, persistence API, global rollback or diagnostics-as-save.

## Source and authority crosswalk

- [P12-D technical design](PHASE12_D_TECHNICAL_DESIGN.md) — City owner fields, owner-section vector, excluded City state, and staged-hydration boundary.
- [P12-D exact design review](PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md) — pass for the D design; implementation readiness restricted to Genealogy.
- [P12-D State](../PHASE12_STATE.md) and [P12 Brief](../phases/PHASE12_BRIEF.md) — City/population/NPC shared-owner work remains outstanding; P12-A remains <code>WAIT_DEPENDENCY</code>.
- [CityRuntime.cs](../../Assets/_Project/Scripts/CityRuntime.cs) — concrete City fields, membership/revision, City daily receipts, normal constructors, and P14 state.
- [NpcRuntime.cs](../../Assets/_Project/Scripts/NpcRuntime.cs) — starting-City construction and <code>SetCurrentPresence</code> use gameplay presence mutation; the exact NPC factory must assign staged City/location refs without replaying those writes.
- [CityData.cs](../../Assets/_Project/Scripts/Data/CityData.cs) — City definition ID and default <code>Open</code>/<code>Free</code> config modes.
- [MarketRuntime.cs](../../Assets/_Project/Scripts/MarketRuntime.cs) and [MarketCounterpartyRuntime.cs](../../Assets/_Project/Scripts/MarketCounterpartyRuntime.cs) — exact market rows/revision and counterparty identity/mode/account reference.
- [PopulationEconomyRuntime.cs](../../Assets/_Project/Scripts/PopulationEconomyRuntime.cs) and [MoneyAccountRuntime.cs](../../Assets/_Project/Scripts/MoneyAccountRuntime.cs) — population-economy ID/mode/account reference and account balance/revision without a stable account ID.
- [SettlementPopulationRuntime.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs) and [SettlementPopulationTransition.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationTransition.cs) — aggregate mutation, operation-receipt ledger, local revision, paired migration, and rollback pruning.
- [CityNpcPresenceCensusProvider.cs](../../Assets/_Project/Scripts/CityNpcPresenceCensusProvider.cs) — exact City owner-section witness and bidirectional ordered-membership/current-City/current-location validation; [CityMarketCensusProvider.cs](../../Assets/_Project/Scripts/CityMarketCensusProvider.cs) and [SettlementPopulationCensusProvider.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationCensusProvider.cs) — exact Market/population owner-section identities and witness values.
- [SimulationRuntime.cs](../../Assets/_Project/Scripts/SimulationRuntime.cs) and [ContinuationCensusProtocol.cs](../../Assets/_Project/Scripts/ContinuationCensusProtocol.cs) — completed-boundary token validation, global mutation epoch, and owner-section snapshots.
- [PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md](PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md) and [PHASE12_P12B_MARKET_OPERATION_INVALIDATION_DESIGN.md](PHASE12_P12B_MARKET_OPERATION_INVALIDATION_DESIGN.md) — the promoted exact City Market section, daily-advance writer bridge, purchase/sale operation IDs, and committed-section notifications.
- [MerchantSystem.cs](../../Assets/_Project/Scripts/MerchantSystem.cs) and [EconomyTransactionService.cs](../../Assets/_Project/Scripts/EconomyTransactionService.cs) — selected merchant action entrypoints, shared transaction owners, operation scopes, compensation, and committed Market changes.
