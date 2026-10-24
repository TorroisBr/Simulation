# CORE-F Restored-Session Commit Authority — Exact-Tip Review

- Review result: **VALIDATED_CANDIDATE** — independent implementation review **PASS**, no actionable findings.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Candidate commit: `89c46074ec2b2514f367789202da784f0fd1be5b`.
- Candidate tree: `3dc91f5849cc1ea40f2e91b6407ccbc42d9b6f9b`.
- Exact review base: `41741733121d8f3d71ec7cbdb7eb0ecc540387b4`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate tip remained unchanged between validation, independent review, and this record.

## Reviewed change

The candidate moves the restored-session commit decision and the single active-session publication exchange into `DailyV1Application.TryCommitRestoredCandidate`. Core verifies the exact application/session/candidate relationship, bootstrap lineage, owner-thread requirement, restored provenance and bound restore attestations, candidate health/admission, idle operation state, and the required completed-boundary/profile conditions. Replacement of an active session requires the exact exclusive operation that authorizes the current-session capture.

Unity retains its existing authority for producing health/census attestations and constructing the restored owner graph. `TesteSimulacao` stages the matching projection binding around the Core commit, removes the staged binding if Core rejects, and retires the old projection binding after success. It cannot veto the publication after Core commits. This bounded change does not claim that the health/census computation or restored graph construction has moved into Core.

The independent reviewer examined the full five-file diff against the stated base, including identity, authority, failure rollback, reservation ownership, publication ordering, and the changed regression. No actionable defect or test-integrity issue was found.

## Validation evidence

- CoreValidation run `62f9dfa75afe4c07bda8a5b56aebd169`: **704/704 cold and 704/704 warm PASS**. Captured validation inputs were stable (digest `0b8d9daabe208bc692cc525b4302bbd220d0e44e19cd96848716499224617311`); the tested source/test inputs are the changes committed in this candidate. Original reports remain in the protected local evidence area. The current TRX packager rejected the runner's source-context schema (`simulation-core-validation/v1`) because its reviewed adapter expects `dotnet-trx-source-context-v1`; this record does not claim a compliant public CoreValidation package.
- Focused Unity restore-publication regression, run `ad16ad05a51046f28534a593d479b125`: **1/1 PASS**, stable exact-source inputs; XML SHA-256 `7f725ccdcc31b6cfe53889a9e6f2cce18bb193a2d372df4666be3175711a284d`.
- Full Unity EditMode, run `60f473fdecc1460eb522d87772f4da6f`: **3259/3259 PASS**, `inputStable=true`; XML SHA-256 `9186843fe6536b8cd38b72f13011d68279fc38f3cb6354c05e377b35b48c9aa4`; log SHA-256 `0b8177d1808ef2219f7d5d7672b79be8ad0d749b01ea0e8e6a0a10903e8c16f9`. Official evidence archive verification passed: one run, one archive entry, archive SHA-256 `5056cbe44263757a635c4887043f66c1b89a11264d843cee1d00ebd6b268d022` (172023 bytes).
- Smoke-name-filtered EditMode, run `3dc7512c353a4db698939ae7c7d9226a`: **5/5 PASS**, stable exact-source inputs; XML SHA-256 `b5fb1885cf2cf734ea86d409bd65ec54497a3c9fdfbc8499c00424f0ff0acefc`.
- An earlier first-import focused run was unstable because Unity changed ProjectSettings; it is excluded. A PlayMode-filter invocation returned zero tests and is not treated as coverage. A later two-test Editor filter passed, but is not needed to support the full EditMode result.
- `git diff --check`: PASS before candidate commit.

## Scope limits and integration constraints

This is an unpromoted incremental candidate checkpoint, not complete CORE-F acceptance. It does not establish a Core-owned `SimulationRuntime`, Core-owned restore health/census or restored graph construction, public `DailyV1Application.TryStart`, detached .NET hosting, descriptor-v2 host integration, or separate-process T/T+1/T+2 continuation. P12 and CORE-F remain open. No canonical promotion is requested or recorded here.

The CoreValidation original reports remain protected. The source-context packaging mismatch is an outstanding evidence-packaging limitation for any gate that requires a public TRX package; it must be resolved through the approved adapter/tool workflow, not by altering or reinterpreting the original reports.