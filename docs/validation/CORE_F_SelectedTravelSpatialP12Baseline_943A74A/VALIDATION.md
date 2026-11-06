# CORE-F Selected TravelState and SpatialKnowledge P12 Baseline

This record accompanies candidate `943a74a96c4e35d85d40cf3541e012b34220b5aa` (tree `aab3346d9710ad78f6b2b84432c0aba22c54ea83`), based on Core canonical `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The bounded change adds Core-owned baselines and exact receipt checks for the selected solo travel-start operation. It captures the exact TravelState owner and paired SpatialKnowledge rows around host-vector preflight, rejects unreceipted drift, and accepts only the reviewed one-revision TravelState or single-row SpatialKnowledge changes. It preserves full host owner-vector admission and section notifications. This checkpoint does not claim full CORE-F completion, detached hosting, or broader P12 readiness.

## Unity validation

Official Unity EditMode validation ran against the exact candidate commit and tree using Unity `6000.3.9f1` and the repository runner. The run used PowerShell Core `7.6.5` / .NET `10.0.11`.

- Full EditMode run `031b05ae0c0f4b2c926f1855fe736a8c`: **3,300/3,300 passed**, zero failed, skipped, or inconclusive; stable input snapshot `4b61b7f10a8aa6f0a3544fca24596ee6ad84852b2e67aa5d71ac552d62dd31c9`.
- Original full NUnit XML: 3,290,725 bytes, SHA-256 `c9968419df5be8407e31956f5c14bb5d731f1e66351b14ae49385a3fad06e5f8`.
- Normalized full NUnit XML: 1,728,340 bytes, SHA-256 `7c580bd60cc86a92a791f18803ab07a57509216fa19bbb8468719aa2125da190`.
- Public normalized result package: `Unity/coref-selected-travel-spatial-p12-editmode-943a74a9.zip`, 174,931 bytes, SHA-256 `7bf3d3d8dd4ad18d1b55366a3ef851dceb55fb384dffd8df7440901a7404e3c0`. Its companion manifest records the run, source-input identity, result hashes, counts, and path policy.
- The repository packager verified the archive and manifest (`runs=1`, `entries=1`). It removed XML output and applied the declared local assembly-suite path redaction. The original XML and raw log remain in the protected local evidence store.

The separately filtered official Smoke run `38491fe1bdab4a0eafc0232b85f6c6ff` passed **5/5**, with the same candidate commit, tree, and input snapshot. Each of its five test identities and outcomes was found unchanged in the full EditMode report:

- `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`
- `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`
- `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`
- `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`
- `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`

The filtered Smoke XML is 6,361 bytes (SHA-256 `f0b57dbd4971e237be9f22ad9273f9b3db7a73e155939ec15da1321010575924`); its raw log is 52,030 bytes (SHA-256 `fa67922b203430e5ffc110c3b7462d3dfb41017c52a1fb09e0fce383b618f465`). Both are preserved with the original private run evidence. The filtered report is not added as a second public report because the full report already carries all five identities and the filtered NUnit root describes only that selection. They are not added as a second public report because the full report already carries all five identities and the filtered NUnit root describes only that selection.

The prior first-import EditMode run was excluded because Unity changed generated `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` inputs before that run completed. The accepted Smoke and full EditMode runs were rerun after that one-time initialization and both reported stable input snapshots. The candidate worktree and its protected ProjectSettings were not changed by Unity validation.

## CoreValidation and source checks

The official CoreValidation runner passed **1,037/1,037** for both cold and warm runs (`eff334e824b64707b976682748f573d1`). `git diff --check` passed for the implementation change before its candidate commit. The Unity validation source identity is unchanged from the implementation commit; only this validation record and normalized result package are added here.

## Evidence limits

This is validation evidence for the bounded selected solo travel-start/P12 baseline integration only. It does not establish whole-phase closure, full owner coverage, full shared-epoch coverage, persistence/profile expansion, or the complete detached .NET host contract.