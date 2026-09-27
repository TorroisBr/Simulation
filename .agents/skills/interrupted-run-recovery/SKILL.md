---
name: interrupted-run-recovery
description: Resume roadmap orchestration after a task or machine interruption from durable Git, review, and validation evidence without repeating unchanged completed work.
---

# Interrupted run recovery

Use when orchestration stops before its intended objective is complete. Repository history and exact-tip evidence are the recovery boundary.

1. Fetch refs and inspect canonical local/remote tips, candidate/integration branches, worktrees, task status, and checkout changes. Read `AGENTS.md` and current `docs/EXECUTION_MODEL.md`, architecture, Roadmap, and affected Phase Briefs/States.
2. Separate durable commits/pushed work and completed independent reviews/tests from uncommitted, interrupted, or otherwise unverified work. Preserve unrelated changes. Do not redo a review or test when its exact candidate tip and relevant contract are unchanged.
3. Identify work interrupted before a durable commit or review result. Recover it from its existing branch/worktree when safe; otherwise resume from the last committed boundary. Do not discard a candidate solely because the previous process ended.
4. Apply `dependency-refresh` to reconcile new canonical/docs changes, revalidate or reintegrate only where required, rebuild the complete DAG, and resume every safe READY track. Preserve all formal human gates.
5. Record the recovered baseline, retained evidence, interrupted work, and next actions when the Execution Model or active workflow requires a durable status update.

Escalate only an unrecoverable repository state, required runtime permission, or genuine unresolved product/canonical architecture decision. Routine branch, worktree, commit, push, and validation recovery is autonomous within standing authority.
