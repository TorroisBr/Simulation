# P16-A current-base implementation candidate handoff

**Checkpoint:** P16-A, One Passage Military Movement with Finite Supply. **Status:** implementation candidate pending independent exact-tip review and canonical promotion. **Architecture:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`. **Current-base candidate commit:** `aefe4335eb02e03876afa411610c2705c3895a66`; executable code tree: `0e31475f4944606fbe90a09955057d9b83b9e42f`. **Integrated base:** P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4`, including P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` and the promoted P14-A/P8 authorities. The earlier owner-only implementation `d2276ae` was recomposed here without rewriting its published history.

## Delivered boundary

- `ArmedForceSpatialStateStore` remains the sole P16 position owner. Its selected P16-A record combines the one-force binding, factual position, one compatible item with finite carried quantity, and the one-shot successful crossing receipt. The low-level crossing mutation is internal; callers use `SimulationRuntime.TryExecuteP16AMilitaryCrossing`.
- The explicit `P16AOneHopMilitary` composition requires the selected P16 profile and existing force/spatial authorities. The operation revalidates the force, current source, registered adjacent Hexes, passage, revisions, fixed `P16A-MilitaryOneHop/v1` context, supply, and one-shot receipt before replacing the owner record once.
- The runtime captures its construction-thread identity for this profile, rejects off-owner-thread operations, and uses the existing non-reentrant advance/operation lease. The lease is reentrancy protection under the current single-writer model, not a general cross-thread lock. Rejection leaves position, supply, receipt, and revision unchanged.
- This is a one-boundary daily proving operation. The P16-A composition rejects a P18 intraday profile because the current receipt records an absolute day and fixed operation order, not a sub-day logical instant/causal sequence. Timed military movement remains outside this checkpoint.
- `UnityBootstrap-Daily-v1` rejects both unmoved populated P16 state and state after a committed crossing before P12 admission can publish it. Tests assert the exact position, quantity, receipt reference, and owner revision remain unchanged; the empty daily P16 profile still composes.
- P16-A is not wired into a production Unity bootstrap/gameplay world. No new content, order UI, route planning, replenishment, automatic travel, Battle/War/occupation effects, or P12 serialization is included.

## Exact-tree validation

All validation below ran after the final P16 source/test edits, on executable tree `0e31475f4944606fbe90a09955057d9b83b9e42f`. The exact result XML and log files are retained under `docs/validation/P16A/` and included with this candidate evidence.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P16AMilitaryMovementTests` | 17/17 | `F116E91835301AADDE5BBA72C7EBA9BE78AA389CCF6308178DDA59BDB8CA1EE2` | `479D892EEE620949B6338CB075C640785DF91B9BEE622676CB0249AB9D84F00D` |
| ALL EditMode | 2268/2268 | `3E4D1C20DFE4BB83D66433ED3F68E32EDB62D65DED90C7284B80DA84EC6E3DB2` | `05A53DE8B4CC44FD909FC9ABC0BB2665A18597130DE3F23520CD920461F40B2C` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `12AA0BA0F7FFA821C41FDDE2639F334A47E8D28BDAC286DBC8E90FD6E54BD64D` | `F07DBFAD8A77F87B40AF4F28369DEAFF7F10E8B6DEA4D02EE113BCFE17F4415B` |
| `git diff --check` | PASS | — | — |

The result artifacts are retained under `docs/validation/P16A/`. The Unity logs are stored together in `P16A-validation-logs.zip` to avoid committing a 102 MB raw ALL EditMode log. The original uncompressed log SHA-256 values remain in the table; the archive SHA-256 is `92598C986B963416BD7AC4F7CC2B40237F32DF7CAC39A00C82A0E16931F07096`.

- `EditMode-20261004-025152-08c276630c79456e9bf95394773c96b0.xml` — focused P16-A suite.
- `EditMode-20261004-025229-0b26922294b84da58071ea458108a52d.xml` — ALL EditMode.
- `EditMode-20261004-025331-e414b1dd3b794d4db260a4f045444804.xml` — Official Smoke.

Each log is archived under its original filename inside `P16A-validation-logs.zip`; extracting reproduces the exact raw logs represented by the table's hashes.

The full EditMode run includes the affected P7/P8/P12/P15 regressions. No daily-loop behavior changed, so a long-run gate is not implied.

## Limits and review gate

This candidate does not claim P12-B completion, complete owner or shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-A readiness, or P13 readiness. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked. P16 remains open after P16-A until its Phase objective and any later checkpoints are handled separately.

The code candidate is not yet independently reviewed or canonical. Review must cover the complete exact-tip diff against the current P15/P12 base, P16-A scope, the P18 temporal exclusion, P12 fail-closed admission, exact unchanged-state rejection behavior, and all three validation artifact pairs. Canonical promotion and a formal State record follow the repository Execution Model after review/preflight.
