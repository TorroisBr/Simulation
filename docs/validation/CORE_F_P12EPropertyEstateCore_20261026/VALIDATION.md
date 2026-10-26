# CORE-F P12-E Property/Estate Core source boundary

Status: validated candidate checkpoint only. It is not a P12-E completion, CORE-F acceptance, or Core-canonical promotion.

## Source identity

- Base: 71ccb1bb06215c4f068bd0da8676064100e08c7d (37973fd0535a432e09aa7bd5f60c067f78ef0e9c tree).
- Candidate code commit: 12f96489110d093eb549f084c3b13d2c04316c85.
- Candidate tree: 390773692e7d33f583a20d50fc9f52bd4e745761.
- Core canonical remains a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Scope: byte-identical moves of Estate contracts/store, property and estate census providers, P12-E owner snapshot, and codec into Packages/com.simulation.core; original Unity .meta GUIDs are preserved. Six explicit Core SDK compile entries and the existing P12-E codec tests are registered in CoreValidation. No domain implementation logic changed.

## Validation

- Official CoreValidation: 842/842 cold, 842/842 warm, manifest simulation-core-validation/v1, status Passed, SDK 9.0.315. All 469 captured source inputs still match the worktree after the candidate commit; input digest 36e458730c435875de5b0d0131991809ce1ef9f39f4292ce18e3bfd653f8df99.
- Original CoreValidation manifest SHA-256: 98833896f36ca2d1bba3f0a9bff7b2bc6585760fa304202083885a660d6afc2b. Cold TRX SHA-256: 0565e60ee4a6c7d1fb7f3f291fdf1642525b60d680470197f7b6c5a25e9774e7; warm TRX SHA-256: b8b160bf20daea53569b39206235e6f34e537ab42be0ba5955b321dddf05b67c. Original reports, logs, runtime traces, and manifest remain in the protected local evidence store. They were not rewritten or included in the repository.
- Official Unity EditMode: 3271/3271 passed, Unity 6000.3.9f1, inputStable=true. All 1,928 captured validation inputs still match the worktree. Original XML SHA-256: a2250f1f4791c92721902a7d6929a1bfbebc21d21633f3b6c194d9428141431f.
- The result-only EditMode package is Evidence/coref-p12e-property-estate-editmode-20261026.zip; SHA-256 9cbf95a3e93613a6614095a98fa3a21293482ce10405e788da888361dc609218, 173,127 bytes. Its manifest is Evidence/coref-p12e-property-estate-editmode-20261026.manifest.json; SHA-256 27f7eef74a28c7f4607c990d9fb5bfe1527344ee423ae7ce01ae8d8c1a4319d0. The approved packager verified the archive and its result claims against the original protected run manifest.
- Official EditMode Smoke-filter run: 5/5 passed; original XML SHA-256 828c104e1d7f089b1fba061911f9cae5e0e07ed24cf12287db916c972c4a0bce. The packager correctly refused this filtered XML because its root suite declares testcasecount=3271 while only five selected descendants are present. The original remains unchanged in protected local evidence. The five selected case identities and passing outcomes are present in the packaged full EditMode report; this was cross-checked against the original filtered report. No standalone Smoke package is claimed.
- git diff --check passed for the candidate commit.

## Review and limits

Independent exact-tip implementation review: PASS with no actionable findings. See REVIEW.md. The candidate was published on its existing noncanonical branch. Unrelated user changes and protected local evidence were not staged or included. This source move does not establish detached P12-E restoration, full owner closure, DailyV1Application readiness, or CORE-F completion.
