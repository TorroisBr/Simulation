# P12-B Crime/Social Appraisal Invalidation Implementation Review — R1

**Result:** `VALIDATED_CANDIDATE` — independent exact-tip implementation review PASS.

## Reviewed source

- Candidate branch: `codex/phase12/P12BCrimeSocialAppraisalInvalidationImplementation`
- P12 canonical base: `a00cba49f642c9b3203df838f9ba27d675f560b2`
- Current architecture baseline: `ffd75652d89d862b83d634868c560f8540869b89`
- Reviewed design: `7770119d7ae4dd69186ff6394946168db1473527`
- Corrected code candidate: `ec956c7247c39f8984f7c5a6bca3d813091a0047`
- Corrected code tree: `61956354146f3437396d93d91c4df246da8df89d`

The review covers the corrected code tip, not the later documentation-only
evidence update. The executable tree remains unchanged at the reviewed tip.

## Finding and correction

The initial review at `bb45f5025128f1aa192cb2a498ed1b887c741e65` identified a
post-commit failure-path defect: after TheftOutcome, Knowledge, and Reaction
rows had committed, a failed final census notification could make the
integration report rejection. `CrimeSystem` would then compensate the money
transfer while leaving those rows committed.

The corrected implementation preserves the accepted/recorded result when the
domain rows have committed, while the census protocol still faults closed on
the failed notification. Standalone Knowledge/Appraisal uses the same
post-commit result rule. The added regression injects a stale census witness
at final notification and verifies that the transfer and committed rows
remain, and that later census assessment reports `ProtocolFaulted`.

The independent reviewer confirmed this correction at the exact code tip and
reported no additional source defect. The base-to-code diff check is clean.

## Validation evidence

All validation below is for code tree
`61956354146f3437396d93d91c4df246da8df89d`; the source and test files were
unchanged after the runs and are committed at `ec956c7247c39f8984f7c5a6bca3d813091a0047`.

| Suite | Result |
|---|---:|
| `P12CrimeSocialAppraisalInvalidationTests` | 11/11 PASS |
| ALL EditMode | 2367/2367 PASS |
| Official Smoke | 5/5 PASS |
| `git diff --check` | PASS |

The six result artifacts are retained in
[`P12B-CrimeSocialAppraisal-validation-20261005-r1.zip`](../validation/P12B-CrimeSocialAppraisal-validation-20261005-r1.zip).
Its SHA-256 is
`7c13183315486d6753879b36002f42ffa962fea15e6c651ccb94f0e8f35e253f`.
The candidate manifest records the per-file XML and log hashes. An independent
follow-up verification confirmed that all six archive entries match those
hashes and that the XML results report the stated passing totals.

## Scope and limitations

This candidate adds census and invalidation coverage only for the existing
singleton TheftOutcome, CrimeKnowledge, and SocialReaction owners and their
reviewed direct/composite write paths. It adds no gameplay behavior, does not
change theft eligibility or amounts, and does not introduce a generalized
transaction framework.

It does not establish complete selected-profile owner or shared-epoch
coverage, global owner-thread/quiescence, capture eligibility, export,
hydration, save/load support, P12-A readiness, P12-B completion, or P13
readiness. Phase 12 remains open. This review does not promote the candidate.
