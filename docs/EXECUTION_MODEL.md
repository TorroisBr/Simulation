# Multi-phase execution model

This document defines the scheduling and promotion process, not simulation semantics, a Master Orchestrator prompt, or permission to execute. `SIMULATION_ARCHITECTURE.md` is the semantic authority; `ROADMAP.md` and Phase Briefs are subordinate planning; Phase States and canonical code record delivery. Skills apply these rules to a particular workflow without owning phase scope.

A Phase Brief names objective/closure, approved checkpoint IDs and dependencies, gates, exclusions, reconstruction-sensitive state/inputs, hotspots, downstream unlocks, readiness and deferred questions. It does not carry execution progress. A future Phase without a `PHASE*_STATE.md` has no Phase-specific promotion record yet; do not synthesize delivered status from its Brief or a candidate branch.

## Two readiness layers

Architectural readiness does not imply implementation readiness:

```text
architecture and checkpoint scope accepted
→ bounded technical design, when needed
→ independent technical review
→ READY_FOR_IMPLEMENTATION
→ isolated implementation
```

Technical design may settle concrete ownership, affected components/interfaces, migration and integration sequence, and validation within accepted semantics. It may not choose a durable semantic or product answer. Such an issue blocks that track as `BLOCKED_ARCHITECTURE` or `BLOCKED_PRODUCT_DECISION`.

For a substantial checkpoint, keep a bounded design artifact in its named design/candidate branch or a reviewed checkpoint execution record. A small checkpoint may use a recorded reviewed plan instead of a permanent file. Either form must be recoverable through a known Git ref or durable record, not only an untracked local file. The Master must be able to identify checkpoint ID, canonical base SHA, reviewed scope/technical boundary, reviewer/verdict, and whether architecture assumptions are still valid. Technical plans are evidence, not a second constitution; their approval does not promote code.

Planning readiness vocabulary for a Phase or checkpoint entry:

| Label | Meaning |
|---|---|
| `DEFERRED` | No current entry work should be scheduled. |
| `WAIT_DEPENDENCY` | A named contract, capability, or gate is missing. |
| `ENTRY_ARCHITECTURE_READY` | Entry questions may be resolved; no implementation contract is implied. |
| `READY_FOR_TECHNICAL_DESIGN` | Architecture, scope, and closure are sufficient for bounded technical design; implementation is not ready. |
| `TECHNICAL_DESIGN_IN_PROGRESS` | Bounded design/review is under way. |
| `READY_FOR_IMPLEMENTATION` | Required technical review and implementation dependencies passed. |
| `IN_PROGRESS` | Authorized implementation is under way. |

These are planning views, not declarations that a whole Phase's capabilities exist. Use the current Phase 8 State for checkpoint delivery/readiness, not a phase-wide label. No future implementation checkpoint is schedulable merely because its entry architecture or Brief can be discussed.

Checkpoint execution states are `PLANNED`, `IN_PROGRESS`, `SUBMITTED`, `VALIDATED_CANDIDATE`, `APPROVED`, and `PROMOTED`. `READY`/`NOT_READY` are derived from current dependencies, gates, technical review, and available isolation rather than stored as independent truth. `CLOSED` is a Phase status. Block reasons (`BLOCKED_DEPENDENCY`, `BLOCKED_ARCHITECTURE`, `BLOCKED_PRODUCT_DECISION`, `BLOCKED_INTEGRATION`) annotate the affected track; a block in one track does not freeze independent work.

## Dependency evaluation

| Dependency | What it can unlock |
|---|---|
| Semantic contract | Once consolidated in canonical `SIMULATION_ARCHITECTURE.md`, downstream architecture or technical design that needs only that contract. An approved Brief cites it and records which checkpoint depends on it; the Brief does not establish domain meaning. |
| Promoted capability | Downstream implementation/integration that needs working upstream behavior, after canonical code and State validation. |
| Integration | Isolated candidate development may proceed; combined promotion waits for the named composition boundary. |
| Architecture gate | Requires an explicit semantic decision and its approved consolidation. |
| Product decision gate | Requires user intent; neither worker nor Architecture Lab supplies it implicitly. |
| Validation dependency | Used only where reliance on upstream requires specific evidence beyond its mere existence; ordinarily validation is a promotion gate. |
| Soft/ordering | Preference, not a readiness blocker. |

Readiness is calculated from current canonical docs/code and explicit dependency IDs, not from historical chat memory or phase-number order. A candidate or unreviewed plan does not satisfy a promoted-capability edge. An explicit isolated dependency on a named candidate may allow preparation, but not implicit downstream canonical unlock.

On each run start and after canonical/architecture/roadmap changes: verify branch and local/remote SHA; load current architecture, roadmap, Briefs, States, and relevant code; derive the checkpoint DAG; evaluate gates and technical design; classify READY work; assess semantic and file/hotspot interference; schedule a safe set; repeat after promotion. Stop rather than inventing missing checkpoint contracts. Record the baseline and dependency evidence used for each dispatched track.

## Context, parallelism, and blockers

Give workers the exact base SHA, current `AGENTS.md`, relevant architecture sections, their Brief/State/checkpoint contract, upstream evidence, explicit exclusions, owned hotspots, and applicable workflow Skill. They must reread current architecture when a semantic question arises. Do not paste all historic conversation or assume a long-lived agent remembers it.

`MUST WAIT` means a hard gate/contract/capability or unresolved shared semantic boundary prevents safe work. `PARALLEL WITH ISOLATION` permits separate worktrees with explicit ownership and serial integration. `PARALLEL SAFE` requires both semantic and Git/file independence; read-only investigation often qualifies. Different files do not prove semantic independence. `SimulationRuntime`, daily loop, diagnostics, spatial/travel, Knowledge, command capture, persistence composition, and shared stores merit explicit ownership windows rather than a universal lock manager.

An architecture blocker record identifies checkpoint, base SHA, exact unanswered decision, why current architecture does not answer it, options and consequences, affected invariants/downstream work, safe independent work, and the minimum decision required. Product blockers use the same traceability but ask the user for the missing intent. The affected track stops; independent READY tracks continue. After a decision is approved and consolidated, refresh canonical and recalculate the DAG before resuming.

## Candidate, promotion, and closure

```text
implementation → focused validation → SUBMITTED
→ independent review → VALIDATED_CANDIDATE
→ integration/regression → explicit human approval initially
→ PROMOTED + State evidence → DAG refresh
```

Worker completion, green tests, candidate existence, and technical-plan approval are not promotion. Review the full diff against its actual base and check semantic architecture, technical design, mutation authority, deterministic and replay/fork implications, stale state, atomicity, scope, tests, and hotspot interference. Before promotion verify ancestry, expected diff, current remote, integration evidence, State accuracy, required tests, `git diff --check`, and absence of unresolved blockers. Do not force-push, silently merge main, or discard user work.

Canonical promotion initially requires human approval. A later policy may explicitly authorize narrow automatic classes; this document does not. Phase closure separately requires the closure objective and mandatory checkpoints canonical, appropriate regression and independent closure review, known limitations and deferred consumers recorded, and a formal State marker. A state-only closure commit may be appropriate. Closing a phase never requires every future consumer.

After canonical advancement, classify each active candidate with orchestrator/reviewer evidence: `UPSTREAM_IRRELEVANT` (continue, still validate at integration), `REVALIDATE` (rerun affected review/tests), `REINTEGRATE` (refresh composition without discarding work), or `INVALIDATED` (return to design/work). A worker alone cannot declare a semantic change irrelevant. Failed tests should be diagnosed in-track; failed review returns for changes; merge conflicts require inspection of both semantics. Preserve a recoverable candidate record rather than restarting or deleting blindly.

## Reconstruction-sensitive gate and stop conditions

Each checkpoint introducing authoritative mutable state or external causal input must answer: **what must future reconstruction recover to reproduce this causality?** Consider stable IDs, factual state and commitments, payload/authority/logical boundary/ordering of commands, effective content/configuration and calendar, random context, and causally relevant provenance. Put the declaration in the Brief/checkpoint contract, verify it in technical and candidate review, and record actual delivery in State. This gate does not require early save/replay implementation. Diagnostics, events, and selective History are not primary truth.

Stop a run when all safe READY work is exhausted; only human/architecture gates remain; canonical promotion or phase closure awaits required approval; an active semantic conflict cannot be isolated; validation cannot be resolved in scope; canonical changed unexpectedly and impact cannot yet be classified; or resource/context limits require a resumable handoff. Do not promise background execution absent an actual scheduled/run mechanism, wait indefinitely inside a worker for a human decision, or start speculative distant phases to keep agents busy.

Roadmap revisions may insert/split/merge phases or checkpoints. Preserve historical State, record new dependency impact, and reclassify candidates. A change to simulation meaning must go through architecture approval; a change to product scope or material roadmap priorities remains human-gated.

## Intraday and extension impact review

The 2026-09-26 approved requirements are consolidated in architecture §§2,
11–12 and 91–92, Roadmap and Phase 18/19 Briefs. Preserve existing phase and
checkpoint IDs. Read `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` when
refreshing pre-change candidates; do not infer that this documentation change
promotes their code or cancels their delivered daily-profile contracts.

For timed work, inventory the logical instant, pending work/activities,
availability, same-time ordering/sequences and compatible calendar/profile.
For extensible generation/mods, inventory stage/contributor identities and
versions, causal contribution order, authoritative extension state, and explicit
retrofit inputs. A daily timestamp or seed alone cannot capture this causality.
Retain these semantics when introduced; later persistence cannot recover
information discarded by an earlier checkpoint.

Design to current natural semantic seams now. Defer loader/public API mechanics
to P19; no worker may add speculative mod infrastructure or new sleep/theft/needs
gameplay to satisfy P18. Day-based proof fixtures may remain explicitly scoped,
but a worker may not turn their daily cadence into the permanent actor or travel
contract. Domain validation is distinct from adversarial player/mod security.
