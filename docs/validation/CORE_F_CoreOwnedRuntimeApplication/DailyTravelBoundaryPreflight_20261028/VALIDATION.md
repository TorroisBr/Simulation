# Admitted travel/P12 boundary preflight — validation

Date: 2026-10-28

## Scope

For a fully selected, admitted Daily-v1 execution, Core now obtains the selected travel projection and P12 context before logger startup, demographic writers, and the remaining daily callbacks. It reuses that exact context at the existing post-actor-turn travel stage. Travel advancement, committed projection publication, post-travel observation, and P12 mutation timing remain in their prior positions.

The preflight is selected from Core's exact roster only when the runtime is admitted and fully selected. Mixed-roster compatibility still executes its existing Unity day-start fallback before the later travel-context acquisition. Non-admitted acquisition timing is unchanged.

The new regression rejects a missing travel/P12 context before logger and enabled demographic-writer callbacks. The mixed-roster regression additionally asserts that its compatibility callback occurs before the later travel-context acquisition. No action outcome or travel ordering expectation was changed.

This is an implementation checkpoint only. It does not complete or promote CORE-F, publish `DailyV1Application.TryStart`, remove Unity action/P12 adapters, or establish detached-host continuation.

## Exact source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Implementation base commit: `f2be368f809c2c5a3e1d7fad933af686d6951ad1`
- Base tree: `f91819baf3b4044d1cc1c8bebdf909b28af2e53f`
- CoreValidation input digest: `b812877ab0062d6a752824d32b1bb0dd820be2c2ed0f83f2d3b82de3629f8f54`
- Unity EditMode and Smoke input digest: `6ac60e5c8c8dc46b30ad68e78fb575d3c0433d03bdf3eba33617a8d8d94c36a2` (identical before/after each run)

| Changed input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `2afa32b99c0d03a7ec36b2a7b2073c951141c4e82fd9eea8466430f75a4e08bf` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `c68d0646dc6bac7aec3cd59e190c248fb13a77fbc5bcdab15ede9665e063108e` |

CoreValidation recorded stable source and generated-dependency inputs. Unity EditMode and Smoke both recorded stable before/after input inventories and the same production-file hash as CoreValidation. The Unity runners do not compile the separate .NET test project.

## Validation results

- Official CoreValidation cold: 909/909 PASS.
- Official CoreValidation warm: 909/909 PASS.
- CoreValidation run: `d297aa9dfd494e72be698f11129a2629`.
- Private CoreValidation manifest SHA-256: `e3a2a1a9715a2ca011bec2f151691c968fb728d9ccdaa56fc7123eb2888388bc`.
- Original cold TRX SHA-256: `d3b14a7e7f36f6aacffc5e7c5417986839347ded761bee0ac1cb83288354cccc`.
- Original warm TRX SHA-256: `f8e0bb312b60e96e9fc85db3e7a93f91596a74d48a21c185700bba606c9f92cc`.
- Full official Unity EditMode: 3276/3276 PASS; zero failed, skipped, or inconclusive tests. The report contains all 7 `SimulationRuntimeLongRunTests` cases, all passing.
- Unity EditMode run: `4a4ec1ba02ac4702b395036c92d923e7`; private manifest SHA-256: `4e6a10cb1778b9212af310ee5183573068b83630e30ed2523a49c93ffe63ce2d`.
- Original EditMode XML SHA-256: `ef6f9728b1492a8a30b66c0de4ed999b9099b1a8fdd3f57938c043d5e85bba03`.
- Official filtered Smoke: 5/5 PASS; zero failed, skipped, or inconclusive tests.
- Smoke run: `c5cbd8082eb44c499e458f0b0afe0466`; private manifest SHA-256: `48d86ba761f7cb01452e9606b2389f14308aa1f2f78da50f5efbb3bb8d96776d`.
- Original Smoke XML SHA-256: `02be34fcdd40dba212c6a22f2172b9ab68962ad50ab11bf33e9314c050abf290`.
- Unity normalized evidence package: [manifest](UnityEvidence/coref-daily-travel-boundary-20261028.manifest.json), [archive](UnityEvidence/coref-daily-travel-boundary-20261028.zip). Official verification passed against the protected original source manifest (`runs=1`, `entries=1`).
- Public package manifest SHA-256: `fc0af11369d25816151646d65422fadb963f0aed1766f90036940b1b0329f823`.
- Normalized archive SHA-256: `c4e2073c969b002fffbee4d298ed8ac918e505bbab562a772f043608ef48ab8a`.
- `git diff --check`: PASS.

Original Core TRX reports, Unity XML, raw logs, and private runner manifests remain unchanged in protected local evidence. No public .NET TRX package was created: the official CoreValidation run did not emit the required `dotnet-trx-source-context-v1` adapter, so no source context was invented. The exact original report hashes and private manifest identity are recorded above. The Unity normalized package verifier checked its privacy and integrity controls. No Library-independent .NET claim is made; the validation worktree had a Unity Library.

Unrelated ProjectSettings changes, untracked user files, metadata, and protected evidence were not staged or modified.