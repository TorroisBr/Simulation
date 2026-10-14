# Independent exact-tip review: CORE-B CoreValidation runtime evidence

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/core/CORE-B-secure-evidence-directory`
- Candidate commit: `15833b01830d0a8789803af0e28e0e5529ddf916`
- Candidate tree: `163c296a530c477a968f2326166cf62a7a26366f`
- Direct parent: `fc90244ab924643aed42a58c9c2ed3b1ff885b8b`
- Core canonical verified at review: `origin/codex/core/canonical` = `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`; it is an ancestor of the candidate.
- CORE-B source/test baseline: `3560f3952b006d0c6c621d447236f7e163c766dc`; the candidate retains that reviewed production/test source tree.
- Reviewer: independent Codex reviewer; candidate files and branch were not modified.

## Verdict

**VALIDATED_CANDIDATE — PASS for this exact tip and its supporting CoreValidation evidence.** No actionable finding remains in the assigned scope. This review does not promote CORE-B or close a Phase.

## Code and source applicability

The reviewed delta adds protected per-run CoreValidation storage, captures one CLR selection trace for each isolated test process, binds the trace to the test assembly runtime configuration, records trace provenance in the run manifest, and documents/tests that behavior. The exact candidate changes are limited to:

- `Tools/CoreValidation/CoreValidation.psm1`
- `Tools/CoreValidation/Invoke-CoreValidation.ps1`
- `Tools/CoreValidation/README.md`
- `Tools/CoreValidation/tests/Invoke-CoreValidationTests.ps1`

The helper’s trace hash is computed from the same byte array used to record its length. The implementation binds each cold/warm trace to the matching testhost runtime configuration, retains the traces under the owner-protected run directory, and records their hashes. Both isolated trace directories contain one `testhost.*.log`; each reports the expected selected CLR `8.0.28`. The protected-directory and protected-file checks passed for the run directory and trace files.

The exact package manifest binds source commit/tree to this candidate and reports no worktree changes. Production and test source paths remain identical to the reviewed CORE-B implementation baseline. No unrelated files were included. `git diff --check` passed for the candidate delta.

## Post-commit run and package

The post-commit run `74920c53a6894ab1a1b7d942748a02b2` recorded status `Passed` and 97 discovered tests. Cold and warm runs each completed with 97/97 passed, zero failed, skipped, inconclusive, timeout, error, or aborted results. The stable input digest was `66f27ccbc8d032ba766446ffc143aec4541b2de5d14db23e74c04938016ef786`. The cold and warm runtime trace SHA-256 values were `1ccb4b4924de4c6e33d7a0118ca048dd33b30760465efbc873edda0b44d49ffa` and `752738e7872d50b55f99f4a7040abd59588c8d7337b1eb491981bfe0173e48cf`; both selected CLR `8.0.28`.

The retained run package `coreb-coldwarm-74920c53a6894ab1a1b7d942748a02b2.zip` is 39,702 bytes with SHA-256 `b3c745f1c45acaf88397d325c6cc09ae45bfd37d11a2222b757b0b35ec7779a3`. Exact package verification passed with two report entries and `originalComparisonVerified`. The public manifest and normalized reports contain no local drive or UNC paths. The package binds the exact candidate source identity; no raw original TRX or private local path is reproduced in this review record.

## Review limits

This review inspected the exact four-file candidate diff, source/test applicability, run manifest, trace provenance and ACL checks, package hashes, and exact package verification. The reviewer did not rerun Unity or .NET suites; the cold/warm results above are the exact candidate’s retained post-commit evidence. The package was produced before canonical promotion. This review does not claim canonical integration, CORE-B promotion, P12 readiness, full SimulationRuntime hosting, cross-build save compatibility, or CORE-C readiness.
