# Core-only Merchant Knowledge admission review fix

## Candidate

- Implementation commit: `56f0767e8d65ff22fa0a542b339c0746f72dbf49`
- Parent candidate: `59b34260a6a3953cbbcac59144632fec158f2007`
- Implementation tree: `86cc877645af3eff1417545a2abd6d339db41d0e`
- Production source SHA-256: `7dfb07da033c29536057e1ac38b00ad732fe8ec2fa5750c5d07d20c1e8afbbd6`
- Test source SHA-256: `bc312c416360e1911eba2ffcc47bd946e491b7a7968e4241b55caa9b7d375f12`

The final two source hashes match the files exercised by the validation runs. The new production behavior faults the selected census protocol and invalidates the current capture token when a bound, changed owner write is refused by exact Core admission. Invalid and no-op owner writes remain rejected before this admission callback.

## Validation

- Focused `CoreOnlyActorTurnExecutesAdmittedActionWithCompleteP12Census`: 10/10 passed.
- Official CoreValidation: cold 1172/1172 and warm 1172/1172 passed. Run `8ace6d853bb24078b1e5129368b12266`; input digest `ee20ae01eb8cd07cfda82a818d14e2a85bed00f013931f099fcd10176bb18d16`; protected runner manifest SHA-256 `defe7914ac2c717eac3a613dde6058d660d6ecc994b9e998e666c407253eb86e`.
- Original cold TRX SHA-256: `82049672d03fed75681eb3b74b8f43b45464dbc1e01d3f3b44863625ff0ab5ca`; warm TRX SHA-256: `596d95f7727474a73dbafb2709580de211585c5f175703929fb539e62647394e`.
- Official Unity EditMode: 3325/3325 passed, zero failures, skips, or inconclusive cases. Run `8d8438fd8eac4c239e84c02ba413dc1f`; input digest `6f1c42a56e12a8fda9570baaa25abb7547aa8cc73a12fc90fd09bce54e5cc117`; protected runner manifest SHA-256 `b682ff353f16863a5a58eb7b397e17a44f8dd63210bd3a04cd134f4d1efe0d04`.
- Official Smoke filter: 5/5 passed, zero failures. Run `b41f0d3a31b54726900f2f7cfcdcd8f8`; it used the same Unity input digest. The filtered report is not packaged separately because its NUnit XML root count does not satisfy the approved result-only packager’s shape requirements; the full EditMode report includes the Smoke cases.
- `git diff --check`: passed.

The sanitized Unity result-only package is `Unity/coref-local-knowledge-reviewfix-editmode.zip` (SHA-256 `54a0b62bf13ec83220958de87768e37b59d098cc0682232f9745609950327cff`). Its package manifest SHA-256 is `f3ce5b9f4e735984cd9c672a9b05a74a5ed9874ad733ab5eed3a4ff98047ab1c`. The approved verifier accepted the package against the original run manifest.

Original .NET TRX reports and runner logs remain in protected local evidence storage and are not included in this repository. The approved .NET TRX packaging adapter does not accept the current CoreValidation manifest’s source-context schema. No report was rewritten, normalized by hand, or published without that provenance. The protected runner manifest and report hashes above preserve their identities for authorized local review.

## Independent exact-tip review

Two read-only reviewers returned PASS on implementation commit `56f0767e8d65ff22fa0a542b339c0746f72dbf49`:

- The successful nested Merchant Knowledge mutation was exercised through `CommercialKnowledge.RecordObservation` inside the real Core Merchant operation, with exact sibling census identities/cardinalities, shared revision, census validation, and one epoch by operation close.
- The wrong-thread changed write was refused before fact, revision, or epoch changes; the census was faulted, the existing capture token invalidated, and capture remained rejected after operation teardown. Invalid and no-op paths remain before the callback.
- Existing assertions were preserved; no unrelated scope or assertion weakening was found.

The reviewers performed static exact-tip review and did not rerun validation. CORE-F remains incomplete and this candidate is not a Core-canonical promotion.
