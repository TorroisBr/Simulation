# P12-E ArmedForceStore passive census design

**Status:** Bounded technical design for the accepted P12-E core-domain census capability. Independent design review pending.

**Design base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

**Authority:** The accepted P12-E scope includes armed-force, manpower, and
position authorities. This design covers only the existing `ArmedForceStore`
owner. It adds no checkpoint, product behavior, profile scope, or
implementation authorization.

## Bounded witness contract

`ArmedForceStore` already exposes stable owner-local `Count`,
`ContingentCount`, `RelevantPersonCount`, and monotone `Revision` values. Add
three fixed schema-v1 owner sections using the existing
`IOwnerSectionCensusProvider` and `OwnerSectionCensusWitness` contracts:

| Store value | Section ID | Census cardinality |
|---|---|---|
| `Count` | `p12e.armed-force.forces` | Registered force records |
| `ContingentCount` | `p12e.armed-force.contingents` | Registered contingent records |
| `RelevantPersonCount` | `p12e.armed-force.relevant-person-references` | Registered relevant-Person references |

Each provider reads from the same exact `ArmedForceStore` instance, reports
the corresponding count and that store's current `Revision`, and uses the
store instance as the shared opaque owner identity. Create them from
`SimulationBootstrapComposition.Runtime.ArmedForceStore`, the
runtime-installed clone. Do not read the source/template store or create a
second authority.

The selected `UnityBootstrap-Daily-v1` profile composes this store with zero
forces, contingents, and relevant-Person references. The expected live
day-zero evidence is therefore three zero counts at revision 0, identified
by the installed store instance. This means **composed-empty**. It does not
classify either separate `ContingentManpowerStateStore` or
`ArmedForceSpatialStateStore` as empty or excluded.

## Revision and proof boundaries

The existing store revision advances on successful writes to forces,
contingents, and relevant-Person references, including same-cardinality
changes. The audited paths include registration, reparent/detach/reattach,
location and commander changes, contingent registration/replacement/amount
updates/termination, relevant-Person addition, and committed battle amount
mirror batches. Failed preflight and empty/no-op batches do not advance it;
the supported atomic mirror rollback restores the prior store revision.

Tests should prove provider identity/cardinality/revision on the installed
owner, one successful write for each reported collection, a successful
same-cardinality mutation advancing revision, and representative rejected
and rollback/no-op paths preserving their current evidence. This is a local
owner witness; it does not replace the static writer map or prove notification
to the shared P12 mutation epoch.

## Limits retained

This adds only passive live census evidence for `ArmedForceStore`. It does
not witness `ContingentManpowerStateStore`, `ArmedForceSpatialStateStore`, or
other P12-E authorities; add global invalidation; establish owner-thread or
quiescence; make collection atomic; grant capture eligibility; or add export,
staged hydration, or restore behavior. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.
