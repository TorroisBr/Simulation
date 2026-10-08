# P12-C Daily-v1 Deterministic-Random-Root Implementation Review — R2

**Result:** NEEDS_CHANGES. The corrected exact-tip implementation has no identified source correctness or scope defect in this review, and it now includes the selected-profile composition assertion required by the reviewed design. It is not yet a validated candidate because the required Unity validation and exact-tree evidence are absent.

**Exact implementation candidate:** `14d62862b46458f2b0b452c8e1f469cbd147d63f`
**Exact candidate tree:** `40e584da37bd614f1376950b1517bb52983e65b6`
**Assets subtree:** `71903bf34150c84fb94c151dc8061ca6dbb103a5`
**Candidate branch:** `codex/phase12/P12CRandomRootSnapshotImplementation` (local only; no matching origin ref)
**Actual parent:** `386b2bac79aa3fe8aed97fe49a478963a81c2969`
**P12 canonical:** `82125b8e20ca997226ede0069bc875472cf90430`
**Architecture:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Reviewed design record:** `c9b411faf7a8ab0648c65edf08780232eb849a9b`
**Reviewed design:** `8fbe9fcb881af65c68a96b24226deef5887d62eb`

## Review findings

The change since the prior reviewed implementation tip `386b2bac79aa3fe8aed97fe49a478963a81c2969` is confined to `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`. The new `UnityBootstrapDailyV1UsesBuiltInDeterministicRandomRootAndEffectiveSeed` test loads the selected `Simulation-DailyV1.asset`, derives its effective seed using the same fixed-seed/default-zero rule as `TesteSimulacao.InitializeSimulation`, starts the normal Unity bootstrap with `UnityBootstrapDailyV1` admission, confirms bootstrap publication, and asserts the concrete `DeterministicRandomSource` and exact seed in both the active source and captured root snapshot (lines 140–168). This satisfies the specific missing profile-level proof from the prior review.

The full candidate diff against current P12 canonical contains only the reviewed design/review records plus `DeterministicRandom.cs`, its focused snapshot tests and metadata, and the selected-profile composition test. The owner API remains bounded: immutable schema/provider/algorithm metadata and exact `Int32` seed capture; staged reconstruction rejects null or unsupported metadata without mutating the active source; no draw, stream, identity allocation, consumer-key change, or domain behavior is introduced. The P9 integration requirement remains: a later aggregate must require the RNG effective seed to equal the P9 manifest seed and reject disagreement. This slice does not implement that cross-owner check or claim P12-C completion, P12-A readiness, P13 readiness, export/hydration, promotion, or Phase 12 closure.

The candidate worktree was clean at the reviewed tip. `origin/codex/phase12/canonical` is still `82125b8e20ca997226ede0069bc875472cf90430`; no remote implementation-candidate ref exists. `git diff --check` passes for canonical-to-candidate. No Unity tests were run and no validation archive is present at this tip.

## Required validation still outstanding

Run and retain exact-source evidence for `DeterministicRandomRootSnapshotTests`, existing `DeterministicRandomTests`, the selected-profile bootstrap composition suite, ALL EditMode, official Smoke, applicable `SimulationRuntime` LongRun validation, and `git diff --check`. The validation artifacts and hashes must correspond to the exact reviewed candidate code/tree. Then obtain a fresh exact-tip review if the code tree changes. This review does not authorize canonical promotion.
