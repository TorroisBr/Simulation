# Phase 14 State — Productive Sources & Material Flow v1

**Status:** PHASE 14 IN PROGRESS — P14-A IMPLEMENTATION

**Canonical implementation base:** `codex/phase8/canonical` at
`470667d37863384edadb3d93ef64d8004aff46a3`.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`, including
`docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

**Current candidate branch:** `codex/phase14/P14ALocalMaterialFlow`. The branch
is based on the P8 canonical tip above and includes the reviewed P14 planning
artifacts. This State records planning and execution progress only; no P14 code
capability has been delivered or promoted.

## Checkpoint status

| Checkpoint | Status | Scope / evidence |
|---|---|---|
| P14-A — Local Daily Material Flow v1 | IN_PROGRESS | Approved scope: one authored City, one exogenous daily source for one item, that City's free population consumption, and closing stock `opening + applied source − actual free consumption`. The source has no inputs, reserves, depletion, or transformation. Settlement owns the material; the market is custodian. Contract: `docs/design/PHASE14A_LOCAL_DAILY_MATERIAL_FLOW_CHECKPOINT.md`. |

The user approved this bounded profile and current-base independent review
passed for checkpoint content `7a9e8d71ed7a53578b7dc36ef1cafd03b856b0f0` with
technical design `565a3c0e7d19c14736149ecae90887e8357dfef1`, against P8
canonical `470667d` and architecture `c285466`, including both alignment
records. The approved checkpoint and the active full-roadmap task permit
isolated implementation to proceed. Canonical promotion remains separately
human-gated.

## Dependency and impact record

- P14-A consumes P8-A stable `LocationId` truth and promoted P8-B/C City anchor
  composition. It does not consume P8-D passages or P8-E travel.
- P9-A and P11 Actor Choice are promoted. Their closure/promotion updates do
  not add semantic dependencies to this passive daily source/sink. P9's former
  serial ownership edge is satisfied; P10 is optional authored content.
- P18-A/B are promoted, but P14-A does not promise duration or intraday
  production. P20 is conditional on a later multi-participant consumer. P19's
  public API/loader remains deferred while current extensibility constraints
  apply to design and review.
- P12/P13 own future persistence and reconstruction implementation; their
  capabilities do not block this daily domain slice.
- P15 may consume P14 material cost after promotion. P16 may consume material
  logistics after its relevant movement prerequisites; P14-A adds no routes or
  transport.

## Implementation, validation, and closure

Implementation has started on the isolated candidate branch from the P8
canonical base. No implementation commit or validation result is recorded
yet. The candidate must preserve stable settlement/source/store/item identity,
LocationId-to-City anchor agreement, title/custody separation, atomic source
overflow rejection, stock-limited actual consumption, the `Economy.Enabled`
gate, deterministic closing-balance diagnostics, and the reconstruction
inventory in the checkpoint contract.

Run focused economy, market stock, free-consumption, identity/anchor, overflow,
disabled-economy, diagnostic, and same-input determinism coverage. Independently
review the full diff against its actual base. Before code integration or
promotion, run repository-required regression suites, official full Smoke,
`git diff --check`, and any daily-loop validation required by the final diff.
Record exact evidence here after each validated candidate. P14 is not closed
until the approved objective is delivered, required review and validation pass,
the capability is canonically promoted with human approval, and a separate
closure record is independently reviewed.
