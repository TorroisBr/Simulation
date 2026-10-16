# CORE-F Institution and Claim Owners — Validation

## Candidate identity

- Canonical base: b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a
- Code candidate: 02b2ecdb21641cbcb5a34ab84a091bd0019eadac
- Code tree: fbf403849ea90e5b454ff90b0f406d823fafc919
- Unity input snapshot: candidate commit/tree above; Unity 6000.3.9f1
- .NET source snapshot: candidate commit/tree above; generated-dependency inventory stable

## Validation results

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

## Integrity and scope

- git diff --check: **PASS**.
- The candidate code/test source identity is 02b2ecdb21641cbcb5a34ab84a091bd0019eadac / fbf403849ea90e5b454ff90b0f406d823fafc919.
- The pre-existing local ProjectSettings/EditorBuildSettings.asset, ProjectSettings/ProjectSettings.asset, and ProjectSettings/ShaderGraphSettings.asset changes remain unstaged and untouched. They are not part of the candidate commit.
- This record validates only the approved CORE-F institution/claim/property-owner extraction. It does not claim CORE-F closure, full runtime extraction, a detached .NET host, or deterministic continuation.