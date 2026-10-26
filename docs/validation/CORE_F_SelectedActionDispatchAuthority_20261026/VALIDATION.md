# Core-owned selected action dispatch outcome — validation

## Candidate identity and scope

- Tested base: `7ee230428ec7880b5631bce9b66690d9a061a370`.
- Source/test checkpoint: `a32baba3fd4ecc6bb38363dfe93eae87f20c4780`.
- Checkpoint tree: `f1cbe04dcc95866707eddd2675e3aed877a94a26`.
- The six committed production/test files are the exact source changes validated below. No production or test source changed after the runs.
- Core now derives the selected action success/result type from its dispatch. The Unity boundary no longer returns those authoritative values; it retains host projection, presentation and the existing partial-transaction fault check.
- The change does not alter owner census registration or the P12 89-family coverage, reservation protocol, owner identities, operation order, or persistence codecs. This is one bounded runtime-authority step, not completion of the shared runtime, `DailyV1Application`, P12 restoration, or CORE-F.

## Validation results

| Gate | Result | Durable identity |
|---|---:|---|
| Official CoreValidation cold | 867/867 PASS | Run `6da49b7c92184c42b3e44c61aa7f3d79`; input digest `740abcf5169c8050072e211f13bb916f07995326bfc118f58ae57fbce0dd3486` (503 compile inputs, stable). |
| Official CoreValidation warm | 867/867 PASS | Same run and stable source inventory. |
| Focused Unity `EconomyTransactionTests` | 59/59 PASS | Run `40129bfeb908409e8d83aba3cb7d8b62`; original XML SHA-256 `b4f301af257dc34ae692e27c40cebc46c4a153cd4e603eaa07f8b1bd9e75caaf`; log SHA-256 `84c55a093ecb45f0f9c03d571474a574876a52a4b1151871bf6cb040dc8621e7`. |
| Full Unity EditMode | 3271/3271 PASS | Run `82514490730749939a976b797d368590`; stable input SHA-256 `013e38f75c17fd0570f20ed135257918974334ec54a2b7fd9358c1496fce59ae`. |
| Complete Smoke cases within full EditMode | 5/5 PASS | Verified in the same normalized, verified full EditMode package; all five exact cases are listed below. |
| `git diff --check` | PASS | Checked before and after the source checkpoint. |

The verified full EditMode package is `coref-action-editmode-full`: manifest SHA-256 `a475a86e0548f3459032d886cd2a0627a372b99099cc9206160e4ae63a87e02b`; ZIP SHA-256 `d9c35abfde8e948be013b708ad616ddb1667c3f78ef66f280b9f14dddbd6e6f5`. The package verifier bound it to the protected original run manifest and confirmed all 3271 results.

The five passing Smoke cases present in that full run are:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`

The separate filtered Smoke XML was not used as evidence because its parent NUnit suite aggregate count did not match its filtered descendants, so the fail-closed packager rejected it. The full-suite package contains and verifies all five cases. Original reports and logs were left unchanged.

## .NET evidence boundary

The official CoreValidation manifest SHA-256 is `5ba41f0c5142ee73cd75c235c57612ebfa811f36982ad4f50ee077894e848d73`. Its original cold/warm TRX SHA-256 values are `ebe1f279e4b8d440646ff9691ddbce86890e71273a6291000bcda84c0a0263df` and `c0ef43ae28f5b5e21038f42cc24d52d7de2441cc6ff3990f4229fa68766f9062`. They remain in protected owner-only E: retention. The run emitted `simulation-core-validation/v1`; no public DotNetTrx package is claimed because the approved packager requires the separate `dotnet-trx-source-context-v1` context, which this run did not produce. No manifest or TRX was modified or reinterpreted.

The CoreValidation runner reported that the checkout had a Unity `Library`; this run makes no Library-independence claim. The unrelated local `ProjectSettings` edits and untracked evidence remained untouched and were not staged.
