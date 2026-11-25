# Core-owned selected action dispatch outcome — validation

## Candidate identity and scope

- Tested base: `7ee230428ec7880b5631bce9b66690d9a061a370`.
- Production source commit: `a32baba3fd4ecc6bb38363dfe93eae87f20c4780` (tree `f1cbe04dcc95866707eddd2675e3aed877a94a26`).
- Expanded test and independent review checkpoint: `3ea2a3a94501582af354921d18c5554e99adb767` (tree `93d7d1237bbfdfcf0a7dc4e45d542e27049ada93`).
- The CoreValidation run below used the expanded test source committed in that checkpoint. No production or test source changed after that run.
- Core now derives the selected action success/result type from its dispatch. The Unity boundary no longer returns those authoritative values; it retains host projection, presentation and the existing partial-transaction fault check.
- The change does not alter owner census registration or the P12 89-family coverage, reservation protocol, owner identities, operation order, or persistence codecs. This is one bounded runtime-authority step, not completion of the shared runtime, `DailyV1Application`, P12 restoration, or CORE-F.

## Validation results

| Gate | Result | Durable identity |
|---|---:|---|
| Official CoreValidation cold | 868/868 PASS | Run `e199074c338a484dbb5d81fea2a75781`; input digest `2dea2ecd477443fc6f826bf4b1ee969d542f38368c861ea7c08fb2b4501e11f1` (503 compile inputs, stable). |
| Official CoreValidation warm | 868/868 PASS | Same run and stable source inventory. The earlier 867/867 run remains retained as historical evidence for the first reviewed version. |
| Focused Unity `EconomyTransactionTests` | 59/59 PASS | Run `40129bfeb908409e8d83aba3cb7d8b62`; original XML SHA-256 `b4f301af257dc34ae692e27c40cebc46c4a153cd4e603eaa07f8b1bd9e75caaf`; log SHA-256 `84c55a093ecb45f0f9c03d571474a574876a52a4b1151871bf6cb040dc8621e7`. |
| Full Unity EditMode | 3271/3271 PASS | Run `82514490730749939a976b797d368590`; stable input SHA-256 `013e38f75c17fd0570f20ed135257918974334ec54a2b7fd9358c1496fce59ae`. |
| Complete Smoke cases within full EditMode | 5/5 PASS | Verified in the same normalized, verified full EditMode package; all five exact cases are listed below. |
| `git diff --check` | PASS | Checked before the test checkpoint and after the validation-record update. |

The verified full EditMode package is `coref-action-editmode-full`: manifest SHA-256 `a475a86e0548f3459032d886cd2a0627a372b99099cc9206160e4ae63a87e02b`; ZIP SHA-256 `d9c35abfde8e948be013b708ad616ddb1667c3f78ef66f280b9f14dddbd6e6f5`. The package verifier bound it to the protected original run manifest and confirmed all 3271 results. The subsequent commit changes only .NET Core test source and a review record; it does not change any input root captured by the Unity run.

The five passing Smoke cases present in that full run are:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`

The separate filtered Smoke XML was not used as evidence because its parent NUnit suite aggregate count did not match its filtered descendants, so the fail-closed packager rejected it. The full-suite package contains and verifies all five cases. Original reports and logs were left unchanged.

The first independent review of candidate `ef475927a2070c371e283bd5fe61949ec64dba02` returned NEEDS_CHANGES only for direct per-kind resolver coverage. That historical result is recorded in [`CORE_F_SelectedActionDispatchAuthority_REVIEW_ef475927.md`](../../reviews/CORE_F_SelectedActionDispatchAuthority_REVIEW_ef475927.md). The expanded tests now cover success, operation failure, and failed rolls for all ten kinds; a fresh exact-tip review is still pending.

## .NET evidence boundary

The current official CoreValidation manifest SHA-256 is `99e5cd5c19a8a15945c4eccdb1d25b72a405addfa2535fbee2d632a2a7ba6515`. Its original cold/warm TRX SHA-256 values are `a75996589946b8dc2d7ec2850863b281126a6edfeef30bc0981cb82b2d563ba7` and `1505ea9e6b7b8cef39047efe17d88bab2b4d2fe8d99b7c6d37c40f8fa3927bfd`. The first run's manifest and reports remain preserved as well. All originals remain in protected owner-only E: retention. The current run emitted `simulation-core-validation/v1`; no public DotNetTrx package is claimed because the approved packager requires the separate `dotnet-trx-source-context-v1` context, which this run did not produce. No manifest or TRX was modified or reinterpreted.

The CoreValidation runner reported that the checkout had a Unity `Library`; this run makes no Library-independence claim. The unrelated local `ProjectSettings` edits and untracked evidence remained untouched and were not staged.
