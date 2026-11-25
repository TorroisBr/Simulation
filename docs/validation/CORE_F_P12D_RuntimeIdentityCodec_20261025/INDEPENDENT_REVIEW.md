# CORE-F Runtime Identity Codec Extraction - Independent Review

Date: 2026-10-25

## Review identity

- Verdict: PASS / VALIDATED_CANDIDATE for code.
- Candidate branch: codex/core/CORE-F-p12d-core-hydration
- Candidate commit: 038d7c199d5c2eda248c8c684e7ada0d98351ee1
- Candidate tree: 3f9a35055859fea6d1961d9ad4c6a9ede1000ddf
- Candidate parent: a3088251cbb6f225d75b8739c8fe34cf8a1974e8
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote candidate branch was synchronized to the candidate commit.

## Findings

The independent reviewer found no actionable code defects. The codec and snapshot move preserves admitted owner families, section order and IDs, schema version, alias constraints, revision checks, and rejection of malformed or unsupported input. Unity capture remains in an adapter with the revision-before/after consistency check. The moved Unity metadata GUIDs are unchanged. The Core project and test project compile lists include the new production and test files.

No writer, owner identity, wire value, profile, P12 admission, or publication semantics were changed. No assertions were weakened.

## Evidence boundary

The reviewer could not access the protected E: validation manifests and therefore did not independently attest the reported test results or artifact hashes. The orchestrator's validation record documents its separate source-hash and report-hash checks. This review is a code review only; it does not approve canonical promotion or claim P12/CORE-F completion.
