# CORE-F P12-D NPC Core Hydration — Independent Exact-Tip Review

**Verdict: `VALIDATED_CANDIDATE`**

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Exact candidate tip: `6f25db05ee22fc32ebe3d48bdd52aad50f7421da`
- Candidate tree: `c472ae36598d749b112350624fabd5a93fde7644`
- Base: `d14e7047f97b8a9572d5b37a05dc50f94a3c6e09`
- Code implementation commit: `5f2a9d25cb3c46586b92ce7b08f5ebbf4b38b31d`
- Code implementation tree: `b311d8089bdcd1824c6edd013d6a804929641ad6`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The complete base-to-tip diff was reviewed. The implementation is the `d14e7047..5f2a9d25` source/test change. The following commits add the validation bundle and then correct its recorded public package-manifest hash; the latter changes only `VALIDATION.md`. The implementation and test source tree remains `b311d808`.

## Implementation findings

No actionable implementation findings remain.

- The Core batch decoder obtains one validated runtime-identity snapshot and one decoded PersonStore snapshot for the image path. The existing Unity image decoder first validates the complete image census, decodes those two snapshots, then passes them to the Core batch decoder and places its results in the detached image-owner snapshot. The change does not add a second owner authority or claim that the full Unity restoration path is Core-owned.
- NPC runtime identities are validated as nonblank and unique, then sorted with `StringComparer.Ordinal`, preserving the previous image decoder order. Each row is decoded against the exact runtime identity; owner section IDs, schema version, row keys, revisions, bound Person identity, and duplicated Person life/residence facts are checked by the existing per-NPC codecs.
- The decoder stages D/F row pairs in local lists and returns read-only lists only after every NPC succeeds. Failure leaves both output lists null, so a later invalid NPC cannot publish partial rows. The added batch test verifies ordinal output and a later missing-owner failure with no partial output.
- The selected profile contract requires at least one NPC and City, so rejecting an empty NPC batch is consistent with the admitted profile. This does not establish support for a zero-NPC profile.
- The fixed 22 section-family identities, optional non-Person residence rule, aliases, and schema-v1 values remain consistent with the existing P12 census contract. The moved key encoder is byte-for-byte equivalent in behavior to the former `SpatialStableKey` wrapper, which delegates to `StableKeyEncoding`.
- The detached F-row and commercial-share-receipt value types preserve the existing captured fields. Receipt construction applies the same validation and operation-identity encoding as the former owner receipt factory; hydration reconstructs the owner receipt before installing the commercial owner. No P12 section wire layout change was found.
- Test edits add the batch behavior test and update types/section helpers for the Core move. I found no weakened assertions in the reviewed test diff.

## Validation and evidence inspected

I did not rerun Unity or .NET tests. I inspected the exact-source manifests and supporting reports:

- CoreValidation run `a1fde1b294684c63b55f3891a94b9663`: private runner manifest SHA-256 `a31440613fdbe145739187277e1d4a5d722ada14ef723a0898d0489f8e766bfb`; source commit/tree `5f2a9d25` / `b311d808`; stable input digest `47571def1125e2f6363d651b131fe65bc94a4a3e30c0c5e2e8117d302c01f3f2`. The manifest records 783/783 cold and warm. I verified the protected TRX bytes against their recorded hashes: cold `574a697e6ef3d57c1fa755cb741639e56538c0e6eb33c4c5524aee1b7d6efd78`, warm `184b710afeb1f0b8b377d8f6742d827813614133e61e6a3b9f9d0e0ab2c1ec37`; each report contains 783 passed results and no failed results. The recorded worktree changes are unrelated ProjectSettings and temporary/evidence files; no implementation or test source appears in that list.
- Unity EditMode run `50f2596df0c44ccaaec7525bd8f9bb73`: exact source commit/tree `5f2a9d25` / `b311d808`; 3264/3264 passed. Public manifest file SHA-256 `48df36665280f358577de6fa71d8cff5bf5cde0dd9aea9a7e15c052b1abda495`; package archive SHA-256 `9153fe0e7b22817de49c7bd8a0100c6e70f1cc8eeefdf986026d30e219df54f8`. The archive contains only the normalized XML; its entry hash `077c9213f02fcd2b5a23b1ad9bbf549cb011d6616b68bc38b833ae5d8d17515d` matches the manifest, which records 3264 passed and zero failed, skipped, or inconclusive.
- Official filtered Smoke run `b8d97f024b524b1eb991e0266be72a63`: 5/5 passed, exact source commit/tree `5f2a9d25` / `b311d808`, stable before/after snapshot. I verified the raw result hash `984581d940af3bc8c0bc83e9aa2c95d5c3e02129115cc25ff88039bddd7135e2` and confirmed all five passing case identities occur in the full 3264-case EditMode package. The earlier wrong-mode filter selected zero cases and is correctly excluded from validation claims.
- `git diff --check` passed for the implementation diff and the docs-only manifest-hash correction.

## Privacy, scope, and integration limits

The corrected `VALIDATION.md` cites the actual committed public package-manifest hash above. The previous `2f4f…` value referred to a different run manifest; that mismatch is corrected at the reviewed tip. I scanned the committed validation record, public manifest, and archived normalized XML: none contains a private workstation path. Original Core reports and logs remain in the protected evidence store and are not published.

This validates the bounded Core per-NPC codec and deterministic batch-decode stage only. It does not complete P12 image composition or restoration, close Phase 12 or CORE-F, establish P12-A readiness, deliver the detached .NET host, or promote Core canonical. Those remain subject to their own contracts and gates.
