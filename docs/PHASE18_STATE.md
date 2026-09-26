# Phase 18 State — Intraday Temporal Execution v1

**Status:** INTEGRATED AND REVIEWED REFRESHED CANDIDATE — canonical promotion pending

**Canonical base:** `codex/phase8/canonical` at `77f3e1a47a1e007492a794ea777d681a21a36d09`

**Architecture update:** `c285466c355103d3637ac165246591b72eb7bda0`

**Integration branch:** `codex/phase18/TimelineIntegrationPostP8E`
**P18-A refreshed source commit:** `4dea565a95a05ff03f61a9c18cfc163d47e509e1` (merges the unchanged P18-A implementation onto promoted P8-E canonical); later merge `b974276` synced canonical State/Roadmap-only updates at `77f3e1a`, with no P18 source changes.

**Final refreshed review:** PASS at candidate/State commit `a1463e8d46ed8e4526de0b0aa7a8c86d6ecf666e`.

## Phase status

| Work | Status | Evidence / prerequisite |
|---|---|---|
| P18-A — Logical Timeline Scheduler | REFRESHED REVIEW PASS; AWAITS CANONICAL PROMOTION | Original source commit `dd4d46ae1a4519d5ae07931965fbb6156692830e`; integrated as `1af92a1` plus collision-safe identity fix `350c6a0`, then refreshed against P8-E canonical in `4dea565`. The P18-A source diff is unchanged. Refreshed `LogicalTimelineTests` 18/18 (`Temp/ValidationResults/EditMode-20260926-212439-794ceaae956f47f7aebde4fade3d05bc.xml`), ALL EditMode 1717/1717 (`Temp/ValidationResults/EditMode-20260926-212600-77374b90b7ad4504b1c0408a27e4dbec.xml`), and complete Smoke 5/5 (`Temp/ValidationResults/EditMode-20260926-212641-e7c2128b74f54c5eb8e7872fd2700d85.xml`); `git diff --check d95b60d 4dea565` passed. Independent refreshed review at `a1463e8` passed and confirmed the alignment/cardinality/reconstruction boundaries. |
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

P8-E was promoted to canonical at `d95b60d`, and canonical State/Roadmap were
updated at `77f3e1a`. P18-A has no P8-E dependency and its implementation adds
no overlapping source files; this refresh preserves the existing candidate on
the current canonical base. The repository's human
canonical-promotion gate remains in force. After approval, rebuild the DAG.
P18-B implementation still requires its own accepted checkpoint; then
continue to P18-C and only those P18-D adapters whose concrete consumer
prerequisites are met.
