# P12-B P8-B passage and crossing witness design

**Status:** Submitted for independent technical review. This is a small owner-
evidence work package inside accepted P12-B; it adds no checkpoint ID and is
not implementation authorization beyond the already accepted P12-B capability
work.

**Canonical base:** `codex/phase12/canonical` at
`4d48a5f88145d748c32c8dc42ab251dcb04f81e4`.

**Dependency:** The non-admitting `ContinuationCensusProtocol` is canonical.
P8-B has no semantic dependency on P8-C. P8-C witnesses are also canonical at
this base; they are not inputs to these providers.

## Owner evidence contracts

The selected `UnityBootstrap-Daily-v1` profile composes a live
`SpatialPassageAuthority` child and its owning `SpatialAuthorityStore` after
authored P8-A geography. It contains no passage options, barriers, or crossings
at the day-zero boundary. Use two separate `ExplicitlyEmpty`, schema-v1
sections:

| Section | Owner identity | Cardinality | Revision stamp |
|---|---|---|---|
| `p8b.passage-option-barrier-state` | The installed `SpatialPassageAuthority` reference | `Options.Count + Barriers.Count` | Its parent installed `SpatialAuthorityStore.Revision` |
| `p8b.crossings` | The installed `SpatialAuthorityStore` reference | `CrossingCount` | The same `SpatialAuthorityStore.Revision` |

`SpatialPassageAuthority` owns registered passage-option and barrier records;
their conditions are paired state, not separate membership. `OptionStates`
also projects registered crossings as `IsCrossing` rows, so those derived
rows are not added to passage-option cardinality. Crossing records are
counted by the separate `p8b.crossings` section.

There is no passage-local revision. The parent's revision is a conservative
stamp: every passage registration/condition commit runs through
`TryCommitPassageMutation`, which applies the mutation and then advances that
revision; `TryRegisterCrossing` likewise increments it after installation.
Other spatial-parent commits may advance the same stamp without changing these
counts. This can cause extra revalidation, but cannot hide a supported P8-B
commit. It is not the shared P12-B mutation epoch.

## Implementation boundary

- Add two passive `IOwnerSectionCensusProvider` adapters in a new P12 P8-B
  census source file. The passage adapter captures the installed parent and
  child references, verifies on every read that
  `ReferenceEquals(parent.PassageAuthority, capturedPassage)`, snapshots the
  sorted `Options` and `Barriers` lists once each, and computes their checked
  cardinality sum. If owner identity changed or the sum overflows, fail the
  read so the future protocol can fail closed. The crossing adapter reads the
  installed parent's `CrossingCount` and `Revision`.
- In `SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`,
  assert both section/schema identities, exact zero cardinalities, revision 1
  after the single P8-A geography commit, stable owner references, empty
  passage option/barrier/state projections, zero crossings, and valid spatial
  invariants.
- In `SpatialPassageAuthorityTests`, cover successful connection and barrier
  registration, crossing registration, and option/barrier/crossing condition
  changes. Each write must update the matching count where applicable and
  advance the shared parent revision. Duplicate or invalid mutations must
  leave the witness values unchanged. Assert that a crossing appears in
  `OptionStates` but is not counted as a passage membership.
- Do not change P8 owner semantics, `SimulationRuntime`, bootstrap registration,
  census protocol registration, mutation-epoch wiring, capture eligibility,
  or the existing P8-C witnesses. Do not claim owner-thread/quiescence or
  complete P12-B/P12-A readiness.

The selected-profile owner and method paths were read from canonical
`SpatialAuthorityStore.cs` / `SpatialPassageAuthority.cs` and the existing
P8-B EditMode suites. P8-B's provider reads remain unsynchronized and are not
usable for capture before P12-B's owner-thread/quiescence gate is established.
