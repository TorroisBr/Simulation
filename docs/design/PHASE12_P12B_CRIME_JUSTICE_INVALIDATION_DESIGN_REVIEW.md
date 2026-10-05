# P12-B selected-profile Crime/Justice invalidation design review

**Result:** PASS — `READY_FOR_IMPLEMENTATION` under the previously accepted P12-B prerequisite-capability authority.

**Exact design candidate:** `452426687a57c1cfebb3f007511c5e23a21273a2`
**Exact candidate tree:** `e970567ae059dc8c537739469cfff65c0d67fa6b`
**Candidate base / current P12 canonical:** `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`
**Architecture:** `ffd75652d89d862b83d634868c560f8540869b89`
**Intraday/extensibility alignment:** `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
**Multi-participant activity alignment:** `c285466c355103d3637ac165246591b72eb7bda0`
**P19 proposal consulted:** `686a23cbdf4d8e78ba4d4f76c3e6b62fc08f4df2`

The independent reviewer verified the exact candidate branch and tree, the documentation-only diff from current P12 canonical, and `git diff --check`. The review traced the daily path through the single outer `runtime.advance-day` operation, including the later actor turns. The four legacy daily calls retain sequential per-call epoch reservations. Crime/Guard action leaves, failed-Escape handling, successful actor/target status effects, and forced Escape use immediate per-leaf capacity/baseline checks and notifications; they do not hold an epoch reservation or defer notifications across unrelated owner callbacks.

The design covers the specified selected-profile NPC status/hidden and singleton Justice facts. It binds mutable wanted and sentence row aliases to their exact Justice owner, rejects unsupported direct P12-bound status/Justice writes before mutation, and rejects selected-profile P18 receipt commits while preserving explicit-empty receipt sections. Cardinalities are bounded to one `NpcRuntime` per registered `RuntimeId`, one `JusticeSystem`, ordered status values, one hidden scalar, and dynamically sampled zero-or-more historical warrant/sentence rows. Temporal row identities remain deferred to P12-E export; this invalidation slice makes no export claim.

The reviewer found no remaining bypass within those specified fact families. `CrimeSocialAppraisal`, `TheftOutcome`, `CrimeKnowledge`, and `SocialReaction` owners remain explicit uncovered P12-B blockers. P17-A has no semantic conflict; its own exclusion/admission proof and serialized runtime integration remain separate. No complete owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-A readiness, P13 readiness, or Phase 12 closure is established.

Implementation may proceed only within the reviewed design and previously accepted P12-B capability scope. Required focused tests, affected regressions, ALL EditMode, official Smoke, and `git diff --check` remain outstanding for the implementation candidate. This review does not approve canonical promotion.
