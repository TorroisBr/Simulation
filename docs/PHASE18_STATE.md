# Phase 18 State — Intraday Temporal Execution v1

**Status:** IN_PROGRESS  
**Canonical base:** `codex/phase8/canonical` at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`  
**Architecture update:** `c285466c355103d3637ac165246591b72eb7bda0`  
**Integration branch:** `codex/phase18/TimelineIntegration`  
**P18-A integrated code commit:** `350c6a049404b4f339044a31d1620a9596f55f5a`

## Phase status

| Work | Status | Evidence / prerequisite |
|---|---|---|
| P18-A — Logical Timeline Scheduler | INTEGRATED CANDIDATE; AWAITS CANONICAL PROMOTION | Candidate source commit `dd4d46ae1a4519d5ae07931965fbb6156692830e`; integrated as `1af92a1` plus collision-safe identity fix `350c6a0`. Independent review passed. Focused timeline 18/18, ALL EditMode 1714/1714, complete Smoke 5/5; `git diff --check` passed. |
| P18-B — Activity Lifecycle | DESIGN REVIEW PASS; IMPLEMENTATION BLOCKED | Waits for P18-A promotion and its own accepted implementation checkpoint. Stable activity-instance identity is independent of PersonId and participant identity; zero/one/many actors are allowed only for unformed Proposed instances, with Scheduled/Active requiring at least one. |
| P18-C — Availability-Driven Decisions | DESIGN REVIEW PASS; IMPLEMENTATION BLOCKED | Waits for the relevant promoted P18-A/B contracts. Decisions remain PersonId-owned and Knowledge-bounded; execution revalidates current truth. |
| P18-D — Consumer Integration | BLOCKED BY P18-C AND SELECTED CONSUMER CAPABILITIES | Integrate only explicit consumers whose prerequisites are met. No blanket P8/P11/P20 gate is implied. |

## Architecture constraints carried forward

- The intraday/extensibility alignment is active: logical instants and stable
  causal ordering are required where the selected consumer needs them;
  extensibility-compatible identity/data shape is a review constraint now.
  Public Mod API and loader mechanics remain deferred to P19.
- The multi-participant alignment is active: ActivityInstanceId is not
  PersonId, NpcRuntimeId, or participant identity. Participant cardinality is
  not fixed by one-actor examples. P20 does not wait for P18-D or P19.
- P18-A's `(worldId, profileId, absoluteDay)` boundary identity and
  `(ownerId, workId, revision, occurrence)` work identity use injective,
  length-prefixed encoding. Pending boundary day/identity and the owner's
  idempotency/effect state are explicit reconstruction facts.
- P18-A does not integrate into `SimulationRuntime.AdvanceDay`; legacy daily
  execution remains authoritative until a selected P18-D consumer migrates.
- P18-A has no dependency on P8-E, P11, or P20. P18-D may depend on the
  specific P8-E/P11 consumer contracts it selects. P20 depends on relevant
  P18-A/B/C outputs but not P18-D or P19.

## Promotion and next actions

The P18-A integration candidate is prepared on a noncanonical branch. The
repository's human canonical-promotion gate remains in force; this does not
block independent READY work. After promotion, rebuild the DAG and implement
P18-B if its checkpoint is schedulable, then P18-C, followed by only those
P18-D adapters whose concrete consumer prerequisites are met.
