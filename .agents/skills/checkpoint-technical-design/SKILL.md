---
name: checkpoint-technical-design
description: Prepare and review a bounded technical design for an architecture-ready simulation checkpoint before implementation; stop at unresolved semantic or product questions.
---

# Checkpoint technical design

Use when a checkpoint has approved semantic scope and closure but lacks an implementation boundary. This workflow does not implement the checkpoint.

1. Verify the named canonical branch, exact local/remote baseline and clean target worktree. Read current architecture, roadmap, execution model, owning Brief/State and relevant code/tests. Confirm hard contract gates; do not presume code capability from architecture text.
2. Define concrete owner(s), affected components/files, implementation-level interfaces, migration/integration sequence, test strategy, hotspot separation and replay/fork-sensitive state. Keep the design as small as the checkpoint permits; do not require a permanent file for a tiny reviewed plan.
3. Record checkpoint ID, baseline SHA, assumptions, exclusions, review evidence and dependencies in a bounded artifact or checkpoint execution record when writing is authorized. Technical design does not amend `SIMULATION_ARCHITECTURE.md` or promote a candidate.
4. Obtain independent technical review proportionate to risk. Only a reviewed design with still-valid architecture assumptions may be marked ready for implementation.

If a durable semantic answer is missing, output `BLOCKED_ARCHITECTURE`; if user intent is missing, output `BLOCKED_PRODUCT_DECISION`. If canonical advances, classify the design's freshness before handoff. Report design location or reviewed plan, base, review verdict, implementation readiness and remaining gates. Never implement merely because design passed.
