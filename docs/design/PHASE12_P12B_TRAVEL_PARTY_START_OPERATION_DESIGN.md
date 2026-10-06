# P12-B selected-profile TravelParty start operation design

**Status:** ready for independent technical review. This is a bounded P12-B
prerequisite capability design, not an implementation or a readiness claim.
It does not add a Phase checkpoint ID, gameplay rule, or Phase 12 closure.

**Canonical base:** `codex/phase12/canonical` at
`29162cd0cf63e31f9542e612023a7256ac36ca4c`.

**Architecture:** `codex/architecture/world-identity-projection` at
`ffd75652d89d862b83d634868c560f8540869b89`; intraday/extensibility alignment
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`; multi-participant activity
alignment `c285466c355103d3637ac165246591b72eb7bda0`.

**Planning authority:** `docs/phases/PHASE12_BRIEF.md`,
`docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, and the accepted
P12-B prerequisite-capability authorization. The current State remains
P12-B `INCOMPLETE`, P12-A `WAIT_DEPENDENCY`, P13 `BLOCKED`.

## Source finding

The selected-profile public start path is
`SimulationRuntime.TryStartTravelParty` → `TravelPartySystem.TryStartTravelParty`
(with `TesteSimulacao` forwarding to the runtime). The system validates the
existing action and participant rules, then allocates a `TravelPartyId`, starts
each member's travel, charges each member, inserts the party, associates the
members with it, records `TravelPartyStartedEvent`, and discovers the origin
location and route for each member.

The call is a multi-owner operation, but the P12 runtime currently registers
only `runtime.travel-party.advance`. The existing passive allocator provider
already defines `p12c.runtime-id-allocator.travel-parties`, but that counter is
not registered or mutation-bound. The exact TravelParty store, participant
NPC travel state, source City presence, NPC MoneyAccounts, per-NPC spatial
Knowledge, Event counter, and record sequence already have their own census or
mutation hooks. With no start scope, their notifications can publish multiple
partial epoch steps and a consumed TravelParty ID is not represented in the
P12 epoch.

There is a second call path to preserve: `ExpeditionSystem` calls
`TravelPartySystem.TryStartTravelParty` directly for P12-F Expedition start and
return. Those calls bypass `SimulationRuntime.TryStartTravelParty` and remain
outside this P12-B slice. The new TravelParty-start admission seam rejects an
unwrapped `TravelPartySystem` start before its TravelParty ID allocation or
TravelParty/NPC/account/Knowledge/Event/sequence writes. It is not a P12-F
Expedition admission adapter: `ExpeditionSystem` can allocate its own ID and
commit its own store before calling TravelParty. Do not count or change those
P12-F writes here; if the refreshed selected profile can reach an Expedition
write, record that as a separate P12-F dependency gap. Unbound runtimes retain
their current behavior.

## Bounded contract

Add one named selected-profile operation, `runtime.travel-party.start`, for a
successful or partially committing group start entered through
`SimulationRuntime.TryStartTravelParty`. Current production call sites are the
`TesteSimulacao` runtime forwarder and the two excluded direct Expedition calls;
this operation is standalone on that current path. It does not wrap Expedition
start or return and does not change the existing performer/support eligibility,
participant ordering, group formation, route checks, charges, start timing,
event semantics, or rollback behavior.

Keep TravelParty instance identity separate from participant identity. The
allocator counter is one singleton owner. Each prepared member is a distinct
installed `NpcRuntime` selected by its existing `RuntimeId`; each per-NPC
travel, account, and Knowledge witness must bind to that exact runtime owner.
The existing action rules determine the member set and cardinality. This slice
adds no universal participant limit and makes no P20 Activity identity or
participant-count claim.

### Admission and unsupported direct entry

The start boundary must be entered after `TravelPartySystem.TryPrepareTravel`
has produced the exact members, costs, origin Location, and origin City, but
before `AllocateTravelPartyId()` or any owner mutation. Use an internal,
runtime-bound preparation callback at that point. The selected-profile runtime
wrapper establishes a synchronous one-call request before invoking the system;
the callback admits only that request, validates the exact prepared owners,
and opens the named protocol operation. The runtime wrapper closes the scope
in `finally`, including when the operation returns `false` or throws.

When a P12-bound `TravelPartySystem` is invoked without that runtime request,
the preparation callback rejects it before ID allocation. This is the
fail-closed boundary for the TravelParty operation only; it does not add an
Expedition checkpoint, edit `ExpeditionSystem`, or register an Expedition
operation. A rejection caused by this unsupported direct call need not fault
an otherwise healthy P12 runtime. If the runtime request exists but exact-
owner admission, baseline, thread, or capacity validation fails, fault the
selected P12 runtime closed before the first TravelParty-start write. With no
selected P12 callback, non-P12 behavior remains unchanged.

The temporary request is internal synchronous control flow, not player/actor
authorization or a new public command contract. It is set and cleared by the
runtime wrapper and cannot remain active after a call exits.

### Exact owners

The preflight builds one de-duplicated section set from the prepared inputs and
current composition. Require exact identity, schema, cardinality, and current
revision for every included owner:

| Owner section | Exact owner and reason |
|---|---|
| `p12c.runtime-id-allocator.travel-parties` | The runtime's exact `RuntimeIdAllocator`, cardinality one; the TravelParty sequence advances immediately after preparation and is not rolled back. |
| `p12f.travel-parties` | The exact installed `TravelPartyStore`; party insertion and possible compensating removal use its existing local revision. |
| `p12f.npc-travel-state/{RuntimeId}` | One exact installed `NpcRuntime` per prepared member; start, party association, and possible compensation alter its travel commitment. |
| `p12e.npc-money-account/{RuntimeId}` | Each exact member `MoneyAccountRuntime`; group charge and possible refund are owner writes. Include every prepared member's account, including zero-cost members, while reserving revision capacity only where the actual cost is positive. |
| `p12b.city.important-npcs/{CityRuntimeId}` | Every exact currently occupied City projection that `StartTravel` removes a member from, plus the prepared origin City projection used by compensation. De-duplicate by exact City instance and validate its installed provider. |
| Existing per-NPC SpatialKnowledge location and route sections | Both sections for each exact member Knowledge owner; a successful start may discover the origin Location and selected Route. Keep their existing separate section identities while batching their common owner revision. |
| `p12c.runtime-id-allocator.events` | The exact allocator Event counter, which `DomainEventRecorder` consumes if it records the start event. |
| `p12c.simulation-record-sequence` | The exact record-sequence owner, which the same event path consumes after EventId allocation. |

Do not add read-only TravelPlan, destination City, destination Location/Route,
Inventory, EventStore, or Expedition owners to the changed-section set. The
City set is derived from actual member presence and the existing origin City;
do not assume that every member occupies a City projection. The operation
continues to rely on the existing account, travel, City, Knowledge, Event, and
sequence callbacks for their local preflight and post-commit reporting.

### Capacity and commit reporting

Before the operation's first mutation, validate all listed section baselines,
the current selected-profile owner thread, exact provider/store/registry
identity, distinct participant RuntimeIds, and capacity for one shared epoch
step. Preserve and supplement existing local saturation preflights:

- NPC travel-state revisions must retain the existing four-increment
  `CanStartTravelProjectionTransaction` headroom per member for StartTravel,
  party-ID association, and the existing compensation sequence.
- Source/origin City-presence revisions must retain the existing aggregate
  headroom for member removals and possible compensation additions.
- TravelPartyStore must retain its existing two-commit headroom for Add and
  compensating Remove.
- Each positively charged MoneyAccount must have headroom for debit and the
  existing possible refund before any group charge begins.
- Each Knowledge owner must have headroom for the actual possible origin and
  route discoveries; no-op discoveries do not increment revision.
- The TravelParties allocator counter preflights its own non-wrapping next ID
  before increment. Existing Event and record-sequence allocation boundaries
  retain their non-wrapping checks.

Only the exact prepared operation sections participate in this scope. Existing
owner callbacks append committed section IDs to one runtime-owned set while
the scope is active; they must recognize this scope alongside the current
TravelParty-advance, Merchant, and solo-travel scopes. Update the shared
`NotifyP12MutationSections` dispatch and epoch-reservation checks narrowly for
this context. On scope exit, notify once with the unique set; an empty set does
not advance the epoch. A committed TravelParty ID remains in the set when a
later start step returns `false` or throws. Partial owner commits and existing
compensation revisions are reported, not rolled back by the P12 adapter.

Preserve current order and result behavior: the ID is allocated before member
travel starts; travel charges follow successful member starts; Party insertion
and member association precede event recording; a rejected event follows the
existing member/party/charge compensation; Knowledge discovery follows a
recorded event. No retry, reordered writes, new rollback, or event policy is
introduced. If an observer/notification failure occurs after an owner commit,
fault P12 admission closed and retain the committed domain result.

## Implementation and integration boundary

Reuse the existing provider and TravelParty/NPC/City/Account/Knowledge/Event/
sequence authorities. Add only the missing TravelParties allocator provider
registration/binding, its counter-specific P12 mutation boundary, the named
start operation/context, and the internal preparation admission seam needed
to exclude direct P12-F entry. Adapt each current callback to batch through
the new context; do not add a generic operation framework.

The serialized code hotspot is `SimulationRuntime` plus its one-way binding to
`TravelPartySystem` and `RuntimeIdAllocator`. Do not edit `ExpeditionSystem`
or the P20 activity owner. Keep P12-F excluded. Revalidate current P7/P8/P16
and P17 contracts, but this local P12 continuation adapter does not change
their domain behavior. P17-A's explicit withdrawal/concession checkpoint is
already promoted in P17 canonical and is not part of this work.

## Required tests and validation

- Exact allocator TravelParties witness registration: section ID, schema,
  exact owner identity, cardinality one, and revision equal to the current
  TravelParty counter. Successful allocation increments exactly once;
  exhaustion/admission rejection changes no counter.
- Runtime-start success admits `runtime.travel-party.start` and advances one
  shared epoch for the unique committed section set despite multiple owner
  callbacks. Cover multiple performers/supports without adding a new count
  policy, plus an already-known discovery no-op.
- A valid preparation followed by an existing failure after ID allocation
  returns the same result and preserves the consumed ID; the epoch includes
  every actual owner revision, including compensation. Cover failed later
  debit/commit and event-store rejection without changing current transaction,
  event, or refund semantics.
- An unwrapped direct `TravelPartySystem` call under the selected P12 profile
  is rejected before TravelParty ID allocation, charge, member state, party
  store, or Event/sequence change. Do not assert that this test proves
  Expedition owner invalidation; Expedition remains P12-F and outside this
  slice. The equivalent unbound runtime path keeps existing behavior.
- Admission/preflight failure leaves the counter and every owner unchanged;
  wrong-thread/stale or same-ID replacement owners fail closed before writes.
- Saturation tests cover allocator counter, NPC travel revisions, City
  presence, account revisions, Knowledge revisions, PartyStore, EventId, and
  record sequence. Confirm no owner changes without its report and no
  operation-scope leak after `false`, exception, or notification fault.

At implementation time run the focused TravelParty start/group travel,
TravelParty census, NPC owner, account, City-presence, SpatialKnowledge,
runtime-admission/orchestration and bootstrap-composition suites; affected P8
travel regressions; ALL EditMode; official Smoke; and `git diff --check`.
Retain inspectable XML/log hashes against the exact code tree. A changed code
tree requires fresh exact-tip implementation review.

## Limits and readiness

This slice covers only the normal selected-profile runtime group-start path
and the exact owner commits it can perform. It does not cover Expedition
start/return, all TravelParty APIs, arbitrary standalone writers, other
allocator counters, complete P12-B owner/operation/shared-epoch coverage,
global quiescence, capture eligibility, export, hydration, P12-A, P13, or
Phase 12 closure. P12-B stays `INCOMPLETE`; P12-A stays `WAIT_DEPENDENCY`;
P13 stays `BLOCKED`.
