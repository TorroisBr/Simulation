# CORE-C current-baseline applicability revalidation

**Disposition:** PASS for technical-contract applicability against the current Core and P14 baselines. This is a read-only design revalidation, not implementation review, validation, authorization, canonical promotion, or Phase closure.

**Review date:** 2026-10-14.
**Reviewer:** Codex independent reviewer, separate from the design author; no candidate files were changed.

## Exact reviewed refs

- Reviewed design branch: `codex/core/CORE-C-technical-design`.
- Design candidate: `e80ae27cdf597e9f551fe94dd22827f59636712a`.
- Design tree: `65fdf4455b391e32e200dd2df91758d905edd19c`.
- Exact-tip design PASS record: `cb98d3e99c9491ce2c34278516272763333b271e`, on `codex/core/CORE-C-design-review-e80`.
- Earlier record `93d3bf6d1043227805459817b9557822c36cfe6d` reviews draft `30e260d7be19f1daa288ec3a5672b0957dabc874`; it is not the exact-tip review record for e80. Do not substitute it for cb98d3e.
- Current Core canonical: `codex/core/canonical` at `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`.
- Current P12 canonical: `codex/phase12/canonical` at `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Current P14 canonical: `codex/phase14/canonical` at `09fcbe46ffb5a7d80377185692d550310debc110`.
- Architecture extraction baseline: `codex/architecture/simulation-core-extraction` at `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- Repository: `https://github.com/TorroisBr/Simulation`.

The remote refs were checked during this review and matched the SHAs above. The exact e80 candidate is based on P14 canonical 09fcbe4. Core canonical c957528 is based on P12 canonical 4cc3107.

## Current-baseline findings

**CORE-A compatibility — PASS.** CORE-A provides the shared `Simulation.Core` package/assembly, an engine-free Unity assembly boundary, and explicit SDK compile lists. Its additions do not change the P12 `MarketRuntime` finite-stock admission, prepared-install, revision, or post-commit notification hooks that CORE-C retains in Unity. The P12 `FiniteSourceProductionService.cs` source blob is present unchanged in current Core canonical. The proposed public immutable request/result/row values and kernel are suitable for the separate no-engine assembly. Implementation must add the new Core source and linked tests to the explicit SDK project compile lists; this is ordinary integration work already anticipated by the design.

**P14-B source behavior — PASS.** The current P14 finite-source path still uses the reviewed one-source prepared reserve-debit/Market-stock-credit operation, stable source/settlement/store/item identities, local source and Market revisions, the strictly newer absolute-day boundary, exact matching-row cardinality, checked stock addition, and an atomic two-owner prepared install. The reviewed callback sequence remains the contract: three admission checks and one post-install notification for direct positive production; four checks and one notification for the prepared City route. The design preserves this behavior in Unity while moving only the value-based operation into Core.

**P14-C and exclusions — PASS.** P14 canonical adds the promoted mixed-source consumer in `CityRuntime`/`MarketRuntime` and `P14CMixedSourceProductionPreparation`. The design explicitly leaves P14-C's source ordering, contribution calculation, receipts, aggregate prepared replacement, revision-increment count, and commit path in Unity, requires its full suite as a regression, and does not claim that P14-C calls the new kernel. It retains P14-A behavior, P10/P14 profile separation, and the P12 Daily-v1 early rejection. It adds no save/load, P12 capture eligibility, export/hydration, transport, trade, money, or broader profile claims.

P14 canonical is not an ancestor of P12 canonical; their merge base is `405f70e58a7a1dd8be255798b795faff095f44b4`. This is a branch-lineage fact, not a design conflict: the design candidate is based on the exact current P14 tip, and the relevant P12 hook baseline is explicitly identified. Any implementation must compose the P14 deltas onto current Core canonical and review the resulting integration tree; it must not assume the P14 tip can be fast-forwarded onto Core.

## Readiness and limits

The documented CORE-A → CORE-C dependency is satisfied by promoted Core canonical c957528. P14-B is promoted on current P14 canonical. The exact e80 technical-design review passed. CORE-B is explicitly not a dependency for this operation; CORE-C can be scheduled after CORE-B to serialize the shared Core package, test-project, Market and City integration hotspots, but it is not technically blocked on CORE-B.

The design and `docs/CORE_STATE.md` retain stale readiness wording. The e80 design still says independent review is pending/not completed and labels implementation `WAIT_DEPENDENCY` on CORE-A; Core State still says CORE-A is the first checkpoint and CORE-C awaits design corrections/review. Those statements are superseded by the exact e80 PASS record and current promoted Core canonical. Reconcile that wording before formally marking CORE-C `READY_FOR_IMPLEMENTATION`. This revalidation does not itself provide implementation authorization or change the design's stated separation between design review and implementation gates.

No tests were run for this read-only applicability review. The implementation must follow the design's Unity numeric-baseline and bit-parity gate, linked .NET build/test workflow, Unity focused and regression suites (including P14-C and Market/transaction callers), ALL EditMode, official Smoke, `git diff --check`, and any applicable daily-loop regression. No implementation or promotion evidence is claimed here.
