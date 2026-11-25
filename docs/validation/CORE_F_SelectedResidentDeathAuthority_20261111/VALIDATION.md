# Selected Resident Death Authority Validation

## Candidate change

This checkpoint adds a Core-selected `PopulationResidentDeath` execution path for person-backed NPC resident death under the exact admitted `UnityBootstrap-Daily-v1` graph. It reuses the existing selected Person death preflight and commit, while validating the legacy resident-death proposal against the same Person, NPC, City, population revision, and transition before mutation. The Core boundary receives the distinct `runtime.population.resident-death` contract ID. Mixed or unselected rosters continue through the existing compatibility path. When P12 admission is active, failure to establish the legacy operation scope now faults closed instead of falling through to an unscoped write.

The tests cover the returned transition, one population decrement, selected owner identity/revisions, action clearing, one epoch publication, and rejection of a same-ID foreign City before any authoritative write. The assertions were not weakened.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated base commit: `769ec77d0d0acf9d18ee6fffa942765c7238bd6f`
- Validated base tree: `5fb9bd958d5166d18a6d823072b03dd69d5a6fd6`
- Unity runner input digest: `f98507b5ed6a4bb0020d1607bd9fc195059c81071fc758a89356e1585eb73b57`
- Unity source snapshot was stable for the full run. All five changed production/test files were re-hashed afterward and match the runner snapshot.
- CoreValidation working-input digest: `d2df12141477a7dece90f7b4f2b37b4a152dd2afef73992bd885d83194fd9618`
- The CoreValidation manifest records the same base commit/tree and the five intended changed files. It also records pre-existing user-owned ProjectSettings and local evidence changes; those paths were not staged or modified by this checkpoint.

Changed-file SHA-256 values at validation:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `fcdee13c8a0557ee6c0b2b10312561e2d96e8caf28847526ee2a5c6de98bbcf3` |
| `Assets/_Project/Scripts/P12PopulationLifecycleCensus.cs` | `bb9a8e2ca4917cd53c059c8c46215007f2292e5605ae35ba97a508c8c1b09bb6` |
| `Assets/_Project/Scripts/Population/NpcPopulationLifecycleSystem.cs` | `a2bce6b1d97a7b3cd8e468d5762fb72ae9722c6aa8a7f46e99cc3da14bd16268` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `4165e25a70f4ce6ff26509fea3ca2796377f96a5c833d3d731e36861259805a3` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `cee0e030c3ec77d2a604affb40237eb8d2491f42c50bd5ecaaeba2f3805a8958` |

## Validation results

All listed Unity runs used the pinned Unity `6000.3.9f1`, Test Framework `1.6.0`, and the approved PowerShell `7.6.5` runner. Raw runner reports and logs remain in the private, owner-only evidence store; they are not copied into this repository.

| Validation | Run ID | Result | Input identity |
| --- | --- | --- | --- |
| CoreValidation cold and warm, including Lab build/run | `9d89c1484a744cd3b2de64ced58e1af3` | 1,092/1,092 PASS cold; 1,092/1,092 PASS warm | `d2df12141477a7dece90f7b4f2b37b4a152dd2afef73992bd885d83194fd9618` |
| Focused P12 population lifecycle suite | `5be1a293133e458d87244955362e7af8` | 25/25 PASS | `f98507b5ed6a4bb0020d1607bd9fc195059c81071fc758a89356e1585eb73b57` |
| Related person natural mortality suite | `2cc4a2f5ff7e4b27882cc54cdc23555b` | 15/15 PASS | `f98507b5ed6a4bb0020d1607bd9fc195059c81071fc758a89356e1585eb73b57` |
| Official Smoke | `6df7c43cd9ad4a2cbba1c8b650fb8d1b` | 5/5 PASS | `f98507b5ed6a4bb0020d1607bd9fc195059c81071fc758a89356e1585eb73b57` |
| Full Unity EditMode | `578e8cc710b8462d9f1b2c8e3babe8e9` | 3,308/3,308 PASS; zero failed or skipped | `f98507b5ed6a4bb0020d1607bd9fc195059c81071fc758a89356e1585eb73b57` |
| `git diff --check` | local check | PASS | five changed source/test files |

An earlier full-class attempt (`59e395963282422e895b047f6b1e9afc`) was 24/25 because the new rejection test called the normal epoch accessor after the expected protocol fault. Runtime rejection and pre-write state assertions had passed. The test was corrected to inspect the raw unchanged epoch only in that faulted fixture; the complete suite then passed 25/25. No production behavior or assertion was weakened.

The standalone focused run `bd80cba8c2504e649cb02808d9db2c04` passed 1/1 before the foreign-City regression was added; the later complete suite is the applicable final focused evidence.

## Public Unity evidence package

The official packager normalized the successful full EditMode result and removed output elements. The archive contains only the normalized test-result XML.

- Archive: `Unity/coref-selected-resident-death-editmode-20261111.zip`
- Archive SHA-256: `faaab77788215c275af387dc09506222adbaf1285b4326d9e4550319f7cc5ca3`
- Archive size: 175,329 bytes
- Package manifest SHA-256: `87ff6eb8c3c6aa71a53fa2bd5319232ad79625f4f47accf1f5517bc467507732`
- Official verifier: PASS; one run, one archive entry. Independent XML inspection found 3,308 cases, zero failures, and zero skips.

The CoreValidation runner stores TRX and logs privately and emits its own `simulation-core-validation/v1` manifest. The current runner does not emit the `dotnet-trx-source-context-v1` context required by the approved normalization adapter. Therefore no raw .NET TRX/log is published and this checkpoint makes no public normalized-.NET-evidence claim. Private manifest SHA-256: `5022e778fea9f1475f2a9c5fe685b306940f6b7a9d96c1144351aeedb5eaaf33`. Cold TRX SHA-256: `9cd733bd1e2874835097d541b8e712c7328f518cb77fe8e85297cb70a7cf3645`. Warm TRX SHA-256: `86c866856907741388f1d18acf61374771127907f83632b2c3b8fd451bd81b5c`. These originals remain private and unchanged.

## Scope boundary

This is an intermediate candidate checkpoint, not CORE-F completion, independent exact-tip code review, or Core canonical promotion. The shared runtime/application/restore/host contract remains incomplete.
