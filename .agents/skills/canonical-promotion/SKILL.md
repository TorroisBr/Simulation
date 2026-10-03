---
name: canonical-promotion
description: Promote an independently validated, in-scope simulation checkpoint candidate to its named canonical branch after full integration checks and autonomous-promotion preflight.
---

# Canonical promotion

Use when an independently validated candidate is ready for canonical consideration. Complete the refreshed preflight without asking about routine Git mechanics. If every `AUTONOMOUS_BOUNDED_PROMOTION` condition in `docs/EXECUTION_MODEL.md` passes, promotion is authorized and must proceed without a separate human approval. Stop only for a failed preflight, genuine human gate, or unresolved product/canonical architecture decision.

1. Verify named canonical branch, local/remote HEAD, candidate SHA/base/ancestry, clean relevant worktrees, and all upstream changes. Refresh or reintegrate the candidate when required; do not silently promote an outdated candidate.
2. Confirm checkpoint scope, required design approval, independent exact-tip review, blockers, integration order, and all risk-appropriate validation including `git diff --check`. Ensure the review and tests cover the exact proposed promotion tip.
3. Prepare the factual State update with full SHAs, validation/review evidence, and known limits. When the bounded-promotion criteria pass, promote additively under the normal Git workflow, update State, push the intended canonical branch, and verify local/remote synchronization.
4. Immediately refresh the full dependency DAG and continue newly READY work. Request human action only for a genuine gate retained by `AGENTS.md` and `docs/EXECUTION_MODEL.md`.

Output prior/final full SHAs, files and integration path, validation/review evidence, State update, remote synchronization, and the refreshed DAG. Stop if required evidence, authority, tests, or remote consistency is missing, or if a genuine human gate remains. Do not silently broaden scope to later checkpoints.
