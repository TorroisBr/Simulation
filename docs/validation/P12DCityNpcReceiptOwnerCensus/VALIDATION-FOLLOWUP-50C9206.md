# P12-D saturated-epoch roster reconciliation validation

This review-requested test-only follow-up is based on candidate commit `50c92061603ec34150cba0bf2a0f6bfddf13d40e` on `codex/phase12/P12DCityNpcReceiptWitnessImplementation`.

## Follow-up scope

The added test `SaturatedEpochCommittedRosterAddFaultsWithoutPublishingReceiptMapsOrProviders` commits an NPC membership addition inside the existing membership boundary, then exhausts the shared mutation epoch before outer reconciliation. It asserts reconciliation publishes no new receipt-family provider rows or receipt maps, leaves registered/expected section maps unchanged, keeps the epoch at `long.MaxValue`, faults census admission closed, and rejects a subsequent registration. No production source or contract behavior changed.

The test file used for validation, read from the worktree, has SHA-256 `0e11b6751567d164bbaf8307c8579a6a3658f0088d3cab8b7876a369084465a6`. At candidate tip `4947ec926b3a48427e9c07de5d722d45014cf1bf`, the committed Git blob has SHA-256 `bc09681aae9074e8d060e61394a66f10c8c2b1dbc550bbfb122295b289cbcf6e`. Raw comparison shows the blob differs from the validated worktree bytes by exactly one inserted CR byte at offset 21439, consistent with a line-ending/filter transformation. The worktree reports the test file clean; this is a byte-level correspondence note, not a later source edit. No tests were rerun for this documentation-only correction.

## Current-tip validation

| Suite | Result | XML artifact | XML SHA-256 | Compressed Unity log | Log SHA-256 |
|---|---:|---|---|---|---|
| Receipt owner focused suite | Passed 13/13; failed=0; skipped=0 | `Raw/Followup-50c9206/Focused/EditMode-20261008-181457-8c18b8072c144cec8e718d58a4fe6ab7.xml` | `262cf3d5dd0d40b34f1d18662c772562ae05c7e2ae01e5d52cc59bea91c0f7f1` | `Raw/Followup-50c9206/Focused/EditMode-20261008-181457-8c18b8072c144cec8e718d58a4fe6ab7.log.gz` | `df43c35160d7fe9c2b20fcbaf73640a1843bc532bde200b7ae85fcc234238beb` |
| ALL EditMode | Passed 2582/2582; failed=0; skipped=0 | `Raw/Followup-50c9206/AllEditMode/EditMode-20261008-181521-69e29d37c80046618b40f5bf2ce3ba46.xml` | `979783a3c596d657dffb7d348cc3c78543e67b00c6b8adf016b00e9c9f55d7f8` | `Raw/Followup-50c9206/AllEditMode/EditMode-20261008-181521-69e29d37c80046618b40f5bf2ce3ba46.log.gz` | `f3c659c04bf16b6d11c1df8b83ff5daca2499bcf13620c6714c97ff386d01312` |
| Official Smoke | Passed 5/5; failed=0; skipped=0 | `Raw/Followup-50c9206/OfficialSmoke/EditMode-20261008-181630-0c353891905644899b01fd044dd78f31.xml` | `7d78251769972d6a452768f999b6b02d2412710c7cc8d359e7f9baef932dad8b` | `Raw/Followup-50c9206/OfficialSmoke/EditMode-20261008-181630-0c353891905644899b01fd044dd78f31.log.gz` | `0f22ddb7354774f9005297463f248324636e94c3782ca4d2ff606fe3c735494d` |

Each compressed log was decompressed and SHA-256 matched to its raw log before the uncompressed duplicate was removed. `git diff --check` passed after implementation of the test and is repeated for the candidate commit.

## Scope boundary

This follow-up adds only the accepted D-owned failure-atomicity witness. It does not change P18 receipt behavior, P12-B semantics/readiness, P12-A readiness, P13 status, export, hydration, or capture eligibility. Unity-generated ProjectSettings edits and unrelated `.meta` files are excluded.
