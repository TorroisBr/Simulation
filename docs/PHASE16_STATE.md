# Phase 16 State — Military Movement & Logistics v1

**Status:** PHASE 16 IN PROGRESS. P16-A is PROMOTED; Phase 16 remains open.

**Canonical branch:** `codex/phase16/canonical`. **P16-A promotion commit:** `bbf3e49b160ca5ce42bde43e76f1f93aab7585e9`, created as a clean branch from the exact reviewed and validated candidate. This State-only update follows that promotion.

**Architecture baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`. The accepted P16-A scope and technical design were promoted in architecture at `a2788e6400251b5ce3cf8d2269ea8a5b5fdfd4a8`; the current architecture tip descends from that promotion. The P16 Brief marks P16-A `READY_FOR_MASTER_IMPLEMENTATION_HANDOFF`.

## Checkpoint status

| Checkpoint | Status | Evidence / boundary |
|---|---|---|
| P16-A — One Passage Military Movement with Finite Supply | PROMOTED | Promotion commit `bbf3e49b160ca5ce42bde43e76f1f93aab7585e9`; integrated code commit `98f80648a226212cd13c37bce34d0e2d6c68574a`, executable tree `1abb2f83817659fa9bce8e66826f79fde34765b8`. Independent exact-tip implementation review PASS is recorded in `docs/design/PHASE16A_IMPLEMENTATION_REVIEW.md`. |

## Integrated prerequisites and validation

The current-base candidate incorporates P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4`, P14 canonical `06e9c30101a74bd618d3651885c489c79fe866bb` (including P14-B and P10-B), P10 canonical `e53252de5277fd5af46bbacb8eda5ee6e74aff08`, and P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. The current P12 canonical ref remains at that exact tip. The P14/P10 integration was classified `BASE_DRIFT_ONLY`; production changes were disjoint, and the combined City/Ruin profile remains rejected before identity allocation or publication.

Validation on code commit `98f80648a226212cd13c37bce34d0e2d6c68574a`, tree `1abb2f83817659fa9bce8e66826f79fde34765b8`, using Unity `6000.3.9f1`:

- `P16AMilitaryMovementTests`: 20/20 PASS.
- `SimulationRuntimeAdmissionTests`: 37/37 PASS.
- `SimulationBootstrapCompositionTests`: 21/21 PASS.
- ALL EditMode: 2323/2323 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check`: PASS.

Exact XML/log hashes and archive SHA-256 `5C7C6D878EB880A5879A558AD52370144C782413F05E8F3904097E6FCDD09BFE` are recorded in `docs/validation/P16A/P16A-current-base-revalidation-1abb2f8.md`.

## Scope and limits

P16-A covers one selected existing ArmedForce, one existing valid passage crossing, one successful operation, and a finite carried quantity of one compatible supply item. The existing military position owner holds position, supply, receipt, selected-force binding, and revision together. The controlled operation has one daily boundary/order and no duration.

The checkpoint does not add route planning, multiple legs, timed movement, replenishment, travel parties, Person availability, new geography or settlement, Battle/War/occupation/control effects, production bootstrap/gameplay content, Save/Replay, or P12 serialization. P18 intraday execution remains excluded. The selected P12 Daily profile rejects populated P16 state before publication.

P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked. This checkpoint does not claim complete owner or shared-epoch coverage, global quiescence, capture eligibility, export, or hydration. Phase 16 remains open; no Phase closure is claimed.
