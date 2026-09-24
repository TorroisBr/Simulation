---
name: phase-checkpoint-implementation
description: Implement one explicitly authorized, implementation-ready simulation checkpoint in an isolated worktree and submit evidence; do not approve or promote your own work.
---

# Phase checkpoint implementation

Use only after a checkpoint has approved architecture/scope, reviewed technical design where needed, promoted implementation prerequisites, closure criteria and explicit authorization to implement. Skill availability alone grants none of these.

1. Verify named canonical local/remote HEAD against supplied SHA; inspect checkout status. Read `AGENTS.md`, current architecture, roadmap/execution model, owning Brief/State, upstream contracts and the approved technical design. Stop on unexpected baseline change until impact is classified.
2. Work in an isolated branch/worktree with explicit file/hotspot ownership. Do not have concurrent writers in one checkout. Stay within the checkpoint's exclusions and implementation boundary; do not edit canonical architecture to justify new behavior.
3. For each new mutable authoritative fact or causal external input, state what future reconstruction must recover. Preserve current authority, determinism, stale validation and domain boundaries.
4. Run focused tests and appropriate affected regressions, inspect the full diff, and run `git diff --check`. Diagnose failures in scope; surface architecture/product blockers instead of inventing answers.

Submit base SHA, branch/candidate SHA, changed files, closure evidence, tests, replay/fork declaration, unresolved risks and integration needs. Do not self-approve, merge/promote canonical, or push a shared branch beyond the separately authorized workflow.
