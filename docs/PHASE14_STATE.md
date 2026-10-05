# Phase 14 State — Productive Sources & Material Flow v1

**Status:** PHASE 14 IN PROGRESS — P14-A AND P14-B PROMOTED; REMAINING PHASE WORK OPEN

**Canonical implementation base:** `codex/phase8/canonical` at
`470667d37863384edadb3d93ef64d8004aff46a3`.

**Architecture baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`, including
`docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

**Planning parent:** `codex/phase14/P14ALocalMaterialFlow`.
**Implementation source:** `codex/phase14/P14AImplementation` at
`6d37afa8891be4cbf3bbbc3aef36f5c91f194dd2`, based on
`0bb47b1db9e350127074768024c6de1a7d4b3d0c` (reviewed planning artifacts on
the P8 baseline). **Integration candidate:** `codex/phase14/P14ALocalMaterialFlow`
at the same code tip; fast-forward integration has no code divergence. P14-A
is PROMOTED at `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`, with this
State-only promotion record at `f8a61fe9634ba9ab56ee31d50b57b45fef292a6f`.
Phase 14 remains open; this promotion does not close the Phase.

## Checkpoint status

| Checkpoint | Status | Scope / evidence |
|---|---|---|
| P14-A — Local Daily Material Flow v1 | PROMOTED | Approved scope: one authored City, one exogenous daily source for one item, that City's free population consumption, and closing stock `opening + applied source − actual free consumption`. The source has no inputs, reserves, depletion, or transformation. Settlement owns the material; the market is custodian. Contract: `docs/design/PHASE14A_LOCAL_DAILY_MATERIAL_FLOW_CHECKPOINT.md`. |
| P14-B - Finite Reserve Daily Source v1 | PROMOTED | Code `709bf0ec198da5c64c276ba4ab6084faccba87ff`, tree `5b5b18cb652d383e9ae9202066a95766c9097c20`; exact-tip review, validation, and the fast-forward to `codex/phase14/canonical` at `82b6a8acd8489509b4434b79519a32b815fe8aa7` are recorded below. |

The user approved this bounded profile and current-base independent review
passed for checkpoint content `7a9e8d71ed7a53578b7dc36ef1cafd03b856b0f0` with
technical design `565a3c0e7d19c14736149ecae90887e8357dfef1`, against P8
canonical `470667d` and architecture `c285466`, including both alignment
records. The user approved promotion; the capability is canonical at
`c44904b`, recorded in State at `f8a61fe`. Any future P14 checkpoint and its
canonical promotion remain separately scoped and gated.

### Current P14-B candidate (2026-10-04)

P14-B is promoted on `codex/phase14/canonical`. Its source/test commit is 709bf0ec198da5c64c276ba4ab6084faccba87ff (tree 5b5b18cb652d383e9ae9202066a95766c9097c20), based on integration commit 1ba58eeda6296be349b0a1def6d1d08a1fb1258f. It includes P10 canonical e53252de5277fd5af46bbacb8eda5ee6e74aff08 and P12 canonical a6572ab3d4330d81edb334ae8b4c84ca5e6b173e. Focused validation, ALL EditMode 2293/2293, official Smoke 5/5, and implementation diff-check pass; exact-tree evidence archive: docs/validation/P14B/P14B-current-base-anchor-deferral-validation-5b5b18c.zip (SHA-256 6FE147E20D38B054AEF4D5EE3792B57F657407977549A736FEA87F7CFA0F3EBB). Independent exact-tip implementation review passed with no actionable findings; durable review: docs/design/P14B_FINITE_SOURCE_IMPLEMENTATION_REVIEW.md. This does not claim P12 or P13 readiness, export/hydration, capture eligibility, or Phase 14 closure.

## P14-B bounded checkpoint promotion — 2026-10-04

Following fresh remote-ref refresh and final preflight, `codex/phase14/canonical` was fast-forwarded from `4caecbbfb0464c965811402b3c11d8717605114a` to `82b6a8acd8489509b4434b79519a32b815fe8aa7`. The promoted candidate includes implementation `709bf0ec198da5c64c276ba4ab6084faccba87ff` with unchanged reviewed tree `5b5b18cb652d383e9ae9202066a95766c9097c20`, durable exact-tip review `docs/design/P14B_FINITE_SOURCE_IMPLEMENTATION_REVIEW.md`, and exact-tree validation evidence `docs/validation/P14B/P14B-current-base-anchor-deferral-validation-5b5b18c.md` plus its archive. Remote canonical synchronization was verified at the promoted tip.

Promotion scope is the bounded P14-B finite-reserve daily source and its admission/composition integration. The combined P14 City/P10 Ruin bootstrap remains deferred; P8 one-owner-per-Location and the existing P10/P14 proving scopes remain unchanged. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 14 remains open. No capture eligibility, export/hydration, complete owner/epoch coverage, or Phase closure is claimed.

#### P10/P14 bootstrap composition boundary

The accepted proving profiles remain separate. P8 retains its one-owner-per-Location invariant; this P14 checkpoint does not relax it, and P10 does not create another City Location to make a combined bootstrap pass. Because the currently authored P14 City and P10 Ruin would both claim the sole P8 Location, the combined P14/P10 profile is rejected during profile admission before world identity, runtime owners, or publication are allocated. P14-A and P14-B standalone profile behavior and the P10 profiles remain independently supported within their existing scopes. A future combined world needs distinct factual Locations from the appropriate geography/genesis layer. Historical succession from a City to ruins remains a possible future representation, as does a ruin represented as local/site content inside a living City; neither representation is decided or implemented by this checkpoint. This decision defers only the current combined bootstrap profile and does not claim that Cities and Ruins can never be historically related.

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

The candidate preserves stable settlement/source/store/item identity,
LocationId-to-City anchor agreement, title/custody separation, atomic source
overflow rejection, stock-limited actual consumption, the `Economy.Enabled`
gate, deterministic closing-balance diagnostics, and the reconstruction
inventory in the checkpoint contract.

Independent code review passed at exact candidate `6d37afa` against base
`0bb47b1`. The five focused EditMode suites passed on that exact integrated
tree: `LocalDailyMaterialFlowTests` 13/13,
`SettlementStockOwnershipTests` 14/14, `PopulationConsumptionTests` 22/22,
`WorldStateDiagnosticsTests` 46/46, and `EconomyTransactionTests` 33/33.
Final integration gates also passed: ALL EditMode 1712/1712, official complete
EditMode `Smoke` 5/5, `SimulationRuntimeLongRunTests` 7/7, and
`git diff --check`. Unity result XML/logs are under `Temp/ValidationResults`
in the P14 implementation worktree for this exact commit; the separate
integration checkout's first cold Unity invocation stopped during package
resolution with `ENOSPC` before test discovery, so the gates were rerun
successfully using the already-resolved checkout at the identical commit.
These results validate the integration candidate promoted to
`codex/phase14/canonical` at `c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`.

Run focused economy, market stock, free-consumption, identity/anchor, overflow,
disabled-economy, diagnostic, and same-input determinism coverage. Independently
review the full diff against its actual base. Before code integration or
promotion, run repository-required regression suites, official full Smoke,
`git diff --check`, and any daily-loop validation required by the final diff.
Record exact evidence here after each validated candidate. P14 is not closed
until the approved objective is delivered, required review and validation pass,
the capability is canonically promoted with human approval, and a separate
closure record is independently reviewed.
