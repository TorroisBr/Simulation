# CORE-F Owner Consumer Boundary — exact-tip independent review

**Verdict: NEEDS_CHANGES.** The exact-tip code review passes with no remaining actionable code finding. The candidate is not a `VALIDATED_CANDIDATE` because required Unity regression and full validation evidence is absent; do not promote this candidate on the present record.

## Reviewed identities

- Repository origin: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-owner-consumer-boundary-20261017`.
- Candidate commit: `71122ee7f08b413cace9831dce12615299513aaf`.
- Candidate tree: `85199fcff4ae99fb831692538b983923aed66874`.
- Candidate parent: `75a3215649c33a166f288c56baa2ed2ba3d62f1c`.
- Review base: `b12aeb21564c88e16fca12b3c85ee3bfd5058e94`.
- Requested current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Remote preflight: `origin` identifies the stated repository; `ls-remote` returned the exact candidate branch SHA above and the exact Core canonical SHA above. The candidate contains the current canonical as an ancestor. The candidate branch was clean for tracked source changes; pre-existing unrelated ProjectSettings modifications and untracked .NET build outputs were left untouched.
- Review scope: full `b12aeb2..71122ee` candidate diff (nine files), including selected typed Inventory/Market owners, Unity City/Market adapters, P12 mutation admission/notification, P14-C mixed-source handling, Core and Unity tests, and candidate progress record. `git diff --check` passes.

## Code review

The selected `DailyV1MarketOwner` maintains row definitions as exact admitted graph objects. Prepared-state creation rejects invalid or duplicate rows by reference identity; lookup and stock adjustment use the exact `DailyV1ItemDefinition` reference. The selected mixed-source City path resolves the graph item, requires exactly one matching market row, and derives production and market values from the admitted effective definition. This preserves item identity and bounded cardinality across preparation and install.

The selected P14-C path sorts contributors by ordinal `productionSourceId`, applies each accepted contribution to one detached typed Market state, and records per-source outcomes. It installs one prepared Market replacement with the revision increments for applied contributors and publishes one post-install mutation notification. The finite reserve changes only when its contribution was actually applied. P14-C’s two-source profile remains the bounded proving composition; this review does not treat that fixture cardinality as a universal source limit.

The two findings from the preceding review are resolved:

1. **Zero-increment selected receipt:** `CityRuntime.TryCommitDailyEconomy` skips Market owner installation and notification when `MarketIncrements == 0`, while still committing the receipt. The focused Unity regression checks that the Market revision and exact row instance remain unchanged for no-op PriceRefresh and Consumption.
2. **Direct selected Market mutation boundary:** the prepare and commit paths both check P12 material-flow admission. A successful install sends one notification after the Market state and revision are visible; rejected admission leaves the owner and receipt unchanged. The targeted regression covers both preflight denial and admission changing between prepare and commit.

The additional saturated finite-revision finding from `75a3215` is resolved at this tip. `P14CMixedSourceProductionPreparation.CanInstall()` now checks finite-owner revision/day installability only when `finiteAppliedQuantity > 0`; a rejected or absent finite contribution therefore cannot block another accepted contributor. The new `SelectedMixedOverflowCommitsExogenousContributionWhenFiniteOwnerCannotAdvance` case sets the finite revision to `long.MaxValue`, admits the exogenous stock increase, rejects the finite quantity for aggregate overflow, and asserts unchanged finite reserve/revision plus one post-install Market callback. This matches the existing overflow contract: a source-specific rejected contribution does not debit its reserve or erase a distinct accepted contribution.

No additional source defect was found in the reviewed scope. The direct and prepared paths preserve stale-revision checks and avoid installing partially prepared state. P14 mixed and finite mutation ordering remains bounded by their existing prepare/preflight/install sequence; no API or gameplay semantics were added by this review.

## Validation evidence and gap

The candidate’s `docs/validation/CORE_F_OwnerConsumerBoundary/PROGRESS.md` reports focused `DailyV1InventoryMarketOwnerTests` 7/7 PASS and `git diff --check` PASS. The recorded SHA-256 values for the affected Core source and test files match the exact candidate files. The progress record says the official CoreValidation wrapper was blocked before test execution by SDK diagnostic access, so no complete CoreValidation result is claimed. No candidate-specific raw Core test report/log package was present in the validation directory for independent result verification.

The new and prior Unity regressions—including the saturated-revision case—were **not run**. The approved Unity evidence wrapper was blocked before Unity launch because the required evidence-directory ACL could not be established. The candidate has no Unity result XML/log package. Therefore this review does not verify the Unity behavior by execution and does not claim focused Unity tests, ALL EditMode, official Smoke, or full integration validation passed.

No tests were launched during this review. The candidate source hashes in the progress manifest match the reviewed files, and no code changed after the reviewed tip. The candidate still requires the approved Unity focused and regression validation, plus the repository-required full gates and reviewable provenance, before it can be promoted. The ACL restriction must be resolved through an authorized environment; it is not a reason to weaken or bypass the evidence contract.

## Scope and status boundary

This review is limited to the bounded owner-consumer source slice. It does not complete CORE-F, establish a detached host, grant P12 readiness, or close a phase. It does not approve canonical promotion. `NEEDS_CHANGES` here means validation evidence is incomplete; no code correction remains identified at `71122ee`.
