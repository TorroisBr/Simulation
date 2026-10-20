# CORE-F selected action execution stage checkpoint — 2026-10-19

Status: **recovery checkpoint only**. This records a selected action execution increment and does not claim a complete Daily-v1 runtime, independent full-package review, Unity validation, or canonical promotion.

## Source identity and scope

- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`.
- Candidate base before this checkpoint: `245912a72aea6eee2d1b41a9d2b95e099edda9e4` (same hash observed on the matching remote branch before commit).
- The checkpoint adds a Core-owned, exact-slot-bound execution stage for selected open-market actions, planned NPC sales (including plan admission and revision-overflow preflight), and selected solo travel. Core resolves/validates selected payloads and owns transaction/travel effects; Unity adapters retain P12 admission/notification and event/log projection.
- Focused Core tests cover selected owner graph integration, market actions, NPC transaction/order/compensation, planned buyer/sale completion, plan admission rejection and max-revision rejection, and solo travel route/charge/refund behavior.
- Unity still owns the enclosing roster/directive/actor-choice ordering and the full Daily-v1 coordinator. This checkpoint does not close those boundaries.
- Protected ProjectSettings edits, pre-existing untracked metadata, `.codex-tmp`, and host `bin/obj` are excluded from the checkpoint.

## Validation

- Focused test command:
  `dotnet test tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~DailyV1SelectedSoloTravelExecution|FullyQualifiedName~DailyV1SelectedTravelTransitionPlanTests|FullyQualifiedName~DailyV1SelectedRuntimeOwnerGraphTests|FullyQualifiedName~DailyV1SelectedNpcTradeExecutionTests|FullyQualifiedName~DailyV1MerchantActionPlanningTests|FullyQualifiedName~DailyV1NpcMerchantTradePlanOwnerTests|FullyQualifiedName~DailyV1SelectedOpenMarketActionExecutionTests|FullyQualifiedName~DailyV1SelectedOpenMarketSaleTests|FullyQualifiedName~DailyV1SelectedOpenMarketPurchaseTests|FullyQualifiedName~DailyV1DecisionRecordsTests|FullyQualifiedName~DailyV1NpcDefinitionRulesTests" -v minimal`
- Result: **76/76 passed** (SDK 9.0.315; test run completed after the code changes in this checkpoint).
- `git diff --cached --check`: PASS for the staged CORE-F paths.
- Scoped independent review of planned-sale preflight: PASS. In particular, the exact next-plan transition is checked for revision overflow before admission or sale effects; P12 admission rejection is mutation-free. This is a scoped code review result, not an exact-tip full-package review.
- Unity EditMode/Smoke and full CoreValidation were not run for this checkpoint.

## Next boundary

Continue with Core-side candidate selection and record-to-slot installation, then integrate that into the ordered roster/directive actor-turn coordinator. Keep P18-D receipt behavior and existing P12 adapters at their established boundaries.
