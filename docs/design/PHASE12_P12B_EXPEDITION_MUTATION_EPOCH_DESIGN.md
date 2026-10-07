# P12 Expedition mutation and shared-epoch boundary — design candidate

**Status:** Current-base technical-design candidate, explicitly dependency-
deferred. This is not implementation-ready. P12-F active-commitment work stays
behind P12-C, P12-D, and P12-E; this design neither implements nor advances
that dependency. No P12-B, P12-A, P13, save, or capture-eligibility readiness
is claimed.

## Checkpoint, base, and decision

- **Current P12-B boundary:** profile admission and completed-boundary
  lifecycle, under the accepted `UnityBootstrap-Daily-v1` profile. This design
  does not accept or authorize the deferred P12-F implementation scope.
- **Owner family:** Expedition/active commitments, assigned to P12-F by the
  current canonical capability matrix. P12-F is blocked on C/D/E.
- **Design base:** P12 canonical
  `94551b08be8cc9347de35eae5051b8e578ea4c1e`, branch
  `codex/phase12/canonical`.
- **Baseline authorities at that commit:**
  - `docs/SIMULATION_ARCHITECTURE.md` blob
    `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`.
  - `docs/ROADMAP.md` blob
    `f9bb445880948b5e493fbf7f5682c38a33d18589`.
  - `docs/phases/PHASE12_BRIEF.md` blob
    `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`.
  - `docs/PHASE12_STATE.md` blob
    `b3d278f71d36b8c1f3e7366f21c4632ed4d881fd`.
  - `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md` blob
    `3c3a1c0d722099da08b5c22fdb86e0f6e388c5d9`.
  - `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` blob
    `7872d9365b9cb9286080073f90970f2c12a5835c`.
  - `docs/design/PHASE12_F_TECHNICAL_DESIGN.md` blob
    `916310bc45e35822042fa94b0ddf191f17e930c6`.
  - `docs/EXECUTION_MODEL.md` blob
    `9d007aa93e602a2f8242000d3c60864614b049f3`.

The canonical matrix supersedes the older October 3 “Expedition start next”
sentence. It says P12-F owns the Expedition/active-commitment owner and directs
the orchestrator not to schedule Expedition work ahead of C/D/E. The design
below preserves that deferral. It records the concrete mutation boundary and
future validation contract so that the source finding is not lost; it does not
declare an implementation checkpoint ready.

## Current source boundary

The following source paths were inspected at the exact design base:

- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Scripts/ExpeditionSystem.cs`
- `Assets/_Project/Scripts/ExpeditionStore.cs`
- `Assets/_Project/Scripts/ExpeditionRuntime.cs`
- `Assets/_Project/Scripts/ExpeditionCensusProvider.cs`
- `Assets/_Project/Scripts/TravelParty.cs`
- `Assets/_Project/Scripts/P12TravelPartyStartOperation.cs`
- `Assets/_Project/Scripts/ContinuationCensusProtocol.cs`
- `Assets/_Project/Scripts/RuntimeIdentity.cs`

`TesteSimulacao.Start` publishes the selected profile composition. Genesis
creates one `ExpeditionStore`; `RebuildSystems` creates an
`ExpeditionSystem` over that same object; `SimulationBootstrapComposition`
checks owner/system identity and exposes both the store and the passive
`ExpeditionCensusProvider`. After publication, the public
`TesteSimulacao.TryStartExpedition` method calls the composed system. The public
`Expeditions` property also exposes the store. Thus the owner is in the
selected-profile composition and its public mutation APIs are reachable after
publication; a passive provider alone does not bind those writes to the
protocol.

There is an important current P12 integration constraint. The profile-bound
`TravelPartySystem` has `BindP12StartOperationAdmission`; its P12 callback
requires a request established by `SimulationRuntime.TryStartTravelParty` /
`TryStartP12TravelParty`. The public Expedition system instead invokes
`TravelPartySystem.TryStartTravelParty` directly. `TryAdmitP12TravelPartyStartPreparation`
rejects that direct call when no P12 request is active. Consequently a
post-publication `TryStartExpedition` attempt can successfully commit
`ExpeditionStore.AddAndFence`, have the nested Party start rejected, then
successfully compensate with `RemoveReserved`. It returns false, but those
two Expedition owner commits have each advanced its local revision. They have
no Expedition section notification in the P12 shared epoch. This is a real
stale-epoch path even though a successful Expedition start is not currently
admitted through that nested Party path.

The separate daily autonomy path is not established by the selected bootstrap:
`SimulationRuntime` invokes `adventureExpeditionAutonomySystem` conditionally,
but `TesteSimulacao` does not construct or pass that optional system. Do not
claim autonomous Expedition advance/start is a normal Daily-v1 writer on this
base. `SimulationRuntime` does call `ExpeditionSystem.ReconcileAfterTravel`
from the travel lifecycle when applicable; that system path is included in
the writer classification below.

## Writer inventory and disposition

The proposed future P12-F owner boundary is one exact `ExpeditionStore`
(`p12f.expeditions`, schema v1, `ExpeditionCensusProvider`) and its existing
owner-local monotone revision. That section name and protocol registration are
not present in the current sealed profile: bootstrap exposes the passive
provider, but does not register it in the protocol. A future implementation
must attach P12 observation to the actual store installed in the published
composition, never a reconstructed lookalike. The following paths cannot be
declared unsupported by documentation alone:

| Source path | Current successful owner writes | Design disposition |
|---|---|---|
| `ExpeditionStore.Add` | Attaches/indexes an Expedition and increments revision. | Cover as a public direct write when the store is P12-bound. |
| `ExpeditionStore.Remove`, `Complete`, `TryFinalizeCompletion` | Remove or finalize an exact stored owner and increment revision. `ExpeditionRuntime.TryComplete` reaches `TryFinalizeCompletion`. | Cover each successful commit through the same owner boundary. A false/no-op return has no revision notice. |
| `ExpeditionRuntime.TryBeginExploration`, progress/place/connection/observation methods, objective completion, begin-return, and completion | Attached instances route their state change through `ExpeditionStore.TryMutate` or `TryFinalizeCompletion`; `TryMutate` increments owner revision only when the owner snapshot changes. The store exposes a read-only list whose elements remain live mutable runtimes. | Cover the central attached-store commit path, not each facade separately. Verify snapshot-changing and no-op cases. Unattached standalone objects retain existing behavior. |
| `ExpeditionSystem.TryStartExpedition` | `TryReserveNew` reserves two local owner revisions; `AddAndFence` commits the new owner; direct nested Party start can fail P12 admission; `RemoveReserved` may commit compensation. On success, `CommitReserved` associates the new Expedition with the Party. Each successful reserved commit increments local revision. | Before any first commit, enter the named Expedition operation and validate exact owner/protocol state. Notify after every successful local owner commit, including compensation even when the public operation returns false. Do not defer notification until the whole start returns success. Route the nested Party start through an explicitly admitted P12 composition; do not silently bypass its existing callback. |
| `ExpeditionSystem.TryBeginReturn` and return reconciliation | Reserved commits mutate return state; `TryStartTravelParty` is also called directly; failure may commit cancellation compensation. `SimulationRuntime.ReconcileAfterTravel` may reach this path. | Same owner preflight/commit/notification rules. Party start must use its own registered P12 start contract nested under a tracked Expedition operation, or the owner mutation must fail before its first commit. No semantic change to current compensation ordering. |
| `TryRetrieveTargetResource`, `TryRetrieveNotableItem`, `TryResolvePlaceOpposition`, exploration/traversal, and `ReconcileAfterTravel` | The system can commit Expedition state and also mutate PlaceContent, performer Inventory/Knowledge, Party, or event/sequence owners. Objective completion uses `CommitReserved`; attached runtime progress uses `TryMutate`. | Expedition-local commits use the same owner boundary. Cross-owner operation scopes and each non-Expedition owner’s own notification remain owner-specific P12-F integration work; this design does not claim those operations complete. |
| `TesteSimulacao.Expeditions` / `SimulationBootstrapComposition.Expeditions` callers | Public `Add`, `Remove`, `Complete`, `TryFinalizeCompletion` remain callable directly after publication; they bypass `ExpeditionSystem`'s `MutationGuardBinding`. | Cover successful direct writes in the bound profile, preserving the public API and standalone behavior. No canonical P12 contract marks these methods unsupported or promises callers will not use them. Do not label them unsupported unless a later reviewed profile admission contract and executable fail-before-write boundary establish that restriction. |
| `RuntimeIdAllocator.AllocateExpeditionId` and event recording | Identity allocation precedes the Expedition commit; start/return may also record events/sequences. | Separate owner sections and existing P12-C/record-sequence/Event counter responsibilities. This design does not relabel or silently absorb them into `p12f.expeditions`. Their exact registration remains a dependency to verify before a start path can be called fully covered. |

The daily autonomous writer is currently conditional and not composed by
`TesteSimulacao`; it is excluded from the current profile path list. A future
change that composes it must refresh this inventory. The system remains publicly
exposed, so its other public methods are not automatically excluded merely
because autonomous daily execution is absent.

## Proposed implementation boundary after dependency refresh

This is a target contract for later work, not authorization to edit the shared
runtime hotspot now.

1. **Exact binding.** Bind one mutation boundary to the exact
   `ExpeditionStore` and `p12f.expeditions` provider from the published
   composition. Require the expected schema, owner identity, role, baseline
   revision, and current owner thread. A split store/system/provider or
   missing section faults admission before a write.
2. **Direct and system commits.** Instrument successful `Add`, `Remove`,
   `Complete`, `TryFinalizeCompletion`, `TryMutate`, `AddAndFence`,
   `CommitReserved`, and `RemoveReserved` at their shared owner commit points.
   Perform owner/protocol preflight before changing owner state. After the
   local revision has advanced, report exactly the Expedition section through
   the existing P12 protocol. A no-op/rejected write changes neither local
   revision nor protocol epoch. If post-commit reporting fails, keep the
   committed owner truth, fault the runtime admission closed, and do not
   pretend the domain write rolled back.
3. **Thread and stale-state checks.** Use the runtime’s already bound P12
   owner-thread identity and the registered provider baseline; reject off-owner
   direct calls before mutation. Validate the exact installed store/provider,
   local revision/census and protocol epoch capacity for each write. Do not
   rely on the `ExpeditionSystem` health-only mutation guard as a P12 epoch.
4. **Tracked multi-owner ingress.** Register a bounded operation for the
   supported Expedition start ingress. Enter it before `AllocateExpeditionId`
   or `AddAndFence`; close it on all return/throw paths. Teach the P12
   TravelParty admission bridge to recognize only this exact active
   Expedition-start request and then enter its existing
   `runtime.travel-party.start` operation with its existing exact Party,
   allocator, sequence, NPC, City-presence and other required changed-owner
   set. The protocol permits operation-count nesting, but current runtime
   mutation batching supports at most one batch context; keep the Expedition
   tracker as an outer activity scope and notify Expedition commits directly
   at their store commit points. Do not merge changed sets or suppress the
   Party operation’s own notification. Reject direct unscoped Party starts as
   today.
5. **Failure and compensation.** Preserve the current reservation budget and
   call order. If the Party admission fails before a Party commit, each
   preceding successful Expedition commit and successful compensation still
   has its own owner revision and shared-epoch notification. If any post-commit
   notification fails, close admission; never return a healthy capture state
   over an unreported commit. A later complete operation must not reuse a
   stale owner baseline.
6. **Scope boundary.** Bind the same owner boundary to all public direct
   store methods and attached-runtime mutation funnels. The cross-owner
   resource/opposition/travel paths still need their P12-F operation and owner
   matrix; do not assert those operations are covered merely because the
   Expedition section notified. Export/hydration of active commitments,
   including detached immutable values rather than live runtime aliases,
   remains P12-F work.

The smallest central code seam is `ExpeditionStore`'s successful commit
methods because direct and system mutation funnels converge there. The smallest
public operation seam is an Expedition-start adapter that composes with the
already registered TravelParty start adapter. This is still one owner-family
design; it is not a general operation framework.

## Atomicity, recovery, and limits

Expedition start is not an all-or-nothing transaction across Expedition and
TravelParty: the existing code explicitly commits owner revisions and may
compensate. Preserve these existing semantics. The mutation epoch must reflect
each successful owner commit even when the enclosing method returns false;
do not defer its only notification to an all-success return. Party commits
remain covered by the TravelParty operation’s own baseline and changed-section
contract. The Expedition notice uses the Expedition provider and never counts
the Party revision as the Expedition revision.

The existing `OperationReservation` protects Expedition-store revision
capacity for its reserved local writes. It is not a shared protocol epoch
reservation. The shared protocol currently exposes preflight/capacity checks,
operation scopes, and post-commit notifications; preserve their exact
contracts. Any future change needing a mutation-epoch reservation must satisfy
the protocol’s active-operation constraints and receive its own review; do not
hold such a reservation across nested `TryEnterOperation` calls.

This adapter would prove only the observed Expedition owner writes in its
explicit boundary. It would not prove the complete effective profile census,
all P12-F cross-owner writers, all runtime operations, global quiescence,
successful completed-boundary token issuance, or save continuation. The
`ExpeditionCensusProvider` is not itself an export or a staged hydrator.

## Dependency and implementation sequence

1. Keep P12-F Expedition/active-commitment implementation deferred until the
   documented P12-C/D/E prerequisites are satisfied and the refreshed P12
   matrix permits that work. Preserve the current exact-zero/profile rejection
   rules while that owner remains unsupported for continuation.
2. Revalidate the design against the then-current architecture, P12 Brief and
   State, capability DAG, P12-B owner matrix, profile composition, and
   `SimulationRuntime`/TravelParty code. Reclassify if the selected-profile
   ingress or P12-F edge changed.
3. Only after the dependency refresh, obtain independent design review and
   confirm the shared `SimulationRuntime`, `ExpeditionStore`,
   `ExpeditionSystem`, and P12 TravelParty start hotspots are available. The
   review must explicitly approve the nested operation bridge and all public
   direct mutation behavior before implementation is dispatched.
4. Implement in an isolated current-base candidate; preserve standalone
   `ExpeditionStore`/system semantics. Keep P12-F immutable export and staged
   hydration as separate owner deliverables.
5. Require the validation listed below and independent exact-tip code review.
   Promotion and State refresh remain separate gates.

## Deferred candidate validation

No tests were run for this documentation candidate. A later implementation
candidate should add focused coverage for:

- Exact composition: provider and system bind to the published store; a
  mismatched owner/provider or missing requirement rejects before mutation.
- Direct store success: `Add`, `Remove`, `Complete`, and
  `TryFinalizeCompletion` advance their local revision and the P12 shared
  epoch once per committed operation; failed/no-op calls advance neither.
- Attached runtime mutation: every public state-changing
  `ExpeditionRuntime.Try*` path uses the attached store boundary; a
  snapshot-identical no-op does not notify, while each state-changing commit
  does. Read-only `TryGet`/validation methods remain reads.
- Reserved commits: successful `AddAndFence`, `CommitReserved`, and
  `RemoveReserved` preflight/notify exactly once; failed validation/action
  commits nothing. A failed Expedition start that commits AddAndFence then
  compensation invalidates both successful commits, despite returning false.
- Current nested Party behavior: raw direct P12-bound TravelParty start
  remains rejected. The Expedition wrapper either enters its explicitly
  reviewed registered nested Party operation or rejects before its first
  Expedition commit. It must not commit AddAndFence, discover the missing
  Party scope, then leave a healthy stale epoch.
- Successful nested start and return after integration: owner-thread and
  exact changed-owner baselines are checked; Party notification remains
  distinct; Expedition start/return commits and compensation are observable;
  no duplicate notifications or swallowed Party writes occur.
- Adversarial rejection: wrong thread, stale revision, wrong owner identity,
  protocol fault, local revision saturation, shared epoch exhaustion, and
  post-commit notification failure. Check no preflight failure mutates data;
  post-commit failure faults admission without pretending rollback.
- Public-profile exposure: calls through `TesteSimulacao.Expeditions`,
  `Bootstrap.Expeditions`, `ExpeditionSystem`, and attached
  `ExpeditionRuntime` all follow the same bound owner boundary. Unbound
  standalone fixtures retain their existing contract.
- Regression: affected Expedition/TravelParty suites, runtime admission,
  bootstrap composition, runtime orchestration, ALL EditMode, official Smoke,
  and `git diff --check`, each on the exact proposed code tree. P12-F export/
  hydration tests remain separately required and are not replaced by these
  invalidation tests.

## Readiness result

The exact canonical source shows a real mutation/invalidation seam, including
the compensated start failure and public direct store APIs. The technical
boundary is recorded for future dependency-safe work. However, the current
canonical P12 matrix assigns Expedition/active commitments to P12-F and
explicitly defers it behind C/D/E. This design therefore remains
`BLOCKED_DEPENDENCY` for implementation. It changes no checkpoint ID, does not
make an Expedition P12-B slice implementation-ready, and changes no
dependency/readiness label: P12-B remains `INCOMPLETE`, P12-A remains
`WAIT_DEPENDENCY`, P12-F remains blocked on C/D/E, and P13 remains blocked.
