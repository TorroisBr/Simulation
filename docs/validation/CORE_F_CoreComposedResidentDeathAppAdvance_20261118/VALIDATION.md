# CORE-F Core-Composed Resident Death Application Advance — Validation

## Scope

This checkpoint adds an application-level regression proving that the admitted Core-owned daily stage can execute a resident Person death through the published `DailyV1Application` session, using exact selected owner identities and a complete Core-bound census without a Unity demography projection. The test also checks the one-epoch commit, owner revisions, completed-day capture token, released operation/lease state, and complete post-advance P12 census. The companion selected-stage test seam exposes deterministic mortality samples and Core host ports for this regression. This checkpoint changes tests only; it does not change production semantics.

## Source identity

- Base commit: `bb70de35913e527c5c75c36752923ea85ca0cfec`.
- CoreValidation recorded the base tree and a stable working-input digest; its native manifest is retained privately. The run included the candidate test sources and pre-existing unrelated dirty inputs, so the digest is reported as run provenance, not as a digest of this checkpoint alone.
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`: SHA-256 `0ae224f7b680ebb49732da547e1667b4b9da3db286fe0e84448884bd77c238da`.
- `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs`: SHA-256 `e6baee7a9bc025655b38732053813f1e348884cafa5377a30c3984128e1df6e6`.

## Validation

- Focused application regression: 1/1 passed.
- Official CoreValidation, including its lab build/tests: cold 1,160/1,160 passed; warm 1,160/1,160 passed; native runner status `Passed`. Private run ID `229352bfdfdf455e90dc482f8e3ee520`; private native-manifest SHA-256 `4d65fcdfa67fcfd1f9d4393d1e791b5cfaed45a70f05487277f26043f537dae8`; working-input digest `d043dfc9c3b88ee6fcab54d6a798e155153f69d135bf1643c206fd5112cb804f`. Cold and warm report SHA-256 values: `bbd815331254aa14601f87772ee92a796e69034a580848395a83374f1f1f6799`, `7a65f5e3ea3654cb856ddda63aad7d396f31c2a2c2503c13d68f9a41df516e8c`.
- Official Unity EditMode: 3,322/3,322 passed, 0 failed, 0 skipped; source inputs stable. Original XML SHA-256 `6fe8e97f7451c1acbeb78194edff6b261705a0fa2b8ccb26483855b3c3e364e5`. The repository packager created and verified the sanitized bundle at `Unity/coref-app-death-editmode.manifest.json` and `Unity/coref-app-death-editmode.zip`; archive SHA-256 `c926a7efa74b3ddf2d0ddb14cd0215d7b2dda5cd53e37ea8e52f9799909756ac`; public package-manifest SHA-256 `36de75f35a25841dab60631c32434be245f9a97418272b8582d5fb530de0c98d`.
- Official Smoke: 5/5 passed, 0 failed, 0 skipped; source inputs stable. The retained filtered report SHA-256 is `3674306bfc4f41cee5aa2f722890283361508c3fa45c0a4cbf91953a6cc3f075`. The evidence packager refused this filtered report because its ancestor suites retain full-suite testcase counts (3,322) while the report contains five cases, and two fixture suite counts also disagree. The XML was not changed or republished. All five exact Smoke case identities and their `Passed` outcomes are present identically in the independently verified full EditMode bundle. The original Smoke report and run manifest remain in protected local evidence retention.
- No public .NET TRX package is claimed: the CoreValidation runner emits its native `simulation-core-validation/v1` manifest but not the separate `dotnet-trx-source-context-v1` required by the approved TRX adapter. No source context was fabricated; original reports, manifest, and logs remain protected and unchanged.
- `git diff --check`: PASS.

## Evidence boundary

Only the sanitized full EditMode package is included here. No raw Unity logs, original TRX, private manifest, or workstation path is published. The separate Smoke execution and CoreValidation artifacts remain private for independent review.