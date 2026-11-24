# CORE-F P18-D Merchant Trade State Receipt Owner

## Source identity and scope

This candidate was validated in `codex/core/CORE-F-restore-stage-transaction` with base commit `7c4338d1d7588df4ad3795a31a162d661afd9dd1` and base tree `92d668ed8643cb7db3fe23e29fce5559475b9de8`. The official Unity runner captured source-input digest `b3a2f912c015ee24b70c9cc9f051c8664bab14b403b3b33d2957a6f1b44d30b5`; all 1,988 snapshotted inputs matched the validated worktree.

The slice moves the selected NPC Merchant trade-state receipt owner, its receipt-producing transaction behavior, and its selected census binding into the existing Core owner graph. The Unity bridge updates the compatibility projection from the already committed Core owner state. P12 continues to accept only the established exact-empty Merchant receipt row; this does not add populated receipt persistence.

The exact missing-census regression shrinks from 23 to 13 rows. Remaining rows are Crime P18 (1), Justice P18 (1), keyed-sale Economy (1), and LocalObservation (10). This slice does not claim complete 89-family coverage, census sealing, active-session readiness, Daily-v1 startup, detached restoration, or process-separated continuation. LocalObservation and other unselected owners retain their existing host behavior.

## Validation

- Official CoreValidation: cold and warm `1198/1198` passed. Run `3f4b073f698d4d3f81c30a1ffb389bff`; Core input digest `5ef37e57b036bb94840399d79e92be4bca4666cf883ecd4a8ce9ea6eecd12781`. All 557 inventoried Core inputs match the current worktree. The original cold and warm TRX files match their private manifest hashes and byte counts; the independent reviewer confirmed distinct run identities and all 1,198 results passed with zero non-pass outcomes. Originals and logs remain in protected private storage; no public .NET TRX package is claimed because this run has no approved TRX source-context record.
- Focused selected Merchant integration test: `1/1` passed, run `a95e586cedd64b5e8ede81641b39899d`.
- Official full Unity EditMode: `3326/3326` passed, run `c4d2fb6bc6434756b437147471826fdd`, zero failed, skipped, or inconclusive results.
- Official EditMode Smoke filter: `5/5` passed, run `1bf72206c72341a48130a40b9a66c90e`. Each of the five Smoke case identities also occurs once as passed in the full EditMode report.
- `git diff --check`: passed.

The full EditMode report is packaged at `UnityFull/coref-p18d-merchant-receipt-editmode-20261123.zip`. Its SHA-256 is `910e03074e71e5921b593e3df3db026f7193c862a995ee5814694b6fb9c4912f`; the official package verifier passed for one run and one report. The normalized XML contains no workstation paths or `<output>` element.

The focused and filtered Smoke original XMLs remain unchanged in protected private evidence. The official packager declines these filtered reports because their parent-suite `testcasecount` attributes retain the unfiltered suite counts. They are not included in the public package and are not treated as failures. The coherent full EditMode report contains the focused integration test and all five Smoke cases.

## Independent review and promotion boundary

Independent source/test review: PASS, with no actionable findings. The reviewer verified the Core transaction and Unity projection consistency, the selected integration assertions, in-scope source/test hashes, full EditMode package identity, CoreValidation report hashes/counters, and that protected or unrelated work remained outside the candidate scope. The post-review source correction was included in the focused, full EditMode, and Smoke runs above.

This is a bounded candidate checkpoint only. It does not promote Core canonical or complete CORE-F. The remaining 13 receipt rows continue to block full selected-profile census closure and active-session readiness.