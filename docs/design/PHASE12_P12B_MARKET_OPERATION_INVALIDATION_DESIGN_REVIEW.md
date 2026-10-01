# P12-B selected Market operation/invalidation design review

**Result:** PASS — bounded design contract is ready for implementation within the previously accepted P12-B prerequisite scope.

- Canonical base reviewed: `b8a7da54864bee3fb9b8916793240e91fbce0955`.
- Exact design tip reviewed: `9a71eb65b00d7425172225d0812de5f5cfb2c121` on `codex/phase12/P12BMarketOperationInvalidation`.
- Independent reviewer: `/root/p12_witness_docs_review`.
- Review method: read-only comparison of the exact candidate against canonical source, owner/operation contracts, selected-profile composition, and the prior review findings.

The review initially returned NEEDS_CHANGES for two concrete gaps: `MarketRuntime.UpdatePrices` could mutate price fields at an exhausted local revision without notifying the shared epoch, and the design did not state how the Market wrappers obtain the shared P12-bound transaction service. The revised design requires a price-change/revision-capacity preflight and a no-write result when revision is exhausted; it also binds the exact transaction-service instance used by `MerchantSystem` to all registered City Markets before bootstrap publication. Focused tests are required for both boundaries.

The reviewer confirmed the selected-profile owner set and Open purchase/sale commit/compensation ordering against source. The two existing City Market census providers are registered as required sections; account-backed City/Counterparty paths remain excluded; Merchant plan completion and other operation families remain uncovered. This design does not establish complete owner coverage, complete shared-epoch coverage, capture eligibility, export/hydration, P12-A readiness, P13 readiness, or Phase 12 closure.

`git diff --check` passed. No Unity validation applies to this documentation-only review. Canonical promotion remains a separate human gate.
