# P12-G current Crime/Social ingress and epoch crosswalk

**Baseline:** P12 canonical `97bcc5c66fba0b03ef1242807fe8d9dc51a6dc10`
**Production source tree:** `Assets` tree `1b90b4f77586f69c04330b564a32e0a9475808d4`; `_Project/Scripts` matches the source-audit baseline at `02009f9063dd252bd4b177fd6aef1e74dcd947f5`.
**Scope:** Source-level reconciliation for the explicit Crime/Social row in the P12-G Daily-v1 owner/operation/publication audit. No code, test, operation-ID, or gameplay change.

## Normal selected Daily-v1 theft ingress

When a supported actor turn selects a `Steal` action through `CrimeSystem`, the runtime path is:

1. `SimulationRuntime.TryAdvanceDay` enters the registered `runtime.advance-day` operation and calls `TryAdvanceDayCore` while that operation scope is active.
2. The actor loop reaches `TryExecuteCurrentAction`, then `TryExecuteAction`. For a P12-bound Crime provider, `TryExecuteAction` opens the existing Crime/Justice action-mutation scope around `CrimeSystem.TryExecuteAction`; this scope protects its exact participant/Justice owners and is not a separately registered census operation ID.
3. `CrimeSystem.TryExecuteSteal` requires both NPCs to have `PersonId` and the action to have a stable occurrence key before accepting a social outcome. It checks `CanAcceptTheftOutcome`, transfers money, and calls `TryAcceptTheftOutcome`. If acceptance fails, the existing path attempts the reverse money transfer. Justice warrant mutation follows successful social acceptance.
4. `CrimeSocialAppraisalIntegration.TryAcceptTheftOutcome` opens its internal `TheftAcceptance` coordinator context. It writes the outcome, then calls `TryRecordKnowledgeAndAppraise`, which joins as the one permitted nested `KnowledgeAndAppraisal` context. On failure, existing compensation removes the new outcome; the outer context publishes the union of changed Crime/Social sections once.

Therefore the selected Daily-v1 external operation enclosing a normal actor theft is `runtime.advance-day`. `TheftAcceptance` and `KnowledgeAndAppraisal` are integration-local coordinator stages, not new IDs in the 24-operation protocol matrix. The Crime/Justice participant action scope is a separate owner-admission boundary; it does not register or subsume the three Crime/Social owner sections.

The authored sample begins with no registered Persons and its legacy NPCs have no `PersonId`, so this path cannot initially emit a theft outcome. Existing P12 membership/materialization can later bind Persons; the identity precondition is conditional state, not evidence that the owners remain permanently empty.

## Direct synchronous ingress and epoch accounting

The reviewed P12-B contract supports its existing direct store writes and standalone integration calls outside a registered operation, provided they run synchronously on the bound owner thread and pass the exact owner/revision and epoch-capacity preflights. These calls do not receive a fabricated operation ID.

- A successful direct single-store row change advances that store's local revision and immediately notifies its exact section, consuming one shared epoch.
- A standalone `TryRecordKnowledgeAndAppraise` owns one synchronous composite context and reports its changed Knowledge/Reaction sections once at close.
- `TryAcceptTheftOutcome` owns the outer composite context across its one nested appraisal call and any outcome compensation. All changed Outcome/Knowledge/Reaction sections are reported together once when any rows changed, including a compensated partial failure whose final row counts return to their prior values.
- The coordinator uses the existing reserved-token path when capacity can be reserved. It selects the reviewed immediate path only when reservation reports `OperationInProgress` and the existing mutation-epoch-capacity validation succeeds. If neither path can be admitted, it rejects before the first affected row write. Local revision headroom and relevant owner baselines are preflighted before writes.

These are the existing one-write and bounded composite paths from the accepted P12-B design. They do not establish that arbitrary off-thread, reentrant, or unbound calls are supported; they add no global quiescence or complete shared-epoch claim.

## Existing evidence and remaining limit

At the source-audit baseline, `P12CrimeSocialAppraisalInvalidationTests` covered exact selected-profile owner registration, initially empty owners and later Person admission; direct store writes outside and inside registered operations; composite theft batching in reserved and immediate modes; nested compensation; and refusal on owner-revision or shared-epoch exhaustion. Its action regressions called `CrimeSystem.TryExecuteAction` directly, so runtime ingress evidence remained open at that point.

Thus this audit resolves the source mapping and distinguishes internal integration scopes from registered runtime operations. It does not claim all supported direct paths or complete shared-epoch coverage.

## Status boundary

No registered operation ID is added or changed, and no Crime/Social gameplay semantics are changed. This document closes only the stale source-mapping question for Crime/Social in the P12-G owner/operation/epoch ledger. It does not complete the entire owner matrix, prove all supported direct paths, make P12-G implementation-ready, make P12-A ready, unblock P13, or close Phase 12. Other operation/epoch rows, fresh target witnesses, restored-boundary admission, single-session publication, and whole-graph proof retain their recorded gates.

## Current-base runtime ingress evidence — 2026-10-09

The test-evidence gap above was subsequently closed on P12 canonical base `bc9ab6a27d5bb5c1e2de0987aa6377db6a96c37e` by `P12CrimeSocialAppraisalInvalidationTests.SelectedDailyV1AdvanceDayExecutesStealInsideOuterOperationAndReconcilesCrimeSocialOwners`.

The test starts the actual authored `Simulation-DailyV1.asset` bootstrap under `UnityBootstrapDailyV1`, selects the profile's existing NPC whose default actions include the authored `Action-Roubar`, and restricts the runtime's configured action list to that action for this fixture. It registers and materializes Persons for the NPCs in the same City through normal runtime admission because the authored profile begins with an empty PersonStore. The test makes the authored action non-failing only for this run and restores its original setting in `finally`, so the assertion proves the successful Crime/Social commit path without depending on a probabilistic action roll or changing the saved asset.

The test calls `SimulationRuntime.TryAdvanceDay`. At the integration sink, the registered protocol reports one active operation; the protocol identifies that enclosing operation as `runtime.advance-day`. After return, the test observes one TheftOutcome, one CrimeKnowledge row and one SocialReaction row, each with local revision `1`; a greater shared mutation epoch; zero active operations; successful registered-operation quiescence; and a successful roster census. The focused `P12CrimeSocialAppraisalInvalidationTests` suite passed 12/12, ALL EditMode passed 2733/2733, and official Smoke passed 5/5. Exact result artifacts and hashes are in `docs/validation/P12GCrimeSocialRuntimeOperation/VALIDATION.md`.

This closes only the selected Daily-v1 Crime/Social runtime-ingress witness. It does not complete the owner/commit matrix, prove all supported direct paths, establish global quiescence, complete P12-G, make P12-A ready, unblock P13, or close Phase 12. The other recorded operation/epoch rows, fresh target witnesses, restored-boundary admission, single-session publication and whole-graph proof remain outstanding.
