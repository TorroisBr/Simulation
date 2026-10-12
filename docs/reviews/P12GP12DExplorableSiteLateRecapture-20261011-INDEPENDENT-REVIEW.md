# P12-G P12-D ExplorableSite late recapture — independent review

**Verdict:** VALIDATED_CANDIDATE  
**Review:** PASS — exact-tip implementation and sanitized-artifact review  
**Reviewed evidence tip:** ed1f122cbb95ad9b9a4181027f020e749b37ba4c  
**Code commit:** 3707f5593fd9480b00957402f37e039ea3ffc71c  
**Code tree:** f577bce3393d719e9e1f9234d9e3bc8e2e3c0b06  
**Assets tree:** fbb9ad14489530ab126db075bedc01d04a6d7907  
**Test source blob:** 8ab47743932d04787f7f15505cc60ec497077c1a  
**Canonical base:** 4ab594d35e0d8a02fe29335e696d69aeb1f2daef  
**Candidate branch:** codex/phase12/P12GP12DExplorableSiteLateRecapture

## Scope and review

The candidate adds one test to `SimulationRuntimeAdmissionTests.cs` and a sanitized validation package. It changes no production code, runtime behavior, profile configuration, or domain semantics.

At `CandidateGuardBound`, after graph checks and guard binding, the test uses `ExplorableSiteStore.Add` to insert one synthetic `Generic` site at an already-staged legacy location. The exact selected Daily-v1 `p12d.explorable-sites` target row changes from cardinality/revision 0/0 to 1/1. Restore rejects with `TargetOwnerVectorFailed` / `OwnerCoverageIncomplete` before final target-vector recapture, admission, or publication. The source session, completed-boundary token, source site owner, and source projection remain unchanged; a valid retry reconstructs the same graph and the next continuation matches an uninterrupted control.

This is only a bounded late-recapture rejection witness for the explicitly-empty site owner. It does not add sites to Daily-v1 or claim site export/hydration or broader P12-G readiness.

## Validation and artifact review

The code commit is a direct child of the canonical base. Its only executable-tree change is the new admission test. The validation/evidence commits do not change the reviewed Assets tree or test source.

Exact-tree validation reports:

- Focused case: 1/1 passed.
- `SimulationRuntimeAdmissionTests`: 198/198 passed.
- ALL EditMode: 2877/2877 passed.
- Official Smoke: 5/5 passed.
- Zero failures, skips, or inconclusive tests.
- `git diff --check`: PASS.

All ten manifest entries match committed blob OIDs, byte counts, and SHA-256 values. The four XML reports parse to the stated totals, and the new test appears in focused, admission, and full EditMode reports. All four gzip hashes and decompressed sanitized-log hashes match the recorded values.

The reviewer compared all four raw XML/log pairs against the sanitized candidate. Inverse substitutions reproduce each raw log byte-for-byte and each XML after Git's CRLF-to-LF normalization. All eight original hashes match `VALIDATION.md`; sanitized artifact hashes match the published bytes. XML result metadata is identical before and after sanitation. Replacement counts match `SANITIZATION.md`.

The complete candidate payload has no absolute drive paths, UNC paths, local account labels, email addresses, credential-like tokens, bearer tokens, or private-key markers. Protected ProjectSettings edits and untracked `.meta` files remain outside the candidate and untouched.

## Limits

This single witness does not establish complete target-owner coverage, complete owner/shared-epoch coverage, runtime-wide quiescence, universal graph rejection or atomicity, causal no-replay, all-owner continuation parity, capture eligibility, export/hydration, P12-A or P13 readiness, or Phase closure. P12-G remains `WAIT_DEPENDENCY`.
