# CORE-F initial publication NoSave admission gate

Date: 2026-10-27

## Candidate identity

- Base: `2a97c2fe3271b4c7249283594c19ba8fd20bb3c3`
- Code candidate: `b86a3c9a0b4942d6edf49c21692baf8b9f41143f`
- Committed code tree: `c47b5e54a5cf4a655b1dd9423541a22845270ce6`
- Scope: prevent the direct initial-publication path from bypassing successful NoSave proof; preserve the prepared-genesis and restored-publication paths.
- This is a bounded CORE-F candidate checkpoint. It does not complete or promote CORE-F.

## Validation

- Focused `DailyV1ApplicationSessionGateTests`: 54/54 passed.
- Official CoreValidation: 902/902 cold and 902/902 warm passed; runner recorded the candidate commit and tree above, stable source inputs, and zero command exit codes. Run identity: `de4dc72b2c59446586c71f44e9639e33`.
  - Original protected runner-manifest SHA-256: `3aebc6925bf930898b8b09e7b4e8dce5a96858801494c09147f48e1b44645561`.
  - Original cold TRX SHA-256: `828498aa3c751e93734566ba9e32154dc4d00e8392335b1103094c1f3d09caf9`.
  - Original warm TRX SHA-256: `6bc8192fc474fdd520b1ff01fd35f07aa0410b3ea19bc259d1de40dc0d00a013`.
- Official Unity EditMode: 3274/3274 passed; zero failed, skipped, or inconclusive. Official Smoke filter: 5/5 passed. Both ran at the exact candidate commit/tree.
- Unity EditMode public package: `coref-initial-publish-editmode-b86a3c9`; package archive SHA-256 `4bc4235495497261d1364ad0f7951e545bcef35be0fe370bb7be3494339a4e4d`; package manifest SHA-256 `a6debd11510e0044459e7cfb0432ad8e0ae80fde0989e52331908f5841c44808`.
- The official Unity package verifier passed. `git diff --check` passed for the candidate source changes.

## Evidence boundary

The original CoreValidation manifest, cold/warm TRX reports, and command logs remain unchanged in protected local retention. No public DotNetTrx package or original-to-normalized TRX comparison is claimed: this CoreValidation run emitted `simulation-core-validation/v1`, while the approved packager requires a separate, complete `dotnet-trx-source-context-v1` context. Passing the runner manifest directly would not satisfy that contract, and no context was fabricated. The original evidence hashes above identify the retained inputs without publishing their local locations or contents.

The EditMode package is the normalized report artifact only. Its original Unity run manifest and raw report remain under the approved protected evidence retention. The separately filtered Smoke result is retained privately and is not represented as a second package.

## Scope exclusions and working-tree preservation

Only `DailyV1Application.cs` and `DailyV1ApplicationSessionGateTests.cs` are in the code candidate delta. Unity-generated local `ProjectSettings` changes are excluded and were not staged. CORE-F remains incomplete and unpromoted.
