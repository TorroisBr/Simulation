---
name: phase-closure
description: Formally close an approved simulation Phase after its mandatory checkpoints are canonical, validated, and independently reviewed; preserve deferred consumers.
---

# Phase closure

Use when a Phase appears to have delivered its mandatory checkpoints. Prepare a concrete closure candidate and independent review without conflating the last checkpoint promotion with closure. Formal Phase closure approval remains a separate human gate where required by the current `AGENTS.md` and `docs/EXECUTION_MODEL.md`.

1. Verify canonical branch and local/remote baseline; read current architecture, Brief/State, and applicable evidence. Refresh if the baseline advanced.
2. Check the Phase objective and every mandatory accepted checkpoint against canonical code and State, including reviews, integration/regression results, open findings, and limitations. Preserve explicit scope boundaries and deferred consumers; do not add gameplay or erase historical evidence to make closure appear complete.
3. Obtain an independent closure review. Prepare a docs/State-only closure candidate recording the factual verdict, exact evidence, known limitations, and deferred work. Present the concrete record at the formal closure gate and stop until approval.
4. After approval, record formal `COMPLETED` status and closure SHA/evidence, run `git diff --check`, commit/push under the approved workflow, verify synchronization, and refresh the DAG. Closure does not itself authorize unrelated roadmap implementation.

Output closure verdict, remaining REQUIRED versus OPTIONAL/DEFERRED items, review evidence, files changed, final SHA, and refreshed readiness. Stop on any unmet required checkpoint, unresolved blocking mismatch, or missing approval.
