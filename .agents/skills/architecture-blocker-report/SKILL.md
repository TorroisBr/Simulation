---
name: architecture-blocker-report
description: Report an unresolved simulation checkpoint architecture or product decision without choosing the answer; keep independent work schedulable.
---

# Architecture or product blocker report

Use when design, implementation or review encounters a durable decision not settled by current repository authority. This skill reports; it does not alter the architecture or product scope.

1. Verify checkpoint and canonical baseline SHA; reread current `docs/SIMULATION_ARCHITECTURE.md`, owning Brief/State and relevant code before declaring a gap.
2. Classify `BLOCKED_ARCHITECTURE` for missing semantic contract, `BLOCKED_PRODUCT_DECISION` for missing user intent, or an ordinary technical issue if existing authority already answers it.
3. State the exact question, why current authority does not answer, concrete options/consequences, affected invariants, downstream work blocked, safe unrelated work, and the minimum decision needed. Preserve the candidate/worktree state for resumption.
4. Route an architecture question to Architecture Lab and product intent to the user. After an approved/consolidated change, request dependency refresh before resuming the track.

Output a concise blocker record with checkpoint, branch/base SHA and owner. Do not choose an option, rewrite `SIMULATION_ARCHITECTURE.md`, start a replacement implementation, or pause unrelated READY tracks. Write a record only in an explicitly authorized candidate/process location; otherwise report in the task.
