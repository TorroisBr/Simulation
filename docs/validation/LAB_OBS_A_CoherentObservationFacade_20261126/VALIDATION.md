# LAB-OBS-A Coherent Observation Facade - Validation

Date: 2026-11-26

## Candidate and input identity

- Candidate branch: codex/core/LAB-OBS-A-coherent-observation-facade
- Validated implementation commit: 67117b3ed9263f7a82d2b36da207d04a3b37fd58
- Implementation tree: 4090d6be3960abdca1be51aa7c2efbd887416f1c
- Base and Core canonical: 08f6700edbcb6977ab49f353555ca16693024845
- Architecture canonical: cb71a70fc503e926c3b3f39415b86f2fab896ba8

Changed files are the fixed Core observation run, its tests, two explicit project compile lists, and the existing shared DailyV1Application reservation bridge. No Unity-owned observation state, callback, Lab CLI/JSON publication, new profile, geographic binding, Market mutation, Atlas behavior, P13 or CORE-X work was added.

After commit, every file hash matched the official validation inventories: 568 Core inputs, 4 generated Core dependencies, and 1,996 Unity inputs. Core input digest: 1bf76cc860329f99f80f4530356b1b175a9b391e2ae66ac78d3dd552a7f9b4cc. Stable Unity input digest: 14fd8f877b520a75c3017b629c231494519b9f64acab85a2bf55a7fe504234ac. EditMode and Smoke captured the same digest.

## Focused and CoreValidation

Focused facade tests passed 12/12. The official CoreValidation cold and warm results also include all twelve facade test identities as Passed.

- CoreValidation run 36f820db6ace4780950bc2e299735d2e: Passed; .NET SDK 9.0.315; no Unity Library.
- Cold: 1,226/1,226 Passed. Warm: 1,226/1,226 Passed. Core build, CalendarDemo, and Lab cold/warm gates succeeded.
- Private native runner manifest SHA-256: 70ebaf8cd498a17e9b30e244a2a0a17899d14fea9bf779d09c6f9b45dfc14e87.
- Original cold TRX: 1,770,544 bytes; SHA-256 8d50a2083cc446f866813c7a7472bcab3e7c54880865f9a35573881f8fd1d24c.
- Original warm TRX: 1,770,544 bytes; SHA-256 d50232136b905ff1e7cd414d6c3801f9dc70b68dc20eb1bc8c50fea7fb0d6f0c.
- ProcessSeparatedDailyV1ContinuationMatchesThreeDayGenesisControl passed.

The CoreValidation runner emitted its native simulation-core-validation/v1 manifest and original cold/warm TRX, but not the separate dotnet-trx-source-context-v1 required by the approved normalized DotNetTrx adapter. The original reports, manifest, and logs remain in protected local storage. No source context was fabricated and no normalized public .NET package is claimed.

## Unity EditMode and Smoke

Unity Editor 6000.3.9f1; Test Framework 1.6.0; pinned evidence packager PowerShell 7.6.5 / .NET 10.0.11.

- Final ALL EditMode run 742e61a93faa449a861beffecc2f6871: 3,327/3,327 Passed; zero failures, skips, or inconclusive results.
- Stable Unity run-manifest SHA-256: 73b51b665fa59b511c77d69c2fce8fb86b1b8220ea8146cbab4e1188b14dea13.
- Original full XML: 3,307,942 bytes; SHA-256 78d3f1d996b34a9fc3b10c1f01f0ce2dd212f4e6338d597103753a62f60b2c86.
- Original full log: 252,442,888 bytes; SHA-256 c003d5f5ef1f1d0c895973a21c52e7328ce0bc639e2b93f1cd1c7975635171ec.
- Verified normalized full-report bundle labobs-a-all-editmode-ea9a4eeb: archive 176,791 bytes, SHA-256 623e42f5b05f84130fb5746f2ae5121361875ef336eac2825df20efcee2f6e79; package-manifest SHA-256 a0d3c5c24a87c41c47c6eb220c364147b687b82556518986f5268e32f2fc4891. Official Verify passed against the private run manifest. Original XML/log remain unchanged.
- Official Smoke filter Smoke, run 02147942257041868190df5dc8961c6d: 5/5 Passed, zero failures; same stable 1,996-file input digest.
- Original filtered Smoke XML: 6,360 bytes; SHA-256 534b0e77eca1ac57d215f6ee9c68f3172fc07b2965b566fef4f54342320ea0b8. Raw log: 52,165 bytes; SHA-256 84de7fde28ba51840b8ce92baae83308fc16416b4f6f76dd4c342a1f351a1589. Both remain private and unchanged.

The five Smoke identities appear exactly once as Passed in both reports:
- GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver (2188)
- GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver (2189)
- WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode (4560)
- WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference (4588)
- WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences (4611)

The filtered Smoke XML retains Simulation testcasecount=3327 while containing only five descendants, so the fail-closed packager cannot package it alone. The accepted CORE-B/CORE-C evidence treatment and current Core State record this same case: preserve the filtered original privately and verify the same five passing identities in the complete packaged EditMode report. No synthetic XML or repaired original was used.

An initial full EditMode run passed but packaging rejected its input snapshot after Unity normalized line endings in ProjectSettings/EditorBuildSettings.asset and ProjectSettings/ShaderGraphSettings.asset. That run is diagnostic only. The stable full rerun above passed and packaged successfully. Both settings files remain outside this candidate.

## Coverage reconciliation

| Obligation | Evidence |
| --- | --- |
| Fixed NoSave startup, fresh root, session publication, adjacent cuts and stable WorldId | StartsFixedNoSaveProfileAndCapturesTwoAdjacentDetachedCuts |
| Deterministic repetition with a fresh WorldId | EquivalentFixedRunsMatchGameplayFactsAndAllocateFreshWorldIdentity |
| Complete completed-boundary census, not the 62-row lower-level census | The facade recaptures the full 299-row / 89-family admission evidence; adjacent-cut test checks the complete vector. |
| Exact P8, City and Market identities/revisions | Full census/source checks; ScopedMarketReaderRejectsForeignOwnerIdentity. |
| Present-zero versus absent stock | ScopedMarketReaderDistinguishesPresentZeroFromAbsent. |
| Foreign session and equivalent-but-distinct definitions | ReaderRejectsForeignSessionAndEquivalentButDifferentDefinitions. |
| Active incompatible lease/operation, revoked token, competing advance and reservation lifetime | ReaderRejectsIncompatibleLeaseActiveOperationAndRevokedTokenWithoutMutation; ReaderRejectsWrongCutAndReadDoesNotAddMutationOrReleaseAdvanceReservation. |
| Changed/swapped owner witnesses and writer isolation | Complete owner vector and revisions are checked within one reserved session; active competing writes reject and read does not add an epoch. Foreign Market identity is directly rejected. The public API has no mutation/fault-injection seam, so no synthetic owner-swapping hook was added. |
| Invalid/foreign source rows | Foreign session/definition and foreign Market identity are rejected. Fixed admitted sources expose no public row-injection API; malformed internal rows are not fabricated with a test hook. |
| Failed/partial advance | The facade publishes no cut when Core advance fails. Existing Core partial-continuation/failure-atomicity and process-separated continuation tests pass; no custom fault-injection host was added. |
| Advance succeeds, then capture rejects; no fictitious rollback | ReaderRejectsWrongCutAndReadDoesNotAddMutationOrReleaseAdvanceReservation checks the completed advance remains after capture rejection. |
| Ordinary and observing advance equivalence | OrdinaryApplicationAdvanceMatchesObservingRunFacts. |
| Wrong-thread use/disposal and owner-thread cleanup | WrongThreadStopsRunWithoutAdvancing; DisposeRequiresOwnerThreadAndRejectedDisposalPreservesRun. |
| Detached values and no mutable runtime/host-port exposure | PublicFacadeSurfaceDoesNotExposeRuntimeStateOrHostPorts; capture has no callbacks. |

Measured Campo Verde / TRIGO output: Day 1 = 450; Day 2 = 480; adjacent delta = +30. These are observed facade values, not a synthesized Market mutation. Selected Cities retain no P8 Location binding; terrain revision remains an opaque token.

## Disposition

git diff --check passes. This delivers only the Core facade input; whole LAB-OBS-A remains WAIT_DEPENDENCY until the official Lab producer is delivered. ATLAS-B remains outside this candidate. Core canonical is unchanged.

The two Unity-touched ProjectSettings files are preserved as uncommitted worktree modifications and are not part of this candidate. Protected raw reports/logs and private manifests are not committed.

Exact-tip independent review remains pending. No Core canonical promotion was requested or performed.



## Exact-tip validation addendum - 2026-11-26

The follow-up candidate is `f8d8e1c02fe7ea8493019f482adb37af14555b0f`, tree `e0eaa3ad7ac35de1b96dfce786c68576a35340e1`, based on Core canonical `08f6700edbcb6977ab49f353555ca16693024845`. The code-only delta from the initially reviewed facade candidate `67117b3ed9263f7a82d2b36da207d04a3b37fd58` is limited to `src/Simulation.Core/LabObservation/DailyV1LabObservationRun.cs` and `tests/Simulation.Core.Tests/DailyV1LabObservationRunTests.cs`.

Official CoreValidation at the exact candidate tip passed cold and warm: **1,229/1,229** each, with zero failed, skipped, aborted, inconclusive, timed-out, or unexecuted tests. Core build, CalendarDemo cold/warm, and Lab build/cold/warm gates also passed. The exact-source Library-free confirmation used run `f29d80b3befd4c759f1d98ddc4c3c2a7`, SDK 9.0.315, `LibraryAbsent=True`, and Core input digest `4f589d70acac822214018555d3a7dc5bcbca9ef679ac20096812293d75cad13f`. Its protected native manifest SHA-256 is `90f9e405a034d641f9fa53e35cec2da53205aa4c330f5d5bf51bda329db254e3`. No normalized public .NET TRX package is claimed.

The prior Unity EditMode run `742e61a93faa449a861beffecc2f6871` (**3,327/3,327**) and official Smoke run `02147942257041868190df5dc8961c6d` (**5/5**) remain evidence for the unchanged Unity input set, whose recorded digest is `14fd8f877b520a75c3017b629c231494519b9f64acab85a2bf55a7fe504234ac`. The exact `67117b3 to f8d8e1c` delta above changes no Unity package, Assets, ProjectSettings, Unity test, or Unity validation-tool input. These results are carried forward only by unchanged-input applicability; no new Unity execution at `f8d8e1c` is claimed or required for this .NET-only delta.

The independent review of `f8d8e1c` found no implementation defects; its sole NEEDS_CHANGES item was that this record still described the earlier tip and 12 facade tests. This addendum supplies exact-tip validation and states the Unity evidence boundary. Final exact-tip evidence review is pending. No canonical promotion occurred. The two local ProjectSettings edits and protected raw validation reports/logs remain outside the candidate.

## Evidence closure and current-canonical refresh — 2026-11-27

The Core/Lab `core-lab-15-artifacts-v1` TRX profile is now integrated in Core canonical `363d3646a68ac650a942f649d849739d9ced06e4`. The official package verifier accepted the preserved `f29d80b3befd4c759f1d98ddc4c3c2a7` cold/warm run package with original comparison verified: 1,229/1,229 Passed in each report, all eight runner gates successful, and 17 original report/log artifacts preserved byte-for-byte in protected retention. The result ZIP SHA-256 is `58dec316d1de34327bbc91dff5df90ddb3d7c9fa89b37ff0da0b9d9440f7db57`; the manifest SHA-256 is `4c71ae58b86fd4e23e2966cf0cc6f6c4124896f9ad8fd93c89d2844d6cecfe4f`. The independent tooling/package review recorded no findings. No filtered-Smoke XML is claimed or synthesized.

The facade candidate has been refreshed onto that exact canonical base. Its five executable source/project/test files are byte-identical to both the previously reviewed candidate `273badbec178f5b1fcfc3f94f197f7fc1f615b81` and the validated implementation source `f8d8e1c02fe7ea8493019f482adb37af14555b0f`. The Core input digest remains `4f589d70acac822214018555d3a7dc5bcbca9ef679ac20096812293d75cad13f`; Unity inputs remain at `14fd8f877b520a75c3017b629c231494519b9f64acab85a2bf55a7fe504234ac`. The promoted TRX-profile changes are limited to evidence capture/normalization and do not change the Core test runner, Unity execution, or product source. The existing exact-source CoreValidation, Unity EditMode, and Smoke results above are reused by unchanged-input applicability; no new run is claimed for this base refresh.

The refreshed six-path candidate is based on Core canonical `363d3646a68ac650a942f649d849739d9ced06e4`; it carries only the facade source, its tests/project entries, and this validation record. Newer canonical tooling and `docs/CORE_STATE.md` are retained unchanged. Independent exact-tip review of the refreshed candidate remains pending. The facade has not been promoted.
