# Independent review — CORE-F Runtime Owner Section Contracts fix

**Verdict:** `VALIDATED_CANDIDATE` for the exact implementation and test diff; not promotion-ready.  
**Candidate tip reviewed:** `7edb0d1118d336fe7f0fd81259c98e03a47f38c2`  
**Implementation commit/tree:** `59a03927d61afbf30c138db0201c59afc3c86ab7` / `3e1549c50be2b7fa919efa56770d5111c5f1633e`  
**Base:** `3faaa8ab66e7c257fa2cccf7777ca087be9adfa0`  
**Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This review supersedes the prior `NEEDS_CHANGES` record only for the two findings listed there. That historical record remains preserved unchanged.

## Review result

The reviewed change binds replacement of the sealed owner-section inventory to the exact active `runtime.npc-membership` operation. An unrelated active MarketSale operation is rejected without changing the catalogue revision; the membership operation succeeds and advances it once. Generic runtime-operation teardown remains unchanged.

The added shared-protocol regression performs an NPC roster addition and PersonStore binding, compares the Core-sorted catalogue with its provider-facing projection, and checks the expected catalogue cardinality, inventory revision, mutation epoch, captured owner rows, and Person membership/binding cardinality and revisions. The test uses the shared Core execution state and real owners. It exercises the census boundary directly; it does not claim to exercise `SimulationRuntime.TryMaterializePerson` end to end. Existing assertions were not weakened.

The independent reviewer found no additional defect in operation gating, transition ordering, sorted projections, schema/role/cardinality validation, revision/epoch effects, deterministic behavior, or standalone protocol behavior. `git diff --check` passed.

## Evidence reviewed

- The official CoreValidation run is recorded as 888/888 cold and warm in run `ceb1370d271a4459b481afdc45770171`.
- The official full Unity EditMode package verifies successfully and records 3,272/3,272, with no failures, skips, or inconclusive results. Its manifest SHA-256 is `1A8C164B1CCDAF4C9D97F4FAA74C7939E56DF2D89728E74A6CFF33D6344BFC5`; archive SHA-256 is `42AC8D657AE0FC5F0CE92B0F6AC9F8F67573FEA15448EF39DF622E614EF6CBDB`.
- The separate official filtered Smoke run is recorded as 5/5, but its original and private run manifest are not in a durable package. The EditMode archive is not substituted for that invocation.
- CoreValidation originals and the private manifest remain protected locally. The independent reviewer could not verify them from a compliant durable package; the EditMode package does not cover that gap.

## TRX adapter clarification and promotion limit

The earlier validation note said the approved adapter did not accept the CoreValidation manifest schema. That description was inaccurate: the adapter recognizes `simulation-core-validation/v1`. The concrete mismatch is its closed artifact profile: the adapter requires exactly two report artifacts and exactly eleven command-log artifacts, while this preserved run manifest has two reports and fifteen logs. No report, manifest, or log was edited, filtered, or repackaged, and no out-of-profile package was created.

The code/test review therefore passes, but the required durable evidence set is incomplete. This candidate is not promotion-ready. Resolving the packaging mismatch requires an authorized, independently reviewed extension of the evidence profile or a qualifying run produced by an already approved workflow. No Core canonical ref or CORE-F completion status changed.