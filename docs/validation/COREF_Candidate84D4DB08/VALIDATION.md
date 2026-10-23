# CORE-F descriptor-correction candidate validation

This record supplements the initial exact-tip review
[`CORE_F_Candidate_84D4DB08_ExactTip_Review.md`](../../reviews/CORE_F_Candidate_84D4DB08_ExactTip_Review.md).
It does not claim completion of the CORE-F application/host package or
authorize canonical promotion.

## Source identity

| Item | Identity |
|---|---|
| Candidate branch | `codex/core/CORE-F-core-owned-runtime-application` |
| Code candidate | `84d4db084f5aa3c7ba8ebff66aef4459226d8297` |
| Code tree | `3076e601b1cd49685470fd0abadba42435b490a1` |
| Validation checkout | `eb3de113e88f6ce00fdeef5b345a7dd9c4b9adee` |
| Validation checkout tree | `a02ffbe324920a7e46a2c57f06c6cd3e67f521d5` |
| Delta from code candidate | Documentation-only exact-tip review record; no Assets, Packages, ProjectSettings, or validation-tool inputs changed. |

## Exact-source Unity validation

| Gate | Run | Result |
|---|---|---|
| Full EditMode | `ac39786dedb145e9ae1a298d9d6f2087` | **3255/3255 PASS**; 0 failed, skipped, or inconclusive |
| Input stability | Same run | Before/after validation input snapshot stable |
| Evidence package | `coref-editmode-ac39786dedb145e9ae1a298d9d6f2087.zip` | Packaged and verified by the approved Unity evidence packager |
| `git diff --check` | Candidate documentation package | PASS |

The package archive SHA-256 is
`e4d22a58bb58fae75996d0ec5be83a9e5149fb90cccf37e4543dc0e1afaf3d20`
(172,265 bytes). Its manifest SHA-256 is
`850722778845d8accc6e42791a0eb57de1bf0a1fdaa6432e30dda0f7f8b4f005`
(437,504 bytes). The original XML SHA-256 is
`839b026ae5496da53791a9dd58b3bcc661b5aceda54595ab731c4f709c4dab95`;
the normalized XML SHA-256 is
`2af2e85aef3b70df0125b95c984a1a2a80be1e6d43b4cbb3f5596a8bde6d2df8`.
The complete public package and manifest are retained beside this record.
Original XML and raw logs remain in protected local retention and were not
modified.

All five established Smoke test identities occur in the complete EditMode
package and pass. A separate filtered 39/39 bootstrap run also passed, but its
raw XML reports the unfiltered 3255-test aggregate in parent-suite metadata;
the approved packager correctly rejected that filtered XML. It remains private
and is not counted as a packageable result. No report was edited and the
packager was not weakened.

## .NET and review evidence

CoreValidation cold and warm each passed **674/674** (run
`9ad39ab4ac50490e90284843ef4765fa`). The independent exact-tip reviewer
verified all 342 manifest inputs, 3 generated dependencies, and report hashes
against the candidate source tree. The earlier focused Unity failure was
environmental; this later full EditMode run was made after Unity licensing was
restored and supersedes that missing gate for this candidate's Unity regression
evidence.

The initial independent exact-tip review found no code defects. Its durable
verdict is recorded in the linked review document. A separate R1 evidence
re-review is pending for this full Unity package. Neither review nor these
results establishes full CORE-F application/host acceptance, detached P12
continuation, .NET T/T+1/T+2 parity, or canonical promotion.
