# CORE-F Initial Start Transaction — Independent Review

Date: 2026-11-06

## Reviewed identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Implementation commit: 612f837bf775e12ecb3f65b0694af16d58730e62
- Candidate tree: c1b2c4bc099ff898ffb0e6fd97093e3559436329
- Exact base: 1d44f47fdc0eae5f960eb688764d568e889c9e2e
- Base tree: 80883eb19caa6fd6edf92ec16fb70875240277b8
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Verdict

**PASS — source-level exact-tip review.** The reviewer inspected the complete candidate delta against the exact base. No code changes are requested.

Core owns the initial admission and preparation, then invokes the host projection factory. A failed factory result returns before commit. Unity's genesis builder captures its pending projection and returns before the previous publication path; restore returns the candidate created from the exact admitted image. Core commits through the existing NoSave or restored-image proof gates, checks runtime-boundary identity, and publishes through its application session gate.

The review found no initial-admission bypass, duplicate publication authority, or loss of restore-proof identity. The complete delta passes git diff --check.

## Review boundary

No tests were rerun during review. The reported CoreValidation and Unity results are not independently verified from portable artifacts: raw reports are retained privately, while the approved Unity wrapper/packager requires PowerShell Core 7.6.5 and the available runtime is 7.6.9. The Unity run also lacked the wrapper's before/after input inventory while unrelated local ProjectSettings changes were present.

Therefore this record is a source-level code-review PASS only. It does not declare portable evidence complete, VALIDATED_CANDIDATE, CORE-F completion, or canonical promotion readiness. The implementation commit remains an unpromoted recoverable candidate.