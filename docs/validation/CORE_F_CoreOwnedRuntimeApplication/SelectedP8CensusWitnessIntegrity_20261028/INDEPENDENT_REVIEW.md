# Independent Exact-tip Review — Selected P8 Census Witness Integrity

## Verdict

**PASS.** No actionable findings.

## Reviewed identities

- Candidate commit: `24b39e0250ccf50a3fc43a6dd981a801b3274b87`
- Candidate tree: `dc021dc9cf20f0a37f4a29db9d3209fd994e17ff`
- Parent: `b1cf8ee121b3be049a7565bcf0a1e8212ef84fd5`

## Review findings

For each graph-bound P8-C owner, Core reads its existing passive provider witness and compares section ID, schema version, exact owner instance, cardinality, and revision to the supplied row. Duplicate rows reject. The complete-vector validation still runs first, and a stale/mismatched witness rejects with `OwnerCoverageIncomplete` while clearing both census and epoch outputs. The comparison runs after existing post-callback admission/quiescence checks on the bound owner thread.

Unbound P8 owners remain under the existing host census protocol. The 89-family reader and P12 scope are unchanged. Tests cover exact-vector acceptance, substituted identity, stale cardinality, stale revision, and incomplete-vector rejection; assertions were not weakened.

## Validation applicability

The exact source hashes match the validation record and saved private manifests. CoreValidation passed 918/918 cold and warm, Unity EditMode passed 3275/3275, official Smoke passed 5/5, all with stable inputs. `git diff --check` passed. These checks apply to this candidate tree; this review record itself is docs-only.

This review does not promote Core canonical, claim complete P8 census ownership, or close CORE-F.
