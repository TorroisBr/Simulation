# P12-B TravelParty owner census design

**Status:** Revised after independent review; awaiting exact-tip re-review. This is a
bounded owner-census sub-capability within the accepted P12-B scope. It adds
no checkpoint ID, product behavior, capture eligibility, or readiness claim.
It is a design artifact only; implementation follows the accepted P12-B
review gates and the dependency order below.

**Canonical base:** `codex/phase12/canonical` at
`f961477380508647d2975272da72d96892944ed9`.

**Scope authority:** `docs/phases/PHASE12_BRIEF.md`, the accepted
`docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, and the current
P12-B evidence gaps in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. The
blocker record explicitly identifies the selected profile's `TravelPartyStore`
as composed-but-empty with only a direct empty-list observation and no owner
revision witness. This design resolves only that one passive census gap.

## 1. Evidence contract

Publish one schema-v1 owner section for the exact `TravelPartyStore` instance
already composed by `UnityBootstrap-Daily-v1`:

| Field | Contract |
|---|---|
| Section ID | `p12f.travel-parties` |
| Schema version | `1` |
| Section role | `Required`: travel parties may be populated during supported play. The selected day-zero sample happens to contain zero; it is not an excluded or permanently empty owner. |
| Owner identity | The exact `TravelPartyStore` instance supplied to `SimulationBootstrapComposition`; repeated reads retain reference identity. It must be the same store exposed by `TravelParties` and `GroupTravel.Store`. |
| Cardinality | `TravelPartyStore.ActiveParties.Count`, the exact number of current store members. Do not count party members or derive the value from NPC travel fields. |
| Revision | A new store-owned, read-only monotone `long Revision`, initialized to zero and incremented once per successful actual `Add`, `Complete`, or `Remove`. |

Use one passive `IOwnerSectionCensusProvider` that captures the exact store
reference and returns an `OwnerSectionCensusWitness` with those fields. A
proposed provider is `TravelPartyCensusProvider`; expose one fixed provider on
`SimulationBootstrapComposition` constructed from its installed
`TravelParties` owner. Keep the provider set fixed to that owner; do not accept
an arbitrary store list, rebuild the store, or use a second identity token.

The count and revision are separate evidence. A successful Add followed by a
successful compensating Remove changes the store twice even if its final count
matches the earlier count. Record both revision increments. The section is
owner-backed census metadata, not a party snapshot or a claim that a matching
revision represents a reconstructable travel state.

## 2. Store revision and commit boundary

Keep mutation authority in `TravelPartyStore` and preserve its existing public
operation semantics:

- `Add` checks the mutation guard, validates the party and member uniqueness,
  and checks for existing party/member conflicts before changing either
  collection. After all rejection checks, preflight revision capacity, add the
  same party to `partiesById` and `activeParties`, then advance revision once.
- `Complete` resolves the existing party and preflights revision capacity
  before changing party state. It marks that party completed, removes it from
  both indexes, and advances revision once. A missing party is a no-op. A
  completed party is not left in `ActiveParties`.
- `Remove` resolves the existing party and preflights capacity before removing
  it from both indexes, then advances revision once. Preserve current behavior:
  Remove does not mark the detached party completed.
- Guard rejection, invalid/null/inactive party, duplicate party or member,
  missing ID, and other rejected/no-op paths leave count, party state, and
  revision unchanged.
- At `long.MaxValue`, reject a would-be store mutation before its first
  mutation. Do not wrap, partially update an index, mark a party completed, or
  issue a witness with a fabricated revision. Test saturation by setting the
  private revision through reflection, consistent with existing census tests;
  do not add a production test setter.

Revision increments describe successful store-owner commits, not an outer
`TravelPartySystem` outcome. `TryStartTravelParty` can successfully add a party
and later remove it as compensation if arrival-event recording fails; those are
two successful store commits and must produce two increments. A failed start
that never reaches Add produces none. A compensation Remove is not suppressed
because the outer start returned false.

Before allocating the party ID or changing member travel state, charging
costs, or recording an event, `TryStartTravelParty` must preflight capacity for
both possible store commits: Add and, if start-event recording fails, its
compensating Remove. This call is a synchronous runtime operation; no other
supported `TravelPartyStore` writer runs between its Add and possible
compensation. Therefore the two-slot check is sufficient without adding a
reservation API or changing the public store contract. At revision
`long.MaxValue - 1`, reject start before any non-store effect; at revision
`long.MaxValue - 2`, Add followed by successful compensation can reach exactly
`long.MaxValue`. If both commits succeed, retain both revision increments.

### Saturation and arrival atomicity

`TravelPartySystem.AdvanceParties` currently advances member state and records
arrival before it calls `TravelPartyStore.Complete`. If Complete can reject at
revision saturation, a late failure could leave the members arrived while the
party remains active. The implementation must preflight the store's ability to
complete that party before applying the final arrival mutation (after the
existing synchronization/arrival checks, but before the first member advances
on the arrival boundary). If capacity is exhausted, leave the party, members,
Knowledge, and event sequence unchanged for that attempted arrival. This is a
local overflow-consistency check only; at ordinary revision values, preserve
the current daily travel ordering, costs, member progress, event handling,
and arrival behavior. Do not weaken `Complete`'s own capacity preflight.

No census provider is allowed to mutate, bind, or bypass the existing
`AuthoritativeMutationGuard`. `TravelPartyStore` already implements
`IAuthoritativeMutationGuardBindable`; its `Add`, `Complete`, and `Remove`
check the bound guard. `SimulationRuntime` binds the `TravelPartySystem`, which
binds the party store. The provider only reads the installed store. A denied
mutation must leave the witness unchanged. Do not add a second guard or
operation scope.

## 3. Bootstrap evidence and verification

Extend
`SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
to read the provider published by the normal `Simulation-GeneralTest.asset`
bootstrap and assert:

- exactly one provider is published for the one installed party store;
- section ID and schema version are `p12f.travel-parties` / `1`;
- role is registered as `Required` by the later canonical P12-B protocol
  integration (zero at startup does not make the section `ExplicitlyEmpty`);
- owner identity is the exact `Bootstrap.TravelParties` reference and agrees
  with `Bootstrap.GroupTravel.Store`;
- day-zero cardinality and revision are both exactly zero;
- repeated reads retain that owner identity, cardinality, and revision.

Add focused `TravelPartyCensusTests` for the provider and owner lifecycle:

1. A valid Add changes `(count, revision)` from `(0,0)` to `(1,1)`.
2. Complete changes `(1,1)` to `(0,2)`, marks the detached runtime completed,
   and a repeated Complete does not advance revision.
3. Remove changes `(1,1)` to `(0,2)` without changing the detached party's
   existing active/completed flag; a repeated Remove is a no-op.
4. Null/inactive, duplicate-ID, overlapping-member, missing-ID, and
   guard-denied mutations leave exact witness values and existing party state
   unchanged.
5. A successful Add plus successful compensating Remove yields two revision
   increments even though final cardinality is zero. Inject a start-event
   recording failure at revision `long.MaxValue - 2`; verify the party is
   removed, members and costs are rolled back, and revision reaches exactly
   `long.MaxValue`. At revision `long.MaxValue - 1`, verify the same start is
   rejected before ID allocation, member mutation, cost charge, or event
   recording, leaving gameplay state and witness unchanged.
6. At `long.MaxValue`, valid Add/Complete/Remove attempts fail before changes;
   for Complete, `IsCompleted` remains false. Saturated final-arrival coverage
   proves `AdvanceParties` does not progress/clear members or record an arrival
   before the store can commit Complete.
7. After an ordinary successful start, group travel has one active party and
   one store revision increment. Intermediate travel-day progress does not
   change party-store membership/revision. Successful arrival removes the party
   and increments revision once.

Use existing `GroupTravelTests` fixtures and invariants where they make the
outer commit path easier to exercise; do not change travel requirements to
simplify census tests. Re-run the selected-profile bootstrap test and the
focused `GroupTravelTests` suite when implementing. The full implementation
candidate must follow the applicable current P12 validation gate and
`git diff --check`; this design branch itself does not run Unity tests.

## 4. Reconstruction sensitivity and limits

TravelParty state is continuation-sensitive, not a disposable diagnostic or
read model. The accepted P12-F contract requires exact active commitment state
including stable party identity, legacy origin/destination/route references,
traveler and escort membership, cost snapshots, total travel duration,
origin decision identity, lifecycle, and reciprocal NPC travel bindings.
`TravelPartyRuntime` retains these values; individual progress and related
state also live on each `NpcRuntime`. Losing or regenerating an active party can
change who travels together, costs, destination/route references, completion,
and subsequent outcomes.

This P12-B slice proves only that the selected runtime has a concrete party
owner and that its active membership has a current cardinality/revision
witness. It does not export party fields, hydrate them, validate their graph,
prove continuation parity, or replace owner revisions with a digest. A
nonzero witness is still not a restorable party snapshot. P12-F must preserve
these facts under its own dependency gate.

Reads are unsynchronized. The witness is usable only within the eventual
P12-B owner-thread/quiescence and post-collection revalidation protocol. It
does not prove owner-thread identity, quiet state, shared-epoch notification,
or absence of concurrent callers. A read-only `ActiveParties` wrapper does
not make its backing list synchronized or provide a detached snapshot.

## 5. Integration order, ownership, and dependencies

**Hard integration prerequisite:** complete and promote the in-flight NPC
Inventory census work's shared P12-B protocol/provider-composition API first.
Refresh this design against that exact canonical tip, reuse its owner-section
contract and registration path, and avoid a competing edit to the shared
composition surface. The Store-local counter/provider may be developed only
under a separately isolated ownership window after that integration boundary
is canonical. Do not register an alternate coordinator, mutation epoch, or
capture path.

Proposed implementation ownership after that prerequisite:

- `Assets/_Project/Scripts/TravelParty.cs`: the owner-local revision and its
  Add/Complete/Remove commits; the two-commit start-capacity preflight and
  local final-arrival capacity preflight in `TravelPartySystem` described
  above. This is the spatial/travel shared
  semantic hotspot and requires exclusive ownership while edited.
- New `Assets/_Project/Scripts/TravelPartyCensusProvider.cs`: the passive
  schema-v1 adapter over the exact installed `TravelPartyStore`.
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`: publish the
  fixed provider from the already-composed store, only after Inventory's
  shared protocol/composition changes are canonical.
- New `Assets/_Project/Tests/EditMode/Editor/TravelPartyCensusTests.cs`, plus
  narrow additions to `SimulationBootstrapCompositionTests.cs` and
  `GroupTravelTests.cs` as needed. Do not edit `SimulationRuntime.cs`,
  `SimulationTime.cs`, P18's advance lease, or the broader persistence
  composition in this slice.

Relevant current source evidence at the exact base:

- `TravelParty.cs:31-143`: party identity, route/endpoints, member/cost
  snapshots, mutable completion flag, and internal `MarkCompleted`.
- `TravelParty.cs:205-346`: exact store-owned active list and ID map, public
  ActiveParties view, `Add`/`Complete`/`Remove`, and mutation-guard binding.
- `TravelParty.cs:396-529`: group-start ordering, store Add and the successful
  compensating Remove after an event-recording failure.
- `TravelParty.cs:535-623`: daily party progression and arrival ordering; the
  saturation preflight must occur before final member mutation.
- `ExpeditionSystem.cs:807-819`: after a successful return-party start,
  `TryBeginReturnTravel` associates its nonempty ID with an expedition already
  moved to `Returning`; a defensive failure branch compensates with a direct
  `TravelPartyStore.Remove`. Under the supported serialized synchronous flow,
  the return-state transition just succeeded, the created party ID is
  nonempty, and `TryStartTravelParty` has no callback that mutates expedition
  state before this association. Thus the association cannot fail under its
  current supported preconditions. Retain the defensive branch, but classify
  it as unreachable in the rollback matrix; do not rely on it as a reachable
  third store commit in the two-slot start-capacity check. If these preconditions
  change, the caller must preflight capacity for its own compensation before
  starting the party.
- `TravelParty.cs:627-659` and `SimulationRuntime.cs:455,1050`: party system and
  store guard binding in the live runtime.
- `SimulationBootstrapComposition.cs:22-24,102-103`:
  bootstrap owns the exact passed TravelPartyStore and exposes it with its
  system.
- `SimulationBootstrapCompositionTests.cs:180-183,634`:
  selected normal-profile bootstrap case and its existing direct empty-list
  assertion, which this provider assertion replaces/strengthens.
- `GroupTravelTests.cs:220-239,449-476`: successful start, arrival, and
  aggregate rollback coverage to retain; `RuntimeAuthoritativeMutationGuardTests.cs:367-370`
  confirms the existing bind path.

Planning dependencies: P12-B's accepted profile and exact census protocol;
the Inventory shared API canonical integration above; the exact selected
bootstrap owner. This is not an implementation of P12-F. The Brief order
remains P12-B → P12-C → P12-D/P12-E → P12-F → P12-G. P12-F export/hydration
must wait until C, D, and E are satisfied even though this passive P12-B
owner-census slice can be prepared earlier. It does not complete P12-B, make
P12-C through P12-G READY, make P12-A READY, or authorize final profile
integration.

## 6. Exclusions

- No travel decision, party formation, time-step, cost, arrival, retry, event,
  or daily travel behavior change at ordinary revisions.
- No P12-F party export/hydration, save envelope, reconstruction, graph
  validation, or continuation-parity claim.
- No shared P12-B epoch notifications or partial writer instrumentation.
- No runtime owner-thread binding, quiescence fence, capture token, or capture
  eligibility.
- No edits to `SimulationRuntime`, `SimulationTime`, `TravelSystem`, or the
  P18 advance lease; the local `TravelPartySystem` saturation preflight is
  limited to preventing a partial arrival if the new owner counter cannot
  commit.
- No new checkpoint ID, product semantics, phase readiness, or P12-A
  implementation authorization.
