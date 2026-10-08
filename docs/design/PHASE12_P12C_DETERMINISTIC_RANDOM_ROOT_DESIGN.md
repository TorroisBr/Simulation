# P12-C Daily-v1 Deterministic-Random-Root Technical Design

**Status:** Design candidate for independent exact-tip technical review. This record does not authorize implementation and does not mark P12-C complete or implementation-ready.

**Baseline:** P12 canonical `82125b8e20ca997226ede0069bc875472cf90430`; architecture `47eff220c7ce00f6e7c759bdc2b76780bb46f628`. The canonical branch has the identity/sequence snapshot slice promoted as partial P12-C work. Current State lists deterministic-random roots as a remaining P12-C obligation and this bounded slice as `READY_FOR_DESIGN` (`docs/PHASE12_STATE.md:32-64`). The accepted scope is P12-C in `docs/phases/PHASE12_BRIEF.md:14,26-30` and `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md:118-149`.

## Objective and boundary

Capture and privately reconstruct the selected profile's built-in `DeterministicRandomSource` root: its pinned provider/algorithm identity and its exact effective seed. Preserve all current output behavior. This owner-level slice adds no P12 envelope, profile export/hydration, or consumer changes.

Architecture requires deterministic continuation to retain deterministic-random state along with compatible content/version, authoritative state, and logically ordered inputs (`docs/SIMULATION_ARCHITECTURE.md:707-718,6206-6209`). This design records the provider root needed by the accepted same-build/current-host `UnityBootstrap-Daily-v1` profile. It does not promise cross-host or cross-version numeric portability.

## Source-use inventory and seed sufficiency

The selected profile is the SampleScene-selected P9-B-only `Simulation-DailyV1.asset` (`docs/phases/PHASE12_BRIEF.md:26-27`). `TesteSimulacao.InitializeSimulation` constructs one `DeterministicRandomSource` from `simulationConfig.useFixedSimulationSeed ? simulationConfig.simulationSeed : 0` (`Assets/_Project/Scripts/TesteSimulacao.cs:214`). The same source is passed to `SimulationRuntime` (`:298`), `CrimeSystem` (`:1551-1559`), and `NpcDecisionSystem` (`:1579`). The source's only persistent root value is immutable `Seed` (`Assets/_Project/Scripts/DeterministicRandom.cs:60-70`). `NextUnit` derives a result from seed, key, and draw index (`:73-92`); the source holds no mutable draw counter or stream registry.

The selected call sites invoke direct keyed draws with the default draw index zero:

- NPC choice key `npc-decision|<NpcRuntimeId>|<day>`: `Assets/_Project/Scripts/NpcDecisionSystem.cs:54-58,337`.
- Action-success key `action-success|<actor>|<decision-or-action>|<action>|<day>`: `Assets/_Project/Scripts/SimulationRuntime.cs:10781-10788`.
- Crime target key `crime-steal|<thief>|<day>`: `Assets/_Project/Scripts/CrimeSystem.cs:999-1002`.

The profile does not require `DeterministicRandomStream` cursor state. `CreateStream` creates a separate mutable `DeterministicRandomState` cursor (`DeterministicRandom.cs:9-37,40-58,94-99`). The only production construction found is in `SeededConflictRandomSource` (`ConflictFoundation.cs:674-690`), which is constructed by the separate `WorldObserverDemoBootstrap` (`WorldObserverDemoBootstrap.cs:161-164`). That demo is not the Daily-v1 bootstrap. Battle resolution also constructs a separate seeded provider in `BattleResolutionComputation.cs:57-80` and is outside this profile.

This source inventory makes seed-only owner capture sufficient for the currently selected Daily-v1 root: every included root draw is a pure function of the root seed plus call-site context, and the current calls use draw index zero. The dynamic keys are not random-root state; their authoritative inputs remain the responsibility of their domain owners. Re-run the full source-use audit if the selected profile or any admitted consumer changes.

## Owner and API boundary

Owner: concrete `DeterministicRandomSource` in `Assets/_Project/Scripts/DeterministicRandom.cs`. Keep `IAuthoritativeRandomSource` unchanged; do not add snapshot methods to alternate/test providers. The proposed owner APIs are:

```csharp
public DeterministicRandomRootSnapshot CaptureSnapshot();
internal static bool TryCreateStagedFromSnapshot(
    DeterministicRandomRootSnapshot snapshot,
    out DeterministicRandomSource source,
    out string diagnostic);
```

The factory returns a fresh concrete built-in source. It must not mutate the live source, consume a draw, create a stream, or allocate any domain identity. The future composition layer may consume this staged owner after it has validated the supported profile/provider; that layer is outside this slice.

## Immutable snapshot contract

`DeterministicRandomRootSnapshot` is a detached immutable value object with get-only scalar properties:

| Field | Current value / meaning |
|---|---|
| `SchemaId`, `SchemaVersion` | `deterministic-random-root`, `1` |
| `ProviderId`, `ProviderVersion` | `simulation/deterministic-random-source`, `1` |
| `AlgorithmId`, `AlgorithmVersion` | `fnv1a64-keyed-utf16`, `1`; pins the current seed/key/draw mixing and folded output mapping |
| `Seed` | Exact effective `Int32` seed held by the source |

The algorithm identifier names the existing implementation in `DeterministicRandom.cs:62-92,101-120`; increment its version whenever an implementation change can alter a keyed output. The snapshot stores the effective seed only. The P9-B manifest remains the owner of authored configuration and seed provenance; later aggregate P12-C composition must reconcile the shared effective seed without duplicating P9 provenance in this DTO.

`TryCreateStagedFromSnapshot` fails closed for null snapshots, mismatched/unknown schema ID or version, mismatched/unknown provider ID or version, and mismatched/unknown algorithm ID or version. Every `Int32` seed is valid because the existing provider accepts the full `int` domain; preserve its exact bit pattern without normalization. On success, construct `new DeterministicRandomSource(snapshot.Seed)` and return it only as a private staged candidate. Rejecting a snapshot never changes an active source.

No stream key, draw cursor, RNG output, or per-consumer context is stored here. The current Daily-v1 provider has no owned cursor, and the included consumers supply keyed context from authoritative state. No `NextUnit`, `CreateStream`, or consumer key semantics change.

## Tests and validation plan

Add `Assets/_Project/Tests/EditMode/Editor/DeterministicRandomRootSnapshotTests.cs` with focused cases for:

1. Capture records exact current schema, provider, algorithm, version, and seed values.
2. Staging reconstructs a new built-in source with the exact seed and reproduces direct outputs for the three selected key shapes at draw index zero; include explicit nonzero indices to guard the existing public keyed API.
3. `int.MinValue`, zero, and `int.MaxValue` seeds round-trip exactly.
4. Null, wrong schema/version, provider, or algorithm metadata fails with a diagnostic, returns no staged source, and leaves a separately held active source's outputs unchanged.
5. Snapshot values are detached and immutable; capture/staging do not advance or otherwise mutate the source.

Run existing `DeterministicRandomTests` (`SameSeedContextAndDrawProduceTheSameValue`, `UnrelatedStreamConsumptionDoesNotShiftOperationStream`, `StreamStateMakesDrawPositionExplicitAndIndependent`, `NpcDecisionSelectionDoesNotDependOnActionInsertionOrder`) unchanged. Add or extend a selected-profile case in `SimulationBootstrapCompositionTests` to assert that the normal Daily-v1 composition owns the built-in provider with the expected effective seed; retain `SameSeedWorldsReceiveDistinctStableIdentitiesWithoutChangingSeededRandomDraws` as a regression check (`SimulationBootstrapCompositionTests.cs:103-133`). Do not assert that the separate demo/Battle streams are capturable by this root slice.

After independent design review passes and implementation occurs on a fresh branch from then-current P12 canonical, validate the new root-snapshot suite, `DeterministicRandomTests`, and selected-profile bootstrap composition suite; then run ALL EditMode, official Smoke, applicable `SimulationRuntime` LongRun validation, and `git diff --check` on the exact integrated tree. Retain exact source/test artifacts and hashes, then obtain fresh independent exact-tip implementation review. No implementation tests were run for this design-only candidate.

## Dependencies, hotspots, and exclusions

P12-B is complete within its bounded contract; the identity/sequence snapshot slice is already promoted. P12-C remains partial. P8-A geography and P9-B genesis provenance are separate accepted P12-C owners. The current State also identifies a P8-A snapshot design track (`docs/PHASE12_STATE.md:54-64`); this document changes no spatial ownership. The implementation hotspot is `DeterministicRandom.cs`; avoid edits to `TesteSimulacao.cs`, `SimulationRuntime.cs`, `NpcDecisionSystem.cs`, and `CrimeSystem.cs` unless an exact reviewed integration need emerges. The current design owns only one new documentation artifact.

Excluded: `SeededConflictRandomSource` and WorldObserver demo cursors; Battle RNG; random consumer changes or new draw keys; the separate demographic sample function; P8 geography; P9-B manifest/provenance; P12-A envelope/bootstrap integration; P13 replay/fork guarantees; cross-host/version guarantees; and any amendment to `SIMULATION_ARCHITECTURE.md`. P12-C completion, P12-A readiness, and Phase 12 closure are not claimed.

## Review status and open decisions

No unresolved product or architecture choice was identified by the source audit. The provider/algorithm identifiers and DTO/API names above are proposed technical labels for exact-tip review. This branch remains a design candidate until independent review confirms the scope, source-use inventory, strict staging contract, and test plan. Implementation readiness must not be claimed before that review passes.