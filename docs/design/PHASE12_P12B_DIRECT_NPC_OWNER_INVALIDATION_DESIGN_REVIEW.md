# P12-B direct NPC owner-write invalidation design review

**Result:** PASS

**Reviewed design candidate:** `codex/phase12/P12BDirectNpcOwnerInvalidationDesign` at
`9b94d2fa8ac69021cf741e1f5ededc914dc3edc5`.

**Canonical base:** `codex/phase12/canonical` at
`b77e154e86b510c9f47ea9042fe5a1edb39749b0`.

Independent review verified the design against the exact promoted NPC
MoneyAccount/Inventory census, NPC-trade integration, Market-operation
candidate, owner runtime methods, and current P12-B blocker matrix.

The revised contract preserves exact owner identity, local revision and
section baselines; owner-thread pre-write checks; all-section notification for
aliased Inventory owners in one batch; and one shared-epoch notification per
physical committed leaf write. It retains the existing outer NPC-trade and
Open-market operation scopes while moving per-leaf account/Inventory
notifications to the owners, avoiding duplicates and preserving Market
notifications.

It resolves the prior `void AddItem` rejection ambiguity with
`TryAddItem`, keeps `AddItem` as a legacy/setup compatibility wrapper, and
identifies the NPC trade, Open-market purchase, and Expedition resource
retrieval transactional call sites. Their rejected additions return failure
without claiming rollback of earlier committed effects or introducing new
atomicity. It specifies post-commit epoch saturation without capacity
preflight: preserve committed domain success, fault the protocol closed, and
make subsequent census assessment unavailable. Focused tests cover these
behaviors.

Prepared-install paths and other owner/operation families remain excluded.
The design does not claim P12-B completion, P12-A readiness, P13 readiness,
global mutation coverage, capture eligibility, or export/hydration.

The first review found and resolved the `AddItem` result and epoch-saturation
ambiguities before this PASS. No runtime code was changed by the design
candidate. Canonical promotion of code remains a separate human gate.
