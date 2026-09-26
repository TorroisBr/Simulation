# Multi-participant activity alignment — 2026-09-26

## Verified baseline and decision

The user authorizes a documentation-only addition for generic multi-participant
activities. Canonical `codex/phase8/canonical` local/remote were verified at
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`; the canonical worktree was clean.
The update is prepared in isolated `codex/architecture/multi-participant-activities`.

Introduce **Phase 20 — Multi-participant Activities v1** after the relevant
P18-A/B/C temporal capabilities, without waiting for all P18-D migrations or
the P19 mod loader. Entry/decomposition can use accepted P18 contracts; execution
waits for promotion and reviewed bounded technical design. No P20 checkpoint
IDs or implementation readiness are inferred from this Brief.

P18 retains its scheduler/lifecycle/availability scope. It must distinguish
definition from concrete instance and keep activity identity/lifecycle independent
of one actor/NPC. It may prove an individual slice. Formation, participant-role
coordination, shared start/execution and joint reservations belong to P20, not
an expanded P18 implementation. There is no dependency cycle back from P18.

## Required semantics and exclusions

- Activity definition/content is distinct from the stable concrete instance.
  One or more participants remain individual actors; roles and min/max counts
  are used where needed, not a universal hierarchy or fixed arrangement catalog.
- Proposal/formation may precede execution. Independent actor decisions can
  create future commitments/reservations. Partial formation is valid pending
  state; each mutation is coherent. Agreement is not availability or execution.
- Scheduled start checks required participants/roles and current availability,
  commitments and other domain preconditions before execution effects. Failure
  must not accidentally start only part of the required shared activity.
- Cancellation, failure-to-form, withdrawal/abort, reservation release and
  composition changes need explicit rules for supported consumers. No implicit
  rollback of effects, automatic recruitment or omniscient participant selection.
- Shared context does not imply identical outcomes, shared Knowledge, merged
  identities or a temporary NPC. Domain authorities retain participant effects.
- Persistent Group/Organization membership is separate and not required.
  Large-scale military aggregates retain their own execution/availability
  semantics; War does not schedule thousands of individual Persons in P20.
- Future code mods use compatible semantic activity/participation/effect seams
  without editing `NpcRuntime` or requiring a base-game manager per mechanic.
  The P19 public API is conditional later work, not implemented in P18/P20 entry.
- No robbery/gang, hunting, co-travel, meal, patrol, ritual, construction, War,
  social negotiation, full AI planner or workflow engine is implemented here.
  Exact APIs, schemas, role policies and participant arrangements remain bounded
  technical/consumer design rather than speculative infrastructure.

## Conditional downstream dependencies

| Track | Contract effect |
|---|---|
| P18-B/C | Preserve definition/instance/participant identity and availability seams; no permanent one-to-one Activity/Actor contract. One-participant proof remains valid. |
| P8/P11 | Individual traveler/SellGoods closure unchanged. Future co-travel or participation commands consume actual P20 plus relevant domain/input capabilities. |
| P12/P13 | Shared-activity coverage inventories/restores formation, participant roles/agreements/reservations, scheduled start, lifecycle/context, individual effects and causal ordering. No blanket P20 dependency for narrower saves. |
| P14/P15 | Multi-worker consumers require relevant P20/P18 capabilities. Source/stock truth and factual creation alone remain independent. |
| P16/P17 | No mandatory P20 dependency; aggregates may reuse compatible temporal/commitment concepts with their own semantics. |
| P19 | Multi-participant activity adapters consume relevant P20 contracts/capability; basic platform does not wait for all P20. Definition/policy/effect contribution must not require NPC/base-game rewrites. |
| P9/P10 | No new scope/capability dependency. A future profile initializing activity/participation facts follows the relevant declared domain inventory. |

## Revalidation and current work

No P18/P20 implementation or technical-design artifact is tracked in this
canonical baseline. The existing orchestration task was not active at inspection;
existing P8-D/E and P11 candidate refs remain separate from canonical. This
update starts/resumes no implementation and rewrites no candidate branch.

- Delivered P5–7/P8-A/B/C and bounded P8-D/E/P11 work: `UPSTREAM_IRRELEVANT`
  for this additional participant requirement; prior intraday-refresh review
  obligations remain in force. No new collective-execution gate or discarded work.
- Any P18-B/C technical design already prepared elsewhere: `REVALIDATE` instance
  identity/lifecycle and actor commitment assumptions before approval. P18-A
  must also permit a stable temporal subject independent of a single actor.
  A design that permanently fuses instance and actor must return for correction;
  no such implemented contract was observed in this baseline.
- P12/P13 and P19 broad future plans: targeted inventory/extension-contract
  refresh when shared activities enter supported scope. P14/P15 planning adds
  only conditional multi-worker edges; P16/P17 are not recast as small-group systems.

No promoted capability is invalidated. No historical validation record is
rewritten and no false P18/P20 delivery State is created. The superseded
assumption is only that all future Activity instances must belong to exactly
one actor.

## Reconstruction and document validation

Recover compatible definition/versions, instance and participant identities,
roles/requirements where causal, formation/decisions, reservation intervals,
scheduled start, lifecycle/context and already-applied effects, plus input
boundaries/order, pending work/reconstruction inputs and random context.
Earlier forks retain the then-current formation/roster, not a later final set.
Derived indexes and diagnostics do not replace these facts. No save schema now.

Required gates are independent document/dependency review and `git diff --check`.
No executable content changes, so Unity validation is not required for this update.

Independent documentation review and targeted re-review passed with no remaining
blocker, major or minor findings. Review covered participant semantics, phase
scope/DAG, reconstruction, mod boundaries and candidate impact. `git diff --check`
passed; no Unity tests were run for this documentation-only change.
