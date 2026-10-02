# P12-B Merchant daily-operation design — revision 2 review

**Verdict: PASS — independent exact-tip design review**

Reviewed candidate `e9fbdada98eb6fed2d1e7a446ac9a4085504c676`, tree
`914399981d019c8f39560ef926acd0ba0e18672a`, based on the requested exact
P12 canonical base `a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab`. The base-to-tip
diff adds only
`docs/design/PHASE12_P12B_MERCHANT_DAILY_OPERATION_DESIGN.md`. The candidate
records the prior review `fff5e9364c7c63c4c36e6635f12adb32e610c795` and
addresses both findings. The architecture baseline, P12 Brief/State,
Execution Model, Roadmap, blocker matrix, and actual base source cited by the
design were reviewed. `git diff --check` passes; no test suite was run for
this documentation-only candidate.

## Review findings

1. **Allocator and sequence scope is corrected.** The revised design removes
   `RuntimeIdAllocator`, `SimulationRecordSequence`, and
   `NpcDecisionStore` from this invocation's preflight and epoch batch. It
   preserves existing decision-record and redirect behavior and records
   those global roots/read-model writes as uncovered P12-C/P12-F work. This
   is compatible with the accepted partial P12-B capability contract and
   makes no capture, completeness, or P12-A readiness claim.

2. **Same-owner invalidation is now technically bounded.** The revised
   contract adds exact owner-bound commit hooks for the four relevant owner
   families: `SpatialKnowledgeRuntime`, `CommercialKnowledgeRuntime`, the
   embedded `MerchantTradePlanRuntime`, and `NpcTravelPlanRuntime`. It
   requires exact owner/RuntimeId binding, shared-revision sibling
   notification, owner-thread admission before bound writes, post-commit
   notification, and fail-closed handling. Supported pre/post Merchant
   writes in the normal daily composition execute under the already admitted
   `runtime.advance-day` scope. Those writes refresh their exact baselines;
   the named nested Merchant invocation collects its own changes and
   publishes one post-commit batch. A bound direct owner mutation outside a
   named operation does not acquire that operation ID. This resolves the
   stale-baseline issue without claiming that the excluded call's domain
   effects are part of `runtime.merchant.advance-npc-trade-state`.

   For implementation, “all successful mutations” of the two plan owner
   families must include existing mutators such as
   `MerchantTradePlanRuntime.RegisterSale` and
   `IncrementPendingTravelDay`, as well as Set/Clear/Redirect/wait methods,
   and `NpcTravelPlanRuntime.Set/Clear`. This is supported by the revised
   owner-family-wide hook contract; the business effects of their callers
   remain outside this named operation. `TravelActionProvider`,
   `CrimeSystem`, `MerchantSystem.TryExecuteAction`, daily knowledge refresh,
   knowledge sharing, and post-travel observations therefore cannot leave a
   silently stale baseline when their bound owner commits are routed through
   those hooks.

The candidate introduces no new gameplay or product semantics and does not
amend canonical architecture. Its exact owner surface, nested commit
collector, and global-root exclusions remain bounded prerequisite capability
design, not a claim of complete selected-profile writer coverage.

## Limits retained

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked; Phase 12 remains open. This review does not authorize implementation
or canonical promotion and claims no complete owner census, shared-epoch
coverage, runtime-wide quiescence, capture eligibility, export, or hydration.
