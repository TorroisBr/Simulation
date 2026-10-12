# P12-G post-guard P8-D observation — independent review

**Verdict:** VALIDATED_CANDIDATE
**Review:** PASS — exact-tip implementation and sanitized-artifact review
**Reviewed evidence tip:** 1339c9b4b8df7f67def2d4518a4d86f72813e2a8
**Code commit:** d09c0e3cb2cca908b9b4bb44c70c74d36a32f93a
**Code tree:** 756e06f191f484e3b4e304c7ba35e6179c6b65a6
**Assets tree:** bf9155bd4165aaf89c4bbac3cc0f94e6920bbfcf
**Test source blob:** fff1258a1977f9c640d5ff07028b09463889a099
**Canonical base:** baf3ca2cbe09f0e568b9066644593ac4d98a849f
**Candidate branch:** codex/phase12/P12GP8DPostGuardRecaptureSanitized

## Scope and review

The candidate adds the bounded P8-D late-recapture test and an 11-file sanitized validation package. The evidence-package commit is a direct child of the validated code commit. Its diff contains only the package files under `docs/validation/P12GP8DPostGuardRecapture-20261011/`. The candidate ancestry excludes both prior sanitation commits `5ce4f1d` and `12a13e3`, so the superseded account-label-bearing artifact is not reachable from this publication branch.

The test injects a supported `SpatialRouteKnowledgeStore.TryRecordObservation` write at `CandidateGuardBound`, after target checks and before final target-owner recapture. The exact-empty target row changes from cardinality/revision 0/0 to 1/1 while the observed shared census epoch remains unchanged. Normal recapture rejects before successful recapture, boundary admission, or publication. Assertions preserve the source session, completed-boundary token, health and owner projection, compare continuation against the uninterrupted control, and verify a later valid retry. No production behavior, profile configuration, or domain semantics changed.

The validated code tree, Assets tree and test-source blob remain identical to the exact identities recorded above. The sanitized evidence update changes only artifacts and their records.

## Validation and artifact review

Exact-tree validation reports:

- Focused P8-D test: 1/1 passed.
- `SimulationRuntimeAdmissionTests`: 196/196 passed.
- ALL EditMode: 2875/2875 passed.
- Official Smoke: 5/5 passed.
- Zero failures, skips, or inconclusive results.
- `git diff --check`: PASS.

All ten manifest rows match the committed Git blob OID, SHA-256 and byte count. The four XML reports parse to the stated totals. The four compressed logs contain six generic `SANITIZED_LOCAL_ACCOUNT` markers each; scans found no absolute drive/UNC paths, local account labels, credential patterns or private paths. The final decompressed log hashes match `VALIDATION.md`. XML counts and validation semantics are preserved. Unity was not rerun because sanitation changed only incidental artifact text and the executable/test source tree is unchanged.

## Limits

This is one bounded P12-G post-guard P8-D target-owner recapture witness. It does not establish complete target-owner coverage, complete owner/shared-epoch coverage, runtime-wide quiescence, universal rejection or atomicity, causal no-replay, all-owner continuation parity, capture eligibility, export/hydration, P12-A or P13 readiness, or Phase closure. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.