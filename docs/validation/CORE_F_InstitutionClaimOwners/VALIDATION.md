# CORE-F Institution and Claim Owners — Validation

## Original implementation candidate (historical, before review corrections)

- Canonical base: b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a
- Code candidate: 02b2ecdb21641cbcb5a34ab84a091bd0019eadac
- Code tree: fbf403849ea90e5b454ff90b0f406d823fafc919
- Unity input snapshot: candidate commit/tree above; Unity 6000.3.9f1
- .NET source snapshot: candidate commit/tree above; generated-dependency inventory stable

## Initial validation results (historical; not the final promotion evidence)

### Unity

- Full EditMode run 915bd9212ca54371b23e39ed45c36fe1: **3179/3179 passed**, 0 failed, skipped, or inconclusive.
- Smoke gate 8dd8943c32e440eda2f515a2a674e8db: **5/5 passed**. These are the supported Smoke identities; all five also occur in the full EditMode report.
- The full report includes the relevant owner and persistence coverage:

| Suite | Passed |
| --- | ---: |
| InstitutionFoundationTests | 18/18 |
| InstitutionalVacancyRecognitionTests | 7/7 |
| InstitutionWorldIntegrationTests | 6/6 |
| P12EInstitutionOfficeOwnerSnapshotTests | 11/11 |
| P12EInstitutionOfficeOwnerCodecTests | 6/6 |
| PoliticalClaimFoundationTests | 14/14 |
| P12PoliticalClaimCensusTests | 6/6 |
| P12EPoliticalClaimOwnerSnapshotTests | 9/9 |
| P12EPoliticalClaimOwnerCodecTests | 4/4 |
| P12EDailyV1OwnerPackageTests | 6/6 |
| P12GDailyV1OwnerVectorTests | 15/15 |
| P12GPoliticalBindingRestoreTests | 11/11 |

The Smoke filter is an EditMode selection. No PlayMode result is claimed.

### .NET

- Clean-worktree CoreValidation -SkipLab run ae52003f4f5a4110bda712654a54d744, at the exact candidate commit/tree: cold **188/188 passed**, warm **188/188 passed**, status Passed, LibraryAbsent=True.
- -SkipLab is the documented standard CORE TRX profile; Lab is a separate optional gate and is not claimed here.
- The initial private -SkipLab run fbe113886a15439e8783493b03bb0acb is preserved but is not the packaged report. Its captured worktree changes included protected local ProjectSettings files and a then-untracked evidence directory, which the closed TRX provenance profile cannot map as source inputs. The cold/warm evidence was regenerated in a clean detached worktree at the same exact code commit; no production or test source changed.

### Evidence packages

- Unity EditMode archive: [core-f-institution-claim-owners-all-editmode.zip](Evidence/core-f-institution-claim-owners-all-editmode.zip), 167,945 bytes, SHA-256 2518bcd824cb0d39abb920938f87b7d35d3695fb2afa622b4d41dac54ffa0c2a.
- Unity public manifest SHA-256: 72c9d7a63ad60aa5a3b70e31dc4995b482c3e409397c8577c11c156eeb6333f4.
- .NET cold/warm archive: [core-f-institution-claim-owners-dotnet-cold-warm.zip](Evidence/core-f-institution-claim-owners-dotnet-cold-warm.zip), 86,078 bytes, SHA-256 23be1a5090db2533c9506a7d35e4ef61f4d60a09da1c8708abcb94d640380d86.
- .NET public manifest SHA-256: ef81dc5e86c6ea43d9ae8ca50621bf7fcd10db86c9ce0d2f36837cb08b7196ec.
- Unity archive integrity verification passed. The TRX package passed context-bound original-to-projection verification (originalComparisonVerified), with two entries and 188 tests in each cold/warm report.
- Public archives contain normalized result XML only. Private runner manifests, raw logs, runtime traces, logger binary, and assembly/PDB retention remain in owner-only local evidence storage. Public evidence checks passed without local absolute paths or credential-like values. Original TRX files were not modified.

## Exact-tip revalidation after independent review findings

The review fixes are committed at `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d` (tree `e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`), based on the implementation candidate above. The prior implementation and its validation remain historical evidence; this section records the final candidate evidence and is the applicable validation for promotion.

The restore regression now starts from a successful Daily-v1 advance, captures the owner image, encodes and decodes it with the P12 continuation codec, and restores through `TryCreateRestoredSessionFromImage`. It checks target owner-vector recapture, C-F owner parity, the same-day closed office tenure and political-claim recognition history, and two later continuation boundaries. The extra public `InstitutionOfficeOwnerBridge.CreateTenureRecord` API was removed; the approved public `OfficeTenureRecord` constructor is used by the runtime clone and typed owner staging paths.

### Final Unity validation

- Unity `6000.3.9f1`; exact input commit/tree above.
- Focused `P12GPoliticalBindingRestoreTests`: runner run `8a50e0ce3aa440dd896de1369bc6a446`, **11/11 passed**, with stable inputs. Its original filtered XML remains unchanged in protected local retention. The strict public packager rejects this filtered XML because the ancestor `Simulation` suite `testcasecount` describes full discovery; the final complete report below contains all 11 exact test identities and pass outcomes.
- Full EditMode: runner run `a049ae0481be45489d9712e5aa4a24cc`, **3179/3179 passed**, 0 failed, skipped or inconclusive. The normalized package is [`core-f-institution-claim-owners-final-editmode.zip`](Evidence/core-f-institution-claim-owners-final-editmode.zip), 167,357 bytes, SHA-256 `003a5c391500e29e0bc78b1137539ffbdd2773f811711f530acab08749663a52`; manifest SHA-256 `5db0b31a01aa44cf427ea34ed3dbd292f0be49a0832793b0a3c40fec0e7d4c97`. Package verification passed against the protected runner manifest and binds the exact commit/tree.
- Official filtered Smoke: runner run `7700e3bd41154ec58370c33fdf06945b`, **5/5 passed**, 0 failed, skipped or inconclusive, with stable inputs. The original report SHA-256 is `ecdff5d8b48e39647aa032dbc871d9e9ee9a2453686ca53e06422c0e90bc5d8b` (6,367 bytes) and remains unchanged in protected local retention. The public packager rejects the filtered report because its parent `Simulation` suite count reflects full discovery, so no separate normalized Smoke package is claimed. Each of the five exact Smoke identities and its `Passed` outcome occurs exactly once in the packaged full EditMode report: `GMConsolePlayModeSmokeTests.GmConsoleCanPreviewApplyAndRefreshObserver`, `GMConsolePlayModeSmokeTests.GmConsoleNaturalLanguageCanTranslatePreviewApplyAndRefreshObserver`, `WorldObserverPlayModeSmokeTests.ObserverSceneCanRenderAndSelectNode`, `WorldObserverReadModelTests.ObserverPrefabSmokeInitializesWithoutMissingReference`, and `WorldObserverVisualTests.ObserverPrefabSmokeHasNoMissingSerializedReferences`. No filtered XML was edited or reinterpreted.

### Final .NET validation

- CoreValidation `-SkipLab`, run `5d026bdb1d64473fb3d8b004ec8f3151`, exact source commit/tree above, no Unity `Library`: cold **188/188 passed** and warm **188/188 passed**.
- The cold/warm TRX package is [`core-f-instclaim-final-dotnet-cold-warm.zip`](Evidence/core-f-instclaim-final-dotnet-cold-warm.zip), 85,744 bytes, SHA-256 `2b61e8e0b373b0f486dab2df74d011da95890fe62b7d8b5cfd46c8dc6c4a6166`; manifest SHA-256 `d12070bccffa3c272a208cbc83a6d494b7177cb504be4c8c0029be984d6ad12d`. Private context-bound verification passed with `originalComparisonVerified`; cold and warm reports remain separate.

`git diff --check` passed for the review-fix implementation. The protected pre-existing ProjectSettings edits in the candidate worktree remain unstaged and untouched. The final Unity and .NET packages contain normalized results only; original reports and logs remain in their verified private retention. This validation does not claim CORE-F closure or authorize downstream owner/runtime composition work.

## Integrity and scope

- git diff --check: **PASS**.
- The original implementation code/test source identity was 02b2ecdb21641cbcb5a34ab84a091bd0019eadac / fbf403849ea90e5b454ff90b0f406d823fafc919. The final review-fix source identity and applicable validation are recorded above.
- The pre-existing local ProjectSettings/EditorBuildSettings.asset, ProjectSettings/ProjectSettings.asset, and ProjectSettings/ShaderGraphSettings.asset changes remain unstaged and untouched. They are not part of the candidate commit.
- This record validates only the approved CORE-F institution/claim/property-owner extraction. It does not claim CORE-F closure, full runtime extraction, a detached .NET host, or deterministic continuation.
