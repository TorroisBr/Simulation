# P12-B selected Market operation and invalidation design

**Status:** Independent exact-tip design review passed for `9a71eb65b00d7425172225d0812de5f5cfb2c121`. This is a bounded implementation slice within the accepted P12-B prerequisite scope and creates no new checkpoint ID or product behavior.

## Baseline and authority

- Canonical base: `codex/phase12/canonical` at `b8a7da54864bee3fb9b8916793240e91fbce0955`.
- Current accepted design: `docs/design/PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md`.
- Current promoted transaction slice: bounded NPC-to-NPC trade invalidation at canonical candidate tip `522cf9158d9f650675eccfb6bcec4144dbaa32e2`, with code reviewed at `49b32c548e4b8c666657c031105401246cff2563` and review record `0279c8e40b5c51bdf5cd6dd03247832766a726de`.
- The Phase 12 Brief records accepted P12-B–P12-G prerequisite implementation scope. This design stays within that scope. It does not authorize canonical promotion, complete P12-B, or authorize P12-A.

## Refreshed P12-B owner/operation/epoch matrix

| Area | Canonical evidence at this base | Current classification | Remaining boundary |
|---|---|---|---|
| Sealed partial owner census | 142 required sections: PersonStore, per-NPC SpatialKnowledge, Inventory, MoneyAccount, and Knowledge families. | PARTIAL | Not the complete effective-profile owner inventory. |
| City Market owners | Existing `CityMarketCensusProvider` yields one schema-v1 witness per composed City, bound to its exact `MarketRuntime`; section ID is `p12e.city-market-stock-rows/{length}:{CityRuntimeId}` and cardinality is the live Market row count. | PASSIVE ONLY; not registered in the P12 protocol | Register the existing providers as required sections before sealing the protocol inventories. Selected profile has two City Markets. |
| Registered operation contracts | `runtime.npc-membership`, `runtime.bootstrap-publication`, `runtime.advance-day`, and promoted `runtime.economy.npc-trade`. | PARTIAL | Market purchase/sale and other selected mutators do not have named P12 operation scopes. |
| Mutation epoch notifications | NPC membership reconciliation and the bounded NPC trade service report exact committed sections. | PARTIAL | Market, direct Market owner writes, transfers, remaining economy, Travel/Expedition, and other included authorities remain uncovered. |
| Runtime admission/quiescence | Selected owner thread and named active operation accounting exist. | PARTIAL | The count is not a lock; full writer coverage and capture eligibility/token are absent. |
| P12-A/P13 | Canonical State keeps P12-A `WAIT_DEPENDENCY`; P13 blocked. | BLOCKED | No complete profile inventory, owner export/hydration, or P13 prerequisite completion is established here. |

### Why Market operation work is next

The accepted invalidation design orders remaining selected economy transactions beginning with Market purchase/sale, followed by money transfer and other flows. The actual selected-profile merchant path calls `EconomyTransactionService.TryExecuteMarketPurchase` and `TryExecuteMarketSale` with the already shared service. These operations join an NPC MoneyAccount, NPC Inventory, and a City Market stock owner whose exact passive witness already exists. Registering that existing Market owner and notifying it after real commits advances the shared operation/epoch blocker rather than adding another census provider.

The alternatives rank lower for this isolated next slice. Crime's theft path uses the NPC account transfer helper, but the transfer is followed by theft-outcome and warrant mutations, and rejection can initiate a reverse transfer; its complete outer owner set is not yet registered. Travel charge/restore and group compensation span wider participant and Travel/Party owners. Account-backed population consumption and City/Counterparty accounts are excluded by the selected GeneralTest profile's Free/Open configuration.

## Bounded implementation contract

### Owner registration and local commit bridge

1. Reuse `CityMarketCensusProvider.CreateProviders(cities)` for the exact two selected City Markets. Register each provider's existing section as `Required`, with schema v1, exact Market object identity, live row cardinality (including zero), and Market-local revision. For the current two-City profile this adds two sections to the partial registered set; it does not make the set complete.
2. Bind each registered `MarketRuntime` to its exact Market section notification callback after protocol registration and before bootstrap publication. A successful `AddStock`, `RemoveStockUpTo`, or `UpdatePrices` revision commit reports that one Market section immediately. `UpdatePrices` must first determine whether any price would change; if so and the revision is exhausted, it performs no price writes. If no price changes, it remains a no-op even at the revision limit. A no-op or failed owner call does not report a change. The callback faults P12 admission closed if the protocol cannot record a committed change; it never rewrites an already established domain result.
3. This owner-local bridge also covers selected daily production, Free-mode consumption, and price refresh: `SimulationRuntime.SimulateEconomyDay` already runs under `runtime.advance-day`, and its City methods use those Market writers. The P18-specific prepared `CityRuntime.TryCommitDailyEconomy` path is excluded from `UnityBootstrap-Daily-v1` and is not changed.

### Purchase and sale operations

1. Register stable operations `runtime.economy.market-purchase` and `runtime.economy.market-sale` before sealing the operation inventory.
2. Before the first possible owner commit, resolve the exact rostered NPC, its exact registered `p12e.npc-money-account/{RuntimeId}` and `p12f.inventory/{RuntimeId}` sections, and the exact City Market object/section. Require their current identities and revisions to match accepted protocol baselines. On the bound P12 path, reject and fault admission closed before writes if the owner-thread, participant, Market, section, or baseline boundary cannot be established. An unbound standalone service keeps its existing domain path.
3. Keep the named service scope active from before the first possible commit through all owner commits, compensation, and result publication. The selected profile supports Open liquidity only. Account-backed City/Counterparty MoneyAccounts are not composed by this profile; a bound P12 service must reject an account-backed path before any write rather than imply its owners are covered.
4. Purchase commit order is the existing NPC debit, Market stock removal, then NPC Inventory addition. Notify each exact changed section after its local revision commits. If a later stage fails and the existing method refunds the NPC, notify that committed account revision too. Sale commit order is NPC account credit, NPC Inventory removal, then Market stock addition, with the existing account reversal notified when Inventory removal fails.
5. Before a sale's first owner write, require that the exact Market revision can advance once as well as the existing stock-capacity preflight. The current service ignores a failed `Market.AddStock` result after earlier writes; the implementation must check the committed amount and must never treat an absent Market revision as a successful stock commit. If the preflightable revision-capacity check fails, return the existing transaction failure before any owner changes. If an unexpected post-preflight install failure occurs, notify every owner that actually committed and fault P12 closed; do not claim whole-operation rollback.
6. `TesteSimulacao` already creates the transaction service used by `MerchantSystem`. After constructing `SimulationRuntime` and before bootstrap publication, bind that exact same service to the runtime's P12 census bridge and to every registered City `MarketRuntime`. The Market wrappers use this installed service; an unbound standalone Market retains the current fresh-service fallback. Do not create a second transaction service for a composed P12 Market.

## Explicitly bounded exclusions

- This slice covers only Open-liquidity NPC market purchase/sale and owner commits to the selected City Markets. It does not include account-backed Counterparty or PopulationEconomy owners, which require another supported profile inventory.
- Merchant plan completion, TravelPlan changes, CommercialKnowledge observations/sharing, and other consequences after the transaction service returns remain separate uncovered owner commits. They do not become covered merely because MerchantSystem called the transaction.
- NPC account transfer/Crime's theft outcome and warrants, other economy methods, P14 material-flow composition, P18 prepared installs/timeline economy, and other selected operation families remain separate work.
- Direct Market owner methods notify their own exact section when the installed P12 runtime is bound. This is mutation-epoch evidence, not a universal synchronization lock or a promise that arbitrary hand-constructed unbound services are admitted by the selected runtime.
- No complete owner inventory, shared-epoch completeness, CaptureEligible/token, export, hydration, P12-A readiness, P13 readiness, or Phase 12 closure is claimed.

## Validation and review plan

Before code submission, add focused tests for exact Market registration/identity and row cardinality changes; Open purchase/sale success and each committed owner notification; successful compensation; stale Market/NPC baselines and wrong-thread rejection before writes; bound Market wrappers using the shared service; daily production/Free consumption/price-refresh notifications; no-op and failed Market writes; Market revision-exhaustion sale preflight; and changed/no-op `UpdatePrices` behavior at revision exhaustion. Preserve existing unbound domain behavior.

Run the relevant EconomyTransaction, Market/City stock, SimulationRuntime admission and daily-economy suites, ALL EditMode, official Smoke, and `git diff --check` on the exact code tree. Record exact XML/log hashes. Obtain independent exact-tip review against the actual canonical base before treating implementation as a validated candidate. Canonical promotion remains a separate human gate.

## Independent design review correction — 2026-10-01

Exact-tip review of `85d6761` returned NEEDS_CHANGES. The corrections were
independently re-reviewed at exact design tip
`9a71eb65b00d7425172225d0812de5f5cfb2c121`; PASS is recorded in
`PHASE12_P12B_MARKET_OPERATION_INVALIDATION_DESIGN_REVIEW.md`. The first review
identified that
`MarketRuntime.UpdatePrices` could change price fields at `long.MaxValue`
without advancing its revision, and that the candidate had not specified how
`MarketRuntime.BuyItem` / `SellItem` receive the shared P12-bound transaction
service. This revision adds a pre-mutation price-change/revision-capacity rule
and test, and explicitly binds the exact service already used by MerchantSystem
to each composed P12 Market before bootstrap publication. Standalone Market
fallback behavior remains unchanged. No implementation or canonical promotion
is claimed.
