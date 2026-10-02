# WI-A implementation partition: serialized runtime handoff

**Base:** P12 canonical `2f7c7422812de40aa1223e8310dcbd9f5d8ca474`.
**Status:** the isolated implementation adds canonical `WorldId`, allocates it
before genesis validation, stores it on the private bootstrap composition, and
opens the `TesteSimulacao` publication gate only after synchronous stages and
the selected P12 bootstrap scope finish successfully. It does not yet deliver
the complete WI-A runtime/P18 handoff.

## Reserved serialized integration seam

`TesteSimulacao.InitializeSimulation` currently constructs `SimulationRuntime`
inside `p9.genesis.validate-profile/v1`, before it creates the private draft at
`p9.genesis.publish/v1`. The candidate's `unpublishedWorldId` already exists at
that point. The serialized `SimulationRuntime.cs` integration must:

1. Accept an optional typed `WorldId` construction argument so existing direct
   runtime test fixtures can remain identity-less, while the published authored
   composition always supplies the allocated identity.
2. Retain that exact immutable instance and expose it read-only from the
   runtime. `SimulationBootstrapComposition.WorldId` and
   `SimulationRuntime.WorldId` must be `ReferenceEquals` for a composed world.
3. Reject a P18 profile whose world identity differs from the composed runtime
   identity. This check belongs at the composition boundary; no runtime may
   silently adopt a second identity.

`SimulationRuntime.P18D.cs` owns the compatibility seam. Add a typed
`P18DIntradayProfile(WorldId, ...)` constructor/factory that derives the
existing canonical string passed to the P18 timeline, preserving the current
string constructor for standalone fixtures. Do not alter occurrence encoding,
scheduling, or standalone P18 fixture behavior. Tests must cover same-instance
handoff, conflicting typed identity rejection, and unchanged encoded IDs for
equivalent canonical text.

The `SimulationRuntime.cs` constructor call in `TesteSimulacao` is the single
serial overlap with active P12-B and FR-B work. Do not integrate that seam in
parallel with those owners. The current P12 close check uses the already
promoted `TryAssessNpcRosterCensus` after disposing the bootstrap scope; it
proves the selected registered-owner inventory is healthy and quiescent at
that point, without claiming global owner coverage or capture eligibility.

## Candidate limitations

Until the reserved seam is integrated and revalidated, this candidate claims
only a canonical identity on the bootstrap composition and fail-closed public
genesis publication. It does not claim that `SimulationRuntime` owns or exposes
the same ID, typed P18 identity compatibility, save/continuation identity,
copy/fork behavior, P12-A readiness, P13 readiness, or WI-A completion. No
canonical promotion is implied by this note or by the implementation branch.
