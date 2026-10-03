# WX-D revalidation before solo-travel promotion (historical)

This record describes the pre-integration P12 canonical at `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`. It remains valid for that baseline and is superseded as the current integration baseline by [WXD_V2_POST_SOLO_TRAVEL_INTEGRATION.md](WXD_V2_POST_SOLO_TRAVEL_INTEGRATION.md).

## Impact

The new P12 slice registers and invalidates the existing Event-ID allocator
counter in the selected daily continuation census. It changes
`RuntimeIdAllocator`, `SimulationRuntime`, `SimulationBootstrapComposition`,
`TesteSimulacao`, continuation census registration, and P12 tests. Its scope
is P12 Event-ID allocation and partial mutation-epoch invalidation.

The WorldId API and publication, public bootstrap `FactualReads` surface,
FR-B coherent admission and Faction/Person revisions, FR-C reader/capability/
result/diagnostic contract, and Faction/Person owners remain unchanged across
the advance. No new WX-D dependency or mapping rule was introduced. The
existing Phase 12 State still marks P12-B incomplete and does not claim export,
hydration, P12-A readiness, or P13 readiness.

**Classification: REINTEGRATE + REVALIDATE.** The WX-D design-review and
implementation commits were replayed on the new canonical tip in this clean,
E:-based worktree. WX-D production code remains isolated in the package and
host adapter and changes none of the shared runtime/bootstrap hotspots. The
new base does change those hotspots, so focused WX-D, FR-B, FR-C, bootstrap,
ALL EditMode, and official Smoke validation are rerun on the replayed
candidate before implementation review and handoff.

## External contract

Simulation-External `origin/main` remains
`0ce8403ba05f778db6850f566a974a4c56cf4edb`, confirmed by the refreshed local
tracking ref and `git ls-remote`. The pinned v2 schema source is unchanged.
Simulation-External is not modified.
