---
name: phase-closure
description: Formally close an approved simulation Phase after its mandatory checkpoints are canonical, validated, and independently reviewed; preserve deferred consumers.
---

# Phase closure

Use only when the user has authorized a closure review or formal closure. The last checkpoint promotion does not itself close the Phase; human closure approval is required initially.

1. Verify named canonical branch, exact local/remote baseline and current Brief/State. Read current architecture and applicable evidence; stop on unexpected advancement.
2. Check the Phase objective and all mandatory checkpoints against canonical code and State, including integration/regression results, open findings and whether known limitations invalidate closure. Keep deferred future consumers separate from required work.
3. Obtain independent closure review and explicit human approval. Do not invent a missing feature to make closure look complete or erase historical State records.
4. If authorized, record formal `COMPLETED` status, closure SHA/evidence, known limitations and deferred consumers in the Phase State. A documentation-only closure commit is allowed when appropriate; run `git diff --check`, commit/push under the approved canonical workflow and verify synchronization.

Output closure verdict, remaining REQUIRED versus OPTIONAL/DEFERRED items, review evidence, files changed and final SHA. Stop on any unmet required checkpoint, unresolved blocking mismatch or missing approval. Do not start the next Phase by virtue of closure.
