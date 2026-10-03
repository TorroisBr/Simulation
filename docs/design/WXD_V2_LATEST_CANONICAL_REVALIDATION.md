# WX-D revalidation on the latest Phase 12 canonical

**Current Simulation canonical:** `origin/codex/phase12/canonical` at
`e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.

**Promoted P12-B Event-counter code:**
`a573e5120951f8ac10c2da5b6ad79e066991a57a`, tree
`8e3e2966601d834c2c23429e253d02a9d1a7bb8c`. The latest canonical tip also
contains its review, validation, promotion, and State evidence.

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
