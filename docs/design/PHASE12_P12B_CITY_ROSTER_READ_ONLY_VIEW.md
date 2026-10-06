# P12-B City roster read-only view design

**Status:** Proposed for independent technical review. This is a bounded
implementation task inside the already accepted P12-B profile-admission and
completed-boundary capability; it adds no checkpoint ID and makes no Phase
readiness claim.

**Canonical base:** `69a5a41ca879fb66ce62efbe0d62e31f7298675b` on
`codex/phase12/canonical`, after the P12 identity/spatial protocol promotion.

## Source finding

`SimulationRuntime` owns a private `List<CityRuntime> cities`, sorts and assigns
it during construction, and has no post-construction internal add/remove
callsite. The public property is typed `IReadOnlyList<CityRuntime>` but returns
that `List<CityRuntime>` directly. A consumer can cast the result back to
`List<CityRuntime>` and add, remove, or reorder City owners without changing
the exact `RuntimeIdentityRegistry` City witness or rebuilding the per-City
protocol providers. The NPC roster already exposes a stable read-only view
over its privately owned list.

## Bounded contract

Keep `Cities` source-compatible as `IReadOnlyList<CityRuntime>` and preserve
the constructor's sorted order and exact owner references. Store one
`AsReadOnly()` view over the private City list and return that stable view.
The private list remains constructor-owned and is not mutated after
composition. Mutation attempts through an `IList<CityRuntime>` cast must
report `IsReadOnly` and throw `NotSupportedException`; reading and enumeration
remain unchanged.

This closes the accidental mutable alias while preserving the API's existing
read-only contract. It requires no new census section or revision because
City membership is fixed after construction in the selected Daily-v1
composition and already has an exact RuntimeIdentityRegistry owner witness.
Any future supported City addition/removal requires its own accepted
operation, stable revision, and shared-epoch invalidation contract.

## Changes and validation

Expected implementation files are `Assets/_Project/Scripts/SimulationRuntime.cs`
and a focused EditMode test in
`Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`.
The regression will use the exact selected P9-B Daily-v1 profile, assert its
two City identities and ordering, attempt list mutation through the
`IList<CityRuntime>` view, and verify the City count/references and registered
identity census remain unchanged. Existing code reading `Cities` is
read-only; no migration or serialized data change is required.

Validation after implementation: focused bootstrap-composition and
Daily-v1 admission/inventory tests; ALL EditMode; official Smoke; and
`git diff --check`. Reuse the current E: worktree and Unity `Library` where
possible. This touches the `SimulationRuntime` hotspot, so integrate serially
with any concurrent runtime/bootstrap change.

## Scope limits and gates

No new City creation/removal gameplay, city composite export/revision,
registry mutation path, P12-A export/hydration, capture token, P12-B
completion, or Phase closure is included. The accepted Daily-v1 profile
remains P9-B-only. P10-A Ruin/LocalTopology remains in
`Simulation-GeneralTest.asset`; P10-B remains separately rejected by
Daily-v1. The current profile's exact 253-section census and previous
P12-B limitations remain unchanged.

Implementation readiness requires an independent technical review of this
boundary. A reviewed design authorizes only this read-only view correction;
the normal exact-tip code review, validation, and canonical promotion gates
remain applicable.
