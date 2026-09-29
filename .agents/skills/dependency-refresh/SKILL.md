---
name: dependency-refresh
description: Reconcile delivered and in-flight work with updated architecture/roadmap contracts, classify impact, rebuild the multi-phase dependency DAG, and schedule safe ready work.
---

# Dependency refresh

Use for a read-only readiness audit, after an architecture/Roadmap/Execution Model/Phase Brief/State/canonical-baseline change, or as the first step of an explicitly authorized orchestration run. Reuse compatible work; do not restart a candidate merely because documentation changed. This skill never authorizes implementation, canonical promotion, or Phase closure by itself.

1. Identify the named canonical branch; fetch and verify local/remote HEAD, checkout/worktree status, and durable candidate/review refs. Reconcile State claims with actual history before using cached readiness.
2. Read current `AGENTS.md`, architecture, Roadmap, Execution Model, affected Briefs/States, and referenced ADRs/alignment records. Compare updated contracts with delivered work, candidates under implementation/review/integration, and outstanding revalidation obligations.
3. Classify each affected item as `UPSTREAM_IRRELEVANT`, `REVALIDATE`, `REINTEGRATE`, or `INVALIDATED` per `docs/EXECUTION_MODEL.md`. Preserve compatible candidates; revalidate relevant assumptions (including temporal identity/cardinality where applicable), reintegrate on a newer base when required, and invalidate only work whose contract is incompatible.
4. Rebuild the complete checkpoint/contract DAG from documented dependency edges, not numeric Phase order. Do not invent checkpoint IDs. Distinguish accepted contract, reviewed design, promoted capability, integration/validation gate, and soft ordering. A candidate does not satisfy a canonical capability edge by default.
5. Compute ready sets for architecture entry, design, implementation, review, and integration. Classify concurrency as `MUST WAIT`, `PARALLEL WITH ISOLATION`, or `PARALLEL SAFE` using semantic and file/hotspot conflicts. If the active user objective authorizes execution, launch all safely parallel READY work under the repository's Luna-first policy; otherwise report readiness without starting work. A blocker pauses only dependent tracks.

Output baseline SHA, impact classification per active/delivered candidate, ready work and gates, blocked tracks with precise reasons, and any docs/history mismatch. Record the refresh when the active workflow requires durable evidence. Continue independent READY work only when the active objective authorizes orchestration; escalate only an unresolved authority mismatch or material product/architecture decision not determined by repository evidence.
