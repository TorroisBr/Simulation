# P12 Daily-v1 / P10-A profile reconciliation audit

**Result:** profile identity and composition conflict found; full live-profile census remains blocked.
**P12 canonical inspected:** `bda0bb84df9226702cd67a48025502e5a628957b`.
**Code-bearing P12 tip inspected:** `2bc6d3264c76347eed21b70dcfcde98533f7aa66`, tree `9043a8da8718a364f602eee56aaf48f83f06ad51`.
**Architecture:** `ffd75652d89d862b83d634868c560f8540869b89`.
**P12 authority:** `docs/phases/PHASE12_BRIEF.md` and `docs/PHASE12_STATE.md`.

## Evidence

- `Assets/Scenes/SampleScene.unity` selects `runtimeAdmissionProfile: 1`, which is `UnityBootstrapDailyV1`, and references config GUID `ba87bf49ee034da6bda3daeef8e40c3f`.
- `Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset.meta` has that same GUID. The asset has `useAuthoredGeographyProfile: 1` and a non-null `authoredP10RuinSite` (GUID `6d33bbcced2e469daead9112c21e8a71`).
- `SimulationConfigData.GenesisProfileContractIdentity` selects the P10-A profile identity whenever `authoredP10RuinSite` is non-null, ahead of the authored-geography-only identity. `P10RuinLocalTopologyGenesis.IsEnabled` uses the same field; `SimulationGenesisPipeline.ExecuteStages` includes the P10-A stage when enabled; `TesteSimulacao` composes the Ruin/LocalTopology in that stage.
- The early Daily-v1 rejection in `TesteSimulacao` covers P10-B generated Ruin only. There is no corresponding P10-A rejection. The P14/P10 rejection is conditional on an authored material-flow City, which is a different condition and does not enforce the P12-A P10 exclusion.
- `SimulationBootstrapCompositionTests.SelectedSampleSceneProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne` loads this same asset, asserts the P10 Ruin is non-null, and expects three Locations, one ExplorableSite, three LocalPlaces, and two LocalConnections. It does not set `runtimeAdmissionProfile`; it is standard sample-bootstrap evidence, not a Daily-v1 admission test.
- The P12 Brief defines `UnityBootstrap-Daily-v1` around the SampleScene-selected GeneralTest asset and P9-B authored geography, and separately says the P10-A Ruin/LocalTopology profile is excluded from P12-A. The current SampleScene/asset composition therefore conflicts with the accepted P12 profile description.

## Impact classification

- The promoted TravelParty-start operation remains valid within its exact reviewed owner set and does not depend on P10 topology. Its code/review/validation tree is unchanged.
- The complete P12-B owner/cardinality inventory and P12-A live-profile inventory are not validated: the Daily-v1 profile is currently configured with P10-A identity and state that the accepted P12 scope excludes.
- No P12-A, P12-B completion, capture, export, hydration, or P13 readiness follows. No runtime behavior was changed by this audit.

## Decision required before changing profile composition

Two materially different profile outcomes are possible:

1. Preserve the accepted P12 scope: make the Daily-v1 scene use a P9-B-only authored-geography profile, and keep the P10-A Ruin/LocalTopology composition as its separate proving profile.
2. Revise the accepted P12 profile to include P10-A Ruin/LocalTopology identity, genesis provenance, owner inventory, and eventual exact continuation coverage.

The current P12 Brief and the user's separate P10/P14 scope decision support option 1, but the current SampleScene asset references option 2. A scene/config change or P12 scope revision affects the supported world composition, so neither is applied by this audit. The P12 implementation and profile-inventory tracks remain blocked at this boundary pending resolution.
