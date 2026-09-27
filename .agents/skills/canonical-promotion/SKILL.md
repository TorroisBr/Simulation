---
name: canonical-promotion
description: Promote an independently validated simulation checkpoint candidate to its named canonical branch after explicit human approval and full integration checks.
---

# Canonical promotion

Use when an independently validated candidate is ready for canonical consideration. Prepare the complete preflight without asking about routine Git mechanics. Canonical promotion still requires the formal human approval defined in the current `AGENTS.md` and `docs/EXECUTION_MODEL.md`; this skill does not grant it.

1. Verify named canonical branch, local/remote HEAD, candidate SHA/base/ancestry, clean relevant worktrees, and all upstream changes. Refresh or reintegrate the candidate when required; do not silently promote an outdated candidate.
2. Confirm checkpoint scope, required design approval, independent exact-tip review, blockers, integration order, and all risk-appropriate validation including `git diff --check`. Ensure the review and tests cover the exact proposed promotion tip.
3. Prepare a factual State update with full SHAs, validation/review evidence, and known limits. Present the concrete candidate, diff/integration path, evidence, and required formal approval as the sole human gate. Stop before promotion until approval arrives.
4. After approval, promote additively under the approved Git workflow, update State, push the intended canonical branch, and verify local/remote synchronization. Immediately refresh the full dependency DAG and continue newly READY work.

Output prior/final full SHAs, files and integration path, validation/review evidence, State update, remote synchronization, and the refreshed DAG. Stop if approval, evidence, authority, tests, or remote consistency is missing. Do not silently broaden scope to later checkpoints.
