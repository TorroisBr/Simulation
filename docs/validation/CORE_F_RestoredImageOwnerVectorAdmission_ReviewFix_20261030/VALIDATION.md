# CORE-F Restored Image Owner-Vector Review Fix

Date: 2026-10-30
Candidate parent: `b4e04296cf254e8f5e2124e1f96af1a19e7aaf11`
Prior exact-tip review: `d565e2ffe4971e4ade0fe3e9de33e7a6fb50bca3` (`NEEDS_CHANGES`)

## Review findings addressed

1. The Application image path and legacy fallback emitted `TargetOwnerVectorValidated` twice or at different points. The Application helper now reports only capture; the restore coordinator emits validation once after the shared target checks. The successful persisted-image test asserts exactly one event in the established sequence.
2. The earlier CoreValidation report used a stale `DailyV1Application.cs` identity. A fresh official run was completed after the Application correction. Its recorded source SHA-256 for that file matches the current source exactly.
3. The prior mutation test exercised in-memory restore, not the persisted-image/Application path. The added regression encodes and decodes a completed persisted image, stages a real private Application candidate, injects a roster write after its first target-vector capture, and verifies rejection during post-guard recapture before boundary admission or publication. It also verifies the original source image bytes remain unchanged.

The candidate guard may be bound to the private candidate before recapture detects the changed vector. That candidate is not published; `ActiveSession` remains null. The test asserts this actual failure boundary rather than requiring rollback of private candidate state.

## Exact source inputs

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` — `eee3a3c5f6ced17c22fc0b8ae48bbbd02ac28863750f0682a90e5e33e81af55d`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` — `754cf37423c5343d19034503fd1bf9508b7fe0392c673d4b2b42f756982509a6`
- `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` — `6bd710d3097a2527e198da94d8d9acd11117f0993852cc8ab306f20a9d276910`

## Validation

- Official CoreValidation: **936/936 cold and 936/936 warm**. Run `991387e8d19543a4aa0f32d79dee05d9`; private manifest SHA-256 `efbf6bfa71a765766eaaf562f7a4bba5685ed40fd7413821413d55e4ceb4c8c5`; working input digest `9809e4fac912d0aceed14f7806a085f419da7e07e1d912117c046fbef7e4ee0f`. The `DailyV1Application.cs` input SHA above matches the manifest. Original cold/warm TRX SHA-256 values: `77a0730a53109687cc91996198fa20cbb667cc6f0c5fe5c2da79e7dc38a51270` and `e7a2af0de0a0d1c8b4e752cd7bbf70429f911f39e2c9a07feeef449eae81fbed`.
- Persisted-image atomicity regression: **1/1 PASS**, run `a29ab22c2899400b83f71bf224d6f137`; private manifest SHA-256 `e7f7c8e7548c3909fde95f8180bad1e035c05f7843d1b11cca6fa9f6475036a0`; XML SHA-256 `bd634b15adbf2cdbf8ef174f4be4f87d006ef607c8e77520fbedff71a2a65d77`.
- P12G restore fixture: **16/16 PASS**, run `216c09275fb340f89c6119df6435f254`; private manifest SHA-256 `b23430de60ba4cc2fb7bbad03d2d896a151374a29263b6344970a08c3aa57043`.
- Full Unity EditMode: **3280/3280 PASS**, run `a90d8390256c430faa3507b78d6ac96f`; package below contains the independently verifiable full report.
- Official Smoke filter: **5/5 PASS**, run `3035cb6f72bd46c88f2c2a6e968fb9fd`; private manifest SHA-256 `ee0e2a1d76cb8d439f4637988e45593573bfdf6e8ebe53ffb57cf4812063925d`. The filtered XML has stale parent suite counts and therefore is not published as a standalone package. All five exact Smoke test identities are also present and Passed in the full packaged 3280-case report.
- `git diff --check`: PASS.

## Public Unity evidence package

The official evidence packager and verifier were used. Package verification passed against the exact full-run manifest (`runs=1`, `entries=1`). The packaged XML and manifest scan found no workstation paths. The archive SHA-256 is `4a7fc953640da173a486042b9f95e711a4668cc1b1eca8538d0459c85e8c9d8d`; the public manifest SHA-256 is `7dbeaf7cd43e7d988373d1997a1d10e2ee4e5b40bf59a6b452620dd3e1c4c58c`.

- [Evidence manifest](UnityEvidence/coref-restored-image-owner-vector-review-fix-20261030.manifest.json)
- [Evidence archive](UnityEvidence/coref-restored-image-owner-vector-review-fix-20261030.zip)

Original run manifests, reports, and logs remain in their private evidence stores and were not modified. This record supersedes the prior review-fix candidate's validation claims only for the exact source hashes listed above. Independent exact-tip re-review is pending. CORE-F remains incomplete and unpromoted.