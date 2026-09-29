# P12-E Manpower and Armed-Force Position Census Design

**Status:** Bounded passive-witness proposal; existing accepted P12-B/P12-E
capability authorization applies. Independent design review required before
implementation.

**Integration anchor:** `codex/phase12/P12BCensusOwnerIntegration` at
`cad27805f7f014ac753ebe6ecf34f3e322a90feb`, which descends from the reviewed
ActorChoice/record-sequence integration candidate at `b355919`.

## Finding resolved

The selected `UnityBootstrap-Daily-v1` path does not supply prebuilt
`ContingentManpowerStateStore` or `ArmedForceSpatialStateStore` instances.
`SimulationRuntime` constructs runtime-owned instances on its normal default
composition path, bound to its installed `ArmedForceStore` and spatial
authority. `TesteSimulacao` supplies no `LocalTopologyStore`, so the optional
local-topology reference on the position store is null; that does not mean the
position store itself is absent.

The authored day-zero profile has no armed forces or contingents. Its
runtime-installed manpower store therefore has `States.Count == 0` and
`Revision == 0`. Its runtime-installed position store has `Count == 0` and
`Revision == 0`. These are live initial observations, not exclusions of later
supported populated state.

## Fixed sections

Add two schema-v1 passive owner sections through the existing
`SimulationBootstrapComposition` handoff:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.contingent-manpower.states` | `Runtime.ContingentManpowerStateStore` | `States.Count` | `Revision` |
| `p12e.armed-force-spatial.positions` | `Runtime.ArmedForceSpatialStateStore` | `Count` (equal to `Positions.Count`) | `Revision` |

Each witness reports the exact installed store object as opaque owner identity.
The providers are fixed and read-only. They must not construct a second store,
expose mutation methods, alter owner APIs, or count the same records under the
existing `ArmedForceStore` sections.

These are distinct authorities. Manpower owns source bindings, cohort rosters,
and contingent revisions; `ArmedForceStore` owns contingent identities and
their `Amount` mirrors. Position state owns the optional typed current
position keyed by force identity. `SpatialAuthorityStore` and the optional
`LocalTopologyStore` resolve those references but do not own the position
rows.

## Revision and relation semantics

The witness copies the store's existing current revision. It is a local state
stamp, not the P12-B global mutation epoch. In particular,
`ContingentManpowerStateStore.RestoreBattleBatch` intentionally restores its
prior root revision along with the prior state; the provider must report that
restored value rather than infer a monotonic event counter. Successful public
manpower changes advance its revision; rejected operations and no-ops do not.
Position set/clear advances its revision only when the mapping changes; a
same-position set, absent clear, rejection, or faulted operation leaves it
unchanged.

The census does not claim atomicity across the two owners. Later P12-B
invalidation and capture-eligibility work must account for all supported
commits, including direct owner APIs and Battle's coordinated manpower
commit/rollback path.

## Required evidence

- On the selected live profile, assert both provider identities are the exact
  runtime-installed owners and both day-zero witnesses are exact zero at
  revision zero; repeat reads to prove stable owner identity.
- For manpower, exercise a supported roster/source or cohort change and a
  same-cardinality state change; assert the witness reads current cardinality
  and owner revision. Also assert a rejected operation/no-op preserves them.
- For positions, exercise set, same-position no-op, changed-position set, and
  clear; assert the witness reports exact current cardinality and revision.
- Keep existing domain validation responsible for validating roster-to-
  contingent `Amount` mirrors, source/cohort invariants, and position
  references. Census providers must not add duplicate validation semantics.

## Deferred boundaries

This slice does not add serialization, export, staged hydration, restore,
shared epoch wiring, owner-thread/quiescence enforcement, capture eligibility,
or a whole-profile census registration. P12-E eventual export/hydration must
stage manpower together with its contingent roots and `Amount` mirrors; force
positions must hydrate after force identities and validate references against
the composed P8 spatial authorities. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.
