# Phase 14 Entry Architecture — Bounded v1 Proposal

**Status:** `ENTRY_ARCHITECTURE_READY`; the prior refreshed proposal passed independent entry review at `109341530c6d5f293974fc0475ff0c7c1279c7a4`. The identity-reconciled and P8-E promotion refresh passed independent review at `c4b7e91`. It remains a proposal for technical design, not canonical architecture, an implementation contract, checkpoint approval, or authorization to implement. No Phase 14 checkpoint IDs are approved.

**Historical baseline:** Phase 8 canonical at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, incorporating architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`.

**Current revalidation baseline:** `codex/phase8/canonical` at `77f3e1a47a1e007492a794ea777d681a21a36d09`, including P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`. P8-A through P8-E are canonical. The Phase 14 Brief is `ENTRY_ARCHITECTURE_READY`. This packet inspects the current canonical code; any specifically historical P7 facts are labeled as such.

**P8-E and alignment impact:** the reviewed single-City daily source/sink has no transport, route flow, timed activity, or multi-participant work. P8-E is available but adds no dependency; the daily cadence retains current semantics. P18/P20 remain conditional only for later timed-activity or shared-participation consumers. Current moddability constraints apply to future seams/review; P19 API/loader and module lifecycle remain deferred.

## 1. Governing constraints

Phase 14 establishes a bounded factual account of productive sources and material flow with explicit source/sink and ownership/custody semantics. The recommendation below selects the first consumer and closure using current code evidence; it deliberately keeps the v1 scope narrow.

Relevant architecture rules in `docs/SIMULATION_ARCHITECTURE.md`:

- §41 separates ownership from inventory/custody.
- §§42–43 separate ownership, jurisdiction, control, and allegiance; they caution against a universal holder model before a real consumer requires one.
- §45 requires explicit payer, receiver, limiting balance, and source/sink boundaries when a significant money flow exists.
- §69 establishes Hex/Location as simulation geography and separates Location identity from rendering coordinates.
- §§91–92 require recoverable causal state at simulated boundaries and distinguish save/reconstruction from history; the project is not event-sourced by default.
- §13 keeps economy, merchant autonomy, effective configuration, and commercial knowledge as distinct concerns.

`docs/EXECUTION_MODEL.md` separates entry architecture, technical design, and implementation. The prior refreshed proposal passed independent entry review at `109341530c6d5f293974fc0475ff0c7c1279c7a4`, and the identity/P8-E refresh passed independent review at `c4b7e91`. Technical design remains a separate review gate. No Phase 14 State or checkpoint contract exists in the observed base.

## 2. Current repository evidence

| Area | Current canonical behavior | Relevance to the proposed boundary |
|---|---|---|
| City production and population consumption | `CityData` authors market stock, consumption rates, and fixed `amountPerDay` production configurations. `CityRuntime.SimulateProductionDay()` adds configured quantities directly to the City market. `SimulateConsumptionDay()` removes available stock, either for free or through the existing account-backed transaction path. | This is the smallest existing local source/sink loop. Production has no input stock, separate producer runtime, finite reserve, or depletion state. The P14 proposal makes that direct configured addition an explicit source boundary. |
| City market stock | `MarketRuntime` holds aggregate integer quantities per `ItemData`. `StockOwnerRuntimeId` projects the market counterparty’s runtime ID; it is not a separate title registry or custodian record. Production and consumption results are returned as operation results, not retained material ownership records. | The proposal distinguishes settlement title from market custody and requires technical design to define durable semantic IDs. |
| Trade and inventories | `InventoryRuntime` stores integer item quantities on an `NpcRuntime`. `EconomyTransactionService` supports item transfers and checks money, stock, and capacity for its current transaction types. Trade receipts identify runtime IDs. | Merchant transfer, actor carriage, and durable title are outside the proposed first consumer. Their existing runtime-ID transaction path does not establish a general asset/title model. |
| Money | `MoneyEffect` distinguishes transfers and deliberate monetary sources/sinks. Population consumption defaults to free; account-backed consumption is also implemented. | The proposed proving case uses free consumption and makes no monetary mutation. |
| Property | `PropertyOwnershipStore` records `PropertyId → PersonId` and provides explicit property-transfer transitions. | It is a Person-based property foundation, not a generic stock or material-title store. |
| Spatial authority | P8-A provides factual Hex/Location geography; P8-C composes City/Site anchor bindings and Person spatial position; P8-D and P8-E are canonical. | A source attaches to a City instance with an authored stable `SettlementSemanticId` and its P8 `LocationId`. The current P8 City anchor owner key is checked against `CityRuntime.RuntimeId` during composition, so it is only an in-process lookup key for this profile, not durable settlement identity. The profile's `LocationId` must resolve and equal the selected City instance's composed P8 anchor. No route or passage behavior is needed for same-City production and consumption. |
| Daily execution | With Economy enabled, `SimulationRuntime.AdvanceDay()` runs City production for all cities, then population consumption and price updates. | The proposed cadence follows this existing daily autonomous order. |

The relevant existing coverage includes `SettlementStockOwnershipTests`, `PopulationConsumptionTests`, `EconomyTransactionTests`, and `PropertyTransferFoundationTests`. These tests demonstrate current behavior; they do not choose Phase 14 product scope. No tests were run as part of this documentation proposal.

## 3. Recommended first consumer and closure

The proposed v1 proving scenario uses one City, one configured `CityProductionConfig` entry for one `ItemData`, and a matching market consumption configuration for that same item. Each enabled economy day, the configured output is added to that City’s market; the same City’s population then consumes available stock.

For this v1, the configured production entry is an **explicit exogenous source**. It adds its configured quantity without inputs, a finite reserve, depletion, or processing. This is an intentional source boundary, not a claim that a mine, farm, or other physical resource exists behind it.

Closure ends at the City market’s closing balance after the same-day population-consumption sink. For the selected item, the proving balance is:

`closing stock = opening stock + committed configured output - actual population consumption`

The proving scenario isolates this source and sink. It does not include merchant purchases/sales or other material transfers.

## 4. Proposed entry decisions for independent review

These are the recommended entry decisions for this bounded v1; they are proposals, not accepted canonical decisions.

1. **First consumer and closure:** one City’s configured daily production into its own market, followed by consumption by that City’s population. The closure boundary is the closing market balance after that consumption.
2. **Source semantics:** each configured output is an explicit exogenous source with a stable authored source key; source identity never comes from a list position. If a legacy entry has no key, compatibility is limited to a unique `SettlementSemanticId`-plus-item identity within that settlement's configured sources. Duplicate entries that this scoped identity cannot distinguish require stable authored keys and are unsupported by that compatibility path. Inputs, finite reserves, depletion, and processing are outside v1.
3. **Accounting depth:** count abstract integer quantities per `ItemDefinitionId`, stored as aggregate balances. Do not introduce physical units, lots, source provenance, or transformations.
4. **Title and custody:** the settlement/City owns the configured source and output; the City market is the custodian of the aggregate stock. Each selected City instance carries an authored stable `SettlementSemanticId`, unique across City instances in the supported world/profile and independent of `CityData.DefinitionId`, the legacy P8 anchor owner key, and `CityRuntime.RuntimeId`. Missing or duplicate identities reject profile composition. The profile records its association with the current P8 `LocationId`; runtime object/ID references are lookup handles only, and that LocationId must equal the selected City instance's composed P8 anchor. This v1 needs only that concrete settlement holder, not a universal holder model.
5. **Spatial scope:** anchor the source at the current P8 City Location. No Hex footprint, route, passage, or physical transport is included.
6. **Execution and cadence:** use the existing daily autonomous economy cadence, gated by `Economy.Enabled`, with production before consumption. This is not command-initiated production.
7. **Money:** use free population consumption. No money moves in the proposed flow; no payer, receiver, account, or monetary source/sink is introduced.
8. **Downstream promise:** this v1 makes no P15 construction-cost or P16 supply/logistics guarantee. Those consumers may use a promoted P14 capability only when their own scope requires it.
9. **Explicit exclusions:** actor-owned inventories, merchant transfers, physical resource extraction, production inputs, transformation, spoilage/loss accounting, and a general trade or supply network are outside this proposed closure.

## 5. Spatial and conditional dependencies

P8-A through P8-E are canonical at the current revalidation baseline. The proposed local City scenario uses P8-A geography and the P8-C City anchor binding to a Location. It does not consume P8-D or P8-E route/travel facts: no actor plans a route, traverses a passage, or carries material between locations.

If later scope adds physical movement, that work must use the promoted P8 travel authority required by the actual flow. Legacy fixed-day City routes are not a substitute for that authority.

P18 is conditional only if a later approved P14 consumer promises duration-based or intraday production; the existing bounded daily source/sink needs no P18 capability. P20 is conditional only for a later consumer that coordinates multiple participants; this single-City passive source/sink has no such dependency. Neither condition expands this entry proposal into a loader/API, physical resource gameplay, or broader economy system.

## 6. Reconstruction and fork-sensitive state

Per `EXECUTION_MODEL.md` and architecture §§91–92, authoritative state that affects a future boundary must remain reconstructible. The proposed scope adds no requirement for universal event sourcing or a save/replay implementation inside Phase 14. Its causal inventory must nevertheless be available to the persistence/reconstruction consumer:

- durable semantic identity for the source, settlement, market store, and item definition;
- opening market stock and the effective production/consumption configuration;
- each committed daily source addition and actual population-consumption quantity, with the logical day and execution order;
- settlement title and market custody; and
- effective calendar/configuration context needed to reproduce the daily cadence.

This is a reconstruction constraint, not a requirement to retain every operation as history. The specific P12/P13 storage and replay mechanism remains with those phases.

## 7. Technical questions after entry review

If independent entry review accepts this proposal, technical design should resolve:

1. Technical design should specify how the selected profile carries the already selected authored, stable per-instance `SettlementSemanticId` and its P8 `LocationId` association, validates that the `LocationId` resolves to the same City anchor composed by P8, then assigns durable source and market-store IDs without persisting the current runtime-keyed P8 owner lookup. Preserve the P12/P13 identity contract; do not use definition, display, list-position, or runtime identity as an instance ID.
2. How to apply production and consumption without partial mutation, including overflow, insufficient stock, and the existing mutation guards.
3. Which existing City market state is the authoritative owned store for this slice and how its title/custody projection composes with existing market counterparties and transaction paths.
4. Which source, stock, configuration, order, and result fields must appear in deterministic diagnostics/reconstruction projections so later save/fork work can preserve the causal state.

These are technical design questions within the proposed product boundary. This document creates no checkpoint IDs and grants no implementation approval.
