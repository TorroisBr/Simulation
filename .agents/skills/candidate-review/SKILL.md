---
name: candidate-review
description: Independently review a submitted simulation checkpoint candidate against its actual base, architecture, technical design, tests, and replay-sensitive boundaries.
---

# Candidate review

Use after a worker submits a bounded candidate or integration candidate. Reviewer must be independent of the candidate's author and must not edit the candidate while reviewing.

1. Verify the full candidate SHA, exact base, branch, current canonical SHA, clean checkout, and whether the candidate tip changed after tests or earlier review. Read current architecture, owning Brief/State, checkpoint contract, reviewed technical design, and required validation gates.
2. Inspect the complete diff against the actual base, not a convenient branch comparison. Check scope, authority, mutation/stale/atomic behavior, deterministic ordering/randomness, knowledge boundary, reconstruction-sensitive state/inputs, temporal identity/cardinality assumptions where applicable, hotspot overlap, and test coverage.
3. Verify validation evidence belongs to this exact tip and includes the required focused, regression, full-suite, Smoke, and diff-check gates. Do not infer correctness solely from green tests. A changed code tip requires affected validation and a fresh review; classify newer-canonical impact using `dependency-refresh`.

Record the review result durably, with `REJECTED`, `NEEDS_CHANGES`, or `VALIDATED_CANDIDATE`, precise evidence, full base/candidate/current-canonical SHAs, test gaps, and integration constraints. `VALIDATED_CANDIDATE` alone does not promote code: the separate canonical-promotion workflow must still verify every condition in the autonomous bounded-promotion policy or surface a genuine human gate. Stop and report unresolved architecture/product decisions instead of resolving them in review.
