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

`P12CrimeSocialAppraisalInvalidationTests` covers exact selected-profile owner registration, initially empty owners and later Person admission; direct store writes outside and inside registered operations; composite theft batching in reserved and immediate modes; nested compensation; and refusal on owner-revision or shared-epoch exhaustion. The action regressions call `CrimeSystem.TryExecuteAction` directly. The current test set does not drive the authored `Steal` action through `SimulationRuntime.TryAdvanceDay` and assert the combined `runtime.advance-day` plus Crime/Social notifications.

Thus this audit resolves the source mapping and distinguishes internal integration scopes from registered runtime operations. It does not claim end-to-end selected-runtime execution evidence for that ingress. That test-evidence item remains open for P12-G review; no test was added or run as part of this documentation audit.

## Status boundary

No registered operation ID is added or changed, and no Crime/Social gameplay semantics are changed. This document closes only the stale source-mapping question for Crime/Social in the P12-G owner/operation/epoch ledger. It does not complete the entire owner matrix, prove all supported direct paths, make P12-G implementation-ready, make P12-A ready, unblock P13, or close Phase 12. Other operation/epoch rows, fresh target witnesses, restored-boundary admission, single-session publication, and whole-graph proof retain their recorded gates.
