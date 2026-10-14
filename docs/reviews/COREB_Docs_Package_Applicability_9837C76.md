# Independent exact-tip applicability review: CORE-B evidence successor

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/core/CORE-B-secure-evidence-directory`
- Documentation/package successor: `9837c76f2807e039328ce508028b6a71588ce4b3` (tree `4e1d5558243eec1f8e610d31795e42d4b3fa5f50`)
- Parent and reviewed code tip: `15833b01830d0a8789803af0e28e0e5529ddf916` (tree `163c296a530c477a968f2326166cf62a7a26366f`)
- Current canonical verified at review: `origin/codex/core/canonical` = `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`; this is the candidate’s merge base.
- Prior exact-tip implementation review: `79460dbffef35eeaa85f814e11e2a7b6a6bc894f`.
- Reviewer: independent Codex reviewer; candidate and canonical refs were not modified.

## Verdict

**VALIDATED_CANDIDATE — PASS for this exact docs/package successor.** Its cumulative code and test inputs remain the exact reviewed tip `15833b0`; no implementation finding was introduced. This review does not promote CORE-B or close a Phase.

## Successor delta and evidence identity

The exact delta from `15833b0` contains only `docs/CORE_STATE.md`, `docs/validation/COREB_DeterministicTemporal/VALIDATION.md`, the package manifest, and the package ZIP. The production, test, and tooling paths are unchanged. `git diff --check` passes.

The candidate manifest blob and ZIP blob were compared with the respective files retained from private run `74920c53a6894ab1a1b7d942748a02b2`; their Git blob identities match exactly. The retained package is 39,702 bytes with SHA-256 `b3c745f1c45acaf88397d325c6cc09ae45bfd37d11a2222b757b0b35ec7779a3`. The retained 42,804-byte manifest has SHA-256 `16f6331cc36c0c01f953e2cda09d71139824bf4ef92ab710287e8e4d9f3e3044`, matching the public documentation and committed artifact.

The manifest binds source commit/tree `15833b01830d0a8789803af0e28e0e5529ddf916` / `163c296a530c477a968f2326166cf62a7a26366f`, stable input digest `66f27ccbc8d032ba766446ffc143aec4541b2de5d14db23e74c04938016ef786`, and two cold/warm report entries. Each completed with 97/97 passed. Its recorded source projections match the unchanged original reports; the private package verification previously returned `originalComparisonVerified`. The archive contains only the two normalized cold/warm reports, not the private original TRX files. The package’s owner-only retention receipt and planned 30-day post-promotion retention are consistent with the validation record.

The public manifest and both reports were scanned for drive and UNC paths; no matches were found. The docs and manifest expose no absolute local checkout path or secret. The added artifacts and wording preserve the original-report privacy boundary.

## State and scope

`CORE_STATE.md` and the validation record identify `15833b0` as reviewed and still unpromoted, pending canonical fast-forward. They retain the narrow CoreValidation tooling scope, exact run and package identity, protected retention and privacy claims, and the explicit lack of a P12 readiness claim. The `worktreeLibraryAbsent=True` statement matches the retained run manifest. The prior Unity Library limitation is described separately and remains scoped to that earlier Unity worktree.

## Review limits

This was a docs/package applicability review. No Unity or .NET suites were rerun because source, test, and tooling inputs are identical to the previously reviewed candidate. The exact manifest and archive match the already verified retained run. This record does not replace that execution or private comparison, promote the candidate, or claim P12 readiness.
