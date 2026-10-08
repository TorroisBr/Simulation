# P12-E ArmedForce, Manpower, and Position Snapshot Design

**Status:** bounded technical-design proposal for independent review. It is not
implementation authorization, a new checkpoint identity, P12-E completion,
or P12-A readiness.

**Revision after review:** addresses finding E1 in review record
`docs/design/PHASE12_P12E_ARMED_FORCE_MANPOWER_POSITION_SNAPSHOT_DESIGN_REVIEW_CE0A9B9.md`
at `f1cb3120bc490abb9331dd19648e5fb259504975` by using the exact installed
owner's immutable `SourceProvider` property as a capture/staging precondition.
This revision still requires fresh independent exact-content review.

**P12 canonical base:** `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`.
**Architecture authority:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Applicable general P12-E design at this base:**
`docs/design/PHASE12_E_TECHNICAL_DESIGN.md`, blob
`15aaee09d3295cff81a48e166b620c89f5156346` (latest correction commit
`030dc1b13956bdb946f1a63869397d3e464850e4`). Its prior independent R1
review applies to an earlier blob and is `NEEDS_CHANGES`; this proposal does
not treat that stale review as a pass. The reviewer must compare this proposal
against the exact current design and its review status.
**Accepted owner scope:** P12 Brief and current P12-E ownership contract.
**Existing census evidence:**
`docs/design/PHASE12_P12E_MANPOWER_SPATIAL_CENSUS_DESIGN.md` and the current
owner coverage inventory. Those passive witnesses are consumed as-is; this
proposal adds no census provider or registration.

## 1. Bounded slice

Add detached value export and private staged reconstruction for the already
included Daily-v1 `ArmedForceStore`, `ContingentManpowerStateStore`, and
`ArmedForceSpatialStateStore`. They form one dependency-ordered owner group:
the force store owns force/contingent/reference facts; manpower owns each
contingent's source binding and roster; spatial state owns optional typed
force positions. The source owners are already included under the accepted
P12-E scope and have P12-B passive census sections. This design assigns no new
checkpoint ID and proposes no change to their scope.

The slice preserves current supported facts and owner-local revisions. It
consumes, but does not create or extend, the P12-B completed-boundary token and
revision vector. It does not modify P12-B mutation wiring, admission,
quiescence, owner-thread rules, capture eligibility, bootstrap composition, or
`SimulationRuntime`.

The Daily-v1 starting state is exact-empty: the three ArmedForce census
sections, manpower state section, and position section each have cardinality
zero and their corresponding local owner revisions are zero. These are
composed owners with explicit required sections, not absent owners. Future
supported runtime writes must round-trip; startup emptiness is not a permanent
empty-only contract.

## 2. Section identity, capture binding, and DTO values

Use existing schema-v1 section IDs and bind all captured values to the same
ephemeral P12-B token/vector:

| Section | Exact owner | Cardinality | Local revision |
|---|---|---:|---:|
| `p12e.armed-force.forces` | installed `ArmedForceStore` | `Forces.Count` | `ArmedForceStore.Revision` |
| `p12e.armed-force.contingents` | the same installed `ArmedForceStore` | `Contingents.Count` | the same revision |
| `p12e.armed-force.relevant-person-references` | the same installed `ArmedForceStore` | `RelevantPersons.Count` | the same revision |
| `p12e.contingent-manpower.states` | installed `ContingentManpowerStateStore` | `States.Count` | `ContingentManpowerStateStore.Revision` |
| `p12e.armed-force-spatial.positions` | installed `ArmedForceSpatialStateStore` | `Count`, equal to `Positions.Count` | `ArmedForceSpatialStateStore.Revision` |

For each section, the owner object identity checked by the census is opaque and
transient; never place a CLR reference, process-specific identity, completed
token, owner-section vector, or invented epoch in the persisted DTO. Require exact
section ID, schema, owner identity, cardinality, and local revision agreement
with one capture stamp. Read each owner through its defensive sorted view once,
copy the entire value set, and fail the private capture if the existing token
or any recorded owner stamp does not match that same boundary. This design
does not claim that local revisions prove atomicity or capture eligibility.

These five existing sections are registered into the current P12-B protocol;
they are not merely exposed by `SimulationBootstrapComposition`. In
`SimulationRuntime.InitializeNpcRosterCensusProtocol`, a non-null
`runtimeAdmissionContext` requires `TryRegisterP12EMilitaryOwnerSections`.
That helper is implemented in `P12RuntimeIdentitySpatialCensus.cs` and
registers the three ArmedForce sections plus manpower, positions, Conflict,
War, and Battle as `Required`. Each is passed through
`TryRegisterP12FixedOwnerSection`, which reads the provider and checks exact
section ID, schema, owner object identity, nonnegative cardinality/revision,
then registers the expected section and provider before both inventories are
sealed. At completed-boundary capture,
`SimulationRuntime.TryReadDailyCaptureEvidence` delegates to
`ContinuationCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot`; the
resulting owner-section snapshots and mutation epoch are stored in
`DailyCaptureEligibilityToken` and compared during later token validation.
Thus the five sections above are present in the existing token's section
vector. This consumption does not claim complete invalidation: current P12-B
direct write/epoch coverage remains incomplete, and this P12-E design adds no
mutation notification or epoch behavior.

Export a detached immutable schema-v1 graph containing:

| Owner values | Exact fields to preserve |
|---|---|
| Force | `ArmedForceId.Value`, `DisplayName`, `CreatedAbsoluteDay`, nullable `ParentForceId.Value`, nullable legacy `OperationalLocationReference`, nullable `CommanderPersonId.Value`, exact `ArmedForceLifecycleState`, nullable `TerminatedAbsoluteDay`, and `IsDetached`. Preserve the legacy location string as opaque compatibility data; it is not a typed position. |
| Contingent | `ContingentId.Value`, `ForceId.Value`, `Amount`, origin `Domain` and `Value`, open-ended `ServiceType`, and the complete characteristic key/value set in the owner's ordinal key order. |
| Relevant Person reference | `ArmedForcePersonReferenceId.Value`, `ForceId.Value`, `PersonId.Value`, and exact `RoleKey`. Preserve the existing stable ID; do not rebuild it when it was supplied explicitly. |
| Manpower state | `ContingentId.Value`, nullable `ManpowerSourceId.Value`, all cohorts (`InjuryState`, `CustodyState`, nullable `CustodianForceId.Value`, `AvailabilityState`, and positive `Amount`), each state's exact local `Revision`, and the owner's exact local `Revision`. |
| Position | `ArmedForceId.Value` and the typed `SpatialReference` kind plus the exact value for that kind. Preserve the existing stable-key/reference semantics; do not flatten typed identities to a location string. |

The DTO stores complete owner rows, not census summaries or computed views.
`LivingRosterAmount`, `AvailableAmount`, and the manpower fingerprint are
deterministically derived from the exact source binding/cohorts by the current
state constructor. Validate the derived values against the source contract;
do not serialize a second, potentially divergent copy of them. `Count` and
section cardinalities likewise remain stamps, not substitutes for the rows.
All child arrays are copied and read-only. DTOs contain no owner records,
identity wrapper objects, stores, mutation guards, source provider, runtime,
Unity objects, or `SpatialReference` object. Typed IDs and references are
reconstructed from their exact string values at the private owner boundary.

## 3. Existing owner semantics and committed writes

### ArmedForceStore

`ArmedForceStore` owns force hierarchy/lifecycle, contingent identity and
metadata, and relevant-Person relations. Preserve the existing complete
invariants: unique stable IDs; registered and legal parent/force links; no
hierarchy cycles; parent lifecycle and detached-state rules; day ranges;
valid `PersonStore` references for commanders and relevant Persons; and the
existing force-to-contingent relations. Validate with the existing owner
invariant code, not newly invented military rules.

Successful force writes include `TryRegister`, `TryReparent`, `TryDetach` and
`TryReattach`, legacy `TrySetOperationalLocation`, `TryAssignCommander`,
`TryAddRelevantPerson`, and `TryTerminate`. Each committed change advances the
single local ArmedForce revision once; a rejected operation does not. The
legacy detach/location overload and `OperationalLocationReference` remain
opaque compatibility fields. Current physical position is exclusively the
separate spatial owner.

When manpower is attached, managed contingent registration/replacement is
owned by the manpower authority. `TryRegisterContingentFromManpower` adds the
identity and advances the ArmedForce revision once; successful manpower
source/cohort changes update the `ContingentRecord.Amount` mirror through
`TryUpdateContingentAmountFromManpower`, advancing the ArmedForce revision.
Direct managed contingent registration/replacement is already rejected.
Battle's prepared amount-mirror batch can update force records and restore the
exact old records and revision on rollback. Preserve this boundary and do not
replay Battle or any operation during hydration.

### ContingentManpowerStateStore

Manpower owns the canonical source binding and ordered normalized cohorts for
each contingent. Require exactly one state for every managed contingent and
no extra state. Require `LivingRosterAmount == ContingentRecord.Amount` for
each ID, and preserve the state's own revision and the owner revision exactly.
Validate checked cohort totals, positive cohort amounts, defined enums,
captured/unavailable rules, required active custodian references, free/no-
custodian rules, and all existing source-coverage/capacity checks for a source
binding that is actually admissible in the selected profile. Keep force
lifecycle/containment and Battle transaction semantics under their existing
authorities.

Existing successful writes are `TryRegisterContingent` (jointly registering
an initially empty roster through the ArmedForce owner), `TrySetSourceBinding`,
`TryAllocate`, `TryDemobilize`, and `TryRedistribute`. Effective commits
advance the existing owner revision and affected contingent revision exactly
as implemented; rejected/no-op operations retain them. Source/capacity checks,
expected contingent revision, overflow, stale source, and mirror checks must
remain in force. The prepared Battle batch can update both manpower state and
the ArmedForce `Amount` mirror. Failure/exception rollback restores prior
rows and prior revisions; those local revisions can therefore move backward
on rollback and are not event counters or epochs.

### ArmedForceSpatialStateStore

The baseline section owns a zero-or-one typed current position per registered
force. `TrySetPosition` accepts only an active registered force and a typed
reference that the composed spatial authority resolves; setting the same
position is a successful no-op. `TryClearPosition` removes an existing
position and is a successful no-op when absent. Only an effective mapping
change increments the local position-store revision; rejections and no-ops do
not. Preserve any legacy opaque `OperationalLocationReference` independently.

Daily-v1's `LocalTopologyStore` is `NOT_COMPOSED`, as recorded by the current
owner inventory and P12-E contract; it is not a composed-empty store. The
staged position owner is bound to the exact staged P8 `SpatialAuthorityStore`
and a null topology owner. Preserve supported Hex/Location/Crossing typed
references only when they resolve under this selected P8 composition; reject
SubLocation, unresolved, wrong-kind, or injected topology references. Do not
hydrate or synthesize P10-A LocalTopology.

P16-A adds carried supply and a crossing receipt to this same owner. The
selected Daily-v1 composition must continue to reject any `P16Profile`,
P16-specific supply or crossing receipt state, including the P16-A initial
state before movement. Current evidence is `SimulationRuntime`'s constructor
guards (`hasP16AProfileState`, selected composition checks and
`P16-A state requires the explicit P16AOneHopMilitary composition`) and
`P16AMilitaryMovementTests.DailyProfileRejectsUnmovedAndMovedP16StateBeforeAdmission`.
Do not serialize this later extension into the baseline position DTO, infer
support from its shared store, or weaken the negative admission tests. Keep
P17-A rejection as well: `SimulationRuntime` rejects P17-A War state outside
its explicit composition; `P17ARuntimeTests.P17StateIsRejectedByStandardAndSelectedDailyCompositionsWithOrWithoutP16Input`
records that boundary. Do not capture P17 provenance, trusted authority,
strategic state, or War extensions here. P16-A and P17-A remain outside
Daily-v1; this does not exclude their later profile support.

## 4. Manpower source absence and profile boundary

The inventory distinguishes the selected profile's no-registration manpower
source configuration from absent `LocalTopologyStore`; inspect the actual
resolved provider rather than inferring it from the prose label "empty
registry". In current code, `SettlementManpowerSourceRegistry.TryCreate`
returns a null registry when registrations are null or empty. A non-empty
registration set returns a registry. `SimulationRuntime` resolves the provider
as that registry or the supplied `manpowerSourceProvider` / input store's
`SourceProvider`, then constructs or clones the installed
`ContingentManpowerStateStore` with that exact resolved provider. The installed
store retains it in a `readonly` field and exposes it through the getter-only
`SourceProvider` property. Therefore, on the ordinary selected Daily-v1 path,
an exact check that the P12-B census-bound installed manpower owner has
`SourceProvider == null` proves that no registration-created registry,
explicit injected provider, or input-store provider survived runtime
composition. This property cannot change after construction. A non-null
provider is rejected for this Daily-v1 slice.

Source evidence: `SettlementManpowerSourceRegistry.TryCreate` in
`Assets/_Project/Scripts/Military/ManpowerSourceConsequencePlanning.cs`;
provider resolution and installed-owner creation in
`Assets/_Project/Scripts/SimulationRuntime.cs`;
readonly field/getter in `Assets/_Project/Scripts/MilitaryManpowerFoundation.cs`;
and exact owner identity/cardinality/revision provider in
`Assets/_Project/Scripts/P12EMilitaryOwnerCensusProviders.cs`.

At capture, read `SourceProvider` directly from the exact store object already
bound by `p12e.contingent-manpower.states` to the same P12-B capture token.
Require null before accepting the snapshot; at staging require the target
Daily-v1 store's exact property is null before installing state. The census
section remains unchanged: this is a direct immutable owner-property
precondition, not a new P12-B census field, shared epoch, runtime mutation
wire, or serialized provider value. Its proof is the source-level constructor
chain above plus the exact installed owner identity supplied by the existing
census/token; do not infer absence from the asset, registrations alone,
missing manpower owner, or day-zero empty cohort list. Do not serialize a
provider object, provider implementation, or source capacity.

Any non-null `SourceId` must resolve against an exact admitted stable source
authority and satisfy the current owner checks at staging. A source-bound
state cannot be accepted merely because its source identifier is well formed.
Under this selected Daily-v1 composition the installed source provider must be
null, so any source-bound row rejects. A future profile that admits a provider
requires its own accepted source-owner contract and is outside this slice.

## 5. Private construction and validation order

Capture and reconstruction preserve identity/ownership boundaries as follows:

1. P12-B has already admitted a successful completed boundary and exact
   effective provider inventory; P12-C supplies the existing `WorldId`, typed
   identity registry, shared allocator/sequence/random roots. P12-D supplies
   the staged `PersonStore` and the canonical P8 spatial authority. These are
   inputs, not values duplicated by this slice.
2. Validate the complete detached document before allocating candidates:
   required section presence; schema 1; nonnegative exact revisions and
   cardinalities; unique/non-empty IDs; valid enums and scalar ranges;
   deterministic ordering; all row counts; null `SourceProvider` on the exact
   census-bound owner; P10
   `NOT_COMPOSED`; and the P16/P17 rejection conditions above.
3. Construct a private ArmedForce candidate from all force records first,
   without replaying `TryRegister`; after all records exist, validate hierarchy
   and lifecycle globally. Install contingents and relevant-Person references
   only after force rows and staged Persons are available. Preserve exact
   ArmedForce owner revision and validate full owner invariants.
4. Construct a private manpower candidate bound to that exact staged
   ArmedForce candidate and require its immutable `SourceProvider` property is
   null for this selected Daily-v1 slice. Install all per-contingent state
   rows and exact owner revision without invoking register/allocate/source
   operations. Validate one-to-one state coverage, cohort constraints,
   source-null semantics, and every `Amount` mirror. The factory must be private,
   side-effect-free, and unable to publish or mutate the original owner.
5. Construct the private baseline position owner bound to the same staged
   ArmedForce and staged spatial authority, with `LocalTopologyStore == null`.
   Install only baseline positions and exact owner revision; no P16 profile,
   supply, receipt, or P17 provenance. Validate each registered force and
   typed reference against staged P8 authority, plus Daily-v1 topology absence.
6. Cross-check each P12-B census section's exact owner identity/schema/count/
   revision against the same capture token/vector and compare its cardinality
   with the complete exported rows. Require the three force sections to share
   the same exact ArmedForce owner and revision. Any mismatch discards all
   private candidates. P12-G remains responsible for whole-graph validation,
   unresolved D/E/F relations, and publication.

Factories install saved values/revisions directly into private owners. They do
not call public write methods, replay receipts, run domain consequences,
allocate IDs, mutate source truth, or touch the active runtime. An internal
factory may accept only detached validated values and exact staged dependencies;
it must not accept arbitrary provider objects from the snapshot.

## 6. Required tests and validation evidence

The implementation suite should extend existing ArmedForce/manpower/spatial
tests and add focused owner snapshot/factory tests. It must prove:

- exact explicit-empty round-trip for all five sections with their five
  cardinalities and corresponding owner revisions, bound to the installed
  owners; repeat reads retain owner identity;
- populated round-trip preserving every field, null, stable reference ID,
  cohort order, characteristic order, typed position, and exact store and
  contingent revisions;
- export deep detachment and read-only collections after subsequent source
  writes;
- force hierarchy, lifecycle, legacy opaque location, Person/commander and
  relevant-reference identity/cardinality validation against staged owners;
- one-to-one contingent/manpower state coverage and exact
  `Amount == LivingRosterAmount` mirrors; source binding resolution and
  fail-closed behavior when the exact admitted source authority is absent;
- for the selected Daily-v1 runtime, the exact census-bound manpower owner's
  `SourceProvider` is null at capture and on the private target owner; a
  non-null injected/provider-registry path rejects before snapshot acceptance
  or hydration. The property is getter-only over a readonly field and no
  second P12-B section is introduced;
- all effective direct/manpower/mirror write paths preserve their existing
  exact revision changes, no-op/failure behavior, checked overflow, and
  Battle prepared-commit rollback to exact prior rows and revisions;
- positions set, same-position no-op, changed position, clear, absent-clear,
  wrong/terminated force, unresolved typed reference, and revision-overflow
  behavior match current owner semantics;
- P16-A Daily-v1 rejection for both unmoved and moved P16 state remains
  passing; added staging tests reject profile, supply, receipt, or provenance
  payloads instead of omitting them. P17-A Daily rejection remains passing;
- typed P10 LocalTopology `NOT_COMPOSED` is retained; injected/composed
  topology, SubLocation, and other unresolved spatial references reject;
- unknown schema, missing/duplicate rows, duplicate IDs, invalid enums,
  negative/overflowed amounts or revisions, count mismatch, malformed
  hierarchy/cohort/source/position relations, and any failed private factory
  reject without changing the active runtime;
- failed staging at each owner boundary leaves live values, bindings,
  revisions, guards, and all observable state unchanged.

Validation for an eventual implementation must include focused force,
manpower, spatial, Battle rollback and current P16/P17 rejection suites;
ALL EditMode; official Smoke 5/5; and `git diff --check`. Add a broader
regression suite only where the actual code delta touches the corresponding
owner. This document reports no test result.

## 7. File ownership, integration hotspots, and exclusions

The implementation candidate should limit code ownership to:

- `Assets/_Project/Scripts/ArmedForceStore.cs` and
  `ArmedForceContracts.cs` for detached force/contingent/reference values and
  a private ArmedForce factory;
- `Assets/_Project/Scripts/MilitaryManpowerFoundation.cs` for manpower DTO,
  exact owner factory, and mirror/source validation;
- `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs` for baseline position
  DTO/factory and explicit P16/P17 rejection;
- focused ArmedForce/manpower/spatial tests plus a new owner-snapshot suite;
- one design-specific validation record.

These three authority files are one serialized implementation hotspot because
the factories bind exact owners and preserve the contingent amount transaction
boundary. Battle shares the ArmedForce/manpower mutation and rollback surface;
coordinate with its owner before changing those methods. `SimulationRuntime`,
`TesteSimulacao`, bootstrap composition, capture token/vector, quiescence,
mutation epoch, owner admission, and P12-B evidence are separate active
hotspots and must not be edited by this slice. Do not modify the P12-D owner
assembler, P12-G publication/parity, `PersistentConflictWarBattleStores.cs`,
P8/P10 profile inputs, P16/P17 implementation, or architecture documents.

Exclude City/NPC/economy roots, Conflict/War/Battle facts (their own E
sections), P12-F Knowledge or commitments, events/history, runtime activity,
P16 carried supply/crossing receipts, P17 authority/provenance, P8 passage or
travel state, P10 LocalTopology, P14 material flow, save/load envelope, P12-A
profile integration, P12-G whole-graph publication, P13 history/fork,
gameplay, and any new participant/cardinality or military semantics.

The design is implementation-ready only after an independent exact-content
technical-design review confirms current sources and profile witnesses,
including current P12-B registration of the five sections, the exact
token-bound owner vector, and the direct immutable `SourceProvider == null`
precondition. The E1 correction is not yet reviewed; additionally, the current
general P12-E design blob recorded above has an earlier R1 `NEEDS_CHANGES`
review against a stale blob, so its current applicability must be confirmed
in fresh review. A design review does not itself promote code, close P12-E,
establish complete owner coverage, satisfy P12-A, complete P12-B, or unblock
P13. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. P12-B remains
incomplete.
