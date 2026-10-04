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

All validation below ran after the final P16 source/test edits, on executable tree `0e31475f4944606fbe90a09955057d9b83b9e42f`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P16AMilitaryMovementTests` | 17/17 | `AB7FC56C37BCC8A0CC332D33A77007D654244BF3859B21D8C695DDA0841104FD` | `89BE1434AEF012D027D858C59ADA1CAC5B088ADAE35DCD2220C6C791331BCEE0` |
| ALL EditMode | 2268/2268 | `13F8ED91EC47CEE85901533A6246A7107003428C0FA523A6058BC1E9710371CF` | `0D50E59155E93D6B1440F2A36D0458F15E4FDDFB1244154C5A34051B328EAA34` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `534F8BCDB5557CE94744C3C27C9BA530BAF6C3C4C227E09E7D244763BD87F5C6` | `80376041FD2A10CC5E048D37D063F5FF4BFDB9B05B8427A7C1FC8763A8AD1300` |
| `git diff --check` | PASS | — | — |

The corresponding XML/log filenames are retained in this candidate worktree under `Temp/ValidationResults/`:

- `EditMode-20261004-024226-724b7728b8ed49e597c211c686162315.xml` and `.log`
- `EditMode-20261004-024256-0d4e311577db451bb0a9faf4c8e0ad4e.xml` and `.log`
- `EditMode-20261004-024352-ec4185fb8c7e4890a8982e9549d9b441.xml` and `.log`

The full EditMode run includes the affected P7/P8/P12/P15 regressions. No daily-loop behavior changed, so a long-run gate is not implied.

## Limits and review gate

This candidate does not claim P12-B completion, complete owner or shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-A readiness, or P13 readiness. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked. P16 remains open after P16-A until its Phase objective and any later checkpoints are handled separately.

The code candidate is not yet independently reviewed or canonical. Review must cover the complete exact-tip diff against the current P15/P12 base, P16-A scope, the P18 temporal exclusion, P12 fail-closed admission, exact unchanged-state rejection behavior, and all three validation artifact pairs. Canonical promotion and a formal State record follow the repository Execution Model after review/preflight.