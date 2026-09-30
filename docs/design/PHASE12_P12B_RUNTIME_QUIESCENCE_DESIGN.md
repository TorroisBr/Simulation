# P12-B Runtime Admission and Quiescence Adapter — Technical Design

**Status:** Proposed bounded prerequisite design; implementation and independent
design review are pending. This document does not declare P12-B complete or
P12-A ready.

**Evidence base:** Phase 12 canonical `e9ced8e451f42e80ed2132ce494cd5c26e439894`,
including `docs/PHASE12_STATE.md` and
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`; current
`SimulationRuntime`, `TesteSimulacao`, `ContinuationCensusProtocol`, and P18
logical-timeline/advance-lease implementation on that commit.

## Purpose and boundary

The selected `UnityBootstrap-Daily-v1` profile needs a runtime-owned proof that
its supported bootstrap publication and outer time-advance operations are
admitted on the Unity `Start` thread and that a census cannot be assessed while
one of those operations is still executing. The existing P12 census protocol
already binds to a managed thread and counts explicitly registered synchronous
operation scopes. `SimulationRuntime` currently binds that protocol to the
thread on which its constructor runs, but it only accounts for the nested
NPC-membership operation. `TesteSimulacao.Start` currently does not retain an
explicit thread identity, bootstrap has no operation scope held through
publication, and daily/intraday outer advances are guarded by P18's
reentrancy-only lease rather than a thread-affinity/quiescence scope.

This design adds only the runtime admission adapter for:

1. the existing `TesteSimulacao.Start` authored-genesis/publication path;
2. the existing P12 `runtime.npc-membership` nested operation; and
3. accepted, nonzero `SimulationRuntime` day and P18 intraday outer advances,
   including their synchronous post-success work and handoff.

The adapter is a single-thread admission/counting mechanism, not a lock and not
a substitute for owner-store synchronization. It does not establish exhaustive
owner coverage, map every writer to an invalidation, make uninstrumented commits
safe, add operation registration around arbitrary public methods, or itself
issue capture eligibility. Census completeness, committed-write invalidation,
exact owner/cardinality evidence, and export/hydration remain separate P12-B
obligations. P12-A remains `WAIT_DEPENDENCY` until all of its independently
specified prerequisites and authorization are satisfied.

## Existing behavior and constraints

- `TesteSimulacao.Start` calls `InitializeSimulation` synchronously. Its
  `SimulationGenesisPipeline.ExecuteStages` creates `SimulationRuntime` in
  `p9.genesis.validate-profile/v1`, validates, then assigns
  `publishedComposition` in `p9.genesis.publish/v1`. That assignment occurs
  before the stage callback and before the entire pipeline returns.
- `SimulationRuntime.InitializeNpcRosterCensusProtocol` currently registers
  the partial fixed and dynamic census providers, seals their inventories,
  registers `runtime.npc-membership`, seals the operation inventory, binds to
  `Thread.CurrentThread`, and establishes an initial baseline.
- `ContinuationCensusProtocol.TryEnterOperation` rejects/faults a call from a
  non-owner thread and increments a registered operation count. Its
  `SimulationOperationScope.Dispose` decrements the count on the bound thread.
  Assessment returns `OperationInProgress` whenever the count is nonzero.
- `BeginNpcMembershipCensusScope` nests the existing membership scope and
  closes it after membership reconciliation. Keep that scope and its exact
  commit semantics unchanged; it naturally nests inside bootstrap or advance
  scopes when those outer scopes are active.
- `SimulationRuntime.TryAdvanceDay`, `TryAdvanceDays`, and
  `TryAdvanceIntradayTo` use `AdvanceLease`. The lease rejects reentrant
  advances, but its own comments and behavior expressly do not make it a
  cross-thread lock. Keep its Boolean/reentrancy behavior unchanged.
- P18 intraday advancement can finish timeline clock movement before the
  runtime's synchronous post-success reconciliation/handoff finishes. The
  outer admission scope must cover both the timeline core and all of that
  handoff work. Do not add a separate scope inside the P18 clock core that
  would close before the handoff.
- Existing zero-day `TryAdvanceDays(0)` is a successful no-op; negative day
  counts fail as invalid. These semantics remain unchanged.

## Proposed contract

### 1. Capture and bind the Unity startup thread explicitly

At the beginning of `TesteSimulacao.Start`, capture both the `Thread.CurrentThread`
object and its `ManagedThreadId`, before `InitializeSimulation` performs any
genesis stage. Retain that immutable expected identity for the lifetime of the
component. Do not infer it later from whichever thread happens to construct a
runtime, and do not use a synchronization context or thread ID alone as the
identity check.

Pass that exact expected thread identity into the `SimulationRuntime` created
by this bootstrap. During runtime census-protocol setup, verify by reference
identity and managed ID that the current thread equals the captured `Start`
thread, then bind the protocol to that expected thread. Binding must be
explicit; a mismatched or absent expected binding fails closed and must not
silently fall back to the constructor's current thread. Test-only direct runtime
composition may pass its explicitly selected current thread as the expected
owner; it must use the same binding contract.

`TesteSimulacao.Update` verifies it is on the captured startup thread before it
can call `Simulate`. Regardless, each public day/intraday admission path in
`SimulationRuntime` also verifies the protocol owner thread before acquiring
the existing advance lease or mutating any clock/domain state. Thus callers
that bypass `Update` cannot advance from a worker thread. Wrong-thread admission
faults the partial protocol closed and returns its existing typed failure (or
throws through the existing `AdvanceDay` wrapper); it performs no advance.
Wrong-thread scope disposal also follows the existing protocol fail-closed
behavior.

The protocol baseline is established only after fixed and dynamic providers,
expected sections, and the complete set of operation IDs in this bounded
adapter have been registered and sealed. This baseline remains a baseline for
the currently registered partial census, not proof of the complete profile.

### 2. Hold one bootstrap operation scope through publication

Register one stable operation ID, for example `runtime.bootstrap-publication`,
in the protocol operation inventory before it is sealed. Immediately after the
initial census baseline succeeds, `SimulationRuntime` enters that operation
scope and retains it as its bootstrap scope. Starting it before the baseline
would make the initial assessment busy; starting it after publication would
leave the authored-genesis gap unaccounted.

The scope begins after protocol baseline and before the remaining authored
genesis/profile validation work continues. It remains active while
`SimulationGenesisPipeline` executes all remaining stages, including authored
world creation, runtime composition and validation, and publication. Once
`publishedComposition` has been assigned and `ExecuteStages` returns normally,
`TesteSimulacao` calls a runtime completion method that verifies the expected
thread and published composition, then disposes the bootstrap scope. The
completion call must be after successful return from the entire pipeline, not
inside the `publish` stage: stage callbacks and code after assignment are still
synchronous startup work.

Wrap startup completion in `try/finally`. If any stage, publication callback,
or post-publication startup step throws, or the pipeline returns without a
published composition, mark runtime admission/protocol faulted and keep the
bootstrap incomplete. A scope may be disposed in the failure path only after
the protocol is faulted; disposing it must never make the failed runtime look
quiescent/admissible. The `TesteSimulacao` instance must not retry into or
expose that failed runtime as a healthy published world. Preserve the original
exception behavior after recording the fail-closed condition.

### 3. Add outer operation scopes for actual advances

Register separate stable operation IDs for the outer day-advance API and the
outer intraday-advance API (including `runtime.advance-day` and
`runtime.advance-intraday`, or equivalent fixed identifiers). Keep
`runtime.npc-membership` distinct and nested. Do not register a generic caller
supplied operation name.

For `TryAdvanceDay`, `TryAdvanceDays(dayCount > 0)`, and
`TryAdvanceIntradayTo`, first perform non-mutating argument/profile/reentrancy
preflight and verify the bound owner thread. A wrong-thread call faults and
returns before clock or domain work. Preserve the existing P18 advance lease:
if it is already held, return the current reentrant/busy failure without
starting a new outer operation. Once an otherwise supported nonzero outer
advance is admitted, enter exactly one corresponding census operation scope
and acquire/use the existing lease according to current failure semantics.
Close the census scope in `finally` on success, ordinary failure, or thrown
exception.

For `TryAdvanceDays(dayCount > 0)`, the one scope covers the complete requested
batch, every successful daily boundary in the loop, and any subsequent
post-success work; it is not opened and closed around each internal day. A
negative count returns its existing invalid-count failure without a scope, and
zero remains a no-op with no scope or epoch effect. A call rejected because the
runtime is unsupported/faulted or because the existing lease is already held
does not enter a scope. This preserves the distinction between admission of a
real operation and a no-op/rejected call.

For P18, the day API's scope covers the next-day timeline target through
successful advancement and all synchronous P18 handoff/reconciliation before
the public method returns. `TryAdvanceIntradayTo` similarly covers input
sealing, causal timeline steps, timeline-success handoffs, pending post-advance
handoff completion, and runtime post-success work. Scope disposal is in the
outer public method's `finally`, after the existing `AdvanceLease` cleanup.
No change is made to P18 timeline semantics, timeline-owned state, bool lease
reentrancy, accepted input order, continuation protocol, or P18 failure mapping.

If an exception occurs after time advances but before post-success handoff is
complete, the scope remains active until stack unwinding reaches `finally`,
then closes on the captured owner thread. Existing domain/runtime fault policy
continues to determine whether the runtime may advance again; this design does
not turn a partially completed advance into a success or rollback it.

## Fail-closed behavior

Any of the following prevents further admitted advances and makes partial
census assessment fail closed: inability to bind the expected Unity startup
thread; binding/current-thread mismatch; missing operation registration;
attempted operation entry from another thread; impossible scope accounting;
bootstrap exception or missing publication; or disposal/exit on a different
thread. A failed bootstrap cannot publish a healthy runtime. A wrong-thread
advance cannot acquire the domain mutation path or move `SimulationTime` or the
P18 `CurrentInstant`.

Protocol operation counts are not synchronization. The bounded contract relies
on Unity's normal serialized main-thread callback execution and rejects
off-thread runtime admission. It does not block or make direct mutable store
references thread-safe. Out-of-band writes that bypass an instrumented owner
commit remain outside this adapter and must be resolved through the separate
P12-B writer/invalidation map.

## Validation matrix for a later implementation candidate

Tests should use the existing protocol/runtime/bootstrap fixtures where
possible and prove both behavior and unchanged boundaries:

| Case | Required evidence |
|---|---|
| Unity `Start` binding | Captured thread reference and managed ID are passed to the composed runtime; baseline succeeds only on that exact thread. A mismatched explicit binding fails closed. |
| Bootstrap in progress | From a stage callback after baseline and before publication, census/quiescence assessment reports `OperationInProgress`; it becomes idle only after successful full pipeline return and completion. |
| Successful publication | Published composition exists before bootstrap scope closes; later census assessment can succeed and reports active count zero. |
| Bootstrap exception before publication | Protocol/admission becomes faulted, no healthy composition is exposed, active scope cannot be mistaken for successful quiescence, and original startup failure propagates. |
| Exception after assignment/before pipeline return | Bootstrap still fails closed; stage callback exception does not leave a published composition treated as ready. |
| `Update`/direct runtime wrong thread | No day, timeline instant, or domain owner advances; protocol/admission faults or returns the existing typed owner-thread failure. Verify both the Unity entry guard and direct runtime entry guard. |
| Accepted day advance | Operation count is nonzero during day-boundary/domain work and returns to zero in `finally`; an NPC membership mutation nests without closing the outer scope early. |
| Accepted multi-day advance | One outer scope spans all requested days and releases after the final day or a later-day failure. |
| P18 intraday success | Scope remains active through timeline completion and every synchronous successful-advance handoff, then becomes idle. |
| P18 day-boundary success | The day API's scope covers the P18 timeline path and its post-success handoff. |
| Reentrant advance | Existing `AdvanceLease` rejection and failure mapping remain unchanged; no nested outer scope is leaked. |
| Zero/invalid/no-op | `TryAdvanceDays(0)` remains true, advances nothing, and enters no operation scope; negative count and invalid/unsupported operations preserve existing failure semantics without mutation. |
| Ordinary failure and thrown exception | Every admitted scope closes via `finally`; existing timeline and mutation-guard failure behavior is unchanged. |
| Existing P18 semantics | Existing P18 input, continuation, reentrancy, failure mapping, and handoff suites pass unchanged. |

## Remaining P12-B obligations after this adapter

This adapter would close only the runtime thread/admission/quiescence slice
for the explicitly named scopes. It does not establish that the selected
profile has every expected owner/cardinality, nor that all successful commits
to those owners notify the shared epoch. In particular, the existing passive
census stack—including dynamic NPC/Person membership and Inventory witnesses,
City NPC-presence, geography/network and other promoted owners—remains partial.
The live profile still needs a complete owner/cardinality census and exact-zero
witnesses; remaining direct/indirect writes still need mapping to owner commit
notifications or explicit exclusions. Uninstrumented operations must not be
claimed as covered by bootstrap/day/intraday scopes merely because they happen
on the same thread.

The design does not issue a capture token, define save bytes, add owner
registration for uninstrumented commits, add export/staged hydration, establish
P12-A readiness, authorize P12-A implementation, close P12-B, or change any
P18 time/lease semantics.
