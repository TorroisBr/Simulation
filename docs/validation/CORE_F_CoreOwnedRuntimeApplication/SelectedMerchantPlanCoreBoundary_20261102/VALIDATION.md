# CORE-F Selected Merchant Plan Core Boundary

Date: 2026-11-02

## Candidate

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Parent: `2aa79527bbd93446dfb848086a9d3d8df764c917`.
- Implementation commit: `ae282167680ba2f45028773f20af2eb95c0a4829`.
- Implementation tree: `a09d7a8010beea62aed1af0613c5209ea3baef62`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Independent exact-tip review: pending.
- Canonical promotion: not requested or performed.

## Bounded change

The admitted Daily-v1 Merchant operation now writes the selected
`MerchantTradePlan` and `TravelPlan` owners through a Core-owned boundary.
Core retains exact graph/owner checks, no-op behavior, plan mutation and local
revision decisions, and uses the bound Core decision roots when available. The
host ports supply P12 admission and committed-revision notification, logging,
and preflight for Unity's required travel-plan projection. The Unity callback
checks the exact selected owner and projection, mirrors the committed snapshot
through `PublishCommittedOwnerState`, and emits the existing P12 receipt once.

Tests cover exact-owner rejection, admission/projection rejection without an
owner write, exact replay as a no-op, revision/receipt order, and projection of
set and clear snapshots. This is an incremental authority-boundary checkpoint;
it does not deliver `DailyV1Application.TryStart`, the detached host, full P12
restoration, or CORE-F completion.

## Validation

- Official CoreValidation cold and warm: **992/992 PASS** each; cold build,
  test discovery, and both demos passed. Input snapshot was stable with digest
  `a3b21e2eeba21beda6db866d0270cbcdd4e96f9df52db61d483def0d8d110fcd`.
  Private run ID: `d1e4a0fbed9240489f14c19d04248dbd`; private manifest SHA-256:
  `ef821d151e46a2f8cf883ae5b54e72ae9e8c561391cfc040b5b606049738c60a`.
- Focused Unity EditMode filter `MerchantBoundaryOwnerStepTests`: **10/10
  PASS**. The original filtered report remains in protected local evidence.
- Full Unity EditMode: **3296/3296 PASS**, zero failed or skipped; the input
  snapshot was stable. Official Smoke filter: **5/5 PASS** on the identical
  input digest `0c4f1a5fb532fe62b17a7d7cfdbf8bc2f546ae750bb0d61cd338b4848a8d1973`.
  All ten focused cases and all five Smoke cases are present and passed in the
  full report.
- The full Unity result package is [coref-merchant-boundary-editmode-20261102.zip](coref-merchant-boundary-editmode-20261102.zip),
  SHA-256 `ff933203997087c7bbbd431e2f01bba48845a5f14931fdd1f97b09e5b5382795`;
  its manifest is [coref-merchant-boundary-editmode-20261102.manifest.json](coref-merchant-boundary-editmode-20261102.manifest.json),
  SHA-256 `eb7961904e578e413d5e952833a9a45b8ad809994e2e8e7825f1de0dcb1f9fb0`.
  The approved verifier passed and the package contains the normalized complete
  EditMode result. The filtered Focused and Smoke XMLs were not edited or
  packaged because the packager rejects their stale enclosing-suite aggregate
  counts; their original reports remain protected. The complete package
  contains those case results and was verified against the full run manifest.
- The CoreValidation runner emitted `simulation-core-validation/v1`, not the
  separate `dotnet-trx-source-context-v1` required by the public TRX adapter.
  The adapter rejected the private runner manifest as an unsupported context;
  no source context was fabricated and no public .NET TRX package is claimed.
  Original cold/warm TRX reports, logs, and the private manifest remain
  unchanged in owner-only local evidence. Their SHA-256 values are recorded in
  the private manifest.
- `git diff --check`: PASS.

The first two Unity attempts produced no XML because the candidate had a
duplicate method and then an invalid projection API call. Both compile errors
were corrected. They are failed attempts, not test runs. The final focused,
full EditMode, and Smoke executions compiled the corrected source and passed.

## Review and limits

The full exact-tip implementation review is still required. No canonical
promotion or full CORE-F acceptance is claimed. Existing unrelated
`ProjectSettings` edits, `TesteSimulacao.cs`, untracked files, and protected
evidence were excluded from the candidate commit and left untouched.
