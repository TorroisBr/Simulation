# CORE-F Justice Census Projection Binding — Validation

## Scope

This candidate binds the selected Core-owned Justice records census row and its Unity P18 receipt projection to the exact selected Justice owner during normal admission capture and restored-boundary capture. Core derives records cardinality and revision from the selected owner. The host supplies the existing Unity projection identity and current receipt revision. This is a bounded capture-consistency check; it does not complete CORE-F or claim detached-host readiness.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated pre-commit HEAD: `1e3cbc8dbb4c0cfc4ca10a3762d158ec3170ff53`
- Committed base tree: `d4be057eb1446d4b2d6279b1270c0717f472e4bf`
- CoreValidation captured working-tree input digest: `f41f72e7c347ce1dc13a83b56ce3f2284f2bfc5f25e87cccff3649cfd074d52e`
- The validated production/test snapshot was the four-file implementation/test diff at the captured input digest above. After validation, one XML summary comment was moved to the helper it documents; no executable C# statement or test source changed. This comment-only correction is independently reviewed at the final candidate tip. The validation artifacts identify the pre-comment-move source snapshot.

## CoreValidation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1` run `f966e1b6cb144163927c213d5d20c0dd` passed cold and warm: **1,013/1,013 each**, with zero failed, skipped, or unexecuted tests. The runner manifest reports a successful build and stable captured inputs.

- Private runner manifest SHA-256: `6eda5a7c30739eb764905efc343c9bc791d81442fb9831c908d6fb4855f54637`
- Cold TRX SHA-256: `2cf6ece347b564f808e8fc6fa00768b7aa14297c9a1dae51520882362b51c67a`
- Warm TRX SHA-256: `6b853c88a7fa05b6eb4683c43a92e1d463ba0119db685a22955d51fc4a1d865c`

The original TRX files, runner manifest, and logs remain in protected local validation storage. They are not copied into this repository because they contain private execution metadata. No DotNetTrx public package is claimed by this record.

## Unity validation

The official Unity validation runner passed the full EditMode suite: **3,299/3,299**, zero failures. The official `Smoke` filter also passed **5/5**. The packaged full EditMode XML includes these five smoke identities:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`

The privacy-safe full EditMode package is under `Unity/`:

- Bundle: `coref-justice-census-1104`
- Package manifest SHA-256: `d7c4b3985aba05ef4e7d5d1e8f6721f2a900bfba7a439c6f0381aa86b68d6f88`
- Archive SHA-256: `aa11e6b71dfdb23c04a7041d6c7c9ba0dfb90261bcc330d967c08b7c68bd9452`
- Original protected XML SHA-256: `6e4392b2b861dbd81faef8f891f701ed3080a860c84056237e079c44bfc8e086`
- Normalized XML SHA-256: `642d2b5608854ce2fd428535f935b754f44a354a755522947ef3c67ee8b5f0dc`
- Official packager verification: `VALID`, one run, one archive entry.

The original XML and full log remain in protected local validation storage. The package preserves the normalized result and original-to-normalized hashes without including raw logs.

## Additional checks and limits

- `git diff --check`: passed on the implementation diff before evidence documentation was added.
- An earlier invocation launched from the wrong working directory tested a different checkout and is excluded from this evidence.
- A filtered PlayMode invocation produced an empty report and is excluded; the official EditMode `Smoke` filter is the accepted 5/5 result.
- This validation does not assert full CORE-F completion, all-owner migration, an independent .NET host, or detached continuation.