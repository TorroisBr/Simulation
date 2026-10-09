# P12-G restored-boundary admission — current-base revalidation

**Result:** `REVALIDATE` — PASS for this bounded capability only

**P12 canonical base:** `678b01dc9c9dddf05cbd0a64033afc7b1ed1615b`

**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Revalidated design:** `PHASE12_G_RESTORED_BOUNDARY_AND_PUBLICATION_SEAM_REVIEW_65A16E0.md`

**Revalidated design review:** `65a16e0cae85aa0b8fbc29bd596b05e0f06c07df`, tree `518abf72b26476ad01cf15f84fd2dce4f1e4a98b`

## Current-base drift

Comparing canonical `SimulationRuntime.cs` at the design's source baseline
`02009f9063dd252bd4b177fd6aef1e74dcd947f5` with current P12 canonical finds
one production-source change: successful Person-bound NPC materialization now
calls `censusScope.MarkCityPresenceChanged(startingCity)` when a starting City
exists. This is the already-promoted City-bound materialization correction
`11653ebd3947220a39d785c0c0641837e5cfadbe`.

That write stays inside the existing `runtime.npc-membership` boundary. Its
provider witness uses the exact `CityRuntime`, `ImportantNpcs.Count`, and
`ImportantNpcRevision`; the focused selected-profile test verifies the
cardinality and revision each advance by one and that the shared mutation epoch
also advances by one. It adds no restore-specific owner, operation, or
publication behavior.

## Contract result

The existing bounded API remains compatible with the refreshed source:

```csharp
TryAdmitRestoredDailyBoundary(
    WorldId expectedWorldId,
    long preservedAbsoluteDay,
    long preservedCompletedCoreSequence,
    out DailyCaptureEligibilityFailure failure)
```

The candidate runtime checks the exact `WorldId` instance and its
constructor-captured initial absolute day, requires a positive preserved
completed-core sequence, and rejects an existing boundary or published factual
read. It requires the bound owner thread, healthy Daily-v1 runtime authorities,
no runtime operation in progress, and a sealed census protocol. Its fresh
quiescent snapshot reads the current City-presence revision/cardinality and
shared mutation epoch after the membership operation has completed. If that
operation is still active, the census protocol rejects admission. The new
token binds the candidate runtime identity, target owner vector, preserved day
and sequence, and `RestoredContinuation` provenance.

The seam neither transfers the source token nor calls `CurrentDay`, advances
time, or increments the successful gameplay-advance sequence. The next normal
successful advance replaces the restored token and increments from the
preserved sequence with `CompletedAdvance` provenance.

The current P12-G design and this revalidation do not add an envelope parser,
whole-graph assembler, publication owner/swap, export, hydration, or gameplay
semantics. A passing admission token is only the candidate-bound completed
boundary prerequisite. It does not make P12-G ready, complete P12-B's bounded
scope anew, unlock P12-A or P13, or close Phase 12.

## Validation scope

The current candidate validation is recorded in
[`../validation/P12GCrimeSocialRuntimeOperation/P12GRestoredBoundaryAdmissionValidation.md`](../validation/P12GCrimeSocialRuntimeOperation/P12GRestoredBoundaryAdmissionValidation.md).
It includes the 75-test runtime-admission suite and the 26-test selected
bootstrap-composition suite, followed by ALL EditMode and official Smoke.

P12-B through P12-F remain promoted within their recorded scopes. P12-G and
P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains
`OPEN`. This revalidation adds no P12-G readiness, expands no P12-B scope, and
implies no P12-A or P13 readiness.
