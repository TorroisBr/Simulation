# P12-B selected-profile solo travel-start operation design

**Status:** bounded technical design for independent review. This is a
sub-slice of the already accepted P12-B prerequisite capability work. It adds
no Phase checkpoint ID, gameplay rule, or readiness claim.

**Canonical base:** `codex/phase12/canonical` at
`e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.

**Contract authority:** `docs/SIMULATION_ARCHITECTURE.md`,
`docs/EXECUTION_MODEL.md`, `docs/phases/PHASE12_BRIEF.md`,
`docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, and the current
P12-B owner/operation/epoch matrix in
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`.

## Current source boundary

The current selected-profile path is:

```text
SimulationRuntime.TryExecuteAction
  -> action success roll
  -> TravelActionProvider.TryExecuteAction
  -> TravelSystem.TryStartTravel
  -> EconomyTransactionService.TryChargeTravel
  -> NpcRuntime.StartTravel and source-City presence removal
  -> discover origin and route in SpatialKnowledge
  -> DomainEventRecorder.Record (Event ID, then record sequence)
  -> TravelActionProvider.ClearTravelPlan
  -> SimulationRuntime.ApplySuccessStatusChanges (after provider returns)
```

`TravelActionProvider` is the only non-test production caller of
`TravelSystem.TryStartTravel` in the current source tree. `TravelPartySystem`
uses separate group-start and advance paths. The selected `UnityBootstrap-Daily-v1`
runtime invokes the action provider inside its existing
`runtime.advance-day` operation. P12's Event-counter and
`SimulationRecordSequence` invalidation hooks are already promoted and notify
the active operation batch when one exists.

The missing contract is one named, nested P12 operation that preflights the
exact owners for this existing solo start path and coalesces their committed
notifications. It must preserve TravelSystem's current domain validation,
charge/compensation behavior, partial-commit behavior, event handling, action
roll, and deterministic ordering.

## Bounded owner set

For one exact installed `NpcRuntime`, preflight the current sections that the
provider can change:

| Owner section | Required exact identity and witness | Why this operation can change it |
|---|---|---|
| `NpcMoneyAccountCensusProvider.SectionPrefix + RuntimeId` | The installed NPC's exact `MoneyAccountRuntime`, exactly one roster binding, current local revision | Travel charge and, if the travel-state transition rejects after charge, charge restoration |
| `NpcTravelStateCensusProvider.SectionIdFor(RuntimeId)` | The exact installed `NpcRuntime` and its existing travel-state census binding | Successful `StartTravel` changes the actor's travel commitment/state |
| `NpcPlanCensusProvider.TravelPlanSectionPrefix + RuntimeId` | The NPC's exact installed `NpcTravelPlanRuntime` and its one-owner binding | `TravelActionProvider` clears the plan after a successful start |
| SpatialKnowledge Locations and Routes sections for `RuntimeId` | The exact installed per-NPC `SpatialKnowledgeRuntime`; both sibling sections share its current revision | A successful start discovers the origin and executed route |
| `CityNpcPresenceCensusProvider` section for the source City, when the NPC currently has a source-City projection | The exact installed City and current reciprocal NPC projection | `NpcRuntime.StartTravel` removes the departing NPC from its source City's presence projection |
| `RuntimeIdAllocatorCensusProvider.EventsSectionId` | The selected runtime's exact `RuntimeIdAllocator` Event counter | `DomainEventRecorder.Record` can allocate the Event ID after travel commits |
| `SimulationRecordSequenceCensusProvider.SectionId` | The selected runtime's exact record-sequence owner | Successful event construction/storage allocates a record sequence |

Do not include a target-City presence section: departure does not add the NPC
to the destination. A non-City source has no City projection section. Do not
include Inventory, Market, TravelPartyStore, or unrelated allocator counters;
this path does not mutate them. The source City is included only when the
current NPC projection identifies one and must be removed by the normal
transition.

The runtime must confirm the actor is the exact unique installed roster
instance for its `RuntimeId`; every listed provider/binding must point to that
same owner and the expected section ID/schema; the NPC account must retain
cardinality one; and the SpatialKnowledge sibling sections must refer to the
same owner and revision. Confirm that section IDs are unique and validate the
current baselines and capacity for one mutation-epoch notification before
calling the provider. This is P12 owner/baseline correctness, not an
authorization or anti-tamper boundary.

## Operation boundary and batching

Register `runtime.travel.start` in the selected daily profile's sealed
operation inventory when the runtime composes `TravelSystem`.

In `SimulationRuntime.TryExecuteAction`, after the existing action success roll
has succeeded and immediately before invoking the installed
`TravelActionProvider`, enter the named operation for the admitted
`UnityBootstrap-Daily-v1` profile. This places it under the existing
`runtime.advance-day` operation and inside the existing action execution path,
including scheduled request-action directives that use that path. A failed
action roll does not enter this operation. Legacy runtimes without P12
admission retain the current direct provider call.

Use a runtime-owned, owner-thread-bound operation context with a deduplicating
ordinal set of changed section IDs and a `SimulationOperationScope`. Before
entry, reject/fault closed if the selected runtime thread, actor identity,
owner bindings, section inventory, any relevant baseline, or mutation-epoch
capacity is invalid, or if another exclusive Merchant, TravelParty, or solo
travel batch is active. Do not invoke the action provider after a failed P12
preflight. The existing daily-advance failure path should receive the
admission failure so the runtime faults closed before gameplay mutation.

`EconomyTransactionService.TryChargeTravel` and
`TryRestoreTravelCharge` commit directly through `MoneyAccount.TryDebit` and
`MoneyAccount.TryCredit`; they do not enter the separate
`runtime.economy.money-transfer` operation used by NPC-to-NPC transfers. Do
not add that child operation for travel. The existing account callbacks join
the outer solo-travel changed set, and the outer scope owns the single
coalesced epoch notification for the whole travel-start attempt, including a
debit followed by compensation.

While the operation is active, route existing owner callbacks through its
changed-section set instead of notifying the protocol separately. Update both
`CanCommitP12MutationSections` and `NotifyP12MutationSections` so exactly one
active operation context is recognized. Baseline checks may exclude sections
already committed by this context; this is needed when a single account is
debited and then compensated. Keep the current TravelParty and Merchant batch
semantics intact. Owner callbacks remain after their existing owner commit.

On scope exit, including exception unwinding, notify the protocol once with the
deduplicated non-empty changed set, then clear the active context and dispose
the nested operation scope in `finally`. An empty set creates no mutation
epoch. Multiple successful owner writes and compensation of the same account
still produce one changed-section report and at most one epoch step for this
named operation.

`TravelActionProvider.ClearTravelPlan` remains inside the operation because it
runs before the provider returns. `ApplySuccessStatusChanges` runs after the
provider and remains outside this operation; this slice makes no NPC
composite/status invalidation claim.

## Existing failure and partial-commit semantics

- A rejected charge, invalid current route, or other ordinary domain refusal
  preserves existing TravelSystem behavior. If nothing commits, the scope
  closes with no changed sections and no epoch step.
- If the debit commits but `NpcRuntime.StartTravel` rejects, preserve the
  existing attempted charge restore. If both writes commit, the account's
  final revision reflects both writes while its section is deduplicated in
  the one operation-close notification. Do not add rollback or alter the
  charge result.
- After a successful start, preserve the current discovery, event-recording,
  logging, and plan-clear order. If event recording returns failure after any
  Event ID or record-sequence allocation, those owner callbacks join the same
  batch; do not undo the already committed travel or change event-failure
  policy.
- If a later call throws after one or more owner commits, scope disposal
  reports the accumulated commits before the existing daily-advance exception
  handling faults admission. Do not retry or roll back partially committed
  travel.

## Implementation and review sequence

1. Add the conditional expected-operation registration and a dedicated
   solo-travel context/scope in `SimulationRuntime`.
2. Add exact selected-profile preflight using the existing account resolver,
   current owner bindings/providers, City-presence section map, Event-counter
   provider, record-sequence provider, and protocol baseline/epoch checks.
3. Enter the scope only around the successful-roll `TravelActionProvider`
   invocation. Route all already-bound callbacks into the batch and flush it
   once on close. Keep the implementation in the existing SimulationRuntime
   hotspot; do not add a general operation framework or change domain owners
   unless source-level validation identifies a missing existing callback.
4. Add focused tests before integration. Review the exact code tip and rerun
   validation if its tree changes.

Required focused coverage:

- operation is registered only for the admitted selected daily composition,
  appears nested below `runtime.advance-day`, and is absent from legacy
  unadmitted execution; travel charge/restore produce no synthetic
  `runtime.economy.money-transfer` child operation;
- exact actor/account/travel-state/travel-plan/SpatialKnowledge/City/Event/
  sequence identity, cardinality, section IDs, revision baselines, owner
  thread, uniqueness, and epoch-capacity preflight; stale or substituted
  owners fail closed before charge or travel mutation;
- successful start batches account, travel state, conditional source-City
  presence, both Knowledge sections, travel plan, Event counter, and sequence
  callbacks into one operation-close notification/epoch step;
- failed start with a committed debit and successful compensation reports the
  account once while retaining its two local revision changes; ordinary
  no-op/refusal produces no epoch step;
- event-recording failure and exception-after-partial-commit preserve the
  existing travel/event outcome while flushing only committed owner sections;
- NPCs without a source City do not report a fabricated City section; target
  City and TravelParty owners remain unchanged;
- scheduled request-action travel uses the same wrapper, and success-status
  changes remain outside the bounded operation.

Run the focused solo travel-start/action-provider, travel charge and
compensation, travel/presence, SpatialKnowledge, Event-counter,
record-sequence, bootstrap-composition, runtime-admission/orchestration, and
long-run suites; then ALL EditMode, the complete official Unity Smoke filter,
and `git diff --check`. Preserve inspectable XML/log artifact paths and hashes.

## Explicit limits

This operation covers only the current selected-profile solo Travel action
provider path. It does not add or change user actor choice, authorization,
travel rules, route choice, TravelParty formation/advance, general travel
reconciliation, P18 temporal execution, mod hooks, or action/status semantics.
It does not prove all TravelSystem entry points are globally guarded, complete
owner or shared-epoch coverage, global quiescence, capture eligibility,
P12-B completion, export, hydration, P12-A readiness, P13 readiness, or Phase
12 closure. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13
remains blocked.
