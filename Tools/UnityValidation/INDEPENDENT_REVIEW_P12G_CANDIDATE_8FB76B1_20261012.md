# P12-G political binding candidate exact-tip review

Date: 2026-10-12. Reviewer: `/root/p12g_candidates_audit` (independent, read-only).

## Reviewed identity

- P12 canonical base: `eef376d51f36641918a13b2e937e704051814820`.
- Candidate branch: `codex/phase12/P12GPoliticalBindingCompletion`.
- Exact candidate tip: `8fb76b13885c56266d9221f6617b4732a2292621`.
- Candidate Git tree: `f4dfa89f9678fa4478afcefb21c606fe8b8698aa`.
- Validated code commit: `a01d73297228c6ee466bb01a636ca0bcdecc9f68`, tree `de049a4ff5efb78845ce643080ee056787f2cf20`, Assets tree `a61dad9e1755c28c87fc3f7e01e6e30bb1f9964f`.
- Code-source blobs at the validated commit: coordinator `e1ccf80c05fc3e3b8e2ec739376bd78b1f208ea2`; political fixture test `2960505666ce4e1451120c9ef1d010059bb0a217`.
- Git ancestry from the stated base to candidate is a clean fast-forward. The implementation checkout also contained unrelated uncommitted user files, which were not part of the candidate diff and were left untouched. At review time remote P12 canonical remained the base and no remote candidate branch existed; this verdict reviews the exact local commit above.

## Verdict

**NEEDS_CHANGES.** The implementation and supporting matrix are otherwise within the adopted handoff scope, but the validation record assigns three source fingerprints to the wrong objects. The candidate must not be treated as publication-ready or promoted until that integrity record is reconciled and independently checked. This review does not authorize publication or canonical promotion.

## Blocking finding

`docs/validation/P12GPoliticalBindingClosure/VALIDATION.md`, lines 11–13, reports:

- `Assets` tree as `e1ccf80...`, but Git reports `a61dad9...`;
- coordinator blob as `296050...`, but Git reports `e1ccf80...`;
- political test blob as `a61dad9...`, but Git reports `296050...`.

The three labels are rotated. The validated source tree and both file blobs are independently reproducible from the stated code commit. The matrix and current State carry the corrected mapping, but that does not repair the contradictory validation record. No candidate file was edited during this review; the known correction blocker was preserved as instructed.

## Review evidence and scope

- The code change is limited to the bounded internal restored-admission-context factory plumbing and its political binding fixture. The production null-factory path still calls `CreateRestoredContinuationContext`; a supplied factory is restricted to restored-context sources and its result is checked for fresh identity, Daily-v1 restored profile, same/current owner thread. The fixture uses detached unpublished E/F owners, a real successful advance/token and ordinary E/F capture/staging; its three private target corruptions exercise binding rejection and retry. This matches the adopted handoff and does not add a Daily-v1 political writer or production gameplay behavior.
- The packager projects only the supported absolute NUnit Assembly-suite root, declares the path policy, compares source and normalized report semantics under that same projection, and rejects unsupported or ambiguous Assembly paths. The follow-up file-URI scanner change is separately reviewed at `0a41988fec13b0d036c4e286c8ce2f83f5cd7d89`. Retained results report 64 packager assertions and wrapper self-tests 33/33; Unity was not rerun for the tool-only follow-up because the Assets tree did not change.
- The closure matrix’s 56 relative links resolve, and its 35 referenced test/code identifiers occur in the candidate Assets tree. Its Window 2/3 dispositions remain bounded to the inspected Daily-v1 source and do not claim P12-G closure, P12-A readiness, or P13 readiness.
- Candidate validation records focused suites, ALL EditMode 2890/2890, and official Smoke 5/5 with zero failed/skipped/inconclusive results. The deterministic archive and manifest hashes match; the package verifier reports one valid XML entry, and the packaged XML has the declared portable Assembly-suite identity with no scanned absolute local paths. Internal package verification passed. The protected original source-run manifest was not available for independent re-verification in this review checkout; its verification is retained as candidate evidence.
- Base ancestry and `git diff --check` pass. The candidate diff does not change ProjectSettings. Unrelated ProjectSettings edits and untracked `.meta` files present in the implementation checkout were left untouched.

After the fingerprint labels are corrected in the candidate and the metadata-only delta is independently checked, the unchanged code/test tree does not by itself require rerunning Unity under the stated evidence policy. A fresh exact-tip review and normal publication/promotion checks remain necessary.
