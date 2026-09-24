---
name: candidate-review
description: Independently review a submitted simulation checkpoint candidate against its actual base, architecture, technical design, tests, and replay-sensitive boundaries.
---

# Candidate review

Use after a worker submits a bounded candidate. Reviewer must be independent of the candidate's author and must not edit the candidate while reviewing.

1. Verify candidate branch/SHA, exact base commit, current canonical SHA and whether upstream has advanced. Read current architecture, owning Brief/State, checkpoint contract and reviewed technical design if one exists.
2. Inspect the complete diff against the real base, not a convenient branch comparison. Check scope, semantic authority, mutation/stale/atomic behavior, deterministic ordering/randomness, knowledge boundary, reconstruction-sensitive state/inputs, hotspot overlap and test coverage.
3. Check focused and affected regression results, but do not infer correctness solely from green tests. Identify whether a change requires revalidation or reintegration against newer canonical.

Return `REJECTED`, `NEEDS_CHANGES`, or `VALIDATED_CANDIDATE` with severity, precise evidence, base/current SHAs, test gaps and integration constraints. A validated candidate is not human approval or canonical promotion. Stop and report unresolved architecture/product decisions instead of resolving them in review.
