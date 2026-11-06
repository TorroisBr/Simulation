# CORE-F selected TravelState and SpatialKnowledge P12 baseline validation

Validation date: 2026-11-06

## Candidate and review identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Reviewed implementation commit: 783994b50d0cef619d54ea5c35706fa43fb7515a
- Reviewed implementation tree: 5b4b8060b8d0f4c8349f106ac09e2a974c86a445
- Candidate documentation/review tip: ca598e2afb2246b18b21783e4cff8e3f2d0f981d
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Independent source-review record: ../../reviews/CORE_F_SelectedTravelSpatialP12Baseline_783994B_REVIEW.md (PASS for the exact implementation commit/tree; no source or test-integrity findings).

The candidate source and test files were unchanged after that exact-tip source review. This record adds validation evidence only. No canonical promotion or full CORE-F acceptance is claimed.

## Official Unity validation

Both accepted runs used Unity 6000.3.9f1 and pinned PowerShell Core 7.6.5 with .NET 10.0.11. Each recorded the same Git head, Git tree, and stable validation-input snapshot.

- Full EditMode: run fcb46b3ee2764300aafa1f12c7364625; all tests selected; 3,300/3,300 passed; zero failed, skipped, or inconclusive; inputStable true.
- Official Smoke: run cd0570e8507d412e86fcfc74218606d9; EditMode filter Smoke; 5/5 passed; zero failed, skipped, or inconclusive; inputStable true.

The five Smoke identities each occur exactly once with Passed outcome in both original reports and the packaged full EditMode report:

- GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver
- GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver
- WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode
- WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference
- WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences

The full EditMode original XML is 3,290,817 bytes (SHA-256 bad0dc13321f32a61086dba4df8b7a6549f4c30680571e50fbf29c6b8c3c5f93). Its original raw log is 249,341,473 bytes (SHA-256 333508f381cb57efcdc620b8649af8a9235631ffb2292d6fbb484d83d279a382). The filtered Smoke original XML is 6,364 bytes (SHA-256 c36ba20cdca61bb2df59ddf3e847e2af4c652d40744d14a6b35236c07005e4de). Original XML and logs remain unchanged in protected private retention outside Git.

The official packager rejects the filtered Smoke XML as a standalone public package because the parent Simulation suite testcasecount describes full discovery rather than the filtered five cases. The raw report was not edited or reinterpreted. The five exact Smoke cases and outcomes are cross-checked against the complete packaged EditMode result above, following the repository's established evidence procedure.

Initial first-import and incorrect-filter diagnostic runs were excluded from gate evidence and remain privately retained. The accepted runs use a stabilized common input snapshot.

## Public evidence package

- Manifest: Evidence/coref-travel-spatial-p12-783994b.manifest.json (SHA-256 A65AC055F4250397E09B2419634BD9879B8EA96FD478FD13E3F74B5B59B648B0)
- Result archive: Evidence/coref-travel-spatial-p12-783994b.zip (174,669 bytes; SHA-256 4c0b7f7eeea63f6b332fc8cedc6eb6a4df0e345f9e2c9cc32341db5859faadb0)
- Official package verification: VALID; one full EditMode run, one normalized result XML entry, original-to-package source context verified.
- Privacy: result-output elements were removed under the approved packager policy; the manifest and archive were scanned for local Windows user paths with no matches. The original reports were preserved privately.

## Other validation retained from the reviewed checkpoint

- Official CoreValidation run 448c5f897ad84574b2cb438d872607d2: 1,032/1,032 cold and 1,032/1,032 warm. The exact-tip source review verified its source hashes against the reviewed code tree.
- git diff --check: PASS, recorded by the exact-tip source review.

## Disposition

The selected TravelState/SpatialKnowledge P12 baseline integration now has stable official Unity EditMode and Smoke evidence, in addition to its exact-tip source review and retained CoreValidation result. This closes the validation gate for this bounded candidate only. The candidate remains unpromoted; remaining CORE-F application, restoration, host, and continuation requirements are unchanged.