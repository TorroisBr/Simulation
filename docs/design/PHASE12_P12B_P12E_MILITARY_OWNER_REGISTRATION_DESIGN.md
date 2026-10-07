# P12-B — P12-E military owner census registration

**Checkpoint:** P12-B partial profile-census registration  
**Design baseline:** `88d476729715aa82578cb2a204e32a69263e6402`  
**Status:** Proposed for independent technical review; implementation has not started.

## Contract and basis

The accepted P12-B capability authorizes exact selected-profile owner admission
and a completed-boundary lifecycle, subject to a bounded technical design and
independent review. The selected profile is the P9-B-only
`UnityBootstrap-Daily-v1`; P10-A Ruin/LocalTopology remains a separate profile.
P12-E's accepted owner inventory includes the composed ArmedForce, manpower,
armed-force-position, Conflict, War, and Battle authorities. The current source
audit found existing schema-v1 owner-issued providers and day-zero identity,
cardinality, and local-revision witnesses for these authorities, but no entries
for them in the sealed `ContinuationCensusProtocol` inventory.

## Bounded change

Register exactly these eight existing sections as `OwnerSectionRole.Required`
when `runtimeAdmissionContext` is present, before the protocol seals its
expected-section and provider inventories:

| Section | Provider | Exact runtime owner | Day-zero witness |
|---|---|---|---|
| `p12e.armed-force.forces` | `ArmedForceStoreCensusProvider` | `armedForceStore` | count 0, shared ArmedForce revision 0 |
| `p12e.armed-force.contingents` | `ArmedForceStoreCensusProvider` | `armedForceStore` | count 0, shared ArmedForce revision 0 |
| `p12e.armed-force.relevant-person-references` | `ArmedForceStoreCensusProvider` | `armedForceStore` | count 0, shared ArmedForce revision 0 |
| `p12e.contingent-manpower.states` | `ContingentManpowerCensusProvider` | `contingentManpowerStateStore` | count 0, local revision 0 |
| `p12e.armed-force-spatial.positions` | `ArmedForceSpatialCensusProvider` | `armedForceSpatialStateStore` | count 0, local revision 0 |
| `p12e.conflicts` | `PersistentConflictCensusProvider` | `conflictStore` | count 0, local revision 0 |
| `p12e.wars` | `PersistentWarCensusProvider` | `warStore` | count 0, local revision 0 |
| `p12e.battles` | `PersistentBattleCensusProvider` | `battleStore` | count 0, local revision 0 |

Reuse the existing provider types and
`TryRegisterP12FixedOwnerSection` validation/registration path in
`P12RuntimeIdentitySpatialCensus.cs`. The ArmedForce sections deliberately
share one owner identity and one local revision. Construct providers only over
the exact stores installed on this `SimulationRuntime`; missing owners,
identity/schema disagreement, negative witness values, or duplicate/failed
registration must retain the existing fail-closed protocol initialization
behavior. Do not mark these P12-E owners `ExplicitlyEmpty`: zero is the current
profile's day-zero observation, while `Required` admits their supported
future cardinality without claiming mutation notification coverage.

Add one registration call in `InitializeNpcRosterCensusProtocol` before either
inventory is sealed. Keep the registration behind the existing runtime
admission-context condition so non-admission runtime modes do not acquire a new
profile inventory.

## Tests and validation

Extend `SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne` to assert:

1. The exact expected selected-profile inventory increases from 260 to 268.
2. All eight section IDs are present with schema version 1 and role `Required`.
3. The sealed protocol contains a provider for each section, bound to the exact
   runtime store identity and reporting the tested day-zero count/revision.
4. Repeated witnesses retain the same owner identity, cardinality, and local
   revision; the three ArmedForce section witnesses share their single owner
   revision.
5. The initial census assessment still succeeds.

Update the existing fixed-inventory assertion in
`PropertyEstateMutationEpochTests.DailyV1ProfileAddsPropertyEstateSectionsAlongsideIdentitySpatialOwners`
from 260 to 268; that test remains a property/estate regression and does not
need duplicate provider assertions.

Run the focused selected-profile composition suite, the affected provider
regressions, ALL EditMode, official Smoke, and `git diff --check` on the final
code tree. Record exact Unity evidence before independent implementation
review.

## Files and integration boundary

- `Assets/_Project/Scripts/SimulationRuntime.cs`: one call before inventory
  sealing.
- `Assets/_Project/Scripts/P12RuntimeIdentitySpatialCensus.cs`: the bounded
  eight-section registration method, reusing the existing fixed-owner helper.
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`:
  exact 268-section and provider-binding regression.
- `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs`:
  keep its existing selected-profile inventory expectation current.

These files are already a shared P12 census/composition hotspot; implement and
integrate this slice serially on its isolated branch. No provider implementation
or domain owner is changed.

## Explicit exclusions and limits

This design adds no census schema, gameplay, P17 composition, operation ID,
mutation callback, owner-thread/quiescence guarantee, shared mutation-epoch
notification, export, staged hydration, capture eligibility, or profile
expansion. It does not establish complete owner coverage or complete
invalidation coverage. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and P12-F Expedition remains deferred
behind P12-C/D/E. Any later supported military/Conflict/War/Battle writer needs
its own current-profile path and owner-invalidation review.

## Review record

Independent technical review: **PASS**, reviewer `p12_action_owner_audit`,
recorded after reviewing exact design tip
`56cb0599d0230b415c5fbb0bd5b4099f47028d3c` against baseline
`88d476729715aa82578cb2a204e32a69263e6402`. The reviewer confirmed all eight
section IDs/provider schemas, exact runtime-owner bindings, the shared
ArmedForce revision, `Required` roles, pre-seal registration placement,
runtime-admission gating, the 268-from-260 inventory oracle, and the stated
scope limits. P12-E includes these owners even though the selected profile's
day-zero witnesses are empty. No edits or tests were made by the reviewer.

**Implementation readiness:** established for this bounded registration slice
under the accepted P12-B prerequisite authorization. Implement serially at
the existing `SimulationRuntime` census hotspot; P12-B remains incomplete.

**Test-surface addendum:** after the PASS, source inspection found a second
existing hard-coded `260` selected-profile inventory assertion in
`PropertyEstateMutationEpochTests`. Updating that assertion to 268 is test-only
maintenance for the same registration and does not alter the reviewed scope;
independent confirmation of this design-record addendum is pending.
