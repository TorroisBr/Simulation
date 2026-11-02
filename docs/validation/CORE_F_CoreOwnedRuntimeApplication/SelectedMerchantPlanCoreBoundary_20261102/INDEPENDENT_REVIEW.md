# Independent exact-tip review — selected Merchant plan Core boundary

Date: 2026-11-02

## Verdict

**`VALIDATED_CANDIDATE`.** No actionable implementation findings. This review
does not promote Core canonical and does not complete CORE-F.

This record was prepared by the Master Orchestrator from the independent
reviewer's report. The reviewer performed the code and Unity-package review;
the Master performed the local verification of the protected CoreValidation
TRX files described below. The reviewer did not access those private files.

## Exact identities

- Candidate tip: `a1b4cf23ee8ab9aeb117db7faa5b0ba0576c96bf`.
- Candidate base: `2aa79527bbd93446dfb848086a9d3d8df764c917`.
- Implementation commit: `ae282167680ba2f45028773f20af2eb95c0a4829`.
- Implementation tree: `a09d7a8010beea62aed1af0613c5209ea3baef62`.
- Current `codex/core/canonical`: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate path: `2aa79527` → implementation `ae282167` → validation
  record `911c54da` → CoreValidation evidence-verification note `a1b4cf23`.
  The latter two commits are documentation/evidence only; the implementation
  tree remains `a09d7a80`.

## Implementation review — PASS

The previous travel-plan projection finding is resolved. Core preflights the
exact host projection before P12 admission or mutation for both target changes
and clears. After commit, the host verifies the selected owner and revision,
reconstructs the projection from the exact graph location/City, mirrors the
committed snapshot, and emits the existing P12 receipt once. A rejected clear
projection leaves the Core owner and revision unchanged. Tests cover exact
owner rejection, admission/projection failure, set, replay/no-op, clear,
revision, and receipt order. The review found no additional authority,
determinism, or scope defects.

## Validation evidence

### Independent reviewer checks

- The changed Unity production/test input hashes match the stable Unity
  validation manifest.
- Full EditMode: **3296/3296 PASS**, zero failed/skipped; Smoke: **5/5 PASS**;
  focused `MerchantBoundaryOwnerStepTests`: **10/10 PASS**.
- The exact Unity manifest/archive pair passed the repository verifier. The
  reviewer independently matched the archive byte count (174,163) and SHA-256
  `ff933203997087c7bbbd431e2f01bba48845a5f14931fdd1f97b09e5b5382795` to the
  manifest and confirmed the single normalized XML entry and absence of
  absolute workstation paths in the manifest. The reviewer did not decompress
  the XML; the Master separately ran the approved verifier against the copied
  package and protected source manifest.

### Master-local CoreValidation checks

The protected CoreValidation run is `d1e4a0fbed9240489f14c19d04248dbd`;
private manifest SHA-256 is
`ef821d151e46a2f8cf883ae5b54e72ae9e8c561391cfc040b5b606049738c60a`.
Input digest is
`a3b21e2eeba21beda6db866d0270cbcdd4e96f9df52db61d483def0d8d110fcd`.
The Master parsed the protected original cold/warm reports with the repository
`Get-TrxFacts` parser and compared every `(testId, executionId, outcome)` tuple
against its private manifest list:

| Run | Result | Cases | Identity/outcome mismatches | Original TRX SHA-256 |
|---|---|---:|---:|---|
| Cold | PASS | 992/992 | 0 | `3f5d8b6497bddb47e900ea52dffa78515eed9a8e2a82e9d94bdb87b2c5d37a3d` |
| Warm | PASS | 992/992 | 0 | `2c757cf262328feaac6830b14821b404ec9d5ece1a02ceb7383d87aae83c6f07` |

All six changed Core production/test file hashes match the runner's input
inventory. Raw reports, logs, and private manifests remain in owner-only local
evidence; none are published. The CoreValidation runner emits
`simulation-core-validation/v1`, not the separate
`dotnet-trx-source-context-v1` required by the public TRX adapter, so no public
.NET TRX bundle is claimed and no source context was fabricated.

## Scope and next gate

The candidate is an incremental selected Merchant plan authority boundary. It
does not claim a complete shared runtime, `DailyV1Application.TryStart`,
detached P12 restoration, .NET host, or CORE-F acceptance. Continue the
approved implementation graph; consider canonical promotion only after the
appropriate complete integration gate and current policy preflight.
