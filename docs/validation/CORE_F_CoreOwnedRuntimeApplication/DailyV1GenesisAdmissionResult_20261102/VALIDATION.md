# Core genesis admission-result validation

Date: 2026-11-02

## Candidate

- Repository: https://github.com/TorroisBr/Simulation
- Branch: `codex/core/CORE-F-core-owned-runtime-application`
- Source commit: `a237eb243b6dddbcd69d0a81ae7cea2fc0716603`
- Source tree: `9187c25301d41588289b001b193366fd2c814dd0`
- Parent: `520abace6ad2c3e8cb1d1bccd34a6e522a5d6a70`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The correction retains the exact `P12AResult` produced by the application's one-time initial slot inspection/load. Genesis start-preparation now requires an admitted `NoSave` result before composing and returns that same result instance. The regression asserts reference identity between the admitted result and the preparation result. Present, invalid, inaccessible, or incompatible slots continue to fail closed.

This is still an internal start-preparation boundary. It does not complete public `DailyV1Application.TryStart`, Core-owned detached image restoration, or the independent .NET host. No canonical promotion is claimed.

## Validation

- Official CoreValidation: cold `995/995` PASS; warm `995/995` PASS. Run `7aab628373374b6b8d6eca98baf77e89`; private native manifest SHA-256 `5645213536e0d66f756e8573d1aad6e2174b79d5570e5727a02fa88046d0ac16`.
- Official Unity ALL EditMode: `3296/3296` PASS, zero failures, skips, or inconclusive; run `bc591a7914514b1c96d0b7f79ee79038`; stable input snapshot. The normalized result package is `coref-dailyv1-genesis-admission-result-editmode.zip`, SHA-256 `38dbb7f0669cd17cb26ab165c62d8f16e4e24ad1e9bbd857fb05c6c4eda5b5c7`; manifest SHA-256 `20e918e4dd1abfa58cd8c01135fd59ceb26bb8b33d2a31abfc9491436e727176`. The official package verifier passed against the protected run manifest.
- Official Unity EditMode `Smoke` filter: `5/5` PASS, zero failures, skips, or inconclusive; run `7dd6ebf314754ce4bcb79ccf5da4b6f9`; stable input snapshot. The filtered original remains protected; the packager rejected its suite aggregate because the parent suite's `testcasecount` did not match the filtered descendants. No filtered Smoke package is claimed. The full EditMode package above remains the complete normalized result.
- `git diff --check`: PASS.

The CoreValidation runner's original cold/warm TRX and native manifest remain in protected E: evidence. No public .NET TRX package is claimed: the native CoreValidation manifest is not the separate `dotnet-trx-source-context-v1` adapter schema, and no adapter context was fabricated.

Validation ran before the source/test commit, while those two files were staged only afterward. Their exact bytes match the committed contents: `DailyV1Application.cs` SHA-256 `a31b19d82e9dcd0810b61e0a9677261ea7748f22f637c30c70e46cffb57d3634`; `DailyV1ApplicationHostTests.cs` SHA-256 `959831906941511227b320992a334dd4e60ae94e0cdf419cf8d5376259e3172d`. No source changes followed validation. The three user-owned ProjectSettings modifications were present in the Unity input snapshot and remain uncommitted and untouched.
