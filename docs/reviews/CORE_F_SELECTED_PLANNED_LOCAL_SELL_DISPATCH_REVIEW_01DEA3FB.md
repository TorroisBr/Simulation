# CORE-F Planned Local SellGoods Projection Recovery Review

## Verdict

VALIDATED_CANDIDATE

## Exact identities

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Reviewed tip: 01dea3fb0f500909416d200595a7dba7e6677a9b
- Reviewed candidate tree: bd759b239ac36259756dcfb89919915be3f58e56
- Reviewed base: ec659552f211390c2e227686d05c4af0bf4be574
- Implementation commit: 5d9ce19a7c4221dca385f2053f222dff5094e68a
- Implementation tree: 99a68f4f37bb9ae0b2159fe832bbfe98a50055e6
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Reviewer: independent reviewer agent planned_sell_exact_tip_review

## Reviewed scope and findings

The reviewer inspected the complete base-to-tip delta: the projection-recovery gate in SimulationRuntime.cs, its additive EditMode regression, the validation record, and the normalized result-only evidence package.

The gate is limited to an active-plan local SellGoods action with no NPC target. It requires the exact admitted action, current City, selected item binding, plan item and target City, and a positive amount within the plan remainder. Missing or ambiguous City projections reject before random draws or owner writes. Projection reconstruction does not publish a P12 mutation. Existing Core planned-sale execution continues to own eligibility, outcome, market transaction, plan mutation, and its P12 admission/commit callbacks.

The regression covers rejection before writes, projection recovery without owner revision or epoch changes, the deterministic draw identity, successful account/inventory/market/plan revisions, the four mutation epochs, and final census. The reviewer found no weakened existing assertions, unrelated candidate changes, or actionable defects.

## Evidence verification

- Exact source hashes match the candidate and its validation record.
- Focused test: 1/1 PASS.
- Full Unity EditMode: 3292/3292 PASS, including the new test.
- Official Smoke: 5/5 PASS.
- All reported runs had zero failures, skips, or inconclusive cases and shared a stable validation-input digest.
- git diff --check: PASS.
- Official evidence package verification: VALID, one run and one normalized XML entry; archive SHA-256 aa5750d3a718cc21ac72ead01a6909bcb9b3693a811ccea83dfb46b6032ff48d.
- Privacy scan found no absolute workstation paths or credential-like values in the public record/package.
- The reviewer independently checked the successful report and test identity against the package.

Three unsuccessful intermediate test attempts are accurately recorded as excluded from the passing gate counts and privately retained. They do not correspond to the final tested source.

## Limitations

This review validates only the bounded planned local-sale projection recovery. It does not complete CORE-F, authorize Core canonical promotion, or claim projectionless planned NPC sales, cross-City purchases, the shared application lifecycle, detached restoration, or the .NET host.