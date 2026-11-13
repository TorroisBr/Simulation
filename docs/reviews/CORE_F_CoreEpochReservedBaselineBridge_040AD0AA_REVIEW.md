# CORE-F reserved Crime/Justice census refresh — independent review

Verdict: **PASS** — exact candidate `678a4bfb3eda6bff83c45f56523654ef3efd6748`.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `040ad0aae03340fd78b4d2272010c32504d4b6c8`
- Validation-note follow-up: `678a4bfb3eda6bff83c45f56523654ef3efd6748`
- Implementation base: `a001664f55f8e7d4659d6729d9a69bb7f95d67a3`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `epoch_final_exact_tip_review` (not the implementation author)
- Reviewed diff: full base-to-candidate diff; the final follow-up changed only the validation note.

## Findings

The reservation-aware census refresh is sound. During a selected Core mutation-epoch reservation, the protocol rejects the ordinary refresh path and accepts a reserved refresh only for the exact opaque token on its owner thread. It refreshes the changed owner witnesses without advancing the epoch; Core completes the reservation once afterward. Failure releases the reservation and faults the session. Tests cover ordinary and wrong-token rejection, exact-token refresh, no-op phases, failed refresh, and single epoch completion. No assertion weakening or other actionable implementation issue was found.

## Exact-source evidence

The protected official CoreValidation manifest matches SHA-256
`acfb2269b365c57c022f39b7c52f6fc2f904387e6157ac7c61fdc87efd31881f`. It binds implementation commit `040ad0aa`, tree `a4fd703e`, and stable 541-input digest
`e2e0cbb074de59e44380d3c92bd63859ba798341af9116d469a0fc8c4292cb5e`. All seven changed Core source/test inputs match the manifest. Cold and warm runs each passed 1,122/1,122; original TRX SHA-256 values are `2cb7b7d7420deee2b643ab5deffdc9b7589e13979a66b79b87f1a422e3eb428b` and `ac1244d0b461b21a5a73236fee0a6297bd96d012230f1afcdea70117d464a9ac`.

The Unity evidence manifest/archive match their recorded hashes. Full EditMode passed 3,320/3,320, official Smoke passed 5/5, and the archive contains zero failures, skips, or inconclusive results. Changed Unity-run inputs match the candidate. `git diff --check` passes through the reviewed tip.

Raw .NET TRX remains in protected local retention. No normalized .NET TRX package is claimed because the official runner emitted no supported source context.

## Scope boundary

This checkpoint fixes only reservation-aware Crime/Justice census baseline refresh. It does not migrate the complete 89-family registry, capture, or hydration; it makes no CORE-F completion, detached host, public `TryStart`, or cross-process continuation claim. Core canonical remains unchanged.