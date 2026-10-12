# P12-G post-guard P8-C City-binding recapture — independent review

**Disposition:** VALIDATED_CANDIDATE  
**Review:** PASS — exact-tip code and sanitized-artifact review  
**Reviewed evidence tip:** `4e8c602e3fa94898153676c2521988f15c822556`  
**Code commit:** `73a9763b2f17075eb6fe2de1aa7a3dd95d35a2c8`  
**Code tree:** `b218c60bddab88d45709f3e12b3f04e07b200cef`  
**Assets tree:** `71d4f6a5716f3e7eabb3cc36a01822b412f5d009`  
**Test-source blob:** `ea16a734b9f7b85bdeeea1832bb7c3b5e86997b6`  
**Canonical base:** `309b2e592cdc39224e256a992bb92cec819ebfa7`  
**Candidate branch:** `codex/phase12/P12GP8CCityBindingLateRecapture`

## Review findings

The candidate is exactly two commits above canonical base: the code commit is a direct child of the base, and the evidence-package commit is a direct child of the code commit. The complete diff contains one test-source change and the 11-file sanitized validation package. The Assets tree and test-source blob match the identities above.

The test adds a valid P8-C City-to-Location binding to the private restored target at `CandidateGuardBound`, after target checks and before final target-owner recapture. It verifies the exact owner row moves from cardinality/revision 0/0 to 1/1, while the observed shared census epoch remains unchanged. Normal recapture rejects the candidate before target-vector admission or publication. Assertions preserve the source session, completed-boundary token, health and projection, check a later valid retry, and compare continuation with the uninterrupted control. No production/profile behavior or domain semantics changed.

## Validation and artifact review

Exact-tree validation reports focused P8-C 1/1, `SimulationRuntimeAdmissionTests` 197/197, ALL EditMode 2876/2876, and official Smoke 5/5. All reports show zero failures, skips, or inconclusive tests; the focused case appears in the focused, admission, and ALL EditMode reports. `git diff --check` passes.

Every manifest entry matches the candidate Git blob SHA-256, blob ID, and byte count. The independent artifact scan checked all XML reports and compressed logs, including decompressed log hashes. Results and test identities are preserved. Sanitized artifacts contain no absolute drive or UNC paths, local account labels, credential patterns, or private paths. The evidence package records original and sanitized hashes and exact redaction counts. Unity was not rerun because only incidental paths/account markers in generated artifacts were sanitized; production code and test source are unchanged from the validated tree.

Protected ProjectSettings edits and the three unrelated untracked `.meta` files are outside the candidate diff and remain untouched.

## Limits

This is one bounded P12-G post-guard P8-C City-binding target-owner recapture witness. It does not establish complete target-owner or owner/shared-epoch coverage, runtime-wide quiescence, universal rejection or atomicity, causal no-replay, all-owner continuation parity, capture eligibility, export/hydration, P12-G completion, P12-A or P13 readiness, or Phase closure. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.
