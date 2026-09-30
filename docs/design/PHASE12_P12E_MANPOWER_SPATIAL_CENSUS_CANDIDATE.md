# P12-E Manpower and Armed-Force Position Census Candidate

**Status:** Implementation, required validation, and independent exact-tip
implementation review complete; durable review record is
`PHASE12_P12E_MANPOWER_SPATIAL_CENSUS_REVIEW.md`.

**Canonical promotion:** Included in the cumulative census stack promoted at
`b889b47`; exact integration review passed at `35ec988`, with full-tree
validation on code tip `65ebc7f`.

**Candidate branch:** `codex/phase12/P12EManpowerSpatialCensus`.

**Code-bearing candidate:** `89613fa` (`Add manpower and force-position
census witnesses`). It is based on integrated candidate `cad2780`, which
contains ActorChoice/record-sequence, RuntimeIdAllocator, and ArmedForceStore
census evidence. The design review PASS is recorded in
`PHASE12_P12E_MANPOWER_SPATIAL_CENSUS_DESIGN_REVIEW.md`.

## Delivered boundary

The bootstrap composition exposes two fixed schema-v1 passive witnesses:

- `p12e.contingent-manpower.states` reports
  `Runtime.ContingentManpowerStateStore.States.Count` and that installed
  store's existing revision;
- `p12e.armed-force-spatial.positions` reports
  `Runtime.ArmedForceSpatialStateStore.Count` and that installed store's
  existing revision.

Each witness uses the exact runtime-installed store object as its opaque owner
identity. The selected profile verifies both are composed-empty at day zero,
with stable identity and revision zero. Tests cover manpower state changes
that preserve cardinality, no-op and stale rejection behavior, position set,
same-position no-op, same-cardinality replacement, invalid-reference
rejection and clear. Battle rollback tests verify the manpower witness reports
the restored owner revision after rollback.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `ContingentManpowerCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260929-235324-d22baf70f2c2492cba0ebcf8b4b79780.xml` |
| `ArmedForceSpatialPositionTests` | 12/12 | `Temp/ValidationResults/EditMode-20260929-235346-00c728d821eb49e880a28bd7cac23723.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260929-235402-8cc5aaae1cd14b908aceaacb1399c5ed.xml` |
| `BattleOutcomeApplicationTests` | 20/20 | `Temp/ValidationResults/EditMode-20260929-235418-9be4fcaaff0149e9b8ddad66c8b129c3.xml` |
| ALL EditMode | 1977/1977 | `Temp/ValidationResults/EditMode-20260929-235442-3f0534de95064670b72e6fdbbfe13ea8.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260929-235524-170591e824b04f7eb0555323c4041f71.xml` |
| `git diff --check` | PASS | Candidate tree |

## Limits retained

These remain separate owners from `ArmedForceStore`, its contingent Amount
mirror, and `SpatialAuthorityStore`. Manpower revision may roll back with a
successful domain rollback; it is not the P12-B global mutation epoch. This
candidate adds no epoch wiring, operation/thread scopes, quiescence proof,
capture eligibility, complete profile census, export, staged hydration, or
restore behavior. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.
