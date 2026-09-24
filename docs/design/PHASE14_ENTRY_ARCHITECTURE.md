# Phase 14 Entry Architecture Decision Packet

**Purpose:** support the Phase 14 entry decision for a limited productive-source/material-flow slice. This is an evidence packet and option map, not an implementation contract, a checkpoint plan, or an approval to implement.

**Observed base:** `codex/phase14/MaterialFlowEntryArchitecture`, clean at `1f4651e99db2c357dd3be3c6b9284d104379f706` (`codex/phase7/canonical` baseline). The Phase 14 Brief is `ENTRY_ARCHITECTURE_READY`; no Phase 14 checkpoint IDs are approved. This document intentionally chooses neither the first consumer nor the closure boundary.

## 1. Governing constraints

The Phase 14 Brief asks for a bounded factual account of productive sources and material movement with explicit ownership/custody and source/sink semantics. Before technical design, entry approval must decide the first concrete consumer, accounting/conservation semantics, and actor/asset authority. If a choice changes v1 scope or material constraints, it is a product decision for the user.

Relevant canonical rules in `docs/SIMULATION_ARCHITECTURE.md`:

- §41: ownership and inventory/custody answer different questions.
- §§42–43: ownership, jurisdiction, control, and allegiance differ; future titleholders may include Persons, institutions, organizations, or polities, but a universal holder model and co-ownership are not already decided.
- §45: significant money flows must identify payer, receiver, limiting balance, and any deliberate source or sink.
- §§69 and 91–92: spatial identities/anchors and replay/fork reconstruction are authoritative concerns; diagnostics and history are not primary truth, and the project is not event-sourced by default.
- §13: economy, merchants, merchant autonomy, effective commercial configuration, and merchant knowledge are separate concerns. Existing autonomy/configuration rules do not imply a universal trade network.

`docs/EXECUTION_MODEL.md` separates entry architecture from technical design and implementation. A proposed plan may choose interfaces and migration only after semantic and product questions are closed. No Phase 14 State or Phase 14 checkpoint contract exists in this base. The docs inventory has no separate ADR directory; the cited architecture, Brief, Roadmap, Phase 7 State, and Phase 8 Brief are the relevant current sources.

## 2. Current repository facts

| Area | Implemented behavior at the observed base | Boundary relevant to Phase 14 |
|---|---|---|
| City production and consumption | `CityData` authors initial/desired market quantities, a per-1,000-population consumption rate, and fixed `amountPerDay` production configs. `CityRuntime.SimulateProductionDay()` adds configured item counts to its market; `SimulateConsumptionDay()` removes available counts. `SimulationRuntime` runs production for all cities, then consumption and price updates for all cities when Economy is enabled. | This is a concrete existing local source/sink-like consumer, but production is a direct configured addition: it consumes no input, identifies no productive source/site, records no resource depletion, and does not establish ownership of an underlying resource or producer. Population consumption removes stock; its result reports settlement/population/item quantities and payment data, not a general material movement identity. |
| City market stock | `MarketRuntime` stores item-definition references, integer amount, desired amount, and a derived price. A city market's `StockOwnerRuntimeId` projects its counterparty ID, normally the city runtime ID. The market is one aggregate stock container per item. | The current market's “stock owner” is a runtime/counterparty association; it is not a separate title registry plus custodian/location. The stock has no source identity, lot provenance, physical units, or multiple owners. |
| Person/NPC inventory and trade | `InventoryRuntime` stores integer item quantity and average unit cost inside `NpcRuntime`. `EconomyTransactionService` transfers stock for NPC-to-NPC trade and market purchase/sale, and receipts identify item source/destination runtime IDs. It prevalidates several money/stock/capacity cases. `MerchantSystem` chooses and executes opportunities using current market observations and `TravelSystem`. | This supplies a real trade/material-transfer consumer and existing transactional precedent. Inventory is attached to an execution runtime; transaction receipt IDs are runtime IDs, and receipts do not establish stable asset title or an independent carrier/custodian relation. A successful merchant purchase/sale does not define how a productive source owns its output. |
| Money accounting | `MoneyEffect` distinguishes `Transfer`, `ExplicitSource`, and `ExplicitSink`. Account-backed city market trades move funds between NPC and settlement accounts; open-market purchase/sale paths deliberately report a money sink/source. Travel can charge the NPC. | Explicit monetary effects are a useful precedent for deliberate boundaries. They do not account for material creation, consumption, loss, transformation, or title. P14 must not silently treat an open market or a material source as infinite. |
| Property and estates | `PropertyOwnershipStore` registers `PropertyId → PersonId`; explicit transfer is a domain transition with stale-state/revision checks and history. It is composed in `SimulationRuntime`. Architecture calls for eventual non-Person titleholders but does not settle that model. | This is a durable title precedent, not a general stock or asset system. It does not currently bind a property to material quantity, custodian, source output, or `LocationId`. Reusing it for goods would conflate different facts unless a separate contract is approved. |
| Spatial authority and travel | P7 D0 delivered typed `HexId`/`LocationId`, `LocationRecord → AnchorHexId`, and a world-owned `SpatialAuthorityStore`. P7 State explicitly leaves City/Site mapping and route/travel rewrite deferred. Current `CityRuntime` and `TravelSystem` use legacy `SpatialLocationRuntime`, direct `SpatialRouteRuntime`, and fixed `TravelDays`. | `PHASE8_BRIEF.md` is `READY_FOR_TECHNICAL_DESIGN` and states that no Phase 8 implementation has started. Its P8-C is the City/Site anchor and civil-presence bridge; relevant P8 passage/travel behavior is required for route-dependent P14 execution. The P14 Brief does not require every P8-E behavior for a local source. Legacy fixed-day routes are not a second authority for a new route-dependent material flow. |
| Daily execution | `SimulationRuntime.AdvanceDay()` calls `SimulateEconomyDay()` before merchant/NPC action processing; travel advances later in the boundary. | A scheduled production/consumption choice will touch the daily-loop/economy composition hotspot. A command-driven or route-integrated choice has different integration and replay inputs; this is unresolved until the consumer and cadence are selected. |

Concrete test evidence already present (read only; no tests were run for this packet): `SettlementStockOwnershipTests` covers market/counterparty ownership projection, production, account-backed receipts, explicit open-market source/sink money effects, consumption, overflow, and failed no-transfer behavior. `EconomyTransactionTests` covers money and item transfer paths. `MerchantLiquidityTests` covers trade execution under counterparty liquidity. `PropertyTransferFoundationTests` covers explicit, stale-safe, PersonId-based title transfer. These prove current behavior only; they do not settle Phase 14 semantics.

## 3. Candidate first consumers (no selection)

1. **Existing City production → settlement market stock → local population consumption.** Smallest existing end-to-end local loop. It can exercise configured local output and an explicit sink without requiring route-based movement. To count as a productive-source slice, entry must decide whether the source is merely an authored City output, a distinct anchored source with finite/replenishing stock, or a producer/process with inputs. Existing City production alone does not make that distinction.
2. **A localized resource source feeding a settlement or other stock holder.** Examples could be an authored mine, forest, farm, or other site, but none is represented by a general productive-source runtime in the inspected code. This creates the clearest source identity/location/ownership question. Its implemented anchor requires the relevant promoted spatial anchor/City-Site bridge.
3. **Merchant or other actor material transfer between existing inventories/markets.** This uses existing transaction and trade consumers, but adds title/custody questions and route dependence if goods physically accompany a traveler. Route-based implementation waits for the relevant promoted P8 passage/travel authority; the local trade contract can be considered separately from physical transport.
4. **Material consumption/cost by a later domain, especially P15 construction or P16 supply.** These are named downstream users, not implemented consumers in this base. P15's Brief requires only the applicable promoted P14 capability, not all of P14. Choosing a downstream cost as the first P14 consumer would expand the entry boundary and must be explicit.

These choices are not equivalent. A local production/consumption loop can close without transport; merchant transport can close without productive-source depletion; construction costs require a caller that is not present here. The user must select which causal behavior Phase 14 is intended to make factual first.

## 4. Conservation and accounting choices

Any selected boundary should keep these statements distinct:

- **Transfer:** moving quantity between authoritative stores preserves the total for the same material definition, except for an explicit loss. Changing title, custodian, or location alone does not create or destroy quantity.
- **Source:** initial authored stock, production, harvest/extraction, import, or another inflow needs an identified and deliberate source boundary. A configured daily increment is an explicit quantity source even if its physical basis is intentionally outside v1.
- **Sink:** consumption, destruction, spoilage, export, or another outflow needs a deliberate sink/recipient boundary. A physical consumer may need an explicit receiving domain rather than silent stock deletion.
- **Transformation:** inputs and outputs of different item definitions require a stated conversion/yield rule and treatment of byproducts/loss. Per-item count conservation alone cannot establish physical conservation across unlike goods.
- **Money:** keep payer/receiver/account constraints and deliberate money source/sink separate from item quantity and title.

Possible v1 accounting depths, with consequences:

| Option | Consequence |
|---|---|
| Count each item definition in a set of owned stores; permit transfer plus named source/sink boundaries. | Supports local quantities and transfer conservation with little ontology. Does not make unlike goods physically comparable and leaves processing recipes outside scope. |
| Add explicit source records and source-specific depletion/replenishment with each output transition. | Makes the productive origin and finite versus renewable supply factual. Requires a decision about source identity, availability, replenishment, and output ownership. |
| Include transformations, inputs, outputs, byproducts, and loss in the selected slice. | Supports processing chains and more downstream consumers, but needs definitions for units, recipes, yield/rounding, invalid/stale proposals, and atomic multi-store updates. It is broader than simple item-count transfer. |

A per-item accounting statement could be checked as: `closing quantity = opening quantity + deliberate additions + transfers in - transfers out - deliberate sinks - transformation inputs + transformation outputs`, scoped to the selected authoritative stores and material definitions. This is an accounting prompt, not an approved formula or v1 contract. Whether quantities mean abstract item counts or measured physical units remains open because `ItemData` currently defines names/prices/capability modifiers, not units, mass, volume, or conversion rules.

The selected mutation boundary should decide which inputs are revalidated at apply time and whether multi-store output is atomic. Existing transaction tests establish a pattern for rejecting invalid/stale or capacity-limited flows without partial material movement, but a P14 source/production transaction contract has not been approved.

## 5. Actor, asset, ownership, and custody options

The architecture already requires `ownership != inventory/custody`, but the current stores do not express that distinction for generic materials. The entry choice affects transfer rules, identity, population scale, and downstream construction/logistics:

| Candidate authority shape | Consequences and limits |
|---|---|
| **Container/settlement-owned aggregate stock**, with a city/market as owner and store. | Closest to current city-market behavior and compact for aggregate trade/consumption. It keeps stocks usable without individual producers, but ownership and custody may remain collapsed; current `CityRuntimeId` is not a durable generic asset-holder contract. |
| **Person/actor-owned inventory**, with custody/location tracked separately. | Fits individual merchants and lets an actor retain title while goods travel or are stored elsewhere. Person-backed cases can use persistent identity rather than depending on materialized `NpcRuntime`; aggregate population and non-Person holders need separate answers. A PersonId-only answer would constrain source or institutional ownership. |
| **Source/establishment/settlement/institution-owned output**, held in a separately identified stock container. | Separates productive rights/output title from producer, carrier, or store. It fits institutional/aggregate producers but requires choosing which holder kinds are in v1 and their stable identities. Architecture §43 leaves general non-Person titleholders open and warns against a universal holder model without a consumer. |
| **Owned lots/batches versus fungible per-definition balances.** | Lots can preserve source/provenance, separate title and custody, and support traceable transformations; they add stable identity and replay state. Aggregate balances are smaller and simpler but cannot answer which source/owner a particular portion came from after blending. Neither form is prescribed by the current item catalog. |

Entry must settle at least: (a) who owns the source itself; (b) who owns output on creation; (c) whether output owner is distinct from worker/producer; (d) who has custody and where stock is held while carried/stored; (e) what transfer changes title versus only custody/location; and (f) whether populations or institutions can produce/own without materializing a Person or NpcRuntime. These are domain choices. A technical design can then map them onto stores and transition boundaries.

## 6. Spatial and travel dependencies

- A source described as localized must reference the approved physical location/anchor authority rather than inventing a City-only or renderer coordinate. P7 provides typed spatial records, but its closure record says legacy City/Site anchors have not yet been migrated. Therefore a new source's *implementation* requires whichever relevant P8 anchor capability is promoted; the existing `SpatialLocationRuntime` bridge is not evidence that P8-C is complete.
- A source that is only a local City/Location-bound stock source need not assume the whole civil travel vertical slice. Do not add a multi-Hex footprint, terrain extraction, or route model without a selected consumer that needs it. Section 69 identifies `Location → AnchorHexId`; the exact source geometry/footprint remains consumer-specific.
- If a flow physically travels between locations, the flow must join the same spatial/travel authority used by that trip. P8's approved direction is factual passage/position plus Knowledge-bounded civil travel; the current legacy `SpatialRouteRuntime.TravelDays` path is a transitional adapter, not an alternate P14 route authority. P14 can design this dependency, but route-dependent implementation waits for the needed promoted P8 capability.
- A sale at a shared local market, an ownership transfer, and physical carriage are separate candidate facts. Entry must say which ones v1 includes. No route requirement follows merely because merchant trade exists in code.

## 7. Reconstruction and fork-sensitive state

Per `EXECUTION_MODEL.md` and architecture §§91–92, authoritative causal facts must be recoverable at every actually simulated boundary; this does not require universal event sourcing or implementing replay as part of P14. Depending on the selected slice, the causal inventory must account for:

- stable identities for productive sources, holders/actors, stock containers, material definitions, and locations; never rely on Unity discovery order or an ephemeral runtime ID where a persistent relation is required;
- opening/initial stock, quantities, source state/depletion, owner and custodian, and the factual relationship between source and output;
- each authoritative production, replenishment, transfer, consumption, transformation, loss, and material-cost result, including the inputs, authority, logical boundary/order, and explicit source/sink or recipient;
- effective material/production parameters, compatible content definitions, calendar/day, deterministic ordering, and causal RNG context if the chosen behavior is randomized;
- external command payload/actor/authority/application boundary when a command initiates a source or movement; and
- if movement is in scope, route/travel commitments and progress, passage truth needed for execution, and any actor Knowledge that affects a route decision.

A snapshot of current balances alone may fail the Phase 13 fork guarantee if prior authoritative causality is required to reconstruct historical boundaries. Conversely, a selective transaction/history record is not a substitute for primary factual stock/ownership/source state. The precise P12/P13 storage/replay mechanism is outside this entry packet.

## 8. Explicit decisions still required

Before technical design can be bounded, the entry reviewer/user must decide:

1. **First consumer:** which one of the local City loop, a distinct localized source, merchant/material movement, or an explicit downstream construction/supply use is the v1 proving case? Name the concrete scenario and the material(s) that make it meaningful.
2. **Closure boundary:** does v1 end at source registration, source output into a stock, conservation-safe local consumption, ownership/custody transfer, physically routed delivery, or a combination? State what is deliberately outside closure.
3. **Productive-source meaning:** are v1 sources configured producers, finite extractable stocks, replenishing sources, or transformations from inputs? Which need is causal/product-relevant now?
4. **Material accounting depth:** abstract item counts or physical units; aggregate balances or provenance-bearing lots; and whether transformations, byproducts, loss, storage limits, spoilage, and/or prices are inside scope.
5. **Owner and custodian authority:** which holder kinds can own a source and its output in v1; whether Person/aggregate/institution holders are included; whether the owner may differ from producer, market, store, or carrier; and which operations change title versus custody/location.
6. **Money relationship:** whether a selected flow requires a paid exchange, existing explicit source/sink behavior, or no monetary mutation. If money moves, identify the payer, receiver, balance constraint, and any deliberate source/sink per §45.
7. **Spatial scope:** whether the selected source needs a Location anchor only, a Hex-bound source or footprint, or route travel; for travel, which promoted P8 capability the design consumes. This must not be treated as satisfied by unpromoted candidate code.
8. **Execution origin/cadence:** whether output/transfer is a scheduled autonomous domain behavior or an explicit validated command, and which actor may propose it. Any autonomous decision may use Knowledge, but execution must revalidate current World Truth.
9. **Downstream promise:** exactly which P15 material-cost or P16 supply behavior, if any, Phase 14 closure will unlock. A general economy or trade-network promise is explicitly excluded by the Brief.

Until those answers are made, only this option/evidence packet is ready. Technical design and implementation should not infer a selected consumer or closure boundary from existing tests, asset names, or the roadmap's downstream edges.
