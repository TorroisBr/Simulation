---
name: canonical-promotion
description: Promote an independently validated simulation checkpoint candidate to its named canonical branch after explicit human approval and full integration checks.
---

# Canonical promotion

Use only for an explicitly authorized promotion. Human approval is required under the initial multi-phase policy; invoking or discovering this skill does not grant it.

1. Verify named canonical branch, expected local/remote HEAD, candidate SHA/base/ancestry and clean relevant worktrees. Stop on unexpected advancement or unreviewed divergence.
2. Confirm owning Brief checkpoint scope/closure, approved architecture, reviewed technical design where required, independent candidate review, absence of unresolved blockers, expected diff, integration order and risk-appropriate focused/regression tests. Run `git diff --check` on the promoted candidate/integration.
3. Review the proposed Phase State update for factual delivery, exact SHAs, tests and known limitations. Candidate status does not count as promoted capability until the canonical branch and State reflect the validated result.
4. Under the approved Git workflow, promote without force-push, shared-history rewrite, main merge or discarded user work; push the intended canonical branch and verify local/remote synchronization. Recompute the dependency DAG only after promotion is confirmed.

Output prior/final full SHAs, files and integration path, validation/review evidence, State update, remote synchronization and newly eligible dependency refresh. Stop if approval, evidence, authority, tests or remote consistency is missing. Do not silently broaden scope to later checkpoints.
