# Simulation — repository working rules

This repository implements a persistent-world simulation. Do not infer permission to begin a phase or promote a candidate merely because its roadmap entry exists.

## Read the current authorities

Before architecture, technical design, implementation, or promotion work, read the current `docs/SIMULATION_ARCHITECTURE.md` from the target checkout. Read the relevant `docs/ROADMAP.md`, `docs/phases/PHASE*_BRIEF.md`, and `docs/PHASE*_STATE.md`; inspect current code/tests when the task depends on implemented behavior. Historical State text can describe an earlier “next” task: use the latest formal closure/promotion record and actual canonical code for delivered status.

- Architecture defines long-lived domain meaning and invariants.
- Roadmap and Briefs define approved planning scope and dependencies, subordinate to architecture.
- Phase States record factual canonical delivery, validation, limitations, and closure.
- Code/tests show what is actually implemented; a future architectural direction is not an existing capability.
- This file defines repository operations. Skills define repeatable workflows, not domain truth.

If these disagree materially, report the mismatch. Do not silently choose a new semantic answer. Phase-specific historical instructions in older States do not govern unrelated future phases.
Existing `.codex/agents/phase5_*` and `phase6_*` roles remain scoped to those historical phases; do not reuse them as phase-neutral workers by name.

## Separate design from implementation

Architectural readiness does not imply implementation readiness. A checkpoint may require bounded technical design and review before an implementation worker starts. Technical design may choose ownership, interfaces, migration, tests, and integration sequence within accepted semantics. An unresolved semantic or product choice blocks the affected track; independent safe work may continue.

Do not implement a roadmap item, start an orchestration run, or change `docs/SIMULATION_ARCHITECTURE.md` merely because related documentation is being edited. Architecture changes need explicit human/Architecture Lab approval. Product-intent decisions belong to the user.

## Before a work wave

1. Verify the named canonical branch, local/remote HEAD, expected baseline, and worktree status. Stop on unexpected advancement until its impact is classified.
2. Reconstruct checkpoint readiness from current architecture, roadmap, Briefs, States, and canonical code; distinguish a closed contract from a promoted capability.
3. Classify work as `MUST WAIT`, `PARALLEL WITH ISOLATION`, or `PARALLEL SAFE`, considering semantic and Git/file conflicts.
4. Give each worker a bounded objective, closure criterion, baseline SHA, relevant context, dependency evidence, exclusion list, and owned hotspots.
5. Use isolated branches/worktrees for concurrent writers. Never have two writers modify the same checkout or own the same semantic hotspot without an agreed boundary.

Typical hotspots include `SimulationRuntime`, `AdvanceDay`, spatial/travel and Knowledge authorities, diagnostics, command capture, persistence composition, and shared domain stores. Prefer explicit integration order to a general lock framework. Read-only exploration/review can be parallel when useful.

## Candidates, review, and validation

Worker completion is not a validated candidate; a validated candidate is not canonical. Review the full candidate diff against its actual base, including architecture, mutation authority, stale-state handling, atomicity, determinism, replay/fork sensitivity, scope, and shared hotspots. A worker does not approve its own work. Green tests alone do not approve a change.

Run focused tests during implementation, affected regressions at checkpoint/integration, and the promotion gate appropriate to risk and the current Brief/State. A change to daily-loop or long-horizon causal behavior requires corresponding long-run validation. Run `git diff --check`. Diagnose failed tests rather than treating every failure as a product decision. Documentation-only changes do not need Unity tests unless they unexpectedly affect executable content.

For concurrent branches, establish their common base, inspect both diffs and semantic overlap, select integration order, and rerun affected validation after integration. Do not discard either branch silently.

## Promotion and closure

Initially, canonical promotion and phase closure require explicit human approval. Before promotion verify canonical ancestry/remote state, candidate review, required tests, scope, architecture compliance, unresolved blockers, State accuracy, and diff checks. Never force-push, rewrite shared history, delete tags/main, merge or modify main, or discard user changes. Preserve blocked/stale candidate work for diagnosis or recovery.

After an approved promotion, record the final SHA and evidence in the owning Phase State, push the intended canonical branch, verify local/remote synchronization, and recompute readiness. Phase closure is a separate review of its objective, required promoted checkpoints, regressions, known limitations, and deferred consumers; a state-only closure commit may be appropriate.

Routine read/search, Git, and relevant Unity validation operations may be performed within the active task and permission policy. If the sandbox requires technical approval, request the minimum permission. No operational rule here overrides a user's explicit read-only or narrower scope.

The lifecycle and dependency vocabulary is in `docs/EXECUTION_MODEL.md`; it does not grant permission to execute the roadmap.
