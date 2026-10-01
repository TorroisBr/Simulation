# P12-B per-City Market stock census — independent design review

**Verdict:** `PASS — proceed within accepted P12-B scope`.
**Canonical base:** `7be88ca7816b14c934729bffec56178ce3eb7e5a`.
**Reviewed design commit:** `bb3537c` (`docs/design/PHASE12_P12B_MARKET_STOCK_CENSUS_DESIGN.md`).
**Reviewer:** independent read-only review by `p12_next_gap_audit` on 2026-10-01.

The design adds one passive owner witness per selected-profile City and matches the existing P12-B owner-witness inventory. `CityRuntime.Market`, `MarketRuntime.Items`, and `MarketRuntime.Revision` support binding to the exact installed Market owner, reporting stock-row count, and observing that owner's local revision. The selected authored profile evidence supports two distinct owners at five rows each. `AddStock` covers both a successful same-row update and creation of a new row, advancing Market's local revision.

The per-City bootstrap provider pattern is appropriate for this bounded bootstrap composition. It does not resolve dynamic City composition or prove the complete effective-profile provider inventory. Reads are unsynchronized and provide no owner-thread or quiescence guarantee; the design correctly makes no capture/readiness claim.

Keep the limitations exact: this does not cover counterparty/account, Inventory, transactions, production/consumption, aggregate economy, mutation-epoch wiring, capture eligibility, export, hydration, or P12-A/P13 readiness. No new human checkpoint acceptance is required because this is a work package inside the already accepted P12-B scope. Proceed to isolated implementation and exact-tip candidate review; canonical promotion remains a separate human gate.
