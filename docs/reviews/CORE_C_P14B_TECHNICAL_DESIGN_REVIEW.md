# CORE-C P14-B finite-source technical design review

**Disposition:** VALIDATED_CANDIDATE (PASS) for the exact documentation candidate below.
**Review date:** 2026-10-13.
**Reviewer:** Codex independent reviewer; review conducted separately from the design author.

## Reviewed refs and scope

- Candidate branch: `codex/core/CORE-C-technical-design`.
- Candidate commit: `e80ae27cdf597e9f551fe94dd22827f59636712a`.
- Candidate tree: `65fdf4455b391e32e200dd2df91758d905edd19c`.
- Candidate parent: `fb7b0344b9ed0f88a820aab594dc8a2472b5bf84`.
- Actual P14 base: `09fcbe46ffb5a7d80377185692d550310debc110`.
- Production/P12 baseline: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Architecture baseline: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- The remote candidate ref was refreshed and verified at the exact candidate commit. The corrected candidate changes only the CORE-C technical-design document relative to its prior reviewed draft; no production implementation is included.

## Findings

**Single price authority and numeric behavior — PASS.** The design names `MarketPriceCalculator.CalculateCurrentPrice` as the sole pure-Core implementation and requires the existing Unity `MarketItemRuntime.CalculatePrice` to delegate to it. Existing Market, transaction-adjacent, and P14-C callers remain on the same Unity Market authority and consequently the delegated formula. The design preserves the source expression's binary32 order, integer `Mathf.Max` overload for the stock floor, float division, `Mathf.Clamp` comparison behavior, and NaN/infinity/signed-zero cases. It provides exact-bit vectors and makes captured Unity 6000.3.9f1 baseline parity a pre-implementation gate; .NET must match the measured Unity bits. It expressly forbids a duplicate finite-kernel formula or unrelated input validation.

**P12 callback count and sequence — PASS after the bounded correction.** At P14 source `09fcbe46`, the direct positive operation has three admission calls: `TryPrepareFiniteStockIncrease`, service preflight `CanInstallFiniteStock`, then `FiniteSourceProductionPreparation.Install()`'s `CanInstall()` recheck. It sends one notification after the source and Market installs. The prepared City route adds exactly two calls: `CityRuntime.TryCommitDailyEconomy` validates with `FiniteSource.CanInstall()`, then `FiniteSource.Install()` rechecks through `CanInstall()`. Therefore the City prepared route has four admission calls and one post-install notification, not five. The candidate now describes this exact path and correctly notes that the outer Market revision check does not invoke the admission callback. The proposed tests record order/count, fail each position, assert no partial mutation, cover callback exceptions, and assert one notification after successful installation.

**Core API, Unity adapter, and atomic install boundary — PASS.** The document defines public immutable value request/result/row DTOs and a public kernel entry point suitable for the separate Unity/Core assemblies. It keeps `FiniteProductionSourceStore` and `MarketRuntime` as the only live owners. Unity snapshots rows in original order with retained object references, distinguishes null rows and null-item rows, preserves duplicate IDs for exact-cardinality rejection, maps effective row item/base-price values, and reconstructs `PreparedMarketState` without changing non-target values or the target `ItemData` reference/desired amount. It requires a hard stop if one-to-one representability cannot be preserved. Existing revision checks, P12 admission, two-owner install ordering, and post-commit notification remain in the Unity coordinator; Core returns only detached prepared values.

**P14-C compatibility and scope — PASS.** P14-C remains its already-promoted Unity mixed-source consumer. The design preserves its contribution calculation, source ordering, exogenous behavior, replay/receipt behavior, one aggregate replacement, revision-increment count semantics, and commit boundary; it does not claim P14-C calls the CORE-C kernel. It requires the full P14-C suite and affected Market, transaction, continuation, and admission regressions. The design remains a P14-B finite-source extraction and does not widen the P14 profile or move P14-C, P12 admission, persistence, gameplay, or other consumers.

**Dependencies and readiness — PASS with dependency retained.** CORE-A remains an explicit architecture prerequisite for CORE-C even though this operation accepts an already-resolved `long absoluteDay`; the design correctly does not self-remove that DAG edge. CORE-B is not required for this operation, and CORE-D remains downstream. P14-B is promoted and P14-C is a regression boundary. The candidate clearly marks implementation as `WAIT_DEPENDENCY` on CORE-A and says design PASS does not authorize implementation or promotion.

## Review limits and next gate

This is an exact-tip technical-design review only. No implementation or Unity/.NET tests were run, and no implementation validation is claimed. The candidate is suitable to be recorded as a reviewed design. Before implementation, CORE-A must satisfy the documented dependency and the implementation workflow must capture the Unity numeric baseline, migrate the explicit source/test closure, run the required Core and Unity validation, and obtain a separate exact-tip implementation review. This review does not promote CORE-C or close any Phase.
