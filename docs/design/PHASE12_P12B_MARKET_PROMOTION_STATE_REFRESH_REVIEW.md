# P12-B Market promotion State/matrix exact-tip review

**Result:** PASS

**Reviewed candidate:** `codex/phase12/P12BMarketPromotionStateRefresh` at
`57259f66ace203a28b7cd2773881e849593a0c54`.

**Base:** `codex/phase12/canonical` at
`b77e154e86b510c9f47ea9042fe5a1edb39749b0`.

**Review scope:** documentation-only updates to `docs/PHASE12_STATE.md` and
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`, independently reviewed against
the direct parent and current P12 canonical state.

The update accurately records the approved Market bundle promotion from
`b8a7da54864bee3fb9b8916793240e91fbce0955` to `b77e154`, preserves the
reviewed code tip `b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`, code tree
`88ec452a979a7439b825858a6b1b271f8bc6c948`, and exact-tip review evidence.
It corrects stale “promotion pending” and “NPC membership is the only
notifying path” statements while retaining the bounded Market scope and
limitations.

The refreshed classification keeps P12-B incomplete, P12-A
`WAIT_DEPENDENCY`, and P13 blocked. It does not claim complete owner coverage,
shared-epoch coverage, capture eligibility, export, or hydration. The matrix
identifies the remaining direct per-NPC MoneyAccount/Inventory write gap and
classifies the bounded follow-up design as `DESIGN_REQUIRED`.

`git diff --check` passed. No code, ProjectSettings, or existing `.meta`
files changed.
