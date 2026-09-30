# P12-B Expedition owner census design

**Status:** Bounded technical contract for an accepted P12-B owner-census work
package. This is a design proposal only. It is not implementation, a new
checkpoint ID, P12-B completion, or P12-A readiness.

**Evidence base:** `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. The proposal follows the existing
P12 fixed-owner `IOwnerSectionCensusProvider` pattern and does not extend the
generic dynamic NPC census protocol.

## Owner and witness

Publish one fixed schema-v1 Required section with stable identity
`p12f.expeditions`. Its provider is bound to the exact `ExpeditionStore`
installed in `SimulationRuntime` and reports:

- cardinality: `ExpeditionStore.ActiveExpeditions.Count`;
- revision: one monotone `ExpeditionStore` revision for committed changes to
  active expedition runtime or objective truth.

The provider reads under the store's serialized owner read window. It must
validate that the active list and ID index agree, every list member is the
exact object indexed by its nonblank `ExpeditionId`, every indexed expedition
is active and present exactly once in the list, and no duplicate expedition
identity exists. Any mismatch, saturated/invalid revision state, or owner
identity mismatch fails closed. It must not synthesize an expedition from
events, decisions, NPC travel parties, or UI state.

`ExpeditionStore` currently retains only active expeditions. `Complete` removes
an expedition from both indexes only after it has entered `Completed`; a
successful completion therefore changes the census owner state even though
the resulting cardinality returns to its prior value. The owner revision is
required to expose that committed change.

## Mutable truth and mutation coverage

The active owner includes `ExpeditionRuntime.State`, `TravelPartyId`, current
local place, visited-place and observed-connection membership, and the
objective's progress/completion fields. The objective's target and policy
fields, expedition identity, target/origin/route IDs, and participant IDs are
constructor-fixed and need no mutation tracking. A successful operation that
changes any mutable active-owner fact must advance the store revision once.

All supported mutations must run through a store-owned reentrant serialized
mutation window. The window protects the operation's validation, capacity
preflight, owner changes, any attached runtime/objective leaf mutation, and
revision publication as one owner commit boundary. Provider reads use the
same monitor. Attach each stored `ExpeditionRuntime` and its
`ExpeditionObjectiveRuntime` to that exact store when `Add` commits; direct
runtime leaf methods must enter the attached owner window and report whether
they changed truth. Keep their current public surface where practical, but an
unattached runtime retains its current standalone behavior. Never permit one
runtime to be attached to two stores or silently rebind it.

The revision contract is per actual committed owner mutation, not per high-
level user intent:

- `Add` increments once after both active indexes are coherently updated.
- A successful state/progress/current-place/visited-place/observed-connection/
  objective transition increments once for the entire leaf operation. A
  successful traversal that changes both connection and destination-place
  facts still increments once.
- A successful `Remove` of a Preparing expedition increments once; successful
  `Complete` removal increments once.
- Invalid, rejected, missing, duplicate, already-completed, or semantic no-op
  calls do not increment. For example, recording an already-observed
  connection with no other changed fact remains a no-op; repeating a terminal
  transition remains rejected.
- Saturation rejects before any mutation that needs a revision. Revisions do
  not wrap. If a compound action has multiple independently committed owner
  writes, each write increments once; do not collapse them into one intent
  stamp.

There is an existing conflict requiring a source-faithful implementation
choice: `ExpeditionSystem.TryStartExpedition` currently calls
`ExpeditionStore.Add` before starting the `TravelParty`, then calls `Remove`
as compensation if party creation or `TryBeginTravel` fails. A successful
start commits Add and the `Preparing` to `TravelingToSite` transition (+2). If
party creation fails, Add and compensating Remove are two owner writes (+2).
If party creation succeeds but `TryBeginTravel` fails, the writes are Add and
Remove (+2), with no state transition. Do not introduce a special hidden
rollback that erases a committed owner write or resets the revision. Capacity
for both possible writes must be preflighted before Add. If start is later
refactored to prepare every fallible dependency before publishing the
expedition, that may avoid the temporary Add/Remove pair, but it is outside
this census-only design and must preserve existing transaction semantics.

The owner window and attached callback must include these current mutation
paths:

- Start: `TryStartExpedition`, `ExpeditionStore.Add`, `TryBeginTravel`, and
  possible `Remove` compensation.
- Travel arrival: `ReconcileAfterTravel` and `TryArriveAtSite`.
- Exploration: `TryBeginExploration`, abstract `TryAdvanceAbstractProgress`,
  `TrySetCurrentLocalPlace`, `TryRecordObservedConnection`, and
  `TryTraverseLocalConnection`.
- Objective completion through resource retrieval, notable-item retrieval,
  opposition resolution, and direct `TryMarkObjectiveComplete`.
- Return and completion: `TryBeginReturn`, return-party creation and
  compensation (`TryCancelReturn`, `TryBeginReturnTravel`), `TryComplete`, and
  `ExpeditionStore.Complete`.
- The autonomous consumer's begin/advance/explore/traverse/retrieve/opposition/
  return paths must use the same attached owner operation paths. The autonomy
  system's reservation and failed-execution caches are not ExpeditionStore
  census facts.

Where one runtime leaf operation changes several facts atomically, use a
nested/reentrant owner window and publish one revision for that leaf
operation. For example, `TryTraverseLocalConnection` may update
observed-connection, current-place, visited-place, and objective progress
together. Lower-level helpers used inside that operation must not
independently double-count it. Distinct successful mutations inside a larger
system call remain distinct owner writes and each advance the revision: a
successful start commits Add and then `TryBeginTravel` (+2); a successful
return commits `TryBeginReturn` and updates `TravelPartyId` with
`TryBeginReturnTravel` (+2). A compensated return stamps each actual state
change (`Returning`, then cancellation to `AtSite`/`Exploring`). Direct public
leaf calls take the attached store window and publish their own single
revision when truth changes.

## Preflight, ordering, and atomicity

Before mutating an attached expedition, preflight revision capacity. Before
`Add`, preflight before changing either list or dictionary. Before a compound
store operation, preflight enough capacity for each independently committed
write it can make, including known compensation. In particular, the current
start sequence requires capacity for two writes (Add plus either
`TryBeginTravel` or compensating `Remove`); if capacity is insufficient,
reject before publishing Add or starting the travel party. `TryBeginReturn`
similarly reserves capacity for the state transition plus return-party ID
association or state compensation before its first owner mutation. A failed
capacity preflight must leave expedition state,
objective state, owner indexes, participant travel state, and emitted
expedition lifecycle events unchanged.

The serialized window is scoped to the ExpeditionStore owner and mutations
covered by this design. It does not make all related owners transactional.
TravelParty, NPC travel state, item stores, content/opposition, knowledge,
event/history stores, and runtime composition remain separate owners with
their existing contracts. Preserve their existing validation, event ordering,
and compensation. If an operation commits an ExpeditionStore write and a later
external owner fails, record the actual ExpeditionStore compensation as its
own revision when it succeeds; do not claim global atomic rollback.

To avoid a new lock-order cycle, all paths acquire the ExpeditionStore monitor
before invoking nested travel-party, content, inventory, conflict, or event
work. Audit the selected collaborators for any callback that re-enters a
different store and then tries to acquire ExpeditionStore in reverse order.
The monitor is reentrant for same-thread nested ExpeditionStore calls; that
does not make arbitrary reentrant callbacks or concurrent cross-owner
transactions safe. If a supported collaborator introduces a reverse
acquisition path, use a scoped owner permit/operation context to preserve the
single serialized owner boundary rather than nesting unrelated locks in an
unreviewed order.

For `ReconcileAfterTravel` completion, finalization must verify exact active
owner identity before mutation: the argument is the exact object returned by
the exact store's ID lookup, it is still active and `Returning`, and the
completed runtime transition and store removal refer to that same object.
Capacity must be available before `TryComplete`; then the transition plus
removal form one owner operation with one revision. If the store no longer
contains that exact object, reject before changing its state. The same exact
store/object identity validation applies to every `ExpeditionSystem` mutation
entry point.

## Bootstrap composition and tests

In the normal authored bootstrap, `SimulationBootstrapComposition` already
receives `ExpeditionStore expeditions` and `ExpeditionSystem expeditionSystem`
and publishes both. Construct one `ExpeditionCensusProvider` from that exact
`expeditions` argument, expose it as an `IOwnerSectionCensusProvider` property,
and keep it a fixed section. Do not add it to per-NPC dynamic provider
creation. Composition should reject or fail closed if the provider's owner is
not reference-identical to `Expeditions` and `ExpeditionSystem.Store`.

Focused tests should establish:

- authored bootstrap exposes exactly one Required `p12f.expeditions`
  schema-v1 provider bound to the exact composed `ExpeditionStore`; initial
  cardinality/revision are zero and the active list/index invariant holds;
- Add, successful direct runtime/objective transitions, Remove, and Complete
  advance once per actual owner mutation; rejected, duplicate, terminal,
  missing, or no-op calls do not advance;
- abstract progress, local exploration, traversal, objective completion,
  retrieval, and opposition resolution each produce the expected owner
  revision changes, including objective progress/completion updates;
- exact-owner spoofing, stale/completed arguments, store/list/index drift, and
  revision saturation fail closed before partial owner state is written;
- Add+Remove compensation advances twice when both writes commit, while
  preflight at insufficient remaining capacity rejects before external travel
  state, costs, or events are changed;
- completion saturation rejects before marking the runtime Completed or
  removing the exact active object;
- a second thread attempting an attached runtime mutation blocks while a
  provider read or owner mutation window is held, then observes the committed
  state/revision pair;
- existing Expedition, ExpeditionExploration, ExpeditionExplorationCorrection,
  AdventureExpeditionAutonomy, and BootstrapComposition suites preserve
  behavior and cover event, retrieval, conflict, return, and compensation paths.

## Limits

This is passive owner evidence only. It does not wire ExpeditionStore writes
to the global P12-B mutation epoch, prove runtime-wide owner-thread ownership
or quiescence, grant capture eligibility, or establish that all P12-B owners
are complete. It does not define P12-F export/hydration, persistence, or
reconstruction, and does not make P12-A READY. Other independent stores touched
by expeditions retain separate census and committed-write obligations.
