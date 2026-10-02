# P12-B bounded NPC money-transfer operation design

**Status:** Documentation-only technical design candidate. Base P12 canonical: `2f7c7422812de40aa1223e8310dcbd9f5d8ca474`. This is an operation slice within the accepted P12-B prerequisite capability scope; it adds no Phase/checkpoint ID, product behavior, or new semantic contract. Implementation readiness depends on promotion of the separate direct NPC owner-commit invalidation capability, whose callbacks this slice relies on.

## Purpose and boundary

Register and account for the existing P12-bound NPC-to-NPC `EconomyTransactionService.TryTransferMoney(NpcRuntime, NpcRuntime, int)` operation. The operation covers only the source and destination NPC MoneyAccount sections during the existing transfer's preflight, debit, credit, any source-credit compensation, and returned result. The service's raw `MoneyAccountRuntime` overload remains the standalone/domain path used by isolated tests; this design does not bind it to a P12 runtime.

The production caller is `CrimeSystem.TryExecuteSteal`. The transfer call and a later reverse transfer are separate operation invocations. The theft outcome, warrant, and any other Crime/Justice writes remain outside this slice. This design does not claim Crime coverage, complete owner/operation coverage, shared-epoch coverage, capture eligibility, export, hydration, P12-A readiness, P12-B completion, or P13 readiness.

## Existing authority and dependency

The accepted `PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md` already identifies money transfer as a remaining selected economy transaction inside P12-B, not a new checkpoint ID. The current `SimulationRuntime` registers stable operation IDs for NPC trade and Market purchase/sale before sealing its selected operation inventory. The current `EconomyTransactionService.TryTransferMoney(NpcRuntime,...)` delegates to the account transfer helper using the two participants' account instances and RuntimeIds. The helper performs source debit, destination credit, and source-credit compensation if the credit does not complete.

This operation depends on the bounded direct NPC owner-commit invalidation slice adding postcommit notifications to `MoneyAccountRuntime.TryDebit` and `TryCredit`, with exact instance-to-section binding and P12 runtime-thread/baseline checks. Until that capability is promoted, this design can be reviewed, but implementation stays `WAIT_DEPENDENCY`; do not duplicate notifications in the transaction service to work around the missing owner hooks.

## Contract

1. **Stable operation admission.** Add the stable ID `runtime.economy.money-transfer` to the selected operation inventory during `SimulationRuntime.InitializeNpcRosterCensusProtocol`, before `SealOperationInventory`. Bind only the shared `EconomyTransactionService` instance already used by the normal runtime. Preserve standalone service behavior when no P12 runtime is bound.

2. **Exact participants.** Resolve the two roster NPCs through the existing P12 dynamic MoneyAccount sections `p12e.npc-money-account/{RuntimeId}`. Before any debit, verify that each argument is the current roster `NpcRuntime`, that each account is the exact instance bound to its section, and that each section still matches its registered owner identity and local revision baseline. Reject failed P12 admission with the existing `TransactionCommitFailed` result before mutation. Preserve the transaction's existing domain validation and result semantics.

3. **One active operation across all commits.** Enter the named operation after all no-write domain/admission preflight and before the first possible account commit. Keep it active across source debit, destination credit, compensation, and result selection. Dispose it on every return path. An admission or identity/baseline rejection before the first commit performs no account write and advances no mutation epoch.

4. **Owner hooks are the notification authority.** Each successful `TryDebit` or `TryCredit` notifies exactly the corresponding registered account section after that owner instance commits its local revision. Do not add duplicate transaction-service notifications. If compensation successfully credits the source, it is another committed revision and receives exactly one source-section notification while the same outer operation remains active. Failed/no-op owner calls do not notify.

5. **Compensation and postcommit faults.** Preserve existing transfer results, including any already committed domain effects when a later step or compensation fails; P12 does not add rollback guarantees. If continuation bookkeeping fails after an account revision commits, fault P12 admission closed without rewriting that committed domain result or implying the revision was undone. The operation scope must still unwind on all ordinary return paths.

6. **Caller boundary.** `CrimeSystem.TryExecuteSteal` calls the NPC overload for the initial transfer, then separately commits theft outcome and may call the NPC overload again to reverse the transfer if the outcome is rejected. Each call gets its own transfer scope. The theft outcome, warrant, Crime owner inventory, and full steal transaction are not covered.

## Validation plan

Retain the existing transaction tests for destination preflight before source debit, successful money conservation/runtime identities, unrepresentable mutation rejection, and the Crime caller's use of the transaction boundary. Add P12 protocol tests for:

- successful transfer: one active named scope across both owner commits; one epoch notification for each committed account revision; exact source/destination owner IDs and local revisions;
- invalid amount, same/invalid participants as applicable to existing semantics, stale/replaced owner, wrong thread, or failed named-operation admission: reject before writes and leave the epoch unchanged;
- destination credit failure and source compensation: preserve the established transfer result and observe each successful debit/refund revision once while the same scope remains active;
- failure after a committed owner notification: admission faults closed while the committed transaction result is preserved;
- scope disposal on success, preflight rejection, partial commit, compensation success/failure, and exception-safe exits.

Run the focused transfer/Crime and P12 protocol suites, ALL EditMode, official Smoke, and `git diff --check` on the eventual exact code candidate. Record the XML/log hashes and exact base/candidate SHAs. This design itself is docs-only: it needs exact-tip independent design review and `git diff --check`; no Unity validation applies.

## Readiness and retained limits

After exact-tip independent design review, implementation becomes eligible only after the direct NPC owner-hook capability is canonical and the implementation worktree is rebased/revalidated against that new canonical tip. The slice advances one existing economy operation family and reduces an `OPERATION_COVERAGE_GAP`; it does not make the selected owner inventory complete, establish universal shared-epoch coverage, prove global owner-thread/quiescence, create capture eligibility, or satisfy P12-A. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.
