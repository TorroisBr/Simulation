# P12-B Expedition owner invalidation — technical design

**Status:** Bounded technical design proposal. Independent technical review is
required before implementation.

**Accepted scope:** P12-B prerequisite capability work. This adds no checkpoint
ID, does not broaden P12-A, and does not claim completion of P12-B.

**Canonical base:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
**Architecture reviewed:** `3bf09249b7dd9e255c3493aacfd75c96080a31e3`.
The current P12 checkout's architecture blob differs from that architecture
revision by omitting §§91A, 91B, and 92A; those additions were considered here
as the requested current architecture authority. This design does not rely on
or alter those sections.

## 1. Purpose and boundary

The selected `UnityBootstrap-Daily-v1` profile composes an active
`ExpeditionStore`, `ExpeditionSystem`, and exact passive
`p12f.expeditions` schema-v1 witness. The store already serializes active rows,
validates exact object/index identity, and increments a local revision at each
committed owner change. The selected P12 runtime does not register that witness
or connect those revisions to its partial mutation epoch.

This slice registers and binds the existing Expedition owner section. Each
successful `ExpeditionStore` commit preflights the exact owner and current P12
baseline before mutation, then reports the committed section after its local
revision advances. Writes in a currently supported outer P12 batch join that
batch; otherwise the section is notified immediately. No new gameplay action,
reservation rule, transaction, or timeline behavior is introduced.

The specific start path is an important proving case, not a new aggregate
operation contract. `ExpeditionSystem.TryStartExpedition` keeps its existing
`AddAndFence` → `TravelPartySystem.TryStartTravelParty` →
`CommitReserved` sequence, and its existing `RemoveReserved` compensation
attempts. Each actual ExpeditionStore commit is separately reported. The
nested TravelParty and its already-bound owners continue to report through
their existing P12 hooks. This design does **not** batch the nested
TravelParty and Expedition commits into one epoch notification. Adding a
single outer Expedition-start operation would require a new SimulationRuntime
batch context and exact dynamic participant-owner admission; that is a
separate design/integration boundary, not needed to close this one-owner
invalidation gap.

## 2. Current selected-profile source boundary

`TesteSimulacao` composes the `ExpeditionStore` and `ExpeditionSystem`, and
passes the system to `SimulationRuntime`. `SimulationBootstrapComposition`
already requires the exact same store on the system and exposes an
`ExpeditionCensusProvider` for the installed store. The selected daily runtime
uses the system in both its normal daily loop and public composed API.

All attached active-row changes reach the store's serialized mutation
boundary:

- `Add` commits an admitted new active Expedition row.
- `AddAndFence` commits the reserved initial row in
  `TryStartExpedition`.
- `CommitReserved` commits start/return lifecycle associations, objective
  completion, and explicit cancellation compensation.
- `RemoveReserved` commits start-failure compensation.
- `Remove`, `Complete`, and `TryFinalizeCompletion` commit owner removal;
  exact completion transitions/removes the row in one commit.
- `TryMutate` is used by attached `ExpeditionRuntime` methods for progress,
  lifecycle, traversal, position, and objective state. It advances revision
  only when the captured owner snapshot changes.

### Direct live-object mutation audit

Every public attached `ExpeditionRuntime` state-changing method routes
through `Mutate`/`ExpeditionStore.TryMutate`: `TryBeginExploration`,
`TryAdvanceAbstractProgress`, both `TrySetCurrentLocalPlace` overloads,
`TryRecordObservedConnection`, `TryTraverseLocalConnection`,
`TryMarkObjectiveComplete`, and `TryBeginReturn`. Internal attached lifecycle
entry points `TryBeginTravel`, `TryArriveAtSite`, `TryBeginReturnTravel`, and
`TryCancelReturn` use the same path when called directly. Reserved system paths
invoke their `Core` variants only inside `CommitReserved`; final completion
uses `TryFinalizeCompletion` and its `TryCompleteCore` mutation. The
`OwnerSnapshot` covers state, party ID, current place, objective progress and
completion, visited places, and observed connections—the fields changed by
these paths. All direct writes found in `ExpeditionRuntime` are constructors,
these core mutators, attachment, or snapshot copies.

`Objective`/`ObjectiveRuntime` returns an `ExpeditionObjectiveRuntime`
reference, but its progress/completion mutators are `internal`; repository
call-site search finds them only inside `ExpeditionRuntime` core methods,
where an attached owner is already under `TryMutate` or `CommitReserved`.
Other callers read objective facts only. Visited-place and observed-connection
collections are exposed read-only. Keep the objective leaf mutators internal,
and route every public attached `ExpeditionRuntime` mutator through these owner
methods. The constructor-alias exception is specified below. If a future
consumer needs to mutate objective state directly, it must first add a same-owner
`ExpeditionRuntime` mutation method routed through `TryMutate`; exposing direct
objective mutation would bypass revision and P12 invalidation.

### Objective reference identity and alias exclusion

The public ExpeditionRuntime constructor retains the caller-provided
ExpeditionObjectiveRuntime reference, and the public Objective accessors expose
that same object. Two runtime rows can therefore share one objective instance.
If those rows attach to different ExpeditionStore instances, a mutation routed
through the second runtime changes the first row's objective while only the
second store advances its local revision and P12 section epoch. More subtly, a
runtime sharing an objective already attached to a store but not itself
registered in that store currently takes the standalone direct-mutation path.
Both cases are supported constructor/write paths in current code, so section
registration alone cannot make the selected owner's witness complete.

Before implementation can claim owner invalidation, bind each objective
instance to at most one ExpeditionStore for its attached lifetime and route
every runtime mutation through that association whenever one exists:

- Add a nonserialized ExpeditionStore attachment identity to
  ExpeditionObjectiveRuntime and an internal atomic bind operation. A compare-
  exchange of the first store reference is sufficient; later binds succeed only
  for that same store. The identity is runtime coordination state, not exported
  objective semantics or another census section.
- ExpeditionRuntime.TryAttach must reject a runtime already attached to a
  different store and bind its exact objective instance to the requested store
  before completing runtime attachment. ExpeditionStore.Add and AddAndFence
  already roll back their tentative active-row/index insertion when TryAttach
  rejects; rejection leaves active row count, local revision and P12 section
  epoch unchanged.
- Runtime mutation routing uses its attached ExpeditionStore when present;
  otherwise it checks the objective's bound store. If the objective has a bound
  store but this runtime is not its exact active row, the existing owner method
  rejects the mutation. Only an unbound runtime/objective pair keeps the
  standalone direct-mutation behavior.
- Keep binding monotonic for the lifetime of the objective/runtime object. Do
  not detach or re-home an objective when an Expedition row is removed; a
  retained reference must not become attachable to a second authority later.
  Reconstructed runtime objects establish fresh runtime bindings through the
  ordinary attach path. This does not claim save/load implementation.
- Same-store objective aliases remain supported. Mutations through any active
  ExpeditionRuntime sharing that objective execute under the same
  ExpeditionStore lock and owner callback, and the store-wide revision/section
  epoch records each actual committed owner change. Document this intentional
  same-owner shared-object behavior; do not incorrectly reject every alias.

Focused proof must construct two runtime rows with one objective, attach the
first to store A, and reject attaching the second to store B without changing
store B or its witness. Calling a mutator through that still-unattached second
runtime must also reject through store A without changing the shared objective
or store A's revision. A second case attaches two rows sharing the objective
to the same store, mutates through either row, and proves one local revision/
section report per actual commit. Exercise both ordinary Add and the reserved
AddAndFence start seam or otherwise prove they share the same attachment check.

Reservation creation/release and fences (`TryReserveNew`,
`TryReserveExisting`, `TryReserveObjectiveCompletion`, and `Release`) are
transient coordination state; they do not change the exported active-row
count/revision witness and remain outside this invalidation slice. Rejected
operations and successful no-ops do not advance the local revision.

### Existing start ordering and failure behavior

`TryStartExpedition` first validates/prepares the target and participants,
then allocates an ExpeditionId, creates the runtime row, and reserves two
Expedition owner revisions. It commits `AddAndFence`, calls
`TryStartTravelParty`, then commits `CommitReserved` with the party ID. A
throwing or rejected TravelParty start invokes `RemoveReserved`; a failed
second Expedition commit also attempts `RemoveReserved`. On success, the
ExpeditionStarted event is recorded after these commits. Existing consumed
IDs and successful compensating owner commits remain committed according to
their current authorities.

`TryStartExpedition` is called by both
`TesteSimulacao.TryStartExpedition` and
`AdventureExpeditionAutonomySystem.TryStartAutonomousExpedition` during the
selected daily advance. TravelParty start has its own owner mutation window
and P12-bound `TravelPartyStore`; participant travel state, travel charges,
and event sequence also use their existing owner boundaries. There is no
current outer P12 Expedition-start collector. Without this design's new
cross-owner collector, those owner commits remain separate epoch reports.

## 3. Registration and mutation interface

During selected-profile P12 inventory initialization, obtain the store from
the installed `ExpeditionSystem`, construct/reuse the provider for that exact
store, and validate:

- section ID `p12f.expeditions` and schema version 1;
- a nonnegative active-row cardinality equal to the store's active row count;
- exact owner identity equal to the installed `ExpeditionStore`;
- witness revision equal to the store's local revision.

Register this section as Required before sealing the selected-profile section
inventory. A missing system/store or invalid witness faults P12 admission
closed. Non-P12 standalone runtimes retain optional omission. At published
bootstrap composition, compare the runtime-registered provider with the
provider created by the composition and reject any different store identity.

Bind a runtime-only P12 mutation admission/committed callback pair to the
exact `ExpeditionStore` after the initial census baseline is established.
Keep callbacks nonserialized. The store invokes them under its existing owner
monitor, following its established owner commit order:

1. Perform current local guard, identity, state, exact-active-row, reservation,
   and local revision-capacity checks.
2. When P12 callbacks are bound, preflight owner-thread validity, exact store
   and provider identity, current section count/revision, unchanged-section
   baseline, and shared-epoch capacity before applying the owner mutation.
   Denial faults selected P12 and throws `InvalidOperationException` before
   applying the owner mutation. This prevents a caller that ignores a nested
   boolean from reporting successful selected-profile progress after P12
   admission has failed. Non-P12 and unbound callers keep their existing
   boolean behavior. Do not turn a failed/no-op domain mutation into a
   successful commit.
3. Apply the existing store mutation and advance its local revision once for
   each actual commit. `TryMutate` advances only when its owner snapshot
   changes. A compensation that changes the owner is its own commit.
4. After revision advancement, verify the exact live witness and report the
   `p12f.expeditions` section through the existing mutation-section dispatcher.
   A failed post-commit report faults runtime admission and throws; it does not
   roll back an already committed row or transition.

With no selected-P12 callbacks, standalone/non-P12 behavior remains unchanged.
Genesis owner rows present before selected-profile baseline establish that
baseline without synthetic invalidations.

## 4. Batching, ordering, and partial-progress limits

`CanCommitP12MutationSections` and `NotifyP12MutationSections` already
coalesce changed IDs while one supported TravelParty, Merchant, or solo-travel
context is active. The Expedition owner callback should use these paths. It
must not create a second context when one of those supported contexts is
already active; the one-section Expedition write joins it and is accounted at
the existing outer boundary.

`TryStartExpedition` itself does not currently run under any of those batch
contexts. Its nested `TryStartTravelParty` uses a TravelParty mutation window,
not a P12 changed-section collector. Therefore the first owner-invalidation
slice reports Expedition and TravelParty/participant owner commits separately
in their actual commit order. In start failure, successful compensation
commits also report separately. This preserves the current owner ordering and
compensation code without claiming a single cross-owner epoch, rollback, or
atomic start.

Here “nested” describes the synchronous call graph, not nested store locks.
The current start windows are sequential: `AddAndFence` commits/releases the
ExpeditionStore monitor; `TryStartTravelParty` enters, mutates, and releases
the TravelPartyStore window; then `CommitReserved` or `RemoveReserved` enters
ExpeditionStore again. Return follows the same pattern: the `Returning`
`CommitReserved` completes/releases before the TravelParty call, and the
association or cancellation `CommitReserved` follows after that call returns.
The P12 wrapper must not hold either store monitor across the other owner's
operation or introduce a nested cross-store lock order. Preserve the current
owner commit, notification, and compensation order, including partial owner
commits if a later step fails; this design adds no cross-owner rollback.

If later evidence requires exactly one completed-operation epoch for the full
Expedition start, that work must add a distinct serialized
`SimulationRuntime` context around the existing `TryStartExpedition` method,
compose its dynamic participant/City/account/Inventory/TravelParty/Expedition
sections, and prove reservation/capacity behavior across every early return,
exception, and compensation path. It must be independently designed and
reviewed. It is not silently part of this owner-level design.

## 5. Files and implementation order

Retained implementation candidate evidence exists on
`codex/phase12/P12BExpeditionCensusImplementation` at
`8be5be36d347fce84d1e583b4539a7d2b20e16b9`, based on common canonical
`9308339`. Its current diff is tests-only in `ExpeditionTests` and
`ExpeditionExplorationTests`; preserve its branch/worktree without cleanup or
rebase. Those tests are candidate test assets, not ExpeditionStore runtime
operation coverage or evidence that the P12 owner invalidation hooks exist.
When implementation begins, refresh/revalidate this evidence against the
then-current canonical base and the exact implementation tree; do not infer
that passing reservation tests establish P12 epoch admission or notification.

Expected implementation changes are limited to:
- ExpeditionRuntime.cs / ExpeditionObjectiveRuntime — monotonic nonserialized objective-to-store binding at the attachment seam; mutation routing must reject cross-store and unregistered aliases while allowing same-store aliases.

- `Assets/_Project/Scripts/ExpeditionStore.cs` — runtime-only callbacks at
  every existing revision commit point, before-write admission, and
  post-revision notification;
- `Assets/_Project/Scripts/SimulationRuntime.cs` — selected-profile required
  registration, exact-owner validation, and callback binding/helpers using
  the existing direct/batched dispatcher;
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs` — verify the
  published Expedition provider matches the runtime owner;
- focused Expedition/P12 census and runtime/bootstrap tests.

Do not change `ExpeditionSystem` ordering or `TravelPartySystem` behavior for
this owner-only slice. The `SimulationRuntime` file is a serialized hotspot;
do not integrate it concurrently with ScheduledDirective, P15/P16 profile
admission, or other callback-context changes. Rebase/revalidate against the
then-current canonical tip before implementation integration.

## 6. Required implementation and validation evidence

Focused coverage must additionally prove cross-store and unregistered objective-alias mutations are rejected without owner revision/epoch changes, while same-store aliases mutate through one shared store section.
Focused coverage should prove exact required section/schema/store identity;
selected-profile missing-owner fail-closed behavior; optional standalone
omission; initial genesis baseline; each direct and reserved store commit;
start's `AddAndFence`, `CommitReserved`, and `RemoveReserved` revision/epoch
ordering; all attached `TryMutate` classes; exact completion/removal; and no
revision/epoch for reservation-only state, rejected operations, or no-ops.
Verify stale owner/revision and epoch-capacity denial precedes owner mutation,
and post-commit report failure faults the runtime while preserving the
committed owner revision. Verify nested TravelParty start keeps its current
order and compensation attempts and that the sections report separately (or
join an already active supported batch without a duplicate increment).

Run focused Expedition, TravelParty, runtime admission, and bootstrap suites;
then required ALL EditMode, official Smoke, and `git diff --check` on the exact
code tree. Record artifact paths and SHA-256 hashes, then obtain independent
exact-tip implementation review. A changed code tree requires current-base
review and validation.

## 7. Exclusions and readiness

This is selected-profile invalidation of the existing ExpeditionStore owner
section only. It does not add or batch a new Expedition-start operation; bind
other RuntimeIdAllocator counters; add sequence semantics; close every
TravelParty, economy, NPC, Knowledge, City, content, Justice, or other owner
write; or claim the full state of an Expedition's collaborators in this
section. It adds no temporal scheduler, activity API, mod hook, or new
gameplay. It does not establish complete live-profile census, complete
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12
closure.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked. This proposal is ready only for independent technical review; it is
not implementation authorization or canonical promotion.
