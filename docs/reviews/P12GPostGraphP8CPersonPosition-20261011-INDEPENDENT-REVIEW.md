# P12-G post-graph P8-C Person position — independent review

**Verdict:** VALIDATED_CANDIDATE
**Review:** PASS — exact-tip implementation and sanitized-artifact review
**Reviewed evidence tip:** 9ac08f4c1f22eec5aa090f4ab1e98655141f624d
**Code commit:** b1a854cf2d2b621240cb3fab4bcce5d4bce5d72d
**Code tree:** 67efac7bb24d3109178578a647e6d69161a0313f
**Assets tree:** 2a0d93e07898deecca2600db8b93a13ae7ffb4da
**Test source blob:** 42e08eef6f470f2c86ec7e2009405c6122072f52
**Canonical base:** 6d334011ed428873c92cc66f492ee94c2cf2152b
**Candidate branch:** codex/phase12/P12GPostGuardP8CPersonPositionCurrentBase

## Scope and review

The candidate adds one test to SimulationRuntimeAdmissionTests.cs and a sanitized validation package. It changes no production code, runtime behavior, profile configuration, or domain semantics.

The test injects a supported PersonSpatialPositionStore.TrySetAt write at CandidateGuardBound, after restored-candidate graph checks and guard binding but before final target census. It verifies the exact-empty P8-C Person-position owner changes from cardinality/revision 0/0 to 1/1 while the observed shared census epoch does not advance. The existing post-binding recapture rejects the private candidate with TargetOwnerVectorFailed and OwnerCoverageIncomplete before target-vector recapture completes, boundary admission, or publication. The assertions also preserve the source session, token, health and owner projection; compare continuation against a control; and prove a later valid restore succeeds.

This is distinct from the existing populated-target case, which changes the target owner before its first census. The candidate closes only this named late-recapture witness.

## Validation and artifact review

The code candidate is a direct child of the recorded canonical base. Its only executable-tree change is the 140-line test addition. The validation and digest-metadata commits add documentation and sanitized artifacts only; the reviewed Assets tree and test source remain unchanged after validation.

Exact-tree validation reports:
- Focused test: 1/1 passed.
- SimulationRuntimeAdmissionTests: 195/195 passed.
- ALL EditMode: 2874/2874 passed.
- Official Smoke: 5/5 passed.
- Zero failures, skips, or inconclusive tests.
- git diff --check: PASS.

All ten manifest rows independently match each artifact's SHA-256, Git blob OID, and byte count. All four gzip logs decompress and match their recorded content hashes. The XML reports parse to the stated totals, and the new test appears in the focused, admission, and full EditMode reports.

The reviewer independently applied the recorded local-root substitution rules to the retained raw XML and logs. Generated sanitized XML/log bytes match the committed artifacts; reversing markers restores every raw file byte-for-byte; raw hashes match VALIDATION.md; and raw versus sanitized XML result/count/test-case metadata is identical. Substitution counts match SANITIZATION.md: Program Files backslash 8, Program Files forward slash 20, user-profile forward slash 12, worktree backslash 288, worktree forward slash 16.

The complete candidate artifact payload has no absolute drive-root, UNC, user/worktree/program-files paths, credential-like values, bearer tokens, or private-key markers. Public Unity package URLs are retained.

## Limits

This is one bounded P12-G target-owner recapture witness. It does not establish complete target-owner coverage, complete owner/shared-epoch coverage, runtime-wide quiescence, universal graph rejection or atomicity, causal no-replay, all-owner continuation parity, capture eligibility, export/hydration, P12-A or P13 readiness, or Phase closure. P12-G remains WAIT_DEPENDENCY.
