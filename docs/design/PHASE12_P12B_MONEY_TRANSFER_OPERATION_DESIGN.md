# P12-B bounded NPC money-transfer operation design

**Status:** Documentation-only technical design candidate for accepted checkpoint P12-B. Refreshed base P12 canonical: `b4c100d256fa0d6c624353ebefd437695100a85a`; promoted code-bearing integration base: `9d1474b4299d8e888dd387e02e9018d9e8627f84`. This existing operation slice adds no Phase/checkpoint ID, product behavior, or new semantic contract. Implementation readiness follows exact-tip independent review of this refreshed design and use of the serialized `SimulationRuntime` integration window.

## Purpose and boundary

Register and account for the existing P12-bound NPC-to-NPC `EconomyTransactionService.TryTransferMoney(NpcRuntime, NpcRuntime, float)` operation. The operation covers only the source and destination NPC MoneyAccount sections during the existing transfer's preflight, debit, credit, any source-credit compensation, and returned result. The raw `MoneyAccountRuntime` overload is a standalone/domain path only while the service is unbound. When the service is bound to a P12 runtime, that overload must reject before any account write, even if its account arguments happen to be registered NPC owners. The NPC overload is the only P12-bound transfer route because it carries the exact roster identities needed for operation admission.

The production caller is `CrimeSystem.TryExecuteSteal`. The transfer call and a later reverse transfer are separate operation invocations. The theft outcome, warrant, and any other Crime/Justice writes remain outside this slice. This design does not claim Crime coverage, complete owner/operation coverage, shared-epoch coverage, capture eligibility, export, hydration, P12-A readiness, P12-B completion, or P13 readiness.

## Existing authority and dependency

The accepted `PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md` already identifies money transfer as a remaining selected economy transaction inside P12-B, not a new checkpoint ID. The current `SimulationRuntime` registers stable operation IDs for NPC trade and Market purchase/sale before sealing its selected operation inventory. The current `EconomyTransactionService.TryTransferMoney(NpcRuntime,...)` delegates to the account transfer helper using the two participants' account instances and RuntimeIds. The helper performs source debit, destination credit, and source-credit compensation if the credit does not complete.

The bounded direct NPC owner-commit invalidation slice is now promoted in P12 canonical (code `c49f957e45c3059231e9ec66e4010a7c3a389988`, integrated in `9d1474b4299d8e888dd387e02e9018d9e8627f84`). It supplies postcommit notifications for `MoneyAccountRuntime.TryDebit` and `TryCredit`, with exact instance-to-section binding and P12 runtime-thread/baseline checks. The combined WI-A/P12 hotspot revalidation passed on exact WI-A code `3b39e0d89858dce517ad72cbb76da621eb954bad`: the P12 hooks, transaction callers, and duplicate suppression are unchanged. Do not duplicate those notifications in the transaction service. P12's partial epoch remains non-universal.

## Contract

1. **Stable operation admission.** Add the stable ID `runtime.economy.money-transfer` to the selected operation inventory during `SimulationRuntime.InitializeNpcRosterCensusProtocol`, before `SealOperationInventory`. Bind only the shared `EconomyTransactionService` instance already used by the normal runtime. Preserve standalone service behavior when no P12 runtime is bound. A P12-bound call through the raw-account overload rejects before mutation and does not advance the epoch; normal runtime callers must use the NPC overload.

2. **Exact participants.** Resolve the two roster NPCs through the existing P12 dynamic MoneyAccount sections `p12e.npc-money-account/{RuntimeId}`. Before any debit, verify that each argument is the current roster `NpcRuntime`, that each account is the exact instance bound to its section, and that each section still matches its registered owner identity and local revision baseline. Reject failed P12 admission with the existing `TransactionCommitFailed` result before mutation. Preserve the transaction's existing domain validation and result semantics.

3. **One active operation across all commits.** Enter the named operation after all no-write domain/admission preflight and before the first possible account commit. Keep it active across source debit, destination credit, compensation, and result selection. Dispose it on every return path. An admission or identity/baseline rejection before the first commit performs no account write and advances no mutation epoch.

4. **Owner hooks are the notification authority.** Each successful `TryDebit` or `TryCredit` notifies exactly the corresponding registered account section after that owner instance commits its local revision. Do not add duplicate transaction-service notifications. If compensation successfully credits the source, it is another committed revision and receives exactly one source-section notification while the same outer operation remains active. Failed/no-op owner calls do not notify.

5. **Compensation and postcommit faults.** Preserve existing transfer results, including any already committed domain effects when a later step or compensation fails; P12 does not add rollback guarantees. If continuation bookkeeping fails after an account revision commits, fault P12 admission closed without rewriting that committed domain result or implying the revision was undone. The operation scope must still unwind on all ordinary return paths.

6. **Zero-value transfer.** Preserve the current successful no-op for a finite `0f` transfer between distinct valid accounts. The NPC overload may run the named operation scope, but neither account revision nor the shared mutation epoch advances and no owner notification is emitted. Same-account rejection and other existing validation order remain unchanged.

7. **Caller boundary.** `CrimeSystem.TryExecuteSteal` calls the NPC overload for the initial transfer, then separately commits theft outcome and may call the NPC overload again to reverse the transfer if the outcome is rejected. Each call gets its own transfer scope. The theft outcome, warrant, Crime owner inventory, and full steal transaction are not covered.

## Validation plan

Retain the existing transaction tests for destination preflight before source debit, successful money conservation/runtime identities, unrepresentable mutation rejection, and the Crime caller's use of the transaction boundary. Add P12 protocol tests for:

- successful transfer: one active named scope across both owner commits; one epoch notification for each committed account revision; exact source/destination owner IDs and local revisions;
- invalid amount, same/invalid participants as applicable to existing semantics, stale/replaced owner, wrong thread, or failed named-operation admission: reject before writes and leave the epoch unchanged;
- raw-account overload invoked on a P12-bound service: reject before debit, leave both balances/revisions and the epoch unchanged; the same overload remains covered for an unbound standalone service;
- finite zero-value transfer through the P12-bound NPC overload between distinct valid accounts: preserve success, with no owner revision or epoch change;
- destination credit failure and source compensation: preserve the established transfer result and observe each successful debit/refund revision once while the same scope remains active;
- failure after a committed owner notification: admission faults closed while the committed transaction result is preserved;
- scope disposal on success, preflight rejection, partial commit, compensation success/failure, and exception-safe exits.

Run the focused transfer/Crime and P12 protocol suites, ALL EditMode, official Smoke, and `git diff --check` on the eventual exact code candidate. Record the XML/log hashes and exact base/candidate SHAs. This design itself is docs-only: it needs exact-tip independent design review and `git diff --check`; no Unity validation applies.

## Readiness and retained limits

After exact-tip independent review of this refreshed design, implementation is eligible under the already accepted P12-B prerequisite capability authorization. It must branch from the current P12 canonical base, use the existing owner hooks as the notification authority, and serialize `SimulationRuntime` integration with other runtime owners. This slice advances one existing economy operation family and reduces an `OPERATION_COVERAGE_GAP`; it does not make the selected owner inventory complete, establish universal shared-epoch coverage, prove global owner-thread/quiescence, create capture eligibility, or satisfy P12-A. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.
