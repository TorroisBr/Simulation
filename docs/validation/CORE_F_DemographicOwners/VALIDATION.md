# CORE-F demographic owners — implementation validation

**Status:** implementation and packaged evidence are ready for independent exact-tip review. No promotion or downstream CORE-F closure is claimed here.

## Candidate and scope

- Core canonical base: `0094febf7f97333d0653eaf9688fe1d24d73327f`.
- Code candidate: `9c1ed5e2065f2d509b61e63b39fb6c84508fd5fe`.
- Candidate tree: `f9934338811f7272c8f7013f5bbda14e06c4a92e`.
- Architecture/handoff baseline: `a20978d2b61e07a4b131afbb3971aae346511acb`.
- Scope: the seven approved Aggregate Population sources and two Genealogy sources, their shared Core bridges/tests and required Unity/P12 call-site adapters. The only behavior correction is preserving Genealogy revision on restored-continuation clones; ordinary clone normalization is unchanged.

The implementation preserves owner authority, runtime guards, writer/revision/epoch behavior, deterministic ordering, immutable staged restore, and P12 bytes/validation. `PopulationOwnerBridge.CreateDecodedTransition` is limited to the existing P12 codec's immutable transition construction; it does not expose the transition constructor or add a second codec. The package does not extract unrelated owners, change runtime composition, or claim detached .NET host support.

## .NET validation

The package-compatible clean-worktree CoreValidation run `0a074f3cf76f48b69de59cc4f774aa62` passed cold and warm at 188/188 each. It ran against the candidate commit/tree above with stable input digest `7fc022921404dfcce5e2e1f5162c23aa465fc8766d94ef344cfe1b1f17cd7882`, SDK `9.0.315`, selected CLR `8.0.28`, Release configuration, and no Unity Library in that .NET worktree. The normalized TRX reports were independently compared to the protected originals by the approved adapter; verification returned `originalComparisonVerified`.

- [TRX manifest](Evidence/core-f-demographic-owners-dotnet-cold-warm.manifest.json) — SHA-256 `7e875adf9d5ea4ed6e1b8c92f1061c936765399c07e545148e6e42e70f2035dc`.
- [Normalized cold/warm TRX archive](Evidence/core-f-demographic-owners-dotnet-cold-warm.zip) — SHA-256 `1513b163b861e8cbfb7868fb2d3a4ca5ec11e2d2013f2dde1d400b5636a100d0`, 85,902 bytes.

The initial full CoreValidation run also passed 188/188 cold/warm and its optional Lab cold/warm demonstrations matched. It was retained unchanged; the package-compatible run above is the permanent result package.

## Unity validation

Unity `6000.3.9f1`, Test Framework `1.6.0`, and evidence packager `1.1.0` were used. The final unfiltered ALL EditMode run `6351e43555c7462d8944b36704be140d` passed 3,177/3,177 with zero failures, skips, or inconclusive cases. Its complete 1,659-file validation input snapshot is stable and bound to the candidate commit/tree.

- [ALL EditMode manifest](Evidence/core-f-demographic-owners-editmode-all.manifest.json) — SHA-256 `e880f97406da7cce8f9adb816a2694c6e2ae7accc1d861f1df5335f176b66a14`.
- [Normalized ALL EditMode result archive](Evidence/core-f-demographic-owners-editmode-all.zip) — SHA-256 `f8903c1caeda8d6a1b9ea811ee6dd20eefd65f53ae99293a993a2b45ca4ec596`, 167,241 bytes. The approved packager verified it against the protected original report.

The standalone `SimulationRuntimeLongRunTests` run `6c8cb142810e4999b4479f79cba7e21c` passed 7/7; the official Smoke run `d929426f77a843af866e30a3e729da43` passed 5/5. The packager rejects these filtered XMLs because NUnit retains full-discovery ancestor/fixture `testcasecount` values while the XML contains only the selected descendants. Neither original was edited or published. An independent read-only identity/outcome comparison confirmed that all 7 LongRun and all 5 Smoke case identities occur as Passed cases in the complete normalized ALL report. This follows the existing CORE-F evidence precedent in `CORE_F_SharedRuntimeSource/IMPLEMENTATION_VALIDATION.md`; the raw filtered originals remain in protected local retention.

Focused owner coverage passed 17/17. The Core project built for its supported target with zero warnings/errors. Unity's complete ALL report includes the focused owner, restoration, writer/revision/epoch and P12 regression cases. `git diff --check` is required again on the final evidence/review tip before promotion.

## Evidence and limits

Only the normalized result packages and this human-authored record are intended for Git. Original TRX/XML and raw logs remain in protected local retention; no raw output or private workstation path is included here. The unrelated ProjectSettings entries in the implementation worktree were left untouched and excluded from candidate commits.

Independent exact-tip review and final canonical preflight are still required. Promotion, if those gates pass, delivers only the bounded demographic-owner extraction. Definition-bound runtime closure, shared bootstrap/restoration, detached .NET hosting, v2 persistence and deterministic continuation remain separately gated by the CORE-F closure contract.
