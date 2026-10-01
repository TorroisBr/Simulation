# P12-B NPC trade owner-commit implementation review

**Verdict:** PASS — bounded implementation candidate; canonical promotion remains separate.

## Exact review identity

- Canonical base: `codex/phase12/canonical` at `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea`.
- Candidate branch: `codex/phase12/P12BPostMoneyAccountBlockerRefresh`.
- Exact reviewed candidate tip: `49b32c548e4b8c666657c031105401246cff2563`.
- Code-bearing commit: `5ab42a9880b866f9f8d94b9365b2c5fb51c15ff7` (tree `6f8bab112ef0f7c97bfde31aa3dbd637db4f8a33`).
- Reviewer: independent P12 operation/census reviewer `/root/p12_next_gap_audit`; the implementation author did not perform this review.
- Reviewed diff: five implementation/test files and five P12 documentation files, including the preserved blocker audit, design, and earlier design-review record. The unrelated ProjectSettings modifications and untracked `.meta` files are not part of the diff.

## Review findings

The first exact-tip review at `5d42cef6a9ceec4eb0af49689e1f67c07d571eaf` returned NEEDS_CHANGES because a failed bound-P12 precommit admission disabled census tracking but still allowed owner writes. Commit `5ab42a9` corrected this: when the bound runtime cannot establish exact participant identity, owner-thread admission, unchanged section baselines, or the named operation scope, `TryExecuteNpcTrade` returns `TransactionCommitFailed` before any trade owner commits. An unbound standalone service retains the existing transaction path.

After a domain owner commit, a census bookkeeping failure faults P12 admission closed without converting the transaction's established result into failure or implying rollback. The reviewer confirmed that the scope covers successful writes and both compensation paths, including individual notifications for every local owner revision change. The four section checks use the exact registered participant NPC, MoneyAccount and Inventory owner identities, section IDs and schemas, local revisions, and accepted baselines.

The implementation remains limited to the NPC trade's two rostered MoneyAccount and two rostered Inventory sections. MerchantSystem plan completion, direct owner entrypoints, other economy and selected-profile operations, complete owner census, capture eligibility, export, and hydration remain uncovered. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.

## Validation evidence review

The reviewer verified that the seven XML/log SHA-256 pairs in `PHASE12_P12B_NPC_TRADE_INVALIDATION_CANDIDATE.md` match the retained files under `Library/ValidationResults/P12NpcTrade/`. The XML reports show:

- `SimulationRuntimeAdmissionTests`: 15/15
- `NpcMoneyAccountCensusTests`: 10/10
- `NpcInventoryCensusTests`: 7/7
- `ContinuationCensusProtocolTests`: 22/22
- `EconomyTransactionTests`: 45/45
- ALL EditMode: 2125/2125
- Official EditMode Smoke: 5/5

`git diff --check` passed. The reviewer did not edit files or rerun tests.

The candidate record also explains the exploratory `195504...` run: it included an uncommitted assertion that same-NPC trade succeeds, while the pre-existing domain method correctly returns `SameAccount`; that temporary test is not in the reviewed candidate.

## Status

This is an independent implementation review only. It does not promote the candidate, complete P12-B, establish capture eligibility, authorize P12-A implementation, or close Phase 12.
