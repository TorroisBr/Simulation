# P12-B per-NPC MoneyAccount census design — independent review

**Verdict:** PASS  
**Reviewed design tip:** `a14f674c188339df18013e7beeaa564f5d8c997d`  
**Canonical base:** `43dba1b5d16cba1558c4c39239f3cd0d4c669958`  
**Reviewed file:** `docs/design/PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_DESIGN.md`

The exact reviewed contract is bounded to one passive census section per installed NPC-owned `MoneyAccountRuntime`, keyed by the NPC's stable RuntimeId and reporting exact owner identity, cardinality 1, and owner-local revision. It excludes balances and all City-side accounts.

The section prefix `p12e.npc-money-account/` is distinct from the existing Inventory and Market stock-row families. The selected-profile ten-NPC cardinality, account installation and revision behavior, roster membership reconciliation, identity replacement behavior, and fail-closed owner validation agree with the current source contracts and existing census protocol pattern.

The design correctly preserves the current missing invalidation: a positive account revision advance without a supported account-section notification must fault the census protocol on the next assessment or membership reconciliation. The proposed tests assert no partial publication and the expected protocol fault, without implying other families remain assessable after the shared protocol faults.

**Limitations retained:** this review approves only the design boundary within the previously accepted P12-B owner/cardinality census work. It does not create a new checkpoint or implementation authorization. It does not add balance export, City-side account coverage, mutation notification/shared-epoch wiring, runtime operation scopes, capture eligibility, export/hydration, or P12-A/P13 readiness. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

No files were modified and no tests were run during this read-only review.
