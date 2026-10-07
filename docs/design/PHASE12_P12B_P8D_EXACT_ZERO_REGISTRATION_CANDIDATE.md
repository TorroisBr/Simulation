# P12-B P8-D exact-zero census registration candidate

**Canonical base:** `e5405cf897c30224f86ce605a9efe6777f93749a`
**Code commit:** `7e827b3fe4b8effefd682838c6be575d25eab501`
**Code tree:** `127edf616d99bca0614041b54f72ae5addeb26b2`
**Design:** [`PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN.md`](PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN.md)
**Validation:** [`../validation/P12P8DExactZeroAdmission/VALIDATION.md`](../validation/P12P8DExactZeroAdmission/VALIDATION.md)

## Delivered boundary

For the selected `UnityBootstrap-Daily-v1` runtime admission context, the
census protocol now registers the existing schema-v1 P8-D route-observation
and route-plan-history providers before sealing the section inventory. Each
section is `ExplicitlyEmpty`, is bound to the exact runtime-cloned owner, and
must report cardinality zero and revision zero. Route-plan registration also
checks both `PlanCount` and retained `History` are empty.

The tested selected-profile partial inventory moves from 258 to 260 sections.
`Simulation-DailyV1.asset` remains the P9-B-only continuation profile;
`Simulation-GeneralTest.asset` retains the separate P10-A Ruin/LocalTopology
profile.

No P8-D route feature behavior, operation ID, mutation-epoch notification,
owner-thread/quiescence capability, completed-boundary token, capture
eligibility, export/hydration, or P12-B completion is added. A future profile
that supports route observations or accepted plans needs a separate reviewed
operation/epoch boundary.

## Files

- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Scripts/P12RuntimeIdentitySpatialCensus.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs`

## Validation and limits

The exact corrected Daily-v1 runtime was revalidated on the current canonical
code tree before this implementation: selected bootstrap/admission and
owner/cardinality inventory passed 1/1. Candidate validation on the code tree
above passed focused bootstrap composition 24/24, ALL EditMode 2434/2434,
official Smoke 5/5, and `git diff --check`. The initial full-suite run exposed
one stale test expectation of 258; the assertion was updated to 260 and the
full suite rerun passed. Exact result hashes are in the linked validation
manifest and its archive.

This is a partial census only. P12-B remains `INCOMPLETE`; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked; P12-F Expedition remains deferred.
No complete effective-owner or shared-epoch coverage, global quiescence,
capture eligibility, export, hydration, or downstream readiness is claimed.
