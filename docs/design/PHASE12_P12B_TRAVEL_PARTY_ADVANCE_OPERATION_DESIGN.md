# P12-B selected-profile TravelParty advance operation design

**Status:** bounded technical design proposal; awaiting independent exact-tip
design review. This is a sub-slice of the already accepted P12-B capability
scope. It adds no gameplay rule, checkpoint ID, capture eligibility, or Phase
readiness claim. Implementation may proceed only after the design review passes
and its assumptions still match canonical.

**Canonical base:** `codex/phase12/canonical` at
`0f36331d84ad3139171d36f980dfe6fa635ae30c` (promotion State and blocker-matrix
refresh after FR-B live integration).

**Contract authority:** `docs/SIMULATION_ARCHITECTURE.md`,
`docs/ROADMAP.md`, `docs/EXECUTION_MODEL.md`, `docs/phases/PHASE12_BRIEF.md`,
the accepted `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`, and
the current P12-B blocker matrix. The earlier TravelParty passive census
design/review/candidate remains preserved in Git history; its exact store
identity, active-party cardinality, local revision, store-local serialization,
and saturation rules are prior capability evidence, not work to restart.

## Current source boundary

For `UnityBootstrap-Daily-v1`, `SimulationRuntime.TryAdvanceDay` enters the
existing `runtime.advance-day` operation before calling
`AdvanceDayAfterClockAdvance`. That method runs actor turns, then calls
`TravelPartySystem.AdvanceParties`, followed by legacy `TravelSystem` travel
advancement. The TravelParty call has no named nested P12 operation today.
P18 intraday execution is a distinct path and is outside this slice.

`AdvanceParties` can commit these independently owned facts:

1. Per-member travel-progress fields on `NpcRuntime`: clear the
   `travelStartedToday` flag or decrement remaining days; on arrival it also
   clears destination, route, total-days and origin-decision fields.
2. A destination City's `ImportantNpcs` projection and revision when a member
   arrives in a City.
3. Per-member SpatialKnowledge location discovery at arrival.
4. A `TravelPartyArrivedEvent`; successful recording also allocates the
   already registered `SimulationRecordSequence` owner.
5. Each arriving member's `activeTravelPartyId` clear and the exact
   `TravelPartyStore.Complete` commit.

The current `TravelPartyStore` and City-presence census providers are already
published by bootstrap but are not registered in the sealed P12 protocol.
Per-NPC SpatialKnowledge and SimulationRecordSequence sections are registered.
No per-NPC travel-progress revision/provider exists. The current source audit
confirms this is the highest-value uncovered selected-profile cross-owner
continuation boundary after FR-B; the FR-B code delta did not alter
`TravelParty.cs` or these owner mutators.

## Bounded owner contract

### NPC travel-progress section

Add one required schema-v1 section for each exact installed `NpcRuntime`, with
section ID `p12f.npc-travel-state/{RuntimeId}`, owner identity the exact
`NpcRuntime` reference, cardinality exactly one, and an owner-local monotone
revision. The witness is census metadata only; it exports no travel payload.
The revision tracks actual changes to these travel-commitment fields:

- destination Location/City references;
- remaining and total travel days;
- route RuntimeId;
- `travelStartedToday`;
- origin DecisionId;
- active TravelPartyId.

Do not derive the TravelParty owner count from these NPC sections. Party
instance identity remains separate from member NPC RuntimeIds; each party may
contain multiple travelers and escorts. Keep current Location/City references
outside this travel-progress field set; they remain NpcRuntime presence facts,
while the City's `ImportantNpcs` collection is its reciprocal projection.
Neither projection count nor travel-progress revision replaces the future
exact NpcRuntime location snapshot, and this census-only section adds no
duplicate field export.

Increment the travel-progress revision once for each successful mutator call
that changes one or more listed fields, and not for rejected/no-op calls. The
write inventory is `StartTravel`, `AdvanceTravelDay`, `CancelTravel`,
`SetActiveTravelPartyId`, and `ClearTravelStartedToday`; verify by searching all
assignments at implementation time. Preflight revision capacity before the
first mutation in each call. Coordinators that can partially compensate or
change multiple members must preflight the complete maximum number of required
travel-progress revisions before their first owner/economy/event commit. Never
wrap or permit an unrevisioned state change at saturation.

The provider set is a dynamic exact-NPC family: one unique RuntimeId and one
reference-identical NpcRuntime per installed roster entry, with no missing,
duplicate, stale, or same-ID replacement owner. Integrate its expected
sections, witnesses, baseline validation, and roster reconciliation through
the existing specialized NPC-roster protocol; do not add generic post-seal
section registration. Roster/materialization changes must reconcile this
family atomically with the currently supported dynamic families and changed
fixed sections.

### Other operation owners

Register only the already composed owners needed by this operation:

| Owner section | Exact identity and witness | Existing source |
|---|---|---|
| `p12f.travel-parties` | The exact installed `TravelPartyStore`; active party count and its existing monotone revision | `TravelPartyCensusProvider` |
| `p12b.city.important-npcs/{CityRuntimeId}` | Each exact installed City; `ImportantNpcs.Count` and `ImportantNpcRevision` | `CityNpcPresenceCensusProvider` |
| `p12f.spatial-knowledge.locations/{RuntimeId}` and `p12f.spatial-knowledge.routes/{RuntimeId}` | Both sections of the same exact per-NPC SpatialKnowledge owner and its shared revision | Existing dynamic SpatialKnowledge family |
| `p12c.simulation-record-sequence` | Exact existing sequence owner/revision | Existing P12 adapter |

All sections are `Required` in this selected profile when their owner exists;
zero current count does not reclassify a mutable owner as permanently empty.
City providers must retain and validate the current installed-NPC roster view
after supported NPC materialization or removal. Update the existing membership
reconciliation boundary to include City sections when their projection
commits; do not claim this closes all City mutation paths unless each supported
path is actually instrumented.

## Operation boundary and commit reporting

Register `runtime.travel-party.advance` as a selected daily-profile operation.
Wrap only the existing `TravelPartySystem.AdvanceParties` call when the active
runtime admission profile is `UnityBootstrap-Daily-v1`; nest it inside the
already active `runtime.advance-day` scope. Leave the P18 intraday path and
legacy runtimes without P12 admission unchanged.

Before entering the call, validate exact current baselines for the registered
TravelParty, City-presence, NPC travel-progress, SpatialKnowledge and sequence
sections that the operation can touch. Verify that the party store, providers,
member registry and all section IDs still identify the selected composition.
If operation admission or preflight fails, fault P12 admission closed before
the TravelParty call mutates anything.

Use one runtime-owned operation context with an ordinal set of changed section
IDs. Owner mutation notifications reached during this operation append to the
set; they do not advance the shared epoch individually. Existing single-owner
notifications outside this nested operation retain their current behavior.
After the call, notify the protocol once with the deduplicated changed set, so
all changed revisions are validated together and one P12 mutation-epoch step
represents this bounded nested operation. If SpatialKnowledge's shared
revision changes, report both of its registered sibling sections. A sequence
allocation during arrival recording joins this same set rather than issuing a
second epoch notification.

Owner callbacks must be emitted only after their owner commit is complete. In
particular, City membership changes made during an NpcRuntime presence
transition may be temporarily non-reciprocal inside the method; defer the
notification until both the City projection and NpcRuntime fields are
consistent. Outside `AdvanceParties`, instrument only the owner mutators needed
to keep these registered witness baselines truthful. Batch the City and
NpcRuntime sections changed by one presence transition into one notification.

If a later member/party throws after earlier commits, preserve the existing
partial progress: report the accumulated committed owner sections when the
operation context closes, then let the existing daily-advance exception path
fault admission closed. Do not introduce rollback, retry, different arrival
ordering, or a new event-failure policy. Existing arrival-event failure
behavior remains unchanged. A no-op advance with an empty changed set issues
no mutation-epoch notification.

## Dependency-ordered implementation and review plan

1. Add the NPC travel-progress witness/revision and its exact dynamic-roster
   reconciliation. Add narrowly bound owner callbacks for successful travel
   progress, City-presence and SpatialKnowledge commits. Preserve the existing
   TravelParty store implementation; add only its P12 callback binding.
2. Register the exact TravelParty and City providers, including City-section
   participation in existing NPC membership reconciliation.
3. Add the nested daily-only TravelParty operation, changed-section batching,
   sequence-notification coalescing, and the exact runtime composition checks.
4. Validate the integrated candidate against current P12 canonical. Keep
   `SimulationRuntime` and bootstrap composition as one serialized hotspot;
   no competing writer may integrate there until this slice is reviewed.

The design remains one bounded P12-B capability slice: the owner witness and
the nested operation are a dependency-ordered implementation sequence, not new
Phase checkpoint IDs. If implementation discovers an owner write that cannot
be accounted for within these existing authorities, stop that subpath and
record the precise evidence gap instead of broadening the operation.

## Required tests

- Exact provider identity, schema, section ID, cardinality and revision for
  every installed NPC, including empty travel state; roster add/remove,
  duplicate RuntimeId, and same-ID owner replacement/reconciliation cases.
- Each travel mutator increments once only on a real change; rejected and
  no-op paths remain unchanged. `AdvanceTravelDay` must revise intermediate
  day decrements even though its existing return value is `false` until
  arrival.
- Revision-saturation rejection occurs before local writes. Group start,
  compensation, synchronized multi-member clearing/advance and final-arrival
  capacity preflight leave no partial unrevisioned travel state.
- Exact TravelParty/City/SpatialKnowledge/sequence registration and live
  provider identity; City and NPC presence transitions validate only after
  reciprocal state is complete.
- Nested operation appears beneath `runtime.advance-day`; a start-day clear,
  intermediate travel progress, and multi-member arrival report exactly the
  changed sections. Multiple changed owners, including the record sequence,
  advance the shared mutation epoch once for the nested operation; no-op does
  not advance it.
- Unsupported/unbound or P18 runtimes do not acquire the new P12 operation.
  Admission/preflight failure prevents the TravelParty call. A later exception
  preserves current partial progress and faults the daily P12 runtime closed.

Run the focused TravelParty census/group-travel, NPC owner, SpatialKnowledge,
bootstrap-composition, runtime-admission/orchestration, and long-run suites;
then ALL EditMode, official Unity Smoke, and `git diff --check`. Review exact
XML/log hashes on the candidate code tree. Documentation-only review does not
replace implementation validation or exact-tip code review.

## Scope limits

This slice does not close P12-B or prove complete owner, operation, or
shared-epoch coverage. It does not prove global quiescence, capture eligibility,
export, hydration, P12-A readiness, P13 readiness, or full-profile
reconstruction. It does not change TravelParty formation/start, Expedition
return, ordinary single-NPC travel policy, P18 temporal ordering, the activity
definition/instance/participant model, or P20 cardinality. The entity census
counts exact TravelParty instances and NPC owners separately; it makes no
universal participant-count or activity ownership claim.
