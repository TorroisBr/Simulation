# P12-B per-City Market stock census — bounded technical design

**Work package:** accepted P12-B owner-witness inventory; no new checkpoint ID.
**Canonical base:** `7be88ca7816b14c934729bffec56178ce3eb7e5a`.
**Status:** independent design review PASS; implementation may proceed under accepted P12-B scope. Code is not yet submitted.

## Evidence and gap

The accepted P12-B contract requires owner-bound section witnesses with stable section/schema identity, exact cardinality, and a revision. The latest canonical blocker map names the two selected-profile `MarketRuntime` owners as five stock rows apiece, with existing local revisions, but says no row-level census witness exists. `CityRuntime` constructs one Market per selected City; `SimulationBootstrapComposition` already publishes fixed per-City owner providers. The selected profile composes exactly two Cities. The operation-footprint audit at the canonical base prohibits treating this passive evidence as shared-epoch coverage or adding another runtime operation.

## Bounded contract

- Add a schema-v1 required witness for each selected-profile City, with stable ID `p12e.city-market-stock-rows/<length>:<CityRuntime.RuntimeId>` and Cities ordered by ordinal RuntimeId.
- Bind each provider to the exact `MarketRuntime` returned by that City's installed `Market` owner at composition. Report row cardinality from `MarketRuntime.Items.Count`, local revision from `MarketRuntime.Revision`, and owner identity as that exact Market object.
- Expose the fixed provider list on `SimulationBootstrapComposition`, created from `Runtime.Cities` using the established per-City provider pattern.
- Validate the selected profile's two distinct owner identities and its expected five rows per owner (ten total) before day one. A direct successful stock change must preserve owner identity and update the existing revision; a newly introduced item row must update both cardinality and revision.
- This is passive census evidence only. It does not modify Market write semantics, bind mutation guards, register providers or operations in `ContinuationCensusProtocol`, or assert shared-epoch invalidation, capture eligibility, complete owner coverage, or readiness.

## Exclusions and boundaries

The witness counts stock rows only. It does not census item amounts/prices as detached values, the separate `MarketCounterpartyRuntime`/account, MoneyAccount, NPC Inventory, City production/consumption, transaction/receipt state, or aggregate economy. It adds no export/hydration, no City/NPC composite revision, no runtime owner-window work, and no P12-A/P13 readiness. P12-B remains incomplete; P12-A stays `WAIT_DEPENDENCY`; P13 stays blocked.

## Implementation and validation if reviewed

Add one provider file and bootstrap composition property/wiring, plus focused tests in the selected-profile composition suite for section IDs/order/schema, exact installed owner identity, 5/5 cardinality, repeat stability, same-row revision invalidation, and new-row cardinality/revision change. Run the focused census and bootstrap composition suites, ALL EditMode, the official Smoke filter, and `git diff --check` on the exact code tree. No `SimulationRuntime`, `MarketRuntime`, `ContinuationCensusProtocol`, or mutation-epoch edit is required or in scope. Keep the candidate isolated on `codex/phase12/P12BMarketStockCensusDesign` and refresh it against canonical before integration if canonical advances.

