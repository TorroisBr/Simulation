# P12-D City Factual-Root Snapshot and Staging - Checkpoint Proposal

**Status:** proposal for a bounded P12-D owner slice only. This is not an implementation authorization, an accepted checkpoint review, a delivered capability, P12-A readiness, or Phase closure.

**Canonical base:** <code>codex/phase12/canonical</code> at <code>da2a73896bc405ae6f11c536a5fbe8d471b00c21</code> (tree <code>ebdb529873d1826320b04224f1a9e79efc5c22db</code>). The <code>Assets</code> tree is unchanged from <code>03d14f8</code>.

**Scope:** exact detached export and unpublished exact-value staging for the current Daily-v1 <code>CityRuntime</code> factual root and its <code>MarketRuntime</code>, <code>MarketCounterpartyRuntime</code>, <code>PopulationEconomyRuntime</code>, and <code>SettlementPopulationRuntime</code> components. This is a sub-slice of existing P12-D, not a new checkpoint ID.

## Readiness and review boundary

P12-B and P12-C are promoted within their accepted scopes. The current P12-D technical design defines the City field boundary, but its exact review at <code>b9a0fd1b5941f0b7615a72acec39fd22e6c9ee0e</code> marks only the isolated GenealogyStore slice <code>READY_FOR_IMPLEMENTATION</code> and explicitly grants no implementation authorization beyond that slice. Current canonical §8 says every other owner needs its own current-source evidence and scoped readiness review. The P12 State still classifies City/population and the D/E/F NPC adapter as serialized shared-owner work. This proposal therefore remains **NOT READY FOR IMPLEMENTATION** until an independent exact-tip review accepts this City slice and its owner/staging seams.

A second seam needs review before City hydration can be composed: the detached City record holds ordered <code>NpcRuntimeId</code> values, while live <code>CityRuntime.ImportantNpcs</code> holds <code>NpcRuntime</code> object references and the NPC owner carries the reciprocal City/location links. This proposal specifies the exact owner factory input, but does not introduce temporary links, a partial City followed by a second hydration, or an ordering that the current D design has not resolved.

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

Capture is valid only for the exact current Daily-v1 completed-boundary token. The detached export carries no token as save state; a separate transient capture envelope retains the token and copied owner vector until capture validation completes.

Use the exact schema-v1 section contracts already emitted by the current owners:

| Section | Exact owner | Expected cardinality | Revision |
|---|---|---:|---|
| <code>p12b.city.important-npcs/&lt;CityRuntimeId&gt;</code> | That exact <code>CityRuntime</code> | <code>ImportantNpcs.Count</code> | <code>ImportantNpcRevision</code> |
| <code>p12e.city-market-stock-rows/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | That City's exact <code>MarketRuntime</code> | <code>Items.Count</code> | <code>MarketRuntime.Revision</code> |
| <code>p12d.city-population.aggregate/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | That City's exact <code>SettlementPopulationRuntime</code> | <code>1</code> | <code>SettlementPopulationRuntime.Revision</code> |
| <code>p12d.city-population.operation-receipts/&lt;length&gt;:&lt;CityRuntimeId&gt;</code> | The same population owner | Retained operation-receipt count | Receipt-ledger-local revision |

<code>&lt;length&gt;</code> is the invariant-culture string length followed by <code>:</code> and the exact City runtime ID, matching the existing Market and population providers. The City presence provider uses its existing <code>SectionIdFor</code> format. Each vector entry must match the section ID, schema, required role, exact owner object identity, cardinality, and revision in the token. Do not add a fifth synthetic City revision.

The capture adapter should:

1. Obtain and validate the existing <code>DailyCaptureEligibilityToken</code> on the simulation owner thread; require the accepted Daily-v1 profile and a completed, quiescent boundary.
2. Select and validate the four City-specific owner-section snapshots above from the token. Confirm the population owner's <code>SettlementRuntimeId</code> equals the City ID and the City's market points to the exact <code>City.MarketCounterparty</code>.
3. Copy City, market, and population values through owner-local exact-copy APIs. The population receipt rows and their ledger revision must be copied together under the existing <code>operationReceiptGate</code>; return detached immutable records sorted by operation identity for deterministic representation, without changing receipt semantics.
4. Fail closed for unsupported account-backed City state and excluded populated state (below).
5. Revalidate the same completed-boundary token after copying. A changed token, owner vector, owner identity, or owner field check rejects the whole City export; no partial package is returned.

The token's mutation epoch and complete section vector remain P12-B evidence. This slice adds no capture lock, census registration, owner-thread mechanism, or P12-B mutation wiring.

## Excluded City state and fail-closed admission

Two City sub-states are outside this Daily-v1 D projection and must be absent:

- **P18 daily-economy continuation receipts.** <code>CityRuntime.dailyEconomyReceipts</code> and <code>dailyEconomyReceiptRevision</code> are not exported. If either is populated/advanced, reject the City package. These are distinct from <code>SettlementPopulationRuntime</code>'s D-owned population-operation receipts, which are included exactly.
- **P14 finite-source/material-flow state.** Reject when <code>HasLocalDailyMaterialFlow</code> is true, <code>FiniteProductionSources</code> is present, or <code>LastMaterialFlow</code> is present. Do not capture finite-source reserves, material-flow receipts, or derived flow results.

The P12-D design states that P18 receipt state must be empty, but this base has no City daily-economy receipt census/provider or public read-only count/revision accessor. The receipt fields are private in <code>CityRuntime</code>, and <code>TryCommitDailyEconomy</code> can advance them. Before implementation, the exact City review must decide and scope a narrow owner-local empty-state check (count and revision together, without exporting P18 values) or another already-authorized proof of emptiness. Do not add a P12-B witness or a new City revision by assumption. This is a concrete owner-evidence gap for the fail-closed check.

## Supported writes, receipts, and rollback semantics

These are current owner paths the snapshot must preserve after their committed domain operations:

- <code>CityRuntime.AddImportantNpc</code> / <code>RemoveImportantNpc</code> and the reciprocal presence mutation path append/remove exact membership and advance <code>ImportantNpcRevision</code> for each effective change. The member order is semantically relevant because current actor-selection providers iterate this list. Preserve it byte-for-byte as an ordered identity sequence: do not sort it or rebuild it from the NPC roster.
- <code>MarketRuntime.AddStock</code>, <code>RemoveStockUpTo</code>, and <code>UpdatePrices</code> mutate ordered market rows and advance the market revision only on their effective commit rules. Economy transactions prepare replacement market rows and install them through the Market owner. Capture the committed rows and current stored prices; restoration must not execute production, consumption, trading, or price refresh.
- <code>SettlementPopulationRuntime.TryApplyTransition</code> updates aggregate population/revision without installing a receipt. <code>TryApplyTransitionWithReceipt</code> installs one new operation receipt with the aggregate change; an exact replay is a successful no-op, while a same-key conflict or stale/invalid transition fails without changing the owner. Successful paired migration changes both population aggregates/revisions and adds no operation receipt.
- <code>SettlementPopulationRuntime.RestoreSnapshot</code> is a rollback hook. It may rewind aggregate value/revision and prune receipts whose transition expected revision is at or after the rollback target. When it prunes one or more receipts, the receipt-local revision advances once if below <code>long.MaxValue</code>; it is not rewound with the aggregate. At saturation, new receipt installs are rejected and pruning can only change cardinality.

The export must represent the live post-commit or post-rollback owner values, including the exact receipt set and both population revisions. Never use <code>RestoreSnapshot</code>, rollback snapshots, or <code>PreparedMarketState</code> as persistence adapters. A new exact population copy/factory path is needed because the rollback API does not restore the retained receipt set.

<code>CityRuntime.TryPrepareDailyEconomyStep</code> / <code>TryCommitDailyEconomy</code> can commit Market/account results and City daily receipts as one daily operation. Any non-empty/advanced City daily receipt state is excluded and rejects this slice. The City daily receipts are not interchangeable with population receipts. P14 paths are rejected as described above.

## Exact staged factory seam

The owner-local factory is private/internal to the D staging assembly and publishes nothing. It resolves IDs against already admitted content and the already staged legacy spatial graph:

- Resolve the exact <code>CityData.DefinitionId</code>, each market <code>ItemData.DefinitionId</code>, and legacy location runtime ID. Reject missing, duplicate/ambiguous, or incompatible resolution; never allocate replacement runtime IDs or rerun bootstrap/genesis.
- Construct one staged <code>MarketCounterpartyRuntime</code> only for the supported <code>Open</code>/no-account state, and share that exact instance with the staged <code>MarketRuntime</code> and <code>CityRuntime</code>.
- Construct <code>MarketRuntime</code> directly from exact row values and stored prices plus its exact revision. Do not seed from <code>CityData.marketItems</code>, call <code>AddStock</code>, or call <code>UpdatePrices</code>.
- Construct <code>SettlementPopulationRuntime</code> directly from its exact aggregate, aggregate revision, detached receipt records, and receipt revision under an owner-specific exact factory. Validate receipt shape and unique operation identities. Do not call <code>RestoreSnapshot</code>.
- Construct one staged <code>PopulationEconomyRuntime</code> from its exact supported identity/mode (<code>Free</code>, no account); do not recreate balances from initial purchasing power.
- Construct one <code>CityRuntime</code> from the exact runtime/definition/location identities, exact nested owner instances, and the captured ordered <code>ImportantNpc</code> identities/revision. Do not call the normal constructor that seeds market and population from definitions, and do not replay <code>AddImportantNpc</code> or NPC presence writes to restore membership.
- Resolve the ordered NPC IDs against the same unpublished D/F staged NPC identity map before the final City owner construction, then validate each member's reciprocal <code>CurrentCity</code> and <code>CurrentLocation</code> against the staged City/location. No NPC fields or NPC serialization belong to this slice.

The current D design sequences City owners before NPC hydration, while the City factory needs concrete <code>NpcRuntime</code> references to restore its <code>ImportantNpcs</code> list. The shared D/NPC stage must settle how it supplies those references while still constructing each City and NPC once. The factory contract above preserves the one-owner/no-second-hydrator rule; this proposal does not choose a placeholder/fixup scheme.

## Focused validation obligations for the later implementation

These are design gates, not tests run by this proposal:

1. Capture empty and populated City roots; prove repeat captures at an unchanged valid token have identical detached values and order, and later source mutations cannot change an earlier export.
2. Verify every City token section above has the exact schema, role, owner identity, cardinality, and revision. Reject missing, duplicate, stale, wrong-owner, wrong-cardinality, wrong-revision, wrong-thread, unsupported-profile, and changed-token capture. Assert no City aggregate revision is introduced.
3. Round-trip City/definition/legacy-location IDs; market item IDs, row order/multiplicity, stock, desired stock, stored current price and revision; counterparty and population-economy IDs/modes/account absence; population value/revision; and the full population receipt set/revision. Confirm restoration executes no economic operation or repricing.
4. Preserve an <code>ImportantNpcs</code> insertion order that differs from NPC roster order, preserve its revision exactly, and later reject duplicate/dangling/non-reciprocal IDs in the merged D graph without sorting or silently dropping members.
5. Cover direct and receipt-backed population transitions, exact replay, conflicting replay, stale rejection, paired migration, rollback receipt pruning, same-cardinality replacement, and receipt-revision saturation. Verify snapshots after rollback preserve the owner state as it exists after rollback.
6. Reject an account-backed counterparty, account-backed population payment mode, or any non-null City MoneyAccount reference; do not accept an NPC account witness as City coverage.
7. Reject non-empty or advanced City daily-economy receipt state and any P14 finite-source/material-flow state. Once the receipt empty-state seam is approved, test its count/revision are read together and are zero.
8. Keep owner tests separate from the later P12-G live/capture integration and full continuation parity gates; this slice does not claim either.

No Unity tests are run for this proposal.

## File ownership and implementation order

For a later isolated implementation, the City owner worker owns only:

- <code>Assets/_Project/Scripts/CityRuntime.cs</code>
- <code>Assets/_Project/Scripts/MarketRuntime.cs</code>
- <code>Assets/_Project/Scripts/MarketCounterpartyRuntime.cs</code>
- <code>Assets/_Project/Scripts/PopulationEconomyRuntime.cs</code>
- <code>Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs</code>
- a new D City snapshot/staging source file and its focused EditMode tests

This worker does **not** own <code>NpcRuntime</code>, <code>SimulationRuntime</code>, <code>TesteSimulacao</code>, bootstrap composition, P12-B registration/protocol, P14/P18 owner implementation, P12-G publication, or whole-D validation. The current City/NPC shared window is serialized: finish and review the owner-local City export/factory seam, then settle the single D/E/F NPC capture/factory integration with its worker. Both projections must use one P12-B token and identical City/NPC owner-component vectors; the concrete <code>CityRuntime</code> is constructed once after its ordered NPC references are available. Later G integration validates the full graph and performs one publication.

Other exclusions: new gameplay; P10/P18/P14 fields; Person or genealogy facts; NPC-owned payload fields or serialization; D/E/F duplication; P12-A or whole-D profile composition; P13 fork/history; save envelope, storage/migration format, persistence API, global rollback or diagnostics-as-save.

## Source and authority crosswalk

- [P12-D technical design](PHASE12_D_TECHNICAL_DESIGN.md) — City owner fields, owner-section vector, excluded City state, and staged-hydration boundary.
- [P12-D exact design review](PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md) — pass for the D design; implementation readiness restricted to Genealogy.
- [P12-D State](../PHASE12_STATE.md) and [P12 Brief](../phases/PHASE12_BRIEF.md) — City/population/NPC shared-owner work remains outstanding; P12-A remains <code>WAIT_DEPENDENCY</code>.
- [CityRuntime.cs](../../Assets/_Project/Scripts/CityRuntime.cs) — concrete City fields, membership/revision, City daily receipts, normal constructors, and P14 state.
- [CityData.cs](../../Assets/_Project/Scripts/Data/CityData.cs) — City definition ID and default <code>Open</code>/<code>Free</code> config modes.
- [MarketRuntime.cs](../../Assets/_Project/Scripts/MarketRuntime.cs) and [MarketCounterpartyRuntime.cs](../../Assets/_Project/Scripts/MarketCounterpartyRuntime.cs) — exact market rows/revision and counterparty identity/mode/account reference.
- [PopulationEconomyRuntime.cs](../../Assets/_Project/Scripts/PopulationEconomyRuntime.cs) and [MoneyAccountRuntime.cs](../../Assets/_Project/Scripts/MoneyAccountRuntime.cs) — population-economy ID/mode/account reference and account balance/revision without a stable account ID.
- [SettlementPopulationRuntime.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs) and [SettlementPopulationTransition.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationTransition.cs) — aggregate mutation, operation-receipt ledger, local revision, paired migration, and rollback pruning.
- [CityNpcPresenceCensusProvider.cs](../../Assets/_Project/Scripts/CityNpcPresenceCensusProvider.cs), [CityMarketCensusProvider.cs](../../Assets/_Project/Scripts/CityMarketCensusProvider.cs), and [SettlementPopulationCensusProvider.cs](../../Assets/_Project/Scripts/Population/SettlementPopulationCensusProvider.cs) — current exact City/Market/population owner-section identities and witness values.
- [SimulationRuntime.cs](../../Assets/_Project/Scripts/SimulationRuntime.cs) and [ContinuationCensusProtocol.cs](../../Assets/_Project/Scripts/ContinuationCensusProtocol.cs) — completed-boundary token validation, global mutation epoch, and owner-section snapshots.
