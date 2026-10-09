# P12-G City-bound Person materialization reconciliation review

**Result:** `VALIDATED_CANDIDATE`

**Candidate branch:** `codex/phase12/P12GCurrentCanonicalInventoryRefresh`

**Exact reviewed candidate:** `d847102c5b3dfc28e3880d1ca1f2a96608ba2127`

**Implementation commit:** `11653ebd3947220a39d785c0c0641837e5cfadbe`

**Base / current P12 canonical at review:** `93fd6ab7f39de572fafbfb1fa160f342935839e5`

**Architecture:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Reviewed Assets tree:** `032e65318ea510c1009f89c8b0431a4c6aea46c0`

An independent exact-tip review passed on the pushed candidate. The candidate is a clean additive descendant of current P12 canonical. The implementation marks the exact City presence section after successful Person materialization with a non-null `startingCity`, inside the existing `runtime.npc-membership` scope. This accounts for the City projection committed after `TryRegisterNpc` has registered an NPC whose `CurrentCity` is still null. The membership reconciliation then validates Person membership/binding, dynamic NPC owner families, and affected City presence together under its reserved mutation epoch.

The selected Daily-v1 witness checks City owner identity, cardinality and revision, the exact independent manifest/provider vector after materialization, a single epoch for the materialization operation, and successful post-commit census assessment. The reviewer confirmed the complete candidate diff is within the accepted P12-B membership operation and P12-G live-inventory witness contracts; it adds no product semantics, owner family, operation, or new checkpoint boundary. It closes only this supported City-bound materialization transition witness.

Exact-tree validation is recorded in [`../validation/P12GCityBoundMaterialization/VALIDATION.md`](../validation/P12GCityBoundMaterialization/VALIDATION.md). The final-tree focused suite passed 26/26, ALL EditMode passed 2732/2732, official Smoke passed 5/5, and `git diff --check` passed. The reviewer independently rechecked all six final XML/log hashes against the committed files and confirmed the Assets tree above.

This record does not change checkpoint identities or infer broader readiness. P12-B through P12-F retain their promoted status within their bounded scopes; P12-G remains `WAIT_DEPENDENCY` on its remaining live-inventory, target validation, restored-boundary, publication, and whole-graph obligations. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. No complete owner/shared-epoch coverage, capture eligibility, export, hydration, or Phase 12 closure is claimed.

The unrelated ProjectSettings edits and untracked `.meta` files were outside the candidate diff and remain untouched.
