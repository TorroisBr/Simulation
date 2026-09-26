# Phase 20 — Multi-participant Activities v1

**Authority:** planning scope under `../SIMULATION_ARCHITECTURE.md` §§11–12,
91–93. **Readiness:** `ENTRY_ARCHITECTURE_READY` for bounded decomposition/design;
implementation `WAIT_DEPENDENCY` on relevant P18 capabilities and reviewed
technical design. No P20 implementation or checkpoint IDs are approved.

## Objective and closure

A temporary activity instance can involve individual actors without requiring
persistent Group/Organization membership. Each retains identity, Knowledge and
decision authority. A bounded shared slice proves independent agreement, future
commitments, a coordinated validated start and common context with potentially
different participant effects, plus appropriate failure/cancellation handling.
Use synthetic test operations or a separately approved existing consumer; do not
add robbery, gangs, hunting, meals, patrol, rituals, construction or War gameplay
to demonstrate the generic capability.

## Semantic boundaries

- **Definition versus instance:** content/rules define activity and applicable
  role requirements/count bounds; concrete instance owns its stable identity,
  temporal lifecycle and shared context. Participation links use semantic actor
  IDs (PersonId for Persons), never materialized NPCs as the only durable identity.
  No fixed universal role catalog, Actor hierarchy or Activity schema.
- **Independent actors:** proposing or recruiting does not select participation
  for everyone. Each decision uses its actor's allowed Knowledge; start/execution
  revalidate factual conditions. A shared context is not shared omniscient
  Knowledge, identical outcomes, merged identity or a temporary synthetic NPC.
- **Formation and commitments:** optional formation/proposal precedes execution
  where recruitment is needed. Agreement may reserve a future interval;
  agreement/reservation does not imply availability or a valid start. Partially
  formed proposals are valid nonexecuting state; each authoritative transition
  must be coherent, not a half-applied reservation mutation.
- **Coordinated start:** validate required roles/counts, every required actor's
  current capability/availability and matching commitments together. Conflict
  or stale state cannot start only part of the required activity accidentally.
  Requirements refer to the participants/windows needed at that boundary;
  later role-specific intervals need their own validated transitions when
  supported. Do not assume every participant occupies the same whole interval.
  No second clock/scheduler or mirrored agenda facts.
- **Lifecycle:** define failure-to-form, cancellation, withdrawal/abort and release
  of affected reservations for the supported slice. Participation changes during
  execution, optional roles and compatibility/overlap of commitments require
  explicit rules when supported. Do not infer rollback of already applied effects.
- **Domain effects:** activity context/lifecycle coordination does not replace
  domain-owned outcomes, transactions or per-participant World Truth. Required
  multi-authority changes need a reviewed coherent commit boundary.
- **Group separation and scale:** persistent membership is separate. Small-group
  individual execution does not mandate the semantics of units/armies/War;
  aggregates may reuse compatible concepts via their own domain contract.

## Dependencies and gates

- **Hard contracts/capabilities:** P18-A logical timeline/scheduler, P18-B lifecycle
  and P18-C availability/decision boundaries relevant to the selected slice.
  Accepted contracts unlock design; integrated execution waits for promotion.
  No blanket requirement for P18-D's legacy migrations, P19 loader, P8 travel,
  persistent Organization or military implementation.
- **Conditional consumers:** traveling together consumes actual spatial/travel
  capabilities; multi-worker production/construction consumes applicable P14/P15;
  external choice consumes relevant P11 input semantics. These integrations
  cannot silently enlarge P20's first proving scope.
- **Entry/technical gate:** bound role/count policy, instance/participation and
  reservation ownership, independent decision path, deterministic participant
  ordering, duplicate/overlapping commitments, stale and loss-of-participant
  behavior, atomic start/release, pending scheduler invalidation and effects.
  Exact APIs, schema and v1 consumer choice are not invented in this Brief.
- **Extensibility:** a future code mod can define a new multi-participant activity
  and policies/effects through semantic contracts without changing `NpcRuntime`
  or adding a dedicated manager to the base game. P19 exposes supported public
  adapters later; official P20 need not ship a loader or speculative registry.
- **Reconstruction:** compatible definition/version, instance ID, formation,
  participant IDs/roles and accepted commitments/intervals, scheduled start,
  lifecycle/context, effects already applied, inputs/order, revisions/causal
  sequence and random context where relevant. Derived invitations/agenda indexes
  may be rebuilt only from sufficient retained facts. No persistence schema now.
- **Validation:** unrelated actors can agree independently; refusal/missing
  requirements prevents start; incompatible reservations/stale availability
  reject coherently; required participant loss follows explicit semantics;
  cancellation releases commitments; deterministic shared execution can apply
  distinct outcomes without shared hidden Knowledge; clone/dormancy/continuation
  retain identities and state. Include single-participant compatibility and P18
  scheduler/lifecycle regressions; later runtime integration follows risk gates.
- **Exclusions:** full AI planner, social negotiation, gangs/Group store, War
  execution, generic workflow/universal effect engine, arbitrary participant
  arrangements implemented up front, mod loader and early save/replay.
- **Hotspots:** temporal/activity/availability authorities, actor decisions,
  domain commit boundaries, commands and diagnostics. Isolate writers and
  review integrations; participant logic must not accumulate in `AdvanceDay`.
