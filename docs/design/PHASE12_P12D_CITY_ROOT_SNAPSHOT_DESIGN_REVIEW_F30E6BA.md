# P12-D City factual-root snapshot design review

**Review ID:** P12D-CITY-ROOT-SNAPSHOT-DESIGN-F30E6BA-R1
**Outcome:** `PASS` — exact current-base design content is coherent with the accepted P12-D boundary, current owner/source contracts, and current staging order.
**Readiness:** Design review passes for this bounded City owner sub-slice only. This does not deliver code, complete P12-D, make P12-A ready, or authorize work outside this sub-slice.

## Exact revisions reviewed

- Candidate: `f30e6ba520eff7841cf35849818910d17e5e4202` (`codex/phase12/P12DCityRootSnapshotCurrentBase`).
- Candidate parent and stated P12 canonical base: `aa1a40f2e6da53a1a7388601bd13052f1a535545`.
- Current `origin/codex/phase12/canonical` at review: `aa1a40f2e6da53a1a7388601bd13052f1a535545`.
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Exact design blob: `docs/design/PHASE12_P12D_CITY_ROOT_SNAPSHOT_DESIGN.md` — `3ae6725227a166d8030a017c210d9a5459cab830`.
- P12-D technical design blob at base: `a6f72aabc26057b46ec8896738c1006c016d880e`.
- P12 Brief blob at base: `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`.
- P12 State blob at base: `bbf5d93b74df0b6aa2e30b4245023696f6c1be88`.
- Current owner/commit ledger blob at base: `709042d2bd881147f794ca9a5ae54fa5315ff230`.

This is an exact technical-design review only. No code changed and no tests were run.

## Review findings

No blocking findings remain. The two prior review notes are resolved in this candidate: the market trace now includes both the selected `MerchantSystem` direct calls and the `MarketRuntime` facades, and it removes the stale requirement to amend P12-D §4 because the current canonical design already specifies the same one-time City/NPC relation assembly.

## Contract and source checks

1. **Accepted scope and owner boundary.** The accepted P12 Brief already includes City/market/economic factual roots and population relations under P12-D. This is a bounded sub-slice of that accepted checkpoint, not a new checkpoint ID or material scope expansion. No separate product-scope acceptance is indicated by the current Brief. The prior D review’s Genealogy-only readiness is respected: this review is specific to the City owner/staging seam and makes no broader D readiness claim.

2. **Identity, cardinality, and revisions.** The design preserves `CityRuntime.RuntimeId`, content `CityData.DefinitionId`, and legacy location `RuntimeId`; it keeps the ordered `ImportantNpcs` IDs and exact `ImportantNpcRevision`. It uses the existing per-City Market section owner/count/revision, population aggregate owner/cardinality-one/revision, and distinct operation-receipt cardinality/receipt revision. `SettlementPopulationCensusProvider` confirms the two population sections share the exact `SettlementPopulationRuntime` owner but use aggregate and receipt-local revisions respectively. No synthetic City revision is introduced. Market counterparty normalization binds its stable ID to the City runtime ID; the population-economy identity is immutable and City-derived.

3. **Market and population values.** `MarketItemRuntime` retains only item reference, amount, desired amount, and stored current price; the proposed detached row contains all four as IDs/values and preserves row order and multiplicity plus `MarketRuntime.Revision`. Hydration explicitly avoids constructor seeding and `UpdatePrices`, so it does not recompute causal stored price. Current Daily-v1 uses `Open`/`Free` City economy and absent City accounts; the design rejects account-backed modes or non-null account references rather than borrowing per-NPC account identity.

4. **Receipt and rollback semantics.** The source confirms direct population transitions update aggregate/revision without a receipt; receipt-backed transitions install the aggregate and receipt together; matching replay is a no-op; conflict/stale rejection leaves state unchanged. `RestoreSnapshot` rewinds aggregate state and prunes qualifying receipts; receipt revision increments once below saturation and is not rewound. At saturation, pruned cardinality still changes, while new receipt installs reject. The design captures post-rollback live state, requires a new exact copy/factory path, and does not mistake rollback APIs for persistence adapters.

5. **Daily market writes and P12-B operation.** `SimulationRuntime.SimulateEconomyDay` runs the selected non-finite path through production, free-population consumption, and price refresh inside the outer `runtime.advance-day` scope. Source confirms `SimulateProductionDay` commits through `MarketRuntime.AddStock`, free-mode `SimulateConsumptionDay` through `RemoveStockUpTo`, and `UpdateMarketPrices` through `MarketRuntime.UpdatePrices`; these advance the Market local revision only on effective commits and report the exact per-City Market section through its bound P12 mutation boundary.

6. **Purchase/sale writers and operation IDs.** `MarketRuntime.BuyItem`/`SellItem` delegate to the bound transaction service in the selected runtime, while normal `MerchantSystem` actions call `TryExecuteMarketPurchase`/`TryExecuteMarketSale` directly. Both enter the same service-level P12 operation boundary and use `runtime.economy.market-purchase` / `runtime.economy.market-sale`; committed account, Inventory, and Market revisions are notified at their owner boundaries, including supported compensation. The revised crosswalk now records both public paths and their shared service contract accurately. Standalone Market fallback service construction is outside the admitted runtime composition and does not alter the selected-profile contract.

7. **Token/vector and capture boundary.** The design consumes one existing Daily-v1 completed-boundary token and the shared transient capture stamp/vector for City and merged D/E/F NPC projections, validates the exact four current City owner sections, and leaves pre/post token validation, owner-thread/quiescence admission, and epoch authority with P12-B orchestration. It adds no new census registration, capture lock, epoch wiring, or second City/NPC capture window.

8. **Excluded state.** The design rejects P18 City daily-economy receipt state unless its owner-local count/revision proof is exactly zero, and rejects P14 finite-source/material-flow state. It distinguishes those excluded City receipts from included P12-D population receipts. It does not serialize P18/P14 state, add account-backed City economics, or broaden the selected Daily-v1 profile.

9. **City-before-NPC staging.** The current P12-D §4 already specifies the sequence reproduced by this candidate: validate IDs; construct each final City once with a private ordered-members list reserved; construct each NPC once against staged City/location references; then fill each City membership list once in captured order and validate reciprocal links before handoff. The candidate’s shared D/E/F integration split respects that order and keeps City/NPC capture/reconstruction serialized. No separate D technical-design amendment is needed.

## Scope and remaining gates

The design review passes only for exact City/Market/Population owner export and unpublished staging, with City account-backed state rejected. It does not implement whole-D validation, P12-G publication/parity, runtime/bootstrap composition, the D/E/F NPC adapter, or profile-wide export/hydration. P12-D remains open; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Any later implementation must follow the isolated ownership boundaries, required focused validation, independent code review, and canonical promotion procedure. No implementation authorization beyond the accepted P12-D City sub-slice is granted by this record.