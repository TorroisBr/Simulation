# CORE-F Core-only autonomous Steal turn

Status: validated implementation candidate; independent exact-tip review passed; candidate publication pending.

## Delivered behavior

The selected Core-only actor-turn boundary admits the existing Steal action when it has the exact supported Core action slot and distinct target relationship. Core's existing crime operation performs selection, theft, owner mutations and P12 census refresh. The slice does not broaden the action catalog or alter Unity's fallback for unsupported or mixed rosters.

## Source identity

- Reviewed candidate commit: `4b50d4b443a1f26a0e8ddc5860bd06f9aa788fa9`
- Candidate tree: `a87157eb8607a6707d7ab075e760fc4a31c28663`
- Implementation source commit: `3c6e3355230c808d16691dba088534dab302606b`
- Review baseline / validation starting tip: `81b187dce3eb1d9c5714b7b4497a25fa2f7b202d`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- CoreValidation working-input digest: `4500ecc0c079d9877b4fb40da6701f5f411383e768d667a88af69e0466d64814`
- Production source SHA-256: `3433f89a5f30e15ed19756048bf5d96769029a5c4df8617ada0b0103700d6d2c`
- Test source SHA-256 as consumed by CoreValidation: `0fee0b43ce5c6ebaa31f1ed53e832fa03ebf0086315af4c95f7dd02b1bb50f29` (229,810 bytes)

The test-source hash is the exact checked-out byte representation compiled by CoreValidation. Git normalizes line endings in the committed blob; after CRLF-to-LF normalization, the candidate blob and tested content are identical. The independent review verified this normalization. Protected ProjectSettings edits, an unrelated earlier validation-document change, private evidence and untracked files remain outside the candidate and untouched.

## Validation

- Focused actor-turn cases: **11/11 PASS**.
- Official CoreValidation exact-tip revalidation: **1,173/1,173 cold and 1,173/1,173 warm PASS**, stable inputs, SDK `9.0.315`. Run `d5adff6c852c4ddeab5e01510a419df4`; native manifest SHA-256 `d62567a7b780a7ac27cab3d230b6bd0da5812210c30b756ca70dbae750347ef9`; cold TRX SHA-256 `67ff3d280d4e240447cce5273f1a7727247b3dacb65dfbe36212549972679eeb`; warm TRX SHA-256 `35c0e3ee61238b3e09cb311130418479611ba8ab1547b3aa9ff2f4e77597c3f0`.
- Official full Unity EditMode: **3,325/3,325 PASS**, zero failed, skipped or inconclusive. All seven `SimulationRuntimeLongRunTests` cases passed. Run `938c3309f1144d0db76cc5ee4e9123f9`; input digest `efe6c98a91361b5d76e7b87fc33ea3b59ca88a4ac3e72e3b2c4618b1c5e83fc7`; original XML SHA-256 `59b175b0600040079431feb2fef6701f47d1bb3fdf07d04dd49d1829046bb52e`.
- Full EditMode result-only package: [manifest](Unity/coref-core-only-steal-full-editmode-20261120.manifest.json), [archive](Unity/coref-core-only-steal-full-editmode-20261120.zip). Approved packager verification passed. Manifest SHA-256 `aa1799c2892b5afb6edf981dc896b7ccd8aeb4056f3f818e11d56afdfb411480`; archive SHA-256 `3f93f7e372c5c6256d5642e21c4d47348aa281e98445f5fb5bb64743f66b88a9`.
- `GMConsolePlayModeSmokeTests` is an Editor fixture that enters Play Mode from EditMode. Its documented PlayMode selection returned zero tests and is not claimed as a pass. Correct EditMode selection passed **2/2** (run `7fb01400eb2a4f90a2c9531a328acf08`, original XML SHA-256 `5fd4fca671b64cf36a40c3df826a7da78dfcaa004613b4260533728ad5a0e207`); both cases are in the full EditMode report. No separate Smoke 5/5 claim is made.
- `git diff --check`: **PASS**.

CoreValidation's raw cold/warm TRX files and native manifest remain in the protected local E: evidence store. No public .NET TRX package is claimed: the approved packager requires `dotnet-trx-source-context-v1`, which was not available, and no context was fabricated.

## Review and scope

Independent exact-tip review is recorded in [INDEPENDENT_REVIEW.md](INDEPENDENT_REVIEW.md). This candidate does not promote Core canonical, close CORE-F, or claim a standalone .NET host or full independent runtime composition.