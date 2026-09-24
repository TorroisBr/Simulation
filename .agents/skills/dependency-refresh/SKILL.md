---
name: dependency-refresh
description: Recompute this repository's multi-phase checkpoint readiness after a canonical, roadmap, architecture, or Phase State change; report design versus implementation work without starting it.
---

# Dependency refresh

Use for a read-only readiness audit or as the first step of an authorized orchestration run. This skill never authorizes implementation or promotion.

1. Identify the named canonical branch and expected baseline. Verify local and remote HEAD and checkout status; stop and report unexpected advancement before trusting cached readiness.
2. Read current `AGENTS.md`, `docs/SIMULATION_ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/EXECUTION_MODEL.md`, relevant Phase Briefs/States, and canonical code only where implemented capability is disputed.
3. Build a checkpoint/contract DAG from documented IDs and typed dependencies. Do not manufacture checkpoint IDs for a Phase with no approved decomposition. Distinguish accepted semantic contract, reviewed technical design, promoted capability, integration/validation gate, and soft ordering.
4. Compute separate ready sets for architecture entry, technical design, and implementation. Implementation requires reviewed technical design where applicable plus actual promoted prerequisites. Candidate branches do not satisfy canonical capability edges by default.
5. For concurrent work, classify `MUST WAIT`, `PARALLEL WITH ISOLATION`, or `PARALLEL SAFE` using semantic and file/hotspot conflicts. A blocker pauses only dependent tracks.

Output baseline SHA, each ready checkpoint or entry task with evidence and missing gates, blocked tracks with precise reasons, and any conflict between docs and actual canonical state. No files are changed unless an independently authorized workflow records the refresh. Stop on unresolved authority mismatch or missing product/architecture decision rather than inferring one.
