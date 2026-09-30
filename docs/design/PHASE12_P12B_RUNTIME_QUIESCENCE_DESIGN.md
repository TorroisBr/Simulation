# P12-B Runtime Admission and Quiescence Adapter — Technical Design

**Status:** Proposed bounded prerequisite design; implementation and independent
design review are pending. This document does not declare P12-B complete or
P12-A ready.

**Evidence base:** Phase 12 canonical `19d0373d6a71b63536248ecc9091e66c9b3a708b`,
including `docs/PHASE12_STATE.md`, `docs/phases/PHASE12_BRIEF.md`, and
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`, plus the current
`SimulationRuntime`, `SimulationTime`, `TesteSimulacao`, and
`ContinuationCensusProtocol` implementation.

## Purpose and boundary

The accepted `UnityBootstrap-Daily-v1` profile needs runtime-owned proof that
its supported publication and completed-day advancement are admitted on the
Unity `Start` thread and that a census cannot be assessed while one of those
operations is still executing. The existing census protocol binds to a managed
thread and counts explicitly registered synchronous scopes. `SimulationRuntime`
currently binds to the thread on which its constructor runs and registers the
nested NPC-membership operation. `TesteSimulacao.Start` does not retain an
explicit thread identity or hold a scope through publication. The public
`SimulationTime.AdvanceDay` and `TryAdvanceDay` methods can also write the
runtime-bound clock outside the full runtime day operation.

This design adds only the runtime admission adapter for the selected
`UnityBootstrap-Daily-v1` profile, and only for:

1. the validation-tail and publication portion of the existing synchronous
   `TesteSimulacao.Start` bootstrap, after `SimulationRuntime` and its partial
   census baseline exist;
2. the existing nested `runtime.npc-membership` operation;
3. nonzero outer daily advances through `SimulationRuntime.AdvanceDay`,
   `TryAdvanceDay`, and `TryAdvanceDays`; and
4. calls to public `SimulationTime.AdvanceDay` / `TryAdvanceDay` on the
   `SimulationTime` instance owned by this runtime, routed through that same
   outer daily-advance operation when, and only when, the runtime's selected
   profile is `UnityBootstrap-Daily-v1`.

The adapter is single-thread admission and operation counting, not a lock and
not a substitute for owner-store synchronization. It does not establish
exhaustive owner coverage, map every writer to invalidation, make uninstrumented
commits safe, register arbitrary operations, or issue capture eligibility.
The live owner/cardinality census and committed-write invalidation obligations
remain separate P12-B blockers. P12-A remains `WAIT_DEPENDENCY` until all of
its prerequisites and separate authorization are satisfied.

The accepted profile is daily and excludes intraday state. This adapter covers
completed daily boundaries only and does not extend the supported profile.
The adapter must be enabled by an explicit selected-profile predicate, not by
the mere fact that a `SimulationRuntime` owns a `SimulationTime`. For
`UnityBootstrap-Daily-v1`, install the private clock callback and use the
`runtime.advance-day` outer admission scope. For a P18-backed timeline profile,
do not install this callback or apply this P12 admission wrapper: there,
`SimulationTime.AbsoluteDay` is a timeline projection,
`SimulationTime.TryAdvanceDay` must continue to return
`TimelineProjectionOwnsClock`, and `SimulationRuntime.TryAdvanceDay` must
advance the P18 timeline under its own contract. P12 adds no P18 operation
scope or temporal behavior.

## Existing behavior and constraints

- `TesteSimulacao.Start` calls `InitializeSimulation`, which runs the authored
  genesis stages synchronously. `SimulationRuntime` is constructed in
  `p9.genesis.validate-profile/v1`, after the authored-world, geography, and
  authored-actor stages. The runtime validates the selected profile, then
  `publishedComposition` is assigned in `p9.genesis.publish/v1`. That assignment
  occurs before the stage callback and before `ExecuteStages` returns.
- `SimulationRuntime.InitializeNpcRosterCensusProtocol` registers the partial
  fixed/dynamic census providers, seals those inventories, registers
  `runtime.npc-membership`, seals the operation inventory, binds to the current
  thread, and establishes an initial baseline. This remains a partial census.
- `ContinuationCensusProtocol.TryEnterOperation` requires its bound thread,
  increments a registered operation count, and fails/faults on a wrong-thread
  call. A `SimulationOperationScope` disposed on the wrong thread faults the
  protocol without allowing the count to appear quiescent.
- `BeginNpcMembershipCensusScope` nests the existing membership scope and
  closes it after membership reconciliation. Preserve its commit and mutation
  notification behavior unchanged.
- `SimulationRuntime.TryAdvanceDay` advances the clock and then executes the
  daily domain sequence. `TryAdvanceDays(0)` is a successful no-op; negative
  counts fail as invalid. Preserve these results and do not open a scope for
  zero, invalid, or otherwise rejected requests.
- The public `SimulationTime.AdvanceDay` and `TryAdvanceDay` currently mutate
  `absoluteDay` directly when the mutation guard permits. `SimulationRuntime`
  exposes its `SimulationTime`, so these are real write paths that must not
  bypass the completed-day operation boundary for a runtime-owned clock.
- Canonical already includes the passive `TravelParty` witness at promotion
  `b78a271f9c552b40bade1a45168388eafa670f59` (code/test tip
  `260a688f7ea1a9f86e9b589788cda2c32357e170`, exact-tip review
  `e8910587fdd894a5090c4208b30c5e834acb528f`). It binds to the exact installed
  `TravelPartyStore`, reports active party-instance cardinality and owner-local
  revision, and the selected bootstrap profile expects cardinality zero. Keep
  this existing exact-zero/stable-owner witness as profile evidence; do not add
  it to or otherwise widen the partial `ContinuationCensusProtocol` inventory.

## Proposed contract

### 1. Capture and bind the actual Unity `Start` thread

At the beginning of `TesteSimulacao.Start`, before the first genesis stage,
capture both the `Thread.CurrentThread` reference and its
`ManagedThreadId`. Retain this immutable expected identity for the component.
Do not infer it later from whichever thread constructs the runtime, and do not
use a synchronization context or a managed ID alone as thread identity.

Pass the captured identity into the `SimulationRuntime` created during the
validation stage. Runtime census-protocol setup verifies by thread-reference
identity and managed ID that construction is occurring on the captured
`Start` thread, then explicitly binds the protocol to that expected thread.
A missing or mismatched expected identity fails closed; it must not silently
fall back to the constructor's current thread. Direct runtime test fixtures
must supply their selected owner thread explicitly and use the same binding
contract.

The Unity genesis pipeline is synchronous, so the authored-world, geography,
and actor stages preceding `SimulationRuntime` construction run on the thread
captured at `Start`. They occur before the runtime census protocol and its
operation scope exist; this adapter does not claim to census or scope those
earlier stages. Binding at runtime construction verifies the synchronous
pipeline has not moved to another thread. The operation scope begins only after
the runtime's initial partial-census baseline and protects the remaining
validation tail and publication.

`TesteSimulacao.Update` verifies the captured thread before calling `Simulate`.
Each runtime daily-advance entry also verifies the bound protocol owner thread
before any clock/domain mutation, so callers that bypass `Update` cannot
advance from another thread. Wrong-thread admission faults the partial
protocol closed and returns the runtime's existing typed failure (or throws
through `AdvanceDay`); no day or domain owner advances.

### 2. Scope the validation tail through publication

Register one fixed operation ID such as `runtime.bootstrap-publication` before
sealing the protocol operation inventory. Register the fixed daily-advance
operation ID from section 3 at the same time. After
provider/operation inventories are sealed and the initial partial-census
baseline succeeds, enter and retain the bootstrap operation scope.

Because `SimulationRuntime` is created in `p9.genesis.validate-profile/v1`,
this scope begins at the validation tail, after the authored-world, geography,
and actor stages have completed. It remains active through the rest of profile
validation, `publishedComposition` assignment in the publish stage, the stage
callback, and normal return from the complete `SimulationGenesisPipeline`.
Only after that return, and after verifying that the component is still bound
to the captured thread and `publishedComposition` is present, does
`TesteSimulacao` close the bootstrap scope. Do not close it inside the publish
stage: assignment precedes callbacks and is not the full startup boundary.

Wrap all of `InitializeSimulation` in a permanent fail-closed bootstrap latch.
Set the latch before rethrowing any exception or reporting a missing
publication. If failure occurs after `publishedComposition` was assigned,
revoke publication by clearing `publishedComposition` and any cached/public
composition exposure, fault the runtime protocol/admission, and keep the
bootstrap marked failed. The component must never retry genesis or expose the
previously assigned composition as healthy. This includes a stage-callback
exception after assignment. Preserve the original exception behavior after
revocation/fault recording.

On any startup failure after the bootstrap scope begins, fault the protocol
before disposal. A faulted protocol with a stranded active count cannot report
successful quiescence. If failure occurs before runtime creation, the component
latch still prevents a later retry; no protocol exists yet to fault. A
successful startup closes the scope exactly once after full pipeline return.

### 3. Scope daily advances and route direct clock writes through them

Only when the runtime's selected-profile predicate identifies
`UnityBootstrap-Daily-v1`, register and use one stable, closed operation ID
for the outer runtime day operation, for example `runtime.advance-day`. Keep
`runtime.npc-membership` separate and nested. No caller-supplied operation ID
is allowed. P18 timeline-backed profiles do not register or enter this P12
operation; their `SimulationRuntime.TryAdvanceDay` follows the P18 timeline
contract.

For `UnityBootstrap-Daily-v1`, `SimulationRuntime.TryAdvanceDay` and
`TryAdvanceDays(dayCount > 0)`:
verify owner-thread identity and perform non-mutating validity/reentrancy
preflight before entering the corresponding operation scope. Once admitted,
the one outer scope covers the full operation: clock advancement, every daily
domain system, nested NPC membership, and all synchronous post-clock work. For
`TryAdvanceDays`, one scope spans the whole requested batch and closes in
`finally` on success, ordinary failure, or exception. A negative count,
zero count, faulted runtime, or rejected/reentrant request opens no scope and
preserves existing no-op/failure behavior.

When, and only when, the runtime's selected profile predicate identifies
`UnityBootstrap-Daily-v1`, bind its `SimulationTime` to a private owner
callback so public `SimulationTime.AdvanceDay` and `TryAdvanceDay` route
through the runtime's normal full-day advance admission instead of writing the
clock independently. A direct call on this profile's runtime-owned clock
therefore has the same completed-day semantics and owner-thread/quiescence
scope as `SimulationRuntime.AdvanceDay` / `TryAdvanceDay`. The runtime's own
daily core uses a separate internal clock-commit primitive to avoid recursive
dispatch. The callback is a fixed runtime binding, not a public
operation-registration hook. For any other profile, this P12 design neither
installs nor changes clock dispatch. In particular, a P18-backed runtime
retains timeline ownership and projection behavior. A detached, unbound
`SimulationTime` retains its standalone clock behavior and is not part of
`UnityBootstrap-Daily-v1`.

For `UnityBootstrap-Daily-v1`, the runtime callback dispatches directly to the
same `TryAdvanceDay` entry, so the single `runtime.advance-day` operation scope
covers calls through either public API. It does not publish an intermediate
point where the clock has advanced but daily domain work is incomplete.
Attempts to enter the same runtime recursively are rejected by existing
runtime reentrancy semantics. Map runtime failures to an explicit
`SimulationTimeAdvanceFailure` result (or equivalent typed internal result)
for `TryAdvanceDay`; the void `SimulationTime.AdvanceDay` wrapper continues to
throw on failure. Overflow and runtime-faulted cases retain their existing
meanings. These callback and result-mapping requirements do not apply to P18
timeline-backed profiles.

## Fail-closed behavior

Failure to bind the expected `Start` thread, wrong-thread admission, missing
operation registration, impossible scope accounting, failed bootstrap,
missing publication, or wrong-thread scope disposal prevents successful
quiescence assessment. A failed bootstrap is permanently latched and any
partially assigned publication is revoked. Wrong-thread calls cannot move the
runtime clock or advance domain state. The direct public clock methods on a
runtime-owned instance cannot bypass runtime admission; standalone unbound
clock instances are outside this profile's capture scope.

Operation counts are not synchronization. The adapter relies on Unity's normal
serialized main-thread callbacks and rejects off-thread runtime admission. It
does not lock owner stores or make direct store references thread-safe.
Out-of-band writes that bypass an instrumented owner commit remain separate
P12-B writer/invalidation blockers.

## Validation matrix for a later implementation candidate

| Case | Required evidence |
|---|---|
| Unity `Start` binding | Captured thread reference and managed ID are passed into the composed runtime; binding succeeds on that exact thread and mismatched/missing binding fails closed. |
| Earlier genesis stages | Assert stage order and that they run synchronously on the captured `Start` thread. Do not claim a census operation scope before the runtime exists. |
| Validation-tail scope | A callback after baseline and before publication observes `OperationInProgress`; the scope becomes idle only after successful full pipeline return. |
| Successful publication | Composition assignment precedes scope close; afterward assessment can succeed and active operation count is zero. |
| Exception before runtime creation | Permanent component latch prevents a second `Start`/retry from creating another world. |
| Exception during validation before publication | Protocol faults; bootstrap stays failed; no healthy composition is exposed; original exception propagates. |
| Exception after composition assignment | Revoke `publishedComposition` and public exposure, latch bootstrap failed, fault runtime admission, and ensure repeat startup cannot retry or expose the assigned object. |
| `Update`/direct runtime wrong thread | No day or owner advances; protocol faults or returns the typed owner-thread failure. Verify both Unity entry and direct runtime entry. |
| Accepted day advance | Operation count is nonzero during clock and daily domain work, including nested NPC membership, then returns to zero in `finally`. |
| Multi-day advance | One outer scope spans all days and releases after the final boundary or later-day failure. |
| Direct `SimulationTime.TryAdvanceDay` | On the runtime-owned clock, dispatches through the full runtime daily advance; during callbacks census reports `OperationInProgress`, no half-advanced clock boundary is observable, and the scope returns to zero. |
| Direct `SimulationTime.AdvanceDay` | Uses the same runtime dispatch and exception behavior; wrong-thread and reentrant calls do not mutate the clock. |
| Selected-profile clock dispatch | For `UnityBootstrap-Daily-v1`, the selected-profile predicate enables callback installation and outer admission; for a P18 timeline profile, no P12 callback is installed, `SimulationTime.TryAdvanceDay` returns `TimelineProjectionOwnsClock`, and `SimulationRuntime.TryAdvanceDay` advances the timeline. Assert both branches explicitly so the daily adapter cannot intercept or reinterpret P18 clock semantics. |
| Zero/invalid/no-op | `TryAdvanceDays(0)` remains true with no time/domain change and no scope/epoch effect; negative count and rejected requests preserve existing failure behavior. |
| Failure and exception | Every admitted outer scope closes in `finally`; runtime faults and incomplete work are not reported as a successful daily boundary. |
| Canonical TravelParty witness | Retain the promoted candidate's existing selected-bootstrap assertion of exact-zero active parties and exact installed `TravelPartyStore` owner identity/revision. Do not add a TravelParty section to or otherwise widen `ContinuationCensusProtocol`. |
| Existing daily behavior | Relevant legacy day-advance, `SimulationTime`, bootstrap, census, and complete Smoke suites pass. |

## Remaining P12-B obligations after this adapter

This adapter would close only the owner-thread and admission/quiescence slice
for the named startup tail and daily operations. It does not establish that the
selected profile has every expected owner/cardinality, nor that all successful
commits notify the shared epoch. The promoted census stack, including the
TravelParty exact-zero/stable-owner witness, is still only partial live-profile
evidence. The full profile owner inventory, remaining exact-zero witnesses,
supported writer map, and invalidation wiring remain open. A same-thread
operation is not covered just because it occurs during startup or a day; only
the specifically named scopes are accounted for.

The design does not issue capture eligibility, define save bytes, add generic
owner registration, provide export/staged hydration, establish P12-A
readiness, authorize P12-A implementation, or close P12-B.
