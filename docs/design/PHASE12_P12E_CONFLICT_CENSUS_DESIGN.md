# P12-E Persistent Conflict Census Design

**Status:** Independent exact-tip design review PASS at `27c74e602fe9e39e809667ffa70c3080603c1292`; durable record is
`PHASE12_P12E_CONFLICT_CENSUS_DESIGN_REVIEW.md`. Existing accepted
P12-B/P12-E capability authorization applies.

**Integration anchor:** `codex/phase12/P12EManpowerSpatialCensus` at
`7a4a95af57e342e94a0d8d7ae781a1f69b6a1daf`.

## Fixed section

Add one schema-v1 passive owner section through the existing
`SimulationBootstrapComposition` handoff:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.conflicts` | `Runtime.ConflictStore` | `Count` | `Revision` |

Use the exact runtime-installed `PersistentConflictStore` object as opaque
owner identity. `SimulationRuntime` constructs the store when no source store
is supplied, clones it against its installed ArmedForce owner, and binds it to
the runtime mutation guard. The selected authored profile has no Conflicts at
day zero, so its exact initial count and revision are both zero. Initial
emptiness does not exclude later supported Conflict state.

## Existing mutation semantics

`PersistentConflictStore` owns stable Conflict records, side membership, and
participant bindings. A successful registration, participant-binding write,
or transition to ended state advances its local revision. Rejected duplicate,
invalid, missing-owner, or already-ended operations preserve count and
revision. These APIs are runtime-guarded through the existing
`IAuthoritativeMutationGuardBindable` binding.

The witness reports the store's current local revision. It does not make that
revision a global P12-B mutation epoch, prove owner-thread identity or
quiescence, or make a collection of independent owner reads atomic. Preserve
those unresolved admission/invalidation blockers.

## Required evidence

- On the selected live profile, assert the exact installed owner identity,
  section/schema, `Count == 0`, `Revision == 0`, and stable identity across
  repeated reads.
- Register a valid Conflict and add a participant binding, verifying revision
  changes while cardinality remains one; end the Conflict and verify another
  same-cardinality revision change.
- Verify a duplicate registration and a rejected post-end participant
  addition preserve the witness. Also verify a missing-Conflict binding and
  a binding to an unregistered ArmedForce preserve the witness.
- Reuse `PersistentConflictStore` invariants for side and ArmedForce links;
  `PersistentWarStore` and `PersistentBattleStore` retain their own Conflict
  reference validation. The provider adds no separate validation or write
  path.

## Deferred boundaries

This slice adds no export, staged hydration, restore, War or Battle witness,
complete profile census, global epoch wiring, operation/thread scopes,
quiescence proof, or capture eligibility. Later P12-E hydration must validate
Conflict side and ArmedForce links and respect War/Battle dependencies. P12-B
remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
