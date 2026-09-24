# Phase 11 — Actor Choice Technical Design

**Status: proposed technical design; not implementation authorization.**
The Phase 11 entry contract is recorded in
[`PHASE11_ENTRY_ARCHITECTURE.md`](PHASE11_ENTRY_ARCHITECTURE.md), latest
reviewed commit `e96fa14` (independent PASS). This document proposes
interfaces, ownership, ordering, diagnostics, tests, and integration sequence
for that selected first consumer. All implementation units below remain
**UNAPPROVED**; Phase 11 has no approved checkpoint IDs or Phase State record.

**Design baseline:** Phase 11 entry branch commit `cc875d4` plus the reviewed
entry updates through `e96fa14`; canonical Phase 8 branch/code and State at
`ed7a40a86a6a16e9f4fda75703470c38135fda0e`. The current P8-D runtime and
diagnostics integration code commit is
`2faf3f873a7a434dc7330191e9292d09f6dd3014`; its latest unpromoted integration
branch HEAD is `6e964c90adfcda828c675e7a1a3f726279c2d298` on
`codex/phase8/P8DKnowledgeRoutePlanIntegration`.

## 1. Scope and authority

The slice is one choice by the trusted local single-player UI of the existing
`SellGoods` action for a living, Person-backed, materialized merchant with a
current city and no active `MerchantTradePlanRuntime`. When planning is
available, that choice replaces autonomous selection for one turn; an
unavailable choice follows the entry contract's autonomous fallback. The typed
choice carries only `PersonId` and the supported
`NpcActionData.DefinitionId`. Existing
merchant planning selects the item, quantity, and action runtime using that
actor's inventory and `CommercialKnowledge`; existing execution revalidates
the current market. One choice replaces one autonomous action selection.

This design does not add actor ownership grants, controller identity,
authentication, security, anti-cheat, anti-tamper, multiplayer, a general
actor-control API, remote trade, or a GM/external intervention. It does not
change `Suggest`, `Request`, `Declare`, or supported `ForceOutcome` meanings.
The player chooses an action; existing domain systems decide whether it can be
planned and executed.

The relevant semantic authorities are `SIMULATION_ARCHITECTURE.md` §§5–6,
81–85, the Phase 11 Brief, and the reviewed entry proposal. Architecture §81
says that controlling a person replaces autonomous choice while preserving
the person's information and leaving outcome authority with the domain.
Architecture §85 describes typed `WorldCommand` ingress for client inputs and
requires command causality to use payload and logical application order. The
Phase 11 Brief requires input capture through the supported
WorldCommand/domain boundary. The reviewed entry proposal reconciles these
contracts: a dedicated typed actor-choice `WorldCommand` queues the normalized
choice at the domain boundary; the separate actor decision/execution path
consumes it later. This actor-choice command is not a GM declaration or an
outcome command. The independent technical reviewer should verify this
reconciliation against the cited sections.

## 2. Proposed command and runtime interfaces

Names are proposed for discussion, not approved public API. The input payload
and actor-choice record are separate from both the sale action runtime and
its domain outcome.

| Owner | Proposed surface | Responsibility |
|---|---|---|
| `Assets/_Project/Scripts/WorldCommandFoundation.cs` | Add `WorldCommandKind.ActorActionChoice`, `WorldCommandOrigin.LocalPlayer`, and `ActorActionChoiceWorldCommandPayload(PersonId actorPersonId, string actionDefinitionId)` | Gives the trusted UI a typed command identity and stable payload. `LocalPlayer` is provenance only. The payload contains no item, quantity, price, target market, or outcome. |
| New `Assets/_Project/Scripts/ActorActionChoiceWorldCommandHandler.cs` | `ActorActionChoiceWorldCommandHandler : IWorldCommandHandler` | Read-only `Preview`; `Execute` accepts only `Request` and queues through the runtime. It does not create a sale, mutate inventory/money, update Knowledge, or claim sale success. `Declare` and `ForceOutcome` are rejected for this kind; `Suggest` remains preview-only under the existing service. |
| `Assets/_Project/Scripts/CoreWorldCommandHandlers.cs` | Add an explicit `RegisterActorActionChoiceHandler(WorldCommandService, SimulationRuntime)` entry point, separate from general core/GM handler registration | Allows the local actor-choice command to use the existing WorldCommand service contract without registering a new GM-console or natural-language command. The local UI composition calls this registration; `GMConsolePanel` and the translator gain no actor-control surface. |
| New `Assets/_Project/Scripts/ActorChoiceContracts.cs` and `ActorChoiceStore.cs` | `ActorChoiceInput`, `ActorChoiceInputId`, `ActorChoiceDisposition`, `ActorChoiceFailure`, `ActorChoiceStore` | Owns normalized input, per-world capture order, pending queue, deferral/rejection/dispatch/attempt lifecycle, mutation guard, clone, and invariant checks. It stores stable IDs and values, not `NpcRuntime`, `NpcActionData`, `ItemData`, or market references. |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `ActorChoiceStore` composition/property; an internal capture method used by the handler; input reconciliation; `EvaluateAction` hook; dispatch-start consumption and terminal returned/thrown outcome recording | Connects trusted command capture to the ordinary runtime actor-turn boundary and existing action path. Keeps authoritative mutations under the existing runtime guard and preserves action-execution exception propagation. |
| `Assets/_Project/Scripts/DecisionRecords.cs` | Add `NpcDecisionOrigin.ActorChoice` | Distinguishes the selected action's decision provenance from `Autonomous` and `ScheduledDirective`. The actor-choice input record remains the durable causal input; `NpcDecisionRecord` remains a separate decision record. |
| Diagnostics files described in §6 | Snapshot projection, canonical output, diff, formatting, and invariants | Makes pending and resolved causal inputs inspectable and digest-sensitive without treating diagnostics as authority. |

The typed UI call is conceptually:

```csharp
WorldCommand command = new WorldCommand(
    WorldCommandKind.ActorActionChoice,
    WorldCommandOrigin.LocalPlayer,
    WorldCommandAuthorityMode.Request,
    new ActorActionChoiceWorldCommandPayload(personId, sellGoodsDefinitionId));

WorldCommandResult captureResult = worldCommandService.Execute(command);
```

The command handler passes `WorldCommandExecutionContext.WorldCommandId` and
the typed payload to a guarded `SimulationRuntime` capture method. A
successful `WorldCommandResult` means **the actor choice was queued**; it does
not mean the action was available, attempted, or that a sale succeeded. The
existing `WorldCommandRecord` keeps command ID, day, origin, authority, kind,
success, and diagnostic metadata, but not the payload or logical queue order.
The actor-choice store therefore retains the normalized payload, its own
choice identity/order, the source `WorldCommandId`, capture day, and each later
disposition. The two identities must not be conflated.

`Preview` may check payload shape and currently supported action identity, but
must remain read-only: no input identity allocation, queue insertion,
`CreateRequestedAction`, merchant candidate planning, market lookup, RNG
consumption, or disclosure of remembered/current market values. The trusted
UI normally executes the selected typed request directly. Command origin is
never used as authorization proof. `Request` means that the ordinary actor
decision/domain path may attempt the choice; it does not grant `Declare` or
outcome authority.

## 3. Input identity, ordering, and lifecycle

### Captured record

The actor-choice owner retains an immutable receipt plus an ordered list of
dispositions. Each receipt contains:

- an `ActorChoiceInputId` allocated monotonically by the owning runtime;
- its `WorldCommandId` correlation from the shared command allocator;
- `InputSequence`, `PersonId`, and the selected action `DefinitionId`;
- `WorldCommandOrigin.LocalPlayer`, `WorldCommandAuthorityMode.Request`, and
  `CapturedAbsoluteDay`;
- a lifecycle state and ordered dispositions: zero or more `Deferred`
  dispositions may precede either one terminal `Rejected`, or one
  `DispatchStarted` marker followed by exactly one terminal attempt outcome.

The lifecycle statuses are `Pending`, `ConsumedAwaitingTerminalAttempt`,
`Rejected`, `AttemptReturned`, and `AttemptThrew`. `Deferred` is a recorded
boundary disposition while the status remains `Pending`. `DispatchStarted`
changes `Pending` to `ConsumedAwaitingTerminalAttempt`; that state is excluded
from the pending queue and is never eligible for another dispatch. Only its
matching terminal attempt outcome can close it. `Rejected`, `AttemptReturned`,
and `AttemptThrew` are terminal.

Attempt classification is `Succeeded`, `Failed`, `ReturnedNoResult`, or
`Threw`. The optional `NpcActionResult` status is populated only for a
non-null returned result (`Succeeded` or `Failed`); it is absent for the other
two classifications.

`DispatchStarted` is recorded immediately before the first call to
`TryExecuteCurrentAction`. That transition removes the input from the pending
queue and permanently marks it consumed, before action dispatch can begin; it
is not a retryable or deferred state. A normal return appends one terminal
`AttemptReturned` disposition. A non-null `NpcActionResult` is classified as
`Succeeded` or `Failed`; a null return is classified as `ReturnedNoResult`
and has no `NpcActionResult` status. If execution throws, append one terminal
`AttemptThrew` disposition with no returned-result status, then rethrow the
original exception. Neither outcome requeues the input. After
`DispatchStarted`, only the matching terminal
`AttemptReturned` or `AttemptThrew` disposition may be appended; no deferral,
rejection, or second dispatch is valid. No disposition may follow a terminal
outcome.

IDs and payload values are captured before action execution. They are not
derived from display names, object hashes, `NpcRuntimeId`, or Unity instance
IDs. The action definition is resolved again from the current configured
actions at application time; holding a ScriptableObject reference in the
queue would make stale-definition behavior depend on object lifetime.

The `WorldCommandId` provides the existing service's serial command order
among commands sent through that service. `InputSequence` provides a compact,
explicit order within the actor-choice owner. The current command ID allocator
and choice counter are in-memory sequences; this design records them for
deterministic snapshots and same-runtime continuation but does not claim
cross-save allocator continuity. Phase 12/13 own durable schema and complete
reconstruction guarantees.

### Application boundary

The application key is `(AbsoluteDay, actor-turn roster ordinal,
PersonId, NormalActionChoice)`. `SimulationRuntime` currently registers its
initial NPC roster in ordinal `RuntimeId` order and appends later registrations;
the actor-choice code preserves the actual current `NpcRuntimes` iteration
order rather than sorting actors by choice submission. This records the
ordering that can affect current market state between merchants.

Input capture records the current absolute day and command order. The command
is considered at the selected person's first later supported processing
boundary. At a normal `EvaluateAction` boundary, actor-turn order remains the
sole ordering authority for cross-actor actions. If multiple choices are
queued for one actor, the proposed store uses FIFO `InputSequence`: at most
one is considered at each ordinary action boundary and later choices remain
pending for later boundaries. It never replaces or coalesces an earlier
accepted choice. This multi-submit detail is a proposed technical contract
for independent review.

### Scheduling precedence and deferred choices

The hook is immediately before autonomous `NpcDecisionSystem.ChooseAction`
inside `SimulationRuntime.EvaluateAction`. It does not move the hook earlier
in `AdvanceDayAfterClockAdvance`. Existing paths retain precedence:

1. Dead actors do not evaluate an action.
2. Traveling actors and expedition participants retain their current
   scheduled-directive handling and then skip normal action evaluation.
3. Reserved expedition activity continues to skip normal action evaluation.
4. For an ordinary actor turn, status evaluation and merchant plan advancement
   continue first; a scheduled directive is processed before the actor choice.
5. Only when the turn reaches ordinary `EvaluateAction` is the actor choice
   considered, then autonomous `ChooseAction` remains the fallback.

For a living, materialized actor skipped by travel, expedition, reservation,
or a scheduled directive, append a `Deferred` disposition with day, roster
ordinal, and reason; keep the input pending. A scheduled directive may execute
as it does today. The choice then waits for the actor's next ordinary
`EvaluateAction` boundary. A deferral is diagnostic causal state only; it does
not reorder turns, execute an action, or change the pre-existing directive.

Daily demography advances before the NPC roster loop. To avoid a choice
remaining pending forever when its actor dies or loses materialization before
`EvaluateAction`, reconcile pending choices after daily demography and again
at an encountered actor's roster slot before the existing dead-actor skip.
If the captured `PersonId` is dead at the current day or no longer resolves to
a currently registered, materialized Person-backed NPC runtime, append one
terminal `Rejected/ActorUnavailable`
disposition at that deterministic reconciliation boundary. Do not materialize
or activate a Person. This rejection does not run autonomous selection for an
actor that has no normal turn. Living/materialized actors skipped only by the
precedence cases above remain deferred.

### Revalidation, application, and one-shot result

At ordinary evaluation, the runtime resolves the current `NpcRuntime` by the
receipt's `PersonId` and revalidates the supported consumer. The action
definition must still resolve to the configured `SellGoods` action; the actor
must still be alive, Person-backed, materialized, a merchant, at a current
city, with no active merchant trade plan; runtime policy/configuration must
still enable the action. This is ordinary game-state validation, not a
controller, ownership, or security check.

The selected action is constructed through the existing
`NpcDecisionSystem.CreateRequestedAction(npcRuntime, actionData)` path, which
uses its registered `INpcActionProvider`. For `SellGoods`, `MerchantSystem`
uses `FindBestLocalSale` to derive the candidate and quantity from this actor's
inventory and `CommercialKnowledge`; it does not add current market truth to
the actor's Knowledge view. The active-plan path is excluded. The UI supplies
no market values, item selection, or quantity.

If the action is no longer supported or the provider returns no runtime
action, append a terminal `Rejected/ActionUnavailable` disposition at that
normal boundary and continue with the unchanged autonomous `ChooseAction`
path. No action or sale is fabricated, and the choice is not retried.

If action construction succeeds, record the choice as the
`NpcDecisionOrigin.ActorChoice`, then append `DispatchStarted` immediately
before calling `TryExecuteCurrentAction`. This transition removes the input
from the pending queue and makes it permanently non-retryable before action
dispatch begins. Execute through the same ordinary
`MerchantSystem`/`EconomyTransactionService` path exactly once. If the call
returns, append the single terminal `AttemptReturned` disposition to the
input: a non-null result is represented as `Succeeded` or `Failed`; a null
return is `ReturnedNoResult` and carries no result status. These labels
describe only what the existing runtime returned; they do not replace the
underlying transaction result.
A failed or partial transaction follows existing `MerchantSystem` behavior:
no automatic retry, no second autonomous action that day, and no Knowledge
rewrite.

If action execution throws after dispatch starts, append the single terminal
`AttemptThrew` outcome with no returned-result status, then rethrow the
original exception with its existing propagation/stack. Do not
swallow it, substitute `Failed`/`Unavailable`, or claim an `NpcActionResult`
that was never returned. The choice remains consumed, no autonomous fallback
or retry runs in that interrupted actor turn, and the runtime's existing fault
behavior applies. A terminal rejection occurs only before dispatch (for
example, when the provider returns no action); it falls back to autonomous
selection as specified above.

This boundary deliberately keeps the records separate:

| Record | Meaning |
|---|---|
| `WorldCommandRecord` | The typed request was accepted into the actor-choice input owner. |
| Actor-choice lifecycle | The input was deferred or rejected, or dispatched once and closed as returned or thrown. `DispatchStarted` means consumed even before the terminal outcome is written. |
| Returned `NpcActionResult` | The existing action execution reported success/failure; a null return or thrown call has no returned result. |

Current `MerchantSystem` execution receives an `EconomyTransactionResult`
internally and returns a transient `NpcActionResult` through the runtime. It
does not retain a `SellGoods` DomainEvent or history entry. The actor-choice
receipt/disposition is not a substitute event and must not manufacture a
transaction receipt, quantity, price, trade event, or history item that the
current domain path does not expose.

## 4. Composition and file ownership

Implementation should use isolated ownership for these shared files:

| Files | Proposed owner and changes |
|---|---|
| `WorldCommandFoundation.cs` | Command contract owner adds the dedicated kind, provenance label, and immutable typed payload. Do not add authority modes or change existing command payload semantics. |
| New `ActorActionChoiceWorldCommandHandler.cs`; `CoreWorldCommandHandlers.cs` | Command-boundary owner implements the typed `Request` handler and explicit registration. Keep it out of GM-console and natural-language command catalogs. It uses `WorldCommandExecutionContext.WorldCommandId` to correlate the queued record. |
| New `ActorChoiceContracts.cs`, `ActorChoiceStore.cs` | Input owner implements stable identity, payload, sequence, disposition lifecycle, mutation guard, clone, and invariants. No runtime object references are retained. |
| `SimulationRuntime.cs` | Runtime owner composes/clones/binds the store to the same world mutation guard, exposes the actor-choice store to diagnostics, captures handler input, reconciles unavailable actors, defers skipped turns, consumes at `EvaluateAction`, records the decision origin, and captures the post-attempt `NpcActionResult`. Keep `AdvanceDay` scheduling order intact. |
| `DecisionRecords.cs` | Decision-history owner adds `ActorChoice` provenance only; it does not turn the input receipt into an outcome event. |
| `Diagnostics/WorldStateSnapshot.cs`; new `Diagnostics/WorldStateActorChoiceSnapshots.cs`; `WorldStateCanonicalWriter.cs`, `WorldStateDiff.cs`, `WorldStateFormatter.cs`, `WorldStateInvariantValidator.cs` | Diagnostics owner projects immutable input/disposition values; adds semantic diff, canonical write, human formatting, and invariant validation. `WorldStateSnapshotDigest.cs` remains driven by canonical output unless its API requires a direct addition. |
| New `Tests/EditMode/Editor/ActorActionChoiceCommandTests.cs`, `ActorChoiceStoreTests.cs`, `ActorChoiceRuntimeTests.cs`, `WorldStateDiagnostics/ActorChoiceDiagnosticsTests.cs`; existing orchestration/merchant/command suites | Test owner covers command ingress, store lifecycle, runtime ordering and fallback, Knowledge-bounded planning/current-truth execution, and diagnostics. Existing shared hotspots have one explicit owner at a time. |

`NpcDecisionSystem.CreateRequestedAction`, `MerchantSystem` action provider,
and `EconomyTransactionService` are reused. They should need no behavior change
for this slice. If implementation requires changing their mutation or
transaction contracts, stop for review rather than smuggling in a new sale
path. Unity `.meta` files are added for new source and test files as part of
implementation.

## 5. Causal determinism, cloning, and reconstruction obligations

The actor-choice input owner is authoritative because a pending input changes
which action can execute in the future. Its snapshot/reconstruction projection
must retain:

- the stable actor `PersonId`, selected action definition identity, separate
  `WorldCommandId`, actor-choice `InputId`, and capture order;
- caller provenance (`LocalPlayer`) and the `Request` authority mode, without
  inventing a caller principal or actor grant;
- capture day, every considered/deferred boundary and reason, the actual
  actor-turn roster ordinal, and the terminal rejection or returned/thrown
  attempt outcome;
- the action/content/configuration versions and effective policy needed to
  interpret the same selection. These remain Phase 12/13 continuation and
  reconstruction inputs; Phase 11 records the selected stable IDs, not a
  broad content snapshot;
- the actor-owned Knowledge and inventory state already in the world, plus
  relevant configuration, calendar, and deterministic-random state from the
  normal runtime. The choice path itself must not use hidden market truth while
  planning.

No wall-clock arrival timestamp determines execution. The typed command order
and absolute-day capture are recorded; application is identified by day,
actor roster ordinal, PersonId, and the normal action-choice boundary. Actor
choices do not sort or otherwise reorder the NPC roster. Creating or reading a
choice, previewing it, snapshotting it, or diffing it does not consume
authoritative randomness. A successfully constructed actor choice bypasses
autonomous weighted selection for that one turn; an unavailable choice is
rejected first and then the unchanged autonomous selector runs with its
existing random-stream key and normal draw behavior. Deferral consumes no
randomness.

The store clone must copy capture sequence, normalized payloads, all pending
choices, disposition order, dispatch markers, and terminal outcomes into the
target runtime while binding to its target `PersonStore` and mutation guard.
An observed `ConsumedAwaitingTerminalAttempt` state remains consumed in the
clone and must never be dispatched again; recovery does not invent a returned
result. Ordinary synchronous execution closes this state with a returned or
thrown outcome before the next actor turn. Full interrupted-run recovery
policy is outside this Phase 11 slice. The clone must not share mutable lists,
runtime references, or allocators with its source. Diagnostics must be a stable
value projection and must not mutate the queue. The current
canonical `SimulationRuntime` constructor clones/composes many input stores,
but this baseline does not define full save/load or historical fork semantics;
Phase 11 must not claim that it does. Phase 12/13 later own durable allocator
continuation and complete reconstruction.

## 6. Deterministic diagnostics contract

The current diagnostics deliberately exclude ordinary NPC decision history
and DomainEvent history (`WorldStateDiagnosticsTests` covers this boundary),
while projecting selected authoritative decision inputs such as political
decisions. The actor-choice input is different from generic decision history:
its pending state can change the future. Extend `WorldStateSnapshotContext`
with an actor-choice input projection and include a dedicated
`WorldStateActorChoiceInputSnapshot` collection in `WorldStateSnapshot`.

Canonical output orders receipts by `InputSequence` and dispositions by their
recorded boundary order (day, roster ordinal, transition ordinal). Stable
writer fields include input ID, correlated WorldCommand ID, PersonId, action
DefinitionId, origin/authority, capture day, disposition and reason, logical
boundary fields, decision-record link when available, and the terminal
attempt outcome. `AttemptReturned` records `Succeeded` or `Failed` only when a
non-null `NpcActionResult` was returned; a null return records
`ReturnedNoResult` with no result status. `AttemptThrew` records `Threw` with
no returned-result status. The writer must use invariant numeric formatting
and delimiter-safe identity encoding. Diff reports pending additions,
deferral transitions, dispatch consumption,
terminal rejection/attempt outcomes, and returned-result changes. The
formatter presents input acceptance separately from domain execution.
Snapshot invariant validation checks unique input/command correlations,
nonnegative monotonic sequences/days, valid stable IDs, legal lifecycle
transitions, at most one dispatch start and one terminal disposition per
input, a terminal attempt only after dispatch start, no rejection/deferral
after dispatch start, a returned-result status iff `AttemptReturned` has a
non-null result, no `NpcActionResult` status on `AttemptThrew` or
`ReturnedNoResult`, and no disposition after terminal resolution. A
`ConsumedAwaitingTerminalAttempt` input has exactly one `DispatchStarted`
marker, remains outside the pending queue, and can only close as returned or
thrown. A dispatched input is never pending or eligible for replay, including
while its terminal outcome is not yet written. The current Person or its
persisted identity may remain dead; that is valid history, not a diagnostic
error.

Snapshot, export, digest, compare, and validation must not allocate input or
command IDs, query `MerchantSystem` for sale candidates, inspect current market
truth for actor Knowledge, invoke the action provider, mutate a disposition,
or consume random state. Pending and all dispatched, rejected, or completed
inputs remain present in snapshots without introducing fake DomainEvents or
ordinary NPC history.

## 7. Proposed regression coverage

The following suites are implementation/review requirements proposed for this
slice; this documentation task does not run them.

| Test surface | Required cases |
|---|---|
| `ActorActionChoiceCommandTests` and `WorldCommandFoundationTests` | Typed payload retains only PersonId/action DefinitionId; `LocalPlayer` is provenance; Request queues without sale mutation; Preview is read-only and allocates no choice/command identity; Suggest cannot execute; Declare/ForceOutcome do not execute this kind; `WorldCommandRecord.Success` means queued, not sold. Existing command authority behavior stays unchanged. |
| `ActorChoiceStoreTests` | Capture order/IDs are deterministic; multiple same-actor choices remain FIFO; rejection and deferral transitions are one-shot; dispatch-start permanently consumes an input; exactly one returned/thrown terminal outcome follows; no disposition follows terminal resolution; dead/unmaterialized actor reconciliation terminates pending choice; clone is independent and preserves sequence, payload, lifecycle state, dispositions, and result; mutation guard blocks writes. |
| `ActorChoiceRuntimeTests` and `SimulationRuntimeOrchestrationTests` | Choice is consumed immediately before action dispatch only on a normal actor turn; actor roster order is unchanged; travel, expedition, reservation, and scheduled directive precedence is unchanged and records deferral; actor unavailable at the input-processing boundary is rejected; stale action/provider/policy eligibility rejection falls through to autonomous selection; accepted action attempts once, with no same-day retry or autonomous second action. If action execution throws, assert one terminal `AttemptThrew`, no returned `NpcActionResult` status, no requeue/retry or autonomous fallback, and propagation of the same exception instance with the original throw frame preserved. Also cover non-null success/failure results versus a null return classified as `ReturnedNoResult`. |
| `MerchantLiquidityTests` plus actor-choice commerce cases | Actor's remembered price/liquidity and own inventory bound candidate planning; another NPC's current state and current market truth do not enrich planning; active plan is rejected; stale current market can return the normal failure/partial-fill action result; no Knowledge rewrite, fabricated transaction receipt, trade DomainEvent, or history entry. |
| `ActorChoiceDiagnosticsTests` and WorldState diagnostics suites | Pending, deferred, rejected, dispatch-started, returned, and thrown states appear in immutable snapshots; returned `NpcActionResult` statuses are present only for non-null results, while null and thrown calls have distinct attempt outcomes and no result status; canonical export/digest/diff/formatting are deterministic; ordering is by causal input/boundary keys rather than collection iteration; validation catches illegal transitions and any disposition after terminal; diagnostic operations are read-only. |

Also retain the existing `CoreWorldCommandHandlerTests`,
`WorldCommandFoundationTests`, `SimulationRuntimeOrchestrationTests`,
`MerchantLiquidityTests`, `EconomyTransactionTests`, and
`WorldStateDiagnostics` regressions. `EconomyTransactionService` behavior is
not changed. The candidate branch should run focused tests during work and the
applicable integration gates after shared composition. If the final runtime
diff changes daily-loop or long-horizon causality beyond consuming a queued
choice, the reviewer should require the matching long-run gate.

## 8. P8-D hotspot forecast and integration sequence

P8-D is not a semantic dependency for this current-city sale. Its current
runtime and diagnostics integration code commit is
`2faf3f873a7a434dc7330191e9292d09f6dd3014`; the latest unpromoted integration
branch HEAD is `6e964c90adfcda828c675e7a1a3f726279c2d298` on
`codex/phase8/P8DKnowledgeRoutePlanIntegration`. Commit `2faf3f8` integrates
route-store composition with `SimulationRuntime.cs` and the shared diagnostic
surfaces: `WorldStateSnapshot.cs`, `WorldStateCanonicalWriter.cs`,
`WorldStateDiff.cs`, `WorldStateFormatter.cs`, and
`WorldStateInvariantValidator.cs`. It also touches `PersonRoutePlanStore.cs`,
`SpatialRouteKnowledgeStore.cs`, and `SpatialRoutePlanningTests.cs`. Across
the candidate diff from the Phase 8 canonical base, the touched route files
also include `PersonRoutePlanStore.cs`, `SpatialRouteKnowledge.cs`,
`SpatialRouteKnowledgeStore.cs`, and `SpatialRoutePlanningSystem.cs`, with
their Unity metadata and route-test metadata; `PHASE8_STATE.md` and
`ROADMAP.md` record the candidate status. These are the actual
shared-runtime/diagnostics hotspots in the current candidate, rather than a
forecast based on the earlier feature-only diff.

Phase 11 implementation must **wait until the P8-D `SimulationRuntime` and
diagnostics integration is integrated into the selected Phase 8 canonical
base and the Phase 11 worktree is refreshed from that base**. Recompute the
actual touched-file set after that integration and assign one writer at a
time for overlapping files. Do not start P11 shared-hotspot changes while
this P8-D integration window remains unintegrated. This is an
integration/hotspot wait only: no P8-D capability or route API is used by the
selected SellGoods action.

Proposed sequence after technical-design review:

1. Keep this design and latest entry commit as the review baseline; confirm the
   typed WorldCommand handler's `Request`-only meaning, FIFO multi-submit
   behavior, death/unmaterialization reconciliation, and separate command,
   input, decision, and action-result records.
2. Wait for the P8-D runtime/diagnostics integration at code commit
   `2faf3f873a7a434dc7330191e9292d09f6dd3014` to be integrated into the
   selected Phase 8 canonical base. Refresh the P11 worktree and recompute
   touched files before assigning P11 shared-hotspot work.
3. Start implementation from the refreshed canonical head in an isolated
   `codex/phase11/<FeatureName>` worktree. Assign one owner per shared hotspot:
   command contract/handler, actor-choice store, runtime boundary, then
   diagnostics. Do not allow concurrent writers on `SimulationRuntime` or
   diagnostics core.
4. Run store/command/runtime/merchant/diagnostics focused tests, then affected
   regressions and required integration gates. Independently review the full
   diff against that actual canonical base, including mutation authority,
   stale eligibility, transactional result semantics, deterministic ordering,
   cloning, and diagnostic completeness.
5. Only after review and validation may the orchestrator assign approved
   checkpoint identity/readiness. This design itself neither promotes code nor
   changes Phase 11 State or architecture.

## 9. Proposed UNAPPROVED work decomposition

These are candidate units only; names do not assign checkpoint IDs or make
implementation schedulable.

| Candidate unit (UNAPPROVED) | Closure evidence |
|---|---|
| Typed actor-choice WorldCommand and capture owner | Request-only typed ingress retains PersonId, action DefinitionId, WorldCommand identity, stable queue order, and accepted/rejected capture result without domain mutation. |
| Runtime decision-boundary adapter | Ordinary `EvaluateAction` consumes one FIFO choice before autonomous selection; higher-priority turn behavior is preserved; dead/unmaterialized inputs terminate; unavailable action falls back once; constructed action is attempted once through current merchant authority. |
| Causal diagnostics and clone | Pending/disposition records are cloned without aliasing and included in deterministic snapshots, diff, canonical output, formatter, digest, and invariant validation. |
| Integrated regression and independent review | Knowledge-bounded planning, current-market execution truth, one-shot result, command semantics, deterministic order, diagnostics, and affected Phase 8/Phase 5 regressions pass review and required integration gates. |
