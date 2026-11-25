# CORE-F Selected Solo-Travel P12 Core Protocol — Validation

## Candidate

- Branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `8952d1205f2df85587ccc0d0b4436f6174a142fc`
- Candidate tree: `8ad0149fb598e04b2e75caa09e062069940fb98d`
- Parent: `df0fb79f62e07aa29d92609763c705a20c62b821`
- Core canonical remains: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The candidate moves selection of the exact solo-travel P12 owner cut and
opening of its Core execution-state scope into
`DailyV1CoreSelectedP12SoloTravelStartProtocol`. Unity retains its registered
census compatibility port and existing travel projection callbacks. Core
continues to validate and record the selected mutation receipts and publish
the shared epoch. The legacy Unity operation path remains intact.

## Validation results

- Official CoreValidation: **1,134/1,134 cold and 1,134/1,134 warm PASS**.
  Run `e4eab7244fa549b2b97a67e7bbab3eae`; pinned SDK `9.0.315`; stable
  541-input digest `53630437a889353ce4da775029ee56560c5de4b4b51f199429c07a6a2fb3e63f`.
  The private CoreValidation manifest and original TRX remain in protected
  local evidence storage. No public .NET TRX package is claimed because the
  separate `dotnet-trx-source-context-v1` publication adapter was not present.
- Official full Unity EditMode: **3,321/3,321 PASS**, zero failures, skips, or
  inconclusive results. Run `2125592844874b2ebaeb6e81273412c1`; Unity
  `6000.3.9f1`; input inventory stable; repository identity in the run record
  is this exact candidate commit and tree.
- Official Smoke filter (`EditMode`, filter `Smoke`): **5/5 PASS**, zero
  failures, skips, or inconclusive results. Run
  `459c87ca33604ac3a20b1ddd70cbe9ee`; input inventory stable and bound to the
  same candidate commit/tree. The standalone filtered NUnit XML retains the
  editor's unfiltered suite `testcasecount` metadata, so the strict evidence
  packager rejects that filtered report. The verified full EditMode package
  contains the same five Smoke case identities and all five pass.
- `git diff --check`: PASS for the candidate source and test changes.

The privacy-safe full EditMode package is in `Final/`:

- Archive: `coref-solo-travel-p12-core-protocol-final-20261116.zip`
- Archive SHA-256: `4f375e13f0f063f0037ba8531f90a35e9339b7e324eb29bc20049d0b20cef9b8`
- Archive bytes: `176167`
- Package verification: PASS (`runs=1`, `entries=1`)

Original Unity XML and raw logs, the private filtered Smoke run, and the
CoreValidation native evidence remain protected locally and were not included
in the public package. The unrelated local `ProjectSettings` change and prior
private/untracked evidence were left untouched and outside the candidate.

## Independent review and limitations

Independent exact-tip review passed with no actionable findings; see
[`CORE_F_SELECTED_SOLO_TRAVEL_P12_CORE_PROTOCOL_8952D120_INDEPENDENT_REVIEW.md`](../../reviews/CORE_F_SELECTED_SOLO_TRAVEL_P12_CORE_PROTOCOL_8952D120_INDEPENDENT_REVIEW.md).

This checkpoint advances only selected solo-travel P12 operation composition.
It does not establish the public application entry point, detached restoration,
independent .NET continuation, or full CORE-F acceptance. It has not been
promoted to Core canonical.
