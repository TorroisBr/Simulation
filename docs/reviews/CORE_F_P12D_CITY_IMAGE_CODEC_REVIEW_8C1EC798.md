# Independent Review — CORE-F P12-D City Image Codec

**Verdict: PASS — exact-tip implementation review; no actionable findings.**

## Reviewed identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Review/evidence tip: `39b29f81b40b01b62ebeb0c872b347c637e4512d`
- Reviewed code commit: `8c1ec798880c64048df0c7bc9d421212b5006560`
- Reviewed code tree: `d29ca2830238c8442962d9be331e07f3e2a6c0bb`
- Base: `6f25db05ee22fc32ebe3d48bdd52aad50f7421da`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The evidence tip is a docs-only child of the reviewed code commit.

## Review result

The independent reviewer inspected the complete base-to-code change, including `P12DCityOwnerImageDecoder`, its Unity adapters, central City census constants, tests, and the validation package/record. The Core codec retains the `p12a.city-owner/v1` payload and schema 1, existing four section identities, row payload ordering, revisions, required roles, alias identity, and previous City-set ordering. Decode validates exact section IDs/schema/aliases, row identity/cardinality, and revisions. The dynamic set decoder rejects duplicate, missing, extra, or unconsumed sections, sorts City IDs ordinally, and assigns the output only after the entire set succeeds. Unity methods convert between Core values and the existing Unity snapshot type; they do not retain a second codec implementation.

The new Core tests cover round-trip identities/values/revisions/aliases, deterministic City ordering, null output on incomplete set, and alias drift rejection. Existing Unity City set and root-owner regression tests remain in the full EditMode suite. No actionable wire compatibility, authority boundary, identity, ordering, failure atomicity, or test-integrity finding remains.

## Validation and evidence reviewed

- CoreValidation run `567ed30981664973ada3b2a3464f471f`: 786/786 cold and 786/786 warm PASS; locked build, CalendarDemo, and Lab gates passed.
- Focused Unity EditMode: `P12ACityOwnerSetCodecTests` 2/2 PASS; `P12DCityRootOwnerSnapshotTests` 18/18 PASS.
- Full Unity EditMode run `baac14cf9ec54f87800f4d9fc7c186ac`: 3264/3264 PASS.
- Official filtered Smoke run `ab2c14dad18b4204b5eb198d915504eb`: 5/5 PASS. The standalone filtered package was rejected by the approved packager because the aggregate `Simulation` suite count did not match descendants; the original remains protected. The record states the five identities/outcomes were verified in the packaged full EditMode report and does not claim a separately published Smoke package.
- Full EditMode package `p12d-city-image-codec-all-editmode.zip`: 172,436 bytes, SHA-256 `e24787baf7fb751e41b0bc1050f0b35ed3cc5d45dd30d8cd7c1e5f6a801b856d`. The reviewer independently hashed the archive bytes and the hash matched. Public manifest SHA-256 `d97af5a630242b869eec16b664472ab4812303bbc825ff88b6098e903ac83231` matched when computed over its recorded CRLF bytes. The manifest contains relative paths/placeholders and no private workstation paths.
- The public Unity manifest's HEAD/tree fields identify the pre-commit base because validation ran from the dirty worktree. The protected runner input manifest was cross-checked against the exact precommit Windows worktree source bytes; after commit, those inputs are represented by reviewed code commit `8c1ec798`/tree `d29ca283` (implementation-owner crosswalk).
- No tests were rerun for this review.

## Scope boundary

This review covers only the P12-D City image codec/set-decoder extraction. It does not claim full P12 restoration, P12-D completion, application/host completion, Core canonical promotion, or CORE-F completion.