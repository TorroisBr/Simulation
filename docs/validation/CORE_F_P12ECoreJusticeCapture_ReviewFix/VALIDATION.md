# CORE-F P12-E Core Justice capture review fix

## Scope

This record closes the exact test-coverage finding from the independent review
of candidate `38f4bbe815d96ef595a1d0797c32f24395cf1bc8` (implementation
`0893555a063ec8848264150ffd70c2a067006e0a`, Core canonical
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`). The reviewer requested a populated
selected-Core capture case with a Justice target reciprocally bound to a
materialized Person. Production code was not changed. The Unity integration
fixture now registers and binds the target Person, then asserts that the exact
PersonId is captured in both its wanted row and sentence row. All changes are
additive; prior assertions remain intact.

The changed test source SHA-256 is
`5132e3ac13b530a2e3b7f5ac91283bcf5a67473f469b57a7f1de662ab568a532`. The
implementation source remains the reviewed code commit above. This is
validation evidence for the review fix, not a promotion or Phase closure.

## Validation

The official Unity 6000.3.9f1 runner used PowerShell 7.6.5 / .NET 10.0.11 and
the same stable validation-input digest for all runs:
`2a47a8bcf356796bfd171495bef521a7cdfdb4e3795ed0f71d8e55333b9b4d1b`.

| Run | Result | Protected run-manifest SHA-256 |
|---|---:|---|
| Focused `SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances` | 1/1 passed | `da199b7e2ed4760d09aaa36694942653aeefcde173d26f4dd300a6dbbb6d25d1` |
| Full EditMode | 3279/3279 passed; 0 failed, skipped, or inconclusive | `9b8496164d925fa142fc81991bead890b996ee9a1a2f3dc184d31f33b3a4b90e` |
| Official Smoke filter | 5/5 passed; 0 failed, skipped, or inconclusive | `83c469af06ebd9c2d8a32227ca1fe3755b894d199a8b29f55c1a43f915dc4ca9` |

All runs reported stable inputs. The full EditMode public package, which
contains the changed test and the Smoke cases, is at `Evidence/`. Its manifest
SHA-256 is
`3dbe73bef1718877d06e993b5731afd41728bf8671a78a2ab47a81af98fa48ba`; its ZIP
SHA-256 is
`2ea04b2f7ca356700d9547ec5b4344a2762937f046e0449eb73580ffac9da82d` (174062
bytes). The approved packager's private original-to-normalized verification
passed (`runs=1`, `entries=1`). The focused and filtered Smoke XMLs remain in
protected retention; the packager rejected those filtered reports because the
Unity `Simulation` suite's declared testcase count did not match its filtered
descendants. No XML was edited or reinterpreted. The complete EditMode package
was packaged and verified successfully.

`git diff --check` passed. The previously recorded cold/warm CoreValidation
936/936 remains tied to unchanged Core implementation and test sources; this
review fix changes only a Unity EditMode fixture.

## Review and status

The prior exact-tip review is durably recorded as NEEDS_CHANGES at
`b977df21e86a636b88e9d10d2cef084537702be5` on
`codex/review/coref-p12e-justice-core-capture-38f4bbe8`. It found no production
defect and requested the Person-backed selected-Core integration case now
covered above. A fresh exact-tip review is required after this test change.

P12-E Core Justice capture remains a bounded capture integration slice. This
record does not claim P12-E completion, complete CORE-F delivery, canonical
promotion, or Phase closure.
