# P12-B per-NPC MoneyAccount census — implementation review

**Result:** PASS.
**Reviewed candidate evidence tip:** `46775bbcc5a6b3baa852cb4317b43bd695052a15`.
**Exact code-bearing tip:** `2bdd0990acc2bdc2d6073b0863fc1ae94a209c4d` (tree `58f0e76525a0ee6dc9b7f73f34dcbf71db944a09`).
**Base:** canonical `43dba1b5d16cba1558c4c39239f3cd0d4c669958`.
**Candidate branch:** `codex/phase12/P12BNpcMoneyAccountCensusImplementation`.

## Review findings

The exact code tip implements one passive section per NPC, bound to the exact installed `MoneyAccountRuntime`, with cardinality one and the owner's existing local revision. Missing/duplicate/aliased owners and in-place account replacement fail closed. The family follows NPC roster membership; retained providers remain stable and separate same-ID re-registration gets the replacement NPC's new owner. Positive debit and credit revision changes, no-op/failed writes, roster add/remove, duplicate IDs, aliases, absent owners, and replacement behavior are covered by focused tests.

No runtime operation or balance-write notification is added. The existing roster mutation epoch changes only when membership reconciliation changes the account-family census; the candidate claims no shared-epoch completeness or account-write epoch wiring. It adds no capture eligibility, export/hydration, or P12-B/P12-A/P13 readiness claim. Recorded phase limits remain unchanged.

## Validation evidence checked

The reviewer checked the four XML summaries and all eight XML/log SHA-256 values recorded in `PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_CANDIDATE.md`; all match. Results are focused census 10/10, selected-profile bootstrap 14/14, ALL EditMode 2118/2118, and official Smoke 5/5, with zero failures or skips. `git diff --check` from canonical base through the exact code tip reported no issues. Tests were not rerun as part of this independent review, and no files were edited by the reviewer.

This review covers code commit `2bdd0990acc2bdc2d6073b0863fc1ae94a209c4d` only. The evidence tip and this review-record commit add documentation; they do not alter the reviewed code tree. This PASS is not canonical promotion approval. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.
