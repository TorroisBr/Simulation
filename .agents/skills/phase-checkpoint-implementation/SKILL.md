---
name: phase-checkpoint-implementation
description: Carry an approved, implementation-ready checkpoint through an isolated, validated, independently reviewed candidate and integration preparation; stop before canonical promotion.
---

# Phase checkpoint implementation

Use only after scope and architecture are approved, required technical design is reviewed, and implementation prerequisites are canonical. Skill availability does not authorize roadmap execution.

1. Verify canonical local/remote HEAD and checkout status. Read `AGENTS.md`, current architecture, Roadmap, Execution Model, owning Brief/State, upstream contracts, and approved technical design. Classify any newer baseline before continuing.
2. Create or reuse an isolated feature branch/worktree from the required canonical base, and record file/hotspot ownership. Implement only the checkpoint contract and exclusions. Preserve authority boundaries, determinism, stale-state semantics, and reconstruction-sensitive inputs/state.
3. Run focused tests during implementation, then the required affected regressions and promotion-level suites stated by `AGENTS.md`, Brief, State, and Execution Model. Keep durable result artifacts and record exact base/candidate SHAs, changed files, tests, replay/fork impact, risks, and integration needs. Run `git diff --check`.
4. Commit/push a durable candidate and validation record under standing Git authority. Request an independent exact-tip review using `candidate-review`; the author must not approve their own work.
5. Address findings with additive commits where possible, rerun affected validation, and obtain a new review for each changed code tip. Prepare dependency-safe integration when required, preserving candidate provenance and rerunning integration validation.
6. Stop at canonical promotion. The candidate and its integration evidence are reviewable inputs to the separate `canonical-promotion` gate, not promotion approval.

Diagnose failed tests and ordinary conflicts from repository evidence. Escalate only an actual unresolved product or canonical architecture decision; do not broaden scope to resolve a blocker speculatively.
